$ErrorActionPreference = "Stop"
$env:JAVA_HOME = "C:\Program Files\Android\Android Studio\jbr"
$env:ANDROID_HOME = "$env:LOCALAPPDATA\Android\Sdk"
$env:ANDROID_SDK_ROOT = $env:ANDROID_HOME
$env:PATH = "$env:JAVA_HOME\bin;$env:PATH"

$root = "Z:\Tolosa\Kiroku"
$csproj = "$root\Kirokuu\Kirokuu\Kirokuu.csproj"
$tfm = "net9.0-android"

function Find-Apk($dir) {
  if (-not (Test-Path $dir)) { return $null }
  Get-ChildItem $dir -Filter *.apk -ErrorAction SilentlyContinue |
    Sort-Object Length -Descending | Select-Object -First 1 -ExpandProperty FullName
}

Write-Host "===== UNIVERSAL ====="
dotnet publish $csproj -f $tfm -c Release -p:AndroidPackageFormats=apk -p:AndroidSdkBuildToolsVersion=35.0.0 -p:RunAOTCompilation=false -p:EnableLLVM=false -v minimal
if ($LASTEXITCODE -ne 0) { throw "Universal build fallo (exit $LASTEXITCODE)" }
$uni = Find-Apk "$root\Kirokuu\Kirokuu\bin\Release\$tfm\publish"
if (-not $uni) { $uni = Find-Apk "$root\Kirokuu\Kirokuu\bin\Release\$tfm" }
if (-not $uni) { throw "No se encontro APK universal" }
Copy-Item $uni "$root\Kiroku.apk" -Force
Write-Host "Universal -> $root\Kiroku.apk  ($([math]::Round((Get-Item $uni).Length/1MB,1)) MB)"

Write-Host "===== OPPO arm64 ====="
dotnet publish $csproj -f $tfm -c Release -p:AndroidPackageFormats=apk -p:AndroidSdkBuildToolsVersion=35.0.0 -p:RuntimeIdentifier=android-arm64 -p:RunAOTCompilation=false -p:EnableLLVM=false -v minimal
if ($LASTEXITCODE -ne 0) { throw "arm64 build fallo (exit $LASTEXITCODE)" }
$arm = Find-Apk "$root\Kirokuu\Kirokuu\bin\Release\$tfm\android-arm64\publish"
if (-not $arm) { $arm = Find-Apk "$root\Kirokuu\Kirokuu\bin\Release\$tfm\android-arm64" }
if (-not $arm) { throw "No se encontro APK arm64" }
Copy-Item $arm "$root\Kiroku-oppo-arm64.apk" -Force
Write-Host "Oppo arm64 -> $root\Kiroku-oppo-arm64.apk  ($([math]::Round((Get-Item $arm).Length/1MB,1)) MB)"

Write-Host "===== LISTO ====="
