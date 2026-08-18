[CmdletBinding()]
param(
    [ValidateSet('Build', 'Stage', 'Validate')]
    [string] $Target = 'Build',

    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',

    [string] $PayloadDirectory,

    [string] $VsixPath,

    [string] $CacheDirectory,

    [ValidatePattern('^[0-9]+\.[0-9]+\.[0-9]+$')]
    [string] $Version
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$script:BuildDirectory = Split-Path -Parent $PSCommandPath
$script:VisualStudioDirectory = Split-Path -Parent $script:BuildDirectory
$script:RepositoryRoot = Split-Path -Parent $script:VisualStudioDirectory
$script:ExtensionProjectDirectory = Join-Path $script:VisualStudioDirectory 'Cliq.VisualStudio'
$script:ExtensionProject = Join-Path $script:ExtensionProjectDirectory 'Cliq.VisualStudio.csproj'
$script:BackendProject = Join-Path $script:RepositoryRoot 'backend\src\Cliq.Api\Cliq.Api.csproj'
$script:ScannerSource = Join-Path $script:RepositoryRoot 'backend\tools\puppeteer-scanner'
$script:Versions = Import-PowerShellDataFile (Join-Path $script:BuildDirectory 'Versions.psd1')

if (-not [string]::IsNullOrWhiteSpace($Version)) {
    $script:Versions.VsixVersion = $Version
}

if ([string]::IsNullOrWhiteSpace($PayloadDirectory)) {
    $PayloadDirectory = Join-Path $script:ExtensionProjectDirectory "obj\$Configuration\vsix-payload"
}

if ([string]::IsNullOrWhiteSpace($CacheDirectory)) {
    $localApplicationData = [Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)
    $CacheDirectory = Join-Path $localApplicationData 'Cliq\build-cache'
}

function Invoke-NativeCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Command,

        [Parameter(Mandatory = $true)]
        [string[]] $Arguments,

        [string] $WorkingDirectory = $script:RepositoryRoot
    )

    Push-Location -LiteralPath $WorkingDirectory
    try {
        & $Command @Arguments
        if ($LASTEXITCODE -ne 0) {
            throw "'$Command' exited with code $LASTEXITCODE."
        }
    }
    finally {
        Pop-Location
    }
}

function Reset-BuildDirectory {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path
    )

    $fullPath = [IO.Path]::GetFullPath($Path)
    $allowedRoot = [IO.Path]::GetFullPath($script:VisualStudioDirectory).TrimEnd('\') + '\'
    if (-not $fullPath.StartsWith($allowedRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to reset a build directory outside '$allowedRoot': '$fullPath'."
    }

    if (Test-Path -LiteralPath $fullPath) {
        Remove-Item -LiteralPath $fullPath -Recurse -Force
    }

    New-Item -ItemType Directory -Path $fullPath -Force | Out-Null
}

function Get-NodeDistribution {
    $nodeVersion = $script:Versions.NodeVersion
    $archiveName = "node-v$nodeVersion-win-x64.zip"
    $versionCache = Join-Path $CacheDirectory "node\$nodeVersion"
    $archivePath = Join-Path $versionCache $archiveName
    $checksumsPath = Join-Path $versionCache 'SHASUMS256.txt'
    $distributionUrl = "$($script:Versions.NodeDistributionBaseUrl)/v$nodeVersion"

    New-Item -ItemType Directory -Path $versionCache -Force | Out-Null

    if (-not (Test-Path -LiteralPath $checksumsPath)) {
        Write-Host "Downloading Node.js checksum manifest for v$nodeVersion..."
        Invoke-WebRequest -UseBasicParsing -Uri "$distributionUrl/SHASUMS256.txt" -OutFile $checksumsPath
    }

    $checksumLine = Get-Content -LiteralPath $checksumsPath |
        Where-Object { $_ -match "^[0-9a-fA-F]{64}\s+\*?$([regex]::Escape($archiveName))$" } |
        Select-Object -First 1
    if ($null -eq $checksumLine) {
        throw "The official checksum manifest does not contain '$archiveName'."
    }

    $officialChecksum = ($checksumLine -split '\s+')[0].ToLowerInvariant()
    $pinnedChecksum = ([string]$script:Versions.NodeArchiveSha256).ToLowerInvariant()
    if ($officialChecksum -ne $pinnedChecksum) {
        throw "The official checksum '$officialChecksum' does not match the pinned checksum '$pinnedChecksum'."
    }

    if (-not (Test-Path -LiteralPath $archivePath)) {
        Write-Host "Downloading pinned Node.js v$nodeVersion win-x64 distribution..."
        Invoke-WebRequest -UseBasicParsing -Uri "$distributionUrl/$archiveName" -OutFile $archivePath
    }

    $actualChecksum = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actualChecksum -ne $pinnedChecksum) {
        throw "Cached Node.js archive checksum '$actualChecksum' does not match '$pinnedChecksum'. Delete '$archivePath' and retry."
    }

    return $archivePath
}

