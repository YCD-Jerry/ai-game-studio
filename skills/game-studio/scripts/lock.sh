#!/usr/bin/env bash
# game-studio lock.sh -- bash twin of lock.ps1: take / refresh / release / status of the shared editor (or import) lock.
#
#   ./lock.sh take    ch3 "rebuild ch3 + shots"    # waits up to GAME_STUDIO_LOCK_WAIT_MIN (default 40), holding nothing
#   ./lock.sh refresh ch3 ["shots"]                # before each long step; exit 1 = you LOST the lock, stop
#   ./lock.sh release ch3                          # only removes it if it is yours
#   ./lock.sh status
#   LOCK_NAME=import ./lock.sh take art "reimport characters"    # import.lock instead of editor.lock
#
# take: when the lock is free (or stale) and guard.sh exits 0, create the file with noclobber (O_EXCL: fails if
# another session won the race), sleep 2 s, read it back. Re-entrant (your own lock is refreshed). Random jitter.
# Typical round:
#   ./lock.sh take ch3 "rebuild" || exit 1
#   trap './lock.sh release ch3' EXIT          # plus hand_back.cs before it, see round.ps1
#
# Exit codes: 0 done | 1 timed out / not yours / lost | 2 bad arguments
# Environment: UNITY_PROJECT_PATH, GAME_STUDIO_LOCK_DIR (default <project>/.game-studio/locks),
#   GAME_STUDIO_EDITOR_LOCK_STALE_MIN (20), GAME_STUDIO_IMPORT_LOCK_STALE_MIN (30), GAME_STUDIO_LOCK_WAIT_MIN (40).
# File format shared with lock.ps1: one line "<owner> <YYYY-MM-DDTHH:MM:SS> <note>", no BOM.
# Not verified: O_EXCL semantics on network drives / synced folders; keep the lock dir on a local disk.

ACTION="${1:-status}"; OWNER="${2:-${GAME_STUDIO_SESSION:-}}"; NOTE="${3:-}"
NAME="${LOCK_NAME:-editor}"
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
to_posix() { if command -v cygpath >/dev/null 2>&1; then cygpath -u "$1"; else printf '%s' "$1"; fi; }
if [ -z "${GAME_STUDIO_LOCK_DIR:-}" ] && [ -z "${UNITY_PROJECT_PATH:-}" ]; then echo "lock: set UNITY_PROJECT_PATH or GAME_STUDIO_LOCK_DIR"; exit 2; fi
LOCKDIR="$(to_posix "${GAME_STUDIO_LOCK_DIR:-$UNITY_PROJECT_PATH/.game-studio/locks}")"
L="$LOCKDIR/$NAME.lock"
if [ "$NAME" = import ]; then STALE="${GAME_STUDIO_IMPORT_LOCK_STALE_MIN:-30}"; else STALE="${GAME_STUDIO_EDITOR_LOCK_STALE_MIN:-20}"; fi
WAIT="${GAME_STUDIO_LOCK_WAIT_MIN:-40}"

mtime() { stat -c %Y "$1" 2>/dev/null || stat -f %m "$1" 2>/dev/null; }
age_min() { local m; m=$(mtime "$1"); [ -n "$m" ] && echo $(( ( $(date +%s) - m ) / 60 )); }
first_line() { head -n1 "$1" 2>/dev/null | LC_ALL=C sed $'s/^\xef\xbb\xbf//' | tr -d '\r'; }
owner_of() { first_line "$1" | awk '{print $1}'; }
older_eq() { awk -v a="$1" -v b="$2" 'BEGIN{exit !(a>=b)}'; }
line() { printf '%s %s %s\n' "$OWNER" "$(date '+%Y-%m-%dT%H:%M:%S')" "$NOTE"; }

if [ "$ACTION" = status ]; then
  [ -f "$L" ] || { echo "$NAME.lock: free"; exit 0; }
  a=$(age_min "$L"); s=""; older_eq "${a:-0}" "$STALE" && s=" (STALE)"
  echo "$NAME.lock: $(first_line "$L"), ${a} min old$s"; exit 0
fi
case "$OWNER" in ""|*" "*) echo "lock: owner must be one token (arg 2 or GAME_STUDIO_SESSION)"; exit 2;; esac

case "$ACTION" in
release)
  [ -f "$L" ] || { echo "$NAME.lock was already free"; exit 0; }
  if [ "$(owner_of "$L")" != "$OWNER" ]; then echo "not releasing: not yours ($(first_line "$L"))"; exit 1; fi
  rm -f "$L"; echo "released $NAME.lock $(date '+%T')"; exit 0 ;;
refresh)
  if [ ! -f "$L" ] || [ "$(owner_of "$L")" != "$OWNER" ]; then cur=free; [ -f "$L" ] && cur="$(first_line "$L")"; echo "LOST THE LOCK: $NAME.lock is $cur - stop this round"; exit 1; fi
  [ -n "$NOTE" ] || NOTE="$(first_line "$L" | cut -d' ' -f3-)"
  line > "$L"; exit 0 ;;
take)
  mkdir -p "$LOCKDIR"
  END=$(( $(date +%s) + ${WAIT%.*} * 60 )); last=""
  while [ "$(date +%s)" -lt "$END" ]; do
    if [ -f "$L" ] && [ "$(owner_of "$L")" = "$OWNER" ]; then line > "$L"; echo "already mine, refreshed"; exit 0; fi
    if [ -f "$L" ]; then
      a=$(age_min "$L")
      if [ -n "$a" ] && older_eq "$a" "$STALE"; then echo "removing stale $NAME.lock ($(first_line "$L"), $a min)"; rm -f "$L"; fi
    fi
    if [ ! -f "$L" ]; then
      sleep "0.$(( RANDOM % 10 ))$(( RANDOM % 10 ))" 2>/dev/null || sleep 1
      g=0
      if [ "$NAME" = editor ]; then msg=$("$HERE/guard.sh" "$OWNER" 2>&1); g=$?; fi
      if [ "$g" = 0 ]; then
        if ( set -o noclobber; line > "$L" ) 2>/dev/null; then :; fi
        sleep 2
        if [ -f "$L" ] && [ "$(owner_of "$L")" = "$OWNER" ]; then echo "taken $NAME.lock $(date '+%T')"; exit 0; fi
      elif [ "$msg" != "$last" ]; then last="$msg"; echo "waiting: guard $g $msg"; fi
    else
      cur="$(first_line "$L")"; [ "$cur" != "$last" ] && { last="$cur"; echo "waiting: $cur"; }
    fi
    sleep 3
  done
  cur="free, but the guard never passed (Play mode or unknown state)"; [ -f "$L" ] && cur="$(first_line "$L")"; echo "NOT TAKEN after $WAIT min: $cur"; exit 1 ;;
*) echo "usage: lock.sh take|refresh|release|status <owner> [note]"; exit 2 ;;
esac
