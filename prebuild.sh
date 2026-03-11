#!/bin/sh
set -eu

# Generate nuget sources file
[ ! -f ./flatpak/flatpak-dotnet-generator.py ] && wget https://raw.githubusercontent.com/flatpak/flatpak-builder-tools/master/dotnet/flatpak-dotnet-generator.py -O ./flatpak/flatpak-dotnet-generator.py
echo "Updating nuget sources"
python3 ./flatpak/flatpak-dotnet-generator.py \
    --dotnet 10 \
    --freedesktop 25.08 \
    nuget-sources.json \
    ./src/Ebony.App/Ebony.App.csproj


# Check if gircore directory is not empty
if [ -d ./lib/gircore/src/Libs/Adw-1/Internal ] && [ -n "$(ls -A ./lib/gircore/src/Libs/Adw-1/Internal 2>/dev/null)" ]; then
  echo "Gir.Core Library sources already generated. Skipping."
  exit 0
fi

cd ./lib/gircore/scripts || exit 1
echo "Generating Gir.Core library sources"
dotnet fsi GenerateLibs.fsx