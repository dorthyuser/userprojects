const fs = require("fs");

function loadProperties() {
  const location = process.env.properties;
  if (!location) return {};
  try {
    const raw = fs.readFileSync(location, "utf8");
    return JSON.parse(raw);
  } catch {
    return {};
  }
}

module.exports = { loadProperties };
