<#
game-studio guard.ps1 -- may this session touch the shared Unity editor right now?

Run it before EVERY recompile, rebuild, reimport, AssetDatabase.Refresh, or file copy into Assets/
(a file landing in Assets/ during Play imports or recompiles mid-Play just the same).
It never changes anything: it reads two lock files and asks the editor for its play state.

  & .\guard.ps1 -Owner ch3            # or positional: & .\guard.ps1 ch3
  if ($LASTEXITCODE -ne 0) { return }  # 3/5/6: wait and retry later; 4: look before retrying

Exit codes (shared with guard.sh, lock.*, round.ps1):
  0  ok: not playing, no import lock, editor lock free or yours
  3  the editor is in Play mode (or entering it): the human is playtesting, do not touch it
  4  play state unknown (editor not running, CLI failed, eval timed out, modal dialog up): do not touch it
  5  import lock present and younger than the import stale age: a long import is running
  6  editor lock held by ANOTHER owner and younger than the editor stale age: wait your turn

Settings (parameter, else environment variable, else default):
  -Owner        GAME_STUDIO_SESSION           your session name, single token, ASCII recommended.
                                              Your own editor lock does not stop you.
  -ProjectPath  UNITY_PROJECT_PATH            required
  -LockDir      GAME_STUDIO_LOCK_DIR          default <project>\.game-studio\locks  (add .game-studio/locks to .gitignore)
  -EvalDir      GAME_STUDIO_EVAL_DIR          folder holding playing.cs; default: this script's folder.
                                              Must be readable by the Unity editor process (not a sandboxed temp dir).
  -EditorStaleMin  GAME_STUDIO_EDITOR_LOCK_STALE_MIN  default 20  (a lock not rewritten for this long is ignored)
  -ImportStaleMin  GAME_STUDIO_IMPORT_LOCK_STALE_MIN  default 30
  -EvalTimeoutSec  default 60 (hard limit on the play-state query)
  UNITY_CLI     path to the unity CLI; default: `unity` on PATH

Lock files: <LockDir>\editor.lock and <LockDir>\import.lock, one line "<owner> <ISO time> <note>".
Age is the file's LastWriteTime, so long rounds must refresh the lock (lock.ps1 -Action refresh) between steps.
Owner match is exact on the first token (the original used StartsWith, so "ch1" also matched "ch10").
#>
param(
    [Parameter(Position = 0)][string]$Owner = $env:GAME_STUDIO_SESSION,
    [string]$ProjectPath = $env:UNITY_PROJECT_PATH,
    [string]$LockDir = $env:GAME_STUDIO_LOCK_DIR,
    [string]$EvalDir = $env:GAME_STUDIO_EVAL_DIR,
    [double]$EditorStaleMin = 0,
    [double]$ImportStaleMin = 0,
    [int]$EvalTimeoutSec = 60
)
if (-not $ProjectPath) { Write-Output "guard: no project path (set UNITY_PROJECT_PATH or pass -ProjectPath)"; exit 4 }
if (-not $LockDir) { $LockDir = Join-Path $ProjectPath '.game-studio\locks' }
if (-not $EvalDir) { $EvalDir = $PSScriptRoot }
if ($EditorStaleMin -le 0) { $EditorStaleMin = 20; if ($env:GAME_STUDIO_EDITOR_LOCK_STALE_MIN) { $EditorStaleMin = [double]$env:GAME_STUDIO_EDITOR_LOCK_STALE_MIN } }
if ($ImportStaleMin -le 0) { $ImportStaleMin = 30; if ($env:GAME_STUDIO_IMPORT_LOCK_STALE_MIN) { $ImportStaleMin = [double]$env:GAME_STUDIO_IMPORT_LOCK_STALE_MIN } }

function Read-Lock([string]$Path) {
    # Returns @{ Owner; Text; AgeMin } or $null. Tolerates a UTF-8 BOM and CRLF.
    if (-not (Test-Path -LiteralPath $Path)) { return $null }
    try {
        $item = Get-Item -LiteralPath $Path -ErrorAction Stop
        $text = ([IO.File]::ReadAllText($Path, [Text.Encoding]::UTF8)).Trim([char]0xFEFF, ' ', "`r", "`n", "`t")
    } catch { return $null }   # deleted between Test-Path and read: treat as free
    $first = ($text -split "`n")[0].Trim()
    $tok = ($first -split '\s+')[0]
    return @{ Owner = $tok; Text = $first; AgeMin = ((Get-Date) - $item.LastWriteTime).TotalMinutes }
}

# 1. import lock (exit 5)
$imp = Read-Lock (Join-Path $LockDir 'import.lock')
if ($imp -and $imp.AgeMin -lt $ImportStaleMin) {
    Write-Output ("IMPORT RUNNING (" + $imp.Text + ", " + [int]$imp.AgeMin + " min) - not touching the editor")
    exit 5
}

# 2. editor lock (exit 6)
$ed = Read-Lock (Join-Path $LockDir 'editor.lock')
if ($ed -and $ed.AgeMin -lt $EditorStaleMin -and -not ($Owner -and $ed.Owner -eq $Owner)) {
    Write-Output ("EDITOR BUSY (" + $ed.Text + ", " + [int]$ed.AgeMin + " min) - wait")
    exit 6
}

# 3. play state (exit 3 / 4), with a hard time limit so a frozen editor cannot hang the caller.
$unity = $env:UNITY_CLI
if (-not $unity) {
    $cmd = Get-Command unity -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($cmd) { $unity = $cmd.Source }
}
if (-not $unity) { Write-Output "guard: unity CLI not found (set UNITY_CLI)"; exit 4 }
$probe = Join-Path $EvalDir 'playing.cs'
if (-not (Test-Path -LiteralPath $probe)) { Write-Output "guard: missing $probe"; exit 4 }

$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = $unity
$psi.Arguments = 'command script eval_file --file "' + $probe + '" --timeout ' + $EvalTimeoutSec
$psi.UseShellExecute = $false
$psi.RedirectStandardOutput = $true
$psi.RedirectStandardError = $true
$psi.CreateNoWindow = $true
$psi.EnvironmentVariables['UNITY_PROJECT_PATH'] = $ProjectPath
$psi.EnvironmentVariables['UNITY_NO_BANNER'] = '1'
$psi.EnvironmentVariables['UNITY_NO_PAGER'] = '1'
$psi.EnvironmentVariables['UNITY_NON_INTERACTIVE'] = '1'
try { $p = [Diagnostics.Process]::Start($psi) } catch { Write-Output "guard: could not start $unity"; exit 4 }
$o = $p.StandardOutput.ReadToEndAsync(); $e = $p.StandardError.ReadToEndAsync()
if (-not $p.WaitForExit(($EvalTimeoutSec + 30) * 1000)) {
    try { $p.Kill() } catch {}
    Write-Output "Could not read the play state in time (editor frozen? modal dialog?) - not touching it"
    exit 4
}
$r = $o.Result + $e.Result
if ($r -match 'playing=True') { Write-Output "UNITY IS IN PLAY MODE - not touching it"; exit 3 }
if ($r -notmatch 'playing=False') {
    Write-Output "Could not read Unity's play state - not touching it"
    Write-Output (($r -replace '\s+', ' ').Trim() | ForEach-Object { if ($_.Length -gt 300) { $_.Substring(0, 300) } else { $_ } })
    exit 4
}
exit 0
