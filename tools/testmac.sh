#!/bin/sh

VERBOSEOUT=/dev/stdout
DEBUGOUT=/dev/null
PROGRAMNAME="$(basename "$0")"
VM_HOSTNAME="macos.shared"
VM_NAME="macOS"
VM_BASESNAPSHOT="Base Installation"
PATH="$PATH:/usr/local/bin:$HOME/.dotnet"

main() {
	if which prlctl >/dev/null 2>&1; then # we're on the host machine
		checkNotRoot
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
			echo "Downloading mffer built on $VM_NAME..." >"$VERBOSEOUT"
			scp -q "$VM_HOSTNAME":built-on-macos.tar.gz . || exit 1
			echo "Resetting virtual machine..." >"$VERBOSEOUT"
			{ prlctl snapshot-switch "$VM_NAME" --id "$BASEVMID" >"$DEBUGOUT" \
				&& prlctl start "$VM_NAME" >"$DEBUGOUT"; } || exit 1
			echo "Testing mffer on macOS virtual machine..." >"$VERBOSEOUT"
			{ tar -xf built-on-macos.tar.gz 'built-on-macos/mffer-v*-osx-x64.zip' >"$DEBUGOUT" \
				&& unzip built-on-macos/mffer-v*-osx-x64.zip >"$DEBUGOUT" \
				&& scp -q -o ConnectTimeout=30 mffer "$VM_HOSTNAME": >"$DEBUGOUT"; } || exit 1
			ssh macos.shared './mffer' || exit 1
		else
			echo "Unable to find VM '$VM_NAME' with snapshot '$VM_BASESNAPSHOT'" >&2
			exit 1
		fi
	else                                # we're on the virtual machine
		if ! which node >"$DEBUGOUT"; then # we need to install the privileged tools
			checkRoot
			installsudotools || exit 1
			exit 0
		fi
		checkNotRoot
		if ! which dotnet >"$DEBUGOUT"; then # we need to install the user tools
			installusertools || exit 1
			exit 0
		fi
		buildrelease || exit 1
	fi
	return 0
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
checkNotRoot() {
	if [ 0 = "$(id -u)" ]; then
		echo "Only run this script as root when installing privileged tools." >&2
		exit 1
	fi
}
checkRoot() {
	if [ 0 != "$(id -u)" ]; then
		echo "Privileged tools must be installed as root." >&2
		exit 1
	fi
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
buildrelease() {
	git clone -q https://github.com/therealchjones/mffer >"$DEBUGOUT"
	cd mffer || return 1
	git checkout -q 143-Test-deployment >"$DEBUGOUT"
	VersionString=v0.1.0 ../.dotnet/dotnet publish -c release >"$DEBUGOUT"
	mv release built-on-macos || return 1
	tar czf ../built-on-macos.tar.gz built-on-macos || return 1
}

main
