[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('EditMode', 'PlayMode')]
    [string]$Platform
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$unityPath = 'C:\Program Files\Unity\Hub\Editor\6000.5.3f1\Editor\Unity.exe'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$platformName = $Platform.ToLowerInvariant()
$logsDirectory = Join-Path $projectRoot 'artifacts\logs'
$resultsDirectory = Join-Path $projectRoot 'artifacts\test-results'
$logPath = Join-Path $logsDirectory "$platformName.log"
$resultsPath = Join-Path $resultsDirectory "$platformName.xml"

if (-not (Test-Path -LiteralPath $unityPath -PathType Leaf)) {
    throw "Unity executable was not found at '$unityPath'."
}

New-Item -ItemType Directory -Force -Path $logsDirectory, $resultsDirectory | Out-Null
Remove-Item -LiteralPath $logPath, $resultsPath -Force -ErrorAction SilentlyContinue

$unityArguments = @(
    '-batchmode',
    '-nographics',
    '-projectPath', "`"$projectRoot`"",
    '-runTests',
    '-testPlatform', $Platform,
    '-testResults', "`"$resultsPath`"",
    '-logFile', "`"$logPath`""
)

$process = Start-Process -FilePath $unityPath -ArgumentList $unityArguments -WindowStyle Hidden -Wait -PassThru

if (-not (Test-Path -LiteralPath $logPath -PathType Leaf)) {
    throw "Unity did not create the expected log '$logPath'."
}

$logText = Get-Content -LiteralPath $logPath -Raw
$failurePattern = 'error CS\d+|Compilation failed|Unhandled Exception'
$logFailure = [regex]::Match($logText, $failurePattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($logFailure.Success) {
    throw "Unity log contains failure marker '$($logFailure.Value)'. See '$logPath'."
}

if (-not (Test-Path -LiteralPath $resultsPath -PathType Leaf)) {
    throw "Unity did not create the expected test result '$resultsPath'. Exit code: $($process.ExitCode)."
}

[xml]$results = Get-Content -LiteralPath $resultsPath -Raw
$testRun = $results.'test-run'
if ($null -eq $testRun) {
    throw "Test result '$resultsPath' does not contain an NUnit test-run root."
}

$failureValue = $testRun.GetAttribute('failures')
if ([string]::IsNullOrWhiteSpace($failureValue)) {
    $failureValue = $testRun.GetAttribute('failed')
}

$failures = 0
if (-not [string]::IsNullOrWhiteSpace($failureValue)) {
    $failures = [int]$failureValue
}

if ($failures -gt 0 -or $testRun.GetAttribute('result') -eq 'Failed') {
    throw "$Platform tests reported $failures failure(s). See '$resultsPath'."
}

if ($process.ExitCode -ne 0) {
    throw "Unity exited with code $($process.ExitCode). See '$logPath'."
}

$total = $testRun.GetAttribute('total')
$passed = $testRun.GetAttribute('passed')
Write-Output "$Platform tests passed ($passed/$total). Results: $resultsPath"
