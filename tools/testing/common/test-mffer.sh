#!/bin/sh

set -e
set -u

echo "testing mffer" >"${VERBOSEOUT:=/dev/stdout}"
failure=''
if [ -z "${MFFER_TEST_RUNDIR:=}" ]; then
	echo "Error:'MFFER_TEST_RUNDIR' is unset or empty" >&2
	failure=y
elif [ ! -x "$MFFER_TEST_RUNDIR/mffer" ]; then
	echo "Error:'$MFFER_TEST_RUNDIR/mffer' is not found or not executable" >&2
	failure=y
elif ! {
	"$MFFER_TEST_RUNDIR/mffer" -h
} >"${DEBUGOUT:=/dev/null}"; then
	failure=y
fi
if [ -n "$failure" ]; then
	echo "FAILED testing mffer" >"$VERBOSEOUT"
	exit 1
else
	echo "PASSED testing mffer" >"$VERBOSEOUT"
fi
