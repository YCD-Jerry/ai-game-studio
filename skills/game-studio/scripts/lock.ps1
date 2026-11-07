<#
game-studio lock.ps1 -- take, refresh, release or inspect the shared editor lock (or the import lock).

One session drives the editor at a time. Queued scripts that all fire when the editor comes back froze it
(a chain of black screens); a script that waited forever while holding the lock blocked every session for an hour.
So: wait for the lock HOLDING NOTHING, with a bound; once you hold it, every step has a deadline; refresh it
between steps; release it in `finally`.

  .\lock.ps1 -Action take    -Owner ch3 -Note "rebuild ch3 + shots" [-WaitMinutes 40]
  .\lock.ps1 -Action refresh -Owner ch3 [-Note "shots"]      # rewrite the time before each long step
  .\lock.ps1 -Action release -Owner ch3                      # only removes the lock if it is yours
  .\lock.ps1 -Action status
  -Name import  works on import.lock instead of editor.lock (for long imports that every rebuild must wait for)

take:  loops until WaitMinutes: when the lock is free (or stale) and guard.ps1 returns 0 (not playing, no import,
       editor lock free), it creates the file atomically (CreateNew: fails if another session won the race),
       waits 2 s and reads it back to confirm. Re-entrant: if the lock is already yours it is refreshed.
       A stale lock (older than the stale age) of another owner is removed with a message.
       Random 0-1.5 s jitter before each attempt so two queued sessions do not collide every time.
refresh: exit 1 if the lock is no longer yours (someone broke it as stale): STOP your round, you lost the editor.

Exit codes: 0 done | 1 timed out / not yours / lost | 2 bad arguments | 3,4,5,6 as guard (status only reports)

Settings: -Owner (GAME_STUDIO_SESSION), -ProjectPath (UNITY_PROJECT_PATH), -LockDir (GAME_STUDIO_LOCK_DIR,
default <project>\.game-studio\locks), stale ages as in guard.ps1. guard.ps1 must sit next to this script.
File format: one UTF-8 line "<owner> <yyyy-MM-ddTHH:mm:ss> <note>". Owner = one token, ASCII recommended
(Windows PowerShell 5.1 reads a .ps1 without BOM as ANSI, so non-ASCII names in scripts get mangled; one project
spelled its session name with [char] codes to dodge this).
#>
param(
    [ValidateSet('take', 'refresh', 'release', 'status')][string]$Action = 'status',
    [string]$Owner = $env:GAME_STUDIO_SESSION,
    [string]$Note = '',
    [ValidateSet('editor', 'import')][string]$Name = 'editor',
    [double]$WaitMinutes = 40,
    [string]$ProjectPath = $env:UNITY_PROJECT_PATH,
    [string]$LockDir = $env:GAME_STUDIO_LOCK_DIR
)
if (-not $ProjectPath -and -not $LockDir) { Write-Output "lock: set UNITY_PROJECT_PATH or -LockDir"; exit 2 }
if (-not $LockDir) { $LockDir = Join-Path $ProjectPath '.game-studio\locks' }
$stale = 20; if ($env:GAME_STUDIO_EDITOR_LOCK_STALE_MIN) { $stale = [double]$env:GAME_STUDIO_EDITOR_LOCK_STALE_MIN }
if ($Name -eq 'import') { $stale = 30; if ($env:GAME_STUDIO_IMPORT_LOCK_STALE_MIN) { $stale = [double]$env:GAME_STUDIO_IMPORT_LOCK_STALE_MIN } }
$path = Join-Path $LockDir ($Name + '.lock')
$utf8 = New-Object Text.UTF8Encoding $false

function Read-Lock {
    if (-not (Test-Path -LiteralPath $path)) { return $null }
    try {
        $item = Get-Item -LiteralPath $path -ErrorAction Stop
        $text = ([IO.File]::ReadAllText($path, [Text.Encoding]::UTF8)).Trim([char]0xFEFF, ' ', "`r", "`n", "`t")
    } catch { return $null }
    $first = ($text -split "`n")[0].Trim()
    return @{ Owner = ($first -split '\s+')[0]; Text = $first; AgeMin = ((Get-Date) - $item.LastWriteTime).TotalMinutes }
}
function Line { return ($Owner + ' ' + (Get-Date -Format s) + ' ' + $Note).Trim() }

