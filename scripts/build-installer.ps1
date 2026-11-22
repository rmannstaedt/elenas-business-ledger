param(
    [string]$Configuration = "Release",
    [ValidateSet("win-x64")]
    [string]$Runtime = "win-x64",
    [string]$InnoSetupCompiler = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
)

$ErrorActionPreference = "Stop"

function Assert-OwnedPath([string]$Root, [string]$Path) {
    $fullRoot = [IO.Path]::GetFullPath($Root).TrimEnd('\')
    $fullPath = [IO.Path]::GetFullPath($Path)
    if (!$fullPath.StartsWith($fullRoot + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw "Build path is outside its permitted directory: $fullPath"
    }

    $cursor = $fullPath
    while ($true) {
        $item = Get-Item -LiteralPath $cursor -Force -ErrorAction SilentlyContinue
        if ($null -ne $item -and ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            throw "The build path contains a redirected filesystem entry: $cursor"
        }
        if ($cursor.Equals($fullRoot, [StringComparison]::OrdinalIgnoreCase)) {
            break
        }
        $cursor = Split-Path -Parent $cursor
    }
}

$repoRoot = (Resolve-Path -LiteralPath (Split-Path -Parent $PSScriptRoot)).ProviderPath
$projectPath = Join-Path $repoRoot "src\ElenasBusinessLedger.App\ElenasBusinessLedger.App.csproj"
$innoScriptPath = Join-Path $repoRoot "installer\ElenasBusinessLedger.iss"
$buildRoot = Join-Path $repoRoot "artifacts\installer-builds"
$installerDir = Join-Path $repoRoot "artifacts\installer"
$installerFileName = "ElenasBusinessLedger-1.4.2-Setup.exe"
$installerPath = Join-Path $installerDir $installerFileName

Assert-OwnedPath $repoRoot $buildRoot
Assert-OwnedPath $repoRoot $installerPath

if (!(Test-Path -LiteralPath $InnoSetupCompiler -PathType Leaf)) {
    throw "Inno Setup compiler not found at '$InnoSetupCompiler'. Install Inno Setup 6 or pass -InnoSetupCompiler."
}
if (!(Test-Path -LiteralPath $projectPath -PathType Leaf)) {
    throw "Application project not found at '$projectPath'."
}
if (!(Test-Path -LiteralPath $innoScriptPath -PathType Leaf)) {
    throw "Inno Setup script not found at '$innoScriptPath'."
}

$stage = Join-Path $buildRoot ([Guid]::NewGuid().ToString("N"))
$publishDir = Join-Path $stage "publish"
$candidateDir = Join-Path $stage "candidate"
$candidatePath = Join-Path $candidateDir $installerFileName
Assert-OwnedPath $buildRoot $stage
if (Test-Path -LiteralPath $stage) {
    throw "An installer staging directory already exists at '$stage'."
}

try {
    New-Item -ItemType Directory -Force -Path $publishDir, $candidateDir | Out-Null
    Write-Host "Publishing $projectPath"
    # MSBuild inserts PublishDir into a quoted file transform, so pass a project-relative directory.
    $projectRelativePublishDir = Join-Path '..\..' $publishDir.Substring($repoRoot.Length + 1)
    & dotnet publish $projectPath --configuration $Configuration --framework net11.0-windows --runtime $Runtime --self-contained true "-p:PublishDir=$projectRelativePublishDir" -p:PublishSingleFile=false
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE."
    }

    Write-Host "Building installer with Inno Setup"
    & $InnoSetupCompiler "/DPublishDir=$publishDir" "/DOutputDir=$candidateDir" $innoScriptPath
    if ($LASTEXITCODE -ne 0) {
        throw "Inno Setup compiler failed with exit code $LASTEXITCODE."
    }

    Assert-OwnedPath $stage $candidatePath
    $candidate = Get-Item -LiteralPath $candidatePath -ErrorAction Stop
    if ($candidate.PSIsContainer -or $candidate.Length -le 0) {
        throw "The installer candidate is missing or empty."
    }

    Assert-OwnedPath $repoRoot $installerPath
    New-Item -ItemType Directory -Force -Path $installerDir | Out-Null
    if (Test-Path -LiteralPath $installerPath -PathType Leaf) {
        [IO.File]::Replace($candidatePath, $installerPath, (Join-Path $stage "previous.exe"))
    } else {
        [IO.File]::Move($candidatePath, $installerPath)
    }

    Write-Host "Installer created at $installerPath"
}
finally {
    if (Test-Path -LiteralPath $stage -PathType Container) {
        Assert-OwnedPath $buildRoot $stage
        $redirected = Get-ChildItem -LiteralPath $stage -Recurse -Force -Attributes ReparsePoint | Select-Object -First 1
        if ($null -ne $redirected) {
            Write-Warning "Staging cleanup was skipped because '$stage' contains a redirected entry."
        } else {
            Remove-Item -LiteralPath $stage -Recurse -Force
        }
    }
}
