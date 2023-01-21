#!/bin/sh

set -e
set -u

echo "Installing Node.js 16.13.2..."
if curl -Ss -O https://nodejs.org/dist/v16.13.2/node-v16.13.2.pkg; then
	if { sudo installer -pkg ./node-v16.13.2.pkg -target /; }; then
		exit 0
	fi
fi
echo "Unable to install Node.js"
exit 1
