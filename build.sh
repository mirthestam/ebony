#!/bin/sh
set -eu

# Disable telemetry
export DOTNET_CLI_TELEMETRY_OPTOUT=1

# Optional target output directory (default: ./dist)
TARGET_DIR="${1:-./dist}"

# check for prerequisites
GIRCORE_DIR="./lib/gircore/src/Libs/Adw-1/Internal"
NUGET_SOURCES_DIR="./nuget-sources"
NUGET_SOURCES_JSON="./nuget-sources.json"

if [ ! -f "$NUGET_SOURCES_JSON" ]; then
  echo "Error: '$NUGET_SOURCES_JSON' not found. Run './prebuild.sh' first." >&2
  exit 1
fi

if [ ! -d "$GIRCORE_DIR" ] || [ -z "$(ls -A "$GIRCORE_DIR" 2>/dev/null)" ]; then
  echo "Error: Gir.Core Library sources missing (folder missing or empty): $GIRCORE_DIR" >&2
  exit 1
fi

# Publish Aria
echo "Info: building to '$TARGET_DIR'."
dotnet publish ./src/Aria.App/Aria.App.csproj -o "$TARGET_DIR" -c Release --no-self-contained --source $NUGET_SOURCES_DIR
cp ./src/Aria.App/bin/Release/net10.0/nl.mirthestam.aria.gresource "$TARGET_DIR"
echo "Info: Finished building to '$TARGET_DIR'"