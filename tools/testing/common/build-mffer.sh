#!/bin/sh

set -e
set -u
if [ -n "${DEBUG:=}" ]; then set -x; fi

echo "building mffer" >"${VERBOSEOUT:=/dev/stdout}"
failure=''
if [ -z "${MFFER_TEST_SOURCE:=${GITHUB_WORKSPACE:-}}" ]; then
	echo "Error:'MFFER_TEST_SOURCE' and 'GITHUB_WORKSPACE' are not defined or are empty" >&2
	failure=y
elif [ -z "${MFFER_TEST_TMPDIR:-}" ]; then
	echo "Error: 'MFFER_TEST_TMPDIR' is undefined or empty" >&2
	failure=y
elif [ -z "${MFFER_BUILD_OS:-}" ]; then
	echo "Error: 'MFFER_BUILD_OS' is undefined or empty" >&2
	failure=y
elif ! {
	dotnet restore "$MFFER_TEST_SOURCE"/mffer.csproj \
		&& dotnet clean "$MFFER_TEST_SOURCE"/mffer.csproj \
		&& VersionString="${MFFER_TEST_COMMIT:-prerelease}" dotnet publish -c Release "$MFFER_TEST_SOURCE"/mffer.csproj
} >"${DEBUGOUT:=/dev/null}"; then
	failure=y
fi
if [ -z "$failure" ]; then
	for os in windows macos linux; do
		file="$MFFER_TEST_SOURCE/release/mffer-${MFFER_TEST_COMMIT:-prerelease}"
		case "$os" in
			windows)
				file="$file-win-x64.zip"
				;;
			linux)
				file="$file-linux-x64.zip"
				;;
			macos)
				file="$file-osx-x64.zip"
				;;
		esac
		dir="$MFFER_TEST_TMPDIR/built-on-$MFFER_BUILD_OS/$os"
		if ! mkdir -p "$dir" \
			|| [ ! -r "$file" ] \
			|| ! unzip "$file" "$dir" >"$DEBUGOUT" \
			|| ! sudo chmod -R a+r "$dir"; then
			echo "Error: Unable to save the $os release built on $MFFER_BUILD_OS" >&2
			failure=y
		fi
	done
fi

if [ -n "$failure" ]; then
	echo "FAILED building mffer" >"$VERBOSEOUT"
	exit 1
else
	echo "PASSED building mffer" >"$VERBOSEOUT"
fi
