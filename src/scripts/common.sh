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
# Can probably implement algorithm used by cd, as in:
# https://pubs.opengroup.org/onlinepubs/9699919799/
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

getexec() {
	if [ "$#" = 0 ]; then
		echo 'Usage: getexec filename [altfile... ]' >&2
		return 1
	fi
	for filename in "$@"; do
		if type "$filename" >/dev/null 2>&1; then
			echo "$filename"
			return 0
		fi
	done
	return 1
}

throwfatal() {
	if [ "$#" -gt 0 ]; then
		echo "$0": "$@" >&2
	fi
	exit 1
}

cleanup() {
	if [ "$#" -gt "1" ]; then
		echo 'Usage: cleanup [signal]' >&2
		return 1
	fi
	case "$1" in
		"" | HUP | INT | QUIT | TERM) ;;
		*)
			echo "'$1' is an unsupported signal" >&2
			return 1
			;;
	esac
	echo "Cleaning up" >"$VERBOSEOUT"
	# Temp directories may still be in use by child processes
	trap '' EXIT HUP INT QUIT TERM
	kill -s "$1" 0
	# Allow cancelling further if, e.g., someone hits Ctrl-C while cleanup is
	# happening
	trap - EXIT HUP INT QUIT TERM
	wait
	[ -n "$MFFTEMPDIR" ] && {
		rm -rf -- "$MFFTEMPDIR" || echo "Unable to complete cleanup" >&2
	}
	if [ -n "$1" ]; then
		kill -s "$1" 0
		# Shells do not portably exit despite POSIX specification
		# on some of the above signals (e.g., bash ignores SIGQUIT)
		exit 1
	fi
	exit
}
signal() {
	if [ "$#" != "1" ]; then
		echo 'Usage: signal signalname' >&2
		return 1
	fi
	signal=""
	case "$1" in
		"HUP" | "INT" | "QUIT" | "TERM")
			signal="$1"
			;;
		"EXIT") ;;
		*)
			echo 'Usage: signal signalname' >&2
			return 1
			;;
	esac
	trap '' EXIT
	echo "Processing $1 signal" >"$VERBOSEOUT"
	if [ -n "$signal" ]; then
		cleanup "$signal"
	else
		cleanup
	fi
}
trapforcleanup() {
	trap cleanup EXIT
	trap 'signal HUP' HUP
	trap 'signal INT' INT
	trap 'signal QUIT' QUIT
	trap 'signal TERM' TERM
}
