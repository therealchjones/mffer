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
# system deployed in a testing virtual machine, and should be expected to run
# only in a POSIX-like shell environment. Specifically, settings and functions
# specific to other areas should be in different files:

# - functions managing Parallels Desktop or Parallels virtual machines should be
#   in testing/common/parallels.sh
# - functions managing specific operating system installations or running
#   command on those systems should be in testing/<operating system name>/

export PATH="${PATH:+$PATH:}/usr/local/bin:${HOME:-}/.dotnet"

# Parameters used throughout the program. The program will try to determine
# them dynamically if not explicitly set in the environment
MFFER_REPO="${MFFER_REPO:-https://github.com/therealchjones/mffer}" # url
MFFER_TEST_COMMIT="${MFFER_TEST_COMMIT:-}"                          # which commit to test
MFFER_TEST_DIR="${MFFER_TEST_DIR:-}"                                # directory containing `test.sh` and the subdirectories of the testing tree
MFFER_TEST_SNAPSHOT="${MFFER_TEST_SNAPSHOT:-Base Installation}"     # Name of the "clean install" snapshot on the testing VM
MFFER_TEST_VM="${MFFER_TEST_VM:-}"                                  # Name of the VM on which to test
MFFER_TREE_ROOT="${MFFER_TREE_ROOT:-}"                              # Root directory of the local mffer repository

# Intentionally global parameters set within the program. These are dynamically
# configured and should not be user-configurable
# shellcheck disable=SC2034 # (used in called scripts)
MFFER_TEST_RUNDIR=""
# shellcheck disable=SC2034 # (used in called scripts)
MFFER_TEST_SOURCE="" # where the local tree to test is
MFFER_TEST_TMPDIR="" # disposable temporary directory
# shellcheck disable=SC2034 # (used in calling scripts)
MFFER_TEST_VM_SYSTEM="" # set by the appropriate script when virtual machine functions are loaded
MFFER_TEST_OS=""

# Variables regarding other software that may be used for testing; leave blank
# to use whatever is available and found automatically
DOTNET_VERSION="${DOTNET_VERSION:-}"
NODE_VERSION="${NODE_VERSION:-}"
PARALLELS_VERSION="${PARALLELS_VERSION:-}"
PYTHON_VERSION="${PYTHON_VERSION:-3.10.6}"

# Variables that should be available to child processes or job processes
MFFER_EXPORT_VARS='
	DEBUG
	DEBUGOUT
	VERBOSE
	VERBOSEOUT
	MFFER_BUILD_OS
	MFFER_EXPORT_VARS
	MFFER_TEST_DIR
	MFFER_TEST_OS
	MFFER_TEST_RUNDIR
	MFFER_TEST_SOURCE
	MFFER_TEST_TMPDIR
'

