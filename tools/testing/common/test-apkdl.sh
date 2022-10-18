#!/bin/sh

set -e
set -u

echo "testing apkdl" >"${VERBOSEOUT:=/dev/stdout}"
failure=''
if [ -z "${MFFER_TEST_OS:-}" ]; then
	echo "Error:'MFFER_TEST_OS' is unset or empty" >&2
	failure=y
elif [ -z "${MFFER_BUILD_OS:-}" ]; then
	echo "Error:'MFFER_BUILD_OS' is unset or empty" >&2
	failure=y
elif [ -z "${MFFER_TEST_TMPDIR:-}" ]; then
	echo "Error:'MFFER_TEST_TMPDIR' is unset or empty" >&2
	failure=y
fi
file="$MFFER_TEST_RUNDIR/apkdl"
if [ "$MFFER_TEST_OS" = windows ]; then file="$file.exe"; fi
if [ -z "$failure" ] && [ ! -x "$file" ]; then
	echo "Error:'$MFFER_TEST_RUNDIR/apkdl' is not found or not executable" >&2
	failure=y
elif [ -z "$failure" ] && ! {
	"$MFFER_TEST_RUNDIR/apkdl" -h
} >"${DEBUGOUT:=/dev/null}"; then
	failure=y
fi
if [ -n "$failure" ]; then
	echo "FAILED testing apkdl" >"$VERBOSEOUT"
	exit 1
else
	echo "PASSED testing apkdl" >"$VERBOSEOUT"
fi
