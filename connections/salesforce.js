try {
  const jsforce = require('jsforce');
  const salesforceConfig = require('./config/salesforce');

  const salesforceConnection = new jsforce.Connection({ loginUrl: salesforceConfig.loginUrl });

  (async () => {
    try {
      const username = process.env.SALESFORCE_USERNAME;
      const password = process.env.SALESFORCE_PASSWORD || '';
      const token = process.env.SALESFORCE_TOKEN || '';
      if (username && (password || token)) {
        await salesforceConnection.login(username, (password || '') + (token || ''));
        console.log('Connected salesforce');
      } else {
        console.log('Failed salesforce - missing credentials');
      }
    } catch (error) {
      console.log('Failed salesforce', error);
    }
  })();

  module.exports = { salesforceConnection };
} catch (error) {
  console.log('Failed connections/salesforce.js', error);
  module.exports = { salesforceConnection: { sobject: () => ({ retrieve: async () => { throw new Error('Salesforce connection failed'); }, create: async () => ({ id: null, success: false, errors: ['Salesforce connection failed'] }), update: async () => ({ success: false, errors: ['Salesforce connection failed'] }), destroy: async () => ({ success: false, errors: ['Salesforce connection failed'] }) }) } };
}
