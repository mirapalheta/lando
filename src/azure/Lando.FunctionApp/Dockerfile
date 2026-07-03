# Build context is expected to be src/azure/ (see .github/workflows/deploy-prod.yml).
# All COPY paths below are relative to that — keeping the context Azure-only
# means the AWS Lambda TypeScript directory isn't shipped to the daemon and
# doesn't bust the build cache on every Lambda-side change.

# ----------------------------------------------------------------------------
# Stage 1: Build
# ----------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

ARG COMMIT=
ARG BRANCH=
ARG VERSION=

WORKDIR /src

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
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated10.0

WORKDIR /home/site/wwwroot

# Install Home Assistant certificate(s) into system trust store.
# Both are optional; the conditional install survives empty values
# without failing the build.
RUN --mount=type=secret,id=ha_caf \
    --mount=type=secret,id=ha_crt \
    [ -s /run/secrets/ha_caf ] && cp /run/secrets/ha_caf /usr/local/share/ca-certificates/ha_ca.crt; \
    [ -s /run/secrets/ha_crt ] && cp /run/secrets/ha_crt /usr/local/share/ca-certificates/ha_cert.crt; \
    { [ -s /run/secrets/ha_caf ] || [ -s /run/secrets/ha_crt ]; } && update-ca-certificates || true

# Copy published app from build stage
COPY --from=build /app/publish .

# Set environment
ENV AzureWebJobsScriptRoot=/home/site/wwwroot
ENV AzureFunctionsJobHost__Logging__Console__IsEnabled=true
ENV FUNCTIONS_WORKER_RUNTIME=dotnet-isolated

EXPOSE 80
