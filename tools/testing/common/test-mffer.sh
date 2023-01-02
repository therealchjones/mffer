#!/bin/sh

set -e
set -u

echo "testing mffer" >"${VERBOSEOUT:=/dev/stdout}"
failure=''
if [ -z "${MFFER_TEST_BINDIR:=}" ]; then
	echo "Error:'MFFER_TEST_BINDIR' is unset or empty" >&2
	failure=y
elif [ ! -x "$MFFER_TEST_BINDIR/mffer" ] && [ ! -x "$MFFER_TEST_BINDIR"/mffer.exe ]; then
	echo "Error:'$MFFER_TEST_BINDIR/mffer' is not found or not executable" >&2
	failure=y
elif ! {
	"$MFFER_TEST_BINDIR/mffer" -h
} >"${DEBUGOUT:=/dev/null}"; then
	failure=y
fi
if [ -n "$failure" ]; then
	echo "FAILED testing mffer" >"$VERBOSEOUT"
	exit 1
else
	echo "PASSED testing mffer" >"$VERBOSEOUT"
fi
