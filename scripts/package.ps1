[CmdletBinding()]
param (
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$SkipTests,
    [switch]$SkipFormatCheck,
    [switch]$SkipInstaller
)

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $RepoRoot

Write-Host "=== Desktop Calendar Packaging & Release Pipeline ===" -ForegroundColor Cyan

# 1. Resolve Version from Directory.Build.props
$PropsFile = Join-Path $RepoRoot "Directory.Build.props"
if (-not (Test-Path $PropsFile)) {
    throw "Directory.Build.props not found at $PropsFile"
}

[xml]$PropsXml = Get-Content $PropsFile
$Version = $PropsXml.Project.PropertyGroup.Version
if (-not $Version) {
    $Version = "1.0.0"
}
Write-Host "Target Version: $Version" -ForegroundColor Green
Write-Host "Configuration:  $Configuration" -ForegroundColor Green
Write-Host "Runtime:        $Runtime" -ForegroundColor Green

# 2. Run Validation (Build, Test, Format) unless skipped
if (-not $SkipTests) {
    Write-Host "`n--- Restoring dependencies ---" -ForegroundColor Yellow
    dotnet restore CalendarWidget.slnx
    if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed" }

    Write-Host "`n--- Building solution ($Configuration) ---" -ForegroundColor Yellow
    dotnet build CalendarWidget.slnx -c $Configuration --no-restore
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed" }

    Write-Host "`n--- Running test suite ($Configuration) ---" -ForegroundColor Yellow
    dotnet test CalendarWidget.slnx -c $Configuration --no-build --verbosity normal
    if ($LASTEXITCODE -ne 0) { throw "dotnet test failed" }
}

if (-not $SkipFormatCheck) {
    Write-Host "`n--- Verifying code format ---" -ForegroundColor Yellow
    dotnet format CalendarWidget.slnx --verify-no-changes
    if ($LASTEXITCODE -ne 0) { throw "dotnet format check failed" }
}

# 3. Setup deterministic artifact directories
$ArtifactsDir = Join-Path $RepoRoot "artifacts"
$PublishFwDir = Join-Path $ArtifactsDir "publish\framework-dependent"
$PublishScDir = Join-Path $ArtifactsDir "publish\self-contained"
$InstallerDir = Join-Path $ArtifactsDir "installer"
$ReleaseDir   = Join-Path $ArtifactsDir "release"

if (Test-Path $ArtifactsDir) {
    Write-Host "`nCleaning existing artifacts directory..." -ForegroundColor Yellow
    Remove-Item $ArtifactsDir -Recurse -Force -ErrorAction SilentlyContinue
}

New-Item -ItemType Directory -Force -Path $PublishFwDir | Out-Null
New-Item -ItemType Directory -Force -Path $PublishScDir | Out-Null
New-Item -ItemType Directory -Force -Path $InstallerDir | Out-Null
New-Item -ItemType Directory -Force -Path $ReleaseDir   | Out-Null

$AppProject = Join-Path $RepoRoot "src\CalendarWidget.App\CalendarWidget.App.csproj"

# 4. Publish Framework-Dependent
Write-Host "`n--- Publishing Framework-Dependent ($Runtime) ---" -ForegroundColor Cyan
dotnet publish $AppProject `
    -c $Configuration `
    -r $Runtime `
    --self-contained false `
    -o $PublishFwDir
if ($LASTEXITCODE -ne 0) { throw "Framework-dependent publish failed" }

$FwZipName = "DesktopCalendar-$Version-$Runtime-framework-dependent.zip"
$FwZipPath = Join-Path $ReleaseDir $FwZipName
Write-Host "Creating archive: $FwZipName" -ForegroundColor Green
Compress-Archive -Path "$PublishFwDir\*" -DestinationPath $FwZipPath -Force

# 5. Publish Self-Contained
Write-Host "`n--- Publishing Self-Contained ($Runtime) ---" -ForegroundColor Cyan
dotnet publish $AppProject `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -o $PublishScDir
if ($LASTEXITCODE -ne 0) { throw "Self-contained publish failed" }

$ScZipName = "DesktopCalendar-$Version-$Runtime-self-contained.zip"
$ScZipPath = Join-Path $ReleaseDir $ScZipName
Write-Host "Creating archive: $ScZipName" -ForegroundColor Green
Compress-Archive -Path "$PublishScDir\*" -DestinationPath $ScZipPath -Force

# 6. Build Windows Installer (Inno Setup)
$InstallerProduced = $false
$SetupExeName = "DesktopCalendar-$Version-$Runtime-setup.exe"
$SetupExePath = Join-Path $ReleaseDir $SetupExeName

if (-not $SkipInstaller) {
    Write-Host "`n--- Building Windows Installer ---" -ForegroundColor Cyan

    $IsccCandidates = @(
        (Get-Command iscc.exe -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source -ErrorAction SilentlyContinue),
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
        "C:\Program Files\Inno Setup 6\ISCC.exe",
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
    )

    $IsccPath = $null
    foreach ($cand in $IsccCandidates) {
        if ($cand -and (Test-Path $cand)) {
            $IsccPath = $cand
            break
        }
    }

    if ($IsccPath) {
        Write-Host "Found Inno Setup compiler: $IsccPath" -ForegroundColor Green
        $IssScript = Join-Path $RepoRoot "installer\setup.iss"
        & $IsccPath "/DMyAppVersion=$Version" $IssScript
        if ($LASTEXITCODE -ne 0) { throw "Inno Setup compilation failed" }

        $BuiltSetup = Join-Path $InstallerDir $SetupExeName
        if (Test-Path $BuiltSetup) {
            Copy-Item -Path $BuiltSetup -Destination $SetupExePath -Force
            $InstallerProduced = $true
            Write-Host "Installer created: $SetupExeName" -ForegroundColor Green
        } else {
            throw "Installer output expected at $BuiltSetup but was not found"
        }
    } else {
        Write-Warning "Inno Setup (ISCC.exe) not found on system. Installer was not built."
    }
}

# 7. Generate SHA-256 Checksums
Write-Host "`n--- Generating Checksums (SHA-256) ---" -ForegroundColor Cyan
$ChecksumFileInRelease = Join-Path $ReleaseDir "SHA256SUMS.txt"
$ChecksumFileInRoot    = Join-Path $ArtifactsDir "SHA256SUMS.txt"

$ReleaseFiles = Get-ChildItem -Path $ReleaseDir -File | Where-Object { $_.Name -ne "SHA256SUMS.txt" }
$ChecksumLines = [System.Collections.Generic.List[string]]::new()

foreach ($file in $ReleaseFiles) {
    $hash = (Get-FileHash -Path $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    $line = "$hash  $($file.Name)"
    $ChecksumLines.Add($line)
    Write-Host "$line" -ForegroundColor Gray
}

$ChecksumLines | Set-Content -Path $ChecksumFileInRelease -Encoding utf8
$ChecksumLines | Set-Content -Path $ChecksumFileInRoot -Encoding utf8

Write-Host "`n=== Packaging Complete ===" -ForegroundColor Green
Write-Host "Release Artifacts directory: $ReleaseDir"
Get-ChildItem -Path $ReleaseDir | Select-Object Name, Length
