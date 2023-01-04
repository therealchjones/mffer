#!/bin/sh

#!/bin/sh

# Ensure this variable was exported by script calling this one
[ -n "${MFFER_TEST_FRAMEWORK:=}" ] || exit 1
# shellcheck disable=SC1090 # source a non-constant file
. "$MFFER_TEST_FRAMEWORK"

echo "testing autoanalyze"
if ! "$(getSourceDir)/release/$(getOs)/autoanalyze" -h; then
	echo "FAILED testing autoanalyze" >"$VERBOSEOUT"
	exit 1
else
	echo "PASSED testing autoanalyze" >"$VERBOSEOUT"
	exit 0
fi
