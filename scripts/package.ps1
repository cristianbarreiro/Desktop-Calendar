[CmdletBinding()]
param (
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$Clean,
    [switch]$SkipTests,
    [Alias("SkipFormatCheck")]
    [switch]$SkipFormat,
    [switch]$SkipInstaller,
    [switch]$SkipFrameworkDependent,
    [switch]$SkipSelfContained,
    [switch]$SkipChecksums
)

$ErrorActionPreference = "Stop"
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$Solution = Join-Path $RepoRoot "CalendarWidget.slnx"
$PropsFile = Join-Path $RepoRoot "Directory.Build.props"
$AppProject = Join-Path $RepoRoot "src\CalendarWidget.App\CalendarWidget.App.csproj"
$InstallerScript = Join-Path $RepoRoot "installer\setup.iss"
$ArtifactsDir = Join-Path $RepoRoot "artifacts"
$PublishRoot = Join-Path $ArtifactsDir "publish"
$PublishFwDir = Join-Path $PublishRoot "framework-dependent"
$PublishScDir = Join-Path $PublishRoot "self-contained"
$InstallerDir = Join-Path $ArtifactsDir "installer"
$ReleaseDir = Join-Path $ArtifactsDir "release"
$Stage = "environment verification"
$Results = [ordered]@{
    Restore = "NOT RUN"
    Build = "NOT RUN"
    Tests = if ($SkipTests) { "SKIPPED" } else { "NOT RUN" }
    Format = if ($SkipFormat) { "SKIPPED" } else { "NOT RUN" }
    FrameworkDependent = if ($SkipFrameworkDependent) { "SKIPPED" } else { "NOT RUN" }
    SelfContained = if ($SkipSelfContained) { "SKIPPED" } else { "NOT RUN" }
    Installer = if ($SkipInstaller) { "SKIPPED" } else { "NOT RUN" }
    Checksums = if ($SkipChecksums) { "SKIPPED" } else { "NOT RUN" }
    Manifest = "NOT RUN"
}

function Invoke-External {
    param([string]$Name, [string]$Command, [string[]]$Arguments)

    Write-Host "`n--- $Name ---" -ForegroundColor Yellow
    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Name failed with exit code $LASTEXITCODE."
    }
}

function Assert-File {
    param([string]$Path, [string]$Description)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "$Description was not produced: $Path"
    }
}

