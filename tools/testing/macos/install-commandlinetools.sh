#!/bin/sh

echo "Installing Xcode Command Line Tools..." >"${VERBOSEOUT:=/dev/null}"
CMDLINETOOLTMP="/tmp/.com.apple.dt.CommandLineTools.installondemand.in-progress"
touch "$CMDLINETOOLTMP"
if ! CMDLINETOOLS="$(softwareupdate -l 2>/dev/null \
	| sed -n \
		-e '/Command Line Tools/!d' \
		-e '/[Bb][Ee][Tt][Aa]/d' \
		-e '/^[ \*]*Label: */{s///;p;}' \
	| sort -V \
	| tail -n1)" \
	|| [ -z "$CMDLINETOOLS" ]; then
	echo "Error: Unable to find Xcode Command Line Tools" >&2
	exit 1
fi
if ! output="$(sudo softwareupdate -i "$CMDLINETOOLS" 2>&1)"; then
	echo "$output" >&2
	echo "Error: Unable to install $CMDLINETOOLS" >&2
	exit 1
fi
rm -f "$CMDLINETOOLTMP"
