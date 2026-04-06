#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PUBLISH_DIR="$ROOT_DIR/flatpak/published"
BUILD_DIR="$ROOT_DIR/flatpak/build-dir"
REPO_DIR="$ROOT_DIR/flatpak/repo"
MANIFEST="$ROOT_DIR/flatpak/io.github.thomasn.MailGrabber.yaml"
BUNDLE_PATH="$ROOT_DIR/MailGrabber.flatpak"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet SDK is required but was not found."
  exit 1
fi

if ! command -v flatpak >/dev/null 2>&1; then
  echo "flatpak is required but was not found."
  exit 1
fi

if command -v flatpak-builder >/dev/null 2>&1; then
  FLATPAK_BUILDER_CMD=(flatpak-builder)
elif flatpak info --user org.flatpak.Builder >/dev/null 2>&1; then
  FLATPAK_BUILDER_CMD=(flatpak run --user org.flatpak.Builder)
else
  echo "Neither flatpak-builder nor org.flatpak.Builder is available."
  echo "Install one of them:"
  echo "  sudo pacman -S flatpak-builder"
  echo "  flatpak install -y --user flathub org.flatpak.Builder"
  exit 1
fi

if ! flatpak remotes --columns=name | grep -qx "flathub"; then
  flatpak remote-add --if-not-exists flathub https://flathub.org/repo/flathub.flatpakrepo
fi

for ref in org.freedesktop.Platform//24.08 org.freedesktop.Sdk//24.08; do
  if ! flatpak info "$ref" >/dev/null 2>&1; then
    flatpak install -y --user flathub "$ref"
  fi
done

rm -rf "$PUBLISH_DIR" "$BUILD_DIR" "$REPO_DIR"

# Publish self-contained binary so Flatpak does not depend on host .NET runtime.
dotnet publish "$ROOT_DIR/MailGrabber/MailGrabber.csproj" \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -o "$PUBLISH_DIR"

# Do not package local secrets/config from the build machine.
rm -f "$PUBLISH_DIR/appsettings.json" "$PUBLISH_DIR/google-client-secret.json"

"${FLATPAK_BUILDER_CMD[@]}" \
  --user \
  --force-clean \
  --repo="$REPO_DIR" \
  "$BUILD_DIR" \
  "$MANIFEST"

flatpak build-bundle "$REPO_DIR" "$BUNDLE_PATH" io.github.thomasn.MailGrabber stable

echo "Build complete: $BUNDLE_PATH"
