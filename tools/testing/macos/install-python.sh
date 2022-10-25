#!/bin/sh

notify "Installing Python $PYTHON_VERSION"
if ! curl -Ss -OL "https://www.python.org/ftp/python/$PYTHON_VERSION/python-$PYTHON_VERSION-macos11.pkg" >"$DEBUGOUT"; then
	echo "Unable to download Python $PYTHON_VERSION" >&2
	return 1
fi
if ! isRoot; then
	warnError "Python must be installed as root. Will use sudo."
fi
if ! { sudo installer -pkg "./python-$PYTHON_VERSION-macos11.pkg" -target /; } >"$DEBUGOUT"; then
	echo "Unable to install Python" >&2
	return 1
fi
return 0
