#!/bin/sh

set -e
set -u

echo "building documentation" >"${VERBOSEOUT:=/dev/stdout}"
failure=''
if [ -z "${MFFER_TEST_SOURCE:=${GITHUB_WORKSPACE:-}}" ]; then
	echo "Error:'MFFER_TEST_SOURCE' and 'GITHUB_WORKSPACE' are not defined or are empty" >&2
	failure=y
elif [ -z "${MFFER_TEST_TMPDIR:=}" ]; then
	echo "Error:'MFFER_TEST_TMPDIR' is not defined or is empty" >&2
	failure=y
elif ! {
	# shellcheck disable=SC1091 # (for virtual environment activate script)
	tmpdir="$MFFER_TEST_TMPDIR" \
		&& python3 -m venv "$tmpdir"/python \
		&& . "$tmpdir"/python/bin/activate \
		&& pip3 install --upgrade pip \
		&& pip3 install wheel \
		&& pip3 install \
			-r "$MFFER_TEST_SOURCE"/tools/requirements.txt \
		&& cd "$MFFER_TEST_SOURCE" \
		&& sh tools/mkdocs.sh
} >"${DEBUGOUT:=/dev/null}"; then
	failure=y
fi
if [ -n "$failure" ]; then
	echo "FAILED building documentation" >"$VERBOSEOUT"
	exit 1
else
	echo "PASSED building documentation" >"$VERBOSEOUT"
fi
