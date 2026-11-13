# Read Unity state and cooperative locks. See editor-safety.md for exit codes.
param(
    [Parameter(Position = 0)][string]$Owner = $env:GAME_STUDIO_SESSION,
    [string]$ProjectPath = $env:UNITY_PROJECT_PATH,
    [string]$LockDir = $env:GAME_STUDIO_LOCK_DIR,
    [string]$EvalDir = $env:GAME_STUDIO_EVAL_DIR,
    [double]$EditorStaleMin = 0,
    [double]$ImportStaleMin = 0,
    [int]$EvalTimeoutSec = 0
)
$python = $env:GAME_STUDIO_PYTHON
if (-not $python) { $python = 'python' }
if (-not (Get-Command $python -CommandType Application -ErrorAction SilentlyContinue)) {
    Write-Output 'guard: Python 3 is required'
    exit 4
}
$probeArgs = @('guard')
if ($Owner) { $probeArgs += "--owner=$Owner" }
if ($ProjectPath) { $probeArgs += "--project=$ProjectPath" }
if ($LockDir) { $probeArgs += "--lock-dir=$LockDir" }
if ($EvalDir) { $probeArgs += "--eval-dir=$EvalDir" }
if ($EvalTimeoutSec) { $probeArgs += "--timeout=$EvalTimeoutSec" }
# Keep accepting the old age flags. Age never unlocks a record.
& $python (Join-Path $PSScriptRoot 'editor_guard.py') @probeArgs
$code = $LASTEXITCODE
if ($code -notin 0,3,4,5,6,7) { exit 4 }
exit $code
