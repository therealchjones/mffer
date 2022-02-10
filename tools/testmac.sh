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
			prlctl snapshot-switch "$VM_NAME" --id "$BASEVMID"
			echo "Starting virtual machine..." >"$VERBOSEOUT"
			prlctl start "$VM_NAME" || exit 1
			echo "Connecting to virtual machine..." >"$VERBOSEOUT"
			scp -o ConnectTimeout=30 "$0" "$VM_HOSTNAME": || exit 1
			echo "Installing privileged virtual machine tools..." >"$VERBOSEOUT"
			ssh -t "$VM_HOSTNAME" "sudo -p 'sudo password for $VM_HOSTNAME:' sh '$PROGRAMNAME'" || exit 1
			echo "Installing userland virtual machine tools" >"$VERBOSEOUT"
			ssh -t "$VM_HOSTNAME" "sh '$PROGRAMNAME'" || exit 1
			echo "Building mffer..." >"$VERBOSEOUT"
			ssh -t "$VM_HOSTNAME" "sh '$PROGRAMNAME'" || exit 1
			echo "Downloading mffer built on $VM_NAME..."
			scp "$VM_HOSTNAME":built-on-macos.tar.gz .
		else
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
	softwareupdate -i "$CMDLINETOOLS" >"$VERBOSEOUT"
	rm "$CMDLINETOOLTMP"

	curl -O https://nodejs.org/dist/v16.13.2/node-v16.13.2.pkg >"$VERBOSEOUT"
	installer -pkg ./node-v16.13.2.pkg -target / >"$VERBOSEOUT"

}
installusertools() {
	curl -OL https://dot.net/v1/dotnet-install.sh
	sh ./dotnet-install.sh --channel 5.0
}
buildrelease() {
	git clone https://github.com/therealchjones/mffer
	cd mffer || exit 1
	git checkout 143-Test-deployment
	VersionString=v0.1.0 ../.dotnet/dotnet publish -c release
	mv release built-on-macos
	tar czf ../built-on-macos.tar.gz built-on-macos
}

main
