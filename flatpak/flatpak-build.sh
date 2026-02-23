#!/bin/sh

# REMINDER: This file is executed by flatpak-builder

# Disable telemetry
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export SRC_APP=./src/Aria.App
export SRC_RESOURCES="${SRC_APP}"/Resources

# Prepare the directory
mkdir -p "${FLATPAK_DEST}"/bin/

# Check if blueprint-compiler is available
which blueprint-compiler || echo "WARNING: blueprint-compiler not found in PATH"

# Copy resources
install -Dm644 "${SRC_RESOURCES}"/"${FLATPAK_ID}".desktop -t /app/share/applications
install -Dm644 "${SRC_RESOURCES}"/"${FLATPAK_ID}".metainfo.xml -t /app/share/metainfo

install -Dm644 "${SRC_RESOURCES}"/icons/icon_16.png /app/share/icons/hicolor/16x16/apps/"${FLATPAK_ID}".png
install -Dm644 "${SRC_RESOURCES}"/icons/icon_32.png /app/share/icons/hicolor/32x32/apps/"${FLATPAK_ID}".png
install -Dm644 "${SRC_RESOURCES}"/icons/icon_64.png /app/share/icons/hicolor/64x64/apps/"${FLATPAK_ID}".png
install -Dm644 "${SRC_RESOURCES}"/icons/icon_128.png /app/share/icons/hicolor/128x128/apps/"${FLATPAK_ID}".png
install -Dm644 "${SRC_RESOURCES}"/icons/icon_256.png /app/share/icons/hicolor/256x256/apps/"${FLATPAK_ID}".png

# Build & Publish App
./build.sh "${FLATPAK_DEST}/bin"