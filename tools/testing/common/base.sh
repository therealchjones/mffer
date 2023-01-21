#!/bin/sh

# function definitions and startup configuration for mffer testing

# This file is designed to be "source"d by a test framework script to provide
# functions and environment variables useful to the individual test scripts so
# that they need not be dependent upon the specific test framework. For
# instance, in the `mffer` repository the tools/testing/test.sh script sources
# this one just after determining its own location. Similarly, GitHub Actions
# testing workflows can source this script to ensure environment variables are
# properly stored for multiple steps. Note that when this is sourced by another
# script functions remain available in the calling script, but when used for
# setting environment variables (as in GitHub actions), functions are not
# included.

# This file should include function definitions, settings, and shared startup
# behavior such as processing the DEBUG and VERBOSE environment variables and
# defining common parameters. The script may be sourced more than once; take
# precautions to ensure you're not overwriting something changed since the last
# time. Functions in this script should generally not depend upon the operating
# system on which it is being run, and should be expected to run
# only in a POSIX-like shell environment. Specifically, settings and functions
# specific to other areas should be in different files:

# - functions utilizing virtual machines without regard to the VM framework
#   (e.g., Parallels Desktop) should be in testing/test.sh or similar
#   higher-level scripts
# - functions directly managing virtual machines at the level of the VM
#   framework (e.g., Parallels Desktop) should be in
#   testing/common/<framework>.sh, e.g., testing/common/parallels.sh
# - functions managing specific operating system installations or running
#   commands on those systems should be in testing/<operating system name>/

export PATH="${PATH:+$PATH:}/usr/local/bin"

