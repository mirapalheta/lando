#!/bin/bash
# ---------------------------------------------------------------------------
# build_lambda.sh — invoked by null_resource.lambda_build in
# aws-lambda-build.tf when local.alexa_smart_home_code_hash changes. Builds
# the Alexa Smart Home Lambda bundle into dist/index.mjs (esbuild target
# defined in package.json) and packages it as $ZIP_PATH for upload by
# aws_lambda_function.alexa_smart_home.
#
# The terraform trigger IS the freshness check: if we're running, sources
# differ from what produced the last artifacts (or we're on a fresh runner
# with no prior artifacts at all), so we always rebuild and rezip — no
# in-script "is dist up to date?" branch needed.
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
