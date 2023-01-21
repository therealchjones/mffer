#!/bin/sh

echo "Installing Doxygen 1.9.5..."
if ! curl -Ss -OL https://www.doxygen.nl/files/Doxygen-1.9.5.dmg; then
	echo "Unable to download Doxygen" >&2
	exit 1
fi
if ! hdiutil attach -quiet "Doxygen-1.9.5.dmg"; then
	echo "Unable to mount Doxygen installer" >&2
	exit 1
fi
if ! {
	sudo cp -a /Volumes/Doxygen/Doxygen.app /Applications/ \
		&& sudo ln -s /Applications/Doxygen.app/Contents/Resources/doxygen /usr/local/bin/doxygen
}; then
	echo "Unable to install Doxygen" >&2
	retval=1
else
	retval=0
fi
exit "$retval"
