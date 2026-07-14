[CmdletBinding()]
param(
    [ValidateRange(1024, 65535)]
    [int]$Port = 4173,
    [string]$Root,
    [switch]$SelfTest,
    [switch]$LogRequests
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$defaultProjectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$projectRoot = if ([string]::IsNullOrWhiteSpace($Root)) {
    $defaultProjectRoot
} else {
    (Resolve-Path -LiteralPath $Root).Path
}
if (-not [string]::Equals($projectRoot, $defaultProjectRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Root must resolve to this Pitch Simulator worktree: '$defaultProjectRoot'."
}

$mimeTypes = @{
    '.html'     = 'text/html; charset=utf-8'
    '.js'       = 'text/javascript; charset=utf-8'
    '.css'      = 'text/css; charset=utf-8'
    '.wasm'     = 'application/wasm'
    '.data'     = 'application/octet-stream'
    '.json'     = 'application/json; charset=utf-8'
    '.unityweb' = 'application/octet-stream'
    '.png'      = 'image/png'
    '.jpg'      = 'image/jpeg'
    '.jpeg'     = 'image/jpeg'
    '.gif'      = 'image/gif'
    '.svg'      = 'image/svg+xml'
    '.ico'      = 'image/x-icon'
    '.webp'     = 'image/webp'
    '.woff'     = 'font/woff'
    '.woff2'    = 'font/woff2'
    '.ttf'      = 'font/ttf'
    '.otf'      = 'font/otf'
}

function Get-FreeLoopbackPort {
    $probe = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
    try {
        $probe.Start()
        return ([System.Net.IPEndPoint]$probe.LocalEndpoint).Port
    } finally {
        $probe.Stop()
    }
}

function Resolve-ServedFile {
    param([Parameter(Mandatory)][string]$RawTarget)

    $question = $RawTarget.IndexOf('?')
    $rawPath = if ($question -ge 0) { $RawTarget.Substring(0, $question) } else { $RawTarget }
    if (-not $rawPath.StartsWith('/', [System.StringComparison]::Ordinal)) {
        return $null
    }

    try {
        $decoded = [System.Uri]::UnescapeDataString($rawPath)
    } catch {
        return $null
    }

    # No served filename needs encoded separators or a second decoding pass.
    if ($decoded.IndexOf([char]0) -ge 0 -or $decoded.Contains('\') -or
        $decoded -match '(?i)%(?:2e|2f|5c)') {
        return $null
    }

    $segments = $decoded.Split('/', [System.StringSplitOptions]::RemoveEmptyEntries)
    if ($segments | Where-Object { $_ -eq '.' -or $_ -eq '..' }) {
        return $null
    }

    if ($decoded -eq '/WebHarness' -or $decoded -eq '/WebHarness/') {
        $decoded = '/WebHarness/index.html'
    } elseif ($decoded -eq '/Build/WebGL' -or $decoded -eq '/Build/WebGL/') {
        $decoded = '/Build/WebGL/index.html'
    }

    if (-not ($decoded.StartsWith('/WebHarness/', [System.StringComparison]::Ordinal) -or
              $decoded.StartsWith('/Build/WebGL/', [System.StringComparison]::Ordinal))) {
        return $null
    }

    $relativePath = $decoded.TrimStart('/').Replace('/', [System.IO.Path]::DirectorySeparatorChar)
    $candidate = [System.IO.Path]::GetFullPath((Join-Path $projectRoot $relativePath))
    $rootPrefix = $projectRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    if (-not $candidate.StartsWith($rootPrefix, [System.StringComparison]::OrdinalIgnoreCase) -or
        -not (Test-Path -LiteralPath $candidate -PathType Leaf)) {
        return $null
    }

    $resolved = (Resolve-Path -LiteralPath $candidate).Path
    if (-not $resolved.StartsWith($rootPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $null
    }
    return $resolved
}

function Write-HttpResponse {
    param(
        [Parameter(Mandatory)][System.Net.Sockets.NetworkStream]$Stream,
        [Parameter(Mandatory)][int]$StatusCode,
        [Parameter(Mandatory)][string]$Reason,
        [Parameter(Mandatory)][string]$ContentType,
        [Parameter(Mandatory)][AllowEmptyCollection()][byte[]]$Body,
        [Parameter(Mandatory)][bool]$IncludeBody,
        [hashtable]$AdditionalHeaders = @{}
    )

    $headerLines = [System.Collections.Generic.List[string]]::new()
    $headerLines.Add("HTTP/1.1 $StatusCode $Reason")
    $headerLines.Add("Content-Type: $ContentType")
    $headerLines.Add("Content-Length: $($Body.Length)")
    $headerLines.Add('Cache-Control: no-store, max-age=0')
    $headerLines.Add('X-Content-Type-Options: nosniff')
    $headerLines.Add('Connection: close')
    foreach ($name in $AdditionalHeaders.Keys) {
        $headerLines.Add("${name}: $($AdditionalHeaders[$name])")
    }
    $headerBytes = [System.Text.Encoding]::ASCII.GetBytes(($headerLines -join "`r`n") + "`r`n`r`n")
    $Stream.Write($headerBytes, 0, $headerBytes.Length)
    if ($IncludeBody -and $Body.Length -gt 0) {
        $Stream.Write($Body, 0, $Body.Length)
    }
    $Stream.Flush()
}

function Invoke-Server {
    $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, $Port)
    $listener.Start()
    Write-Output "Pitch Simulator WebGL server ready at http://127.0.0.1:$Port/WebHarness/ (PID $PID)."
    try {
        while ($true) {
            $client = $listener.AcceptTcpClient()
            $stream = $null
            $reader = $null
            try {
                $client.ReceiveTimeout = 5000
                $client.SendTimeout = 5000
                $stream = $client.GetStream()
                $reader = [System.IO.StreamReader]::new($stream, [System.Text.Encoding]::ASCII, $false, 1024, $true)
                $requestLine = $reader.ReadLine()
                if ([string]::IsNullOrWhiteSpace($requestLine) -or $requestLine.Length -gt 4096) {
                    $body = [System.Text.Encoding]::UTF8.GetBytes('Bad Request')
                    Write-HttpResponse $stream 400 'Bad Request' 'text/plain; charset=utf-8' $body $true
                    continue
                }

                while ($true) {
                    $line = $reader.ReadLine()
                    if ($null -eq $line -or $line.Length -eq 0) { break }
                    if ($line.Length -gt 8192) { break }
                }

                $parts = $requestLine.Split(' ')
                if ($parts.Length -ne 3) {
                    $body = [System.Text.Encoding]::UTF8.GetBytes('Bad Request')
                    Write-HttpResponse $stream 400 'Bad Request' 'text/plain; charset=utf-8' $body $true
                    continue
                }

                $method = $parts[0].ToUpperInvariant()
                $target = $parts[1]
                $includeBody = $method -ne 'HEAD'
                if ($method -ne 'GET' -and $method -ne 'HEAD') {
                    $body = [System.Text.Encoding]::UTF8.GetBytes('Method Not Allowed')
                    Write-HttpResponse $stream 405 'Method Not Allowed' 'text/plain; charset=utf-8' $body $includeBody @{ Allow = 'GET, HEAD' }
                    if ($LogRequests) { Write-Output "$method [redacted-path] 405" }
                    continue
                }

                $targetPath = $target.Split('?')[0]
                if ($targetPath -eq '/favicon.ico') {
                    Write-HttpResponse $stream 204 'No Content' 'image/x-icon' ([byte[]]::new(0)) $false
                    if ($LogRequests) { Write-Output "$method /favicon.ico 204" }
                    continue
                }

                $filePath = Resolve-ServedFile $target
                if ($null -eq $filePath) {
                    $body = [System.Text.Encoding]::UTF8.GetBytes('Not Found')
                    Write-HttpResponse $stream 404 'Not Found' 'text/plain; charset=utf-8' $body $includeBody
                    if ($LogRequests) { Write-Output "$method [unserved-path] 404" }
                    continue
                }

                $extension = [System.IO.Path]::GetExtension($filePath).ToLowerInvariant()
                $contentType = if ($mimeTypes.ContainsKey($extension)) { $mimeTypes[$extension] } else { 'application/octet-stream' }
                $body = [System.IO.File]::ReadAllBytes($filePath)
                Write-HttpResponse $stream 200 'OK' $contentType $body $includeBody
                if ($LogRequests) {
                    $relative = $filePath.Substring($projectRoot.Length).Replace('\', '/')
                    Write-Output "$method $relative 200"
                }
            } catch [System.IO.IOException] {
                # A client may disconnect while probing readiness; keep the server alive.
            } finally {
                if ($null -ne $reader) { $reader.Dispose() }
                if ($null -ne $stream) { $stream.Dispose() }
                $client.Dispose()
            }
        }
    } finally {
        $listener.Stop()
        Write-Output 'Pitch Simulator WebGL server stopped.'
    }
}

function Invoke-RawHttpRequest {
    param(
        [Parameter(Mandatory)][int]$TargetPort,
        [Parameter(Mandatory)][string]$Request
    )
    $client = [System.Net.Sockets.TcpClient]::new()
    try {
        $client.Connect([System.Net.IPAddress]::Loopback, $TargetPort)
        $stream = $client.GetStream()
        $bytes = [System.Text.Encoding]::ASCII.GetBytes($Request)
        $stream.Write($bytes, 0, $bytes.Length)
        $stream.Flush()
        $reader = [System.IO.StreamReader]::new($stream, [System.Text.Encoding]::ASCII)
        return $reader.ReadToEnd()
    } finally {
        $client.Dispose()
    }
}

function Assert-ServerResponse {
    param(
        [Parameter(Mandatory)][int]$TargetPort,
        [Parameter(Mandatory)][string]$Method,
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][int]$Status,
        [string]$ContentType,
        [switch]$ExpectNoBody
    )
    $response = Invoke-RawHttpRequest $TargetPort "$Method $Path HTTP/1.1`r`nHost: 127.0.0.1`r`nConnection: close`r`n`r`n"
    $separator = $response.IndexOf("`r`n`r`n", [System.StringComparison]::Ordinal)
    if ($separator -lt 0) { throw "No HTTP header terminator for $Method $Path." }
    $headers = $response.Substring(0, $separator)
    $body = $response.Substring($separator + 4)
    if (-not $headers.StartsWith("HTTP/1.1 $Status ", [System.StringComparison]::Ordinal)) {
        throw "Expected HTTP $Status for $Method $Path; received '$($headers.Split("`r`n")[0])'."
    }
    if ($ContentType -and $headers -notmatch "(?im)^Content-Type: $([regex]::Escape($ContentType))\r?$") {
        throw "Expected Content-Type '$ContentType' for $Path."
    }
    if ($headers -notmatch '(?im)^Content-Length: \d+\r?$') { throw "Missing Content-Length for $Path." }
    if ($headers -notmatch '(?im)^Cache-Control: no-store, max-age=0\r?$') { throw "Missing no-store cache policy for $Path." }
    if ($ExpectNoBody -and $body.Length -ne 0) { throw "HEAD $Path unexpectedly returned a body." }
}

function Invoke-ServerSelfTest {
    $requiredFiles = @(
        'WebHarness\index.html',
        'Build\WebGL\index.html',
        'Build\WebGL\Build\WebGL.loader.js',
        'Build\WebGL\Build\WebGL.data',
        'Build\WebGL\Build\WebGL.wasm'
    )
    foreach ($relative in $requiredFiles) {
        if (-not (Test-Path -LiteralPath (Join-Path $projectRoot $relative) -PathType Leaf)) {
            throw "Server self-test prerequisite is missing: '$relative'. Run tools/Build-WebGL.ps1 first."
        }
    }

    $testPort = Get-FreeLoopbackPort
    $stdout = Join-Path ([System.IO.Path]::GetTempPath()) "pitch-simulator-server-$testPort.out.log"
    $stderr = Join-Path ([System.IO.Path]::GetTempPath()) "pitch-simulator-server-$testPort.err.log"
    $process = $null
    try {
        $process = Start-Process -FilePath 'powershell.exe' -WindowStyle Hidden -PassThru `
            -RedirectStandardOutput $stdout -RedirectStandardError $stderr `
            -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', "`"$PSCommandPath`"", '-Port', $testPort, '-Root', "`"$projectRoot`"")

        $ready = $false
        $deadline = [DateTime]::UtcNow.AddSeconds(10)
        while ([DateTime]::UtcNow -lt $deadline -and -not $process.HasExited) {
            try {
                Assert-ServerResponse $testPort GET '/WebHarness/index.html' 200 'text/html; charset=utf-8'
                $ready = $true
                break
            } catch {
                Start-Sleep -Milliseconds 100
            }
        }
        if (-not $ready) {
            $diagnostic = if (Test-Path -LiteralPath $stderr) { Get-Content -LiteralPath $stderr -Raw } else { '' }
            throw "Local server did not become ready. $diagnostic"
        }

        Assert-ServerResponse $testPort GET '/Build/WebGL/index.html' 200 'text/html; charset=utf-8'
        Assert-ServerResponse $testPort GET '/WebHarness/harness.css' 200 'text/css; charset=utf-8'
        Assert-ServerResponse $testPort GET '/WebHarness/harness.js' 200 'text/javascript; charset=utf-8'
        Assert-ServerResponse $testPort GET '/Build/WebGL/Build/WebGL.loader.js' 200 'text/javascript; charset=utf-8'
        Assert-ServerResponse $testPort HEAD '/Build/WebGL/Build/WebGL.data' 200 'application/octet-stream' -ExpectNoBody
        Assert-ServerResponse $testPort HEAD '/Build/WebGL/Build/WebGL.wasm' 200 'application/wasm' -ExpectNoBody
        Assert-ServerResponse $testPort GET '/favicon.ico' 204 'image/x-icon' -ExpectNoBody
        Assert-ServerResponse $testPort GET '/WebHarness/%2e%2e/TASKS.md' 404
        Assert-ServerResponse $testPort GET '/missing.txt' 404
        Assert-ServerResponse $testPort POST '/WebHarness/index.html' 405
        Write-Output "Server self-test passed on temporary loopback port $testPort (GET/HEAD, MIME, traversal, missing and method checks)."
    } finally {
        if ($null -ne $process -and -not $process.HasExited) {
            Stop-Process -Id $process.Id -Force
            $process.WaitForExit(5000) | Out-Null
        }
        Remove-Item -LiteralPath $stdout, $stderr -Force -ErrorAction SilentlyContinue
    }
}

if ($SelfTest) {
    Invoke-ServerSelfTest
} else {
    Invoke-Server
}
