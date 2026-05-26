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

# 1. Build dist/ + package the zip in one shot when source files change.
#    build_lambda.sh runs `npm ci && npm run build` then writes the final
#    zip to $ZIP_PATH, so terraform doesn't need a separate archive_file
#    resource — and a fresh runner (no dist/, no zip) is handled implicitly:
#    if code_hash changed, the script rebuilds everything; if not, the zip
#    isn't needed because aws_lambda_function.source_code_hash also matches
#    state and AWS skips the upload.
resource "null_resource" "lambda_build" {
  triggers = {
    code_hash = local.alexa_smart_home_code_hash
  }

  provisioner "local-exec" {
    working_dir = local.lambdas.alexa_smart_home.source_dir
    command     = "${abspath(path.module)}/scripts/build_lambda.sh"
    environment = {
      ZIP_PATH = "${abspath(path.module)}/${local.lambdas.alexa_smart_home.zip_path}"
    }
  }
}
