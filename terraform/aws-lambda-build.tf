# ========================================
# AWS Lambda build + package
# ========================================
locals {
  alexa_smart_home_source_files = concat(
    tolist(fileset(local.lambdas.alexa_smart_home.source_dir, "src/**/*.ts")),
    tolist(fileset(local.lambdas.alexa_smart_home.source_dir, "package.json")),
    tolist(fileset(local.lambdas.alexa_smart_home.source_dir, "package-lock.json")),
    tolist(fileset(local.lambdas.alexa_smart_home.source_dir, "tsconfig.json")),
  )

  # base64sha256 + filesha256 to match AWS's source_code_hash convention.
  # The hash is what aws_lambda_function.alexa_smart_home.source_code_hash
  # consumes directly, so AWS treats it as the change-detection marker.
  alexa_smart_home_code_hash = base64sha256(join("", [
    for f in local.alexa_smart_home_source_files :
    filesha256("${local.lambdas.alexa_smart_home.source_dir}/${f}")
  ]))
}

# 1. Persist the "effective" Lambda version in Terraform state.
#    - triggers_replace fires only when alexa_smart_home_code_hash changes,
#      so a code change destroys/creates this resource and `input` is
#      re-evaluated against the current var.app_version at that moment.
#    - ignore_changes = [input] suppresses in-place updates, so bumping
#      var.app_version with no source change is a no-op: `output` keeps
#      returning the version that was captured the last time the code changed.
resource "terraform_data" "lambda_version_tag" {
  input            = var.app_version
  triggers_replace = [local.alexa_smart_home_code_hash]

  lifecycle {
    ignore_changes = [input]
  }
}

# 2. Build dist/ + package the zip in one shot when source files change.
#    Keyed on terraform_data.lambda_version_tag.id — the only way this
#    resource is replaced is when lambda_version_tag is replaced, which
#    only happens when source files change. Tag-only bumps no longer
#    re-trigger the build.
resource "null_resource" "lambda_build" {
  triggers = {
    state_id = terraform_data.lambda_version_tag.id
  }

  provisioner "local-exec" {
    working_dir = local.lambdas.alexa_smart_home.source_dir
    command     = "${abspath(path.module)}/scripts/build_lambda.sh"
    environment = {
      ZIP_PATH = local.lambdas.alexa_smart_home.zip_path
    }
  }
}
