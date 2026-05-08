#!/usr/bin/env bash
# Builds and publishes the MAUI Android project as an APK.

set -euo pipefail

readonly SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
readonly KIROKUU_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
readonly CSPROJ="${KIROKUU_ROOT}/Kirokuu/Kirokuu.csproj"
readonly TFM="net9.0-android"

CONFIGURATION="${1:-Release}"

if [[ "${CONFIGURATION}" != "Release" && "${CONFIGURATION}" != "Debug" ]]; then
  echo "Invalid configuration: ${CONFIGURATION}" >&2
  echo "Usage: $(basename "$0") [Release|Debug]" >&2
  exit 1
fi

if [[ ! -f "${CSPROJ}" ]]; then
  echo "Project file not found: ${CSPROJ}" >&2
  exit 1
fi

echo "Publishing APK (${CONFIGURATION})..."
dotnet publish "${CSPROJ}" \
  -f "${TFM}" \
  -c "${CONFIGURATION}" \
  -p:AndroidPackageFormats=apk \
  -p:AndroidSdkBuildToolsVersion=35.0.0 \
  -v minimal

readonly PUBLISH_DIR="${KIROKUU_ROOT}/Kirokuu/bin/${CONFIGURATION}/${TFM}/publish"
APK_FILE="$(ls "${PUBLISH_DIR}"/*.apk 2>/dev/null | head -n 1 || true)"

if [[ -z "${APK_FILE}" ]]; then
  echo "APK was not generated in ${PUBLISH_DIR}" >&2
  exit 1
fi

echo "APK generated successfully:"
echo "${APK_FILE}"
