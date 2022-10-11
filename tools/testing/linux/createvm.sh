#!/bin/sh

# Test mffer build and operation on a Linux virtual machine

# Options are set via environment variables; there are no command-line
# flags or switches

# This script can only be run by a process that sets "$0" to the real (relative)
# path of the script. This limits, for instance, the ability to run via stdin or
# (theoretically) some shell interpreters. See also
# https://mywiki.wooledge.org/BashFAQ/028
SCRIPT_FILE=""
if [ -n "${0:-}" ]; then
	if [ -z "${0%%/*}" ]; then # $0 is the full path
		SCRIPT_FILE="$0"
	elif [ -n "${PWD:-}" ]; then # $0 is a relative path
		SCRIPT_FILE="$PWD/$0"
	fi
fi
if [ -z "$SCRIPT_FILE" ] || [ ! -f "$SCRIPT_FILE" ]; then
	echo "Error: Unable to determine script location" >&2
	echo "\$0: $0" >&2
	echo "\$PWD: $PWD" >&2
	echo "\$SCRIPT_FILE: $SCRIPT_FILE" >&2
	exit 1
fi
if ! SCRIPT_DIR="$(dirname "$SCRIPT_FILE")" \
	|| ! MFFER_TEST_DIR="$SCRIPT_DIR"/.. \
	|| ! MFFER_TREE_ROOT="$MFFER_TEST_DIR/../.." \
	|| [ ! -d "$MFFER_TREE_ROOT" ] \
	|| [ ! -d "$MFFER_TEST_DIR" ] \
	|| [ ! -f "$MFFER_TEST_DIR"/common/base.sh ]; then
	echo "Error: mffer source tree has unknown structure" >&2
	exit 1
fi
. "$SCRIPT_DIR/../common/base.sh"

if [ -z "${MFFER_TEST_VM_SYSTEM:-}" ]; then
	. "$SCRIPT_DIR/../common/parallels.sh"
fi

set -e
set -u

if [ 0 != "$#" ]; then
	echo "$(basename "$0") does not accept arguments." >&2
	echo "Usage: sh '$0'" >&2
	exit 1
fi
LINUX_INSTALLER="${LINUX_INSTALLER:-}" # ISO for Ubuntu Desktop 22.04; won't work with Ubuntu Server
LINUX_INSTALLER_URL="${LINUX_INSTALLER_URL:-https://releases.ubuntu.com/jammy/ubuntu-22.04.1-desktop-amd64.iso}"
MFFER_TEST_VM="${MFFER_TEST_VM:-Linux Testing}"
MFFER_TEST_VM_HOSTNAME="$(getVmHostname "$MFFER_TEST_VM")"
SSH_IDENTITY="${SSH_IDENTITY:-$HOME/.ssh/id_ecdsa}"
USERNAME="$(id -un)"

if vmExists "${MFFER_TEST_VM:=}"; then
	echo "virtual machine '$MFFER_TEST_VM' already exists; consider removing" >&2
	exit 1
fi
echo "Creating virtual machine '${MFFER_TEST_VM:=}'" >"$VERBOSEOUT"

