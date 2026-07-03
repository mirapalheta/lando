import type { Context } from "aws-lambda";
import { getHmacSecret } from "./secrets.ts";
import { signRequest } from "./hmac.ts";

/**
 * Reads a required environment variable. Failures are deferred to the first
 * invocation rather than module-load: a misconfigured Lambda still gets the
 * runtime through enough to log a clear "Missing required env var" message
 * to CloudWatch, instead of dying at module load with a confusing stack.
 */
function requireEnv(name: string): string {
  const value = process.env[name];
  if (!value) {
    throw new Error(`Missing required env var: ${name}`);
  }
  return value;
}

/**
 * Smart Home directives carry a top-level `directive`; Custom Skill requests
 * carry a top-level `request.type` (LaunchRequest / IntentRequest /
 * SessionEndedRequest). The same Lambda signs + forwards both — only the
 * destination route differs.
 */
function isCustomSkillPayload(event: unknown): boolean {
  const envelope = event as { directive?: unknown; request?: { type?: string } } | null;
  return envelope?.request?.type !== undefined && envelope.directive === undefined;
}

export const handler = async (event: unknown, context: Context): Promise<unknown> => {
  // Resolve config inside the handler — see requireEnv comment above.
  // AZURE_ENDPOINT is the base Alexa route (e.g. https://<host>/api/alexa); we
  // append the per-skill segment so a single env var serves both paths:
  // `/smart-home` for directives, `/custom-skill` for intents.
  const azureEndpoint = `${requireEnv("AZURE_ENDPOINT")}/${isCustomSkillPayload(event) ? "custom-skill" : "smart-home"}`;
  const hmacSecretArn = requireEnv("HMAC_SECRET_ARN");
  const forwardTimeoutMs = parseInt(process.env.FORWARD_TIMEOUT_MS ?? "8000", 10);

  const body = JSON.stringify(event);

  const secret = await getHmacSecret(hmacSecretArn);
  const { timestamp, signature } = signRequest(secret.key, body);

  const controller = new AbortController();
  const timeoutId = setTimeout(() => {
    controller.abort();
  }, forwardTimeoutMs);

  try {
    const response = await fetch(azureEndpoint, {
      method: "POST",
      signal: controller.signal,
      headers: {
        "Content-Type": "application/json",
        "X-Aws-Request-Id": context.awsRequestId,
        "X-Lando-Timestamp": timestamp,
        "X-Lando-Signature": signature,
      },
      body,
    });

    const text = await response.text();

    if (!response.ok) {
      console.error(
        JSON.stringify({
          level: "error",
          msg: "Azure returned non-2xx",
          status: response.status,
          bodyPreview: text.slice(0, 500),
          requestId: context.awsRequestId,
        }),
      );
      throw new Error(`Azure responded ${String(response.status)}`);
    }

    try {
      return JSON.parse(text);
    } catch {
      throw new Error(`Failed to parse Azure response: ${text.slice(0, 500)}`);
    }
  } catch (err) {
    if (err instanceof Error && err.name === "AbortError") {
      console.error(
        JSON.stringify({
          level: "error",
          msg: "Azure request timed out",
          timeoutMs: forwardTimeoutMs,
          requestId: context.awsRequestId,
        }),
      );
      throw new Error(`Azure request timed out after ${String(forwardTimeoutMs)}ms`);
    }
    throw err;
  } finally {
    clearTimeout(timeoutId);
  }
};
