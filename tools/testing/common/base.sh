#!/bin/sh

# function definitions and startup configuration for mffer testing

# This file should be "source"d rather than run. It should include function
# definitions, settings, and shared startup behavior such as processing the
# DEBUG and VERBOSE environment variables and defining common parameters. The
# script may be sourced more than once; take precautions to ensure you're not
# overwriting something changed since the last time. Functions in this script
# should generally not depend upon the operating system deployed in a testing
# virtual machine, and should be expected to run only in a POSIX-like shell
# environment. Specifically, settings and functions specific to other areas
# should be in different files:
# - functions managing Parallels Desktop or Parallels virtual machines should be
#   in testing/common/parallels.sh
# - functions managing specific operating system installations or running
#   command on those systems should be in testing/<operating system name>/

PATH="${PATH:+$PATH:}/usr/local/bin:${HOME:-}/.dotnet"

# Parameters used throughout the program. The program will try to determine
# most of them dynamically if not explicitly set in the environment
MFFER_REPO="${MFFER_REPO:-https://github.com/therealchjones/mffer}" # url
MFFER_SOURCE_DIR="${MFFER_SOURCE_DIR:-}"                            # where the local tree is or the download should go
MFFER_SOURCE_COMMIT="${MFFER_SOURCE_COMMIT:-}"                      # which commit to test
MFFER_TEST_TMPDIR="${MFFER_TEST_TMPDIR:-}"                          # disposable temporary directory
MFFER_USE_LOCAL="${MFFER_USE_LOCAL:-}"                              # if nonempty, use a local source tree rather than download
MFFER_TEST_SNAPSHOT="${MFFER_TEST_SNAPSHOT:-Base Installation}"     # Name of the "clean install" snapshot on the testing VM
MFFER_TEST_SNAPSHOT_ID="${MFFER_TEST_SNAPSHOT_ID:-}"                # ID of the "clean install" snapshot on the testing VM
MFFER_TEST_VM_HOSTNAME="${MFFER_TEST_VM_HOSTNAME:-}"                # hostname of the VM on which to test
MFFER_TEST_VM="${MFFER_TEST_VM:-}"                                  # Name of the VM on which to test
MFFER_VM_OS="${MFFER_VM_OS:-}"                                      # OS of the VM on which to test

# Variables regarding other software that may be used for testing; leave blank
# to use whatever is available and found automatically
DOTNET_VERSION="${DOTNET_VERSION:-}"
NODE_VERSION="${NODE_VERSION:-}"
PARALLELS_VERSION="${PARALLELS_VERSION:-}"
PYTHON_VERSION="${PYTHON_VERSION:-3.10.6}"
PRLCTL="${PRLCTL:-}" # path to prlctl
#
# Global variables that are defined in the course of the script but should not
# be imported or user-configurable
ALL_VMS=""

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
	if [ -n "${MFFER_TEST_TMPDIR:=}" ]; then
		if ! rm -rf "$MFFER_TEST_TMPDIR" >"$DEBUGOUT" 2>&1 \
			&& ! { chmod -R u+w "$MFFER_TEST_TMPDIR" && rm -rf "$MFFER_TEST_TMPDIR"; }; then
			echo "Error: Unable to delete temporary directory '$MFFER_TEST_TMPDIR'" >&2
			if [ "$exitstatus" -eq 0 ]; then exitstatus=1; fi
		fi
	fi
	exit "$exitstatus"
}
# prints MFFER_TEST_SNAPSHOT_ID, determining and setting it if not already
getBaseVmId() {
	if [ -n "$MFFER_TEST_SNAPSHOT_ID" ]; then
		echo "$MFFER_TEST_SNAPSHOT_ID"
		return 0
	fi
	setParallels || return 1
	setVm || return 1
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
# getTempDir
#
# Prints the path of MFFER_TEST_TMPDIR, setting MFFER_TEST_TMPDIR to a new
# temporary directory if it is null or unset. Returns 0 if successful, 1
# otherwise.
getTempDir() {
	setTmpdir || return 1
	echo "$MFFER_TEST_TMPDIR"
}

getTime() {
	date +%s
}
# Prints (and sets, if necessary) the ALL_VMS parameter, a long string with all the Parallels
# virtual machine info in plist format
getVms() {
	if [ -z "${ALL_VMS:=}" ]; then setVms || return 1; fi
	echo "$ALL_VMS"
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
# uses any of MFFER_TEST_VM, MFFER_TEST_VM_HOSTNAME, MFFER_VM_OS, MFFER_TEST_SNAPSHOT_ID, and
# MFFER_TEST_SNAPSHOT (in that order of priority) that are already set to
# set MFFER_TEST_VM, MFFER_TEST_VM_HOSTNAME, and MFFER_VM_OS; if possible with the provided settings,
# will set the others as well. If multiple sets of values or no sets of values would be
# consistent with the initial set values, prints an error and returns 1
setVm() {
	if [ -z "${MFFER_TEST_SNAPSHOT:=}" ] && [ -z "${MFFER_TEST_SNAPSHOT_ID:=}" ]; then
		echo "Error: Neither MFFER_TEST_SNAPSHOT nor MFFER_TEST_SNAPSHOT_ID is" >&2
		echo "       set. Cannot define the virtual machine and snapshot to use." >&2
		return 1
	fi
	if [ -n "${MFFER_TEST_VM:=}" ]; then
		if ! vmExists "$MFFER_TEST_VM"; then
			echo "Error: 'MFFER_TEST_VM' is set to '$MFFER_TEST_VM', which was" >&2
			echo "       not found." >&2
			return 1
		fi
	fi
}
# defines ALL_VMS, a base64-encoded binary plist of all VM information
# registered with the local copy of Parallels Desktop
setVms() {
	setParallels || return 1
	if [ "$("$PRLCTL" list -i -j | wc -c)" -gt "$(("$(getconf ARG_MAX)" / 2))" ]; then
		echo "Warning: the list of virtual machines is very long; scripts may fail." >&2
	fi
	if ! json="$("$PRLCTL" list -i -j)" || [ -z "$json" ]; then
		echo "Error: Unable to obtain Parallels virtual machine information" >&2
		return 1
	fi
	if ! plist="$(plutil -create binary1 - -o - \
		| plutil -insert vms -json "$json" - -o - \
		| base64)" || [ -z "$plist" ]; then
		echo "Error: Unable to parse Parallels virtual machine information" >&2
		return 1
	fi
	ALL_VMS="$plist"
	return 0
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

setVerbosity
trap cleanup EXIT