main() { # builds a new linux VM, errors if name exists
	setTmpdir || return 1
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
		|| ! mountoutput="$(mount -t cd9660 -o rdonly "$LINUX_INSTALLER_DEV" "$LINUX_INSTALLER_DIR" 2>&1)" \
		|| ! cp -a "$LINUX_INSTALLER_DIR" "$LINUX_SETUP_DIR" \
		|| ! umount "$LINUX_INSTALLER_DIR" \
		|| ! hdiutil detach "$LINUX_INSTALLER_DEV" >"$DEBUGOUT"; then
		echo "$mountoutput" >&2
		echo "Error: Unable to load Linux installation media" >&2
		return 1
	fi

	# TODO: #233 Use current mac password if possible
	# While installing from cd/dvd would be easier and faster, there is a bug
	# limiting customizing disc images. It's a long story, but has been patched
	# and we may be able to switch to that method in the future. For now, we'll
	# use a hard disk image instead.
	echo "Creating Linux installer" >"$VERBOSEOUT"

	if [ ! -f "$SSH_IDENTITY" ]; then
		if [ -f "$SSH_IDENTITY".pub ]; then
			echo "Error: '$SSH_IDENTITY' not found but '$SSH_IDENTITY.pub' exists." >&2
			echo '       Stopping before we break something.' >&2
			return 1
		fi
		echo "Warning: '$SSH_IDENTITY' not found. Creating new key." >&2
		if ! mkdir -p "$(dirname "$SSH_IDENTITY")" \
			|| ! ssh-keygen -q -f "$SSH_IDENTITY" -N ""; then
			echo "Error: Unable to create new key '$SSH_IDENTITY'." >&2
		fi
	fi
	if [ ! -f "$SSH_IDENTITY".pub ]; then
		echo "Warning: '$SSH_IDENTITY' exists but '$SSH_IDENTITY.pub' does not." >&2
		echo "         Regenerating public key." >&2
		if ! ssh-keygen -e "$SSH_IDENTITY" >"$SSH_IDENTITY.pub"; then
			echo "Error: Unable to regenerate '$SSH_IDENTITY.pub'" >&2
			return 1
		fi
	fi
	if ! chmod u+w "$LINUX_SETUP_DIR/boot/grub/grub.cfg" "$LINUX_SETUP_DIR/preseed/ubuntu.seed" "$LINUX_SETUP_DIR/install/" \
		|| ! cp "$SSH_IDENTITY.pub" "$LINUX_SETUP_DIR/install/authorized_keys" \
		|| ! printGrubConf >"$LINUX_SETUP_DIR/boot/grub/grub.cfg" \
		|| ! printSeed >>"$LINUX_SETUP_DIR/preseed/ubuntu.seed" \
		|| ! printSuccess >"$LINUX_SETUP_DIR/install/on_success.sh" \
		|| ! chmod 0755 "$LINUX_SETUP_DIR/install/on_success.sh" \
		|| ! hdiutil create -fs fat32 -volname Ubuntu -layout GPTSPUD -srcfolder "$LINUX_SETUP_DIR" "$LINUX_SETUP_IMG" >"$DEBUGOUT" \
		|| ! prl_disk_tool create --hdd "$LINUX_SETUP_HDD" --dmg "$LINUX_SETUP_IMG"; then
		echo "Error: Unable to create Linux installer" >&2
		return 1
	fi

	echo "Building virtual machine '$MFFER_TEST_VM'" >"$VERBOSEOUT"
	if ! "$PRLCTL" create "$MFFER_TEST_VM" -d ubuntu >"$DEBUGOUT" \
		|| ! "$PRLCTL" set "$MFFER_TEST_VM" --isolate-vm on >"$DEBUGOUT"; then
		echo "Error: Unable to build virtual machine '$MFFER_TEST_VM'" >&2
		return 1
	fi
	if ! "$PRLCTL" set "$MFFER_TEST_VM" --device-add hdd \
		--image "$LINUX_SETUP_HDD" >"$DEBUGOUT" \
		|| ! "$PRLCTL" set "$MFFER_TEST_VM" \
			--device-bootorder 'hdd1' >"$DEBUGOUT" \
		|| ! "$PRLCTL" set "$MFFER_TEST_VM" --efi-boot on >"$DEBUGOUT" \
		|| ! "$PRLCTL" set "$MFFER_TEST_VM" --device-bootorder hdd1 >"$DEBUGOUT"; then
		echo "Error: Unable to configure virtual machine '$MFFER_TEST_VM'" >&2
		return 1
	fi
	echo "Installing Linux on virtual machine '$MFFER_TEST_VM'" >"$VERBOSEOUT"
	if ! "$PRLCTL" start "$MFFER_TEST_VM" >"$DEBUGOUT" \
		|| ! waitForInstallation \
		|| ! "$PRLCTL" set "$MFFER_TEST_VM" --device-del hdd1 >"$DEBUGOUT" \
		|| ! "$PRLCTL" set "$MFFER_TEST_VM" --device-bootorder 'hdd0 cdrom0' >"$DEBUGOUT" \
		|| ! "$PRLCTL" start "$MFFER_TEST_VM" >"$DEBUGOUT"; then
		echo "Error: Unable to install Linux on virtual machine '$MFFER_TEST_VM'" >&2
		return 1
	fi

	echo "Removing old SSH authorization keys" >"$VERBOSEOUT"
	ssh-keygen -R "$MFFER_TEST_VM_HOSTNAME" >"$DEBUGOUT" 2>&1 || true
	ssh-keygen -R "$MFFER_TEST_VM_HOSTNAME".shared >"$DEBUGOUT" 2>&1 || true
	echo "Setting up new SSH authorization" >"$VERBOSEOUT"

	if ! waitForSsh \
		|| ! ssh -o StrictHostKeyChecking=no -i "$SSH_IDENTITY" "$USERNAME"@"$MFFER_TEST_VM_HOSTNAME" \
			true >"$VERBOSEOUT"; then
		echo "Error: Unable to connect to VM via SSH" >&2
		return 1
	fi
	echo "Saving VM snapshot '$MFFER_TEST_SNAPSHOT'" >"$VERBOSEOUT"
	if ! "$PRLCTL" snapshot "$MFFER_TEST_VM" -n "$MFFER_TEST_SNAPSHOT" \
		-d "Initial installation with only openssh-server added. User $USERNAME, password 'MyPassword'. Public key SSH, passwordless sudo." \
		>"$DEBUGOUT"; then
		echo "Error: Unable to save VM snapshot '$MFFER_TEST_SNAPSHOT'" >&2
		return 1
	fi
	updateBaseSystem || return 1
}
cleanup() {
	EXITCODE="$?"
	if [ "$EXITCODE" != 0 ] && [ -n "$DEBUG" ]; then
		echo "Exiting with errors and DEBUG enabled; will leave" >"$VERBOSEOUT"
		echo "any new virtual machines, temporary files and directories, and" >"$VERBOSEOUT"
		echo "mounted filesystems in place." "$VERBOSEOUT"

		if [ -n "$PRLCTL" ]; then
			"$PRLCTL" status "$MFFER_TEST_VM" >"$VERBOSEOUT" || true
		fi
		echo "MFFER_TEST_TMPDIR: ${MFFER_TEST_TMPDIR:-unset}" >"$VERBOSEOUT"
		if [ -n "${LINUX_INSTALLER_DIR:=}" ]; then
			mount | grep "$LINUX_INSTALLER_DIR" >"$VERBOSEOUT" || true
		fi
		exit "$EXITCODE"
	fi
	echo "Cleaning up" >"$VERBOSEOUT"
	if vmIsRunning >"$DEBUGOUT" 2>&1; then
		"$PRLCTL" stop "$MFFER_TEST_VM" --kill >"$DEBUGOUT" 2>&1 || true
	fi
	if [ -n "${LINUX_INSTALLER_DIR:=}" ] && [ "${LINUX_INSTALLER_DIR#"$MFFER_TEST_TMPDIR"}" != "${LINUX_INSTALLER_DIR}" ]; then
		umount "$LINUX_INSTALLER_DIR" >"$DEBUGOUT" 2>&1 || true
	fi
	if [ -n "${LINUX_INSTALLER_DEV:=}" ]; then
		hdiutil detach "$LINUX_INSTALLER_DEV" >"$DEBUGOUT" 2>&1 || true
	fi
	if [ -n "${MFFER_TEST_TMPDIR:=}" ] && [ -n "${MFFER_TEST_TMPDIR_NEW:=}" ]; then
		chmod -R u+w "$MFFER_TEST_TMPDIR"
		rm -rf "$MFFER_TEST_TMPDIR"
	fi
}
getAttachedDevice() {
	if [ -z "$1" ]; then
		echo "Error: getAttachedDevice requires a string argument" >&2
		return 1
	fi
	if ! numentries="$(echo "$1" | plutil -extract 'system-entities' raw -)"; then
		echo "Error: Unable to determine mount device" >&2
		return 1
	fi
	i=0
	device=""

	while [ $i -lt "$((numentries))" ]; do
		nextdevice="$(echo "$1" | plutil -extract "system-entities.$i.dev-entry" raw -)"
		if [ -z "$nextdevice" ]; then
			echo "Error: Invalid plist" >&2
			return 1
		else
			if [ -z "$device" ] || [ "$device" != "${device#"$nextdevice"}" ]; then device="$nextdevice"; fi
			i="$((i + 1))"
		fi
	done
	if [ -z "$device" ]; then
		echo "Error: Unable to find mount device" >&2
		return 1
	fi
	echo "$device"
}
getOptionalVmArg() {
	if [ "$#" -eq 0 ]; then
		if [ -z "$MFFER_TEST_VM" ]; then
			return 1
		fi
		echo "$MFFER_TEST_VM"
	else
		echo "$1"
	fi
}
getSnapshotId() {
	# Usage: getSnapshotId vm snapshot

	# Prints the ID of the snapshot named snapshot on the VM named vm (including
	# the surrounding braces).
	if [ "$#" -ne 2 ]; then
		echo "Error: getSnapshotId() requires 2 arguments" >&2
		return 255
	fi
	if ! vmExists "$1"; then
		echo "Error: Unable to find VM '$1'" >&2
		return 1
	fi
	snapshots="$(getSnapshots "$1")"
	if ! ids="$(echo "$snapshots" | base64 -d | plutil -extract snapshots raw -o - -)" \
		|| [ -z "$ids" ]; then
		echo "Error: Unable to retrieve list of snapshots for VM '$MFFER_TEST_VM'" >&2
		return 1
	fi
	for id in $ids; do
		name="$(echo "$snapshots" | base64 -d | plutil -extract "snapshots.$id.name" raw - -o -)"
		if [ "$2" = "$name" ]; then
			echo "$id"
			return 0
		fi
	done
	echo "Error: Unable to find snapshot '$2' in VM '$1'" >&2
	return 1
}
getSnapshots() {
	# Usage: getSnapshots [vm]

	# Prints a base64-encoded plist of the snapshots in the VM named vm. If vm
	# is not given, defaults to $MFFER_TEST_VM. If vm is not given and
	# MFFER_TEST_VM is unset or null, prints error and returns 255. If there is
	# not a VM named vm, prints error and returns 127. If another error occurs,
	# prints error and returns 1. If there are no snapshots, prints nothing
	# rather than a base64-encoded empty plist.
	vm=""
	if ! vm="$(getOptionalVmArg "$@")"; then
		echo "Error: getSnapshots() requires an argument or defined MFFER_TEST_VM" >&2
		return 255
	fi
	if ! vmExists "$vm"; then
		echo "Error: VM '$vm' not found" >&2
		return 127
	fi
	if ! snapshots="$(prlctl snapshot-list "$vm" -j)"; then
		echo "Error: Unable to obtain list of snapshots for VM '$vm'" >&2
		return 1
	fi
	if [ -z "$snapshots" ]; then
		return 0
	fi
	if ! output="$(plutil -create binary1 - -o - \
		| plutil -insert snapshots -json "$snapshots" - -o - \
		| base64)" \
		|| [ -z "$output" ]; then
		echo "Error: Unable to format list of snapshots for VM '$vm'" >&2
		return 1
	fi
	echo "$output"
}
getVmBaseSnapshotId() { # sets MFFER_TEST_SNAPSHOT_ID if not already
	if [ -n "$MFFER_TEST_SNAPSHOT_ID" ]; then
		if [ -z "$("$PRLCTL" snapshot-list "$MFFER_TEST_VM" -i "$MFFER_TEST_SNAPSHOT_ID")" ]; then
			echo "Error: 'MFFER_TEST_SNAPSHOT_ID' is set to '$MFFER_TEST_SNAPSHOT_ID'," >&2
			echo "       which isn't working." >&2
			return 1
		fi
		return 0
	fi
	if ! "$PRLCTL" list "$MFFER_TEST_VM" >"$DEBUGOUT" 2>&1; then
		echo "Error: Unable to find virtual machine '$MFFER_TEST_VM'" >&2
		return 1
	fi
	if ! SNAPSHOTLIST="$(prlctl snapshot-list "$MFFER_TEST_VM" -j)" \
		|| ! SNAPSHOTS="$(
			plutil -create xml1 - -o - \
				| plutil -insert snapshots \
					-json "$SNAPSHOTLIST" - -o -
		)" \
		|| [ -z "$SNAPSHOTS" ]; then
		echo 'Error: Unable to obtain list of virtual machine snapshots.' >&2
		echo "       The virtual machine '$MFFER_TEST_VM'" >&2
		echo "       may be invalid; consider deleting it." >&2
		return 1
	fi
	MFFER_TEST_SNAPSHOT_ID="$(
		echo "$SNAPSHOTS" \
			| plutil -extract snapshots raw - -o - \
			| while read -r snapshotid; do
				if snapshots="$(echo "$SNAPSHOTS" | plutil -extract snapshots xml1 - -o -)" \
					&& snapshot="$(echo "$snapshots" | plutil -extract "$snapshotid" xml1 - -o -)" \
					&& snapshotname="$(echo "$snapshot" | plutil -extract name raw - -o -)" \
					&& [ "$snapshotname" = "$MFFER_TEST_SNAPSHOT" ]; then
					snapshotid="${snapshotid#\{}"
					snapshotid="${snapshotid%\}}"
					echo "$snapshotid"
					return 0
				fi
			done
	)"
	if [ -z "$MFFER_TEST_SNAPSHOT_ID" ]; then
		echo "Error: virtual machine '$MFFER_TEST_VM'" >&2
		echo "       does not include snapshot '$MFFER_TEST_SNAPSHOT'" >&2
		echo "       Consider deleting this VM; we can rebuild it." >&2
		return 1
	fi
	return 0
}
getLinuxInstaller() { # downloads and sets LINUX_INSTALLER if not already
	setTmpdir || return 1
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
printGrubConf() {
	cat <<-EOF
		set timeout=3

		menuentry "Install Ubuntu" {
			set gfxpayload=keep
			linux /casper/vmlinuz file=/cdrom/preseed/ubuntu.seed automatic-ubiquity "${DEBUG:+debug debug=}" ---
			initrd /casper/initrd
		}
	EOF
}
printSeed() {
	cat <<-EOF
		# customization

		# perform automatic installation
		ubiquity auto-install/enable boolean true
		casper tasksel/first string ubuntu-desktop

		# clock & timezone
		d-i clock-setup/utc boolean true
		d-i time/zone string US/Eastern
		d-i tzsetup/selected boolean false
		tzdata tzdata/Areas string Etc
		tzdata tzdata/Zones/Etc string UTC
		ubiquity ubiquity/automatic/timezone boolean true

		# locale & language
		ubiquity localechooser/languagelist string en
		ubiquity localechooser/shortlist string en
		d-i debian-installer/locale string en_US
		d-i debian-installer/language string en
		ubiquity debian-installer/language string en
		ubiquity debconf/language string us

		# keyboard
		keyboard-configuration console-setup/ask_detect boolean false
		keyboard-configuration keyboard-configuration/variant string English (US)
		keyboard-configuration keyboard-configuration/layout string English (US)
		d-i keyboard-configuration/xbp-keymap select us
		d-i keyboard-configuration/xkb-keymap select us
		console-data keyboard-configuration/layoutcode string us
		console-data console-setup/ask_detect boolean false

		# user
		d-i passwd/user-fullname string Linux Testing
		d-i passwd/username string $USERNAME
		d-i passwd/user-password password MyPassword
		d-i passwd/user-password-again password MyPassword
		ubiquity passwd/auto-login boolean true

		# disk & partitioning
		d-i grub-installer/bootdev string /dev/sda
		d-i partman-auto/disk string /dev/sda
		d-i partman-auto/method string regular
		d-i partman-auto/choose_recipe select atomic
		d-i partman-partitioning/confirm_write_new_label boolean true
		d-i partman/choose_partition select finish
		d-i partman/confirm boolean true
		d-i partman/confirm_nooverwrite boolean true
		d-i partman-partitioning/choose_label select gpt
		d-i partman-partitioning/default_label string gpt

		# installation specifics
		d-i ubiquity/minimal_install boolean false
		d-i ubiquity/use_nonfree boolean false
		d-i ubiquity/download_updates boolean true

		# still more to come
		ubiquity ubiquity/success_command string /cdrom/install/on_success.sh

		# comment the below when troubleshooting to avoid automatic shutdown
		ubiquity ubiquity/poweroff boolean true

		# A few items found in exploring the scripts:
		# (not sure if the domain is casper)
		#casper preseed/early_command string <command to run with sh -c from initramfs>
		# may be able to install package as a driver update? see in init and casper-bottom
		# It's possible the "welcome" stuff is in casper-bottom/52gnome...
		# like the casper one above, the below *_commands are run with sh -c, as root
		# finally, there are templates for everything in the ubiquity repo *.template files
		#ubiquity is the owner for everything in ubiquity (yes, I know)
		#ubiquity ubiquity/custom_title_text
		#ubiquity oem-config/enable
		#ubiquity passwd/auto-login true
		#ubiquity passwd/auto-login-backup oem
		#ubiquity ubiquity/automation_failure_commend
		#ubiquity ubiquity/failure_command
		#ubiquity ubiquity/success_command
		#ubiquity ubiquity/show_shutdown_button
		#ubiquity ubiquity/hide_slideshow
		#ubiquity ubiquity/reboot
		#ubiquity ubiquity/poweroff

	EOF
}
printSuccess() {
	cat <<-"EOF"
		#!/bin/sh
		NEWROOT="/target"
		LOGOUTPUT="/var/log/installer/success_command.log"
		PACKAGES="openssh-server"
		SCRIPTDIR="$( dirname "$0" )"
	EOF
	echo "USERNAME='$USERNAME'"
	cat <<-"EOF"
		main() {
			setupLog
			enablePasswordlessSudo
			# It would be nice to use in-target, but it doesn't seem to work right
			# (or I just am not using it right). This may be ugly, but it's relatively
			# simple and it works
			setupChrootNetwork
			installPackages
			undoChrootNetwork
			getSshKeys
			disableGnomeInitialSetup
		}
		disableGnomeInitialSetup() {
			if ! sudo mkdir -p "$NEWROOT/home/$USERNAME/.config" \
				|| ! touch "$NEWROOT/home/$USERNAME/.config/gnome-initial-setup-done" \
				|| ! sudo chroot "$NEWROOT" chown "$USERNAME" "/home/$USERNAME/.config" \
					"/home/$USERNAME/.config/gnome-initial-setup-done"; then
				echo "Unable to disable gnome-initial-setup" >&2
				return 1
			fi
		}
		enablePasswordlessSudo() {
			echo "Enabling passwordless sudo for user '$USERNAME'"
			tmpfile="$(mktemp)"
			if ! echo "$USERNAME ALL = (ALL) NOPASSWD: ALL" >"$tmpfile" \
				|| ! sudo mv "$tmpfile" "$NEWROOT/etc/sudoers.d/$USERNAME"; then
				echo "Error: Unable to enable passwordless sudo for user '$USERNAME'" >&2
				rm -f "$tmpfile"
				return 1
			fi
		}
		getSshKeys() {
			if [ -e "$SCRIPTDIR/authorized_keys" ]; then
				if ! sudo mkdir -p "$NEWROOT/home/$USERNAME/.ssh/" \
					|| ! sudo cp "$SCRIPTDIR/authorized_keys" "$NEWROOT/home/$USERNAME/.ssh/authorized_keys" \
					|| ! sudo chroot "$NEWROOT" chown "$USERNAME" "/home/$USERNAME/.ssh/authorized_keys" "/home/$USERNAME/.ssh" \
					|| ! sudo chmod 0600 "$NEWROOT/home/$USERNAME/.ssh/authorized_keys" \
					|| ! sudo chmod 0700 "$NEWROOT/home/$USERNAME/.ssh"; then
					echo "Unable to configure ssh for user '$USERNAME'" >&2
					return 1
				fi
			fi
		}
		installPackages() {
			if ! sudo chroot "$NEWROOT" apt-get -q -y install "$PACKAGES"; then
				echo "Unable to install additional packages" >&2
				return 1
			fi
		}
		setupChrootNetwork() {
			if [ ! -e "/etc/resolv.conf" ] \
				|| ! sudo mv -f "$NEWROOT"/run/systemd/resolve/stub-resolv.conf "$NEWROOT"/run/systemd/resolve/stub-resolv.conf.orig \
				|| ! sudo cp -f "/etc/resolv.conf" "$NEWROOT"/run/systemd/resolve/stub-resolv.conf; then
				echo "Unable to setup target network; installing in the target probably won't work" >&2
				return 1
			fi
		}
		setupLog() {
			if ! sudo mkdir -p "$(dirname "$NEWROOT$LOGOUTPUT")" \
				|| ! sudo touch "$NEWROOT$LOGOUTPUT" \
				|| ! sudo chmod 0644 "$NEWROOT$LOGOUTPUT" \
				|| ! TIMESTAMP="$( date +%Y-%m-%dT%H:%M:%S%z )" echo "$TIMESTAMP Running $0" >> "$NEWROOT$LOGOUTPUT"; then
				echo "Unable to write log to '$NEWROOT$LOGOUTPUT'" >&2
				return 1
			fi
			exec >"$NEWROOT$LOGOUTPUT" 2>&1
		}
		undoChrootNetwork() {
			if ! sudo mv -f "$NEWROOT"/run/systemd/resolve/stub-resolv.conf.orig \
				"$NEWROOT"/run/systemd/resolve/stub-resolv.conf; then
				echo "Unable to undo the target network change; the new system may not work" >&2
				return 1
			fi
		}
		main
	EOF
}
resetVM() {
	echo "Resetting virtual machine '$MFFER_TEST_VM' to snapshot '$MFFER_TEST_SNAPSHOT'" >"$VERBOSEOUT"
	if ! getVmBaseSnapshotId \
		|| ! "$PRLCTL" snapshot-switch "$MFFER_TEST_VM" -i "$MFFER_TEST_SNAPSHOT_ID" >"$DEBUGOUT"; then
		echo "Error: Unable to reset virtual machine '$MFFER_TEST_VM' to snapshot '$MFFER_TEST_SNAPSHOT'" >&2
		return 1
	fi
}
sshIsRunning() {
	# returns error if not connectable, including
	# if the hostname is not found
	nc -z "$MFFER_TEST_VM_HOSTNAME.shared" 22 >"$DEBUGOUT" 2>&1
}
updateBaseSystem() {
	# This will update the system with snapshot 'Base Installation' and change
	# the snapshot to point to the new system. This removes the previous snapshot.
	if ! resetVM || ! updateSystem; then
		echo "Error: Unable to update the base installation on '$MFFER_TEST_VM'" >&2
		return 1
	fi
	echo "Changing snapshot '$MFFER_TEST_SNAPSHOT' to the updated system" >"$VERBOSEOUT"
	getVmBaseSnapshotId
	if ! old_id="$MFFER_TEST_SNAPSHOT_ID" \
		|| [ -z "$old_id" ] \
		|| ! old_desc="$("$PRLCTL" snapshot-list "$MFFER_TEST_VM" -i "$old_id" \
			| sed -e '/^Description: /!d' -e 's/^Description: //' -e 's/^[[:space:]]*//' -e 's/[[:space:]]*$//' -e '/^$/d')"; then
		echo "Error: Unable to access original snapshot '$MFFER_TEST_SNAPSHOT' on VM '$MFFER_TEST_VM'" >&2
		return 1
	fi
	new_desc=""
	if [ -n "$old_desc" ]; then
		new_desc="$old_desc [Updated]"
	fi
	sleep 30 # waiting for restart
	if ! waitForSsh; then
		echo "Error: updated system did not restart" >&2
		return 1
	fi
	"$PRLCTL" snapshot "$MFFER_TEST_VM" -n "$MFFER_TEST_SNAPSHOT" -d "$new_desc" >"$DEBUGOUT"
	"$PRLCTL" snapshot-delete "$MFFER_TEST_VM" -i "$old_id" >"$DEBUGOUT"
	MFFER_TEST_SNAPSHOT_ID=""
}

updateSystem() {
	echo "Updating '$MFFER_TEST_VM'" >"$VERBOSEOUT"
	if ! vmIsRunning; then
		if ! "$PRLCTL" start "$MFFER_TEST_VM" >"$DEBUGOUT"; then
			echo "Error: Unable to start virtual machine" >&2
			return 1
		fi
	fi
	if ! sshIsRunning; then
		if ! waitForSsh; then
			echo "Error: Unable to access virtual machine" >&2
			return 1
		fi
	fi
	if ! ssh -q ${SSH_IDENTITY:+-i "$SSH_IDENTITY"} "$USERNAME@$MFFER_TEST_VM_HOSTNAME" sudo apt-get update -q -y >"$DEBUGOUT" \
		|| ! ssh -q ${SSH_IDENTITY:+-i "$SSH_IDENTITY"} "$USERNAME@$MFFER_TEST_VM_HOSTNAME" sudo apt-get upgrade -q -y >"$DEBUGOUT" \
		|| ! ssh -q ${SSH_IDENTITY:+-i "$SSH_IDENTITY"} "$USERNAME@$MFFER_TEST_VM_HOSTNAME" sudo shutdown -r now >"$DEBUGOUT"; then
		echo "Error: Unable to update Linux system" >&2
		return 1
	fi
}
vmExists() {
	vm=""
	if ! vm="$(getOptionalVmArg "$@")"; then
		echo "Error: vmExists() requires an argument or defined MFFER_TEST_VM" >&2
		return 255
	fi
	if ! "$PRLCTL" status "$vm" >"$DEBUGOUT" 2>&1; then
		return 1
	fi
}
vmIsRunning() {
	VM_STATUS=""
	if ! VM_STATUS="$("$PRLCTL" status "$MFFER_TEST_VM")" 2>"$DEBUGOUT"; then
		return 2
	fi
	if [ -z "${VM_STATUS##VM "$MFFER_TEST_VM" exist *}" ]; then # this is in the right format
		VM_ERROR=""
		if [ "stopped" = "${VM_STATUS##* }" ]; then # the last word is 'stopped'
			if [ -z "$VM_DONE" ]; then                 # it might be a fluke; try one more time
				VM_DONE=true
				sleep 1
				if ! vmIsRunning; then # nope, really done or errored
					VM_DONE=""
					return 1
				else # false alarm
					VM_DONE=""
					return 0
				fi
			else # ah, this happened before
				VM_DONE=""
				return 1
			fi
		else # everything looks good, and the last word isn't 'stopped'
			VM_DONE=""
			return 0
		fi
	else                         # uh, that didn't work right
		if [ -z "$VM_ERROR" ]; then # okay, it's the first time, try once more
			VM_ERROR=true
			sleep 1
			vmIsRunning
			case "$?" in
				2) # yeah, it's real
					VM_ERROR=""
					return 2
					;;
				1) # what, now it's stopped? Fine.
					VM_ERROR=""
					return 1
					;;
				0) # yay, we recovered by just checking it again!
					VM_ERROR=""
					return 0
					;;
				*) # some other error, I guess
					VM_ERROR=""
					return 2
					;;
			esac
		else # we've been here before
			VM_ERROR=""
			return 2
		fi
	fi
}
waitForInstallation() {
	starttime="$(getTime)"
	maxtime="$((4 * 60 * 60))" # 4 hours, in seconds
	while vmIsRunning; do
		time="$(getTime)"
		if [ -z "$starttime" ] || [ -z "$time" ]; then
			echo "Error: Unable to get the installation time" >&2
			return 1
		fi
		if [ "$((time - starttime))" -ge "$maxtime" ]; then
			echo "Error: Timed out; VM never shut down" >&2
			return 1
		fi
		sleep 5
	done
}
waitForSsh() {
	starttime="$(getTime)"
	maxtime="$((10 * 60))" # 10 minutes, in seconds
	while ! sshIsRunning; do
		time="$(getTime)"
		if [ -z "$starttime" ] || [ -z "$time" ]; then
			echo "Error: Unable to get the installation time" >&2
			return 1
		fi
		if [ "$((time - starttime))" -ge "$maxtime" ]; then
			echo "Error: Timed out; SSH not available" >&2
			return 1
		fi
		sleep 5
	done
}

trap cleanup EXIT
main
