const { getDatabase } = require("../../helpers/mongodb/connection");
const {
  jsonResponse,
  errorResponse,
} = require("../../helpers/common/response");
const { validateHotelsQuery } = require("../../helpers/common/validation");
const { safeLog } = require("../../helpers/common/logger");

function createHotelsFunction(app) {
  app.http("get-hotels", {
    methods: ["GET"],
    authLevel: "anonymous",
    route: "hotels",
    handler: async (request, context) => {
      const start = Date.now();
      try {
        const { errors, value } = validateHotelsQuery(
          Object.fromEntries(request.query.entries())
        );
        if (errors.length)
          return errorResponse(
            400,
            "VALIDATION_ERROR",
            "Invalid query parameters",
            errors
          );

        const db = await getDatabase();
        const hotels = await db
          .collection("hotels")
          .aggregate([
            { $match: { city: value.city, isActive: true } },
            { $sort: { hotelName: 1 } },
            { $skip: (value.page - 1) * value.pageSize },
            { $limit: value.pageSize },
            {
              $project: {
                _id: 0,
                hotelId: 1,
                hotelName: 1,
                city: 1,
                availableRooms: 1,
                minNightlyRate: 1,
                currency: 1,
              },
            },
          ])
          .toArray();

        if (!hotels.length)
          return errorResponse(404, "NOT_FOUND", "No hotels found");
        safeLog(context, "Hotels search completed", {
          durationMs: Date.now() - start,
          city: value.city,
        });
        return jsonResponse(200, {
          data: hotels,
          page: value.page,
          pageSize: value.pageSize,
          totalItems: hotels.length,
          totalPages: 1,
        });
      } catch {
        return errorResponse(500, "SERVER_ERROR", "Unexpected error");
      }
    },
  });
}

module.exports = { createHotelsFunction };
