#!/bin/sh

# Ensure this variable was exported by script calling this one
[ -n "${MFFER_TEST_FRAMEWORK:=}" ] || exit 1
# shellcheck disable=SC1090 # source a non-constant file
. "$MFFER_TEST_FRAMEWORK"

if ! {
	unzip "$(getSourceDir)/release/mffer-$(getVersionString)-$(getOsPlatform).zip" -d "$(getSourceDir)/release/$(getOs)/" \
		&& chmod -R a+rx "$(getSourceDir)/release/$(getOs)/"
}; then
	echo "Error: Unable to access mffer release for '$(getOs)'" >&2
	exit 1
fi

runTest mffer
runTest apkdl
runTest autoanalyze
