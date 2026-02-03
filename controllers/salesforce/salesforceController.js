try {
  const { salesforceConnection } = require('../../connections/salesforce');

  // Helper to format billing address
  function mapBillingAddress(record) {
    return {
      street: record.BillingStreet || '',
      city: record.BillingCity || '',
      state: record.BillingState || '',
      postalCode: record.BillingPostalCode || '',
      country: record.BillingCountry || ''
    };
  }

  async function getAccount(req, res) {
    const id = req.params.id;
    try {
      const record = await salesforceConnection.sobject('Account').retrieve(id);
      const mapped = {
        Id: record.Id || null,
        Name: record.Name || null,
        Type: record.Type || null,
        Industry: record.Industry || null,
        Phone: record.Phone || null,
        BillingAddress: mapBillingAddress(record),
        CreatedDate: record.CreatedDate || null,
        LastModifiedDate: record.LastModifiedDate || null
      };
      return res.json(mapped);
    } catch (error) {
      console.error('Error in getAccount:', error);
      return res.status(500).json({ errors: [String(error.message || error)] });
    }
  }

  async function createAccount(req, res) {
    try {
      const body = req.body || {};
      const account = {
        Name: body.Name,
        Type: body.Type,
        Industry: body.Industry,
        Phone: body.Phone,
        BillingStreet: body.BillingAddress && body.BillingAddress.street,
        BillingCity: body.BillingAddress && body.BillingAddress.city,
        BillingState: body.BillingAddress && body.BillingAddress.state,
        BillingPostalCode: body.BillingAddress && body.BillingAddress.postalCode,
        BillingCountry: body.BillingAddress && body.BillingAddress.country
      };

      const result = await salesforceConnection.sobject('Account').create(account);

      // jsforce returns an object with id and success and errors
      return res.json({ id: result.id || null, success: result.success || false, errors: result.errors || [] });
    } catch (error) {
      console.error('Error in createAccount:', error);
      return res.status(500).json({ id: null, success: false, errors: [String(error.message || error)] });
    }
  }

  async function updateAccount(req, res) {
    const id = req.params.id;
    try {
      const body = req.body || {};
      const account = {
        Id: id,
        Name: body.Name,
        Type: body.Type,
        Industry: body.Industry,
        Phone: body.Phone,
        BillingStreet: body.BillingAddress && body.BillingAddress.street,
        BillingCity: body.BillingAddress && body.BillingAddress.city,
        BillingState: body.BillingAddress && body.BillingAddress.state,
        BillingPostalCode: body.BillingAddress && body.BillingAddress.postalCode,
        BillingCountry: body.BillingAddress && body.BillingAddress.country
      };

      const result = await salesforceConnection.sobject('Account').update(account);

      return res.json({ success: result.success || false, errors: result.errors || [] });
    } catch (error) {
      console.error('Error in updateAccount:', error);
      return res.status(500).json({ success: false, errors: [String(error.message || error)] });
    }
  }

  async function deleteAccount(req, res) {
    const id = req.params.id;
    try {
      const result = await salesforceConnection.sobject('Account').destroy(id);
      return res.json({ success: result.success || false, errors: result.errors || [] });
    } catch (error) {
      console.error('Error in deleteAccount:', error);
      return res.status(500).json({ success: false, errors: [String(error.message || error)] });
    }
  }

  module.exports = {
    getAccount,
    createAccount,
    updateAccount,
    deleteAccount
  };

  console.log('Connected controllers/salesforce/salesforceController.js');
} catch (error) {
  console.log('Failed controllers/salesforce/salesforceController.js', error);
  module.exports = {
    getAccount: (req, res) => res.status(500).json({ errors: ['Controller failed to load'] }),
    createAccount: (req, res) => res.status(500).json({ id: null, success: false, errors: ['Controller failed to load'] }),
    updateAccount: (req, res) => res.status(500).json({ success: false, errors: ['Controller failed to load'] }),
    deleteAccount: (req, res) => res.status(500).json({ success: false, errors: ['Controller failed to load'] })
  };
}
