const jsforce = require('jsforce');

class SalesforceClient {
  static async getClient() {
    const conn = new jsforce.Connection({
      loginUrl: process.env.SALESFORCE_LOGIN_URL,
      instanceUrl: process.env.SALESFORCE_INSTANCE_URL,
      version: process.env.SALESFORCE_API_VERSION || '59.0',
      maxRequest: Number(process.env.SALESFORCE_MAX_RETRIES || 3)
    });
    return conn;
  }
}

module.exports = { SalesforceClient };