function Write-ScannerNotices {
    param(
        [Parameter(Mandatory = $true)]
        [string] $PackageLockPath,

        [Parameter(Mandatory = $true)]
        [string] $NodeExecutable,

        [Parameter(Mandatory = $true)]
        [string] $Destination
    )

    # Windows PowerShell 5.1 cannot deserialize package-lock.json because its
    # "packages" map contains an empty-string root key. Use the pinned Node
    # runtime to normalize the relevant fields before PowerShell reads them.
    $normalizer = @'
const fs = require("fs");
const lock = JSON.parse(fs.readFileSync(process.argv[2], "utf8"));
const rows = Object.entries(lock.packages || {})
  .filter(([key]) => key.startsWith("node_modules/"))
  .map(([key, value]) => ({
    Name: key.slice("node_modules/".length),
    Version: value.version || "unknown",
    License: value.license || "SEE PACKAGE"
  }));
process.stdout.write(JSON.stringify(rows));
'@
    $normalizerPath = "$Destination.normalizer.cjs"
    Set-Content -LiteralPath $normalizerPath -Value $normalizer -Encoding UTF8
    try {
        $normalizedJson = & $NodeExecutable $normalizerPath $PackageLockPath
        if ($LASTEXITCODE -ne 0) {
            throw "Node.js failed to normalize scanner license metadata."
        }
    }
    finally {
        Remove-Item -LiteralPath $normalizerPath -Force -ErrorAction SilentlyContinue
    }
    $rows = @($normalizedJson | ConvertFrom-Json)

    $lines = @(
        'Cliq Visual Studio Extension - Scanner Third-Party Notices'
        ''
        'The scanner payload includes the following production npm packages.'
        'Copyright and complete license texts remain in each package directory where supplied.'
        ''
    )
    $lines += $rows |
        Sort-Object Name |
        ForEach-Object { "$($_.Name) $($_.Version) - $($_.License)" }
    $lines += @(
        ''
        'Package metadata source: backend/tools/puppeteer-scanner/package-lock.json'
    )

    Set-Content -LiteralPath $Destination -Value $lines -Encoding UTF8
}

