# ==============================================================================
# Build & Package Script: Desktop Bulk Email Sender
# 1. Publishes standalone .NET 8 self-contained Windows x64 binary to ./publish
# 2. Compiles installer_setup.iss using Inno Setup (ISCC.exe) if available
# ==============================================================================

$ErrorActionPreference = "Stop"

# Run from the script's folder so relative paths work wherever it is launched from
Set-Location -Path $PSScriptRoot

# Single source of truth for the version: <Version> in the csproj
$csprojPath = "DesktopBulkEmailSender/DesktopBulkEmailSender.csproj"
$appVersion = ([xml](Get-Content $csprojPath)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
if (-not $appVersion) {
    Write-Error "Could not read <Version> from $csprojPath"
    exit 1
}
Write-Host "Building version $appVersion" -ForegroundColor DarkCyan

Write-Host "=================================================" -ForegroundColor Cyan
Write-Host " Step 1: Publishing .NET 8 WinForms Application  " -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan

# Start from an empty ./publish so a failed build can never leave a stale exe behind
if (Test-Path "./publish") { Remove-Item "./publish" -Recurse -Force }

# Publish self-contained, single-file executable to ./publish
dotnet publish DesktopBulkEmailSender/DesktopBulkEmailSender.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:EnableCompressionInSingleFile=true `
    -o ./publish

# $ErrorActionPreference does not apply to native executables - check the exit code explicitly
if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

if (-not (Test-Path "./publish/DesktopBulkEmailSender.exe")) {
    Write-Error "Publish failed: DesktopBulkEmailSender.exe was not found in ./publish"
    exit 1
}

Write-Host "`n[OK] Published successfully to ./publish/DesktopBulkEmailSender.exe`n" -ForegroundColor Green

Write-Host "=================================================" -ForegroundColor Cyan
Write-Host " Step 2: Compiling Inno Setup Installer          " -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan

# Locate Inno Setup Compiler (ISCC.exe)
$isccCandidates = @(
    "iscc",
    "C:\Program Files\Inno Setup 7\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 7\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe",
    "${env:LOCALAPPDATA}\Programs\Inno Setup 7\ISCC.exe",
    "${env:LOCALAPPDATA}\Programs\Inno Setup 6\ISCC.exe"
)

$isccPath = $null
foreach ($path in $isccCandidates) {
    if (Get-Command $path -ErrorAction SilentlyContinue) {
        $isccPath = $path
        break
    }
}

if ($isccPath) {
    Write-Host "Found Inno Setup Compiler at: $isccPath" -ForegroundColor DarkCyan
    & $isccPath "/DMyAppVersion=$appVersion" "installer_setup.iss"
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Inno Setup compilation failed with exit code $LASTEXITCODE"
        exit $LASTEXITCODE
    }
    Write-Host "`n[OK] Installer generated in ./installer_output/" -ForegroundColor Green
} else {
    Write-Host "Inno Setup Compiler (ISCC.exe) was not found in common paths." -ForegroundColor Yellow
    Write-Host "You can open 'installer_setup.iss' directly in Inno Setup Studio and click 'Build > Compile' (Ctrl+F9)." -ForegroundColor Yellow
}
