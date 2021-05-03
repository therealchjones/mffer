#!/bin/sh

# Prints an absolute pathname corresponding to path $1
#
# This is done by changing to the directory $1, so is best done in a
# subshell, as in:
# NEWDIR="$(getdir "../new_dir")"
# (Otherwise, changing back to the previous directory creates its own
# set of problems.) Additionally, this requires the directory to exist, so it
# should be created before calling getdir if necessary.
#
# TODO: #91 Consider whether we need to deal with paths ending in /
getdir() {
	if [ "$#" != "1" ]; then
		echo 'Usage: getdir dirname' >&2
		return 1
	fi
	cd -- "$1" || {
		echo "Unable to access directory '$1'" >&2
		return 1
	}
	dirname "$PWD/."
}

throwfatal() {
	if [ "$#" -gt 0 ]; then
		echo "$0": "$@" >&2
	fi
	exit 1
}
