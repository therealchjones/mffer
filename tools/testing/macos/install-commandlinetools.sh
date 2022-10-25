#!/bin/sh

echo "Installing Xcode Command Line Tools..." >"${VERBOSEOUT:=/dev/null}"
CMDLINETOOLTMP="/tmp/.com.apple.dt.CommandLineTools.installondemand.in-progress"
touch "$CMDLINETOOLTMP"
CMDLINETOOLS="$(softwareupdate -l 2>/dev/null \
	| sed -n \
		-e '/Command Line Tools/!d' \
		-e '/[Bb][Ee][Tt][Aa]/d' \
		-e '/^[ \*]*Label: */{s///;p;}' \
	| sort -V \
	| tail -n1)"
if ! isRoot; then
	warnError "${CMDLINETOOLS}\n must be installed as root. Using sudo..."
fi
if ! output="$(sudo softwareupdate -i "$CMDLINETOOLS" 2>&1)"; then
	echo "$output" >&2
	warnError "Unable to install $CMDLINETOOLS"
	return 1
fi
rm "$CMDLINETOOLTMP"
