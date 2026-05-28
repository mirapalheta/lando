# ========================================
# Variables that apply across both clouds
# ========================================

variable "project_name" {
  description = "Project name used for resource naming (e.g., 'lando', 'alexa-ha'). Feeds into both Azure resource names and AWS resource tags."
  type        = string
  default     = "lando"
}

variable "tags" {
  description = "Common tags applied to all taggable resources in both AWS and Azure."
  type        = map(string)
  default = {
    ManagedBy = "Terraform",
    Project   = "Lando",
  }
}

variable "hmac_max_clock_skew_seconds" {
  description = "Maximum allowed drift between the Lambda's signed timestamp and the Azure-side clock. 300s matches Slack/Stripe webhook conventions. Lower = more replay-resistant but more sensitive to clock skew."
  type        = number
  default     = 300
}
