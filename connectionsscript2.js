// This can be another connection or a different resource connection.
const axios = require('axios');

const backendService = {
  host: '128.98.09.0',
  port: 8081,
  getWeather: async function(city, date) {
    try {
      const response = await axios.get(`http://${this.host}:${this.port}/weather`, { params: { city, date } });
      return response.data;
    } catch (error) {
      console.error('Error fetching weather data:', error);
      throw new Error('Failed to fetch weather details.');
    }
  }
};