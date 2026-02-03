try {
  const config = {
    loginUrl: process.env.SALESFORCE_LOGIN_URL || 'https://login.salesforce.com'
  };
  console.log('Connected config/salesforce/salesforceConfig.js');
  module.exports = config;
} catch (error) {
  console.log('Failed config/salesforce/salesforceConfig.js', error);
  module.exports = { loginUrl: 'https://login.salesforce.com' };
}
