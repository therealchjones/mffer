#!/bin/sh

echo "Installing Python $PYTHON_VERSION" >"${VERBOSEOUT:=/dev/null}"
if ! curl -Ss -OL "https://www.python.org/ftp/python/$PYTHON_VERSION/python-$PYTHON_VERSION-macos11.pkg" >"${DEBUGOUT:=/dev/null}"; then
	echo "Unable to download Python $PYTHON_VERSION" >&2
	exit 1
fi
if [ "$(id -u)" -ne 0 ]; then
	echo "Warning: Python must be installed as root. Using sudo..." >&2
fi
if ! { sudo installer -pkg "./python-$PYTHON_VERSION-macos11.pkg" -target /; } >"${DEBUGOUT:=/dev/null}"; then
	echo "Unable to install Python" >&2
	exit 1
fi
