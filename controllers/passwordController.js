const crypto = require('crypto');

const AMBIGUOUS_CHARACTERS = new Set(['i', 'l', '1', 'o', '0', 'O', 'I', 'L']);
const DEFAULT_SPECIAL_CHARACTERS = ['!', '@', '#', '$', '%', '^', '&', '*', '-', '_', '=', '+', '?'];

function secureRandomInt(max) {
  return crypto.randomInt(0, max);
}

function shuffle(array) {
  for (let i = array.length - 1; i > 0; i -= 1) {
    const j = secureRandomInt(i + 1);
    [array[i], array[j]] = [array[j], array[i]];
  }
  return array;
}

function pickRandom(chars) {
  return chars[secureRandomInt(chars.length)];
}

function buildPool(policy) {
  const pools = [];
  if (policy.includeUppercase) pools.push('ABCDEFGHIJKLMNOPQRSTUVWXYZ'.split(''));
  if (policy.includeLowercase) pools.push('abcdefghijklmnopqrstuvwxyz'.split(''));
  if (policy.includeNumbers) pools.push('0123456789'.split(''));
  if (policy.includeSpecialCharacters) {
    const specials = Array.isArray(policy.allowedSpecialCharacters) && policy.allowedSpecialCharacters.length > 0
      ? policy.allowedSpecialCharacters
      : DEFAULT_SPECIAL_CHARACTERS;
    pools.push(specials);
  }
  return pools;
}

function filterAmbiguous(chars) {
  return chars.filter((ch) => !AMBIGUOUS_CHARACTERS.has(ch));
}

function validateInput(body) {
  const errors = [];
  const length = Number(body.length);
  if (!Number.isInteger(length) || length < 8 || length > 128) errors.push('length must be an integer between 8 and 128');
  if (![true, false].includes(body.includeUppercase)) errors.push('includeUppercase must be boolean');
  if (![true, false].includes(body.includeLowercase)) errors.push('includeLowercase must be boolean');
  if (![true, false].includes(body.includeNumbers)) errors.push('includeNumbers must be boolean');
  if (![true, false].includes(body.includeSpecialCharacters)) errors.push('includeSpecialCharacters must be boolean');
  if (![true, false].includes(body.excludeAmbiguousCharacters)) errors.push('excludeAmbiguousCharacters must be boolean');
  if (body.allowedSpecialCharacters !== undefined && !Array.isArray(body.allowedSpecialCharacters)) errors.push('allowedSpecialCharacters must be an array of strings');
  return { errors, length };
}

exports.generatePassword = async (req, res) => {
  try {
    const { errors, length } = validateInput(req.body || {});
    if (errors.length) {
      return res.status(400).json({
        success: false,
        error: {
          code: 'VALIDATION_ERROR',
          message: 'Invalid request payload',
          details: errors,
          timestamp: new Date().toISOString()
        }
      });
    }

    const policy = {
      includeUppercase: req.body.includeUppercase,
      includeLowercase: req.body.includeLowercase,
      includeNumbers: req.body.includeNumbers,
      includeSpecialCharacters: req.body.includeSpecialCharacters,
      excludeAmbiguousCharacters: req.body.excludeAmbiguousCharacters,
      allowedSpecialCharacters: req.body.allowedSpecialCharacters
    };

    const pools = buildPool(policy);
    if (!pools.length) {
      return res.status(400).json({
        success: false,
        error: {
          code: 'POLICY_ERROR',
          message: 'At least one character class must be enabled',
          timestamp: new Date().toISOString()
        }
      });
    }

    let allChars = [...new Set(pools.flat())];
    if (policy.excludeAmbiguousCharacters) allChars = filterAmbiguous(allChars);
    if (!allChars.length) {
      return res.status(400).json({
        success: false,
        error: {
          code: 'POLICY_ERROR',
          message: 'No characters available after applying policy restrictions',
          timestamp: new Date().toISOString()
        }
      });
    }

    const passwordChars = [];
    for (const pool of pools) {
      const filteredPool = policy.excludeAmbiguousCharacters ? filterAmbiguous(pool) : pool;
      if (!filteredPool.length) continue;
      passwordChars.push(pickRandom(filteredPool));
    }

    while (passwordChars.length < length) {
      passwordChars.push(pickRandom(allChars));
    }

    const password = shuffle(passwordChars).join('');

    return res.status(200).json({
      password,
      length,
      policy: {
        includeUppercase: policy.includeUppercase,
        includeLowercase: policy.includeLowercase,
        includeNumbers: policy.includeNumbers,
        includeSpecialCharacters: policy.includeSpecialCharacters,
        excludeAmbiguousCharacters: policy.excludeAmbiguousCharacters
      },
      generatedAt: new Date().toISOString()
    });
  } catch (error) {
    console.error(' Failed', { message: error.message, code: error.code || 'PASSWORD_GENERATION_ERROR' });
    return res.status(500).json({
      success: false,
      error: {
        code: 'PASSWORD_GENERATION_ERROR',
        message: 'Unable to generate password',
        timestamp: new Date().toISOString()
      }
    });
  }
};
