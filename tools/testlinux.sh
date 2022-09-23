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
LINUX_INSTALLER="${LINUX_INSTALLER:-}" # ISO for Ubuntu Desktop 22.04; won't work with Ubuntu Server
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
			"$PRLCTL" status "$LINUX_VM_NAME" >"$VERBOSEOUT" || true
		fi
		echo "MFFER_TEST_TMPDIR: ${MFFER_TEST_TMPDIR:-unset}" >"$VERBOSEOUT"
		if [ -n "${LINUX_INSTALLER_DIR:=}" ]; then
			mount | grep "$LINUX_INSTALLER_DIR" >"$VERBOSEOUT" || true
		fi
		exit "$EXITCODE"
	fi
	echo "Cleaning up" >"$VERBOSEOUT"
	if vmIsRunning >"$DEBUGOUT" 2>&1; then
		"$PRLCTL" stop "$LINUX_VM_NAME" --kill >"$DEBUGOUT" 2>&1 || true
	fi
	if [ -n "${LINUX_INSTALLER_DIR:=}" ] && [ "${LINUX_INSTALLER_DIR#"$MFFER_TEST_TMPDIR"}" != "${LINUX_INSTALLER_DIR}" ]; then
		umount "$LINUX_INSTALLER_DIR" >"$DEBUGOUT" 2>&1 || true
	fi
	if [ -n "$LINUX_INSTALLER_DEV" ]; then
		hdiutil detach "$LINUX_INSTALLER_DEV" >"$DEBUGOUT" 2>&1 || true
	fi
	if [ -n "$MFFER_TEST_TMPDIR" ] && [ -n "$MFFER_TEST_TMPDIR_NEW" ]; then
		chmod -R u+w "$MFFER_TEST_TMPDIR"
		rm -rf "$MFFER_TEST_TMPDIR"
	fi
}
createLinuxVirtualMachine() { # builds a new linux VM, errors if name exists
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
		|| ! mountoutput="$(mount -t cd9660 -o rdonly "$LINUX_INSTALLER_DEV" "$LINUX_INSTALLER_DIR" 2>&1)" \
		|| ! cp -a "$LINUX_INSTALLER_DIR" "$LINUX_SETUP_DIR" \
		|| ! umount "$LINUX_INSTALLER_DIR" \
		|| ! hdiutil detach "$LINUX_INSTALLER_DEV" >"$DEBUGOUT"; then
		echo "$mountoutput" >&2
		echo "Error: Unable to load Linux installation media" >&2
		return 1
	fi

	# While installing from cd/dvd would be easier and faster, there is a bug
	# limiting customizing disc images. It's a long story, but has been patched
	# and we may be able to switch to that method in the future. For now, we'll
	# use a hard disk image instead.
	echo "Creating Linux installer" >"$VERBOSEOUT"
	if ! chmod u+w "$LINUX_SETUP_DIR/boot/grub/grub.cfg" "$LINUX_SETUP_DIR/preseed/ubuntu.seed" "$LINUX_SETUP_DIR/install/" \
		|| ! printGrubConf >"$LINUX_SETUP_DIR/boot/grub/grub.cfg" \
		|| ! printSeed >>"$LINUX_SETUP_DIR/preseed/ubuntu.seed" \
		|| ! printSuccess >"$LINUX_SETUP_DIR/install/on_success.sh" \
		|| ! chmod 0755 "$LINUX_SETUP_DIR/install/on_success.sh" \
		|| ! hdiutil create -fs fat32 -volname Ubuntu -layout GPTSPUD -srcfolder "$LINUX_SETUP_DIR" "$LINUX_SETUP_IMG" >"$DEBUGOUT" \
		|| ! prl_disk_tool create --hdd "$LINUX_SETUP_HDD" --dmg "$LINUX_SETUP_IMG"; then
		echo "Error: Unable to create Linux installer" >&2
		return 1
	fi
	# This (presumably prl_disk_tool) leaves the dmg attached, which we should fix

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
	if ! "$PRLCTL" create "$LINUX_VM_NAME" -d ubuntu >"$DEBUGOUT" \
		|| ! "$PRLCTL" set "$LINUX_VM_NAME" --isolate-vm on >"$DEBUGOUT"; then
		echo "Error: Unable to build virtual machine '$LINUX_VM_NAME'" >&2
		return 1
	fi
	if ! "$PRLCTL" set "$LINUX_VM_NAME" --device-add hdd \
		--image "$LINUX_SETUP_HDD" >"$DEBUGOUT" \
		|| ! "$PRLCTL" set "$LINUX_VM_NAME" \
			--device-bootorder 'hdd1' >"$DEBUGOUT" \
		|| ! "$PRLCTL" set "$LINUX_VM_NAME" --efi-boot on >"$DEBUGOUT" \
		|| ! "$PRLCTL" set "$LINUX_VM_NAME" --device-bootorder hdd1 >"$DEBUGOUT"; then
		echo "Error: Unable to configure virtual machine '$LINUX_VM_NAME'" >&2
		return 1
	fi
	echo "Installing Linux on virtual machine '$LINUX_VM_NAME'" >"$VERBOSEOUT"
	if ! "$PRLCTL" start "$LINUX_VM_NAME" >"$DEBUGOUT" \
		|| ! waitForInstallation \
		|| ! "$PRLCTL" set "$LINUX_VM_NAME" --device-del hdd1 >"$DEBUGOUT" \
		|| ! "$PRLCTL" set "$LINUX_VM_NAME" --device-bootorder 'hdd0 cdrom0' >"$DEBUGOUT"; then
		echo "Error: Unable to install Linux on virtual machine '$LINUX_VM_NAME'" >&2
		return 1
	fi

	# Need to enable ssh as the last step of the installation, then shutdown and
	# modify VM as above, then restart and proceed when SSH is detected. Also
	# want to get rid of "connect your online accounts","set up livepatch","help
	# improve ubuntu", privacy, "ready to go", all maybe under "welcome to
	# ubuntu" for updated software, don't prompt, just do it. but no
	# restarting---or maybe if exist, restart until done; add "update" function
	# for existing VMs that updates all software from Base Install and updates
	# the snapshot? Disable screensaver/lock?
	echo "Error: the rest isn't implemented" >&2
	return 1

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
		#ubiquity ubiquity/poweroff boolean true

		# A few items found in exploring the scripts:
		# (not sure if the domain is casper)
		#casper preseed/early_command string <command to run with sh -c from initramfs>
		# may be able to install package as a driver update? see in init and casper-bottom
		# It's possible the "welcome" stuff is in casper-bottom/52gnome...
		# like the casper one above, the below *_commands are run with sh -c, as root
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

		# next up: ubiquity.frontend.base plugin_manager.load_plugins()

	EOF
}
printSuccess() {
	cat <<-"EOF"
		#!/bin/sh
		NEWROOT="/target"
		LOGOUTPUT="/var/log/installer/success_command.log"
		PACKAGES="openssh-server"

		main() {
			setupLog || true
			setupChrootNetwork || true
			installPackages
			undoChrootNetwork
		}
		installPackages() {
			if ! sudo chroot "$NEWROOT" apt -y install "$PACKAGES"; then
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
				|| ! sudo chmod 0666 "$NEWROOT$LOGOUTPUT" \
				|| ! date +%Y-%m-%dT%H:%M:%S%z >> "$NEWROOT$LOGOUTPUT"; then
				echo "Unable to write log to '$NEWROOT$LOGOUTPUT'" >&2
				return 1
			fi
			exec >"$NEWROOT$LOGOUTPUT" 2>&1
		}
		undoChrootNetwork() {
			if ! sudo mv -f "$NEWROOT"/run/systemd/resolve/stub-resolv.conf.orig "$NEWROOT"/run/systemd/resolve/stub-resolv.conf; then
				echo "Unable to undo the target network change; the new system may not work" >&2
				return 1
			fi
		}
		main
	EOF
}
sshIsRunning() {
	# returns error if not connectable, including
	# if the hostname is not found
	nc -z "$LINUX_VM_HOSTNAME.shared" 22 >"$DEBUGOUT" 2>&1
}
vmIsRunning() {
	VM_STATUS=""
	if ! VM_STATUS="$("$PRLCTL" status "$LINUX_VM_NAME")" 2>"$DEBUGOUT"; then
		return 2
	fi
	if [ -z "${VM_STATUS##VM "$LINUX_VM_NAME" exist *}" ]; then # this is in the right format
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

trap cleanup EXIT
main
