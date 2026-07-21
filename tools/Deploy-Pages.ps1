<#
.SYNOPSIS
Builds the release WebGL player and publishes it to the gh-pages branch.

.DESCRIPTION
Publishing is the one irreversible step in this project, and the artifact that
gets published is whatever sits in Build/WebGL. Both `node tools/smoke-webgl.mjs`
and the Unity suites leave the ~92 MB development player there, so a deploy that
trusts the folder ships the wrong build to learners on Malaysian school wifi.

This script therefore builds the release player itself rather than trusting the
folder, verifies the result really is the compressed player, and refuses to
publish anything that fails those checks. Publishing is opt-in: without -Push it
stops after verification and reports what it would have published.

.PARAMETER Push
Publish the verified build to the gh-pages branch. Without this the script
performs a dry run and never touches the remote.

.PARAMETER SkipBuild
Verify and publish the existing Build/WebGL output instead of rebuilding. The
verification still runs, so a development build is still rejected; use this only
to retry a publish after a build you have already verified.
#>
[CmdletBinding()]
param(
    [switch]$Push,
    [switch]$SkipBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$buildRoot = Join-Path $projectRoot 'Build\WebGL'
$indexPath = Join-Path $buildRoot 'index.html'
$publishBranch = 'gh-pages'

if (-not $SkipBuild) {
    Write-Output 'Building the release WebGL player...'
    # A PowerShell script invoked with & does not set $LASTEXITCODE, and under
    # StrictMode reading it is itself a terminating error. Build-WebGL.ps1
    # throws on any failure, so catching is both correct and sufficient.
    try {
        & (Join-Path $PSScriptRoot 'Build-WebGL.ps1') -Release
    }
    catch {
        throw "The release build failed: $_ Nothing was published."
    }
}

# --- Verification. Every check below throws, because a warning that scrolls
# --- past is not a guard when the next step is irreversible.

if (-not (Test-Path -LiteralPath $indexPath -PathType Leaf)) {
    throw "No build entry point at '$indexPath'. Nothing was published."
}

$compressed = @(Get-ChildItem -LiteralPath (Join-Path $buildRoot 'Build') -File -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -like '*.unityweb' })
if ($compressed.Count -lt 3) {
    $found = (Get-ChildItem -LiteralPath (Join-Path $buildRoot 'Build') -File -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty Name) -join ', '
    throw ("Build/WebGL does not contain the compressed release player " +
        "(expected .unityweb data, framework and wasm; found: $found). " +
        'This looks like a development build. Nothing was published.')
}

$indexText = Get-Content -LiteralPath $indexPath -Raw
if ($indexText -notmatch '\?v=[0-9a-f]{6,}') {
    throw ('The build entry point carries no cache-busting stamp. A returning ' +
        'learner would replay a cached build against a fresh framework. Nothing was published.')
}
if ($indexText -notmatch '\.unityweb\?v=') {
    throw ('The stamped URLs do not reference the compressed assets, so the ' +
        'stamp does not match what the loader downloads. Nothing was published.')
}

$totalBytes = (Get-ChildItem -LiteralPath $buildRoot -Recurse -File | Measure-Object -Property Length -Sum).Sum
if ($totalBytes -gt 40MB) {
    throw ("Build/WebGL totals $([math]::Round($totalBytes / 1MB, 2)) MB, which is far " +
        'larger than a release player should be. Refusing to publish a build this size.')
}

Write-Output ("Verified release build: $([math]::Round($totalBytes / 1MB, 2)) MB total, " +
    "$($compressed.Count) compressed assets.")

if (-not $Push) {
    Write-Output ''
    Write-Output 'Dry run. Nothing was published.'
    Write-Output "Re-run with -Push to publish this build to '$publishBranch'."
    return
}

# --- Publish. Reached only with an explicit -Push and a verified build.

Push-Location $projectRoot
try {
    $status = git status --porcelain
    if ($status) {
        throw ('The working tree has uncommitted changes. Commit or stash them so the ' +
            'published build is traceable to a commit. Nothing was published.')
    }

    $sourceCommit = (git rev-parse --short HEAD).Trim()
    $worktreePath = Join-Path ([System.IO.Path]::GetTempPath()) "agrovator-pages-$([guid]::NewGuid().ToString('N'))"

    git fetch origin $publishBranch
    if ($LASTEXITCODE -ne 0) { throw "Could not fetch '$publishBranch'. Nothing was published." }

    git worktree add $worktreePath "origin/$publishBranch"
    if ($LASTEXITCODE -ne 0) { throw "Could not check out '$publishBranch'. Nothing was published." }

    try {
        $publishedBuild = Join-Path $worktreePath 'Build\WebGL'
        if (Test-Path -LiteralPath $publishedBuild) {
            Remove-Item -LiteralPath $publishedBuild -Recurse -Force
        }
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $publishedBuild) | Out-Null
        Copy-Item -LiteralPath $buildRoot -Destination $publishedBuild -Recurse

        Push-Location $worktreePath
        try {
            git add -A 'Build/WebGL'
            $pending = git status --porcelain
            if (-not $pending) {
                Write-Output 'The published build already matches this one. Nothing to publish.'
                return
            }

            git commit -m "deploy: release WebGL build from $sourceCommit"
            if ($LASTEXITCODE -ne 0) { throw 'Could not commit the published build.' }

            git push origin "HEAD:$publishBranch"
            if ($LASTEXITCODE -ne 0) { throw "Could not push to '$publishBranch'." }
            Write-Output "Published the release build from $sourceCommit to '$publishBranch'."
        }
        finally {
            Pop-Location
        }
    }
    finally {
        git worktree remove $worktreePath --force
    }
}
finally {
    Pop-Location
}
