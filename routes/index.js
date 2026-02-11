module.exports = (function () {
  try {
    const express = require("express");
    const router = express.Router();

    // POST /convert - accepts query parameters or JSON body
    router.post("/", async (req, res) => {
      try {
        const fromRaw = (req.query.from || (req.body && req.body.from) || "USD");
        const toRaw = (req.query.to || (req.body && req.body.to) || "INR");
        const amountRaw = req.query.amount || (req.body && req.body.amount);

        const from = String(fromRaw).toUpperCase();
        const to = String(toRaw).toUpperCase();
        const amount = parseFloat(amountRaw);

        if (!isFinite(amount)) {
          const error = { error: "Invalid amount" };
          console.error("Failed", error);
          return res.status(400).json(error);
        }

        // Determine rate using environment variable RATE_USD_INR
        const envRate = parseFloat(process.env.RATE_USD_INR || process.env.BACKEND_RATE_USD_INR || "82.5");
        let rate;

        if (from === to) {
          rate = 1;
        } else if (from === "USD" && to === "INR") {
          rate = envRate;
        } else if (from === "INR" && to === "USD") {
          rate = envRate ? 1 / envRate : 1;
        } else {
          const error = { error: "Conversion not supported" };
          console.error("Failed", error);
          return res.status(400).json(error);
        }

        const converted = +(amount * rate);

        const result = {
          amount: amount,
          from: from,
          to: to,
          rate: rate,
          converted: converted,
          timestamp: new Date().toISOString()
        };

        return res.status(200).json(result);
      } catch (err) {
        console.error("Failed", err);
        return res.status(500).json({ error: "Internal Server Error" });
      }
    });

    console.log("Connected");
    return router;
  } catch (err) {
    console.error("Failed", err);
    throw err;
  }
})();
