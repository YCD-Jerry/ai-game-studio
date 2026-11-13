# Cooperative editor and import locks. Use editor_lock.py directly for recovery.
param(
    [ValidateSet('take', 'refresh', 'release', 'status')][string]$Action = 'status',
    [string]$Owner = $env:GAME_STUDIO_SESSION,
    [string]$Note = '',
    [ValidateSet('editor', 'import')][string]$Name = 'editor',
    [double]$WaitMinutes = 40,
    [string]$ProjectPath = $env:UNITY_PROJECT_PATH,
    [string]$LockDir = $env:GAME_STUDIO_LOCK_DIR,
    [int]$HolderPid = 0
)
$python = $env:GAME_STUDIO_PYTHON
if (-not $python) { $python = 'python' }
if (-not (Get-Command $python -CommandType Application -ErrorAction SilentlyContinue)) {
    Write-Output 'lock: Python 3 is required'
    exit 2
}
$wait = $WaitMinutes.ToString([Globalization.CultureInfo]::InvariantCulture)
$lockArgs = @($Action, "--name=$Name", "--wait-minutes=$wait")
if ($Owner) { $lockArgs += "--owner=$Owner" }
if ($Note) { $lockArgs += "--note=$Note" }
if ($ProjectPath) { $lockArgs += "--project=$ProjectPath" }
if ($LockDir) { $lockArgs += "--lock-dir=$LockDir" }
if ($HolderPid) { $lockArgs += "--holder-pid=$HolderPid" }
& $python (Join-Path $PSScriptRoot 'editor_lock.py') @lockArgs
exit $LASTEXITCODE
