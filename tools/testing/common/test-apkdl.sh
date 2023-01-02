#!/bin/sh

set -e
set -u

echo "testing apkdl" >"${VERBOSEOUT:=/dev/stdout}"
failure=''
file="$MFFER_TEST_BINDIR/apkdl"
if [ -z "$failure" ] && [ ! -x "$file" ]; then
	echo "Error:'$MFFER_TEST_BINDIR/apkdl' is not found or not executable" >&2
	failure=y
elif [ -z "$failure" ] && ! {
	"$MFFER_TEST_BINDIR/apkdl" -h
} >"${DEBUGOUT:=/dev/null}"; then
	failure=y
fi
if [ -n "$failure" ]; then
	echo "FAILED testing apkdl" >"$VERBOSEOUT"
	exit 1
else
	echo "PASSED testing apkdl" >"$VERBOSEOUT"
fi
