try {
  const express = require('express');
  const router = express.Router();
  const controller = require('../../controllers/salesforce/salesforceController');

  // GET account by Id
  router.get('/accounts/:id', controller.getAccount);

  // POST create account
  router.post('/accounts', controller.createAccount);

  // PATCH update account
  router.patch('/accounts/:id', controller.updateAccount);

  // DELETE account
  router.delete('/accounts/:id', controller.deleteAccount);

  console.log('Connected routes/salesforce/salesforceRoutes.js');

  module.exports = router;
} catch (error) {
  console.log('Failed routes/salesforce/salesforceRoutes.js', error);
  module.exports = (req, res) => res.status(500).json({ errors: ['Salesforce routes failed to load'] });
}
