try {
  const config = {
    loginUrl: process.env.SALESFORCE_LOGIN_URL || 'https://login.salesforce.com'
  };
  console.log('Connected connections/config/salesforce.js');
  module.exports = config;
} catch (error) {
  console.log('Failed connections/config/salesforce.js', error);
  module.exports = { loginUrl: 'https://login.salesforce.com' };
}
