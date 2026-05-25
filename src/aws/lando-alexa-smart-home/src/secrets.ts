import { SecretsManagerClient, GetSecretValueCommand } from "@aws-sdk/client-secrets-manager";

const client = new SecretsManagerClient({});

/**
 * Cache the secret in module scope so warm Lambda invocations don't hit
 * Secrets Manager on every request. The cache lives for the lifetime of the
 * execution environment, which AWS recycles periodically — that gives us
 * "rotate by waiting" semantics without explicit invalidation logic.
 *
 * We cache the **Buffer** alongside the string. `createHmac` accepts either,
 * but when given a string it re-encodes UTF-8 every call. Pre-encoding once
 * here moves that work to cache-fill time. The string remains exposed for any
 * caller that prefers it (e.g. logging the secret's hash for ops).
 */
let cachedSecret: { value: string; key: Buffer } | undefined;
let cachedAt = 0;
const CACHE_TTL_MS = 5 * 60 * 1000; // 5 minutes

export interface CachedSecret {
  /** The raw secret string, as fetched from Secrets Manager. */
  value: string;
  /** UTF-8 bytes of {@link value}, suitable for passing directly to `createHmac`. */
  key: Buffer;
}

/**
 * Fetches the HMAC shared secret from AWS Secrets Manager, returning both the
 * string and a pre-encoded UTF-8 `Buffer` ready to feed into `createHmac`.
 * Results are cached in module scope for {@link CACHE_TTL_MS} ms.
 */
export async function getHmacSecret(secretArn: string): Promise<CachedSecret> {
  const now = Date.now();
  if (cachedSecret !== undefined && now - cachedAt < CACHE_TTL_MS) {
    return cachedSecret;
  }

  const response = await client.send(new GetSecretValueCommand({ SecretId: secretArn }));

  if (!response.SecretString) {
    throw new Error(
      `Secret ${secretArn} returned no SecretString (binary secrets are not supported)`,
    );
  }

  cachedSecret = {
    value: response.SecretString,
    key: Buffer.from(response.SecretString, "utf8"),
  };
  cachedAt = now;
  return cachedSecret;
}

/**
 * Test/internal hook to force a re-fetch — exposed only for tests; not for
 * production code paths.
 */
export function _resetSecretCache(): void {
  cachedSecret = undefined;
  cachedAt = 0;
}
