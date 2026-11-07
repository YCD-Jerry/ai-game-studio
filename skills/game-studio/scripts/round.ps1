<#
game-studio round.ps1 -- one complete, bounded turn at the shared Unity editor.

  take lock (wait <= WaitMinutes, holding nothing) -> guard -> bring files in -> refresh/recompile -> settle
  -> compile check -> rebuild (menu) -> verify the build really happened -> review shots -> verify the PNGs
  -> [finally] hand_back.cs (save + light scene) -> release lock -> stop dialog watcher

Every step: deadline check (HoldMinutes from taking the lock), lock refresh (abort if lost), and for anything that
writes into Assets/ a fresh guard (exit 3 = the human started playing: stop, do not copy). Nothing waits unbounded.

Examples
  # rebuild a generated scene and shoot it
  .\round.ps1 -Owner ch3 -Note "ch3 bridge" -BuildMenu "Tools/Build/Chapter 3" `
              -BuiltMarker '\[Build\] Built Assets/Scenes/Ch3' -BuiltScene Assets/Scenes/Ch3.unity -ShotSpec D:\wip\ch3\shots.txt
  # bring a delivered model in from a staging folder, no rebuild, just shots
  .\round.ps1 -Owner modeling -StageDir D:\wip\dais\deliver -AssetsDest Assets/Art/Castle -ShotSpec D:\wip\dais\shots.txt
  # code change staged outside Assets (several files that reference each other move in together)
  .\round.ps1 -Owner ui -StageDir D:\wip\ui\Scripts -AssetsDest Assets/Scripts/UI -Recompile

Parameters
  -Owner        session name, one token (GAME_STUDIO_SESSION)
  -Note         text written into the lock so others can see what is running
  -ProjectPath  UNITY_PROJECT_PATH;  -LockDir GAME_STUDIO_LOCK_DIR;  -EvalDir GAME_STUDIO_EVAL_DIR (default: this folder;
                holds playing.cs, refresh.cs, hand_back.cs, review_shots.cs; must be readable by the editor process)
  -WaitMinutes  max wait for the lock (default 40)        -HoldMinutes  total deadline once holding it (default 25)
  -StageDir / -AssetsDest   copy changed files (size or time differ) from a staging folder into Assets/<...>,
                retrying a locked file (error 1224) 6 times; -MirrorDeletes also removes files (and .meta) gone from stage
  -Recompile    ask for a script recompile even without staged files
  -BuildMenu    menu path of a scene builder ("editor invoke_menu"); its reply says success even when the builder threw,
                so the build is only accepted when -BuiltMarker (regex) appears in the console AND/OR -BuiltScene
                (project-relative .unity) has a write time after the build started. Give at least one.
  -ShotSpec     review_shots spec file (copied to <project>\Temp\game-studio\review_shots.txt); see review_shots.cs
  -ShotsEval    a custom shots eval instead of review_shots.cs (then PNG verification needs -ShotsOut)
  -ShotsOut     folder the PNGs land in (default: out= from the spec, else <project>\Temp\game-studio\shots)
  -NoDialogWatch  do not start dialog_watch.ps1 (TryAgain) in the background
  -NoHandBack   skip hand_back.cs (not recommended: the next Play then waits behind big open scenes)

Exit codes: 0 done | 1 lock not taken | 2 bad arguments | 3 guard refused mid-round (Play started) |
            10 compile errors | 11 build not verified | 12 shots failed | 13 deadline or lost lock | 14 editor never settled
Output: one timestamped line per step; the last line is "ROUND <result>".

Not verified outside one Windows 11 / Unity 6000.3 / CLI 1.0.0-beta.14 setup: command group names (`script eval_file`,
`script recompile`, `editor invoke_menu`, `console get`) depend on the Pipeline package version; check `unity command`.
The CLI's human output can be localized, so this script decides from its own eval replies, not from CLI wording.
Killing a CLI call at its time limit does NOT stop work already running inside the editor (a build keeps going);
the hand-back then queues behind it.
#>
param(
    [string]$Owner = $env:GAME_STUDIO_SESSION,
    [string]$Note = 'round',
    [string]$ProjectPath = $env:UNITY_PROJECT_PATH,
    [string]$LockDir = $env:GAME_STUDIO_LOCK_DIR,
    [string]$EvalDir = $env:GAME_STUDIO_EVAL_DIR,
    [double]$WaitMinutes = 40,
    [double]$HoldMinutes = 25,
    [string]$StageDir,
    [string]$AssetsDest,
    [switch]$MirrorDeletes,
    [switch]$Recompile,
    [string]$BuildMenu,
    [string]$BuiltMarker,
    [string]$BuiltScene,
    [int]$BuildTimeoutSec = 900,
    [string]$ShotSpec,
    [string]$ShotsEval,
    [string]$ShotsOut,
    [switch]$NoDialogWatch,
    [switch]$NoHandBack
)
$ErrorActionPreference = 'Continue'
function Say([string]$m) { Write-Output ((Get-Date -Format 'HH:mm:ss') + ' ' + $m) }
function Quit([int]$code, [string]$m) { Say ("ROUND " + $m); exit $code }

# ---- arguments --------------------------------------------------------------------------------------------
if (-not $Owner -or $Owner -match '\s') { Quit 2 "bad arguments: -Owner must be one token" }
if (-not $ProjectPath -or -not (Test-Path -LiteralPath (Join-Path $ProjectPath 'Assets'))) { Quit 2 "bad arguments: -ProjectPath is not a Unity project" }
$ProjectPath = (Resolve-Path -LiteralPath $ProjectPath).Path
if (-not $LockDir) { $LockDir = Join-Path $ProjectPath '.game-studio\locks' }
if (-not $EvalDir) { $EvalDir = $PSScriptRoot }
foreach ($f in 'playing.cs', 'refresh.cs', 'hand_back.cs') { if (-not (Test-Path -LiteralPath (Join-Path $EvalDir $f))) { Quit 2 "bad arguments: $f missing in $EvalDir" } }
if ($StageDir -and -not $AssetsDest) { Quit 2 "bad arguments: -StageDir needs -AssetsDest" }
if ($AssetsDest -and -not ($AssetsDest -replace '\\', '/').StartsWith('Assets/')) { Quit 2 "bad arguments: -AssetsDest must start with Assets/" }
if ($StageDir -and -not (Test-Path -LiteralPath $StageDir)) { Quit 2 "bad arguments: no $StageDir" }
if ($BuildMenu -and -not $BuiltMarker -and -not $BuiltScene) { Quit 2 "bad arguments: -BuildMenu needs -BuiltMarker and/or -BuiltScene (the menu reply is not proof)" }
if ($ShotSpec -and -not (Test-Path -LiteralPath $ShotSpec)) { Quit 2 "bad arguments: no $ShotSpec" }
$env:UNITY_PROJECT_PATH = $ProjectPath
$env:GAME_STUDIO_LOCK_DIR = $LockDir
$env:GAME_STUDIO_EVAL_DIR = $EvalDir

$unity = $env:UNITY_CLI
if (-not $unity) { $c = Get-Command unity -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1; if ($c) { $unity = $c.Source } }
if (-not $unity) { Quit 2 "bad arguments: unity CLI not found (set UNITY_CLI)" }

# ---- helpers ----------------------------------------------------------------------------------------------
function Quote([string]$a) {
    if ($a -ne '' -and $a -notmatch '[\s"]') { return $a }
    return '"' + (($a -replace '(\\*)"', '$1$1\"') -replace '(\\+)$', '$1$1') + '"'
}
# Runs the unity CLI with a hard wall-clock limit; returns stdout+stderr as one string ("TIMEOUT ..." when killed).
function UnityCli([string[]]$CliArgs, [int]$HardSec = 180) {
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $unity
    $psi.Arguments = ($CliArgs | ForEach-Object { Quote $_ }) -join ' '
    $psi.UseShellExecute = $false; $psi.RedirectStandardOutput = $true; $psi.RedirectStandardError = $true; $psi.CreateNoWindow = $true
    foreach ($kv in @(@('UNITY_PROJECT_PATH', $ProjectPath), @('UNITY_NO_BANNER', '1'), @('UNITY_NO_PAGER', '1'), @('UNITY_NON_INTERACTIVE', '1'))) { $psi.EnvironmentVariables[$kv[0]] = $kv[1] }
    $p = [Diagnostics.Process]::Start($psi)
    $o = $p.StandardOutput.ReadToEndAsync(); $e = $p.StandardError.ReadToEndAsync()
    if (-not $p.WaitForExit($HardSec * 1000)) { try { $p.Kill() } catch {}; return "TIMEOUT after $HardSec s: $($CliArgs -join ' ')" }
    $p.WaitForExit()
    return ($o.Result + $e.Result)
}
function Eval([string]$file, [int]$sec = 300) {
    if (-not [IO.Path]::IsPathRooted($file)) { $file = Join-Path $EvalDir $file }
    return UnityCli @('command', 'script', 'eval_file', '--file', $file, '--timeout', "$sec") ($sec + 60)
}
function Short([string]$s, [int]$n = 400) { $t = ($s -replace '\s+', ' ').Trim(); if ($t.Length -gt $n) { $t.Substring(0, $n) + '...' } else { $t } }
function Guard { $null = & (Join-Path $PSScriptRoot 'guard.ps1') -Owner $Owner -ProjectPath $ProjectPath -LockDir $LockDir -EvalDir $EvalDir; return $LASTEXITCODE }
$script:deadline = $null
function Step([string]$name) {
    if ((Get-Date) -gt $script:deadline) { throw "DEADLINE before '$name'" }
    $null = & (Join-Path $PSScriptRoot 'lock.ps1') -Action refresh -Owner $Owner -Note "$Note : $name" -ProjectPath $ProjectPath -LockDir $LockDir
    if ($LASTEXITCODE -ne 0) { throw "LOST LOCK before '$name'" }
    Say "== $name"
}
function SecondsLeft { [int][math]::Max(30, ($script:deadline - (Get-Date)).TotalSeconds) }
# Settled = not compiling, not importing, not playing, three polls in a row (5 s apart), bounded by the deadline.
function Settle([int]$maxSec = 600) {
    $until = (Get-Date).AddSeconds([math]::Min($maxSec, (SecondsLeft)))
    $ok = 0; $r = ''
    while ((Get-Date) -lt $until -and $ok -lt 3) {
        Start-Sleep -Seconds 5
        $r = Eval 'playing.cs' 60
        if ($r -match 'playing=True') { $script:code = 3; throw "PLAY STARTED while settling" }
        if ($r -match 'compiling=False' -and $r -match 'updating=False') { $ok++ } else { $ok = 0 }
    }
    if ($ok -lt 3) { $script:code = 14; throw "EDITOR NEVER SETTLED: $(Short $r 200)" }
    return $r
}

# ---- 1. take the lock (holding nothing while waiting) ------------------------------------------------------
$t0 = Get-Date
Say "waiting for the editor (max $WaitMinutes min) as $Owner"
& (Join-Path $PSScriptRoot 'lock.ps1') -Action take -Owner $Owner -Note $Note -WaitMinutes $WaitMinutes -ProjectPath $ProjectPath -LockDir $LockDir | ForEach-Object { Say $_ }
if ($LASTEXITCODE -ne 0) { Quit 1 "lock not taken" }
$script:deadline = (Get-Date).AddMinutes($HoldMinutes)
$script:code = 0
$result = 'done'
$watch = $null
if (-not $NoDialogWatch -and $env:OS -eq 'Windows_NT') {
    $watch = Start-Job -FilePath (Join-Path $PSScriptRoot 'dialog_watch.ps1') -ArgumentList 'TryAgain', ($HoldMinutes + 10)
}

try {
    # ---- 2. bring files in (guard first: nothing lands in Assets/ during Play) ---------------------------------
    $broughtIn = 0
    if ($StageDir) {
        Step 'bring in'
        $g = Guard; if ($g -ne 0) { $script:code = 3; throw "guard said $g just before copying: not copying" }
        $dest = Join-Path $ProjectPath ($AssetsDest -replace '/', '\')
        New-Item -ItemType Directory -Force -Path $dest | Out-Null
        foreach ($src in Get-ChildItem -LiteralPath $StageDir -File) {
            $to = Join-Path $dest $src.Name
            $old = Get-Item -LiteralPath $to -ErrorAction SilentlyContinue
            if ($old -and $old.Length -eq $src.Length -and $old.LastWriteTime -eq $src.LastWriteTime) { continue }
            $copied = $false
            for ($t = 0; $t -lt 6 -and -not $copied; $t++) {
                try { Copy-Item -LiteralPath $src.FullName -Destination $to -Force -ErrorAction Stop; $copied = $true }
                catch { Say "copy retry $($src.Name): $($_.Exception.Message)"; Start-Sleep -Seconds 10 }
            }
            if (-not $copied) { throw "COPY FAILED $($src.Name)" }
            $broughtIn++
        }
        if ($MirrorDeletes) {
            foreach ($f in Get-ChildItem -LiteralPath $dest -File | Where-Object { $_.Extension -ne '.meta' }) {
                if (-not (Test-Path -LiteralPath (Join-Path $StageDir $f.Name))) {
                    Say "removed $($f.Name)"; $broughtIn++
                    Remove-Item -LiteralPath $f.FullName -Force
                    Remove-Item -LiteralPath ($f.FullName + '.meta') -Force -ErrorAction SilentlyContinue
                }
            }
        }
        Say "brought in $broughtIn file(s)"
    }

    # ---- 3. refresh / recompile, settle, compile check ---------------------------------------------------------
    if ($broughtIn -gt 0 -or $Recompile) {
        Step 'refresh + compile'
        $g = Guard; if ($g -ne 0) { $script:code = 3; throw "guard said $g before refresh" }
        Say (Short (Eval 'refresh.cs' 600))
        if ($Recompile) { Say (Short (UnityCli @('command', 'script', 'recompile') 300) 200) }
        $st = Settle 900
        if ($st -match 'compileFailed=True') {
            $c = UnityCli @('command', 'console', 'get') 120
            [regex]::Matches($c, 'error CS\d+[^"\r\n]{0,220}') | Select-Object -First 8 | ForEach-Object { Say $_.Value }
            $script:code = 10; throw "COMPILE ERRORS: not building on old code"
        }
        if ($st -notmatch 'compileFailed=False') { $script:code = 10; throw "could not read the compile state: $(Short $st 200)" }
        Say "compiled clean"
    }

    # ---- 4. rebuild and verify it really happened --------------------------------------------------------------
    if ($BuildMenu) {
        Step 'build'
        $g = Guard; if ($g -ne 0) { $script:code = 3; throw "guard said $g before the build" }
        $buildStart = Get-Date
        $hard = [math]::Min($BuildTimeoutSec + 60, (SecondsLeft))
        Say (Short (UnityCli @('command', 'editor', 'invoke_menu', '--path', $BuildMenu, '--timeout', "$BuildTimeoutSec") $hard) 200)
        $null = Settle 600
        $c = UnityCli @('command', 'console', 'get') 120
        $okMarker = $true; $okScene = $true
        if ($BuiltMarker) { $okMarker = $c -match $BuiltMarker; Say "marker '$BuiltMarker' found: $okMarker" }
        if ($BuiltScene) {
            $sf = Get-Item -LiteralPath (Join-Path $ProjectPath ($BuiltScene -replace '/', '\')) -ErrorAction SilentlyContinue
            $okScene = $sf -and $sf.LastWriteTime -gt $buildStart
            Say "scene written after build start: $okScene"
        }
        if (-not ($okMarker -and $okScene)) {
            [regex]::Matches($c, '[A-Za-z.]*Exception[^"\r\n]{0,300}') | Select-Object -First 4 | ForEach-Object { Say $_.Value }
            $script:code = 11; throw "BUILD NOT VERIFIED (the builder may have thrown; an Untitled scene may be open)"
        }
        Say "build verified"
    }

    # ---- 5. review shots, then check the files themselves --------------------------------------------------------
    if ($ShotSpec -or $ShotsEval) {
        Step 'shots'
        $shotStart = Get-Date
        $outDir = $ShotsOut
        if ($ShotSpec) {
            $tmp = Join-Path $ProjectPath 'Temp\game-studio'
            New-Item -ItemType Directory -Force -Path $tmp | Out-Null
            Copy-Item -LiteralPath $ShotSpec -Destination (Join-Path $tmp 'review_shots.txt') -Force
            if (-not $outDir) {
                $o = Get-Content -LiteralPath $ShotSpec | Where-Object { $_ -match '^\s*out\s*=' } | Select-Object -First 1
                if ($o) { $outDir = ($o -replace '^\s*out\s*=\s*', '').Trim() } else { $outDir = Join-Path $tmp 'shots' }
            }
        }
        $file = 'review_shots.cs'; if ($ShotsEval) { $file = $ShotsEval }
        $r = Eval $file ([math]::Min(900, (SecondsLeft)))
        ($r -split '\\n|\r?\n') | Where-Object { $_ -match '^(OK|FAIL|note|ERROR|shots=|scene |animators)' -or $_ -match 'shots=' } | ForEach-Object { Say (Short $_ 300) }
        if ($r -match 'TIMEOUT after' -or $r -cmatch 'ERROR ') { $script:code = 12; throw "SHOTS FAILED: $(Short $r 300)" }
        if ($outDir) {
            $fresh = @(Get-ChildItem -LiteralPath $outDir -Filter *.png -ErrorAction SilentlyContinue | Where-Object { $_.LastWriteTime -gt $shotStart -and $_.Length -gt 1000 })
            Say "fresh PNGs in ${outDir}: $($fresh.Count)"
            $m = [regex]::Match($r, 'shots=(\d+)')
            if ($fresh.Count -eq 0 -or ($m.Success -and [int]$m.Groups[1].Value -ne $fresh.Count)) { $script:code = 12; throw "SHOTS NOT VERIFIED (reply and files disagree)" }
        }
        if ($r -cmatch ' (FLAT|CYAN)') { Say "WARNING: some shots look flat or cyan (async shader placeholder?): look before judging" }
        if ($r -match 'more=([1-9]\d*)') { Say "NOTE: $($Matches[1]) shot(s) left: run again with first=<next index> in the spec" }
    }
}
catch {
    if ($script:code -eq 0) { $script:code = 13 }
    $result = "failed: " + $_.Exception.Message
}
finally {
    # ---- 6. hand back (save + light scene), release, stop the watcher. Always, even after a failure or deadline.
    if (-not $NoHandBack) {
        $hb = Eval 'hand_back.cs' 300
        Say ("hand back: " + (Short $hb 300))
        if ($hb -notmatch '\bOK\b') { Say "WARNING: hand-back did not report OK: look at the editor (Untitled scene? modal dialog? dialog_watch.ps1 -Mode List)" }
    }
    & (Join-Path $PSScriptRoot 'lock.ps1') -Action release -Owner $Owner -ProjectPath $ProjectPath -LockDir $LockDir | ForEach-Object { Say $_ }
    if ($watch) { Stop-Job $watch -ErrorAction SilentlyContinue; Receive-Job $watch -ErrorAction SilentlyContinue | ForEach-Object { Say "dialog watch: $_" }; Remove-Job $watch -Force -ErrorAction SilentlyContinue }
    Say ("total " + [int]((Get-Date) - $t0).TotalMinutes + " min")
}
Quit $script:code $result
