[CmdletBinding()]
param(
    # The development build stays the default: node tools/smoke-webgl.mjs and
    # the Node contract tests depend on its output shape.
    [switch]$Release
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$unityPath = 'C:\Program Files\Unity\Hub\Editor\6000.5.3f1\Editor\Unity.exe'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$logsDirectory = Join-Path $projectRoot 'artifacts\logs'
$flavour = if ($Release) { 'release' } else { 'development' }
$logName = if ($Release) { 'webgl-build-release.log' } else { 'webgl-build.log' }
$logPath = Join-Path $logsDirectory $logName
$buildMethod = if ($Release) {
    'Agrovator.PitchSimulator.Editor.WebGlBuild.BuildRelease'
} else {
    'Agrovator.PitchSimulator.Editor.WebGlBuild.BuildDevelopment'
}
$indexPath = Join-Path $projectRoot 'Build\WebGL\index.html'

if (-not (Test-Path -LiteralPath $unityPath -PathType Leaf)) {
    throw "Unity executable was not found at '$unityPath'."
}

New-Item -ItemType Directory -Force -Path $logsDirectory | Out-Null
Remove-Item -LiteralPath $logPath -Force -ErrorAction SilentlyContinue

$unityArguments = @(
    '-batchmode',
    '-nographics',
    '-quit',
    '-projectPath', "`"$projectRoot`"",
    '-executeMethod', $buildMethod,
    '-logFile', "`"$logPath`""
)

$process = Start-Process -FilePath $unityPath -ArgumentList $unityArguments -WindowStyle Hidden -Wait -PassThru

if (-not (Test-Path -LiteralPath $logPath -PathType Leaf)) {
    throw "Unity did not create the expected build log '$logPath'."
}

$logText = Get-Content -LiteralPath $logPath -Raw
$failurePattern = 'error CS\d+|Compilation failed|BuildFailedException|Build completed with a result of [''"]?Failed|Unhandled Exception|Aborting batchmode due to failure'
$logFailure = [regex]::Match($logText, $failurePattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($logFailure.Success) {
    throw "Unity build log contains failure marker '$($logFailure.Value)'. See '$logPath'."
}

if ($process.ExitCode -ne 0) {
    throw "Unity exited with code $($process.ExitCode). See '$logPath'."
}

if (-not (Test-Path -LiteralPath $indexPath -PathType Leaf)) {
    throw "WebGL build entry point is missing at '$indexPath'. See '$logPath'."
}

Write-Output "WebGL $flavour build created at '$(Split-Path -Parent $indexPath)'."