function Stage-Payload {
    $payloadFullPath = [IO.Path]::GetFullPath($PayloadDirectory)
    $scratchDirectory = Join-Path $script:ExtensionProjectDirectory "obj\$Configuration\vsix-payload-work"
    Reset-BuildDirectory -Path $payloadFullPath
    Reset-BuildDirectory -Path $scratchDirectory

    $apiDestination = Join-Path $payloadFullPath 'runtime\api'
    $nodeDestination = Join-Path $payloadFullPath 'runtime\node'
    $scannerDestination = Join-Path $payloadFullPath 'runtime\scanner'
    $licensesDestination = Join-Path $payloadFullPath 'licenses'
    New-Item -ItemType Directory -Path $apiDestination, $nodeDestination, $scannerDestination, $licensesDestination -Force | Out-Null

    Write-Host "Publishing Cliq.Api $($script:Versions.BackendFramework) $($script:Versions.BackendRuntimeIdentifier)..."
    Invoke-NativeCommand -Command 'dotnet' -Arguments @(
        'restore',
        $script:BackendProject,
        '--locked-mode'
    )
    # Packaging properties such as DebugType=None must be applied to every
    # project reference even when a developer built the backend earlier.
    Invoke-NativeCommand -Command 'dotnet' -Arguments @(
        'clean',
        (Join-Path $script:RepositoryRoot 'backend\Cliq.slnx'),
        '--configuration', $script:Versions.BackendConfiguration
    )
    # Self-contained publishing needs runtime-pack assets that are intentionally not
    # recorded in the repository's portable lock files. Restore those SDK-owned
    # assets without rewriting packages.lock.json after the locked dependency check.
    Invoke-NativeCommand -Command 'dotnet' -Arguments @(
        'restore',
        $script:BackendProject,
        '--runtime', $script:Versions.BackendRuntimeIdentifier,
        '-p:RestorePackagesWithLockFile=true',
        '-p:RestoreLockedMode=false',
        "-p:NuGetLockFilePath=obj/vsix-packages.$($script:Versions.BackendRuntimeIdentifier).lock.json"
    )
    Invoke-NativeCommand -Command 'dotnet' -Arguments @(
        'publish',
        $script:BackendProject,
        '--configuration', $script:Versions.BackendConfiguration,
        '--framework', $script:Versions.BackendFramework,
        '--runtime', $script:Versions.BackendRuntimeIdentifier,
        '--self-contained', 'true',
        '--no-restore',
        '--output', $apiDestination,
        '-p:PublishSingleFile=false',
        '-p:DebugType=None',
        '-p:DebugSymbols=false',
        '-p:ContinuousIntegrationBuild=true',
        '-p:RestorePackagesWithLockFile=true',
        '-p:RestoreLockedMode=false',
        "-p:NuGetLockFilePath=obj/vsix-packages.$($script:Versions.BackendRuntimeIdentifier).lock.json"
    )

    $apiExecutable = Join-Path $apiDestination 'Cliq.Api.exe'
    if (-not (Test-Path -LiteralPath $apiExecutable)) {
        throw "Backend publish did not produce '$apiExecutable'."
    }

    $nodeArchive = Get-NodeDistribution
    $nodeExtractDirectory = Join-Path $scratchDirectory 'node'
    Expand-Archive -LiteralPath $nodeArchive -DestinationPath $nodeExtractDirectory -Force
    $nodeDistributionRoot = Get-ChildItem -LiteralPath $nodeExtractDirectory -Directory | Select-Object -First 1
    if ($null -eq $nodeDistributionRoot) {
        throw "Node.js archive '$nodeArchive' did not contain a distribution directory."
    }

    Copy-Item -LiteralPath (Join-Path $nodeDistributionRoot.FullName 'node.exe') -Destination $nodeDestination
    Copy-Item -LiteralPath (Join-Path $nodeDistributionRoot.FullName 'LICENSE') -Destination (Join-Path $licensesDestination 'Node-LICENSE.txt')

    $scannerInstallDirectory = Join-Path $scratchDirectory 'scanner'
    New-Item -ItemType Directory -Path $scannerInstallDirectory -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $script:ScannerSource 'package.json') -Destination $scannerInstallDirectory
    Copy-Item -LiteralPath (Join-Path $script:ScannerSource 'package-lock.json') -Destination $scannerInstallDirectory
    Get-ChildItem -LiteralPath $script:ScannerSource -File -Filter '*.js' |
        Copy-Item -Destination $scannerInstallDirectory

    $npmCommand = Join-Path $nodeDistributionRoot.FullName 'npm.cmd'
    $previousPath = $env:Path
    $previousSkipDownload = $env:PUPPETEER_SKIP_DOWNLOAD
    $previousSkipChrome = $env:PUPPETEER_SKIP_CHROME_DOWNLOAD
    $previousSkipFirefox = $env:PUPPETEER_SKIP_FIREFOX_DOWNLOAD
    try {
        $env:Path = "$($nodeDistributionRoot.FullName);$previousPath"
        $env:PUPPETEER_SKIP_DOWNLOAD = 'true'
        $env:PUPPETEER_SKIP_CHROME_DOWNLOAD = 'true'
        $env:PUPPETEER_SKIP_FIREFOX_DOWNLOAD = 'true'
        Write-Host 'Installing deterministic production scanner dependencies with browser downloads disabled...'
        Invoke-NativeCommand -Command $npmCommand -Arguments @(
            'ci',
            '--omit=dev',
            '--no-audit',
            '--no-fund'
        ) -WorkingDirectory $scannerInstallDirectory
    }
    finally {
        $env:Path = $previousPath
        $env:PUPPETEER_SKIP_DOWNLOAD = $previousSkipDownload
        $env:PUPPETEER_SKIP_CHROME_DOWNLOAD = $previousSkipChrome
        $env:PUPPETEER_SKIP_FIREFOX_DOWNLOAD = $previousSkipFirefox
    }

    $requiredScannerPackage = Join-Path $scannerInstallDirectory "node_modules\$($script:Versions.ScannerRequiredPackage)\package.json"
    if (-not (Test-Path -LiteralPath $requiredScannerPackage)) {
        throw "Scanner install did not produce the required '$($script:Versions.ScannerRequiredPackage)' package."
    }

    $browserPayload = Get-ChildItem -LiteralPath $scannerInstallDirectory -Recurse -File |
        Where-Object {
            $_.Name -in @('chrome.exe', 'chromium.exe', 'chrome-headless-shell.exe') -or
            $_.FullName -match '[\\/](chrome-win|chromium|chrome-headless-shell)[\\/]'
        } |
        Select-Object -First 1
    if ($null -ne $browserPayload) {
        throw "Browser payload was unexpectedly downloaded: '$($browserPayload.FullName)'."
    }

    Get-ChildItem -LiteralPath $scannerInstallDirectory -File -Filter '*.js' |
        Copy-Item -Destination $scannerDestination
    Copy-Item -LiteralPath (Join-Path $scannerInstallDirectory 'package.json') -Destination $scannerDestination
    Copy-Item -LiteralPath (Join-Path $scannerInstallDirectory 'package-lock.json') -Destination $scannerDestination
    Copy-Item -LiteralPath (Join-Path $scannerInstallDirectory 'node_modules') -Destination $scannerDestination -Recurse

    Copy-Item -LiteralPath (Join-Path $script:RepositoryRoot 'LICENSE') -Destination (Join-Path $licensesDestination 'Cliq-LICENSE.txt')
    Write-ScannerNotices `
        -PackageLockPath (Join-Path $scannerInstallDirectory 'package-lock.json') `
        -NodeExecutable (Join-Path $nodeDestination 'node.exe') `
        -Destination (Join-Path $licensesDestination 'Scanner-THIRD-PARTY-NOTICES.txt')

    $scannerLockHash = (Get-FileHash -LiteralPath (Join-Path $script:ScannerSource 'package-lock.json') -Algorithm SHA256).Hash.ToLowerInvariant()
    $buildInfo = @(
        "Cliq VSIX payload version: $($script:Versions.VsixVersion)"
        "Backend: $($script:Versions.BackendFramework) $($script:Versions.BackendRuntimeIdentifier) self-contained"
        "Node.js: $($script:Versions.NodeVersion) win-x64"
        "Node.js archive SHA-256: $($script:Versions.NodeArchiveSha256)"
        "Scanner lock SHA-256: $scannerLockHash"
        'Browser: not bundled; installed Chrome or Edge is required'
    )
    Set-Content -LiteralPath (Join-Path $payloadFullPath 'PAYLOAD-INFO.txt') -Value $buildInfo -Encoding UTF8

    $stagedBytes = (Get-ChildItem -LiteralPath $payloadFullPath -Recurse -File | Measure-Object Length -Sum).Sum
    Write-Host ("Staged payload: {0:N2} MiB at {1}" -f ($stagedBytes / 1MB), $payloadFullPath)
}

function Test-VsixArchive {
    param(
        [Parameter(Mandatory = $true)]
        [string] $ArchivePath
    )

    $archiveFullPath = [IO.Path]::GetFullPath($ArchivePath)
    if (-not (Test-Path -LiteralPath $archiveFullPath -PathType Leaf)) {
        throw "VSIX archive '$archiveFullPath' does not exist."
    }

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::OpenRead($archiveFullPath)
    try {
        $entryNames = @($archive.Entries | ForEach-Object { $_.FullName.Replace('\', '/') })
        $requiredPatterns = @(
            '^extension\.vsixmanifest$',
            '(^|/)Cliq\.VisualStudio\.dll$',
            '^Payload/runtime/api/Cliq\.Api\.exe$',
            '^Payload/runtime/node/node\.exe$',
            '^Payload/runtime/scanner/scan\.js$',
            '^Payload/runtime/scanner/node_modules/puppeteer-core/package\.json$',
            '^Payload/licenses/Cliq-LICENSE\.txt$',
            '^Payload/licenses/Node-LICENSE\.txt$',
            '^Payload/licenses/Scanner-THIRD-PARTY-NOTICES\.txt$'
        )

        foreach ($pattern in $requiredPatterns) {
            if (-not ($entryNames | Where-Object { $_ -match $pattern } | Select-Object -First 1)) {
                throw "VSIX is missing an entry matching '$pattern'."
            }
        }

        $forbiddenEntryPattern = '(?i)(^|/)(\.env($|\.)|secrets?\.local\.json|workspace\.lock|\.npm|_cacache|node[_-]?cache|chrome-win|chromium|chrome-headless-shell)(/|$)'
        $forbiddenEntry = $entryNames |
            Where-Object {
                $_ -match $forbiddenEntryPattern -or
                $_ -match '(?i)(^|/)(chrome|chromium|chrome-headless-shell)\.exe$'
            } |
            Select-Object -First 1
        if ($null -ne $forbiddenEntry) {
            throw "VSIX contains a forbidden cache, secret, debug, or browser entry: '$forbiddenEntry'."
        }

        $repositoryUtf8 = [Text.Encoding]::UTF8.GetBytes([IO.Path]::GetFullPath($script:RepositoryRoot))
        $repositoryPath = [IO.Path]::GetFullPath($script:RepositoryRoot)
        $binaryEncoding = [Text.Encoding]::GetEncoding(28591)
        $repositoryBinaryText = $binaryEncoding.GetString($repositoryUtf8)
        $textEntryPattern = '(?i)\.(json|js|txt|xml|config|manifest|md|cmd|ps1|props|targets)$'
        $secretAssignmentPattern = '(?i)(api[_-]?token|access[_-]?token|authorization)\s*[:=]\s*["'']?(bearer\s+)?[A-Za-z0-9+/_\-.=]{16,}'
        $hasCommandTableRegistration = $false
        $hasEmbeddedCommandTable = $false

        foreach ($entry in $archive.Entries) {
            if ($entry.Length -eq 0) {
                continue
            }

            $stream = $entry.Open()
            $memory = $null
            try {
                $memory = New-Object IO.MemoryStream
                $stream.CopyTo($memory)
                $bytes = $memory.ToArray()
            }
            finally {
                $stream.Dispose()
                if ($null -ne $memory) {
                    $memory.Dispose()
                }
            }

            $binaryText = $binaryEncoding.GetString($bytes)
            $unicodeText = [Text.Encoding]::Unicode.GetString($bytes)
            if ($entry.FullName -ceq 'extension.vsixmanifest' -and
                -not [string]::IsNullOrWhiteSpace($Version)) {
                $manifestText = [Text.Encoding]::UTF8.GetString($bytes)
                $expectedVersionPattern =
                    '(?s)<Identity\s+[^>]*?\bVersion\s*=\s*["'']' +
                    [regex]::Escape($Version) +
                    '["'']'
                if ($manifestText -notmatch $expectedVersionPattern) {
                    throw "VSIX manifest Identity version does not match the requested version '$Version'."
                }
            }
            if ($entry.FullName -match '(?i)\.pkgdef$' -and
                $unicodeText -match '(?i),\s*Menus\.ctmenu,\s*1') {
                $hasCommandTableRegistration = $true
            }

            if ($entry.FullName -match '(?i)(^|/)Cliq\.VisualStudio\.dll$') {
                $assembly = [Reflection.Assembly]::Load([byte[]] $bytes)
                foreach ($resourceName in $assembly.GetManifestResourceNames()) {
                    if (-not $resourceName.EndsWith('.resources', [StringComparison]::OrdinalIgnoreCase)) {
                        continue
                    }

                    $resourceStream = $assembly.GetManifestResourceStream($resourceName)
                    if ($null -ne $resourceStream) {
                        $resourceReader = New-Object Resources.ResourceReader($resourceStream)
                        try {
                            $resourceEnumerator = $resourceReader.GetEnumerator()
                            while ($resourceEnumerator.MoveNext()) {
                                if ($resourceEnumerator.Key -ceq 'Menus.ctmenu') {
                                    $hasEmbeddedCommandTable = $true
                                    break
                                }
                            }
                        }
                        finally {
                            $resourceReader.Dispose()
                            $resourceStream.Dispose()
                        }
                    }

                    if ($hasEmbeddedCommandTable) {
                        break
                    }
                }
            }

            if ($binaryText.IndexOf($repositoryBinaryText, [StringComparison]::OrdinalIgnoreCase) -ge 0 -or
                $unicodeText.IndexOf($repositoryPath, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
                throw "VSIX entry '$($entry.FullName)' contains the repository's absolute path."
            }

            if ($entry.FullName -match $textEntryPattern) {
                $text = [Text.Encoding]::UTF8.GetString($bytes)
                if ($text -match $secretAssignmentPattern) {
                    throw "VSIX entry '$($entry.FullName)' appears to contain a token or authorization secret."
                }
            }
        }

        if (-not $hasCommandTableRegistration -or -not $hasEmbeddedCommandTable) {
            throw 'VSIX command-table registration does not match the embedded Menus.ctmenu resource.'
        }

        $archiveSize = (Get-Item -LiteralPath $archiveFullPath).Length
        $archiveHash = (Get-FileHash -LiteralPath $archiveFullPath -Algorithm SHA256).Hash.ToLowerInvariant()
        Write-Host ("Validated VSIX: {0:N2} MiB, {1} entries, SHA-256 {2}" -f ($archiveSize / 1MB), $entryNames.Count, $archiveHash)
    }
    finally {
        $archive.Dispose()
    }
}

function Set-VsixManifestVersion {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Version
    )

    $manifestPath = Join-Path $script:ExtensionProjectDirectory 'source.extension.vsixmanifest'
    $updated = [Text.RegularExpressions.Regex]::Replace(
        (Get-Content -LiteralPath $manifestPath -Raw),
        '(<Identity\s+[^>]*?\bVersion=")[^"]*(")',
        "`${1}$Version`${2}",
        [Text.RegularExpressions.RegexOptions]::Singleline)
    if ($updated.IndexOf("Version=`"$Version`"", [StringComparison]::Ordinal) -lt 0) {
        throw "Could not stamp version '$Version' into '$manifestPath'."
    }
    [IO.File]::WriteAllText($manifestPath, $updated, (New-Object Text.UTF8Encoding($false)))
    Write-Host "Stamped VSIX manifest version $Version."
}

