const express = require('express');
const app = express();
const port = 3000;

app.get('/weather', async (req, res) => {
  const { city, date } = req.query;

  if (!city || (date && !/\d{4}-\d{2}-\d{2}/.test(date))) {
    return res.status(400).json({ error: 'Invalid input parameters.' });
  }

  try {
    const weatherData = await backendService.getWeather(city, date);
    res.json(weatherData);
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

app.listen(port, () => {
  console.log(`Server running at http://localhost:${port}`);
});