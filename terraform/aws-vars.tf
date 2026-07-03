# ========================================
# AWS / Alexa proxy Lambda variables
# ========================================

variable "aws_region" {
  description = "AWS region for the Alexa Lambda proxy. us-east-1 is the standard Alexa Smart Home endpoint for North America."
  type        = string
  default     = "us-east-1"
}

variable "alexa_skill_id" {
  description = "Alexa skill ID allowed to invoke the Lambda, for both the Smart Home directive trigger and the Custom Skill (intent) trigger. This is one skill with both models enabled — there's no separate Custom Skill id. Set as lambda:EventSourceToken on both resource-based policy statements. Example: amzn1.ask.skill.XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX"
  type        = string
}

variable "alexa_proxy_log_retention_days" {
  description = "CloudWatch log retention for the Alexa proxy Lambda."
  type        = number
  default     = 30
}

variable "alexa_proxy_memory_mb" {
  description = "Memory allocation for the Alexa proxy Lambda. 256MB gives faster cold starts than the previous 128MB at negligible extra cost for this volume."
  type        = number
  default     = 256
}

variable "alexa_proxy_timeout_seconds" {
  description = "Execution timeout for the Alexa proxy Lambda. Alexa Smart Home requires sub-8s end-to-end; 15s outer bound leaves room for cold-start + Azure latency before AWS kills the invocation."
  type        = number
  default     = 15
}
