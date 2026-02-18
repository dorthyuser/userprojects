const { getPool } = require("../../helpers/db/connection");
const { validateBearerToken } = require("../../helpers/auth/validateToken");
const { validatePayload } = require("../../helpers/validator");
const { v4: uuidv4 } = require("uuid");

module.exports = async function (context, req) {
  const log = (msg, ...args) => context.log(msg, ...args);
  try {
    log("Incoming request for create-travelcard");

    // Header validations
    const clientId =
      req.headers &&
      (req.headers["client_id"] ||
        req.headers["client-id"] ||
        req.headers["clientid"]);
    if (
      !clientId ||
      typeof clientId !== "string" ||
      clientId.length < 1 ||
      clientId.length > 128 ||
      !/^[\w+]+$/.test(clientId)
    ) {
      context.res = {
        status: 400,
        body: {
          error: {
            code: "INVALID_HEADER",
            message: "Missing or invalid client_id header",
            details: {},
          },
        },
      };
      return;
    }

    const authHeader =
      req.headers &&
      (req.headers["authorization"] || req.headers["Authorization"]);

    // Validate token (throws on failure)
    try {
      validateBearerToken(authHeader, clientId, (m, e) =>
        log("Token validation detail", m, e)
      );
    } catch (e) {
      log("Auth error", e.message);
      context.res = {
        status: e.status || 401,
        body: {
          error: { code: "UNAUTHORIZED", message: e.message, details: {} },
        },
      };
      return;
    }

    // Get payload from query param 'payload' which should be a JSON string
    const payloadRaw = (req.query && req.query.payload) || "";
    if (!payloadRaw) {
      context.res = {
        status: 400,
        body: {
          error: {
            code: "MISSING_PAYLOAD",
            message: "Missing payload query parameter",
            details: {},
          },
        },
      };
      return;
    }

    let payload;
    try {
      payload =
        typeof payloadRaw === "string" ? JSON.parse(payloadRaw) : payloadRaw;
    } catch (e) {
      context.res = {
        status: 400,
        body: {
          error: {
            code: "INVALID_JSON",
            message: "payload is not valid JSON",
            details: e.message,
          },
        },
      };
      return;
    }

    // Validate business rules
    const { errors } = validatePayload(payload);
    if (errors && errors.length) {
      context.res = {
        status: 400,
        body: {
          error: {
            code: "VALIDATION_ERROR",
            message: "Payload validation failed",
            details: errors,
          },
        },
      };
      return;
    }

    // Persist to database
    const pool = getPool();
    const client = await pool.connect();
    try {
      await client.query("BEGIN");

      // Insert travelcard
      const travelcardUuid = uuidv4();
      const insertTravelcardText = `INSERT INTO travelcards
        (travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to)
        VALUES ($1,$2,$3,$4,$5,$6,$7,$8)
        RETURNING id`;
      const travelcardValues = [
        payload.travelcardType,
        payload.travelcardValidFrom ? payload.travelcardValidFrom : null,
        payload.travelcardValidTo ? payload.travelcardValidTo : null,
        payload.travelcardName ? payload.travelcardName : null,
        payload.travelcardNumber,
        payload.travelcardRequestedDate,
        payload.travelcardTransactionReference,
        payload.travelcardUsableTo ? payload.travelcardUsableTo : null,
      ];
      const resTravel = await client.query(
        insertTravelcardText,
        travelcardValues
      );
      const travelcardDbId = resTravel.rows[0] && resTravel.rows[0].id;

      // Insert cardholders
      const chInserts = [];
      for (const ch of payload.cardholders) {
        const insertChText = `INSERT INTO cardholders
          (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key)
          VALUES ($1,$2,$3,$4,$5,$6,$7,$8,$9)`;
        const chValues = [
          travelcardDbId,
          ch.cardholderTitle,
          ch.cardholderForename,
          ch.cardholderSurname,
          ch.cardholderType === "Primary" ? "PRIMARY" : "SECONDARY",
          ch.cardholderPhotoName,
          ch.cardholderPhotoRRSKey || null,
          ch.cardholderPhotoURL || null,
          ch.cardholderPhotoKey || null,
        ];
        chInserts.push(client.query(insertChText, chValues));
      }
      await Promise.all(chInserts);

      await client.query("COMMIT");

      // Return success with generated token (short random token) and travelcardId (uuid)
      const token = Math.random().toString(36).substring(2, 8).toUpperCase();
      context.res = {
        status: 200,
        body: {
          travelcardId: travelcardUuid,
          token,
        },
      };
      return;
    } catch (e) {
      await client.query("ROLLBACK");
      log("DB error", e);
      context.res = {
        status: 500,
        body: {
          error: {
            code: "DB_ERROR",
            message: "Failed to persist travelcard",
            details: e.message,
          },
        },
      };
      return;
    } finally {
      client.release();
    }
  } catch (err) {
    log("Unhandled error", err);
    context.res = {
      status: err.status || 500,
      body: {
        error: {
          code: "INTERNAL_ERROR",
          message: err.message || "An internal error occurred",
          details: {},
        },
      },
    };
  }
};
