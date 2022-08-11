#!/bin/sh

set -e

# Build the webapp from source files and deploy it
#

usage() {
	cat <<EOF
Usage: sh $0 [-bghOv] [-D id] [-w url]

Options:
  -b       build only, do not upload
  -D id    use existing deployment with the given deployment ID (requires -O)
  -g       serve HTML from Google Apps Script (default)
  -h       print this usage message and exit
  -N       create a new Google Apps Script project if not associated with one
  -O       overwrite existing deployment (the last if -D is not used)
  -p path  scp upload destination for the web page (when using -w)
  -v       output more information (use twice for debug info)
  -w url   serve HTML from url instead of Google Apps Script

Examples:
  sh $0 -b
  sh $0 -N
  sh $0 -vO -w https://mffer.org -p mffer.org:mffer.org/index.html
  sh $0 -O -D AKfycbw5My9NOpc6ZxeIND5_XWkixTRHKu4lTO5Z7-Bl0J0vgL_yYqmhsRqhkjhIBCUH5Idq
EOF
}

# Default settings
DEFAULTWEBSERVER=AppsScript # "AppsScript" is a special word, otherwise url
INCLUDESTRING="<!-- include JavaScript.js -->"

# Default settings that can be overridden by environment variables
WEBSERVER="${WEBSERVER:-$DEFAULTWEBSERVER}"
WEBSERVERPATH="${WEBSERVERPATH:-}"
BUILDONLY="${BUILDONLY:-}"
USAGEONLY="${USAGEONLY:-}"
VERBOSE="${VERBOSE:-}"
DEBUG="${DEBUG:-}"
VERBOSEOUT="${VERBOSEOUT:-}"
DEBUGOUT="${DEBUGOUT:-}"
OVERWRITEDEPLOYMENT="${OVERWRITEDEPLOYMENT:-}"
GASDEPLOYMENT="${GASDEPLOYMENT:-}"
WORKSPACEROOT="${WORKSPACEROOT:-$(dirname "$0")/..}"
if [ "${WORKSPACEROOT#/}" = "$WORKSPACEROOT" ]; then
	WORKSPACEROOT="$PWD/$WORKSPACEROOT"
fi
BUILDDIR="${BUILDDIR:-${WORKSPACEROOT}/build/webapp}"
WEBAPPDIR="${WEBAPPDIR:-${WORKSPACEROOT}/src/webapp}"
RELEASEDIR="${RELEASEDIR:-${BUILDDIR}/release}"
GASDIR="${GASDIR:-$WEBAPPDIR/gas}"
HTMLDIR="${HTMLDIR:-$WEBAPPDIR/html}"
export PATH="$WORKSPACEROOT/tools/node_modules/.bin:$PATH"
NEWPROJECT="${NEWPROJECT:-}"

