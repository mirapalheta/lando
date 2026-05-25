import { test, beforeEach, mock } from "node:test";
import assert from "node:assert/strict";
import { SecretsManagerClient } from "@aws-sdk/client-secrets-manager";

import { getHmacSecret, _resetSecretCache } from "../src/secrets.ts";

/**
 * `getHmacSecret` caches the fetched secret in module scope for 5 minutes so
 * warm Lambda invocations don't hit Secrets Manager on every directive. These
 * tests pin the cache-hit / cache-miss / cache-reset semantics and the
 * "binary-secret unsupported" error branch.
 *
 * The SDK client is module-scoped, so we mock `SecretsManagerClient.prototype.send`
 * directly rather than re-architecting the source to inject a client.
 */

beforeEach(() => {
  _resetSecretCache();
  mock.reset();
});

void test("returns the SecretString as both value and UTF-8 Buffer", async (t) => {
  const sendMock = t.mock.method(SecretsManagerClient.prototype, "send", () => ({
    SecretString: "the-secret",
  }));

  const result = await getHmacSecret("arn:aws:secretsmanager:us-east-1:0:secret:abc");

  assert.equal(result.value, "the-secret");
  assert.ok(Buffer.isBuffer(result.key));
  assert.equal(result.key.toString("utf8"), "the-secret");
  assert.equal(sendMock.mock.callCount(), 1);
});

void test("caches the result for warm invocations within the TTL", async (t) => {
  const sendMock = t.mock.method(SecretsManagerClient.prototype, "send", () => ({
    SecretString: "the-secret",
  }));

  const a = await getHmacSecret("arn:1");
  const b = await getHmacSecret("arn:1");

  // Same identity ⇒ same object returned from cache (not re-fetched).
  assert.equal(a, b);
  assert.equal(sendMock.mock.callCount(), 1);
});

void test("_resetSecretCache forces a re-fetch on next call", async (t) => {
  const sendMock = t.mock.method(SecretsManagerClient.prototype, "send", () => ({
    SecretString: "the-secret",
  }));

  await getHmacSecret("arn:1");
  _resetSecretCache();
  await getHmacSecret("arn:1");

  assert.equal(sendMock.mock.callCount(), 2);
});

void test("throws when SecretString is missing (binary secrets unsupported)", async (t) => {
  t.mock.method(SecretsManagerClient.prototype, "send", () => ({
    SecretString: undefined,
  }));

  await assert.rejects(() => getHmacSecret("arn:binary"), /returned no SecretString/);
});
