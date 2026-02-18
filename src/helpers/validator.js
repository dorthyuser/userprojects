const Ajv = require("ajv");
const addFormats = require("ajv-formats");
const { DateTime } = require("luxon");
const ajv = new Ajv({ allErrors: true, strict: false });
addFormats(ajv);

const travelcardTypes = [
  "Young",
  "Barcklays",
  "DevonandCornwall",
  "TwoTogether",
  "Family",
  "Senior",
  "DisabledPersons",
  "Network",
  "TwentySixToThirty",
  "SixteenToSeventeen",
  "Veterans",
];

// Define travelcard types that allow a secondary cardholder
const typesAllowingSecondary = [
  "TwoTogether",
  "Family",
  "TwentySixToThirty",
  "DevonandCornwall",
];

const schema = {
  type: "object",
  properties: {
    travelcardType: { type: "string", enum: travelcardTypes },
    travelcardValidFrom: { type: "string", format: "date-time" },
    travelcardValidTo: { type: "string", format: "date-time" },
    travelcardName: {
      type: "string",
      maxLength: 255,
      pattern: "^[A-Za-z0-9 ]*$",
    },
    travelcardNumber: {
      type: "string",
      minLength: 11,
      maxLength: 22,
      pattern: "^[A-Za-z0-9]+$",
    },
    travelcardRequestedDate: { type: "string", format: "date-time" },
    travelcardTransactionReference: {
      type: "string",
      pattern: "^\\d{2}[A-Z0-9]{4}\\d{4}\\d{5}$",
      minLength: 15,
      maxLength: 15,
    },
    travelcardUsableTo: { type: "string", format: "date-time" },
    cardholders: {
      type: "array",
      minItems: 1,
      maxItems: 2,
      items: {
        type: "object",
        properties: {
          cardholderTitle: {
            type: "string",
            minLength: 1,
            maxLength: 15,
            pattern: "^(?!.*[×÷ˇ˘μ])[A-Za-zÀ-žºª .'’\\-]+$",
          },
          cardholderForename: {
            type: "string",
            minLength: 1,
            maxLength: 100,
            pattern: "^(?!.*[×÷ˇ˘μ])[A-Za-zÀ-ž .'’\\-]+$",
          },
          cardholderSurname: {
            type: "string",
            minLength: 1,
            maxLength: 100,
            pattern: "^(?!.*[×÷ˇ˘μ])[A-Za-zÀ-ž .'’\\-]+$",
          },
          cardholderType: { type: "string", enum: ["Primary", "Secondary"] },
          cardholderPhotoName: {
            type: "string",
            minLength: 1,
            maxLength: 100,
            pattern: "^(?!.*[×÷ˇ˘μ])[A-Za-z0-9À-ž _.\\-()\\[\\]',&+#]+$",
          },
          cardholderPhotoRRSKey: {
            type: "string",
            minLength: 39,
            maxLength: 42,
            pattern: "^[A-Za-z0-9-]{36}\\.[A-Za-z0-9]{2,5}$",
          },
          cardholderPhotoURL: {
            type: "string",
            minLength: 20,
            maxLength: 2048,
            pattern: "^(https?://)[A-Za-z0-9._~:/?#@!$&'()\\*+,;=%-]+",
          },
          cardholderPhotoKey: {
            type: "string",
            minLength: 39,
            maxLength: 42,
            pattern: "^[A-Za-z0-9-]{36}\\.[A-Za-z0-9]{2,5}$",
          },
        },
        required: [
          "cardholderTitle",
          "cardholderForename",
          "cardholderSurname",
          "cardholderType",
          "cardholderPhotoName",
        ],
        additionalProperties: false,
      },
    },
  },
  required: [
    "travelcardType",
    "travelcardValidFrom",
    "travelcardValidTo",
    "travelcardNumber",
    "travelcardRequestedDate",
    "travelcardTransactionReference",
    "cardholders",
  ],
  additionalProperties: false,
};

const validateSchema = ajv.compile(schema);

function businessValidations(payload) {
  const now = DateTime.utc();
  const errors = [];

  const requested = DateTime.fromISO(payload.travelcardRequestedDate, {
    zone: "utc",
  });
  if (!requested.isValid) {
    errors.push("travelcardRequestedDate is not a valid date-time");
  } else if (requested >= now) {
    errors.push("Requested date must be in the past");
  }

  const validFrom = DateTime.fromISO(payload.travelcardValidFrom, {
    zone: "utc",
  });
  const validTo = DateTime.fromISO(payload.travelcardValidTo, { zone: "utc" });
  if (!validFrom.isValid)
    errors.push("travelcardValidFrom is not a valid date-time");
  if (!validTo.isValid)
    errors.push("travelcardValidTo is not a valid date-time");

  if (validFrom.isValid && validTo.isValid && validFrom > validTo) {
    errors.push("valid_from date must not be later than valid_to date");
  }
  if (validTo.isValid && validTo <= now) {
    errors.push("valid_to date must be in the future");
  }

  // travelcardValidFrom must not be later than one calendar month from creation date (now)
  if (validFrom.isValid) {
    const oneMonthLater = now.plus({ months: 1 }).endOf("day");
    if (validFrom > oneMonthLater) {
      errors.push(
        "travelcardValidFrom must not be later than one calendar month from now"
      );
    }
  }

  if (payload.travelcardType === "SixteenToSeventeen") {
    if (!payload.travelcardUsableTo) {
      errors.push(
        "travelcardUsableTo is required for SixteenToSeventeen travelcards"
      );
    } else {
      const usableTo = DateTime.fromISO(payload.travelcardUsableTo, {
        zone: "utc",
      });
      if (!usableTo.isValid)
        errors.push("travelcardUsableTo is not a valid date-time");
      else if (usableTo <= now)
        errors.push("travelcardUsableTo must be in the future");
    }
  }

  // Cardholders validations
  const ch = payload.cardholders || [];
  const primaryCount = ch.filter((c) => c.cardholderType === "Primary").length;
  const secondaryCount = ch.filter(
    (c) => c.cardholderType === "Secondary"
  ).length;
  if (primaryCount !== 1)
    errors.push("Exactly one Primary cardholder is required");
  if (secondaryCount > 1)
    errors.push("At most one Secondary cardholder is allowed");
  if (ch.length < 1 || ch.length > 2)
    errors.push("Total cardholders must be 1 or 2");

  if (
    secondaryCount === 1 &&
    !typesAllowingSecondary.includes(payload.travelcardType)
  ) {
    errors.push(
      "Secondary cardholder is not allowed for the selected travelcard type"
    );
  }

  // Each cardholder must have exactly one image field provided
  ch.forEach((c, idx) => {
    const provided = [
      c.cardholderPhotoRRSKey ? 1 : 0,
      c.cardholderPhotoURL ? 1 : 0,
      c.cardholderPhotoKey ? 1 : 0,
    ].reduce((a, b) => a + b, 0);
    if (provided !== 1)
      errors.push(
        `Cardholder at index ${idx} must provide exactly one of cardholderPhotoRRSKey, cardholderPhotoURL or cardholderPhotoKey`
      );
  });

  return errors;
}

function validatePayload(payload) {
  const valid = validateSchema(payload);
  const errors = [];
  if (!valid) {
    validateSchema.errors.forEach((e) =>
      errors.push(`${e.instancePath} ${e.message}`)
    );
  }
  const biz = businessValidations(payload);
  return { errors: errors.concat(biz) };
}

module.exports = {
  validatePayload,
};
