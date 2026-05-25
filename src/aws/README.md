# lando-alexa-smart-home

[![Lambda coverage](https://codecov.io/gh/mirapalheta/lando/graph/badge.svg?flag=lambda)](https://app.codecov.io/gh/mirapalheta/lando?flags%5B0%5D=lambda)

AWS Lambda function that forwards Alexa Smart Home directives to the Azure-hosted Lando bridge, signing every request with HMAC-SHA256 so the Azure side can verify authenticity.

## What it does

```
Alexa Smart Home  →  Lambda (this)  →  HTTPS + HMAC  →  Azure Container App (Lando)  →  Home Assistant
```

1. Reads the Alexa directive from the Lambda `event` argument.
2. Fetches the shared HMAC secret from AWS Secrets Manager (cached for the lifetime of the execution environment).
3. Signs `${timestamp}.${body}` with HMAC-SHA256 and posts the body to `AZURE_ENDPOINT` with `X-Lando-Timestamp` and `X-Lando-Signature: v1=<hex>` headers.
4. Returns Azure's JSON response to Alexa unchanged.

## Configuration (env vars)

| Variable             | Required | Description                                                                                     |
| -------------------- | -------- | ----------------------------------------------------------------------------------------------- |
| `AZURE_ENDPOINT`     | yes      | Full URL of the Azure-side smart-home handler.                                                  |
| `HMAC_SECRET_ARN`    | yes      | ARN of the Secrets Manager secret holding the shared HMAC key.                                  |
| `FORWARD_TIMEOUT_MS` | no       | Outbound HTTP timeout in milliseconds. Defaults to 8000 to stay within Alexa's response budget. |

## Local development

```bash
npm install
npm run build       # → dist/index.mjs (esbuild bundle)
npm run package     # → lambda.zip (what Terraform deploys)
```

## Deployment

Managed by Terraform in `../../../terraform/aws.tf`. The function name, IAM role, log group, and Alexa permission are imported from the existing AWS resources — `terraform apply` updates code and config in-place.

## Signing scheme

See `src/hmac.ts`. The Azure-side verifier is `src/Lando.FunctionApp/Security/Hmac/HmacSignatureVerifier.cs` — both ends must use the same algorithm, version prefix, and timestamp format.
