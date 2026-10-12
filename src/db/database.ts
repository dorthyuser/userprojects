import { Pool } from 'pg';
import type { AppConfig } from '../config.js';
import { SecretsService } from '../services/secrets.js';

export class Database {
  private readonly pool: Pool;

  constructor(config: AppConfig['database'], secrets: SecretsService, logger: { info: Function; warn: Function; error: Function }) {
    const secret = secrets.getDatabaseSecret();
    const connection = {
      host: secret.host || config.host,
      port: secret.port || config.port,
      database: secret.name || config.name,
      user: secret.user || config.user,
      password: secret.password || config.password,
      ssl: config.ssl ? { rejectUnauthorized: false } : undefined
    };
    this.pool = new Pool(connection);
    logger.info({ dbHost: connection.host, dbPort: connection.port, dbName: connection.database }, 'database_initialized');
  }

  async close(): Promise<void> {
    await this.pool.end();
  }

  async createTravelcard(input: {
    travelcardType: string;
    travelcardValidFrom: string;
    travelcardValidTo: string;
    travelcardName?: string | null;
    travelcardNumber: string;
    travelcardRequestedDate: string;
    travelcardTransactionReference: string;
    travelcardUsableTo?: string | null;
    cardholders: Array<{
      cardholderTitle: string;
      cardholderForename: string;
      cardholderSurname: string;
      cardholderType: string;
      cardholderPhotoName: string;
      cardholderPhotoRRSKey?: string | null;
      cardholderPhotoURL?: string | null;
      cardholderPhotoKey?: string | null;
    }>;
  }): Promise<{ travelcardId: string; token: string }> {
    const client = await this.pool.connect();
    try {
      await client.query('BEGIN');
      const travelcardResult = await client.query<{ id: number }>(
        'INSERT INTO public.travelcards (travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to) VALUES ($1,$2,$3,$4,$5,$6,$7,$8) RETURNING id',
        [input.travelcardType, input.travelcardValidFrom, input.travelcardValidTo, input.travelcardName ?? null, input.travelcardNumber, input.travelcardRequestedDate, input.travelcardTransactionReference, input.travelcardUsableTo ?? null]
      );
      const travelcardId = String(travelcardResult.rows[0]?.id ?? '');
      for (const ch of input.cardholders) {
        await client.query(
          'INSERT INTO public.cardholders (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key) VALUES ($1,$2,$3,$4,$5,$6,$7,$8,$9)',
          [travelcardId, ch.cardholderTitle, ch.cardholderForename, ch.cardholderSurname, ch.cardholderType, ch.cardholderPhotoName, ch.cardholderPhotoRRSKey ?? null, ch.cardholderPhotoURL ?? null, ch.cardholderPhotoKey ?? null]
        );
      }
      await client.query('COMMIT');
      return { travelcardId, token: cryptoRandomToken() };
    } catch (error) {
      await client.query('ROLLBACK');
      throw error;
    } finally {
      client.release();
    }
  }
}

function cryptoRandomToken(): string {
  return Array.from(crypto.getRandomValues(new Uint8Array(3))).map((n) => n.toString(16).padStart(2, '0')).join('').toUpperCase();
}