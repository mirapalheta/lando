#!/usr/bin/env bash
# Bind a custom hostname to a Container App with a free managed certificate.
# Called by terraform_data.custom_domain_bind (azure-custom-domain.tf).
#
# Env: CUSTOM_HOSTNAME, CONTAINER_APP, RESOURCE_GROUP, ENVIRONMENT
#
# Two steps: `hostname add` puts the hostname on the app unbound, then
# `hostname bind` issues the managed certificate and binds it. Azure refuses the
# certificate for a hostname that is not on an app yet.
#
# Idempotent: exits 0 at once when the hostname is already bound (SniEnabled).
# `az containerapp hostname bind` reuses an existing managed certificate for the
# hostname in the environment, so a re-run after a partial failure is safe.
# Retries cover DNS that Azure cannot see yet; certificate issuance itself can
# take several minutes inside a single bind call.
set -euo pipefail

: "${CUSTOM_HOSTNAME:?}" "${CONTAINER_APP:?}" "${RESOURCE_GROUP:?}" "${ENVIRONMENT:?}"

ATTEMPTS="${BIND_ATTEMPTS:-6}"
WAIT_SECONDS="${BIND_WAIT_SECONDS:-30}"

binding() {
  az containerapp hostname list \
    --name "$CONTAINER_APP" --resource-group "$RESOURCE_GROUP" \
    --query "[?name=='$CUSTOM_HOSTNAME'].bindingType | [0]" --output tsv 2>/dev/null || true
}

current="$(binding)"
if [ "$current" = "SniEnabled" ]; then
  echo "bind_hostname: $CUSTOM_HOSTNAME already bound to $CONTAINER_APP (SniEnabled); nothing to do"
  exit 0
fi

i=1
while [ "$i" -le "$ATTEMPTS" ]; do
  echo "bind_hostname: attempt $i/$ATTEMPTS: binding $CUSTOM_HOSTNAME to $CONTAINER_APP"
  # A managed certificate is only issued for a hostname already on an app in
  # the environment (RequireCustomHostnameInEnvironment), so add it unbound
  # first. `binding` is empty when the hostname is not on the app at all. The
  # add checks the asuid TXT record, which Terraform has only just written, so
  # it sits inside the retry loop.
  if [ -z "$(binding)" ] && ! az containerapp hostname add \
      --hostname "$CUSTOM_HOSTNAME" \
      --name "$CONTAINER_APP" \
      --resource-group "$RESOURCE_GROUP" \
      --output none; then
    echo "bind_hostname: add failed; DNS may not be visible to Azure yet"
  elif az containerapp hostname bind \
      --hostname "$CUSTOM_HOSTNAME" \
      --name "$CONTAINER_APP" \
      --resource-group "$RESOURCE_GROUP" \
      --environment "$ENVIRONMENT" \
      --validation-method CNAME \
      --output none; then
    current="$(binding)"
    if [ "$current" = "SniEnabled" ]; then
      echo "bind_hostname: $CUSTOM_HOSTNAME bound (SniEnabled)"
      exit 0
    fi
    echo "bind_hostname: bind returned but binding is '${current:-none}'"
  fi
  if [ "$i" -lt "$ATTEMPTS" ]; then
    sleep "$WAIT_SECONDS"
  fi
  i=$((i + 1))
done

echo "bind_hostname: $CUSTOM_HOSTNAME not bound after $ATTEMPTS attempts" >&2
exit 1
