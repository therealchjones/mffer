#!/bin/sh

set -e

RUNDIR="$(dirname "$0")"
DOCDIR="$RUNDIR/../docs"
PYTHONDIR="$RUNDIR/../.venv"

# shellcheck disable=SC1091
. "$PYTHONDIR/bin/activate"

sphinx-build -a -b dirhtml -n -c "$DOCDIR/_config" "$DOCDIR" "$DOCDIR/_build"
