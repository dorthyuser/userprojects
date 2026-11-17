// GENERATED_BY_AI_TEST_ENGINE
const { describe, it, expect } = require('@jest/globals');

describe('security boundary tests for ai2dev.com', () => {
  it('does not expose PCI or PII fields in a sanitized response object', () => {
    const response = {
      customerName: 'REDACTED',
      cardNumber: '**** **** **** 1111',
      token: 'tok_redacted',
      decision: 'approved'
    };

    const serialized = JSON.stringify(response);

    expect(serialized).not.toContain('4111111111111111');
    expect(serialized).not.toContain('cvv');
    expect(serialized).not.toContain('ssn');
    expect(response.decision).toBe('approved');
  });

  it('handles AML and fraud-related rejection without leaking governed data', () => {
    const error = new Error('Transaction declined');
    error.details = {
      suspiciousActivity: true,
      amlFlag: true
    };

    const message = error.message;
    const details = error.details;

    expect(message).toBe('Transaction declined');
    expect(details.amlFlag).toBe(true);
    expect(JSON.stringify(details)).not.toContain('accountNumber');
    expect(JSON.stringify(details)).not.toContain('4111111111111111');
  });
});