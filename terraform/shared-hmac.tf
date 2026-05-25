# ========================================
# HMAC shared secret — cross-cloud bridge
# ========================================
# 48-byte random string. Generated once, replicated to both:
#   - AWS Secrets Manager  → read by the Lambda Proxy (see aws-secrets.tf)
#   - Azure Key Vault      → read by the Container App verifier      (see azure-keyvault.tf)
#
# Rotate by re-creating the random_password:
#   terraform apply -replace=random_password.hmac_shared_secret
#
# Heads-up: this value lives in terraform.tfstate. With local state that's
# acceptable for a homelab; if state ever moves to remote storage, ensure
# encryption-at-rest is enabled.
# ========================================

resource "random_password" "hmac_shared_secret" {
  length  = 48
  special = false # base64-friendly charset only; no shell-escaping headaches
}
