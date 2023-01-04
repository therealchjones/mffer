#!/bin/sh

# Ensure this variable was exported by script calling this one
[ -n "${MFFER_TEST_FRAMEWORK:=}" ] || exit 1
# shellcheck disable=SC1090 # source a non-constant file
. "$MFFER_TEST_FRAMEWORK"

echo "building documentation"
if ! {
	# shellcheck disable=SC1091 # (for virtual environment activate script)
	cd "$(getSourceDir)" \
		&& dotnet restore \
		&& . tools/python/bin/activate \
		&& sh tools/mkdocs.sh
}; then
	echo "FAILED building documentation"
	exit 1
else
	echo "PASSED building documentation"
	exit 0
fi
