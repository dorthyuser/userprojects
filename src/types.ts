export enum TravelcardType {
  Young = 'Young',
  TwoTogether = 'TwoTogether',
  Family = 'Family',
  Senior = 'Senior',
  Network = 'Network',
  TwentySixToThirty = 'TwentySixToThirty',
  SixteenToSeventeen = 'SixteenToSeventeen',
  Veterans = 'Veterans'
}

export enum CardholderType {
  Primary = 'Primary',
  Secondary = 'Secondary'
}

export type CardholderRequest = {
  cardholderTitle: string;
  cardholderForename: string;
  cardholderSurname: string;
  cardholderType: CardholderType;
  cardholderPhotoName: string;
  cardholderPhotoRRSKey?: string | null;
  cardholderPhotoURL?: string | null;
  cardholderPhotoKey?: string | null;
};

export type CreateTravelcardRequest = {
  travelcardType: TravelcardType;
  travelcardValidFrom: string;
  travelcardValidTo: string;
  travelcardName?: string | null;
  travelcardNumber: string;
  travelcardRequestedDate: string;
  travelcardTransactionReference: string;
  travelcardUsableTo?: string | null;
  cardholders: CardholderRequest[];
};

export type CreateTravelcardResponse = {
  travelcardId: string;
  token: string;
};