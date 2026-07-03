# ========================================
# AWS identity — GitHub Actions deploy user
# ========================================
# Mirrors the Azure GitHub Actions service principal in azure-identity.tf.
# A long-lived IAM user with an inline policy scoped to exactly the AWS
# resources the Terraform stack manages on the AWS side.
#
# Bootstrap note (chicken-and-egg): the human runs the first `terraform apply`
# with a privileged AWS identity (`aws configure` / SSO admin), which creates
# this user. After that, the user's access key is exported as a Terraform
# output, copied into GitHub secrets (see deployment.md § "Set GitHub Secrets"),
# and subsequent CI runs of `terraform apply` use it.
#
# The policy is intentionally scoped to:
#   - the Alexa proxy Lambda + its role + its CloudWatch log group
#   - the HMAC shared secret in Secrets Manager
#   - read-only access on this user's own IAM resources (so `terraform plan`
#     can refresh them without granting self-mutation rights — drift on this
#     user has to be fixed by the human with privileged creds, not by CI)
# Anything outside this scope is denied by default.

resource "aws_iam_user" "github_actions" {
  name = "user-${var.project_name}-github-actions"
  path = "/"
  # tags omitted — CI runs as this user and the policy intentionally withholds
  # iam:TagUser (self-mutation rights). Tag the user manually if needed.
}

resource "aws_iam_access_key" "github_actions" {
  user = aws_iam_user.github_actions.name
}

# NOTE: Inline user policies (aws_iam_user_policy) are capped at 2048 bytes
# by AWS — a hard limit on the PutUserPolicy API. This policy is over that
# limit, so we attach a managed policy instead (6144-byte limit, plenty of
# headroom for future actions like lambda:GetFunctionConcurrency etc).
resource "aws_iam_policy" "github_actions_deploy" {
  name        = "policy-${var.project_name}-github-actions-deploy"
  description = "Scoped deploy permissions for the GitHub Actions IAM user — Alexa Lambda + its IAM role + log group + HMAC secret, plus read-only on the user itself."

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid      = "STSIdentity"
        Effect   = "Allow"
        Action   = ["sts:GetCallerIdentity"]
        Resource = "*"
      },
      {
        Sid    = "LambdaManage"
        Effect = "Allow"
        Action = [
          "lambda:CreateFunction",
          "lambda:GetFunction",
          "lambda:GetFunctionConfiguration",
          "lambda:GetFunctionCodeSigningConfig",
          "lambda:UpdateFunctionCode",
          "lambda:UpdateFunctionConfiguration",
          "lambda:DeleteFunction",
          "lambda:ListFunctions",
          "lambda:ListVersionsByFunction",
          "lambda:ListTags",
          "lambda:TagResource",
          "lambda:UntagResource",
          "lambda:AddPermission",
          "lambda:RemovePermission",
          "lambda:GetPolicy",
          "lambda:PutFunctionConcurrency",
          "lambda:DeleteFunctionConcurrency",
        ]
        Resource = [
          "arn:aws:lambda:*:${data.aws_caller_identity.current.account_id}:function:${local.names.lambda_alexa_proxy}",
          "arn:aws:lambda:*:${data.aws_caller_identity.current.account_id}:function:${local.names.lambda_alexa_proxy}:*",
        ]
      },
      {
        Sid    = "IAMRoleManage"
        Effect = "Allow"
        Action = [
          "iam:CreateRole",
          "iam:GetRole",
          "iam:UpdateRole",
          "iam:UpdateAssumeRolePolicy",
          "iam:DeleteRole",
          "iam:TagRole",
          "iam:UntagRole",
          "iam:ListRoleTags",
          "iam:ListRolePolicies",
          "iam:ListAttachedRolePolicies",
          "iam:ListInstanceProfilesForRole",
          "iam:PutRolePolicy",
          "iam:GetRolePolicy",
          "iam:DeleteRolePolicy",
          "iam:AttachRolePolicy",
          "iam:DetachRolePolicy",
          "iam:PassRole",
        ]
        Resource = "arn:aws:iam::${data.aws_caller_identity.current.account_id}:role/${local.names.iam_role_alexa_proxy}"
      },
      {
        # Read on the AWS-managed policy that gets attached to the Lambda's
        # execution role (AWSLambdaBasicExecutionRole), plus read on the
        # customer-managed deploy policy attached to this user — Terraform
        # refresh needs both.
        Sid    = "IAMManagedPolicyRead"
        Effect = "Allow"
        Action = ["iam:GetPolicy", "iam:GetPolicyVersion"]
        Resource = [
          "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole",
          "arn:aws:iam::${data.aws_caller_identity.current.account_id}:policy/policy-${var.project_name}-github-actions-deploy",
        ]
      },
      {
        Sid    = "LogsManage"
        Effect = "Allow"
        Action = [
          "logs:CreateLogGroup",
          "logs:DeleteLogGroup",
          "logs:PutRetentionPolicy",
          "logs:DeleteRetentionPolicy",
          "logs:ListTagsForResource",
          "logs:ListTagsLogGroup",
          "logs:TagResource",
          "logs:UntagResource",
        ]
        Resource = "arn:aws:logs:*:${data.aws_caller_identity.current.account_id}:log-group:/aws/lambda/${local.names.lambda_alexa_proxy}*"
      },
      {
        # logs:DescribeLogGroups is an account-level list action — AWS IAM
        # does not let you restrict it to a specific log-group ARN. The
        # terraform aws_cloudwatch_log_group resource calls it during refresh
        # to look up the existing log group by name. Granting it on `*` is
        # the only way; the read is still filtered server-side by the name
        # prefix the resource asks for.
        Sid      = "LogsList"
        Effect   = "Allow"
        Action   = ["logs:DescribeLogGroups"]
        Resource = "*"
      },
      {
        Sid    = "SecretsManagerManage"
        Effect = "Allow"
        Action = [
          "secretsmanager:CreateSecret",
          "secretsmanager:GetSecretValue",
          "secretsmanager:PutSecretValue",
          "secretsmanager:DescribeSecret",
          "secretsmanager:UpdateSecret",
          "secretsmanager:DeleteSecret",
          "secretsmanager:RestoreSecret",
          "secretsmanager:TagResource",
          "secretsmanager:UntagResource",
          "secretsmanager:ListSecretVersionIds",
          # Newer AWS provider versions read the resource policy as part of
          # refresh. Without this, `terraform plan` fails with AccessDenied on
          # the GetResourcePolicy call even though the secret itself reads fine.
          "secretsmanager:GetResourcePolicy",
        ]
        Resource = "arn:aws:secretsmanager:*:${data.aws_caller_identity.current.account_id}:secret:${local.names.secretsmanager_hmac}*"
      },
      {
        # Read-only on this user's own IAM resources so `terraform plan` /
        # `apply` can refresh state without being able to modify the user.
        # Any drift here has to be reconciled by the privileged human running
        # `terraform apply` locally, not by CI.
        Sid    = "IAMSelfRead"
        Effect = "Allow"
        Action = [
          "iam:GetUser",
          "iam:ListAccessKeys",
          "iam:GetAccessKeyLastUsed",
          "iam:ListAttachedUserPolicies",
          "iam:ListUserTags",
        ]
        Resource = "arn:aws:iam::${data.aws_caller_identity.current.account_id}:user/user-${var.project_name}-github-actions"
      },
    ]
  })
}

resource "aws_iam_user_policy_attachment" "github_actions_deploy" {
  user       = aws_iam_user.github_actions.name
  policy_arn = aws_iam_policy.github_actions_deploy.arn
}
