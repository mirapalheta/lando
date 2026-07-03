#!/bin/bash
# ---------------------------------------------------------------------------
# build_lambda.sh — invoked by null_resource.lambda_build in
# aws-lambda-build.tf on every apply. Builds the Alexa proxy Lambda bundle
# into dist/index.mjs (esbuild target defined in package.json) and packages it
# as $ZIP_PATH for upload by aws_lambda_function.alexa_proxy.
#
# The build runs unconditionally: the zip lives in the ephemeral .terraform/
# dir and never survives between runs (fresh CI runners, or a partially-failed
# apply that advanced state without keeping the artifact), so we always rebuild
# and rezip to guarantee the file exists. Change detection for the actual code
# upload is handled by aws_lambda_function.source_code_hash, which is derived
# from the source files — so rebuilding with unchanged source is a no-op upload.
#
# Inputs:
#   Working directory — npm package root (set by terraform's working_dir).
#   ZIP_PATH (env)    — absolute path where the final zip should be written.
# ---------------------------------------------------------------------------
set -euo pipefail

: "${ZIP_PATH:?ZIP_PATH must be set}"

YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${YELLOW}Building Lambda bundle...${NC}"
npm ci
npm run build

echo -e "${YELLOW}Packaging dist/ into $ZIP_PATH...${NC}"
mkdir -p "$(dirname "$ZIP_PATH")"
rm -f "$ZIP_PATH"

# zip contents of dist/ at the root of the archive so the AWS Lambda
# runtime can find index.handler at index.mjs (not dist/index.mjs).
(cd dist && zip -q -r "$ZIP_PATH" .)
