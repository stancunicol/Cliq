[CmdletBinding()]
param(
    [string] $PayloadDirectory =
        (Join-Path $PSScriptRoot '..\Cliq.VisualStudio\obj\Release\vsix-payload\runtime'),

    [string] $Browser = 'chrome',

    [switch] $KeepWorkspace
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$payload = [IO.Path]::GetFullPath($PayloadDirectory)
$apiExecutable = Join-Path $payload 'api\Cliq.Api.exe'
$nodeExecutable = Join-Path $payload 'node\node.exe'
$scannerScript = Join-Path $payload 'scanner\scan.js'
foreach ($required in @($apiExecutable, $nodeExecutable, $scannerScript)) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) {
        throw "Packaged runtime file '$required' was not found."
    }
}

$smokeRoot = Join-Path ([IO.Path]::GetTempPath()) (
    'cliq-vsix-smoke-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $smokeRoot | Out-Null
$stdout = Join-Path $smokeRoot 'sidecar.out.log'
$stderr = Join-Path $smokeRoot 'sidecar.err.log'
$workspace = Join-Path $smokeRoot 'workspace'
New-Item -ItemType Directory -Path $workspace | Out-Null

$token = [Guid]::NewGuid().ToString('N') + [Guid]::NewGuid().ToString('N')
$previousEnvironment = @{
    CLIQ_API_TOKEN = $env:CLIQ_API_TOKEN
    CLIQ_HOST_KIND = $env:CLIQ_HOST_KIND
    CLIQ_NODE_PATH = $env:CLIQ_NODE_PATH
    CLIQ_PUPPETEER_SCANNER_PATH = $env:CLIQ_PUPPETEER_SCANNER_PATH
}
$env:CLIQ_API_TOKEN = $token
$env:CLIQ_HOST_KIND = 'vsix-smoke'
$env:CLIQ_NODE_PATH = $nodeExecutable
$env:CLIQ_PUPPETEER_SCANNER_PATH = $scannerScript

$process = Start-Process `
    -FilePath $apiExecutable `
    -ArgumentList @('--urls', 'http://127.0.0.1:0') `
    -WorkingDirectory (Split-Path -Parent $apiExecutable) `
    -WindowStyle Hidden `
    -RedirectStandardOutput $stdout `
    -RedirectStandardError $stderr `
    -PassThru

try {
    $readyDeadline = (Get-Date).AddSeconds(45)
    $readyLine = $null
    while ((Get-Date) -lt $readyDeadline -and $null -eq $readyLine) {
        Start-Sleep -Milliseconds 200
        if (Test-Path -LiteralPath $stdout) {
            $readyLine = Get-Content -LiteralPath $stdout |
                Where-Object { $_ -like 'CLIQ_READY *' } |
                Select-Object -First 1
        }

        if ($process.HasExited) {
            throw "Packaged sidecar exited during startup: $(Get-Content -LiteralPath $stderr -Raw)"
        }
    }

    if ($null -eq $readyLine) {
        throw 'Packaged sidecar ready message timed out.'
    }

    $ready = $readyLine.Substring('CLIQ_READY '.Length) | ConvertFrom-Json
    $baseAddress = ([string]$ready.address).TrimEnd('/')
    $headers = @{ Authorization = "Bearer $token" }

    Invoke-RestMethod `
        -Method Post `
        -Uri "$baseAddress/api/v1/workspaces/open" `
        -Headers $headers `
        -ContentType 'application/json' `
        -Body (@{ rootPath = $workspace } | ConvertTo-Json) | Out-Null

    $scan = Invoke-RestMethod `
        -Method Post `
        -Uri "$baseAddress/api/v1/pages/scan" `
        -Headers $headers `
        -ContentType 'application/json' `
        -Body (@{
            url = 'https://example.com/'
            name = 'Example'
            browser = $Browser
            headless = $true
        } | ConvertTo-Json)

    $test = @{
        schemaVersion = 1
        id = 'smoke'
        name = 'Packaged runtime smoke'
        description = 'Exercises the staged VSIX runtime.'
        tags = @('smoke')
        updatedAt = [DateTimeOffset]::UtcNow.ToString('o')
        steps = @(
            @{
                id = 'navigate'
                action = 'navigate'
                label = 'Open example'
                enabled = $true
                url = 'https://example.com/'
            },
            @{
                id = 'fail-url'
                action = 'assertUrl'
                label = 'Capture a deterministic failure'
                enabled = $true
                value = @{ kind = 'literal'; value = '^https://never-match.invalid/$' }
            }
        )
    }
    Invoke-RestMethod `
        -Method Put `
        -Uri "$baseAddress/api/v1/tests/smoke" `
        -Headers $headers `
        -ContentType 'application/json' `
        -Body ($test | ConvertTo-Json -Depth 8) | Out-Null

    $generation = Invoke-RestMethod `
        -Method Post `
        -Uri "$baseAddress/api/v1/generation" `
        -Headers $headers `
        -ContentType 'application/json' `
        -Body (@{
            pageIds = @($scan.page.id)
            testIds = @('smoke')
        } | ConvertTo-Json)

    $run = Invoke-RestMethod `
        -Method Post `
        -Uri "$baseAddress/api/v1/tests/smoke/runs" `
        -Headers $headers `
        -ContentType 'application/json' `
        -Body (@{ browser = $Browser } | ConvertTo-Json)
    $runDeadline = (Get-Date).AddSeconds(75)
    do {
        Start-Sleep -Milliseconds 500
        $run = Invoke-RestMethod `
            -Method Get `
            -Uri "$baseAddress/api/v1/runs/$($run.id)" `
            -Headers $headers
    } while ($run.status -in @('queued', 'running') -and (Get-Date) -lt $runDeadline)
    if ($run.status -in @('queued', 'running')) {
        throw 'Packaged Selenium run timed out.'
    }

    $screenshot = $run.steps |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_.screenshotPath) } |
        Select-Object -First 1
    $screenshotBytes = 0
    if ($null -ne $screenshot) {
        $response = Invoke-WebRequest `
            -UseBasicParsing `
            -Uri "$baseAddress/api/v1/runs/$($run.id)/screenshots/$($screenshot.screenshotPath)" `
            -Headers $headers
        $screenshotBytes = $response.RawContentStream.Length
    }

    $cancelledRun = Invoke-RestMethod `
        -Method Post `
        -Uri "$baseAddress/api/v1/tests/smoke/runs" `
        -Headers $headers `
        -ContentType 'application/json' `
        -Body (@{ browser = $Browser } | ConvertTo-Json)
    Invoke-RestMethod `
        -Method Post `
        -Uri "$baseAddress/api/v1/runs/$($cancelledRun.id)/cancel" `
        -Headers $headers | Out-Null
    $cancelDeadline = (Get-Date).AddSeconds(30)
    do {
        Start-Sleep -Milliseconds 300
        $cancelledRun = Invoke-RestMethod `
            -Method Get `
            -Uri "$baseAddress/api/v1/runs/$($cancelledRun.id)" `
            -Headers $headers
    } while (
        $cancelledRun.status -in @('queued', 'running') -and
        (Get-Date) -lt $cancelDeadline)

    Invoke-WebRequest `
        -UseBasicParsing `
        -Method Post `
        -Uri "$baseAddress/api/v1/workspaces/close" `
        -Headers $headers | Out-Null

    $lockPath = Join-Path $workspace '.cliq\workspace.lock'
    $lockProbe = [IO.File]::Open(
        $lockPath,
        [IO.FileMode]::Open,
        [IO.FileAccess]::ReadWrite,
        [IO.FileShare]::None)
    $lockProbe.Dispose()

    [pscustomobject]@{
        Address = $baseAddress
        PageId = $scan.page.id
        Elements = @($scan.page.elements).Count
        Artifacts = @($generation.artifacts).Count
        RunStatus = $run.status
        ScreenshotBytes = $screenshotBytes
        CancelStatus = $cancelledRun.status
        WorkspaceLeaseReleased = $true
    }
}
finally {
    if (-not $process.HasExited) {
        $process.Kill()
        $process.WaitForExit()
    }
    $process.Dispose()

    foreach ($entry in $previousEnvironment.GetEnumerator()) {
        if ($null -eq $entry.Value) {
            Remove-Item "Env:$($entry.Key)" -ErrorAction SilentlyContinue
        }
        else {
            Set-Item "Env:$($entry.Key)" $entry.Value
        }
    }

    if (-not $KeepWorkspace) {
        $resolved = [IO.Path]::GetFullPath($smokeRoot)
        $expectedPrefix = [IO.Path]::GetFullPath(
            (Join-Path ([IO.Path]::GetTempPath()) 'cliq-vsix-smoke-'))
        if (-not $resolved.StartsWith($expectedPrefix, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to remove unexpected smoke directory '$resolved'."
        }
        [IO.Directory]::Delete($resolved, $true)
    }
}
