#!/bin/sh

# abstraction layer over Parallels Desktop Pro command-line tools

# This script should be "source"d rather than run. It should include functions
# that make use of the `prlctl`, `prl_disk_tool`, `prlsrvctl`, and associated
# tools to manage Parallels Desktop and Parallels virtual machines on macOS. It
# should not expect to have knowledge of the operating systems running on any of
# the Parallels virtual machines other than that obtainable via the Parallels
# command-line tools themselves.

# shellcheck disable=SC2034 # (used in calling script)
MFFER_TEST_SNAPSHOT_ID=""                            # ID of the "clean install" snapshot on the testing VM
MFFER_TEST_VM_HOSTNAME="${MFFER_TEST_VM_HOSTNAME:-}" # hostname of the VM on which to test

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
#
# Prints the hostname for the virtual machine named `vmname`. Returns 1 on a usage error, 0 otherwise.
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

# renameVm vmname newname
#
# Renames the VM named `vmname` as `newname`. Additionally removes any
# ~/.ssh/known_hosts entries for `newname` and replaces any for `vmname` with
# ones for `newname`.
renameVm() {
	if [ "$#" -ne 2 ] || [ -z "$1" ] || [ -z "$2" ]; then
		echo "Error: renameVm() requires two arguments" >&2
		return 1
	fi
	if ! vmExists "$1"; then
		echo "Error: VM '$1' not found" >&2
		return 1
	fi
	echo "renaming VM '$1' as '$2'" >"$VERBOSEOUT"
	oldhost="$(getVmHostname "$1")"
	newhost="$(getVmHostname "$2")"
	if ! "$PRLCTL" set "$1" --name "$2" >"$DEBUGOUT"; then
		echo "Error: Unable to rename VM '$1' as '$2'" >&2
		return 1
	fi
	newlines="$(
		grep -E "(^|,)[[:space:]]*${oldhost%.shared}(\.shared)?([[:space:]]|,)" "$HOME/.ssh/known_hosts" \
			| sed -E -e 's/(^|,)[[:space:]]*'"${oldhost%.shared}"'.shared([[:space:]]|,)/\1'"${oldhost%.shared}"'\2/g' \
				-e 's/(^|,)'"${oldhost%.shared}"'([[:space:]]|,)/\1'"${newhost%.shared},${newhost%.shared}.shared"'\2/g'
	)"
	ssh-keygen -R "${oldhost%.shared}" >"$DEBUGOUT" 2>&1 || true
	ssh-keygen -R "${oldhost%.shared}.shared" >"$DEBUGOUT" 2>&1 || true
	ssh-keygen -R "${newhost%.shared}" >"$DEBUGOUT" 2>&1 || true
	ssh-keygen -R "${newhost%.shared}.shared" >"$DEBUGOUT" 2>&1 || true
	echo "$newlines" >>"$HOME/.ssh/known_hosts"
}

# resetVm vmname snapshotname
#
# resets the VM named `vmname` to the snapshot named `snapshotname` and ensures
# the VM is running and accepting ssh connections. If either of the variables is
# empty, if the reset fails, or if it cannot be confirmed that the VM is running
# and accepting ssh connections, returns 1.
resetVm() {
	if [ "$#" -ne 2 ] || [ -z "$1" ] || [ -z "$2" ]; then
		echo "Error: resetVm() requires two arguments" >&2
		return 1
	fi
	if ! snapshotid="$(getSnapshotId "$1" "$2")"; then return 1; fi
	echo "resetting virtual machine '$1'" >"$VERBOSEOUT"
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
# saveSnapshot vmname snapshotname [snapshotdescription]
#
# Creates a new snapshot named 'snapshotname' (with description
# 'snapshotdescription' if given) on the virtual machine namd 'vmname'. Returns
# 0 if successful, prints an error message and returns 1 otherwise.
saveSnapshot() {
	if [ "$#" -lt 2 ] || [ "$#" -gt 3 ] || [ -z "$1" ] || [ -z "$2" ]; then
		echo "Error: saveSnapshot() requires 2 or 3 arguments" >&2
		return 1
	fi
	echo "Saving snapshot '$2' on virtual machine '$1'" >"$VERBOSEOUT"
	if ! vmExists "$1"; then
		echo "Error: VM '$1' not found" >&2
		return 1
	fi
	fail=0
	if [ -z "${3:-}" ]; then
		if ! "$PRLCTL" snapshot "$1" -n "$2" >"$DEBUGOUT"; then
			fail=1
		fi
	else
		if ! "$PRLCTL" snapshot "$1" -n "$2" -d "$3" >"$DEBUGOUT"; then
			fail=1
		fi
	fi
	if [ "$fail" = 1 ]; then
		echo "Error: Unable to create snapshot '$2' for VM '$1'" >&2
		return 1
	fi
	return 0
}

# startVm vmname
#
# Starts the virtual machine named vmname; does not return until the VM is
# running or times out. Returns 0 if started successfully, prints an error and
# returns 1 otherwise.
startVm() {
	if [ "$#" -ne 1 ] || [ -z "$1" ]; then
		echo "Error: startVm() requires a single argument" >&2
		return 1
	fi
	if ! vmExists "$1"; then
		echo "Error: VM '$1' not found" >&2
		return 1
	fi
	tries=10
	until vmIsRunning "$1" || [ "$tries" -lt 1 ]; do
		if ! "$PRLCTL" start "$1" >"$DEBUGOUT"; then
			echo "Error: Unable to start VM '$1'" >&2
			return 1
		fi
		tries="$((tries - 1))"
		sleep 5
	done
	if [ "$tries" -lt 1 ]; then
		echo "Error: Starting VM '$1' timed out" >&2
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
# vmIsRunning vmname
#
# Returns 0 if the VM named vmname is currently running, 1 if it is not, 2 if
# checking fails, and 255 if a usage error occurs. Allows a single spurious done
# or error status without returning.
vmIsRunning() {
	if [ "$#" -ne 1 ]; then
		echo "Error: vmIsRunning() requires a single argument" >&2
		return 255
	fi
	vm_status=""
	if ! vm_status="$("$PRLCTL" status "$1")" 2>"$DEBUGOUT"; then
		return 2
	fi
	if [ -z "${vm_status##VM "$1" exist *}" ]; then # this is in the right format
		vm_error=""
		if [ "stopped" = "${vm_status##* }" ]; then # the last word is 'stopped'
			if [ -z "${vm_done:=}" ]; then             # it might be a fluke; try one more time
				vm_done=true
				sleep 1
				if ! vmIsRunning "$1"; then # nope, really done or errored
					vm_done=""
					return 1
				else # false alarm
					vm_done=""
					return 0
				fi
			else # ah, this happened before
				vm_done=""
				return 1
			fi
		else # everything looks good, and the last word isn't 'stopped'
			vm_done=""
			return 0
		fi
	else                         # uh, that didn't work right
		if [ -z "$vm_error" ]; then # okay, it's the first time, try once more
			vm_error=true
			sleep 1
			vmIsRunning "$1"
			case "$?" in
				2) # yeah, it's real
					vm_error=""
					return 2
					;;
				1) # what, now it's stopped? Fine.
					vm_error=""
					return 1
					;;
				0) # yay, we recovered by just checking it again!
					vm_error=""
					return 0
					;;
				*) # some other error, I guess
					vm_error=""
					return 2
					;;
			esac
		else # we've been here before
			vm_error=""
			return 2
		fi
	fi
}
# Used in script sourcing this one
# shellcheck disable=SC2034
MFFER_TEST_VM_SYSTEM="parallels"
