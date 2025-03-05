const axios = require('axios');

const weatherApiHost = 'http://128.98.09.0:8081';

function getWeatherData(city, date) {
  return axios.get(weatherApiHost + '/weather', {
    params: { city, date }
  }).then(response => response.data)
    .catch(error => {
      console.error('Error fetching weather data: ' + error);
      throw new Error('Weather data fetch error');
    });
}