#!/bin/sh

sh -u '$MFFER_TEST_FRAMEWORK' || exit 1

if [ "$#" != 1 ]; then
	echo "ERROR: build-remote requires a single argument" >&2
	exit 1
fi

startBuild "$1"
buildRemote "$1"
endBuild "$1"
