# Independent file-system fixtures; synthetic receipts here never certify Unity behavior.
[CmdletBinding()]
param([string]$OutputDirectory = '')
$ErrorActionPreference = 'Stop'
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $PSScriptRoot '../../.harness-runs/scene-work-tests' }
$runner = Join-Path $PSScriptRoot 'SceneWorkspace.ps1'
$testRoot = Join-Path ([IO.Path]::GetFullPath($OutputDirectory)) ([Guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($testRoot)
$checks = New-Object 'Collections.Generic.List[object]'

function Write-Fixture([string]$Path, [string]$Content) {
    [void][IO.Directory]::CreateDirectory((Split-Path $Path -Parent))
    [IO.File]::WriteAllText($Path, $Content)
}
function Digest([string]$Path) { return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant() }
function Invoke-Runner($Fixture, [string]$Action, [string[]]$Extra = @()) {
    $arguments = @('-NoProfile','-ExecutionPolicy','Bypass','-File',$runner,'-Action',$Action,
        '-ProjectRoot',$Fixture.root,'-Workspace',$Fixture.work) + $Extra
    # Failed native fixtures emit stderr intentionally; retain logs without throwing in the parent.
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $output = & powershell.exe @arguments 2>&1
    $code = $LASTEXITCODE
    $ErrorActionPreference = $previous
    $output | Out-File -Encoding utf8 (Join-Path $Fixture.root ("$Action-" + [Guid]::NewGuid().ToString('N') + '.log'))
    return $code
}
function New-Fixture([string]$Name, [string]$Target = 'Assets/Scenes/FarmerTest.unity') {
    $root = Join-Path $testRoot $Name
    Write-Fixture (Join-Path $root 'Assets/Scenes/FarmerTest.unity') 'original scene bytes'
    Write-Fixture (Join-Path $root 'Assets/Scenes/FarmerTest.unity.meta') "fileFormatVersion: 2`nguid: 1234567890abcdef1234567890abcdef`n"
    Write-Fixture (Join-Path $root 'Assets/Scripts/Existing.cs') 'existing code'
    Write-Fixture (Join-Path $root 'Packages/manifest.json') '{"dependencies":{}}'
    Write-Fixture (Join-Path $root 'ProjectSettings/ProjectVersion.txt') 'm_EditorVersion: 6000.3.9f1'
    $fixture = @{ root=$root; work=(Join-Path $root '.harness-runs/edit/project'); target=$Target }
    if ((Invoke-Runner $fixture 'Prepare' @('-Targets',$Target)) -ne 0) { throw "Fixture prepare failed: $Name" }
    $fixture.run = Split-Path $fixture.work -Parent
    $fixture.manifest = Digest (Join-Path $fixture.run 'manifest.json')
    return $fixture
}
function New-Receipt($Fixture) {
    Write-Fixture (Join-Path $Fixture.run 'method-result.json') '{"success":true,"fixtureOnly":true}'
    Write-Fixture (Join-Path $Fixture.run 'unity.log') 'SYNTHETIC fixture; not a Unity execution'
    $candidate = foreach ($directory in @('Assets','Packages','ProjectSettings')) {
        foreach ($file in Get-ChildItem -LiteralPath (Join-Path $Fixture.work $directory) -Recurse -File) {
            @{path=$file.FullName.Substring($Fixture.work.Length+1).Replace('\','/');sha256=(Digest $file.FullName)}
        }
    }
    $receipt = @{ schemaVersion=1; manifestSha256=$Fixture.manifest; exitCode=0;
        resultSha256=(Digest (Join-Path $Fixture.run 'method-result.json'));
        logSha256=(Digest (Join-Path $Fixture.run 'unity.log')); candidate=@($candidate);
        helpers=@(); executeMethod='Fixture.Run'; finishedAtUtc=[DateTime]::UtcNow.ToString('o') }
    Write-Fixture (Join-Path $Fixture.run 'receipt.json') ($receipt | ConvertTo-Json -Depth 10)
    $Fixture.receipt = Digest (Join-Path $Fixture.run 'receipt.json')
}
function Apply-Fixture($Fixture) {
    return Invoke-Runner $Fixture 'Apply' @('-ManifestSha256',$Fixture.manifest,'-ReceiptSha256',$Fixture.receipt)
}
function Check([string]$Name, [scriptblock]$Body) {
    try {
        $ok = [bool](& $Body)
        if (-not $ok) { throw 'Unexpected outcome.' }
        $checks.Add(@{id=$Name;success=$true;message='Pass'})
        Write-Output "PASS $Name"
    } catch {
        $checks.Add(@{id=$Name;success=$false;message=$_.Exception.Message})
        Write-Output "FAIL $Name : $($_.Exception.Message)"
    }
}

Check 'prepare-preserves-dirty-bytes' {
    $f=New-Fixture 'prepare'
    (Get-Content (Join-Path $f.work 'Assets/Scenes/FarmerTest.unity') -Raw) -eq 'original scene bytes'
}
Check 'reject-existing-workspace' {
    $f=New-Fixture 'existing'
    (Invoke-Runner $f 'Prepare' @('-Targets',$f.target)) -ne 0
}
Check 'reject-traversal-target' {
    $f=New-Fixture 'traversal'
    $f.work=Join-Path $f.root '.harness-runs/other/project'
    (Invoke-Runner $f 'Prepare' @('-Targets','Assets/Scenes/../../outside.unity')) -ne 0
}
Check 'apply-exact-target-and-backup' {
    $f=New-Fixture 'apply'
    Write-Fixture (Join-Path $f.work $f.target) 'edited scene bytes'
    New-Receipt $f
    (Apply-Fixture $f) -eq 0 -and
        (Get-Content (Join-Path $f.root $f.target) -Raw) -eq 'edited scene bytes' -and
        (Get-Content (Join-Path $f.run ('backups/'+$f.target)) -Raw) -eq 'original scene bytes' -and
        (Get-Content (Join-Path $f.root 'Assets/Scripts/Existing.cs') -Raw) -eq 'existing code'
}
Check 'reject-original-target-conflict' {
    $f=New-Fixture 'conflict'; New-Receipt $f
    Write-Fixture (Join-Path $f.root $f.target) 'new user work'
    (Apply-Fixture $f) -ne 0 -and (Get-Content (Join-Path $f.root $f.target) -Raw) -eq 'new user work'
}
Check 'reject-original-dependency-conflict' {
    $f=New-Fixture 'dependency'; New-Receipt $f
    Write-Fixture (Join-Path $f.root 'Assets/Scripts/Existing.cs') 'new user code'
    (Apply-Fixture $f) -ne 0
}
Check 'reject-stale-candidate' {
    $f=New-Fixture 'stale'; New-Receipt $f
    Write-Fixture (Join-Path $f.work $f.target) 'changed after receipt'
    (Apply-Fixture $f) -ne 0
}
Check 'reject-out-of-scope-source-change' {
    $f=New-Fixture 'scope'
    Write-Fixture (Join-Path $f.work 'Assets/Scripts/Existing.cs') 'unexpected'
    New-Receipt $f
    (Apply-Fixture $f) -ne 0
}
Check 'reject-project-settings-change' {
    $f=New-Fixture 'settings'
    Write-Fixture (Join-Path $f.work 'ProjectSettings/ProjectVersion.txt') 'unexpected'
    New-Receipt $f
    (Apply-Fixture $f) -ne 0
}
Check 'reject-existing-guid-change' {
    $f=New-Fixture 'guid'
    Write-Fixture (Join-Path $f.work ($f.target+'.meta')) "guid: abcdef1234567890abcdef1234567890`n"
    New-Receipt $f
    (Apply-Fixture $f) -ne 0
}
Check 'reject-receipt-tampering' {
    $f=New-Fixture 'receipt'; New-Receipt $f
    Write-Fixture (Join-Path $f.run 'receipt.json') '{}'
    (Apply-Fixture $f) -ne 0
}
Check 'reject-invalid-new-guid' {
    $f=New-Fixture 'invalid-guid' 'Assets/Scenes/New.prefab'
    Write-Fixture (Join-Path $f.work $f.target) 'new prefab'
    Write-Fixture (Join-Path $f.work ($f.target+'.meta')) 'guid: not-a-guid'
    New-Receipt $f
    (Apply-Fixture $f) -ne 0
}
Check 'reject-duplicate-new-guid' {
    $f=New-Fixture 'duplicate-guid' 'Assets/Scenes/New.prefab'
    Write-Fixture (Join-Path $f.work $f.target) 'new prefab'
    Write-Fixture (Join-Path $f.work ($f.target+'.meta')) "guid: 1234567890abcdef1234567890abcdef`n"
    New-Receipt $f
    (Apply-Fixture $f) -ne 0
}
Check 'reject-manifest-tampering' {
    $f=New-Fixture 'manifest'; New-Receipt $f
    Write-Fixture (Join-Path $f.run 'manifest.json') '{}'
    (Apply-Fixture $f) -ne 0
}
Check 'reject-missing-paired-meta' {
    $f=New-Fixture 'missing-meta' 'Assets/Scenes/New.prefab'
    Write-Fixture (Join-Path $f.work $f.target) 'new prefab'
    New-Receipt $f
    (Apply-Fixture $f) -ne 0 -and -not (Test-Path (Join-Path $f.root $f.target))
}
Check 'apply-new-asset-and-paired-meta' {
    $f=New-Fixture 'new-asset' 'Assets/Scenes/New.prefab'
    Write-Fixture (Join-Path $f.work $f.target) 'new prefab'
    Write-Fixture (Join-Path $f.work ($f.target+'.meta')) "guid: abcdef1234567890abcdef1234567890`n"
    New-Receipt $f
    (Apply-Fixture $f) -eq 0 -and (Test-Path (Join-Path $f.root ($f.target+'.meta')))
}
Check 'reject-open-original-editor' {
    $f=New-Fixture 'open'; New-Receipt $f
    $path=Join-Path $f.root 'Temp/UnityLockfile'
    Write-Fixture $path ''
    $lock=[IO.File]::Open($path,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
    try { (Apply-Fixture $f) -ne 0 } finally { $lock.Dispose() }
}
Check 'allow-stale-unlocked-lockfile' {
    $f=New-Fixture 'stale-lock'; New-Receipt $f
    Write-Fixture (Join-Path $f.root 'Temp/UnityLockfile') ''
    (Apply-Fixture $f) -eq 0
}

$pass=@($checks | Where-Object {-not $_.success}).Count -eq 0
$report=@{success=$pass;checks=@($checks.ToArray());testRoot=$testRoot;receiptType='synthetic fixtures, not Unity evidence'}
$report | ConvertTo-Json -Depth 8 | Out-File -Encoding utf8 (Join-Path $testRoot 'results.json')
Write-Output "SceneWorkspace tests: $(@($checks | Where-Object {$_.success}).Count)/$($checks.Count) passed. Results: $testRoot/results.json"
if (-not $pass) { exit 1 }
