#!/bin/sh

# platform-independent functions for mffer testing
#
# This file should be "source"d rather than run; it should include only
# function definitions and settings. Functions here are platform-independent
# in the sense that they are expected to be run in a POSIX-like shell
# environment on any of the mffer-supported platforms *or* access and manage
# Parallels Desktop virtual machines on a macOS host system where the virtual
# machines may be any of the mffer-supported platforms.

# Functions rely on several already-defined environment variables/parameters.
#
# Variables affecting general program behavior
DEBUG="${DEBUG:-}"      # output everything, including script tracing
VERBOSE="${VERBOSE:-y}" # output brief progress information
PATH="${PATH:+$PATH:}/usr/local/bin:${HOME:-}/.dotnet"
#
# Parameters used throughout the program. The program will try to determine
# most of them dynamically if not explicitly set in the environment
MFFER_REPO="${MFFER_REPO:-https://github.com/therealchjones/mffer}"   # url
MFFER_SOURCE_DIR="${MFFER_SOURCE_DIR:-}"                              # where the local tree is or the download should go
MFFER_SOURCE_COMMIT="${MFFER_SOURCE_COMMIT:-}"                        # which commit to test
MFFER_TEST_TMPDIR="${MFFER_TEST_TMPDIR:-}"                            # disposable temporary directory
MFFER_USE_LOCAL="${MFFER_USE_LOCAL:-}"                                # if nonempty, use a local source tree rather than download
MFFER_VM_BASESNAP_NAME="${MFFER_VM_BASESNAP_NAME:-Base Installation}" # Name of the "clean install" snapshot on the testing VM
MFFER_VM_BASESNAP_ID="${MFFER_VM_BASESNAP_ID:-}"                      # ID of the "clean install" snapshot on the testing VM
MFFER_VM_HOSTNAME="${MFFER_VM_HOSTNAME:-}"                            # hostname of the VM on which to test
MFFER_VM_NAME="${MFFER_VM_NAME:-}"                                    # Name of the VM on which to test
MFFER_VM_OS="${MFFER_VM_OS:-}"                                        # OS of the VM on which to test
#
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
# setSomething - sets one or more environment variables, no output (probably not even needed)
# getSomething - prints value, potentially also determining that value and
#                setting the appropriate environment variable(s) if not already

createTempDir() {
	if ! MFFER_TEST_TMPDIR="$(mktemp -d -t mffer-test)" \
		|| [ -z "$MFFER_TEST_TMPDIR" ]; then
		return 1
	fi
}
# prints MFFER_VM_BASESNAP_ID, determining and setting it if not already
getBaseVmId() {
	if [ -n "$MFFER_VM_BASESNAP_ID" ]; then
		echo "$MFFER_VM_BASESNAP_ID"
		return 0
	fi
	setParallels || return 1
	setVm || return 1
	if ! snapshots="$("$PRLCTL" snapshot-list "$MFFER_VM_NAME" -j)"; then
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
					&& [ "$snapshotname" = "$MFFER_VM_BASESNAP_NAME" ]; then
					echo "$snapshotid"
					break
				fi
			done
	)" || [ -z "$snapshot_id" ]; then
		return 1
	fi
	MFFER_VM_BASESNAP_ID="$snapshot_id"
	echo "$MFFER_VM_BASESNAP_ID"
}
getTempDir() {
	if [ -z "${MFFER_TEST_TMPDIR:=}" ]; then
		if ! createTempDir; then
			return 1
		fi
	fi
	echo "$MFFER_TEST_TMPDIR"
}
# Prints (and sets, if necessary) the ALL_VMS parameter, a long string with all the Parallels
# virtual machine info in plist format
getVms() {
	if [ -z "${ALL_VMS:=}" ]; then setVms || return 1; fi
	echo "$ALL_VMS"
}
# uses any of MFFER_VM_NAME, MFFER_VM_HOSTNAME, MFFER_VM_OS, MFFER_VM_BASESNAP_ID, and
# MFFER_VM_BASESNAP_NAME (in that order of priority) that are already set to
# set MFFER_VM_NAME, MFFER_VM_HOSTNAME, and MFFER_VM_OS; if possible with the provided settings,
# will set the others as well. If multiple sets of values or no sets of values would be
# consistent with the initial set values, prints an error and returns 1
setVm() {
	if [ -z "$MFFER_VM_BASESNAP_NAME" ] && [ -z "$MFFER_VM_BASESNAP_ID" ]; then
		echo "Error: Neither MFFER_VM_BASESNAP_NAME nor MFFER_VM_BASESNAP_ID is" >&2
		echo "       set. Cannot define the virtual machine and snapshot to use." >&2
		return 1
	fi
	if [ -n "$MFFER_VM_NAME" ]; then
		if ! vmExists "$MFFER_VM_NAME"; then
			echo "Error: 'MFFER_VM_NAME' is set to '$MFFER_VM_NAME', which was" >&2
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
vmExists() {
	if [ "$#" -ne "1" ] || [ -z "$1" ]; then
		echo "Error: vmExists() requires a single nonempty argument" >&2
		return 1
	fi
	vms="$(getVms)"
	if ! vmcount="$(echo "$vms" | base64 -d \
		| plutil -extract vms raw -expect array - -o -)" \
		|| [ -z "$vmcount" ] \
		|| [ "$vmcount" = "0" ]; then
		return 1
	fi
	i=0
	while [ "$i" -lt "$vmcount" ]; do
		vmname="$(
			echo "$vms" | base64 -d \
				| plutil -extract "vms.$vmcount.Name" raw - -o -
		)"
		if [ "$1" = "$vmname" ]; then return 0; fi
	done
	return 1
}
