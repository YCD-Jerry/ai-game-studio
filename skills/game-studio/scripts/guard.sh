#!/usr/bin/env bash
# Usage: bash guard.sh [owner].
here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
python_bin="${GAME_STUDIO_PYTHON:-python3}"
if [ -z "${GAME_STUDIO_PYTHON:-}" ] && ! command -v "$python_bin" >/dev/null 2>&1; then
  python_bin=python
fi
if ! command -v "$python_bin" >/dev/null 2>&1; then
  echo 'guard: Python 3 is required'
  exit 4
fi
args=()
if [ -n "${1:-}" ]; then args+=("--owner=$1"); fi
"$python_bin" "$here/editor_guard.py" guard "${args[@]}"
code=$?
case "$code" in
  0|3|4|5|6|7) exit "$code" ;;
  *) exit 4 ;;
esac
