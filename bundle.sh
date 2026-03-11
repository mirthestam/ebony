#!/bin/bash
set -euo pipefail

NUGET_SOURCES_JSON="./nuget-sources.json"
if [ ! -f "$NUGET_SOURCES_JSON" ]; then
  echo "Error: '$NUGET_SOURCES_JSON' not found. Run './prebuild.sh' first." >&2
  exit 1
fi

# Build the repo
flatpak-builder --force-clean --user --install-deps-from=flathub --repo=flatpak/repo flatpak/builddir ./flatpak/nl.mirthestam.ebony.yml

# Bundle the repo
echo "Bundling to ./flatpak/ebony.flatpak"
flatpak build-bundle flatpak/repo flatpak/ebony.flatpak nl.mirthestam.ebony \
  --runtime-repo=https://flathub.org/repo/flathub.flatpakrepo
  
echo "Exported ./flatpak/ebony.flatpak"