const crypto = require('crypto');

const defaultPolicy = {
  minLength: 12,
  maxLength: 64,
  includeUppercase: true,
  includeLowercase: true,
  includeNumbers: true,
  includeSpecialCharacters: true
};

function buildCharset(policy) {
  let charset = '';
  const groups = [];
  if (policy.includeUppercase) groups.push('ABCDEFGHIJKLMNOPQRSTUVWXYZ');
  if (policy.includeLowercase) groups.push('abcdefghijklmnopqrstuvwxyz');
  if (policy.includeNumbers) groups.push('0123456789');
  if (policy.includeSpecialCharacters) groups.push('!@#$%^&*()-_=+[]{}|;:,.<>?/');
  charset = groups.join('');
  return { charset, groups };
}

function secureRandomInt(max) {
  return crypto.randomInt(0, max);
}

function generatePasswordFromPolicy(policy) {
  const { charset, groups } = buildCharset(policy);
  if (!charset) throw new Error('At least one character class must be enabled.');
  const length = Math.max(policy.minLength || 1, Math.min(policy.length || policy.minLength || 12, policy.maxLength || 64));
  const passwordChars = [];
  groups.forEach((group) => {
    passwordChars.push(group[secureRandomInt(group.length)]);
  });
  while (passwordChars.length < length) {
    passwordChars.push(charset[secureRandomInt(charset.length)]);
  }
  for (let i = passwordChars.length - 1; i > 0; i--) {
    const j = secureRandomInt(i + 1);
    [passwordChars[i], passwordChars[j]] = [passwordChars[j], passwordChars[i]];
  }
  return passwordChars.join('').slice(0, length);
}

function validatePasswordAgainstRules(password, rules) {
  const violations = [];
  if (typeof password !== 'string' || !password.length) violations.push('Password is required.');
  if (rules.minLength && password.length < rules.minLength) violations.push('Password does not meet minimum length.');
  if (rules.requireUppercase && !/[A-Z]/.test(password)) violations.push('Password must include an uppercase letter.');
  if (rules.requireLowercase && !/[a-z]/.test(password)) violations.push('Password must include a lowercase letter.');
  if (rules.requireNumbers && !/[0-9]/.test(password)) violations.push('Password must include a number.');
  if (rules.requireSpecialCharacters && !/[!@#$%^&*()\-_=+\[\]{}|;:,.<>?/]/.test(password)) violations.push('Password must include a special character.');
  return violations;
}

exports.getPasswordPolicy = async (req, res) => {
  try {
    console.log('mysql Connected');
    return res.status(200).json({ defaultPolicy });
  } catch (error) {
    console.error('mysql Failed', error && error.message ? error.message : error);
    return res.status(500).json({ error: { code: 'PASSWORD_POLICY_ERROR', message: 'Unable to fetch password policy.' } });
  }
};

exports.generatePassword = async (req, res) => {
  try {
    console.log('mysql Connected');
    const policy = {
      length: Number(req.body.length) || defaultPolicy.minLength,
      includeUppercase: req.body.includeUppercase !== false,
      includeLowercase: req.body.includeLowercase !== false,
      includeNumbers: req.body.includeNumbers !== false,
      includeSpecialCharacters: req.body.includeSpecialCharacters !== false,
      minLength: defaultPolicy.minLength,
      maxLength: defaultPolicy.maxLength
    };
    const password = generatePasswordFromPolicy(policy);
    return res.status(200).json({
      password,
      policy: {
        length: policy.length,
        includeUppercase: policy.includeUppercase,
        includeLowercase: policy.includeLowercase,
        includeNumbers: policy.includeNumbers,
        includeSpecialCharacters: policy.includeSpecialCharacters
      }
    });
  } catch (error) {
    console.error('mysql Failed', error && error.message ? error.message : error);
    return res.status(400).json({ error: { code: 'PASSWORD_GENERATION_ERROR', message: 'Unable to generate password.' } });
  }
};

exports.validatePassword = async (req, res) => {
  try {
    console.log('mysql Connected');
    const violations = validatePasswordAgainstRules(req.body.password, {
      minLength: Number(req.body.minLength) || 0,
      requireUppercase: !!req.body.requireUppercase,
      requireLowercase: !!req.body.requireLowercase,
      requireNumbers: !!req.body.requireNumbers,
      requireSpecialCharacters: !!req.body.requireSpecialCharacters
    });
    return res.status(200).json({ valid: violations.length === 0, violations });
  } catch (error) {
    console.error('mysql Failed', error && error.message ? error.message : error);
    return res.status(400).json({ error: { code: 'PASSWORD_VALIDATION_ERROR', message: 'Unable to validate password.' } });
  }
};
