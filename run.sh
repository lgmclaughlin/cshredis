#!/bin/sh

set -e # Exit early if any commands fail

(
  cd "$(dirname "$0")" # Ensure compile steps are run within the repository directory
  dotnet build --configuration Release --output tmp/CShredis src/CShredis.Network/CShredis.Network.csproj
)

exec tmp/CShredis/CShredis.Network "$@"
