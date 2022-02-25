#!/bin/sh

DEBUGOUT=/dev/null
VERBOSEOUT=/dev/stdout

MFFER_TEST_TMPDIR=""
PATH="$PATH:/usr/local/bin:$HOME/.dotnet"
PROGRAMNAME="$(basename "$0")"
VM_HOSTNAME="macos.shared"
VM_NAME="macOS"
VM_BASESNAPSHOT="Base Installation"

main() {
	if isHostMachine; then
		echo "Starting mffer testing on $VM_NAME..." >"$VERBOSEOUT"
		trap cleanup EXIT
		if isRoot; then
			exitError "Do not run $PROGRAMNAME as root."
		fi
		if ! vmExists "$VM_NAME"; then
			echo "Creating virtual machine '$VM_NAME'..." >"$VERBOSEOUT"
			createVm "$VM_NAME" \
				|| exitError "Unable to create virtual machine."
		fi
		if BASEVMID="$(getBaseVmId)" && [ -n "$BASEVMID" ]; then
			echo "Reverting to '$VM_BASESNAPSHOT' virtual machine image..." >"$VERBOSEOUT"
			prlctl snapshot-switch "$VM_NAME" --id "$BASEVMID" >"$DEBUGOUT" || exit 1
			echo "Starting virtual machine..." >"$VERBOSEOUT"
			prlctl start "$VM_NAME" >"$DEBUGOUT" || exit 1
			echo "Connecting to virtual machine..." >"$VERBOSEOUT"
			scp -q -o ConnectTimeout=30 "$0" "$VM_HOSTNAME": || exit 1
			echo "Installing privileged virtual machine tools..." >"$VERBOSEOUT"
			ssh -qt "$VM_HOSTNAME" "sudo -p 'sudo password for $VM_HOSTNAME:' sh '$PROGRAMNAME'" || exit 1
			echo "Installing userland virtual machine tools" >"$VERBOSEOUT"
			ssh -q "$VM_HOSTNAME" "sh '$PROGRAMNAME'" || exit 1
			echo "Building mffer..." >"$VERBOSEOUT"
			ssh -q "$VM_HOSTNAME" "sh '$PROGRAMNAME'" || exit 1
			if [ -z "$MFFER_TEST_TMPDIR" ]; then
				createTempDir || exit 1
			fi
			echo "Downloading mffer built on $VM_NAME..." >"$VERBOSEOUT"
			scp -q "$VM_HOSTNAME":built-on-macos.tar.gz "$MFFER_TEST_TMPDIR" || exit 1
			echo "Resetting virtual machine..." >"$VERBOSEOUT"
			{ prlctl snapshot-switch "$VM_NAME" --id "$BASEVMID" >"$DEBUGOUT" \
				&& prlctl start "$VM_NAME" >"$DEBUGOUT"; } || exit 1
			echo "Testing mffer on macOS virtual machine..." >"$VERBOSEOUT"
			{ tar -xf "$MFFER_TEST_TMPDIR"/built-on-macos.tar.gz \
				-C "$MFFER_TEST_TMPDIR" \
				'built-on-macos/mffer-v*-osx-x64.zip' >"$DEBUGOUT" \
				&& unzip "$MFFER_TEST_TMPDIR"/built-on-macos/mffer-v*-osx-x64.zip \
					-d "$MFFER_TEST_TMPDIR" >"$DEBUGOUT" \
				&& scp -q -o ConnectTimeout=30 \
					"$MFFER_TEST_TMPDIR"/mffer "$VM_HOSTNAME": >"$DEBUGOUT"; } \
				|| exit 1
			ssh macos.shared './mffer' || exit 1
		else
			echo "Unable to find VM '$VM_NAME' with snapshot '$VM_BASESNAPSHOT'" >&2
			exit 1
		fi
	else
		if ! which node >"$DEBUGOUT"; then # we need to install the privileged tools
			if ! isRoot; then
				exitError "Privileged tools must be installed as root."
			fi
			installsudotools || exit 1
			exit 0
		fi
		if ! which dotnet >"$DEBUGOUT"; then # we need to install the user tools
			if isRoot; then
				exitError "User tools should not be installed as root."
			fi
			installusertools || exit 1
			exit 0
		fi
		if isRoot; then
			exitError "mffer should not be built as root."
		fi
		if ! buildrelease; then
			exit 1
		fi
	fi
	return 0
}
buildrelease() {
	if ! git clone -q https://github.com/therealchjones/mffer >"$DEBUGOUT" \
		|| ! cd mffer \
		|| ! git checkout -q 143-Test-deployment >"$DEBUGOUT"; then
		echo "Unable to get mffer source." >&2
		return 1
	fi
	if ! VersionString=v0.1.0 ../.dotnet/dotnet publish -c release >"$DEBUGOUT" \
		|| ! mv release built-on-macos; then
		echo "Unable to build mffer." >&2
		return 1
	fi
	tar czf ../built-on-macos.tar.gz built-on-macos
}
cleanup() {
	trap - EXIT
	if [ -n "$MFFER_TEST_TMPDIR" ]; then
		echo "Cleaning up..." >"$VERBOSEOUT"
		rm -rf "$MFFER_TEST_TMPDIR"
	fi
}
createTempDir() {
	if ! MFFER_TEST_TMPDIR="$(mktemp -d -t mffer-test)" \
		|| [ -z "$MFFER_TEST_TMPDIR" ]; then
		echo 'Unable to create temporary directory' >&2
		return 1
	fi
}
createVm() {
	if [ -z "$MFFER_TEST_TMPDIR" ] && ! createTempDir; then
		return 1
	fi
	VM_NAME="${1:-$VM_NAME}"
	if ! curl -sS -L -o "$MFFER_TEST_TMPDIR"/mkmacvm-0.3.0.tar.gz \
		https://github.com/therealchjones/mkmacvm/archive/v0.3.0.tar.gz \
		|| ! tar -xf "$MFFER_TEST_TMPDIR"/mkmacvm-0.3.0.tar.gz -C "$MFFER_TEST_TMPDIR" \
		|| ! sudo -p 'sudo password to create VM: ' VERBOSE=y \
			"$MFFER_TEST_TMPDIR"/mkmacvm-0.3.0/mkmacvm "$VM_NAME"; then
		return 1
	fi
	if ! prlctl set "$VM_NAME" --startup-view headless \
		|| ! prlctl snapshot macOS -n "$VM_BASESNAPSHOT" -d "ssh:$VM_HOSTNAME"; then
		return 1
	fi
}
exitError() {
	if [ -n "$*" ]; then
		echo "$*" >&2
	fi
	exit 1
}
getBaseVmId() {
	if ! SNAPSHOTLIST="$(prlctl snapshot-list "$VM_NAME" -j)"; then
		echo 'Unable to obtain list of virtual machine snapshots.' >&2
		return 1
	fi
	SNAPSHOTS="$(
		plutil -create xml1 - -o - \
			| plutil -insert snapshots \
				-json "$SNAPSHOTLIST" - -o -
	)"
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
}
installsudotools() {
	# must be sudo'd for the node and Command Line Tools installations

	CMDLINETOOLTMP="/tmp/.com.apple.dt.CommandLineTools.installondemand.in-progress"
	touch "$CMDLINETOOLTMP"
	CMDLINETOOLS="$(softwareupdate -l 2>/dev/null \
		| sed -n \
			-e '/Command Line Tools/!d' \
			-e '/[Bb][Ee][Tt][Aa]/d' \
			-e '/^[ \*]*Label: */{s///;p;}' \
		| sort -V \
		| tail -n1)"
	if ! output="$(softwareupdate -i "$CMDLINETOOLS" 2>&1)"; then
		echo "$output"
		return 1
	fi
	rm "$CMDLINETOOLTMP"

	curl -Ss -O https://nodejs.org/dist/v16.13.2/node-v16.13.2.pkg >"$DEBUGOUT"
	installer -pkg ./node-v16.13.2.pkg -target / >"$DEBUGOUT"

}
installusertools() {
	curl -Ss -OL https://dot.net/v1/dotnet-install.sh >"$DEBUGOUT"
	sh ./dotnet-install.sh --channel 5.0 >"$DEBUGOUT"
}
isHostMachine() {
	# We assume prlctl is available only on the host machine
	which prlctl >$DEBUGOUT 2>&1
}
isRoot() {
	[ 0 = "$(id -u)" ]
}
vmExists() {
	vm_name_tocheck="${1:-$VM_NAME}"
	if [ -z "$vm_name_tocheck" ]; then return 1; fi
	prlctl status "$vm_name_tocheck" >"$DEBUGOUT" 2>&1
}

main
