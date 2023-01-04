#!/bin/sh

#!/bin/sh

# Ensure this variable was exported by script calling this one
[ -n "${MFFER_TEST_FRAMEWORK:=}" ] || exit 1
# shellcheck disable=SC1090 # source a non-constant file
. "$MFFER_TEST_FRAMEWORK"

echo "testing apkdl"
if ! "$(getSourceDir)/release/$(getOs)/apkdl" -h; then
	echo "FAILED testing apkdl" >"$VERBOSEOUT"
	exit 1
else
	echo "PASSED testing apkdl" >"$VERBOSEOUT"
	exit 0
fi
