[CmdletBinding()]
param (
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$Clean,
    [switch]$SkipTests,
    [switch]$SkipFormat,
    [switch]$SkipInstaller,
    [switch]$SkipFrameworkDependent,
    [switch]$SkipSelfContained,
    [switch]$SkipChecksums
)

$ErrorActionPreference = "Stop"
$Pipeline = Join-Path $PSScriptRoot "package.ps1"
if (-not (Test-Path -LiteralPath $Pipeline -PathType Leaf)) {
    throw "Packaging pipeline not found: $Pipeline"
}

$pipelineParameters = @{}
foreach ($parameter in $PSBoundParameters.GetEnumerator()) {
    $pipelineParameters[$parameter.Key] = $parameter.Value
}

& $Pipeline @pipelineParameters
