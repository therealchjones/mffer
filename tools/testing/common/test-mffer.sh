#!/bin/sh

# Ensure this variable was exported by script calling this one
[ -n "${MFFER_TEST_FRAMEWORK:=}" ] || exit 1
# shellcheck disable=SC1090 # source a non-constant file
. "$MFFER_TEST_FRAMEWORK"

echo "testing mffer"
if ! "$(getSourceDir)/release/$(getOs)/mffer" -h; then
	echo "FAILED testing mffer" >"$VERBOSEOUT"
	exit 1
else
	echo "PASSED testing mffer" >"$VERBOSEOUT"
	exit 0
fi
