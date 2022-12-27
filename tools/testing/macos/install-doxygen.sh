#!/bin/sh

notify "Installing Doxygen 1.9.5"
if curl -Ss -OL https://www.doxygen.nl/files/Doxygen-1.9.5.dmg >"$DEBUGOUT"; then
	if ! isRoot; then
		warnError "Doxygen must be installed as root. Using sudo..."
	fi
	if {
		sudo cp -a /Volumes/Doxygen/Doxygen.app /Applications/ \
			&& ln -s /Applications/Doxygen.app/Contents/Resources/doxygen /usr/local/bin/doxygen
	} >"$DEBUGOUT"; then
		exit 0
	fi
fi
echo "Error: Unable to install Doxygen"
exit 1