function Remove-GeneratedDirectory {
    param([string]$Path)

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $artifactRoot = [System.IO.Path]::GetFullPath($ArtifactsDir).TrimEnd('\') + '\'
    if (-not $fullPath.StartsWith($artifactRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove a path outside generated artifacts: $fullPath"
    }

    if (Test-Path -LiteralPath $fullPath) {
        Remove-Item -LiteralPath $fullPath -Recurse -Force
    }
}

function Write-Archive {
    param([string]$SourceDirectory, [string]$DestinationPath)

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    if (Test-Path -LiteralPath $DestinationPath) {
        Remove-Item -LiteralPath $DestinationPath -Force
    }

    [System.IO.Compression.ZipFile]::CreateFromDirectory(
        $SourceDirectory,
        $DestinationPath,
        [System.IO.Compression.CompressionLevel]::Optimal,
        $false)

    $archive = [System.IO.Compression.ZipFile]::OpenRead($DestinationPath)
    try {
        if (-not ($archive.Entries | Where-Object { $_.FullName -eq "DesktopCalendar.exe" })) {
            throw "Archive does not contain DesktopCalendar.exe at its root: $DestinationPath"
        }
    }
    finally {
        $archive.Dispose()
    }
}

function Find-Iscc {
    $candidates = @(
        (Get-Command ISCC.exe -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source -ErrorAction SilentlyContinue),
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
        "C:\Program Files\Inno Setup 6\ISCC.exe",
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
    )

    foreach ($candidate in $candidates) {
        if ($candidate -and (Test-Path -LiteralPath $candidate -PathType Leaf)) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
    }

    return $null
}

try {
    $Stage = "environment verification"
    $dotnet = Get-Command dotnet -ErrorAction Stop
    foreach ($requiredFile in @($Solution, $PropsFile, $AppProject)) {
        Assert-File -Path $requiredFile -Description "Required project file"
    }
    if (-not $SkipInstaller) {
        Assert-File -Path $InstallerScript -Description "Inno Setup script"
        $IsccPath = Find-Iscc
        if (-not $IsccPath) {
            throw "ISCC.exe was not found. Install Inno Setup 6 or rerun explicitly with -SkipInstaller. No installer will be reported as built."
        }
    }

    [xml]$propsXml = Get-Content -LiteralPath $PropsFile -Raw
    $versionNode = $propsXml.SelectSingleNode("/Project/PropertyGroup/Version")
    if (-not $versionNode -or [string]::IsNullOrWhiteSpace($versionNode.InnerText)) {
        throw "Version is missing from Directory.Build.props."
    }
    $Version = $versionNode.InnerText.Trim()

    $sdkOutput = & $dotnet.Source --version
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to determine the installed .NET SDK version."
    }
    if ([version]$sdkOutput -lt [version]"10.0.100") {
        throw "The project requires the .NET 10 SDK; found $sdkOutput."
    }
    if ($Runtime -ne "win-x64") {
        throw "This project and its Inno Setup installer currently support win-x64 only; received '$Runtime'."
    }

    $gitSha = (& git -C $RepoRoot rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0 -or -not $gitSha) {
        throw "Unable to read the current Git commit SHA."
    }
    $gitChanges = @(& git -C $RepoRoot status --porcelain)
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to read Git working tree state."
    }
    $workingTreeState = if ($gitChanges.Count -eq 0) { "clean" } else { "modified" }

    Write-Host "Version:       $Version" -ForegroundColor Green
    Write-Host "Configuration: $Configuration" -ForegroundColor Green
    Write-Host "Runtime:       $Runtime" -ForegroundColor Green
    Write-Host ".NET SDK:      $sdkOutput" -ForegroundColor Green
    Write-Host "Commit:        $gitSha ($workingTreeState working tree)" -ForegroundColor Green
    Write-Host "Build outputs: $ArtifactsDir (user data is not in this directory)" -ForegroundColor Green

    $Stage = "clean generated outputs"
    if ($Clean) {
        Write-Host "`n--- Removing generated build outputs ---" -ForegroundColor Yellow
        foreach ($projectDirectory in @("src", "tests")) {
            $basePath = Join-Path $RepoRoot $projectDirectory
            if (Test-Path -LiteralPath $basePath) {
                Get-ChildItem -LiteralPath $basePath -Directory -Recurse -Force |
                    Where-Object { $_.Name -in @("bin", "obj") } |
                    Sort-Object { $_.FullName.Length } -Descending |
                    ForEach-Object { Remove-Item -LiteralPath $_.FullName -Recurse -Force }
            }
        }
    }

    $Stage = "clear prior pipeline artifacts"
    foreach ($generatedPath in @($PublishRoot, $InstallerDir, $ReleaseDir)) {
        Remove-GeneratedDirectory -Path $generatedPath
    }
    $rootChecksums = Join-Path $ArtifactsDir "SHA256SUMS.txt"
    if (Test-Path -LiteralPath $rootChecksums -PathType Leaf) {
        Remove-Item -LiteralPath $rootChecksums -Force
    }
    New-Item -ItemType Directory -Force -Path $PublishFwDir, $PublishScDir, $InstallerDir, $ReleaseDir | Out-Null

    $Stage = "restore"
    Invoke-External -Name "Restore dependencies" -Command $dotnet.Source -Arguments @("restore", $Solution)
    $Results.Restore = "PASS"

    $Stage = "build"
    Invoke-External -Name "Build solution ($Configuration)" -Command $dotnet.Source -Arguments @("build", $Solution, "-c", $Configuration, "--no-restore")
    $Results.Build = "PASS"

    if (-not $SkipTests) {
        $Stage = "tests"
        Invoke-External -Name "Run tests ($Configuration)" -Command $dotnet.Source -Arguments @("test", $Solution, "-c", $Configuration, "--no-build", "--verbosity", "normal")
        $Results.Tests = "PASS"
    }

    if (-not $SkipFormat) {
        $Stage = "format verification"
        Invoke-External -Name "Verify formatting" -Command $dotnet.Source -Arguments @("format", $Solution, "--verify-no-changes")
        $Results.Format = "PASS"
    }

    $frameworkZip = Join-Path $ReleaseDir "DesktopCalendar-$Version-$Runtime-framework-dependent.zip"
    if (-not $SkipFrameworkDependent) {
        $Stage = "framework-dependent publish"
        Invoke-External -Name "Publish framework-dependent ($Runtime)" -Command $dotnet.Source -Arguments @(
            "publish", $AppProject, "-c", $Configuration, "-r", $Runtime, "--self-contained", "false", "-o", $PublishFwDir)
        Assert-File -Path (Join-Path $PublishFwDir "DesktopCalendar.exe") -Description "Framework-dependent executable"
        Write-Archive -SourceDirectory $PublishFwDir -DestinationPath $frameworkZip
        $Results.FrameworkDependent = "PASS"
    }

    $selfContainedZip = Join-Path $ReleaseDir "DesktopCalendar-$Version-$Runtime-self-contained.zip"
    if (-not $SkipSelfContained) {
        $Stage = "self-contained publish"
        Invoke-External -Name "Publish self-contained ($Runtime)" -Command $dotnet.Source -Arguments @(
            "publish", $AppProject, "-c", $Configuration, "-r", $Runtime, "--self-contained", "true", "-o", $PublishScDir)
        Assert-File -Path (Join-Path $PublishScDir "DesktopCalendar.exe") -Description "Self-contained executable"
        foreach ($runtimeComponent in @("coreclr.dll", "hostfxr.dll")) {
            Assert-File -Path (Join-Path $PublishScDir $runtimeComponent) -Description "Self-contained runtime component"
        }
        Write-Archive -SourceDirectory $PublishScDir -DestinationPath $selfContainedZip
        $Results.SelfContained = "PASS"
    }

    if (-not $SkipInstaller) {
        if ($SkipSelfContained) {
            throw "The installer consumes the self-contained publish. Remove -SkipSelfContained or explicitly use -SkipInstaller."
        }
        $Stage = "Inno Setup installer"
        Invoke-External -Name "Compile installer" -Command $IsccPath -Arguments @("/DMyAppVersion=$Version", $InstallerScript)
        $builtSetup = Join-Path $InstallerDir "DesktopCalendar-$Version-$Runtime-setup.exe"
        Assert-File -Path $builtSetup -Description "Inno Setup installer"
        Copy-Item -LiteralPath $builtSetup -Destination (Join-Path $ReleaseDir "DesktopCalendar-$Version-$Runtime-setup.exe") -Force
        $Results.Installer = "PASS"
    }

    if (-not $SkipChecksums) {
        $Stage = "checksums"
        $releaseFiles = Get-ChildItem -LiteralPath $ReleaseDir -File | Sort-Object Name
        if ($releaseFiles.Count -eq 0) {
            throw "No release artifacts exist to checksum."
        }
        $checksumPath = Join-Path $ReleaseDir "SHA256SUMS.txt"
        $checksumLines = foreach ($file in $releaseFiles) {
            "{0}  {1}" -f (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant(), $file.Name
        }
        Set-Content -LiteralPath $checksumPath -Value $checksumLines -Encoding utf8
        Copy-Item -LiteralPath $checksumPath -Destination (Join-Path $ArtifactsDir "SHA256SUMS.txt") -Force
        $Results.Checksums = "PASS"
    }

    $Stage = "manifest"
    $manifestPath = Join-Path $ReleaseDir "BUILD-MANIFEST.json"
    $manifestArtifacts = foreach ($file in (Get-ChildItem -LiteralPath $ReleaseDir -File | Where-Object { $_.Name -ne "BUILD-MANIFEST.json" -and $_.Name -ne "SHA256SUMS.txt" } | Sort-Object Name)) {
        [ordered]@{
            Name = $file.Name
            SizeBytes = $file.Length
            SHA256 = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        }
    }
    [ordered]@{
        Version = $Version
        Configuration = $Configuration
        Runtime = $Runtime
        Commit = $gitSha
        WorkingTree = $workingTreeState
        BuildTimestampUtc = [DateTime]::UtcNow.ToString("o")
        Artifacts = @($manifestArtifacts)
        ChecksumsGenerated = (-not $SkipChecksums)
        UserDataPath = "%LOCALAPPDATA%\DesktopCalendar"
        UserDataModifiedByBuild = $false
    } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $manifestPath -Encoding utf8
    $Results.Manifest = "PASS"

    $Stage = "artifact validation"
    if (-not $SkipFrameworkDependent) { Assert-File -Path $frameworkZip -Description "Framework-dependent ZIP" }
    if (-not $SkipSelfContained) { Assert-File -Path $selfContainedZip -Description "Self-contained ZIP" }
    if (-not $SkipInstaller) { Assert-File -Path (Join-Path $ReleaseDir "DesktopCalendar-$Version-$Runtime-setup.exe") -Description "Installer artifact" }
    if (-not $SkipChecksums) { Assert-File -Path (Join-Path $ReleaseDir "SHA256SUMS.txt") -Description "SHA256SUMS" }
    Assert-File -Path $manifestPath -Description "Build manifest"

    Write-Host "`n========================================================" -ForegroundColor Cyan
    Write-Host " Desktop Calendar — Build Summary" -ForegroundColor Cyan
    Write-Host "========================================================" -ForegroundColor Cyan
    Write-Host "Version:       $Version"
    Write-Host "Configuration: $Configuration"
    Write-Host "Runtime:       $Runtime"
    Write-Host "Commit:        $gitSha ($workingTreeState)"
    foreach ($entry in $Results.GetEnumerator()) {
        Write-Host ("{0,-22} {1}" -f ($entry.Key + ":"), $entry.Value)
    }
    Write-Host "Output:        $ReleaseDir"
    Write-Host "User data:     %LOCALAPPDATA%\DesktopCalendar (not cleaned or modified by this script)"
    Write-Host "========================================================" -ForegroundColor Cyan
}
catch {
    Write-Host "`n[FAILED] $Stage`: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "No later pipeline stage was run." -ForegroundColor Red
    throw
}