function Build-Vsix {
    Stage-Payload

    $manifestPath = Join-Path $script:ExtensionProjectDirectory 'source.extension.vsixmanifest'
    $originalManifestBytes = $null
    if (-not [string]::IsNullOrWhiteSpace($Version)) {
        $originalManifestBytes = [IO.File]::ReadAllBytes($manifestPath)
        Set-VsixManifestVersion -Version $Version
    }

    try {
        Write-Host "Building Cliq Visual Studio extension ($Configuration)..."
        Invoke-NativeCommand -Command 'dotnet' -Arguments @(
            'build',
            $script:ExtensionProject,
            '--configuration', $Configuration,
            '-p:CliqPayloadPrepared=true',
            '-p:ContinuousIntegrationBuild=true'
        )

        $candidate = Get-ChildItem -LiteralPath (Join-Path $script:ExtensionProjectDirectory "bin\$Configuration") -Recurse -File -Filter '*.vsix' |
            Sort-Object LastWriteTimeUtc -Descending |
            Select-Object -First 1
        if ($null -eq $candidate) {
            throw "The extension build did not produce a VSIX under 'visualstudio\Cliq.VisualStudio\bin\$Configuration'."
        }

        Test-VsixArchive -ArchivePath $candidate.FullName
        Write-Output $candidate.FullName
    }
    finally {
        if ($null -ne $originalManifestBytes) {
            [IO.File]::WriteAllBytes($manifestPath, $originalManifestBytes)
            Write-Host "Restored the original VSIX manifest."
        }
    }
}

switch ($Target) {
    'Stage' {
        Stage-Payload
    }
    'Validate' {
        if ([string]::IsNullOrWhiteSpace($VsixPath)) {
            throw '-VsixPath is required when -Target Validate is used.'
        }
        Test-VsixArchive -ArchivePath $VsixPath
    }
    'Build' {
        Build-Vsix
    }
}
