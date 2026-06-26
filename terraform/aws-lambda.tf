# ========================================
# AWS Lambda — Alexa Smart Home → Azure Lando proxy
# ========================================
# This file owns the full lifecycle of the Alexa-side Lambda:
#
#   Alexa Smart Home  →  AWS Lambda (this)  →  HTTPS+HMAC  →  Azure Container App
#
# Names are built from var.project_name in shared-locals.tf so swapping the
# project name renames everything in lockstep.
# ========================================

# ----------------------------------------
# IAM
# ----------------------------------------
resource "aws_iam_role" "alexa_smart_home" {
  name = local.names.iam_role_alexa_smart_home
  # AWS rejects characters outside [\t\n\r\x20-\x7E\xA1-\xFF] in description
  # fields, which means no em/en dashes, smart quotes, or other typographic
  # punctuation. Plain ASCII only here.
  description = "Execution role for the ${local.names.lambda_alexa_smart_home} function. Grants CloudWatch Logs write + scoped read on the HMAC secret."

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect    = "Allow"
      Principal = { Service = "lambda.amazonaws.com" }
      Action    = "sts:AssumeRole"
    }]
  })

  tags = local.tags
}

# CloudWatch Logs write permission (AWS-managed policy).
resource "aws_iam_role_policy_attachment" "alexa_smart_home_basic_execution" {
  role       = aws_iam_role.alexa_smart_home.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"
}

# Scoped read on the HMAC secret only.
resource "aws_iam_role_policy" "alexa_smart_home_secrets_read" {
  name = local.names.iam_policy_hmac_read
  role = aws_iam_role.alexa_smart_home.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect   = "Allow"
      Action   = ["secretsmanager:GetSecretValue"]
      Resource = aws_secretsmanager_secret.hmac_shared_secret.arn
    }]
  })
}

# ----------------------------------------
# CloudWatch log group
# ----------------------------------------
# AWS auto-creates this when the Lambda first runs, but managing it via Terraform
# lets us control retention. Name format is mandated by AWS as /aws/lambda/<function-name>.
resource "aws_cloudwatch_log_group" "alexa_smart_home" {
  name              = "/aws/lambda/${local.names.lambda_alexa_smart_home}"
  retention_in_days = var.alexa_smart_home_log_retention_days
  tags              = local.tags
}

# ----------------------------------------
# Lambda function
# ----------------------------------------
resource "aws_lambda_function" "alexa_smart_home" {
  function_name = local.names.lambda_alexa_smart_home
  role          = aws_iam_role.alexa_smart_home.arn
  runtime       = "nodejs24.x"
  handler       = "index.handler"
  architectures = ["x86_64"]

  # Module-relative path to the bundle build_lambda.sh produces (the build step
  # abspath()s the same zip_path; here it stays relative so the value persisted in
  # state is machine-independent). Filename is only read by the AWS provider when
  # source_code_hash differs from state (i.e. an upload is needed) — and that's
  # exactly when null_resource.lambda_build has been triggered to (re)produce the
  # file.
  filename = local.lambdas.alexa_smart_home.zip_path
  # Derive the change-detection marker from the source files directly,
  # not from the built zip. This lets `terraform plan` resolve a stable
  # hash without dist/ or the zip existing (e.g. fresh CI checkout) and
  # makes the hash stable across non-deterministic build artifacts.
  source_code_hash = local.alexa_smart_home_code_hash

  memory_size = var.alexa_smart_home_memory_mb
  timeout     = var.alexa_smart_home_timeout_seconds

  environment {
    variables = {
      AZURE_ENDPOINT  = "https://${azurerm_container_app.lando.ingress[0].fqdn}/api/alexa/smart-home"
      HMAC_SECRET_ARN = aws_secretsmanager_secret.hmac_shared_secret.arn
    }
  }

  logging_config {
    log_format = "Text"
    log_group  = aws_cloudwatch_log_group.alexa_smart_home.name
  }

  tags = merge(local.tags, {
    Version = terraform_data.lambda_version_tag.output
  })

  depends_on = [
    aws_iam_role_policy_attachment.alexa_smart_home_basic_execution,
    aws_iam_role_policy.alexa_smart_home_secrets_read,
    aws_cloudwatch_log_group.alexa_smart_home,
    null_resource.lambda_build,
    terraform_data.lambda_version_tag,
  ]
}

# ----------------------------------------
# Alexa trigger (resource-based policy)
# ----------------------------------------
# Principal is `alexa-connectedhome.amazon.com` (Smart Home), NOT
# `alexa-appkit.amazon.com` (Custom Skills). EventSourceToken locks the
# permission to a specific Alexa skill so other Alexa-issued tokens can't
# invoke this Lambda.
resource "aws_lambda_permission" "alexa_smart_home" {
  statement_id       = "alexa-smart-home-trigger"
  function_name      = aws_lambda_function.alexa_smart_home.function_name
  action             = "lambda:InvokeFunction"
  principal          = "alexa-connectedhome.amazon.com"
  event_source_token = var.alexa_smart_home_skill_id
}
