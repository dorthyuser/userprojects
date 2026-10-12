import { z } from 'zod';
import { CardholderType, TravelcardType } from './types.js';

const isoDateTime = z.string().datetime({ offset: true });

export const createTravelcardSchema = z.object({
  travelcardType: z.nativeEnum(TravelcardType),
  travelcardValidFrom: isoDateTime,
  travelcardValidTo: isoDateTime,
  travelcardName: z.string().min(1).max(255).nullable().optional(),
  travelcardNumber: z.string().min(1).max(64),
  travelcardRequestedDate: isoDateTime,
  travelcardTransactionReference: z.string().length(15),
  travelcardUsableTo: isoDateTime.nullable().optional(),
  cardholders: z.array(z.object({
    cardholderTitle: z.string().min(1).max(50),
    cardholderForename: z.string().min(1).max(100),
    cardholderSurname: z.string().min(1).max(100),
    cardholderType: z.nativeEnum(CardholderType),
    cardholderPhotoName: z.string().min(1).max(255),
    cardholderPhotoRRSKey: z.string().min(1).max(255).nullable().optional(),
    cardholderPhotoURL: z.string().url().max(2048).nullable().optional(),
    cardholderPhotoKey: z.string().min(1).max(255).nullable().optional()
  })).min(1).max(2)
}).superRefine((value, ctx) => {
  const primaryCount = value.cardholders.filter((c) => c.cardholderType === CardholderType.Primary).length;
  const secondaryCount = value.cardholders.filter((c) => c.cardholderType === CardholderType.Secondary).length;
  const now = new Date();
  const validFrom = new Date(value.travelcardValidFrom);
  const validTo = new Date(value.travelcardValidTo);
  const requestedDate = new Date(value.travelcardRequestedDate);
  const usableTo = value.travelcardUsableTo ? new Date(value.travelcardUsableTo) : null;
  const oneMonthAhead = new Date(now);
  oneMonthAhead.setMonth(oneMonthAhead.getMonth() + 1);

  if (primaryCount < 1) ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['cardholders'], message: 'Primary cardholder is required.' });
  if (secondaryCount > 1) ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['cardholders'], message: 'Only one secondary cardholder is allowed.' });
  if ((value.travelcardType === TravelcardType.SixteenToSeventeen || value.travelcardType === TravelcardType.Veterans) && secondaryCount > 0) ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['cardholders'], message: 'Secondary cardholder is not allowed for this travelcard type.' });
  if (value.travelcardType === TravelcardType.SixteenToSeventeen && !usableTo) ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['travelcardUsableTo'], message: 'Travelcard usable to is required for SixteenToSeventeen.' });
  if (usableTo && usableTo <= now) ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['travelcardUsableTo'], message: 'Travelcard usable to must be in the future.' });
  if (requestedDate >= now) ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['travelcardRequestedDate'], message: 'Requested date must be in the past.' });
  if (validFrom > oneMonthAhead) ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['travelcardValidFrom'], message: 'Valid from must be no later than one calendar month from today.' });
  if (validFrom > validTo) ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['travelcardValidFrom'], message: 'Valid from must be earlier than valid to.' });
  if (validTo <= now) ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['travelcardValidTo'], message: 'Valid to must be in the future.' });
  for (const ch of value.cardholders) {
    const photoCount = [ch.cardholderPhotoRRSKey, ch.cardholderPhotoURL, ch.cardholderPhotoKey].filter((v) => typeof v === 'string' && v.trim().length > 0).length;
    if (photoCount !== 1) ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['cardholders'], message: 'Exactly one cardholder photo detail must be provided.' });
  }
});