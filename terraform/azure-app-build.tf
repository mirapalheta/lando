# ========================================
# Azure Function App image build + push
# ========================================
# Builds the Function App container image locally via `docker build`
# (BuildKit, with HA certs mounted as secrets) and pushes the resulting
# tagged image to ACR. The build itself is delegated to
# scripts/build_image.sh, which also performs the "does this tag already
# exist?" pre-check at the top of the script (the script only runs when
# terraform's triggers fire, so the pre-check is what protects against
# accidental overwrites).
# ========================================

# 1. Compute a hash over all source files that affect the built image.
#    Used as a trigger so terraform re-runs the build when (and only when)
#    the .NET sources, project files, Dockerfile, or .dockerignore change.
locals {
  source_path = "${path.module}/../src/azure"
  docker_file = "${local.source_path}/Lando.FunctionApp/Dockerfile"

  # Files that contribute to code_base_hash. `**/*.cs` would otherwise
  # pick up generated AssemblyInfo / AssemblyAttributes files under each
  # project's bin/ and obj/ directories on a developer machine after
  # `dotnet build`, while a fresh CI checkout has no bin/ or obj/ at all.
  # Excluding both keeps the hash stable across local + GitHub runs.
  source_files = [
    for f in concat(
      tolist(fileset(local.source_path, "**/*.cs")),
      tolist(fileset(local.source_path, "**/*.csproj")),
      tolist(fileset(local.source_path, "**/Dockerfile")),
      tolist(fileset(local.source_path, "**/.dockerignore")),
    ) : f if length(regexall("(^|/)(bin|obj)/", f)) == 0
  ]

  # Combines hashes of all files into one master hash string
  code_base_hash = md5(join("", [for f in local.source_files : filemd5("${local.source_path}/${f}")]))
}

# 2. Persist the "effective" image tag in Terraform state.
#    - triggers_replace fires only when code_base_hash changes, so a code
#      change destroys/creates this resource and `input` is re-evaluated
#      against the current var.image_tag at that moment.
#    - ignore_changes = [input] suppresses in-place updates, so bumping
#      var.image_tag with no source change is a no-op: `output` keeps
#      returning the tag that was captured the last time the code changed.
#    Net effect: `output` is the tag of the image that actually corresponds
#    to whatever is currently in src/. Downstream consumers (container app,
#    build provisioner) read it instead of var.image_tag directly.
resource "terraform_data" "image_state" {
  input            = var.image_tag
  triggers_replace = [local.code_base_hash]

  lifecycle {
    ignore_changes = [input]
  }
}

# 3. Build, publish, or fail based on the rules in build_image.sh.
#    Keyed on terraform_data.image_state.id — the only way this resource
#    is replaced is when image_state is replaced, which only happens when
#    source files change. Tag-only bumps no longer re-trigger the build.
resource "null_resource" "acr_image_build" {
  triggers = {
    state_id = terraform_data.image_state.id
  }

  provisioner "local-exec" {
    command = "./scripts/build_image.sh"

    # Pass Terraform variables safely into the shell environment so the
    # script doesn't have to know anything about Terraform.
    environment = {
      REGISTRY_NAME = azurerm_container_registry.lando.name
      IMAGE_TAG     = terraform_data.image_state.output
      IMAGE_NAME    = var.project_name
      DOCKER_FILE   = local.docker_file
      SOURCE_PATH   = local.source_path

      #  HA certificate file paths
      HOME_ASSISTANT_CAF = var.home_assistant_ca_file
      HOME_ASSISTANT_CRT = var.home_assistant_cert_file
    }
  }

  depends_on = [azurerm_container_registry.lando]
}
