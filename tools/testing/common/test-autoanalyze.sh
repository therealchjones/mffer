#!/bin/sh

set -e
set -u

echo "testing autoanalyze" >"${VERBOSEOUT:=/dev/stdout}"
failure=''
if [ -z "${MFFER_TEST_RUNDIR:=}" ]; then
	echo "Error:'MFFER_TEST_RUNDIR' is unset or empty" >&2
	failure=y
elif [ ! -x "$MFFER_TEST_RUNDIR/autoanalyze" ]; then
	echo "Error:'$MFFER_TEST_RUNDIR/autoanalyze' is not found or not executable" >&2
	failure=y
elif ! {
	"$MFFER_TEST_RUNDIR/autoanalyze" -h
} >"${DEBUGOUT:=/dev/null}"; then
	failure=y
fi
if [ -n "$failure" ]; then
	echo "FAILED testing autoanalyze" >"$VERBOSEOUT"
	exit 1
else
	echo "PASSED testing autoanalyze" >"$VERBOSEOUT"
fi
