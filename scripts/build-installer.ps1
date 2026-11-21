param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$InnoSetupCompiler = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "src\ElenasBusinessLedger.App\ElenasBusinessLedger.App.csproj"
$innoScriptPath = Join-Path $repoRoot "installer\ElenasBusinessLedger.iss"
$publishDir = Join-Path $repoRoot "artifacts\publish\ElenasBusinessLedger\$Runtime"
$installerDir = Join-Path $repoRoot "artifacts\installer"
$installerFileName = "ElenasBusinessLedger-1.31.0-Setup.exe"
$installerPath = Join-Path $installerDir $installerFileName

if (-not (Test-Path -LiteralPath $InnoSetupCompiler)) {
    throw "Inno Setup compiler not found at '$InnoSetupCompiler'. Install Inno Setup 6 or pass -InnoSetupCompiler."
}

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "Application project not found at '$projectPath'."
}

if (-not (Test-Path -LiteralPath $innoScriptPath)) {
    throw "Inno Setup script not found at '$innoScriptPath'."
}

if (Test-Path -LiteralPath $publishDir) {
    Remove-Item -LiteralPath $publishDir -Recurse -Force
}

if (Test-Path -LiteralPath $installerDir) {
    Remove-Item -LiteralPath $installerDir -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $publishDir, $installerDir | Out-Null

Write-Host "Publishing $projectPath..."
& dotnet publish $projectPath `
    --configuration $Configuration `
    --framework net11.0-windows `
    --runtime $Runtime `
    --self-contained true `
    --output $publishDir `
    -p:PublishSingleFile=false
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

Write-Host "Building installer with Inno Setup..."
& $InnoSetupCompiler `
    "/DPublishDir=$publishDir" `
    "/DOutputDir=$installerDir" `
    $innoScriptPath
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup compiler failed with exit code $LASTEXITCODE."
}

if (-not (Test-Path -LiteralPath $installerPath)) {
    throw "Expected installer was not created at '$installerPath'."
}

$installer = Get-Item -LiteralPath $installerPath
if ($installer.Length -le 0) {
    throw "Installer was created at '$installerPath' but is empty."
}

Write-Host "Installer created: $installerPath"
