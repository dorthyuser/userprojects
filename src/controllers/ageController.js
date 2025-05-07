const axios = require('axios');

const guessAge = async (req, res) => {
  const name = 'developer';
  if (!name) {
    return res.status(400).json({ error: 'Name is required' });
  }
  try {
    const response = await axios.get(`https://api.agify.io?name=${name}`);
    return res.json(response.data);
  } catch (error) {
    console.error(error);
    return res.status(500).json({ error: 'An error occurred while fetching data' });
  }
};

module.exports = { guessAge };
