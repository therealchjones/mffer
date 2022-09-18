#!/bin/sh

# Test mffer build and operation on a Linux virtual machine

# Options are set via the below environment variables; there are no command-line
# flags or switches

set -e
set -u

VERBOSE="${VERBOSE:-y}"
DEBUG="${DEBUG:-}"

VERBOSEOUT="/dev/null"
DEBUGOUT="/dev/null"
if [ -n "$DEBUG" ]; then
	set -x
	VERBOSE=y
	DEBUGOUT="/dev/stdout"
fi
if [ -n "$VERBOSE" ]; then
	VERBOSEOUT="/dev/stdout"
fi

if [ 0 != "$#" ]; then
	echo "$(basename "$0") does not accept arguments." >&2
	echo "Usage: sh '$0'" >&2
	exit 1
fi
LINUX_INSTALLER="${LINUX_INSTALLER:-}"
LINUX_INSTALLER_URL="${LINUX_INSTALLER_URL:-https://releases.ubuntu.com/jammy/ubuntu-22.04.1-desktop-amd64.iso}"
LINUX_VM_NAME="${LINUX_VM_NAME:-Linux Testing}"
LINUX_VM_HOSTNAME="${LINUX_VM_HOSTNAME:-linux-testing}"
MFFER_TEST_TMPDIR="${MFFER_TEST_TMPDIR:-}"
MFFER_TEST_TMPDIR_NEW=""
PRLCTL="${PRLCTL:-}"
SSH_IDENTITY="${SSH_IDENTITY:-$HOME/.ssh/id_ecdsa}"
USERNAME="$(id -un)"
VM_BASESNAPSHOT="${VM_BASESNAPSHOT:-Base Installation}"
VM_BASESNAPSHOT_ID=""

export DEBUG VERBOSE

