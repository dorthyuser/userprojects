const express = require('express');
const mysql = require('mysql');
const axios = require('axios');

const app = express();
const port = 3000;

const dbConnection = mysql.createConnection({
  host: '128.98.09.0',
  port: 8081,
  user: 'yourUsername',
  password: 'yourPassword',
  database: 'weatherDB'
});

dbConnection.connect(function(err) {
  if (err) {
    console.error('Error connecting: ' + err.stack);
    return;
  }
  console.log('Connected as id ' + dbConnection.threadId);
});

app.get('/weather', async (req, res) => {
  const { city, date } = req.query;

  if (!city || (date && !/\d{4}-\d{2}-\d{2}/.test(date))) {
    return res.status(400).json({ error: 'Invalid input parameters.' });
  }

  try {
    const weatherData = await axios.get(`http://128.98.09.0:8081/weather`, { params: { city, date } });
    res.json(weatherData.data);
  } catch (error) {
    res.status(500).json({ error: 'Failed to fetch weather details.' });
  }
});

app.listen(port, () => {
  console.log(`Server running at http://localhost:${port}`);
});