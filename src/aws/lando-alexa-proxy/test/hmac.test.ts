import { test } from "node:test";
import assert from "node:assert/strict";
import { createHmac } from "node:crypto";
import { signRequest, SIGNATURE_VERSION } from "../src/hmac.ts";

/**
 * `signRequest` is the entire HMAC scheme we share with the Azure verifier —
 * any drift here silently breaks every Alexa directive. These tests pin the
 * shape ("v1=" prefix, lowercase hex, current Unix seconds) and the canonical
 * form (`${timestamp}.${body}`) the verifier expects.
 */

const SECRET = Buffer.from("shared-secret", "utf8");
const BODY = '{"directive":{"header":{"name":"TurnOn"}}}';

void test("signRequest emits 'v1=<hex>' signature shape", () => {
  const { signature } = signRequest(SECRET, BODY);
  assert.match(signature, /^v1=[0-9a-f]{64}$/);
  assert.equal(signature.split("=")[0], SIGNATURE_VERSION);
});

void test("signRequest signs over `${timestamp}.${body}` exactly", () => {
  const { timestamp, signature } = signRequest(SECRET, BODY);
  const expected = createHmac("sha256", SECRET).update(`${timestamp}.${BODY}`).digest("hex");
  assert.equal(signature, `v1=${expected}`);
});

void test("signRequest timestamp is the current Unix seconds (±2s)", () => {
  const before = Math.floor(Date.now() / 1000);
  const { timestamp } = signRequest(SECRET, BODY);
  const after = Math.floor(Date.now() / 1000);
  const value = parseInt(timestamp, 10);
  assert.ok(
    value >= before && value <= after,
    `${String(value)} not in [${String(before)}, ${String(after)}]`,
  );
});

void test("identical body + key produce identical signatures at identical timestamps", () => {
  // Sub the clock by hand so the two signatures share a timestamp.
  const now = Date.now();
  const originalNow = Date.now;
  Date.now = () => now;
  try {
    const a = signRequest(SECRET, BODY);
    const b = signRequest(SECRET, BODY);
    assert.equal(a.signature, b.signature);
    assert.equal(a.timestamp, b.timestamp);
  } finally {
    Date.now = originalNow;
  }
});

void test("different bodies produce different signatures", () => {
  const a = signRequest(SECRET, BODY);
  const b = signRequest(SECRET, BODY + "x");
  assert.notEqual(a.signature, b.signature);
});

void test("different secrets produce different signatures", () => {
  const now = Date.now();
  const originalNow = Date.now;
  Date.now = () => now;
  try {
    const a = signRequest(SECRET, BODY);
    const b = signRequest(Buffer.from("other-secret", "utf8"), BODY);
    assert.notEqual(a.signature, b.signature);
  } finally {
    Date.now = originalNow;
  }
});
