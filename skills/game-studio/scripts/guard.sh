#!/usr/bin/env bash
# game-studio guard.sh -- may this session touch the shared Unity editor right now? (bash twin of guard.ps1)
#
# Why a bash twin: from Git Bash, `powershell -File guard.ps1` can be refused by the execution policy, and
# `-ExecutionPolicy Bypass` may itself be blocked by the agent sandbox. Same files, same exit codes.
#
#   ./guard.sh ch3 || exit $?          # owner as first argument, or GAME_STUDIO_SESSION
#
# Run before EVERY recompile, rebuild, reimport, AssetDatabase.Refresh, or file copy into Assets/.
# Read-only: reads two lock files, asks the editor for its play state.
#
# Exit codes: 0 ok | 3 playing (or entering Play) | 4 state unknown | 5 import lock | 6 editor lock held by another owner
#
# Environment:
#   UNITY_PROJECT_PATH                  required
#   GAME_STUDIO_SESSION                 your owner token (if no argument); your own editor lock does not stop you
#   GAME_STUDIO_LOCK_DIR                default $UNITY_PROJECT_PATH/.game-studio/locks
#   GAME_STUDIO_EVAL_DIR                folder with playing.cs; default this script's folder (must be readable by the editor)
#   GAME_STUDIO_EDITOR_LOCK_STALE_MIN   default 20
#   GAME_STUDIO_IMPORT_LOCK_STALE_MIN   default 30
#   GAME_STUDIO_EVAL_TIMEOUT            seconds, default 60
#   UNITY_CLI                           default: unity on PATH
#
# Lock age = file mtime. Owner = first whitespace token of the first line (UTF-8 BOM and CR stripped), exact match.
# Needs: bash, stat (GNU or BSD), grep, awk; `timeout` (GNU coreutils) if available; `cygpath` on Git Bash for Windows paths.

OWNER="${1:-${GAME_STUDIO_SESSION:-}}"
PROJ="${UNITY_PROJECT_PATH:-}"
[ -n "$PROJ" ] || { echo "guard: set UNITY_PROJECT_PATH"; exit 4; }
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
to_posix() { if command -v cygpath >/dev/null 2>&1; then cygpath -u "$1"; else printf '%s' "$1"; fi; }
to_native() { if command -v cygpath >/dev/null 2>&1; then cygpath -w "$1"; else printf '%s' "$1"; fi; }
LOCKDIR="$(to_posix "${GAME_STUDIO_LOCK_DIR:-$PROJ/.game-studio/locks}")"
EVALDIR="$(to_posix "${GAME_STUDIO_EVAL_DIR:-$HERE}")"
ED_STALE="${GAME_STUDIO_EDITOR_LOCK_STALE_MIN:-20}"
IM_STALE="${GAME_STUDIO_IMPORT_LOCK_STALE_MIN:-30}"
EVT="${GAME_STUDIO_EVAL_TIMEOUT:-60}"
UNITY="${UNITY_CLI:-unity}"

mtime() { stat -c %Y "$1" 2>/dev/null || stat -f %m "$1" 2>/dev/null; }
age_min() { local m; m=$(mtime "$1") || return 1; [ -n "$m" ] || return 1; echo $(( ( $(date +%s) - m ) / 60 )); }
first_line() { head -n1 "$1" 2>/dev/null | LC_ALL=C sed $'s/^\xef\xbb\xbf//' | tr -d '\r'; }
owner_of() { first_line "$1" | awk '{print $1}'; }
# integer compare of minutes against a possibly fractional limit
younger() { awk -v a="$1" -v b="$2" 'BEGIN{exit !(a<b)}'; }

# 1. import lock -> 5
IL="$LOCKDIR/import.lock"
if [ -f "$IL" ]; then
  a=$(age_min "$IL")
  if [ -n "$a" ] && younger "$a" "$IM_STALE"; then echo "IMPORT RUNNING ($(first_line "$IL"), ${a} min) - not touching the editor"; exit 5; fi
fi

# 2. editor lock -> 6
EL="$LOCKDIR/editor.lock"
if [ -f "$EL" ]; then
  a=$(age_min "$EL"); o=$(owner_of "$EL")
  if [ -n "$a" ] && younger "$a" "$ED_STALE" && { [ -z "$OWNER" ] || [ "$o" != "$OWNER" ]; }; then
    echo "EDITOR BUSY ($(first_line "$EL"), ${a} min) - wait"; exit 6
  fi
fi

# 3a. beacon mode (GAME_STUDIO_PLAY_PROBE=beacon): read <project>/Temp/editor_state.txt written by EditorStateBeacon.cs
if [ "${GAME_STUDIO_PLAY_PROBE:-cli}" = "beacon" ]; then
  ST="$(to_posix "$PROJ")/Temp/editor_state.txt"
  [ -f "$ST" ] || { echo "guard: no $ST (beacon not installed or editor closed)"; exit 4; }
  s_age=$(( $(date +%s) - $(mtime "$ST") ))
  [ "$s_age" -le 10 ] || { echo "editor state ${s_age}s old (closed, busy or a modal dialog) - not touching it"; exit 4; }
  grep -q 'playing=True\|willPlay=True' "$ST" && { echo "UNITY IS IN PLAY MODE - not touching it"; exit 3; }
  grep -q 'playing=False' "$ST" || { echo "guard: unreadable beacon state - not touching it"; exit 4; }
  exit 0
fi

# 3. play state -> 3 / 4
PROBE="$EVALDIR/playing.cs"
[ -f "$PROBE" ] || { echo "guard: missing $PROBE"; exit 4; }
export UNITY_NO_BANNER=1 UNITY_NO_PAGER=1 UNITY_NON_INTERACTIVE=1
if command -v timeout >/dev/null 2>&1; then T=(timeout -k 5 $((EVT + 30))); else T=(); fi
read -r -a EVALCMD <<< "${GAME_STUDIO_EVAL_CMD:-command script eval_file}"   # differs between CLI versions
r=$("${T[@]}" "$UNITY" "${EVALCMD[@]}" --file "$(to_native "$PROBE")" --timeout "$EVT" 2>&1)
if printf '%s' "$r" | grep -q 'playing=True'; then echo "UNITY IS IN PLAY MODE - not touching it"; exit 3; fi
if ! printf '%s' "$r" | grep -q 'playing=False'; then
  echo "Could not read Unity's play state - not touching it"
  printf '%s' "$r" | tr '\n' ' ' | cut -c1-300; echo
  exit 4
fi
exit 0