# In an effort to have some consistency, functions should be named in
# camelCase using the following conventions:
#
# isSomething, hasSomething, or somethingExists - returns 0 or 1, no output
# checkSomething - ensures consistency or appropriate setting, returns 0 or 1,
#                  no output
# createSomething - make changes to a persistent store of some kind, like the
#                   filesystem or Parallels Desktop VM registry, potentially
#                   setting the appropriate environment variable if not already;
#                   usually should be called by getSomething rather than
#                   directly
# setSomething - sets one or more environment variables, no output
# getSomething - prints value, potentially also determining that value and
#                setting the appropriate environment variable(s) if not already

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
# checkVm
#
# ensures a VM named $MFFER_TEST_VM exists and includes a base installation
# snapshot named $MFFER_TEST_SNAPSHOT
checkVm() {
	if ! vmExists "$MFFER_TEST_VM"; then
		echo "Error: VM '$MFFER_TEST_VM' was not found" >&2
		return 1
	fi
	if ! hasSnapshot "$MFFER_TEST_VM" "$MFFER_TEST_SNAPSHOT"; then
		echo "Error: VM '$MFFER_TEST_VM' is not properly configured" >&2
		return 1
	fi
}
# prints MFFER_TEST_SNAPSHOT_ID, determining and setting it if not already
getBaseVmId() {
	if [ -n "$MFFER_TEST_SNAPSHOT_ID" ]; then
		echo "$MFFER_TEST_SNAPSHOT_ID"
		return 0
	fi
	if [ -z "${MFFER_TEST_VM:-}" ]; then
		echo "Error: MFFER_TEST_VM is not defined or is empty" >&2
		return 1
	fi
	setParallels || return 1
	if ! snapshots="$("$PRLCTL" snapshot-list "$MFFER_TEST_VM" -j)"; then
		echo 'Error: Unable to obtain list of virtual machine snapshots.' >&2
		return 1
	fi
	if ! snapshots="$(
		plutil -create xml1 - -o - \
			| plutil -insert snapshots \
				-json "$snapshots" - -o -
	)"; then
		return 1
	fi
	if ! snapshot_id="$(
		echo "$snapshots" \
			| plutil -extract snapshots raw - -o - \
			| while read -r snapshotid; do
				if snapshot="$(echo "$snapshots" | plutil -extract "$snapshotid" xml1 - -o -)" \
					&& snapshotname="$(echo "$snapshot" | plutil -extract name raw - -o -)" \
					&& [ "$snapshotname" = "$MFFER_TEST_SNAPSHOT" ]; then
					echo "$snapshotid"
					break
				fi
			done
	)" || [ -z "$snapshot_id" ]; then
		return 1
	fi
	MFFER_TEST_SNAPSHOT_ID="$snapshot_id"
	echo "$MFFER_TEST_SNAPSHOT_ID"
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

