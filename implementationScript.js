const express = require('express');
const app = express();

app.get('/weather', (req, res) => {
  const { city, date } = req.query;

  if (!city || !date) {
    return res.status(400).json({ error: 'City and date are required' });
  }

  getWeatherData(city, date)
    .then(data => {
      res.json({ output: data });
    })
    .catch(err => {
      res.status(500).json({ error: err.message });
    });
});

const PORT = process.env.PORT || 3000;
app.listen(PORT, () => {
  console.log(`Server is running on port ${PORT}`);
});