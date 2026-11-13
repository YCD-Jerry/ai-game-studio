#!/usr/bin/env bash
# Usage: bash lock.sh take|refresh|release|status [owner] [note].
# Use editor_lock.py directly for recovery.
here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
python_bin="${GAME_STUDIO_PYTHON:-python3}"
if [ -z "${GAME_STUDIO_PYTHON:-}" ] && ! command -v "$python_bin" >/dev/null 2>&1; then
  python_bin=python
fi
if ! command -v "$python_bin" >/dev/null 2>&1; then
  echo 'lock: Python 3 is required'
  exit 2
fi
args=("${1:-status}" "--name=${LOCK_NAME:-editor}" "--wait-minutes=${GAME_STUDIO_LOCK_WAIT_MIN:-40}")
if [ -n "${2:-}" ]; then args+=("--owner=$2"); fi
if [ -n "${3:-}" ]; then args+=("--note=$3"); fi
if [ -n "${GAME_STUDIO_HOLDER_PID:-}" ]; then args+=("--holder-pid=$GAME_STUDIO_HOLDER_PID"); fi
exec "$python_bin" "$here/editor_lock.py" "${args[@]}"
