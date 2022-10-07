#!/bin/sh

# abstraction layer over Parallels Desktop Pro command-line tools

# This script should be "source"d rather than run. It should include functions
# that make use of the `prlctl`, `prl_disk_tool`, `prlsrvctl`, and associated
# tools to manage Parallels Desktop and Parallels virtual machines on macOS. It
# should not expect to have knowledge of the operating systems running on any of
# the Parallels virtual machines other than that obtainable via the Parallels
# command-line tools themselves.

if [ "Darwin" != "$(uname -s)" ]; then
	echo "Error: Not on macOS." >&2
	exit 1
fi
PRLCTL="${PRLCTL:-prlctl}"
PRLSRVCTL="${PRLSRVCTL:-prlsrvctl}"
PRLDISKTOOL="${PRLDISKTOOL:-prl_disk_tool}"
DEBUGOUT="${DEBUGOUT:-/dev/null}"
VERBOSEOUT="${VERBOSEOUT:-/dev/null}"

for path in "$PRLCTL" "$PRLSRVCTL" "$PRLDISKTOOL"; do
	if [ -z "$path" ] || ! command -v "$path" >"${DEBUGOUT:-/dev/null}"; then
		message="Error: Unable to locate Parallels Desktop command line tools."
		message="$message\n       Skipping Parallels Desktop setup."
		echo "$message" >&2
		exit 1
	fi
done

# getSnapshotid vmname snapshotname
#
# Prints the ID (with curly brackets) of the snapshot named `snapshotname` on
# the virtual machine named `vmname`. If no such VM exists or it does not
# contain such a snapshot, prints an error message and returns 1.
getSnapshotId() {
	if [ "$#" -ne 2 ] || [ -z "$1" ] || [ -z "$2" ]; then
		echo "Error: setSnapshotId() requires two arguments" >&2
		return 1
	fi
	if ! vmExists "$1"; then
		echo "Error: Unable to identify VM '$1'" >&2
		return 1
	fi
	output=""
	if ! output="$("$PRLCTL" snapshot-list "$1" -j)"; then
		echo "Error: Unable to get the list of snapshots for VM '$1'" >&2
		return 1
	fi
	if [ -z "$output" ]; then
		echo "Error: VM '$1' has no snapshots" >&2
		return 1
	fi
	snapshots=""
	if ! snapshots="$(plutil -create xml1 - -o - | plutil -insert snapshots -json "$output" - -o -)" \
		|| [ -z "$snapshots" ]; then
		echo "Error: Unable to parse the list of snapshots for VM '$1'" >&2
		return 1
	fi
	for snapshotid in $(echo "$snapshots" | plutil -extract snapshots raw -expect dictionary - -o -); do
		if snapshotname="$(echo "$snapshots" | plutil -extract "snapshots.$snapshotid.name" raw -expect string - -o -)" \
			&& [ "$2" = "$snapshotname" ]; then
			echo "$snapshotid"
			return 0
		fi
	done
	echo "Error: VM '$1' does not contain a snapshot named '$2'" >&2
	return 1
}

# getVmHostname vmname
getVmHostname() {
	if [ "$#" -ne 1 ] || [ -z "$1" ]; then
		echo "Error: getVmHostname() requires one argument" >&2
		return 1
	fi
	echo "$1" | tr 'A-Z ' 'a-z-'
}

# hasSnapshot vmname snapshotname
#
# Returns 0 if the virtual machine named `vmname` includes a snapshot named
# `snapshotname`, otherwise returns 1
hasSnapshot() {
	if [ "$#" -ne 2 ] || [ -z "$1" ] || [ -z "$2" ]; then
		echo "Error: resetVm() requires two arguments" >&2
		return 1
	fi
	if ! snapshotid="$(getSnapshotId "$1" "$2")" \
		|| [ -z "$snapshotid" ]; then return 1; fi
	return 0
}

# resetVm vmname snapshotname
#
# resets the VM named `vmname`` to the snapshot named `snapshotname` and ensures
# the VM is running and accepting ssh connections. If either of the variables is
# empty, if the reset fails, or if it cannot be confirmed that the VM is running
# and accepting ssh connections, returns 1.
resetVm() {
	if [ "$#" -ne 2 ] || [ -z "$1" ] || [ -z "$2" ]; then
		echo "Error: resetVm() requires two arguments" >&2
		return 1
	fi
	if ! snapshotid="$(getSnapshotId "$1" "$2")"; then return 1; fi
	echo "Resetting virtual machine '$1'" >"$VERBOSEOUT"
	if ! "$PRLCTL" snapshot-switch "$1" --id "$snapshotid" >"$DEBUGOUT"; then
		echo "Error: Unable to reset VM '$1' to snapshot '$2'" >&2
		return 1
	fi
	tries=5
	until [ "$tries" -lt 1 ]; do
		vm_status=""
		if ! vm_status="$("$PRLCTL" status "$1")" \
			|| [ -z "$vm_status" ]; then
			echo "Error: Unable to get status of VM '$1'" >&2
			return 1
		fi
		vm_status="${vm_status#VM "$1" exist }"
		case "$vm_status" in
			stopped | suspended)
				if ! "$PRLCTL" start "$1" >"$DEBUGOUT"; then
					echo "Error: Unable to start VM '$1'" >&2
					return 1
				fi
				;;
			running)
				break
				;;
			*)
				sleep 5
				;;
		esac
	done
	if [ "running" != "$vm_status" ]; then
		echo "Error: VM '$1' did not start" >&2
		return 1
	fi
	# wait for the VM to start
	hostname="$(getVmHostname "$1")"
	tries=12
	until ssh -q -o ConnectTimeout=30 "$hostname" exit || [ "$tries" -lt 1 ]; do
		sleep 5
		tries="$((tries - 1))"
	done
	if [ "$tries" -lt 1 ]; then
		echo "Error: Unable to connect to reset virtual machine '$1' ('$hostname')" >&2
		return 1
	fi
}
# vmExists vmname
# Returns 0 if a VM named vmname exists, 1 if it does not or if checking fails,
# and 255 if a usage error occurs
vmExists() {
	if [ "$#" -ne 1 ]; then
		echo "Error: vmExists() requires a single argument" >&2
		return 255
	fi
	if ! output="$(prlctl status "$1" 2>&1)" \
		|| ! { echo "$output" | grep "^VM $1 exist " >/dev/null; } \
		|| [ -z "$output" ]; then
		echo "$output" >"$DEBUGOUT"
		return 1
	fi
}
# Used in script sourcing this one
# shellcheck disable=SC2034
MFFER_TEST_VM_SYSTEM="parallels"
