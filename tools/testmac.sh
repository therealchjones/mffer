#!/bin/sh

DEBUGOUT=/dev/null
VERBOSEOUT=/dev/stdout

MFFER_TEST_TMPDIR=""
MFFER_TREE_DIRTY=""
MFFER_SOURCE_DIR="${MFFER_SOURCE_DIR:-}"
MFFER_SOURCE_COMMIT="${MFFER_SOURCE_COMMIT:-}"
PATH="${PATH:+$PATH:}/usr/local/bin:${HOME:-}/.dotnet"
PROGRAMNAME="$(basename "$0")"
PROGRAMDIR="$(dirname "$0")"
VM_HOSTNAME="macos.shared"
VM_NAME="macOS"
VM_BASESNAPSHOT="Base Installation"

# As convention, only main() should exit the program, should do so only if
# unable to reasonably continue, and should explain why.
# Other functions should return a nonzero status, but only output explanation
# text if a "bare" function that's calling external programs rather than
# other functions. This prevents excessive error output via a "stack trace".

main() {
	if isHostMachine; then
		setup || exitError
		testBuild || exitError
		getRelease || exitError
		testMffer || failError "mffer"
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
	if [ -d "mffer" ] && [ -n "$MFFER_SOURCE_COMMIT" ]; then
		if [ "$MFFER_SOURCE_COMMIT" != "$(git -C mffer describe --all --dirty)" ]; then
			error="Local mffer repository is not at commit '$MFFER_SOURCE_COMMIT'"
			error="\nWill attempt to switch..."
			warnError error
			if ! git -C mffer checkout -q "$MFFER_SOURCE_COMMIT" >"$DEBUGOUT"; then
				return 1
			fi
		fi
	elif [ ! -d "mffer" ]; then
		if git clone -q https://github.com/therealchjones/mffer >"$DEBUGOUT"; then
			if [ -n "$MFFER_SOURCE_COMMIT" ]; then
				if ! git -C mffer checkout -q "$MFFER_SOURCE_COMMIT" >"$DEBUGOUT"; then
					warnError "Unable to checkout commit '$MFFER_SOURCE_COMMIT'"
					return 1
				fi
			fi
		else
			warnError "Unable to get mffer source."
			return 1
		fi
	fi
	if [ "$MFFER_SOURCE_COMMIT" = "$(git -C mffer tag --contains)" ]; then
		if ! ./.dotnet/dotnet publish -c Release mffer/mffer.csproj >"$DEBUGOUT"; then
			warnError "Unable to build mffer."
			return 1
		fi
	elif ! VersionString="$MFFER_SOURCE_COMMIT" ./.dotnet/dotnet publish -c release \
		mffer/mffer.csproj >"$DEBUGOUT"; then
		warnError "Unable to build mffer."
		return 1
	fi
	if ! mv mffer/release built-on-macos \
		|| ! tar -czf built-on-macos.tar.gz built-on-macos; then
		warnError "Unable to tar mffer for download"
		return 1
	fi
}
cleanup() {
	exitstatus="$?"
	trap - EXIT
	if [ -n "$MFFER_TEST_TMPDIR" ]; then
		echo "Cleaning up..." >"$VERBOSEOUT"
		rm -rf "$MFFER_TEST_TMPDIR"
	fi
	exit "$exitstatus"
}
createTempDir() {
	if ! MFFER_TEST_TMPDIR="$(mktemp -d -t mffer-test)" \
		|| [ -z "$MFFER_TEST_TMPDIR" ]; then
		warnError 'Unable to create temporary directory'
		return 1
	fi
}
createVm() {
	echo "Creating virtual machine '$VM_NAME'..." >"$VERBOSEOUT"
	if [ -z "$MFFER_TEST_TMPDIR" ]; then createTempDir || return 1; fi
	VM_NAME="${1:-$VM_NAME}"
	if ! curl -sS -L -o "$MFFER_TEST_TMPDIR"/mkmacvm-0.3.0.tar.gz \
		https://github.com/therealchjones/mkmacvm/archive/v0.3.0.tar.gz \
		|| ! tar -xf "$MFFER_TEST_TMPDIR"/mkmacvm-0.3.0.tar.gz -C "$MFFER_TEST_TMPDIR" \
		|| ! sudo -p 'sudo password to create VM: ' VERBOSE=y \
			"$MFFER_TEST_TMPDIR"/mkmacvm-0.3.0/mkmacvm "$VM_NAME"; then
		warnError "Unable to build virtual machine"
		return 1
	fi
	if ! prlctl set "$VM_NAME" --startup-view headless >"$DEBUGOUT" \
		|| ! prlctl snapshot "$VM_NAME" -n "$VM_BASESNAPSHOT" >"$DEBUGOUT"; then
		warnError "Unable to set up virtual machine"
		return 1
	fi
}
exitError() {
	error="${*:-Exiting.}"
	echo "ERROR: $error" >&2
	exit 1
}
failError() {
	error="${*:-}"
	echo "FAIL${error:+: $error}" >&2
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
getRelease() {
	# puts release files in $MFFER_TEST_TMPDIR/mffer-macos/
	scp -q "$VM_HOSTNAME":built-on-macos.tar.gz "$MFFER_TEST_TMPDIR" || exit 1
	{
		tar -xf "$MFFER_TEST_TMPDIR"/built-on-macos.tar.gz \
			-C "$MFFER_TEST_TMPDIR" >"$DEBUGOUT" \
			&& mkdir -p "$MFFER_TEST_TMPDIR"/mffer-macos \
			&& unzip "$MFFER_TEST_TMPDIR"/built-on-macos/mffer-"$MFFER_SOURCE_COMMIT"-osx-x64.zip \
				-d "$MFFER_TEST_TMPDIR"/mffer-macos >"$DEBUGOUT"
	} || {
		warnError "Unable to extract built project on local machine."
		return 1
	}
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
resetVm() {
	echo "Resetting virtual machine..." >"$VERBOSEOUT"
	if ! BASEVMID="$(getBaseVmId)" \
		|| [ -z "$BASEVMID" ] \
		|| ! prlctl snapshot-switch "$VM_NAME" --id "$BASEVMID" >"$DEBUGOUT" \
		|| ! prlctl start "$VM_NAME" >"$DEBUGOUT"; then
		warnError "Unable to reset virtual machine '$VM_NAME' to snapshot '$VM_BASESNAPSHOT'"
		return 1
	fi
	# wait for the VM to start
	tries=10
	until ssh -q -o ConnectTimeout=30 "$VM_HOSTNAME" true || [ "$tries" -lt 1 ]; do
		sleep 5
		tries="$((tries - 1))"
	done
	if [ "$tries" -lt 1 ]; then
		warnError "Unable to reconnect to virtual machine"
		return 1
	fi
}
setup() {
	trap cleanup EXIT
	if isRoot; then
		warnError "Do not run $PROGRAMNAME as root."
		return 1
	fi
	if [ -z "$MFFER_SOURCE_DIR" ]; then
		if ! MFFER_SOURCE_DIR="$(
			dirname "$(
				find "$PROGRAMDIR/../" -name mffer.csproj -print -quit
			)"
		)"; then
			warnError "Unable to find local mffer repository"
			return 1
		fi
	fi
	if fixed_source_dir="$( (cd "$MFFER_SOURCE_DIR" && pwd))"; then
		MFFER_SOURCE_DIR="$fixed_source_dir"
	else
		warnError "Unable to determine canonical mffer source directory."
	fi
	if [ -z "$MFFER_SOURCE_COMMIT" ]; then
		if [ -n "$(git -C "$MFFER_SOURCE_DIR" tag --points-at)" ] \
			&& [ "$(git -C "$MFFER_SOURCE_DIR" status --porcelain)" = "" ]; then
			MFFER_SOURCE_COMMIT="$(git -C "$MFFER_SOURCE_DIR" tag --points-at)"
		else
			MFFER_SOURCE_COMMIT="$(git -C "$MFFER_SOURCE_DIR" describe --all --dirty)"
		fi
	fi
	if ! git -C "$MFFER_SOURCE_DIR" describe --all "${MFFER_SOURCE_COMMIT}" >"$DEBUGOUT" 2>&1; then
		if [ "${MFFER_SOURCE_COMMIT}" != "${MFFER_SOURCE_COMMIT%-dirty}" ] \
			&& [ "$(git -C "$MFFER_SOURCE_DIR" describe --all)" = "${MFFER_SOURCE_COMMIT%-dirty}" ]; then
			error="The current git working tree is not committed;\n"
			error="${error}will be testing uncommitted changes."
			warnError "$error"
			MFFER_TREE_DIRTY=y
		else
			error="'$MFFER_SOURCE_COMMIT' is not a valid commit identifier to test.\n"
			error="${error}Change to the git repository root directory,\n"
			error="${error}or name a valid commit to test."
			warnError "$error"
			return 1
		fi
	fi
	# TODO: if tree is not up to date (i.e., not pushed), also use MFFER_TREE_DIRTY
	echo "Testing mffer commit '$MFFER_SOURCE_COMMIT'" >"$VERBOSEOUT"
	if [ -n "$MFFER_TREE_DIRTY" ]; then
		echo "from local repository '$MFFER_SOURCE_DIR'" >"$VERBOSEOUT"
	fi
	echo "on virtual machine '$VM_NAME'" >"$VERBOSEOUT"
	echo "starting at snapshot '$VM_BASESNAPSHOT'" >"$VERBOSEOUT"
	if ! vmExists "$VM_NAME"; then
		createVm "$VM_NAME" || return 1
	fi
	if [ -z "$MFFER_TEST_TMPDIR" ] || [ ! -d "$MFFER_TEST_TMPDIR" ]; then
		createTempDir || return 1
	fi
}
testBuild() {
	resetVm || return 1
	if ! scp -q -o ConnectTimeout=30 "$0" "$VM_HOSTNAME": \
		|| ! ssh -qt "$VM_HOSTNAME" "sudo -p 'sudo password for $VM_HOSTNAME:' sh '$PROGRAMNAME'" \
		|| ! ssh -q "$VM_HOSTNAME" "sh '$PROGRAMNAME'"; then
		warnError "Unable to configure virtual machine for building mffer."
		return 1
	fi
	if [ -n "$MFFER_TREE_DIRTY" ]; then
		if [ -z "$MFFER_TEST_TMPDIR" ] || [ ! -d "$MFFER_TEST_TMPDIR" ]; then
			createTempDir || return 1
		fi
		if ! tar -cf "$MFFER_TEST_TMPDIR"/build-tree.tar -C "$MFFER_SOURCE_DIR"/.. mffer \
			|| ! scp -q "$MFFER_TEST_TMPDIR"/build-tree.tar "$VM_HOSTNAME": \
			|| ! ssh -q "$VM_HOSTNAME" "tar -xf build-tree.tar"; then
			warnError "Unable to transfer local mffer repository to $VM_HOSTNAME"
			return 1
		fi
	fi
	if ! ssh -q "$VM_HOSTNAME" "MFFER_SOURCE_COMMIT='$MFFER_SOURCE_COMMIT' sh '$PROGRAMNAME'"; then
		warnError "Building mffer failed."
		return 1
	fi
}
testMffer() {
	echo "Testing mffer on $VM_NAME..." >"$VERBOSEOUT"
	resetVm || return 1
	scp -q "$MFFER_TEST_TMPDIR"/mffer-macos/mffer "$VM_HOSTNAME": || return 1
	ssh -q "$VM_HOSTNAME" './mffer' || return 1
}
vmExists() {
	vm_name_tocheck="${1:-$VM_NAME}"
	if [ -z "$vm_name_tocheck" ]; then return 1; fi
	prlctl status "$vm_name_tocheck" >"$DEBUGOUT" 2>&1
}
warnError() {
	if [ -n "$*" ]; then
		echo "WARNING: $*" >&2
	fi
}

main
