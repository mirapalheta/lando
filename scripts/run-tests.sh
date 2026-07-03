#!/bin/bash
set -e

dotnet format --verbosity diagnostic

find . -type f -name "coverage.cobertura.xml" -delete
find . -type d -name TestResults -exec rm -rf {} +
find . -type d -name coverage -exec rm -rf {} +
find . -type d -name bin -exec rm -rf {} +
find . -type d -name obj -exec rm -rf {} +

dotnet restore
dotnet build --no-restore
dotnet test --settings tests/Test.runsettings --no-build --collect:"XPlat Code Coverage"

(
        cd src/aws/lando-alexa-proxy
        npm install
        npm run test:coverage
)

reports=$(find . -type f \( -name "coverage.cobertura.xml" -o -name "lcov.info" \) | tr '\n' ';')

dotnet tool restore
dotnet reportgenerator \
        -targetdir:coverage \
        -reporttypes:Html \
        -reports:"$reports" \
        -sourcedirs:"src/aws/lando-alexa-proxy" \
        -assemblyfilters:"-*.Tests"

open coverage/index.html
