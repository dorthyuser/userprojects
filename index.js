try {
  require("dotenv").config();
  const express = require("express");
  const cors = require("cors");
  const routes = require("./routes");

  const app = express();
  app.use(express.json());
  app.use(cors({ origin: true, methods: ["GET", "POST", "PUT", "DELETE", "OPTIONS"] }));

  app.use("/convert", routes);

  const port = process.env.PORT || 8080;
  app.listen(port, () => console.log("Connected"));

  process.on("uncaughtException", (err) => { console.error("Failed", err); });
  process.on("unhandledRejection", (err) => { console.error("Failed", err); });
} catch (err) {
  console.error("Failed", err);
}
