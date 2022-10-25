#!/bin/sh

notify "Installing Node.js 16.13.2..."
if curl -Ss -O https://nodejs.org/dist/v16.13.2/node-v16.13.2.pkg >"$DEBUGOUT"; then
	if ! isRoot; then
		warnError "Node.js must be installed as root. Using sudo..."
	fi
	if { sudo installer -pkg ./node-v16.13.2.pkg -target /; } >"$DEBUGOUT"; then
		return 0
	fi
fi
warnError "Unable to install Node.js"
return 1