main() {
	getTempDir || exit 1
	getLinuxVirtualMachine || exit 1

}
cleanup() {
	EXITCODE="$?"
	if [ "$EXITCODE" != 0 ] && [ -n "$DEBUG" ]; then
		echo "Exiting with errors and DEBUG enabled; will leave" >"$VERBOSEOUT"
		echo "any new virtual machines, temporary files and directories, and" >"$VERBOSEOUT"
		echo "mounted filesystems in place." "$VERBOSEOUT"

		if [ -n "$PRLCTL" ]; then
			"$PRLCTL" status "$LINUX_VM_NAME" >"$VERBOSEOUT"
		fi
		echo "MFFER_TEST_TMPDIR: ${MFFER_TEST_TMPDIR:-unset}" >"$VERBOSEOUT"
		if [ -n "${LINUX_INSTALLER_DIR:=}" ]; then
			mount | grep "$LINUX_INSTALLER_DIR" >"$VERBOSEOUT"
		fi
		exit "$EXITCODE"
	fi
	echo "Cleaning up" >"$VERBOSEOUT"
	if [ "${LINUX_INSTALLER_DIR#"$MFFER_TEST_TMPDIR"}" != "${LINUX_INSTALLER_DIR}" ]; then
		umount "$LINUX_INSTALLER_DIR" >"$DEBUGOUT" 2>&1 || true
	fi
	if [ -n "$PRLCTL" ]; then
		"$PRLCTL" set "$LINUX_VM_NAME" --device-del hdd1 >"$DEBUGOUT" 2>&1 || true
	fi
	if [ -n "$LINUX_INSTALLER_DEV" ]; then
		hdiutil detach "$LINUX_INSTALLER_DEV" >"$DEBUGOUT" 2>&1 || true
	fi
	if [ -n "$MFFER_TEST_TMPDIR" ] && [ -n "$MFFER_TEST_TMPDIR_NEW" ]; then
		rm -rf "$MFFER_TEST_TMPDIR"
	fi
}
createLinuxVirtualMachine() { # builds a new windows VM, errors if name exists
	getParallels || return 1
	if "$PRLCTL" list "$LINUX_VM_NAME" >"$DEBUGOUT" 2>&1; then
		echo "Error: Virtual machine '$LINUX_VM_NAME'" >&2
		echo "       already exists. Consider deleting it." >&2
		return 1
	fi
	getTempDir || return 1
	getLinuxInstaller || return 1
	LINUX_INSTALLER_DIR="$MFFER_TEST_TMPDIR/LinuxInstaller"
	LINUX_SETUP_DIR="$MFFER_TEST_TMPDIR/LinuxSetup"
	LINUX_SETUP_IMG="$MFFER_TEST_TMPDIR/LinuxSetup.dmg"
	LINUX_SETUP_HDD="$MFFER_TEST_TMPDIR/LinuxSetup.hdd"

	if [ -e "$LINUX_SETUP_DIR" ]; then
		echo "Error: Linux setup directory '$LINUX_SETUP_DIR'" >&2
		echo "       already exists; remove before trying again." >&2
		return 1
	elif [ -e "$LINUX_SETUP_IMG" ]; then
		echo "Error: Linux custom image '$LINUX_SETUP_IMG'" >&2
		echo "       already exists; remove before trying again." >&2
		return 1
	elif [ -e "$LINUX_SETUP_HDD" ]; then
		echo "Error: Linux custom drive '$LINUX_SETUP_HDD'" >&2
		echo "       already exists; remove before trying again." >&2
		return 1
	fi
	echo "Loading Linux installation media" >"$VERBOSEOUT"
	if ! mkdir -p "$LINUX_INSTALLER_DIR" \
		|| ! attachoutput="$(hdiutil attach -nomount -plist "$LINUX_INSTALLER")" \
		|| ! LINUX_INSTALLER_DEV="$(getAttachedDevice "$attachoutput")" \
		|| ! mount -t cd9660 -o ro "$LINUX_INSTALLER_DEV" "$LINUX_INSTALLER_DIR" >"$DEBUGOUT" \
		|| ! cp -a "$LINUX_INSTALLER_DIR" "$LINUX_SETUP_DIR" \
		|| ! umount "$LINUX_INSTALLER_DIR" \
		|| ! hdiutil detach "$LINUX_INSTALLER_DEV"; then
		echo "Error: Unable to load Linux installation media" >&2
		return 1
	fi
	# While installing from cd/dvd would be easier and faster, there is a bug
	# limiting customizing disc images. It's a long story, but has been patched
	# and we may be able to switch to that method in the future. For now, we'll
	# use a hard disk image instead.
	echo "Creating Linux installer" >"$VERBOSEOUT"
	if ! chmod 0600 "$LINUX_SETUP_DIR/boot/grub/grub.conf" "$LINUX_SETUP_DIR/preseed/ubuntu.seed" \
		|| ! printGrubConf >"$LINUX_SETUP_DIR/boot/grub/grub.conf" \
		|| ! printSeed >>"$LINUX_SETUP_DIR/preseed/ubuntu.seed" \
		|| ! hdiutil create -fs fat32 -volname Ubuntu -layout GPTSPUD -srcfolder "$LINUX_SETUP_DIR" "$LINUX_SETUP_IMG" >"$DEBUGOUT" \
		|| ! prl_disk_tool create --hdd "$LINUX_SETUP_HDD" --dmg "$LINUX_SETUP_IMG"; then
		echo "Error: Unable to create Linux installer" >&2
		return 1
	fi

	# if [ ! -f "$SSH_IDENTITY" ]; then
	# 	if [ -f "$SSH_IDENTITY".pub ]; then
	# 		echo "Error: '$SSH_IDENTITY' not found but '$SSH_IDENTITY.pub' exists." >&2
	# 		echo '       Stopping before we break something.' >&2
	# 		return 1
	# 	fi
	# 	echo "Warning: '$SSH_IDENTITY' not found. Creating new key." >&2
	# 	if ! mkdir -p "$(dirname "$SSH_IDENTITY")" \
	# 		|| ! ssh-keygen -q -f "$SSH_IDENTITY" -N ""; then
	# 		echo "Error: Unable to create new key '$SSH_IDENTITY'." >&2
	# 	fi
	# fi
	# if [ ! -f "$SSH_IDENTITY".pub ]; then
	# 	echo "Warning: '$SSH_IDENTITY' exists but '$SSH_IDENTITY.pub' does not." >&2
	# 	echo "         Regenerating public key." >&2
	# 	if ! ssh-keygen -e "$SSH_IDENTITY" >"$SSH_IDENTITY.pub"; then
	# 		echo "Error: Unable to regenerate '$SSH_IDENTITY.pub'" >&2
	# 		return 1
	# 	fi
	# fi
	# if ! cp "$SSH_IDENTITY.pub" "$LINUX_SETUP_DIR/authorized_keys"; then
	# 	echo "Error: Unable to prepare VM for SSH communication" >&2
	# 	return 1
	# fi

	echo "Building virtual machine '$LINUX_VM_NAME'" >"$VERBOSEOUT"
	if ! "$PRLCTL" create "$LINUX_VM_NAME" -d Ubuntu >"$DEBUGOUT" \
		|| ! "$PRLCTL" set "$LINUX_VM_NAME" --isolate-vm on >"$DEBUGOUT"; then
		echo "Error: Unable to build virtual machine '$LINUX_VM_NAME'" >&2
		return 1
	fi
	if ! "$PRLCTL" set "$LINUX_VM_NAME" --device-add hdd \
		--image "$LINUX_INSTALLER" >"$DEBUGOUT" \
		|| ! "$PRLCTL" set "$LINUX_VM_NAME" \
			--device-bootorder 'hdd1' >"$DEBUGOUT" \
		|| ! "$PRLCTL" set "$LINUX_VM_NAME" --efi-boot on \
		|| ! "$PRLCTL" set "$LINUX_VM_NAME" --device-bootorder hdd1; then
		echo "Error: Unable to configure virtual machine '$LINUX_VM_NAME'" >&2
		return 1
	fi
	echo "Installing Linux on virtual machine '$LINUX_VM_NAME'" >"$VERBOSEOUT"
	if ! "$PRLCTL" start "$LINUX_VM_NAME" >"$DEBUGOUT" \
		|| ! waitForInstallation; then
		echo "Error: Unable to install Linux on virtual machine '$LINUX_VM_NAME'" >&2
		return 1
	fi
	echo "Removing old SSH authorization keys" >"$VERBOSEOUT"
	ssh-keygen -R "$LINUX_VM_HOSTNAME" >"$DEBUGOUT" 2>&1 || true
	ssh-keygen -R "$LINUX_VM_HOSTNAME".shared >"$DEBUGOUT" 2>&1 || true
	echo "Setting up new SSH authorization" >"$VERBOSEOUT"
	# Need to wait for updates to finish, shutdown machine, remove installation
	# media, restart, then save snapshot
	if ! ssh -o StrictHostKeyChecking=no -i "$SSH_IDENTITY" "$USERNAME"@"$LINUX_VM_HOSTNAME" \
		shutdown /s >"$VERBOSEOUT"; then
		echo "Error: Unable to connect to VM via SSH" >&2
		return 1
	fi
	echo "Saving VM snapshot '$VM_BASESNAPSHOT'" >"$VERBOSEOUT"
	if ! prlctl snapshot "$LINUX_VM_NAME" -n "$VM_BASESNAPSHOT" \
		-d "Initial installation without additional software. User $USERNAME, password 'MyPassword'. Public key SSH enabled." \
		>"$DEBUGOUT"; then
		echo "Error: Unable to save VM snapshot '$VM_BASESNAPSHOT'" >&2
		return 1
	fi
}
getAttachedDevice() {
	echo "Error: getAttachedDevice() is not implemented" >&2
	return 1
}
getParallels() { # sets PRLCTL if not already
	if [ -n "$PRLCTL" ]; then
		if ! "$PRLCTL" --version >"$DEBUGOUT" 2>&1; then
			echo "Error: PRLCTL is set to '$PRLCTL', which doesn't work" >&2
			return 1
		fi
		return 0
	fi
	for file in prlctl /usr/local/bin/prlctl "/Applications/Parallels Desktop.app/Contents/MacOS/prlctl"; do
		if "$file" --version >"$DEBUGOUT" 2>&1; then
			PRLCTL="$file"
			return 0
		fi
	done
	echo "Error: Unable to find 'prlctl'." >&2
	echo "       Ensure Parallels Desktop is installed and activated." >&2
	return 1
}
getTempDir() { # creates and sets MFFER_TEST_TMPDIR if not already
	if [ -n "$MFFER_TEST_TMPDIR" ]; then
		if [ ! -d "$MFFER_TEST_TMPDIR" ] \
			|| ! ls "$MFFER_TEST_TMPDIR" >"$DEBUGOUT"; then
			echo "Error: 'MFFER_TEST_TMPDIR' is set to '$MFFER_TEST_TMPDIR'," >&2
			echo "       but that isn't working." >&2
			return 1
		fi
		return 0
	fi
	MFFER_TEST_TMPDIR_NEW=y
	if ! MFFER_TEST_TMPDIR="$(mktemp -d -t mffer-test)" \
		|| [ -z "$MFFER_TEST_TMPDIR" ]; then
		echo "Error: Unable to create temporary directory" >&2
		return 1
	fi
	return 0
}
getTime() {
	date +%s
}
getVMBaseSnapshotId() { # sets VM_BASESNAPSHOT_ID if not already
	getParallels || return 1
	if [ -n "$VM_BASESNAPSHOT_ID" ]; then
		if [ -z "$("$PRLCTL" snapshot-list "$LINUX_VM_NAME" -i "$VM_BASESNAPSHOT_ID")" ]; then
			echo "Error: 'VM_BASESNAPSHOT_ID' is set to '$VM_BASESNAPSHOT_ID'," >&2
			echo "       which isn't working." >&2
			return 1
		fi
		return 0
	fi
	if ! "$PRLCTL" list "$LINUX_VM_NAME" >"$DEBUGOUT" 2>&1; then
		echo "Error: Unable to find virtual machine '$LINUX_VM_NAME'" >&2
		return 1
	fi
	if ! SNAPSHOTLIST="$(prlctl snapshot-list "$LINUX_VM_NAME" -j)" \
		|| ! SNAPSHOTS="$(
			plutil -create xml1 - -o - \
				| plutil -insert snapshots \
					-json "$SNAPSHOTLIST" - -o -
		)" \
		|| [ -z "$SNAPSHOTS" ]; then
		echo 'Error: Unable to obtain list of virtual machine snapshots.' >&2
		echo "       The virtual machine '$LINUX_VM_NAME'" >&2
		echo "       may be invalid; consider deleting it." >&2
		return 1
	fi
	VM_BASESNAPSHOT_ID="$(
		echo "$SNAPSHOTS" \
			| plutil -extract snapshots raw - -o - \
			| while read -r snapshotid; do
				if snapshots="$(echo "$SNAPSHOTS" | plutil -extract snapshots xml1 - -o -)" \
					&& snapshot="$(echo "$snapshots" | plutil -extract "$snapshotid" xml1 - -o -)" \
					&& snapshotname="$(echo "$snapshot" | plutil -extract name raw - -o -)" \
					&& [ "$snapshotname" = "$VM_BASESNAPSHOT" ]; then
					snapshotid="${snapshotid#\{}"
					snapshotid="${snapshotid%\}}"
					echo "$snapshotid"
					return 0
				fi
			done
	)"
	if [ -z "$VM_BASESNAPSHOT_ID" ]; then
		echo "Error: virtual machine '$LINUX_VM_NAME'" >&2
		echo "       does not include snapshot '$VM_BASESNAPSHOT'" >&2
		echo "       Consider deleting this VM; we can rebuild it." >&2
		return 1
	fi
	return 0
}
getLinuxInstaller() { # downloads and sets LINUX_INSTALLER if not already
	getTempDir || return 1
	if [ -z "$LINUX_INSTALLER" ]; then
		LINUX_INSTALLER="$MFFER_TEST_TMPDIR/$(basename "$LINUX_INSTALLER_URL")"
	fi
	if [ ! -f "$LINUX_INSTALLER" ]; then
		echo "Downloading Linux installation image" >"$VERBOSEOUT"
		if ! curl -LSs -o "$LINUX_INSTALLER" "$LINUX_INSTALLER_URL"; then
			echo "Error: Unable to download Linux installation image" >&2
			return 1
		fi
	fi
}
getLinuxVirtualMachine() { # creates virtual machine if not already, validates
	getParallels || return 1
	if "$PRLCTL" list "$LINUX_VM_NAME" >"$DEBUGOUT" 2>&1; then
		echo "Using virtual machine '$LINUX_VM_NAME'" >"$VERBOSEOUT"
	else
		echo "Creating virtual machine '$LINUX_VM_NAME'" >"$VERBOSEOUT"
		createLinuxVirtualMachine || return 1
	fi
	getVMBaseSnapshotId || return 1
}
printGrubConf() {
	echo "Error: printGrubConf() is not yet implemented" >&2
	return 1
}
printSeed() {
	echo "Error: printSeed() is not yet implemented" >&2
	return 1
}
sshIsRunning() {
	# returns error if not connectable, including
	# if the hostname is not found
	nc -z "$LINUX_VM_HOSTNAME.shared" 22 >"$DEBUGOUT" 2>&1
}
waitForInstallation() {
	starttime="$(getTime)"
	maxtime="$((4 * 60 * 60))" # 4 hours, in seconds
	until sshIsRunning; do
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

trap cleanup EXIT
main