getTime() {
	date +%s
}
# isRoot
#
# Returns 0 if the effective user id is 0, i.e., the root user; returns 1 otherwise
isRoot() {
	[ 0 = "$(id -u)" ]
}
# isSudo
#
# Evaluates whether the effective user is root and sudo user is not root
# (regular). Returns 0 if the real (sudo) user is a regular user and the
# effective user is root, otherwise returns 1.
isSudo() {
	if ! isRoot || [ -z "$SUDO_UID" ] || [ "$SUDO_UID" = "0" ]; then
		return 1
	else
		return 0
	fi
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
runTest() {
	script=""
	if [ "$#" -ne 1 ] || [ -z "$1" ]; then
		echo "Error: runTest() requires a single argument" >&2
		return 1
	elif [ -z "$MFFER_TEST_RUNDIR" ]; then
		echo "Error: 'MFFER_TEST_RUNDIR' is required for runTest()" >&2
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
# setPasswordlessSudo
#
# Enables passwordless sudo for the primary user on the virtual machine with the
# name $MFFER_TEST_VM is such a virtual machine exists. Uses sudo, so requires
# entering password per regular sudo rules. Prints error and returns 255 if the
# VM does not exist, prints error and returns 1 if unsuccessful or cancelled.
setPasswordlessSudo() {
	echo "Enabling passwordless sudo on virtual machine" >"$VERBOSEOUT"
	if [ -z "${MFFER_TEST_VM:=}" ] || [ -z "${MFFER_TEST_VM_HOSTNAME:=}" ]; then
		echo "Error: MFFER_TEST_VM or MFFER_TEST_VM_HOSTNAME is empty; run setVm before setPasswordlessSudo" >&2
		return 255
	elif ! vmExists "$MFFER_TEST_VM"; then
		echo "Error: No VM named '$MFFER_TEST_VM' is registered with Parallels Desktop" >&2
		return 255
	fi
	if ! username="$(ssh "$MFFER_TEST_VM_HOSTNAME" 'echo $USER')" \
		|| [ -z "$username" ]; then
		echo "Error: Unable to get name of primary user for VM '$MFFER_TEST_VM'" >&2
		return 1
	fi
	echo "Warning: Password for user '$username' on VM '$MFFER_TEST_VM' may be required" >&2
	if ! ssh -qt "$MFFER_TEST_VM_HOSTNAME" "echo $username 'ALL = (ALL) NOPASSWD: ALL' | sudo EDITOR='tee -a' visudo"; then
		echo "Error: Unable to enable passwordless sudo for user '$username' on VM '$MFFER_TEST_VM'" >&2
		return 1
	fi
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
setVerbosity() {
	DEBUG="${DEBUG:-}"
	VERBOSE="${VERBOSE:-}"
	DEBUGOUT="${DEBUGOUT:-/dev/null}"
	VERBOSEOUT="${VERBOSEOUT:-/dev/null}"
	if [ -n "$DEBUG" ]; then
		set -x
		set -e
		set -u
		VERBOSE=y
		DEBUGOUT="/dev/stdout"
	fi
	if [ -n "$VERBOSE" ]; then
		VERBOSEOUT="/dev/stdout"
	fi
	export DEBUG VERBOSE
}
# setVm vmos
#
# Sets the MFFER_TEST_VM variable to the appropriate name for OS `vmos` and
# confirms it exists and has a snapshot named $MFFER_TEST_SNAPSHOT (or "Base
# Installation" if that value is empty). Prints an error and returns 1 if no VM
# with the appropriate name exists or it doesn't have a snapshot named
# appropriately. Prints an error and returns 255 if a usage error occurs.
# Returns 0 otherwise.
setVm() {
	if [ "$#" -ne 1 ] || [ -z "$1" ]; then
		echo "Error: setVm() requires a single argument" >&2
		return 255
	fi
	case "$1" in
		macos)
			MFFER_TEST_VM="macOS Testing"
			;;
		linux)
			MFFER_TEST_VM="Linux Testing"
			;;
		windows)
			MFFER_TEST_VM="Windows Testing"
			;;
		*)
			echo "Error: unknown argument to setVm() '$1'" >&2
			return 1
			;;
	esac
	MFFER_TEST_OS="$1"
	MFFER_TEST_SNAPSHOT="${MFFER_TEST_SNAPSHOT:-Base Installation}"
	if ! checkVm; then
		echo "Error: Unable to use virtual machine '$1'" >&2
		return 1
	fi
	updateEnv
}
# sshIsRunning
#
# Returns 0 if able to connect to MFFER_TEST_VM_HOSTNAME on
# port 22, nonzero otherwise
sshIsRunning() {
	if ! command -v nc >"$DEBUGOUT" 2>&1; then
		echo "Error: 'nc' command not found" >&2
		return 1
	fi
	nc -z "$MFFER_TEST_VM_HOSTNAME" 22 >"$DEBUGOUT" 2>&1
}
# updateEnv
#
# Ensures variables are properly exported for child processes and updated in the
# GitHub Actions environment for steps of the same job
updateEnv() {
	# shellcheck disable=SC2034 # (used in called scripts)
	MFFER_TEST_RUNDIR="${MFFER_TEST_TMPDIR:-}/built-on-${MFFER_BUILD_OS:-}/${MFFER_TEST_OS:-}"
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
waitForShutdown() {
	starttime="$(getTime)"
	maxtime="$((10 * 60))" # 10 minutes, in seconds
	until ! vmIsRunning "$1"; do
		time="$(getTime)"
		if [ -z "$starttime" ] || [ -z "$time" ]; then
			echo "Error: Unable to get the waiting time" >&2
			return 1
		fi
		if [ "$((time - starttime))" -ge "$maxtime" ]; then
			echo "Error: Timed out; VM never shut down" >&2
			return 1
		fi
		sleep 5
	done
}
waitForStartup() {
	starttime="$(getTime)"
	maxtime="$((10 * 60))" # 10 minutes, in seconds
	until sshIsRunning "$1"; do
		time="$(getTime)"
		if [ -z "$starttime" ] || [ -z "$time" ]; then
			echo "Error: Unable to get the installation time" >&2
			return 1
		fi
		if [ "$((time - starttime))" -ge "$maxtime" ]; then
			echo "Error: Timed out; VM never made SSH accessible" >&2
			return 1
		fi
		sleep 5
	done
}
setVerbosity
setTmpdir
setBuildOs
trap cleanup EXIT
updateEnv
