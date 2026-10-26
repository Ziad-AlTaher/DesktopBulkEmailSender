# ==============================================================================
# Build & Package Script: Desktop Bulk Email Sender
# 1. Publishes standalone .NET 8 self-contained Windows x64 binary to ./publish
# 2. Compiles installer_setup.iss using Inno Setup (ISCC.exe) if available
# ==============================================================================

$ErrorActionPreference = "Stop"

Write-Host "=================================================" -ForegroundColor Cyan
Write-Host " Step 1: Publishing .NET 8 WinForms Application  " -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan

# Publish self-contained, single-file executable to ./publish
dotnet publish DesktopBulkEmailSender/DesktopBulkEmailSender.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:EnableCompressionInSingleFile=true `
    -o ./publish

if (-not (Test-Path "./publish/DesktopBulkEmailSender.exe")) {
    Write-Error "Publish failed: DesktopBulkEmailSender.exe was not found in ./publish"
    exit 1
}

Write-Host "`n✔ Published successfully to ./publish/DesktopBulkEmailSender.exe`n" -ForegroundColor Green

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
    & $isccPath "installer_setup.iss"
    Write-Host "`n✔ Installer generated in ./installer_output/" -ForegroundColor Green
} else {
    Write-Host "Inno Setup Compiler (ISCC.exe) was not found in common paths." -ForegroundColor Yellow
    Write-Host "You can open 'installer_setup.iss' directly in Inno Setup Studio and click 'Build > Compile' (Ctrl+F9)." -ForegroundColor Yellow
}
