# A cooperative conflict guard, not a sandbox for untrusted Editor code.
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][ValidateSet('Prepare', 'Run', 'Apply')][string]$Action,
    [string]$ProjectRoot,
    [Parameter(Mandatory = $true)][string]$Workspace,
    [string[]]$Targets,
    [string]$ManifestSha256,
    [string]$ReceiptSha256,
    [string]$UnityPath,
    [string]$ExecuteMethod,
    [ValidateRange(1, 3600)][int]$TimeoutSeconds = 300
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function FullPath([string]$Path) {
    return [IO.Path]::GetFullPath($Path).TrimEnd([char[]]'\/')
}

function Assert-NoLink([string]$Path) {
    $cursor = FullPath $Path
    while ($cursor) {
        if (Test-Path -LiteralPath $cursor) {
            $item = Get-Item -LiteralPath $cursor -Force
            if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "Reparse point is not allowed: $cursor"
            }
        }
        $cursor = [IO.Path]::GetDirectoryName($cursor)
    }
}

function Hash([string]$Path) {
    Assert-NoLink $Path
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return $null }
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Assert-EditorClosed([string]$Root) {
    $lockPath = Join-Path $Root 'Temp/UnityLockfile'
    Assert-NoLink $lockPath
    if (-not (Test-Path -LiteralPath $lockPath)) { return }
    try {
        $lock = [IO.File]::Open($lockPath, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
        $lock.Dispose()
    }
    catch { throw "Unity project is open or its lock is inaccessible; close its Editor first: $Root" }
}

function Write-NewJson([string]$Path, $Value) {
    Assert-NoLink $Path
    $bytes = [Text.Encoding]::UTF8.GetBytes(($Value | ConvertTo-Json -Depth 12))
    $stream = [IO.File]::Open($Path, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write)
    try { $stream.Write($bytes, 0, $bytes.Length) } finally { $stream.Dispose() }
}

function Read-PinnedJson([string]$Path, [string]$ExpectedHash) {
    if ($ExpectedHash -notmatch '^[0-9a-fA-F]{64}$' -or (Hash $Path) -ne $ExpectedHash) {
        throw "Missing or changed pinned evidence: $Path"
    }
    return (Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json)
}

function SourceMap([string]$Root) {
    $map = @{}
    $pending = New-Object 'Collections.Generic.Stack[string]'
    foreach ($name in @('Assets', 'Packages', 'ProjectSettings')) {
        $path = Join-Path $Root $name
        Assert-NoLink $path
        if (-not (Test-Path -LiteralPath $path -PathType Container)) { throw "Missing source directory: $path" }
        $pending.Push($path)
    }
    while ($pending.Count -gt 0) {
        $directory = Get-Item -LiteralPath $pending.Pop() -Force
        if (($directory.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "Reparse point is not allowed: $($directory.FullName)" }
        foreach ($item in Get-ChildItem -LiteralPath $directory.FullName -Force) {
            $relative = $item.FullName.Substring($Root.Length + 1).Replace('\', '/')
            if ($relative -eq 'Assets/_Recovery' -or $relative -eq 'Assets/_Recovery.meta') { continue }
            if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "Reparse point is not allowed: $relative" }
            if ($item.PSIsContainer) { $pending.Push($item.FullName) }
            # Ancestors and each directory/leaf were checked by this traversal.
            # Avoid walking every leaf's full ancestor chain again for each snapshot.
            else { $map[$relative] = (Get-FileHash -LiteralPath $item.FullName -Algorithm SHA256).Hash.ToLowerInvariant() }
        }
    }
    return $map
}

function To-Records($Map) {
    return @($Map.Keys | Sort-Object | ForEach-Object { [ordered]@{ path = $_; sha256 = $Map[$_] } })
}

function From-Records($Records) {
    $map = @{}
    foreach ($record in $Records) {
        if ($map.ContainsKey($record.path)) { throw "Duplicate evidence path: $($record.path)" }
        $map[$record.path] = $record.sha256
    }
    return $map
}

function Assert-Same($Expected, $Actual, [string]$Label) {
    foreach ($path in @(@($Expected.Keys) + @($Actual.Keys) | Sort-Object -Unique)) {
        if ($Expected[$path] -ne $Actual[$path]) { throw "$Label changed: $path" }
    }
}

function Validate-Targets($Paths, [string]$Root) {
    if (@($Paths).Count -eq 0) { throw 'At least one exact target is required.' }
    $allowed = @{}
    foreach ($path in $Paths) {
        if ($path -cnotmatch '^Assets/(?:[^/]+/)*[^/]+\.(unity|prefab|mat|asset)$' -or
            $path -match '[\\:*?"<>|\x00-\x1f]' -or $path -match '(^|/)\.\.?(/|$)' -or
            $path -match '(^|/)(Editor|Scripts|_Recovery|SceneWork)(/|$)' -or
            $path -match '(^|/)[^/]*[. ](/|$)') {
            throw "Not an exact supported asset target: $path"
        }
        if ($allowed.ContainsKey($path)) { throw "Duplicate target: $path" }
        $absolute = Join-Path $Root $path
        Assert-NoLink $absolute
        if (-not (Test-Path -LiteralPath (Split-Path $absolute -Parent) -PathType Container)) {
            throw "New target folders are not supported; select an existing folder: $path"
        }
        if (Test-Path -LiteralPath $absolute -PathType Container) { throw "Target is a directory: $path" }
        $allowed[$path] = $true
        $allowed["$path.meta"] = $true
    }
    return $allowed
}

function Is-Helper([string]$Path) {
    return $Path -cmatch '^Assets/Editor/SceneWork/[^/]+\.cs(?:\.meta)?$' -or
        $Path -ceq 'Assets/Editor/SceneWork.meta' -or $Path -ceq 'Assets/Editor.meta'
}

function Meta-Guid([string]$Path) {
    $lines = [regex]::Matches((Get-Content -LiteralPath $Path -Raw), '(?m)^guid:[^\r\n]*')
    if ($lines.Count -ne 1 -or $lines[0].Value -notmatch '^guid: ([0-9a-fA-F]{32})[ \t]*$') {
        throw "Meta must contain exactly one valid GUID: $Path"
    }
    return $Matches[1].ToLowerInvariant()
}

function Assert-Candidate($Baseline, $Candidate, $Allowed) {
    foreach ($path in @(@($Baseline.Keys) + @($Candidate.Keys) | Sort-Object -Unique)) {
        if ($Baseline[$path] -eq $Candidate[$path]) { continue }
        if ($Allowed.ContainsKey($path)) {
            if (-not $Candidate.ContainsKey($path)) { throw "Deleting targets is not supported: $path" }
            continue
        }
        if ((Is-Helper $path) -and -not $Baseline.ContainsKey($path)) { continue }
        throw "Out-of-scope workspace mutation: $path"
    }
    $targetGuids = @{}
    foreach ($path in $Allowed.Keys) {
        if (-not $Candidate.ContainsKey($path)) { throw "Target or paired meta is missing: $path" }
        if ($path.EndsWith('.meta')) {
            $new = Meta-Guid (Join-Path $script:work $path)
            if ($targetGuids.ContainsKey($new)) { throw "Duplicate target meta GUID: $path" }
            $targetGuids[$new] = $path
            if ($Baseline.ContainsKey($path) -and (Meta-Guid (Join-Path $script:project $path)) -ne $new) {
                throw "Existing meta GUID changed: $path"
            }
        }
    }
    foreach ($path in $Candidate.Keys) {
        if ($path.StartsWith('Assets/') -and $path.EndsWith('.meta') -and -not $Allowed.ContainsKey($path)) {
            $guids = [regex]::Matches((Get-Content -LiteralPath (Join-Path $script:work $path) -Raw), '(?m)^guid: ([0-9a-fA-F]{32})[ \t]*\r?$')
            foreach ($guid in $guids) {
                if ($targetGuids.ContainsKey($guid.Groups[1].Value)) { throw "Target meta GUID collides with: $path" }
            }
        }
    }
}

try {
    if (-not $ProjectRoot) { $ProjectRoot = Join-Path $PSScriptRoot '../..' }
    $script:project = FullPath $ProjectRoot
    $script:work = FullPath $Workspace
    Assert-NoLink $project
    Assert-NoLink $work
    $runRoot = Join-Path $project '.harness-runs'
    $relativeWork = $work.Substring([Math]::Min($work.Length, $runRoot.Length))
    if (-not $work.StartsWith($runRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or
        $relativeWork -notmatch '^[\\/][a-zA-Z0-9_-]+[\\/]project$') {
        throw 'Workspace must be an exact <ProjectRoot>/.harness-runs/<run-id>/project path.'
    }
    $run = Split-Path $work -Parent
    $manifestPath = Join-Path $run 'manifest.json'
    $receiptPath = Join-Path $run 'receipt.json'
    $resultPath = Join-Path $run 'method-result.json'
    $logPath = Join-Path $run 'unity.log'

    if ($Action -eq 'Prepare') {
        if (Test-Path -LiteralPath $work) { throw 'Workspace already exists; use a new run id.' }
        if (Test-Path -LiteralPath $manifestPath) { throw 'Manifest already exists; use a new run id.' }
        $allowed = Validate-Targets $Targets $project
        if (Test-Path -LiteralPath (Join-Path $project 'Assets/Editor/SceneWork')) { throw 'Assets/Editor/SceneWork is reserved for isolated helpers.' }
        $baseline = SourceMap $project
        $targetBefore = @{}
        foreach ($path in $allowed.Keys) { $targetBefore[$path] = $baseline[$path] }
        [void][IO.Directory]::CreateDirectory($work)
        foreach ($name in @('Assets', 'Packages', 'ProjectSettings')) { [void][IO.Directory]::CreateDirectory((Join-Path $work $name)) }
        foreach ($path in $baseline.Keys) {
            $destination = Join-Path $work $path
            [void][IO.Directory]::CreateDirectory((Split-Path $destination -Parent))
            [IO.File]::Copy((Join-Path $project $path), $destination, $false)
        }
        Assert-Same $baseline (SourceMap $project) 'Original during prepare'
        Assert-Same $baseline (SourceMap $work) 'Copied source'
        [void][IO.Directory]::CreateDirectory((Join-Path $work 'Assets/Editor/SceneWork'))
        Write-NewJson $manifestPath ([ordered]@{
            schemaVersion = 1; projectRoot = $project; workspace = $work
            preparedAtUtc = [DateTime]::UtcNow.ToString('o'); targets = @($Targets)
            baseline = @(To-Records $baseline); targetBefore = @(To-Records $targetBefore)
        })
        Write-Output "Prepared $work"
        Write-Output "ManifestSha256=$(Hash $manifestPath)"
        exit 0
    }

    $manifest = Read-PinnedJson $manifestPath $ManifestSha256
    if ($manifest.schemaVersion -ne 1 -or $manifest.projectRoot -ne $project -or $manifest.workspace -ne $work) {
        throw 'Manifest identity does not match this project/workspace.'
    }
    $allowed = Validate-Targets $manifest.targets $project
    $baseline = From-Records $manifest.baseline
    Assert-Same $baseline (SourceMap $project) 'Original source since prepare'
    foreach ($record in $manifest.targetBefore) {
        if ((Hash (Join-Path $project $record.path)) -ne $record.sha256) { throw "Original target changed: $($record.path)" }
    }
    Assert-EditorClosed $work

    if ($Action -eq 'Run') {
        if (-not $UnityPath -or -not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) { throw 'An explicit Unity executable is required.' }
        if ($ExecuteMethod -notmatch '^[A-Za-z_][A-Za-z0-9_.]*\.[A-Za-z_][A-Za-z0-9_]*$') { throw 'An explicit static Editor ExecuteMethod is required.' }
        foreach ($path in @($receiptPath, $resultPath, $logPath, (Join-Path $run 'run-start.json'))) {
            Assert-NoLink $path
            if (Test-Path -LiteralPath $path) { throw "Existing run evidence; prepare a new run: $path" }
        }
        $before = SourceMap $work
        $helpers = @{}
        foreach ($path in $before.Keys) { if ($path -cmatch '^Assets/Editor/SceneWork/[^/]+\.cs$') { $helpers[$path] = $before[$path] } }
        if ($helpers.Count -eq 0) { throw 'Place the authorized Editor helper in Assets/Editor/SceneWork first.' }
        Write-NewJson (Join-Path $run 'run-start.json') ([ordered]@{
            manifestSha256 = $ManifestSha256; executeMethod = $ExecuteMethod
            startedAtUtc = [DateTime]::UtcNow.ToString('o'); helpers = @(To-Records $helpers)
        })
        $arguments = @('-batchmode', '-nographics', '-quit', '-projectPath', $work,
            '-executeMethod', $ExecuteMethod, '-sceneWorkResultPath', $resultPath, '-logFile', $logPath)
        foreach ($argument in $arguments) { if ($argument.Contains('"')) { throw 'Quotes are not allowed in Unity arguments.' } }
        $quoted = @($arguments | ForEach-Object { '"' + $_ + '"' }) -join ' '
        $process = Start-Process -FilePath $UnityPath -ArgumentList $quoted -WindowStyle Hidden -PassThru
        $finished = $process.WaitForExit($TimeoutSeconds * 1000)
        if (-not $finished) {
            $process.Kill()
            [void]$process.WaitForExit(10000)
            throw "Unity timed out after $TimeoutSeconds seconds. No promotion. Log: $logPath"
        }
        $process.Refresh()
        if ($process.ExitCode -ne 0) { throw "Unity exited $($process.ExitCode). No promotion. Log: $logPath" }
        if (-not (Test-Path -LiteralPath $logPath -PathType Leaf)) { throw 'Unity log is missing.' }
        Assert-NoLink $resultPath
        $result = Get-Content -LiteralPath $resultPath -Raw -Encoding UTF8 | ConvertFrom-Json
        if ($result.success -isnot [bool] -or -not $result.success) { throw 'Editor method did not report success:true.' }
        $candidate = SourceMap $work
        foreach ($path in $helpers.Keys) { if ($candidate[$path] -ne $helpers[$path]) { throw "Executed helper changed: $path" } }
        Assert-Same $baseline (SourceMap $project) 'Original during run'
        Assert-Candidate $baseline $candidate $allowed
        Write-NewJson $receiptPath ([ordered]@{
            schemaVersion = 1; manifestSha256 = $ManifestSha256; exitCode = 0
            executeMethod = $ExecuteMethod; finishedAtUtc = [DateTime]::UtcNow.ToString('o')
            resultSha256 = Hash $resultPath; logSha256 = Hash $logPath
            helpers = @(To-Records $helpers); candidate = @(To-Records $candidate)
        })
        Write-Output "Run passed. Log: $logPath"
        Write-Output "ReceiptSha256=$(Hash $receiptPath)"
        exit 0
    }

    Assert-EditorClosed $project
    $receipt = Read-PinnedJson $receiptPath $ReceiptSha256
    if ($receipt.schemaVersion -ne 1 -or $receipt.manifestSha256 -ne $ManifestSha256 -or $receipt.exitCode -ne 0) { throw 'Receipt does not describe a successful run of this manifest.' }
    if ((Hash $resultPath) -ne $receipt.resultSha256 -or (Hash $logPath) -ne $receipt.logSha256) { throw 'Run evidence changed.' }
    $candidate = SourceMap $work
    Assert-Same (From-Records $receipt.candidate) $candidate 'Workspace after run'
    Assert-Candidate $baseline $candidate $allowed
    $changed = @($allowed.Keys | Where-Object { $baseline[$_] -ne $candidate[$_] } | Sort-Object)
    $backup = Join-Path $run 'backups'
    if (Test-Path -LiteralPath $backup) { throw 'Apply already attempted; inspect its evidence instead of retrying.' }
    [void][IO.Directory]::CreateDirectory($backup)
    foreach ($path in $changed) {
        $original = Join-Path $project $path
        if (Test-Path -LiteralPath $original -PathType Leaf) {
            $saved = Join-Path $backup $path
            [void][IO.Directory]::CreateDirectory((Split-Path $saved -Parent))
            [IO.File]::Copy($original, $saved, $false)
        }
    }
    Write-NewJson (Join-Path $run 'apply-start.json') ([ordered]@{
        manifestSha256 = $ManifestSha256; receiptSha256 = $ReceiptSha256
        startedAtUtc = [DateTime]::UtcNow.ToString('o'); paths = $changed
    })
    Assert-Same $baseline (SourceMap $project) 'Original before apply'
    Assert-EditorClosed $project
    foreach ($path in $changed) {
        $original = Join-Path $project $path
        Assert-NoLink $original
        if ((Hash $original) -ne $baseline[$path]) { throw "Concurrent original edit: $path; inspect apply-start.json and backups." }
        [IO.File]::Copy((Join-Path $work $path), $original, $baseline.ContainsKey($path))
        if ((Hash $original) -ne $candidate[$path]) { throw "Copy verification failed: $path; inspect backups." }
    }
    Write-NewJson (Join-Path $run 'applied.json') ([ordered]@{
        receiptSha256 = $ReceiptSha256; finishedAtUtc = [DateTime]::UtcNow.ToString('o'); paths = $changed
    })
    Write-Output "Applied $($changed.Count) file(s). Backups: $backup"
    $changed | Write-Output
    exit 0
}
catch {
    Write-Error -Message $_.Exception.Message -ErrorAction Continue
    exit 1
}
