# ========================================
# AWS Lambda build + package
# ========================================
locals {
  alexa_proxy_source_files = concat(
    tolist(fileset(local.lambdas.alexa_proxy.source_dir, "src/**/*.ts")),
    tolist(fileset(local.lambdas.alexa_proxy.source_dir, "package.json")),
    tolist(fileset(local.lambdas.alexa_proxy.source_dir, "package-lock.json")),
    tolist(fileset(local.lambdas.alexa_proxy.source_dir, "tsconfig.json")),
  )

  # base64sha256 + filesha256 to match AWS's source_code_hash convention.
  # The hash is what aws_lambda_function.alexa_proxy.source_code_hash
  # consumes directly, so AWS treats it as the change-detection marker.
  alexa_proxy_code_hash = base64sha256(join("", [
    for f in local.alexa_proxy_source_files :
    filesha256("${local.lambdas.alexa_proxy.source_dir}/${f}")
  ]))
}

# 1. Persist the "effective" Lambda version in Terraform state.
#    - triggers_replace fires only when alexa_proxy_code_hash changes,
#      so a code change destroys/creates this resource and `input` is
#      re-evaluated against the current var.app_version at that moment.
#    - ignore_changes = [input] suppresses in-place updates, so bumping
#      var.app_version with no source change is a no-op: `output` keeps
#      returning the version that was captured the last time the code changed.
resource "terraform_data" "lambda_version_tag" {
  input            = var.app_version
  triggers_replace = [local.alexa_proxy_code_hash]

  lifecycle {
    ignore_changes = [input]
  }
}

# 2. Build dist/ + package the zip on every apply.
#    The packaged zip lives in the ephemeral .terraform/ dir, so it never
#    survives to the next run: a fresh CI runner recreates .terraform/ empty,
#    and a partially-failed apply can advance the source-change marker
#    (terraform_data.lambda_version_tag) WITHOUT the artifact surviving —
#    leaving aws_lambda_function.filename pointing at a missing file. Gating the
#    build on the change marker (state_id) therefore skips a rebuild exactly when
#    the artifact is absent. Rebuilding unconditionally guarantees the zip exists
#    whenever the function is applied. source_code_hash is derived from the
#    source files (not the built zip), so an unchanged-source rebuild does NOT
#    trigger a spurious code upload — the build just re-materializes the file.
resource "null_resource" "lambda_build" {
  triggers = {
    always_run = timestamp()
  }

  provisioner "local-exec" {
    working_dir = local.lambdas.alexa_proxy.source_dir
    command     = "${abspath(path.module)}/scripts/build_lambda.sh"
    environment = {
      ZIP_PATH = abspath(local.lambdas.alexa_proxy.zip_path)
    }
  }
}
