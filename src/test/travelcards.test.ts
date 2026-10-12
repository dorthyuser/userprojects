import { describe, expect, it } from 'vitest';
import { createTravelcardSchema } from '../validation.js';

describe('travelcard validation', () => {
  it('accepts a valid payload', () => {
    const result = createTravelcardSchema.safeParse({
      travelcardType: 'Young',
      travelcardValidFrom: '2026-01-01T00:00:00.000Z',
      travelcardValidTo: '2026-12-31T00:00:00.000Z',
      travelcardName: 'Test',
      travelcardNumber: '1234567890',
      travelcardRequestedDate: '2025-01-01T00:00:00.000Z',
      travelcardTransactionReference: 'ABCDEFGHIJKLMNO',
      travelcardUsableTo: null,
      cardholders: [
        {
          cardholderTitle: 'Mr',
          cardholderForename: 'John',
          cardholderSurname: 'Doe',
          cardholderType: 'Primary',
          cardholderPhotoName: 'photo.jpg',
          cardholderPhotoURL: 'https://example.com/photo.jpg'
        }
      ]
    });
    expect(result.success).toBe(true);
  });
});