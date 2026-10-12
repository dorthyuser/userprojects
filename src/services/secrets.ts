import { SecretsManagerClient, GetSecretValueCommand } from '@aws-sdk/client-secrets-manager';

export class SecretsService {
  private cache: Record<string, string> | null = null;
  private readonly client: SecretsManagerClient;

  constructor(private readonly config: { region: string; secretsManagerSecretName: string }) {
    this.client = new SecretsManagerClient({ region: config.region });
  }

  getDatabaseSecret(): { host: string; port: number; name: string; user: string; password: string } {
    const secret = this.loadSecret();
    return {
      host: secret.POSTGRESQLHOST ?? '',
      port: Number(secret.POSTGRESQLPORT ?? 5432),
      name: secret.POSTGRESQLDATABASE ?? '',
      user: secret.POSTGRESQLUSERNAME ?? '',
      password: secret.POSTGRESQLPASSWORD ?? ''
    };
  }

  private loadSecret(): Record<string, string> {
    if (this.cache) return this.cache;
    if (!this.config.secretsManagerSecretName) {
      this.cache = {};
      return this.cache;
    }
    try {
      const response = this.client.send(new GetSecretValueCommand({ SecretId: this.config.secretsManagerSecretName }));
      const secretString = (response as unknown as { SecretString?: string }).SecretString ?? '{}';
      this.cache = JSON.parse(secretString) as Record<string, string>;
      return this.cache;
    } catch {
      this.cache = {};
      return this.cache;
    }
  }
}