if ($Action -eq 'status') {
    $l = Read-Lock
    if (-not $l) { Write-Output "$Name.lock: free"; exit 0 }
    $s = ''; if ($l.AgeMin -ge $stale) { $s = ' (STALE)' }
    Write-Output ("$Name.lock: " + $l.Text + ", " + [math]::Round($l.AgeMin, 1) + " min old" + $s)
    exit 0
}
if (-not $Owner -or $Owner -match '\s') { Write-Output "lock: -Owner must be one token (or set GAME_STUDIO_SESSION)"; exit 2 }

if ($Action -eq 'release') {
    $l = Read-Lock
    if (-not $l) { Write-Output "$Name.lock was already free"; exit 0 }
    if ($l.Owner -ne $Owner) { Write-Output ("not releasing: the lock is not yours (" + $l.Text + ")"); exit 1 }
    Remove-Item -LiteralPath $path -Force -ErrorAction SilentlyContinue
    Write-Output ("released $Name.lock " + (Get-Date -Format s))
    exit 0
}

if ($Action -eq 'refresh') {
    $l = Read-Lock
    if (-not $l -or $l.Owner -ne $Owner) {
        $who = 'nobody'; if ($l) { $who = $l.Text }
        Write-Output "LOST THE LOCK: $Name.lock now belongs to $who - stop this round"
        exit 1
    }
    if (-not $Note) { $Note = (($l.Text -split '\s+', 3) | Select-Object -Skip 2) -join ' ' }
    [IO.File]::WriteAllText($path, (Line), $utf8)
    exit 0
}

# take
New-Item -ItemType Directory -Force -Path $LockDir | Out-Null
$guard = Join-Path $PSScriptRoot 'guard.ps1'
$until = (Get-Date).AddMinutes($WaitMinutes)
$last = ''
while ((Get-Date) -lt $until) {
    $l = Read-Lock
    if ($l -and $l.Owner -eq $Owner) { [IO.File]::WriteAllText($path, (Line), $utf8); Write-Output "already mine, refreshed"; exit 0 }
    if ($l -and $l.AgeMin -ge $stale) {
        Write-Output ("removing stale $Name.lock (" + $l.Text + ", " + [int]$l.AgeMin + " min)")
        Remove-Item -LiteralPath $path -Force -ErrorAction SilentlyContinue
        $l = $null
    }
    if (-not $l) {
        Start-Sleep -Milliseconds (Get-Random -Maximum 1500)
        $g = 0
        if ($Name -eq 'editor') {
            $msg = & $guard -Owner $Owner -ProjectPath $ProjectPath -LockDir $LockDir
            $g = $LASTEXITCODE
        }
        if ($g -eq 0) {
            $fs = $null
            try {
                $fs = [IO.File]::Open($path, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::Read)
                $bytes = $utf8.GetBytes((Line))
                $fs.Write($bytes, 0, $bytes.Length)
            } catch { } finally { if ($fs) { $fs.Dispose() } }   # CreateNew throws when another session won
            Start-Sleep -Seconds 2
            $chk = Read-Lock
            if ($chk -and $chk.Owner -eq $Owner) { Write-Output ("taken $Name.lock " + (Get-Date -Format s)); exit 0 }
        } elseif ("$msg" -ne $last) { $last = "$msg"; Write-Output ("waiting: guard " + $g + " " + $msg) }
    } elseif ($l.Text -ne $last) { $last = $l.Text; Write-Output ("waiting: " + $l.Text) }
    Start-Sleep -Seconds 3
}
$l = Read-Lock
$who = 'free (but the guard never passed: Play mode or unknown state)'; if ($l) { $who = $l.Text }
Write-Output ("NOT TAKEN after $WaitMinutes min: " + $who)
exit 1
