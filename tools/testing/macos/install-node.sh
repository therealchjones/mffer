#!/bin/sh

set -e
set -u

echo "Installing Node.js 16.13.2..." >"${VERBOSEOUT:=/dev/null}"
if curl -Ss -O https://nodejs.org/dist/v16.13.2/node-v16.13.2.pkg >"${DEBUGOUT:=/dev/null}"; then
	if { sudo installer -pkg ./node-v16.13.2.pkg -target /; } >"$DEBUGOUT"; then
		exit 0
	fi
fi
echo "Unable to install Node.js" >"$DEBUGOUT"
exit 1
