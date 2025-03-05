const express = require('express');
const app = express();
const axios = require('axios');

const config = require('./config.yaml');
const mysql = require('mysql');

const dbConnection = mysql.createConnection({
  host: config.http.host,
  port: config.http.port,
  user: config.mysql.user,
  password: config.mysql.password,
  database: config.mysql.database
});

dbConnection.connect((err) => {
  if (err) {
    console.error('Database connection failed: ' + err.stack);
    return;
  }
  console.log('Connected to the database');
});

app.get('/weather', (req, res) => {
  const { city, date } = req.query;

  if (!city || !date) {
    return res.status(400).json({ error: 'City and date are required' });
  }

  axios.get(`http://128.98.09.0:8081/weather`, {
    params: { city, date }
  })
    .then(response => {
      res.json({ output: response.data });
    })
    .catch(err => {
      console.error('Error fetching weather data: ' + err);
      res.status(500).json({ error: 'Weather data fetch error' });
    });
});

const PORT = process.env.PORT || 3000;
app.listen(PORT, () => {
  console.log(`Server is running on port ${PORT}`);
});