# Kirokuu - MAUI Android deploy Windows-erako
# Eraikitzen du proiektua eta hautatutako emuladore edo gailu batean exekutatzen du.
# Erabilera: .\abiarazi-windows.ps1 [-Gailu <avd-izena>]
param(
    [string]$Gailu = ""
)

$ErrorActionPreference = "Stop"

$ANDROID_PACKAGE_ID = "com.companyname.kirokuu"
$SCRIPT_DIR   = Split-Path -Parent $MyInvocation.MyCommand.Path
$KIROKUU_ROOT = Split-Path -Parent $SCRIPT_DIR
$CSPROJ       = Join-Path $KIROKUU_ROOT "Kirokuu\Kirokuu.csproj"
$TFM          = "net9.0-android"
$OBJ_ANDROID  = Join-Path $KIROKUU_ROOT "Kirokuu\obj\Debug\$TFM"

# --- Android SDK auto-detekzio ---

$sdkCandidates = @(
    $env:ANDROID_SDK_ROOT,
    $env:ANDROID_HOME,
    "$env:LOCALAPPDATA\Android\Sdk",
    "$env:USERPROFILE\AppData\Local\Android\Sdk"
) | Where-Object { $_ -and (Test-Path $_) }

if (-not $sdkCandidates) {
    Write-Error "Ez da aurkitu Android SDK. ANDROID_SDK_ROOT ezarri."
}

$ANDROID_SDK_ROOT = $sdkCandidates[0]
$env:PATH = "$ANDROID_SDK_ROOT\platform-tools;$ANDROID_SDK_ROOT\emulator;$env:PATH"
$env:ANDROID_SDK_ROOT = $ANDROID_SDK_ROOT

$adb      = "$ANDROID_SDK_ROOT\platform-tools\adb.exe"
$emulExe  = "$ANDROID_SDK_ROOT\emulator\emulator.exe"

if (-not (Test-Path $adb))     { Write-Error "adb.exe ez da aurkitu: $adb" }
if (-not (Test-Path $emulExe)) { Write-Error "emulator.exe ez da aurkitu: $emulExe" }
if (-not (Test-Path $CSPROJ))  { Write-Error "Proiektua ez da aurkitu: $CSPROJ" }

# --- Gailu / AVD hautaketa ---

Write-Host ""
Write-Host "=== KIROKUU - Android Deploy ===" -ForegroundColor Cyan
Write-Host ""

# Dagoeneko konektatutako gailuak (device state-an daudenak)
$rawDevices = & $adb devices 2>$null
$connectedDevices = @()
foreach ($line in $rawDevices) {
    if ($line -match '^(\S+)\s+device$' -and $line -notmatch '^List') {
        $connectedDevices += $Matches[1]
    }
}

# Eskuragarri dauden AVDak
$avdList = @(& $emulExe -list-avds 2>$null | Where-Object { $_.Trim() -ne "" })

$options = @()

if ($connectedDevices.Count -gt 0) {
    Write-Host "Dagoeneko konektatutako gailuak:" -ForegroundColor Yellow
    foreach ($dev in $connectedDevices) {
        $idx = $options.Count + 1
        Write-Host "  [$idx] $dev (konektatua)"
        $options += [PSCustomObject]@{ Label = $dev; Type = "connected"; Serial = $dev }
    }
}

if ($avdList.Count -gt 0) {
    Write-Host ""
    Write-Host "Eskuragarri dauden AVDak:" -ForegroundColor Yellow
    foreach ($avd in $avdList) {
        $idx = $options.Count + 1
        Write-Host "  [$idx] $avd"
        $options += [PSCustomObject]@{ Label = $avd; Type = "avd"; Serial = "" }
    }
}

if ($options.Count -eq 0) {
    Write-Error "Ez da aurkitu gailu edo AVD eskuragarririk."
}

# Parametroz emanda badago (-Gailu), automatikoki hautatu
$selected = $null
if ($Gailu -ne "") {
    foreach ($opt in $options) {
        if ($opt.Label -eq $Gailu) { $selected = $opt; break }
    }
    if (-not $selected) {
        Write-Error "Ez da aurkitu '$Gailu' AVD/gailu izena zerrendan."
    }
    Write-Host ""
    Write-Host "Automatikoki hautatua: $($selected.Label)" -ForegroundColor Green
} else {
    Write-Host ""
    $choice = Read-Host "Zein erabili nahi duzu? (1-$($options.Count))"
    if (-not ($choice -match '^\d+$') -or [int]$choice -lt 1 -or [int]$choice -gt $options.Count) {
        Write-Error "Hautapen baliogabea: $choice"
    }
    $selected = $options[[int]$choice - 1]
    Write-Host ""
    Write-Host "Hautatua: $($selected.Label)" -ForegroundColor Green
}