# cleanup
#
# function to be run just before exiting the script (that is, the one sourcing this one)
cleanup() {
	exitstatus="$?"
	trap - EXIT
	echo "Cleaning up" >"${VERBOSEOUT:=/dev/null}"
	if [ -n "${MFFER_TEST_TMPDIR:=}" ]; then
		if [ 0 != "$exitstatus" ] && [ -n "${DEBUG:=}" ]; then
			echo "Warning: Testing files in progress retained in ${MFFER_TEST_TMPDIR}" >&2
		elif ! rm -rf "$MFFER_TEST_TMPDIR" >"${DEBUGOUT:=/dev/null}" 2>&1 \
			&& ! { chmod -R u+w "$MFFER_TEST_TMPDIR" && rm -rf "$MFFER_TEST_TMPDIR" >"${DEBUGOUT:=/dev/null}" 2>&1; }; then
			echo "Error: Unable to delete temporary directory '$MFFER_TEST_TMPDIR'" >&2
			if [ "$exitstatus" -eq 0 ]; then exitstatus=1; fi
		fi
	fi
	exit "$exitstatus"
}
endBuild() {
	if [ "${buildfails:=0}" -gt 0 ]; then
		echo "FAILED building '$1'" >&2
		return 1
	else
		echo "PASSED building '$1'" >"$VERBOSEOUT"
		return 0
	fi
}
getCanonicalDir() {
	if [ -n "${1:-}" ] && [ -d "$1" ] && (cd "$1" && pwd); then
		return 0
	else
		warnError "Unable to access directory '${1:-}'"
		return 1
	fi
}
# getEnv
#
# Prints the current values of the MFFER_EXPORT_VARS in a manner similar to
# export -p so that they may be reimported
getEnv() {
	for var in $MFFER_EXPORT_VARS; do
		value=""
		eval "value=\$$var"
		printf "%s='%s'\n" "$var" "$value"
	done
}
# getScript scriptbase
#
# Prints the path to the appropriate script for MFFER_TEST_OS with the base name
# scriptbase
getScript() {
	if [ "$#" -ne 1 ] || [ -z "$1" ]; then
		echo "Error: getScript() requires a single argument" >&2
		return 1
	elif [ -z "$MFFER_TEST_OS" ]; then
		echo "Error: 'MFFER_TEST_OS' is required for getScript()" >&2
		return 1
	elif [ -z "$MFFER_TEST_DIR" ]; then
		echo "Error: 'MFFER_TEST_DIR' is required for getScript()" >&2
		return 1
	fi
	for file in "$MFFER_TEST_DIR/$MFFER_TEST_OS/$1.sh" "$MFFER_TEST_DIR/$MFFER_TEST_OS/$1.bat" "$MFFER_TEST_DIR/common/$1.sh"; do
		if [ -r "$file" ]; then
			echo "$file"
			return 0
		fi
	done
	return 1
}
# getTempDir
#
# Prints the path of MFFER_TEST_TMPDIR, setting MFFER_TEST_TMPDIR to a new
# temporary directory if it is null or unset. Returns 0 if successful, 1
# otherwise.
getTempDir() {
	if [ -z "$MFFER_TEST_TMPDIR" ] || [ ! -d "$MFFER_TEST_TMPDIR" ]; then
		setTmpdir || return 1
	fi
	echo "$MFFER_TEST_TMPDIR"
}
# getTime
#
# Prints the time in a standardized YYYYMMDD format
getTime() {
	date +%s
}
# isTreeClean dirname
#
# Evaluates whether dirname is in a clean git repository, i.e., one without
# uncommitted changes
isTreeClean() {
	if [ "$#" -ne 1 ] || [ -z "$1" ]; then
		echo "Error: isTreeClean() requires a single argument" >&2
		return 1
	fi
	if [ ! -d "$1" ]; then
		echo "Error: '$1' is not an accessible directory" >&2
		return 1
	fi
	output=""
	if ! output="$(git -C "$1" status --porcelain)" \
		|| [ -n "$output" ]; then
		return 1
	fi
	return 0
}
runBuild() {
	system=''
	system="$(getLocalOs)" || return 1
	testdir=''
	testdir="$(getTestDir)" || return 1
	buildscript=''
	for systemname in "$system" common; do
		for filename in "build-$1.sh" "build-$1.bat"; do
			if [ -r "$testdir/$systemname/$filename" ]; then
				buildscript="$testdir/$systemname/$filename"
				break 2
			fi
		done
	done
	if [ -z "$buildscript" ]; then
		echo "Error: unable to find a build script for '$1'" >&2
		return 1
	fi
	if MFFER_TEST_FRAMEWORK="$MFFER_TEST_FRAMEWORK" sh "$buildscript"; then
		echo "FAILED building '$1' with '$buildscript'" >&2
		buildfails="$(("${buildfails:=0}" + 1))"
		return 1
	else
		echo "PASSED building '$1' with '$buildscript'" >"$VERBOSEOUT"
		return 0
	fi
}
runTest() {
	script=""
	if [ "$#" -ne 1 ] || [ -z "$1" ]; then
		echo "Error: runTest() requires a single argument" >&2
		return 1
	elif [ -z "$MFFER_TEST_BINDIR" ]; then
		echo "Error: 'MFFER_TEST_BINDIR' is required for runTest()" >&2
		return 1
	elif ! script="$(getScript test-"$1")" || [ -z "$script" ]; then
		echo "Error: test script for '$1' not found" >&2
		return 1
	elif ! sh "$script"; then
		return $?
	else
		return 0
	fi
}
setBuildOs() {
	if [ -n "${MFFER_BUILD_OS:=}" ]; then
		return 0
	fi
	output=""
	if ! output="$(uname -s)" || [ -z "$output" ]; then
		echo "Error: Unable to determine operating system" >&2
		return 1
	fi
	case "$output" in
		Darwin)
			MFFER_BUILD_OS="macos"
			;;
		Linux)
			MFFER_BUILD_OS="linux"
			;;
		*)
			echo "Warning: Operating system '$output' not recognized; assuming Windows" >&2
			MFFER_BUILD_OS="windows"
			;;
	esac
}
# setSources
#
# Identifies a directory tree housing an mffer repository to test using the
# variables MFFER_TEST_COMMIT and MFFER_TREE_ROOT, and sets MFFER_TEST_SOURCE.
# Returns 1 and prints an error if unable to identitfy a directory tree allowing
# any of the variables currently set to remain the same.
#
# MFFER_TREE_ROOT: set based on script location and initial working directory,
#                  the copy of the repository from which the test script is
#                  running
# MFFER_TEST_COMMIT: the commit (or any other object that resolves with git
#                    rev-parse) to test; leave empty to test MFFER_TREE_ROOT as it is
# MFFER_TEST_SOURCE: the root of the repository to test, not necessarily the
#                    same as MFFER_TREE_ROOT. Not inherited.
setSources() {
	if [ -n "${MFFER_TEST_SOURCE:=}" ]; then
		# assume we've already run this
		return 0
	fi
	if [ -z "${MFFER_TREE_ROOT:=}" ]; then
		# should have been set by the script startup
		echo "Error: MFFER_TREE_ROOT is not set" >&2
		return 1
	fi
	# Use the tree as it is, or get a specific commit?
	headtag=""
	if ! isTreeClean "$MFFER_TREE_ROOT" \
		|| ! headtag="$(git -C "$MFFER_TREE_ROOT" tag --points-at)"; then
		headtag=""
	fi
	if [ -z "${MFFER_TEST_COMMIT:=}" ]; then
		MFFER_TEST_SOURCE="$MFFER_TREE_ROOT"
		MFFER_TEST_COMMIT="$headtag"
	elif [ -n "$headtag" ]; then
		headcommit=""
		wantcommit=""
		if headcommit="$(git -C "$MFFER_TREE_ROOT" rev-parse --verify --end-of-options "$headtag")" \
			&& wantcommit="$(git -C "$MFFER_TREE_ROOT" rev-parse --verify --end-of-options "$MFFER_TEST_COMMIT")" \
			&& [ -n "$headcommit" ] \
			&& [ -n "$wantcommit" ] \
			&& [ "$headcommit" = "$wantcommit" ]; then
			MFFER_TEST_SOURCE="$MFFER_TREE_ROOT"
			MFFER_TEST_COMMIT="$headtag"
		fi
	else # MFFER_TEST_COMMIT is nonempty and MFFER_TREE_ROOT is not at MFFER_TEST_COMMIT, so we maybe download
		if [ -z "${MFFER_REPO:=}" ]; then
			echo "Error: unable to download mffer repository; MFFER_REPO is unset or empty" >&2
			return 1
		elif ! git ls-remote --exit-code "$MFFER_REPO" "$MFFER_TEST_COMMIT" >"$DEBUGOUT"; then
			echo "Error: unable to find revision given by MFFER_TEST_COMMIT ('$MFFER_TEST_COMMIT')" >&2
			return 1
		elif ! git clone -q "$MFFER_REPO" "$(getTmpDir)"/mffer >"$DEBUGOUT" \
			|| ! git -C "$(getTmpDir)"/mffer checkout -q --detach "$MFFER_TEST_COMMIT" >"$DEBUGOUT"; then
			echo "Error: unable to checkout repository at revision given by MFFER_TEST_COMMIT ('$MFFER_TEST_COMMIT')" >&2
			return 1
		fi
		MFFER_TEST_SOURCE="$(getTmpDir)"/mffer
	fi
	# Finally, no matter how we got there, we have MFFER_TEST_SOURCE and (maybe) MFFER_TEST_COMMIT
	# Let's be sure if they're both set then there's not a conflict
	if [ -n "$MFFER_TEST_COMMIT" ]; then
		wantcommit=""
		headcommit=""
		if ! headcommit="$(git -C "$MFFER_TEST_SOURCE" rev-parse --verify --end-of-options HEAD)" \
			|| ! wantcommit="$(git -C "$MFFER_TEST_SOURCE" rev-parse --verify --end-of-options "$MFFER_TEST_COMMIT")" \
			|| [ -z "$headcommit" ] \
			|| [ -z "$wantcommit" ] \
			|| [ "$headcommit" != "$wantcommit" ]; then
			echo "Error: '$MFFER_TEST_SOURCE' is not at commit '$MFFER_TEST_COMMIT'" >&2
			return 1
		fi
	fi
}
# setTmpdir
#
# If MFFER_TEST_TMPDIR doesn't already point to an existing directory, create a
# temporary directory and set MFFER_TEST_TMPDIR to its name
setTmpdir() {
	if [ -n "${MFFER_TEST_TMPDIR:=}" ]; then
		if [ ! -d "$MFFER_TEST_TMPDIR" ] \
			|| ! output="$(ls "$MFFER_TEST_TMPDIR")"; then
			echo "$output" >"$DEBUGOUT"
			echo "Error: 'MFFER_TEST_TMPDIR' is set to '$MFFER_TEST_TMPDIR'," >&2
			echo "       but that isn't working." >&2
			return 1
		fi
		return 0
	fi
	cmd="mktemp -d -t mffer-test"
	if isSudo; then cmd="sudo -u $SUDO_USER $cmd"; fi
	if ! MFFER_TEST_TMPDIR="$($cmd)" \
		|| [ -z "$MFFER_TEST_TMPDIR" ]; then
		echo "Error: Unable to create temporary directory" >&2
		return 1
	fi
	return 0
}
startBuild() {
	echo "Starting build '$1'" >"$VERBOSEOUT"
	buildfails=0
}
# updateEnv
#
# Ensures variables are properly exported for child processes and updated in the
# GitHub Actions environment for steps of the same job
updateEnv() {
	# shellcheck disable=SC2034 # (used in called scripts)
	MFFER_TEST_BINDIR="${MFFER_TEST_TMPDIR:-}/built-on-${MFFER_BUILD_OS:-}/${MFFER_TEST_OS:-}"
	MFFER_TEST_DIR="${MFFER_TREE_ROOT:-}/tools/testing"
	if [ -n "${MFFER_EXPORT_VARS}" ]; then
		for var in $MFFER_EXPORT_VARS; do
			export "${var?}"
		done
	fi
	if [ -n "${GITHUB_ENV:=}" ] && [ -r "$GITHUB_ENV" ]; then
		getEnv >>"$GITHUB_ENV"
	fi
}

updateEnv
