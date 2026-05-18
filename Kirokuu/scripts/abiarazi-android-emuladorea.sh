#!/usr/bin/env bash
# Rebuilds the MAUI Android project and deploys it to a running emulator (or starts the default AVD).
# Note: opening the AVD from Android Studio alone does not install the app; you must run this script
# (or dotnet build -t:Run with the correct device) so adb deploys the APK.

set -euo pipefail

readonly ANDROID_PACKAGE_ID="com.companyname.kirokuu"

readonly SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
readonly KIROKUU_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
readonly CSPROJ="${KIROKUU_ROOT}/Kirokuu/Kirokuu.csproj"
readonly TFM="net9.0-android"

: "${ANDROID_SDK_ROOT:=${HOME}/Library/Android/sdk}"
: "${ANDROID_AVD:=Medium_Phone_API_35}"
: "${JAVA_HOME:=/Library/Java/JavaVirtualMachines/jdk-17.jdk/Contents/Home}"
: "${KIROKUU_LOGCAT:=1}"

export ANDROID_SDK_ROOT
export JAVA_HOME
export PATH="${ANDROID_SDK_ROOT}/emulator:${ANDROID_SDK_ROOT}/platform-tools:${PATH}"

if [[ ! -x "${JAVA_HOME}/bin/java" ]]; then
  echo "JAVA_HOME ez da baliozkoa: ${JAVA_HOME}" >&2
  exit 1
fi

if [[ ! -f "${CSPROJ}" ]]; then
  echo "Ez da aurkitu proiektua: ${CSPROJ}" >&2
  exit 1
fi

if [[ ! -x "${ANDROID_SDK_ROOT}/emulator/emulator" ]]; then
  echo "Ez da aurkitu Android emulatorra: ${ANDROID_SDK_ROOT}/emulator/emulator" >&2
  exit 1
fi

emulator_serial="$(adb devices 2>/dev/null | awk '/^emulator-[0-9]+\tdevice$/ { print $1; exit }')"
if [[ -n "${emulator_serial}" ]]; then
  export ANDROID_SERIAL="${emulator_serial}"
  echo "Emuladorea dagoeneko martxan: ${ANDROID_SERIAL}"
else
  echo "Emuladorea abiarazten: ${ANDROID_AVD}"
  "${ANDROID_SDK_ROOT}/emulator/emulator" -avd "${ANDROID_AVD}" -no-boot-anim &
  adb wait-for-device
  adb devices -l
  emulator_serial="$(adb devices 2>/dev/null | awk '/^emulator-[0-9]+\tdevice$/ { print $1; exit }')"
  if [[ -z "${emulator_serial}" ]]; then
    echo "Ez da aurkitu emuladore konektaturik." >&2
    exit 1
  fi
  export ANDROID_SERIAL="${emulator_serial}"
  echo "Itxaron sistema abioa amaitzeko (${ANDROID_SERIAL})..."
  boot_timeout=300
  elapsed=0
  while [[ "$(adb -s "${emulator_serial}" shell getprop sys.boot_completed 2>/dev/null | tr -d '\r')" != "1" ]]; do
    sleep 3
    elapsed=$((elapsed + 3))
    if [[ "${elapsed}" -ge "${boot_timeout}" ]]; then
      echo "Denbora-muga: emuladorea ez da abioa amaitu arte iritsi." >&2
      exit 1
    fi
  done
fi

readonly KIROKUU_PROIEKTU_KARPETA="${KIROKUU_ROOT}/Kirokuu"
readonly OBJ_ANDROID="${KIROKUU_PROIEKTU_KARPETA}/obj/Debug/${TFM}"

if [[ "${KIROKUU_LOGCAT}" == "1" ]]; then
  echo "Logcat garbitzen (${emulator_serial})..."
  adb -s "${emulator_serial}" logcat -c || true
fi

echo "Konpilazio-zerbitzariak ixten (fitxategi-loturak askatzeko)..."
dotnet build-server shutdown 2>/dev/null || true

export MSBUILDDISABLENODEREUSE=1

echo "obj/bin ezabatzen (${TFM}, XARDF7023 / obj hondatua saihesteko)..."
rm -rf "${KIROKUU_PROIEKTU_KARPETA}/obj" "${KIROKUU_PROIEKTU_KARPETA}/bin"

echo "Garbitzen (dotnet clean): ${CSPROJ}"
dotnet clean "${CSPROJ}" -f "${TFM}" -v minimal || true

if [[ -d "${OBJ_ANDROID}" ]]; then
  echo "obj ${TFM} berriz ezabatzen (loturak)..."
  rm -rf "${OBJ_ANDROID}"
fi

sleep 1

echo "Eraikitzen eta exekutatzen: ${CSPROJ} -> ${emulator_serial}"
dotnet build "${CSPROJ}" -t:Run -f "${TFM}" -v minimal -maxcpucount:1 \
  "-p:AdbTarget=-s ${emulator_serial}"

echo "Egiaztatzen instalazioa adb-rekin (${emulator_serial})..."
if ! adb -s "${emulator_serial}" shell pm list packages 2>/dev/null | grep -qF "${ANDROID_PACKAGE_ID}"; then
  echo "ERROREA: '${ANDROID_PACKAGE_ID}' ez dago instalatuta. Konpilazioa huts egin du edo beste gailu batera joan da." >&2
  echo "Saiatu: Android Studio eta beste 'dotnet build' itxi, eta script hau berriro exekutatu." >&2
  exit 1
fi

echo "Aplikazioa aurrealdean irekitzen..."
adb -s "${emulator_serial}" shell monkey -p "${ANDROID_PACKAGE_ID}" -c android.intent.category.LAUNCHER 1 >/dev/null 2>&1 || true

if [[ "${KIROKUU_LOGCAT}" == "1" ]]; then
  echo "Aplikazioaren logak bilatzen..."
  app_pid=""
  for _ in {1..20}; do
    app_pid="$(adb -s "${emulator_serial}" shell pidof "${ANDROID_PACKAGE_ID}" 2>/dev/null | tr -d '\r' | awk '{ print $1; exit }')"
    if [[ -n "${app_pid}" ]]; then
      break
    fi
    sleep 1
  done

  if [[ -n "${app_pid}" ]]; then
    echo "Logcat martxan PID ${app_pid} prozesurako. Gelditzeko: Ctrl+C"
    echo "(ILogger mezuak: adb logcat-k 'Kirokuu' etiketa erabiltzen du; adib: adb logcat -s Kirokuu:I monodroid:D *:S)"
    adb -s "${emulator_serial}" logcat --pid="${app_pid}" -v color
  else
    echo "Ez da aurkitu aplikazioaren PIDa. Logcat orokorra irekitzen. Gelditzeko: Ctrl+C" >&2
    adb -s "${emulator_serial}" logcat -v color
  fi
fi

echo "Eginda. Izenburua: Kirokuu (launcher-ean bilatu behar baduzu)."
