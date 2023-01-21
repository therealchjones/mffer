#!/bin/sh

# Ensure this variable was exported by script calling this one
[ -n "${MFFER_TEST_FRAMEWORK:=}" ] || exit 1
# shellcheck disable=SC1090 # source a non-constant file
. "$MFFER_TEST_FRAMEWORK"

echo "Setting up build environment"

if ! install shell \
	|| ! install dotnet \
	|| ! install node \
	|| ! install git \
	|| ! install python \
	|| ! install doxygen; then
	echo "Unable to set up build environment"
	exit 1
fi
