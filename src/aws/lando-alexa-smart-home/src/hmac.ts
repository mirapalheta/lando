import { createHmac } from "node:crypto";

/**
 * HMAC-SHA256 signing scheme shared with the Azure-side verifier.
 *
 * Signed string:  `${timestamp}.${body}`
 * Signature:      `v1=<hex(HMAC-SHA256)>`
 *
 * The `v1=` prefix is a version marker so the scheme can be rotated in
 * lockstep with the verifier without breaking either side mid-deploy. The
 * Azure verifier dispatches on this version to choose both the hash algorithm
 * and the canonical form, so any future `v2` here must land in both places.
 */
export const SIGNATURE_VERSION = "v1";

export interface SignedHeaders {
  timestamp: string;
  signature: string;
}

/**
 * Computes the headers (`X-Lando-Timestamp`, `X-Lando-Signature`) that
 * authenticate `body` to the Azure verifier.
 *
 * @param key  UTF-8 bytes of the shared secret. Accepting a `Buffer` (rather
 *             than a string) skips the per-call UTF-8 conversion that
 *             `createHmac` would otherwise do — see {@link getHmacSecret}.
 * @param body The exact JSON body bytes that will be sent on the wire.
 *             Must be byte-identical to what the verifier will see.
 */
export function signRequest(key: Buffer, body: string): SignedHeaders {
  const timestamp = Math.floor(Date.now() / 1000).toString();
  const payload = `${timestamp}.${body}`;
  const digest = createHmac("sha256", key).update(payload).digest("hex");
  return {
    timestamp,
    signature: `${SIGNATURE_VERSION}=${digest}`,
  };
}
