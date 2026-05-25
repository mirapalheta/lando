# ========================================
# AWS Secrets Manager — HMAC shared secret
# ========================================
# The Lambda reads this at cold start (with in-process caching). Same value
# is mirrored to Azure Key Vault in azure-keyvault.tf so the Container App
# verifier can check signatures against the same key.
#
# Value comes from random_password.hmac_shared_secret in shared-hmac.tf.

resource "aws_secretsmanager_secret" "hmac_shared_secret" {
  name        = local.names.secretsmanager_hmac
  description = "HMAC-SHA256 shared secret. Read by ${local.names.lambda_alexa_smart_home} Lambda, mirrored to Azure Key Vault for the Container App verifier."

  recovery_window_in_days = 7
  tags                    = var.tags
}

resource "aws_secretsmanager_secret_version" "hmac_shared_secret" {
  secret_id     = aws_secretsmanager_secret.hmac_shared_secret.id
  secret_string = random_password.hmac_shared_secret.result

  lifecycle {
    # When the HMAC secret rotates, the AWS provider incorrectly plans this as
    # an in-place update but attempts a DeleteThenCreate at apply time, causing
    # an "inconsistent final plan" error. replace_triggered_by forces the plan
    # to show DeleteThenCreate from the start, keeping plan and apply consistent.
    replace_triggered_by = [random_password.hmac_shared_secret]
  }
}
