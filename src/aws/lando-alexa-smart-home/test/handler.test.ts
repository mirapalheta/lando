import { test, beforeEach, afterEach, mock, type TestContext } from "node:test";
import assert from "node:assert/strict";
import type { Context } from "aws-lambda";
import { SecretsManagerClient } from "@aws-sdk/client-secrets-manager";

import { handler } from "../src/handler.ts";
import { _resetSecretCache } from "../src/secrets.ts";

/**
 * `handler.ts` is the Lambda entry point. The proxy responsibilities are
 * narrow: read config from env, fetch the HMAC secret, sign the request,
 * forward to Azure, return the parsed JSON or throw a legible error. These
 * tests pin every branch — including the timeout path, which Alexa's 8-second
 * response budget makes a real production concern.
 *
 * We don't go through the AWS network: `SecretsManagerClient.prototype.send`
 * is mocked, and `globalThis.fetch` is stubbed per test.
 */

const CONTEXT = { awsRequestId: "req-123" } as unknown as Context;
const AZURE_ENDPOINT = "https://func.example/api/alexa";

let originalFetch: typeof globalThis.fetch;
let originalEnv: NodeJS.ProcessEnv;

beforeEach(() => {
  originalFetch = globalThis.fetch;
  originalEnv = { ...process.env };
  process.env.AZURE_ENDPOINT = AZURE_ENDPOINT;
  process.env.HMAC_SECRET_ARN = "arn:secret:hmac";
  delete process.env.FORWARD_TIMEOUT_MS;
  _resetSecretCache();
  mock.reset();
});

afterEach(() => {
  globalThis.fetch = originalFetch;
  process.env = originalEnv;
});

function mockSecret(t: TestContext, secret = "the-secret"): void {
  t.mock.method(SecretsManagerClient.prototype, "send", () => ({
    SecretString: secret,
  }));
}

void test("forwards the event to Azure with HMAC headers and returns the parsed body", async (t) => {
  mockSecret(t);
  const calls: { url: string | URL | Request; init?: RequestInit }[] = [];
  globalThis.fetch = mock.fn((url, init): Promise<Response> => {
    calls.push({ url, init });
    return Promise.resolve(
      new Response(JSON.stringify({ event: { header: { name: "Response" } } }), {
        status: 200,
        headers: { "content-type": "application/json" },
      }),
    );
  });

  const result = await handler({ directive: { header: { name: "TurnOn" } } }, CONTEXT);

  // Returned the parsed Azure response.
  assert.deepEqual(result, { event: { header: { name: "Response" } } });

  // One fetch, to the configured Azure endpoint, with the documented headers.
  assert.equal(calls.length, 1);
  assert.equal(calls[0].url, AZURE_ENDPOINT);
  const { init } = calls[0];
  assert.ok(init);
  const headers = new Headers(init.headers);
  assert.equal(headers.get("Content-Type"), "application/json");
  assert.equal(headers.get("X-Aws-Request-Id"), "req-123");
  assert.match(headers.get("X-Lando-Timestamp") ?? "", /^\d+$/);
  assert.match(headers.get("X-Lando-Signature") ?? "", /^v1=[0-9a-f]{64}$/);
});

void test("throws a legible error when AZURE_ENDPOINT is missing", async () => {
  delete process.env.AZURE_ENDPOINT;
  await assert.rejects(() => handler({}, CONTEXT), /Missing required env var: AZURE_ENDPOINT/);
});

void test("throws a legible error when HMAC_SECRET_ARN is missing", async () => {
  delete process.env.HMAC_SECRET_ARN;
  await assert.rejects(() => handler({}, CONTEXT), /Missing required env var: HMAC_SECRET_ARN/);
});

void test("throws when Azure returns a non-2xx status", async (t) => {
  mockSecret(t);
  globalThis.fetch = mock.fn(() => Promise.resolve(new Response("nope", { status: 502 })));

  await assert.rejects(() => handler({}, CONTEXT), /Azure responded 502/);
});

void test("throws when Azure returns a 2xx with a non-JSON body", async (t) => {
  mockSecret(t);
  globalThis.fetch = mock.fn(() => Promise.resolve(new Response("not json", { status: 200 })));

  await assert.rejects(() => handler({}, CONTEXT), /Failed to parse Azure response/);
});

void test("aborts after FORWARD_TIMEOUT_MS and surfaces a timeout error", async (t) => {
  mockSecret(t);
  process.env.FORWARD_TIMEOUT_MS = "50";
  globalThis.fetch = mock.fn((_, init) => {
    const signal = init?.signal;
    return new Promise((_, reject) => {
      signal?.addEventListener("abort", () => {
        const error = new Error("aborted");
        error.name = "AbortError";
        reject(error);
      });
    });
  });

  await assert.rejects(() => handler({}, CONTEXT), /Azure request timed out after 50ms/);
});
