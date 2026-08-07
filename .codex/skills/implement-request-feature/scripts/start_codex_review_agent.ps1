[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RepoRoot,

    [Parameter(Mandatory = $true)]
    [string]$ImplementationSummary,

    [Parameter(Mandatory = $true)]
    [string]$ChangedFiles,

    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'

$resolvedRoot = (Resolve-Path -LiteralPath $RepoRoot).Path
$reviewSkill = Join-Path $resolvedRoot '.codex\skills\reviewing-npc-work-code\SKILL.md'

if (-not (Test-Path -LiteralPath $reviewSkill -PathType Leaf)) {
    throw "Required review skill was not found: $reviewSkill"
}

$codexCommand = Get-Command 'codex.cmd' -ErrorAction SilentlyContinue
if ($null -eq $codexCommand) {
    $codexCommand = Get-Command 'codex' -ErrorAction SilentlyContinue
}
if ($null -eq $codexCommand) {
    throw 'Codex CLI was not found on PATH.'
}

$runDirectory = Join-Path $resolvedRoot '.codex\agent-runs'
$runId = Get-Date -Format 'yyyyMMdd-HHmmss-fff'
$promptPath = Join-Path $runDirectory "$runId-prompt.md"
$outputPath = Join-Path $runDirectory "$runId-output.log"
$errorPath = Join-Path $runDirectory "$runId-error.log"
$finalPath = Join-Path $resolvedRoot 'PublicMD\Code_Evaluation_Result.md'

$prompt = @"
`$reviewing-npc-work-code

You are an expert Unity code reviewer specializing in NPC/worker architecture, responsibility boundaries, dependency direction, and serialized-reference safety. Review only; do not implement fixes.

Implementation summary and validation:
$ImplementationSummary

Changed files:
$ChangedFiles

Review priorities, in order:
1. Architecture and responsibility placement
2. Correctness, lifecycle, cancellation, and regression risks
3. Unity scene, prefab, component, and serialized-reference safety
4. Code convention, maintainability, dead code, and magic values

When invoked:
1. Read the reviewing-npc-work-code skill and the documents below.
2. Run git status, git diff, and targeted read/search commands.
3. Focus on changed files and inspect directly affected dependency surfaces.
4. Verify every claim from repository evidence.

Project documents:
- PublicMD/ARCHITECTURE.md
- PublicMD/ProjectStructure.md
- PublicMD/CodeConvention.md

For each issue provide Severity, Category, Location, Evidence, Description, Recommended fix, and Impact if unfixed. Include reviewed scope, verification limits, positive observations, and a final verdict. State explicitly when no issue is found at a priority level.

Use the reviewing-npc-work-code audit methodology. Your sandbox is read-only: do not attempt to modify any file. Return only the complete Markdown body intended for PublicMD/Code_Evaluation_Result.md as the final response. The launcher owns writing that final response to the report file.
"@

if ($DryRun) {
    [pscustomobject]@{
        Agent = 'Codex'
        Mode = 'Background independent reviewer'
        WorkingDirectory = $resolvedRoot
        Executable = $codexCommand.Source
        Prompt = $prompt
    }
    return
}

New-Item -ItemType Directory -Path $runDirectory -Force | Out-Null
Set-Content -LiteralPath $promptPath -Value $prompt -Encoding utf8

$arguments = @(
    '-a', 'never',
    '-s', 'read-only',
    '-C', $resolvedRoot,
    'exec',
    '--ephemeral',
    '--output-last-message', $finalPath,
    '-'
)

$process = Start-Process `
    -FilePath $codexCommand.Source `
    -ArgumentList $arguments `
    -WorkingDirectory $resolvedRoot `
    -RedirectStandardInput $promptPath `
    -RedirectStandardOutput $outputPath `
    -RedirectStandardError $errorPath `
    -WindowStyle Hidden `
    -PassThru

[pscustomobject]@{
    Agent = 'Codex'
    ProcessId = $process.Id
    Status = 'Started'
    PromptPath = $promptPath
    OutputPath = $outputPath
    ErrorPath = $errorPath
    FinalMessagePath = $finalPath
}
