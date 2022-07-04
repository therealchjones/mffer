#!/bin/sh

set -e

# Build the webapp from source files and optionally upload for testing.
#

usage() {
	cat <<EOF
Usage: sh $0 [-bgw]

Options:
  -b    build only, do not upload
  -g    serve HTML from Google Apps Script (default)
  -w    serve HTML from a separate web server (WEBSERVER environment variable)

EOF
}

# Default settings
DEFAULTWEBSERVER=AppsScript # "AppsScript" is a special word, otherwise domain
CUSTOMWEBSERVER=""

# Default settings that can be overridden by environment variables
WEBSERVER="${WEBSERVER:-$DEFAULTWEBSERVER}"
BUILDONLY="${BUILDONLY:-}"
USAGEONLY="${USAGEONLY:-}"
VERBOSE="${VERBOSE:-}"
DEBUG="${DEBUG:-}"
VERBOSEOUT="${VERBOSEOUT:-}"
DEBUGOUT="${DEBUGOUT:-}"

# As convention, only main() should exit the program, should do so only if
# unable to reasonably continue, and should explain why.
# Other functions should return a nonzero status, but only output explanation
# text if a "bare" function that's calling external programs rather than
# other functions. This prevents excessive error output via a "stack trace".
main() {
	getOptions "$@" || exit 1
	if [ -n "$USAGEONLY" ]; then
		usage
		exit 0
	fi
	exit 0
}
buildPage() {

}
buildScript() {

}
checkClasp() {
	yh
}
checkTsc() {

}
checkScp() {

}
getOptions() {
	while getopts 'hvbgw' option; do
		case "$option" in
			h)
				USAGEONLY=y
				;;
			v)
				if [ -n "$VERBOSE" ]; then
					DEBUG=Y
				fi
				VERBOSE=Y
				;;
			b)
				BUILDONLY=Y
				;;
			g)
				if [ "AppsScript" != "$WEBSERVER" ]; then
					echo "Do not use the -g option with the WEBSERVER variable." >&2
					return 1
				fi
				CUSTOMWEBSERVER="$WEBSERVER"
				WEBSERVER="AppsScript"
				;;
			w)
				if [ "AppsScript" = "$WEBSERVER" ] && [ -z "$CUSTOMWEBSERVER" ]; then
					echo "To use -w, set WEBSERVER environment variable to the" >&2
					echo "desired URL." >&2
					return 1
				elif [ "AppsScript" = "$WEBSERVER" ]; then
					WEBSERVER="$CUSTOMWEBSERVER"
				fi
				;;
			?)
				usage
				return 1
				;;
		esac
	done
	shift $((OPTIND - 1))
	if [ "$#" != "0" ]; then
		usage >&2
		exit 1
	fi

	if [ -n "$DEBUG" ]; then
		set -x
		set -u
		VERBOSE=y
		DEBUGOUT="${DEBUGOUT:-/dev/stdout}"
	fi
	DEBUGOUT="${DEBUGOUT:-/dev/null}"
	if [ -n "$VERBOSE" ]; then
		VERBOSEOUT="${VERBOSEOUT:-/dev/stdout}"
	fi
	VERBOSEOUT="${VERBOSEOUT:-/dev/null}"
}
# Prints an absolute pathname corresponding to directory $1, creating the
# directory if it does not exist.
setdir() {
	if [ "$#" != "1" ]; then
		echo 'Usage: setdir dirname' >&2
		exit 1
	fi
	if [ -e "$1" ]; then
		if [ -L "$1" ]; then
			if ! (cd "$1" >&2); then
				echo "'$1' exists and is not a directory." >&2
				exit 1
			fi
		elif [ ! -d "$1" ]; then
			echo "'$1' exists and is not a directory." >&2
			exit 1
		fi
	else
		mkdir -p "$1" || exit 1
	fi
	(cd "$1" && pwd "$1") || exit 1
}
