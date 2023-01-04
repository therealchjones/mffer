#!/bin/sh

# Ensure this variable was exported by script calling this one
[ -n "${MFFER_TEST_FRAMEWORK:=}" ] || exit 1
# shellcheck disable=SC1090 # source a non-constant file
. "$MFFER_TEST_FRAMEWORK"

echo "building mffer"
if ! {
	dotnet restore "$(getSourceDir)"/mffer.csproj \
		&& dotnet clean "$(getSourceDir)"/mffer.csproj \
		&& VersionString="$(getVersionString)" dotnet publish -c Release "$(getSourceDir)"/mffer.csproj
}; then
	echo "FAILED building mffer"
	exit 1
else
	echo "PASSED building mffer"
	exit 0
fi
