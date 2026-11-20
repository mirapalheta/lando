# Build context is expected to be src/azure/ (the image is built by terraform/scripts/build_image.sh).
# All COPY paths below are relative to that — keeping the context Azure-only
# means the AWS Lambda TypeScript directory isn't shipped to the daemon and
# doesn't bust the build cache on every Lambda-side change.

# ----------------------------------------------------------------------------
# Stage 1: Build
# ----------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:11.0 AS build

ARG COMMIT=
ARG BRANCH=
ARG VERSION=

WORKDIR /src

# Central package versions (ManagePackageVersionsCentrally): restore needs
# them next to the project files.
COPY Directory.Packages.props                                                   ./

# Copy project files first for dependency-resolution layer caching. Order
# doesn't matter for restore but stays alphabetical for easy auditing — add
# new projects here whenever a *.csproj is introduced under src/azure/.
COPY Lando.Abstractions/Lando.Abstractions.csproj                               ./Lando.Abstractions/
COPY Lando.Alexa.Core/Lando.Alexa.Core.csproj                                   ./Lando.Alexa.Core/
COPY Lando.Alexa.CustomSkill/Lando.Alexa.CustomSkill.csproj                     ./Lando.Alexa.CustomSkill/
COPY Lando.Alexa.SmartHome/Lando.Alexa.SmartHome.csproj                         ./Lando.Alexa.SmartHome/
COPY Lando.FunctionApp/Lando.FunctionApp.csproj                                 ./Lando.FunctionApp/
COPY Lando.HomeAssistant.Abstractions/Lando.HomeAssistant.Abstractions.csproj   ./Lando.HomeAssistant.Abstractions/
COPY Lando.HomeAssistant.Core/Lando.HomeAssistant.Core.csproj                   ./Lando.HomeAssistant.Core/

# Restore via FunctionApp.csproj — its ProjectReference graph pulls in the rest.
RUN dotnet restore ./Lando.FunctionApp/Lando.FunctionApp.csproj

# Copy remaining source (all *.cs etc.) on top of the cached restore layer.
COPY . ./

# Publish
RUN dotnet publish ./Lando.FunctionApp/Lando.FunctionApp.csproj \
    --no-restore \
    --configuration Release \
    --output /app/publish \
    --os linux \
    -p:BRANCH="$BRANCH" \
    -p:COMMIT="$COMMIT" \
    -p:VERSION="$VERSION" \
    -p:UseAppHost=false

# ----------------------------------------------------------------------------
# Stage 2: Runtime (Azure Functions base image)
# ----------------------------------------------------------------------------
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated11.0-azurelinux3

WORKDIR /home/site/wwwroot

# Install Home Assistant certificate(s) into system trust store.
# Both are optional; the conditional install survives empty values
# without failing the build.
# The .NET 11 Functions images are Azure Linux only, so the trust anchors live
# under /etc/pki and are compiled with update-ca-trust (not the Debian
# /usr/local/share/ca-certificates + update-ca-certificates pair).
RUN --mount=type=secret,id=ha_caf \
    --mount=type=secret,id=ha_crt \
    [ -s /run/secrets/ha_caf ] && cp /run/secrets/ha_caf /etc/pki/ca-trust/source/anchors/ha_ca.crt; \
    [ -s /run/secrets/ha_crt ] && cp /run/secrets/ha_crt /etc/pki/ca-trust/source/anchors/ha_cert.crt; \
    { [ -s /run/secrets/ha_caf ] || [ -s /run/secrets/ha_crt ]; } && update-ca-trust || true

# Copy published app from build stage
COPY --from=build /app/publish .

# Set environment
ENV AzureWebJobsScriptRoot=/home/site/wwwroot
ENV AzureFunctionsJobHost__Logging__Console__IsEnabled=true
ENV FUNCTIONS_WORKER_RUNTIME=dotnet-isolated

EXPOSE 80
