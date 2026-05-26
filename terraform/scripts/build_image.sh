#!/bin/bash
# ---------------------------------------------------------------------------
# build_image.sh — invoked by null_resource.acr_image_build in
# azure-app-build.tf. Builds and pushes the Azure Function App container
# image locally via `docker build` (BuildKit) and pushes it to ACR.
#
# Inputs (passed via the local-exec `environment` block):
#   REGISTRY_NAME            ACR name (not FQDN), e.g. "acrlandocentralus".
#   IMAGE_NAME               Image / repository name (var.project_name),
#                            e.g. "lando".
#   IMAGE_TAG                Tag to publish (var.image_tag), e.g. "0.7.5".
#                            Also passed straight through as the VERSION
#                            build-arg.
#   DOCKER_FILE              Path to the Dockerfile to build.
#   SOURCE_PATH              Build context passed to `docker build`.
#   HOME_ASSISTANT_CAF       Optional path to a PEM CA bundle. If empty, a
#                            zero-byte tempfile is substituted so BuildKit's
#                            --secret mount still has a source; the Dockerfile
#                            then skips the install via `[ -s ... ]`.
#   HOME_ASSISTANT_CRT       Optional path to a PEM cert. Same empty-string
#                            handling as HOME_ASSISTANT_CAF.
# ---------------------------------------------------------------------------
set -euo pipefail

RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${YELLOW} Source code or image tag changes detected. Building and pushing image '${IMAGE_NAME}:${IMAGE_TAG}' to registry '${REGISTRY_NAME}'..."

# 1. Refuse to overwrite an existing tag.
#    Forces the developer to bump image_tag or manually delete the existing
#    tag instead of silently rewriting history in the registry. This script
#    only runs when terraform's triggers fire (code_hash or image_tag
#    changed), so reaching this point with an existing tag means the user
#    asked for a rebuild without bumping the tag.
TAG_EXISTS=$(az acr repository show-tags --name "$REGISTRY_NAME" --repository "$IMAGE_NAME" --query "contains(@, '$IMAGE_TAG')" --output tsv 2>/dev/null || echo "false")
if [ "$TAG_EXISTS" = "true" ]; then
        echo -e "${RED} |"
        echo -e "${RED} | ERROR: ${NC} Image tag ${YELLOW}'${IMAGE_TAG}'${NC} already exists in ACR."
        echo -e "${RED} |        ${NC} Bump var.image_tag or delete the existing tag to allow a rebuild."
        echo -e "${RED} |"
        exit 1
fi

# 2. Resolve dynamic build settings (COMMIT, BRANCH).
#    Done inline — the values are cheap to compute and the cert contents
#    are mounted via BuildKit --secret (file paths, not contents), so
#    there's nothing multi-line to serialize through env any more.

# Run git from inside SOURCE_PATH so that, when SOURCE_PATH lives inside a
# submodule (lando/ in this repo), we report the *submodule's* HEAD — which
# is what's actually getting built — and detect dirtiness in the submodule's
# working tree rather than the superrepo's.
COMMIT="$(git -C "$SOURCE_PATH" rev-parse --short=8 HEAD)"

# `--porcelain .` covers both staged and unstaged changes AND untracked
# files, scoped to SOURCE_PATH. Any non-empty output means dirty.
if [ -n "$(git -C "$SOURCE_PATH" status --porcelain .)" ]; then
        COMMIT="${COMMIT}-dirty"
fi

# `--abbrev-ref HEAD` returns "HEAD" when detached, which is a fine sentinel
# value for the build label.
BRANCH="$(git -C "$SOURCE_PATH" rev-parse --abbrev-ref HEAD)"

# Generates a dummy temp file if the cert variables are empty
[ -z "$HOME_ASSISTANT_CAF" ] && TEMP_CAF=$(mktemp) && HOME_ASSISTANT_CAF="$TEMP_CAF"
[ -z "$HOME_ASSISTANT_CRT" ] && TEMP_CRT=$(mktemp) && HOME_ASSISTANT_CRT="$TEMP_CRT"

# 3. Log in to ACR so the subsequent `docker push` is authenticated.
echo "Logging into Azure Container Registry..."
az acr login --name "$REGISTRY_NAME"

# 4. Build the image with build-args for COMMIT, BRANCH, VERSION, and the HA certs.
echo "Building image '${IMAGE_NAME}:${IMAGE_TAG}'..."
echo "  COMMIT=$COMMIT  BRANCH=$BRANCH  VERSION=$IMAGE_TAG"

docker build \
        -f "$DOCKER_FILE" \
        -t "$REGISTRY_NAME.azurecr.io/${IMAGE_NAME}:${IMAGE_TAG}" \
        --platform linux/amd64 \
        --build-arg "COMMIT=$COMMIT" \
        --build-arg "BRANCH=$BRANCH" \
        --build-arg "VERSION=$IMAGE_TAG" \
        --secret "id=ha_caf,source=$HOME_ASSISTANT_CAF" \
        --secret "id=ha_crt,source=$HOME_ASSISTANT_CRT" \
        "$SOURCE_PATH"

# 5. Push the image to ACR.
echo "Pushing image to '$REGISTRY_NAME.azurecr.io/${IMAGE_NAME}:${IMAGE_TAG}'..."
docker push "$REGISTRY_NAME.azurecr.io/${IMAGE_NAME}:${IMAGE_TAG}"
