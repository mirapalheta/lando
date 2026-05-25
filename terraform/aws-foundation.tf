# ========================================
# AWS foundation — account/region lookup
# ========================================
# Stable place to add cross-AWS data sources later (default VPC, region info, etc.).

data "aws_caller_identity" "current" {}