# --- AVD abiarazi behar bada ---

$emulatorSerial = $selected.Serial

if ($selected.Type -eq "avd") {
    Write-Host "Emuladorea abiarazten: $($selected.Label) ..." -ForegroundColor Cyan
    Start-Process $emulExe -ArgumentList "-avd", $selected.Label, "-no-boot-anim" -NoNewWindow

    Write-Host "Gailua zain..."
    & $adb wait-for-device 2>$null

    Write-Host "Sistema abioa amaitzeko zain..."
    $timeout = 300
    $elapsed = 0
    do {
        Start-Sleep -Seconds 3
        $elapsed += 3
        $booted = (& $adb shell getprop sys.boot_completed 2>$null) -replace '\r', ''
        if ($elapsed -ge $timeout) {
            Write-Error "Denbora-muga: emuladorea ez da abiatu."
        }
    } while ($booted.Trim() -ne "1")

    # Serial aurkitu
    $rawAfter = & $adb devices 2>$null
    foreach ($line in $rawAfter) {
        if ($line -match '^(emulator-\d+)\s+device$') {
            $emulatorSerial = $Matches[1]
            break
        }
    }

    if (-not $emulatorSerial) {
        Write-Error "Ez da aurkitu emuladorearen seriala abiatu ostean."
    }

    Write-Host "Emuladorea prest: $emulatorSerial" -ForegroundColor Green
}

$env:ANDROID_SERIAL = $emulatorSerial

# --- Konpilatu eta deploy ---

Write-Host ""
Write-Host "Konpilazio-zerbitzariak ixten..." -ForegroundColor Cyan
dotnet build-server shutdown 2>$null

Write-Host "Garbitzen: $CSPROJ"
$env:MSBUILDDISABLENODEREUSE = "1"
dotnet clean $CSPROJ -f $TFM -v minimal

if (Test-Path $OBJ_ANDROID) {
    Write-Host "obj/$TFM ezabatzen..."
    Remove-Item -Recurse -Force $OBJ_ANDROID
}

Start-Sleep -Seconds 1

Write-Host ""
Write-Host "Eraikitzen eta instalatzen -> $emulatorSerial ..." -ForegroundColor Cyan
dotnet build $CSPROJ -t:Run -f $TFM -v minimal -maxcpucount:1 "-p:AdbTarget=-s $emulatorSerial"

# --- Egiaztatu instalazioa ---

$pkgList = & $adb -s $emulatorSerial shell pm list packages 2>$null
$installed = $pkgList | Where-Object { $_ -match [regex]::Escape($ANDROID_PACKAGE_ID) }

if (-not $installed) {
    Write-Host ""
    Write-Host "ERROREA: '$ANDROID_PACKAGE_ID' ez dago instalatuta." -ForegroundColor Red
    Write-Host "Android Studio edo beste dotnet build bat itxi eta saiatu berriro." -ForegroundColor Yellow
    exit 1
}

# --- Ireki aplikazioa ---
# PowerShell 5.1: $ErrorActionPreference "Stop" motan, komando natiboek stderr-era idazterakoan
# NativeCommandError jaurtitzen dute. Atal honetan "Continue" erabiltzen dugu.

Write-Host "Aplikazioa irekitzen..." -ForegroundColor Cyan
$prev = $ErrorActionPreference
$ErrorActionPreference = "Continue"
& $adb -s $emulatorSerial shell monkey -p $ANDROID_PACKAGE_ID -c android.intent.category.LAUNCHER 1 2>&1 | Out-Null
$ErrorActionPreference = $prev

# --- Logcat ---

Write-Host ""
Write-Host "Logcat abiatzen (Ctrl+C gelditzeko)..." -ForegroundColor Cyan
$ErrorActionPreference = "Continue"
& $adb -s $emulatorSerial logcat -c 2>&1 | Out-Null

$appPid = $null
for ($i = 0; $i -lt 20; $i++) {
    $rawPid = & $adb -s $emulatorSerial shell pidof $ANDROID_PACKAGE_ID 2>&1
    $appPid = ($rawPid -replace '\r', '').Trim()
    if ($appPid -match '^\d+') { break }
    Start-Sleep -Seconds 1
}

if ($appPid -match '^\d+') {
    Write-Host "Logcat PID $appPid -rentzat. Gelditzeko: Ctrl+C" -ForegroundColor Green
    & $adb -s $emulatorSerial logcat --pid=$appPid
} else {
    Write-Host "Ez da aurkitu PIDa. Logcat orokorra. Gelditzeko: Ctrl+C" -ForegroundColor Yellow
    & $adb -s $emulatorSerial logcat
}