# As convention, only main() should exit the program, should do so only if
# unable to reasonably continue, and should explain why.
# Other functions should return a nonzero status, but only output explanation
# text if a "bare" function that's calling external programs rather than
# other functions. This prevents excessive error output via a "stack trace".
# To avoid debug output mangling expected outputs, put calls to the "check*"
# functions in this function only.
main() {
	getOptions "$@" || exit 1
	if [ -n "$USAGEONLY" ]; then
		usage
		exit 0
	fi
	checkTsc || exit 1
	buildPage || exit 1
	buildScript || exit 1
	if [ -z "$BUILDONLY" ]; then
		checkClasp || exit 1
		pushScript || exit 1
		if [ -z "$OVERWRITEDEPLOYMENT" ] && [ -n "$GASDEPLOYMENT" ]; then
			if checkDeployment "$GASDEPLOYMENT"; then
				echo "use the -O option to overwrite an existing deployment" >&2
				exit 1
			else
				echo "unable to identify a deployment called '$GASDEPLOYMENT'." >&2
				exit 1
			fi
		elif [ -z "$GASDEPLOYMENT" ]; then
			if [ -z "$OVERWRITEDEPLOYMENT" ]; then
				newDeployment || exit 1
			else
				newDeployment @last || exit 1
			fi
		else
			if ! checkDeployment "$GASDEPLOYMENT"; then
				echo "unable to identify a deployment called '$GASDEPLOYMENT'." >&2
				exit 1
			fi
			newDeployment "$GASDEPLOYMENT" || exit 1
		fi
		if [ "$WEBSERVER" != "AppsScript" ]; then
			checkScp || exit 1
			if [ -z "$GASDEPLOYMENT" ]; then
				GASDEPLOYMENT="$(getLastDeployment)" || exit 1
			fi
			buildPage "$GASDEPLOYMENT" || exit 1
			pushPage || exit 1
		fi
	fi
	echo "done" >"$VERBOSEOUT"
	exit 0
}
buildPage() {
	deployment=""
	if [ 1 -lt "$#" ]; then
		echo "buildPage takes at most one argument" >&2
		return 1
	elif [ 1 = "$#" ]; then
		if ! checkDeployment "$1"; then
			echo "unable to find deployment '$1'" >&2
			return 1
		else
			echo "rebuilding HTML for deployment '$1'" >"$VERBOSEOUT"
			deployment="$1"
		fi
	else
		echo "building HTML" >"$VERBOSEOUT"
	fi
	if ! tsc -b "$HTMLDIR"; then
		echo "unable to build page" >&2
		return 1
	fi
	if [ ! -d "$RELEASEDIR" ]; then
		mkdir -p "$RELEASEDIR"
	fi
	if [ ! -r "$BUILDDIR/JavaScript.js" ]; then
		echo "JavaScript.js not found" >&2
		return 1
	fi
	sed -E -e \
		"/^[[:space:]]*var deploymentId/s/(var deploymentId[^=]*=[[:space:]]*)\"\"/\1\"$deployment\"/" \
		"$BUILDDIR/JavaScript.js" >"$BUILDDIR/JavaScript.js.deployment"
	sed -E -e \
		"/$INCLUDESTRING/{
			s|// $INCLUDESTRING||
			r $BUILDDIR/JavaScript.js.deployment
		}" "$HTMLDIR"/Index.html >"$RELEASEDIR"/index.html
}
buildScript() {
	echo "building GAS scripts" >"$VERBOSEOUT"
	cp -a "$GASDIR"/*.ts "$RELEASEDIR" || return 1
}
checkClasp() {
	if ! type clasp >"$DEBUGOUT"; then
		echo "unable to find 'clasp'" >&2
		return 1
	fi
	if [ ! -r "$GASDIR/.clasp.json" ]; then
		echo "script not associated with an existing project" >"$VERBOSEOUT"
		if [ -z "$NEWPROJECT" ]; then
			echo "no associated project found; use" >&2
			echo "'$0 -N'" >&2
			echo "to create a new project" >&2
			echo "see https://dev.mffer.org/devguide for details" >&2
			return 1
		else
			echo "creating new GAS project" >"$VERBOSEOUT"
			runClasp create --type sheets --title mffer >"$DEBUGOUT" || exit 1
			cp -a "$RELEASEDIR/.clasp.json" "$GASDIR"
			NEWPROJECT=""
		fi
	else
		if [ -n "$NEWPROJECT" ]; then
			echo "$GASDIR/.clasp.json already exists; not creating new project" >&2
			return 1
		fi
	fi
	# clasp 2.4.1 has weird behavior with the "rootDir" setting in .clasp.json;
	# we work around this by just removing the rootDir setting and changing to
	# the directory itself before pushing
	# (see https://github.com/google/clasp/issues/869)
	cp -a "$GASDIR/.clasp.json" "$RELEASEDIR" || exit 1
	runClasp settings rootDir '.' >"$DEBUGOUT" || exit 1
	# another dumb clasp bug (see https://github.com/google/clasp/issues/875)
	echo '{}' >"$RELEASEDIR"/package.json
	if [ -r "$GASDIR/appsscript.json" ]; then
		cp -a "$GASDIR/appsscript.json" "$RELEASEDIR" || return 1
	fi
}
checkDeployment() {
	if [ 1 != "$#" ]; then
		echo "checkDeployment requires an argument" >&2
		return 2
	fi
	if runClasp deployments 2>/dev/null \
		| grep -E "^- $1 @" >/dev/null; then
		return 0
	else
		return 1
	fi
}
checkTsc() {
	if ! type tsc >"$DEBUGOUT"; then
		echo "unable to find 'tsc'" >&2
		return 1
	fi
}
checkScp() {
	if ! type scp >"$DEBUGOUT"; then
		echo "unable to find 'scp'" >&2
		return 1
	fi
}
getLastDeployment() {
	lastdeployedversion=0
	if ! output="$(runClasp deployments \
		| sed -E -e '/^- [^@]* @([1-9][0-9]*) - /!d' \
			-e 's/^- [^@]* @([0-9]*) - .*$/\1/')"; then
		return 1
	fi
	for i in $output; do
		if [ "$lastdeployedversion" -lt "$i" ]; then
			lastdeployedversion="$i"
		fi
	done
	if ! claspOutput="$(runClasp deployments)"; then
		echo "$claspOutput" >&2
		return 1
	else
		if ! lastdeployment="$(
			echo "$claspOutput" \
				| sed -E -e '/^- [^@]* @'"$lastdeployedversion"' -/!d' \
					-e 's/^- ([^@]*) @'"$lastdeployedversion"' .*$/\1/'
		)"; then
			echo "$lastdeployment" >&2
			return 1
		fi
	fi
	echo "$lastdeployment"
}
getLastVersion() {
	# Note that getLastVersion is only run after a version is created, so we
	# don't need to handle the empty case
	if ! claspOutput="$(runClasp versions)"; then
		echo "$claspOutput" >&2
		return 1
	else
		if ! versions="$(echo "$claspOutput" | sed -E -e '/^[1-9][0-9]* - /!d' -e 's/^([1-9][0-9]*) - .*$/\1/')"; then
			echo "$lastVersion" >&2
			return 1
		else
			lastVersion=0
			for i in $versions; do
				if [ "$i" -gt "$lastVersion" ]; then
					lastVersion="$i"
				fi
			done
			echo "$lastVersion"
		fi
	fi
}
getOptions() {
	while getopts 'bD:ghNOp:vw:' option; do
		case "$option" in
			b)
				BUILDONLY=Y
				;;
			D)
				GASDEPLOYMENT="$OPTARG"
				;;
			g)
				WEBSERVER="AppsScript"
				;;
			h)
				USAGEONLY=y
				;;
			N)
				NEWPROJECT=Y
				;;
			O)
				OVERWRITEDEPLOYMENT=Y
				;;
			p)
				WEBSERVERPATH="$OPTARG"
				;;
			v)
				if [ -n "$VERBOSE" ]; then
					DEBUG=Y
				fi
				VERBOSE=Y
				;;
			w)
				WEBSERVER="$OPTARG"
				;;
			*)
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
newDeployment() {
	if [ 1 -lt "$#" ]; then
		echo "newDeployment takes at most 1 argument" >&2
		return 2
	elif [ 1 = "$#" ]; then
		if [ "@last" = "$1" ]; then
			GASDEPLOYMENT="$(getLastDeployment)" || return 1
		else
			if ! checkDeployment "$1"; then
				echo "unable to identify deployment '$1'" >&2
				return 1
			fi
			GASDEPLOYMENT="$1"
		fi
	fi
	runClasp version "$(date +%Y%m%dT%H%M%z)" >"$DEBUGOUT" || return 1
	lastversion="$(getLastVersion)" || return 1
	if [ -z "$GASDEPLOYMENT" ]; then
		# deploy it
		echo "creating new deployment, version $lastversion" >"$VERBOSEOUT"
		runClasp deploy -V "$lastversion" -d "$(date +%Y%m%dT%H%M%z)" >"$DEBUGOUT" || return 1
	else
		# deploy it in the GASDEPLOYMENT spot
		echo "redeploying new version $lastversion to $GASDEPLOYMENT" >"$VERBOSEOUT"
		runClasp deploy -V "$lastversion" -d "$(date +%Y%m%dT%H%M%z)" -i "$GASDEPLOYMENT" >"$DEBUGOUT" || return 1
	fi
}
pushPage() {
	server=""
	path=""
	if [ -z "$WEBSERVERPATH" ]; then #try to get server and path from WEBSERVER
		server="$(
			echo "$WEBSERVER" \
				| sed -E -e 's|^https?://||' \
					-e 's|^([^:/]*)$|\1|' \
					-e 's|^([^/:]*)[:/].*$|\1|'
		)"
		path="$(
			echo "$WEBSERVER" \
				| sed -E -e 's|^https?://||' \
					-e '\|/|!d' \
					-e 's|^[^/]*/(.*)$|\1|'
		)"
	elif ! echo "$WEBSERVERPATH" | grep '[:/]' >/dev/null; then # WEBSERVERPATH has no : or /, we'll assume it's just a server
		server="$WEBSERVERPATH"
		path=""
	elif echo "$WEBSERVERPATH" | grep '^[^/]*:' >/dev/null; then #WEBSERVERPATH has a : before any /, so the part before the first : is the server
		server="${WEBSERVERPATH%%:*}"
		path="${WEBSERVERPATH#*:}"
	else # There's no : before the first /, so we'll assume it's just a path and get the server from WEBSERVER as above
		server="$(
			echo "$WEBSERVER" \
				| sed -E -e 's|^https?://||' \
					-e 's|^([^:/]*)$|\1|' \
					-e 's|^([^/:]*)[:/].*$|\1|'
		)"
	fi
	echo "pushing web page to $server:$path" >"$VERBOSEOUT"
	scp -q "$RELEASEDIR"/index.html "$server":"$path"
}
pushScript() {
	echo "pushing Apps Script files" >"$VERBOSEOUT"
	runClasp push -f >"$DEBUGOUT" || return 1
}
runClasp() {
	worked=y
	for i in 1 2 3 4 5 6 7 8 9 10; do
		# as noted above, due to the behavior of clasp 2.4.1, we change to the
		# release directory before pushing the script files to Apps Script
		if ! output="$( (cd "$RELEASEDIR" && clasp -W "$@" 2>&1))" \
			&& worked="" \
			&& echo "$output" | grep 'Error: Looks like you are offline.' >/dev/null; then
			echo "$output" >"$DEBUGOUT"
			echo "retrying ($i/10)" >"$DEBUGOUT"
			worked=y
		else
			if [ -z "$worked" ]; then # some other error occurred
				echo "$output" >&2
				return 1
			fi
			echo "$output"
			return 0
		fi
	done
	echo "clasp was unable to connect" >&2
	return 1
}
# Prints an absolute pathname corresponding to directory $1, creating the
# directory if it does not exist.
setdir() {
	if [ "$#" != "1" ]; then
		echo 'usage: setdir dirname' >&2
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
main "$@"
