#!/bin/sh

DEBUG="${DEBUG:-}"
VERBOSE="${VERBOSE:-y}"

MFFER_TEST_TMPDIR="${MFFER_TEST_TMPDIR:-}"
MFFER_LOCAL_TREE="${MFFER_LOCAL_TREE:-}"
MFFER_SOURCE_DIR="${MFFER_SOURCE_DIR:-}"
MFFER_SOURCE_COMMIT="${MFFER_SOURCE_COMMIT:-}"
MFFER_REPO="${MFFER_REPO:-https://github.com/therealchjones/mffer}"
PATH="${PATH:+$PATH:}/usr/local/bin:${HOME:-}/.dotnet"
PROGRAMNAME="$(basename "$0")"
PROGRAMDIR="$(dirname "$0")"
VM_NAME="${VM_NAME:-macvm}"
VM_HOSTNAME="${VM_HOSTNAME:-macvm.shared}"
VM_BASESNAPSHOT="${VM_BASESNAPSHOT:-Base Installation}"
MKMACVM_VERSION="0.3.1"
PYTHON_VERSION="3.10.6"

# As convention, only main() should exit the program, should do so only if
# unable to reasonably continue, and should explain why.
# Other functions should return a nonzero status, but only output explanation
# text if a "bare" function that's calling external programs rather than
# other functions. This prevents excessive error output via a "stack trace".

main() {
	getOptions "$@"
	if isHostMachine; then
		setup || exitError
		testBuild || exitError
		getRelease || exitError
		testApkdl || failError "apkdl"
		testAutoanalyze || failError "autoanalyze"
		testMffer || failError "mffer"
	else # on virtual machine
		if ! state="$(checkVmState)"; then
			exitError "Unable to determine what to do next on the virtual machine."
		fi
		case "$state" in
			build) buildRelease || exit 1 ;;
			mffer) runMffer || exit 1 ;;
			apkdl) runApkdl || exit 1 ;;
			autoanalyze) runAutoanalyze || exit 1 ;;
			*)
				exitError "Unable to determine what to do next on the virtual machine."
				exit 1
				;;
		esac
	fi
	return 0
}
buildRelease() {
	if ! which node >"$DEBUGOUT" && ! installsudotools; then # we need to install the privileged tools
		warnError "Unable to install privileged build tools."
	fi
	if ! which dotnet >"$DEBUGOUT" && ! installDotNet; then # we need to install the user tools
		warnError "Unable to install userland build tools."
	fi
	if [ -d "mffer" ]; then
		# we're using the local source rather than repository,
		# which means that MFFER_SOURCE_COMMIT is in the source tree (including HEAD) or is blank
		if [ -n "$MFFER_SOURCE_COMMIT" ]; then
			if [ "$MFFER_SOURCE_COMMIT" != "$(git -C mffer tag --points-at)" ]; then
				error="Local mffer repository may not be at commit '$MFFER_SOURCE_COMMIT'"
				error="\nWill attempt to switch..."
				warnError error
				if ! git -C mffer checkout -q "$MFFER_SOURCE_COMMIT" >"$DEBUGOUT"; then
					return 1
				fi
			fi
		else
			MFFER_SOURCE_COMMIT=prerelease-testing
		fi
	else
		if git clone -q https://github.com/therealchjones/mffer >"$DEBUGOUT"; then
			if ! git -C mffer checkout -q "$MFFER_SOURCE_COMMIT" >"$DEBUGOUT"; then
				warnError "Unable to checkout commit '$MFFER_SOURCE_COMMIT'"
				return 1
			fi
		else
			warnError "Unable to get mffer source."
			return 1
		fi
	fi
	if ! ./.dotnet/dotnet restore mffer/mffer.csproj >"$DEBUGOUT" \
		|| ! ./.dotnet/dotnet clean mffer/mffer.csproj >"$DEBUGOUT"; then
		warnError "Build tree is not in a usable state; unable to build mffer."
		return 1
	fi
	if [ -n "$MFFER_SOURCE_COMMIT" ] \
		&& [ -z "$(git -C mffer status --porcelain)" ] \
		&& [ "$MFFER_SOURCE_COMMIT" = "$(git -C mffer tag --points-at)" ]; then
		if ! ./.dotnet/dotnet \
			publish -c Release mffer/mffer.csproj >"$DEBUGOUT"; then
			warnError "Unable to build mffer."
			return 1
		fi
	else
		if ! VersionString="$MFFER_SOURCE_COMMIT" ./.dotnet/dotnet \
			publish -c Release mffer/mffer.csproj >"$DEBUGOUT"; then
			warnError "Unable to build mffer."
			return 1
		fi
	fi
	if ! mv mffer/release built-on-macos \
		|| ! tar -czf built-on-macos.tar.gz built-on-macos; then
		warnError "Unable to tar mffer for download"
		return 1
	fi
}
checkVmState() {
	state=""
	for file in apkdl autoanalyze mffer; do
		if [ -f "$file" ]; then
			if [ -n "$state" ]; then
				warnError "Both '$state' and '$file' exist."
				return 1
			fi
			state="$file"
		fi
	done
	if [ -z "$state" ]; then
		state="build"
	fi
	echo "$state"
	return 0
}
cleanup() {
	exitstatus="$?"
	trap - EXIT
	if [ -n "$MFFER_TEST_TMPDIR" ]; then
		if [ 0 != "$exitstatus" ]; then
			echo "Test files in progress available in $MFFER_TEST_TMPDIR"
		else
			echo "Cleaning up..." >"$VERBOSEOUT"
			rm -rf "$MFFER_TEST_TMPDIR"
		fi
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
	if ! curl -sS -L -o "$MFFER_TEST_TMPDIR"/mkmacvm-0.3.1.tar.gz \
		https://github.com/therealchjones/mkmacvm/archive/v"$MKMACVM_VERSION".tar.gz \
		|| ! tar -xf "$MFFER_TEST_TMPDIR"/mkmacvm-"$MKMACVM_VERSION".tar.gz -C "$MFFER_TEST_TMPDIR"; then
		warnError "Unable to get mkmacvm"
		return 1
	fi
	if ! isRoot; then
		warnError "Creating the virtual machine requires root privileges. Using sudo..."
		if ! sudo VERBOSE=y \
			"$MFFER_TEST_TMPDIR"/mkmacvm-"$MKMACVM_VERSION"/mkmacvm; then
			warnError "Unable to build virtual machine"
			return 1
		fi
	else
		if ! VERBOSE=y "$MFFER_TEST_TMPDIR"/mkmacvm-"$MKMACVM_VERSION"/mkmacvm; then
			warnError "Unable to build virtual machine"
			return 1
		fi
	fi
	if ! prlctl set "$VM_NAME" --startup-view headless >"$DEBUGOUT" \
		|| ! prlctl set "$VM_NAME" --memsize 8192 >"$DEBUGOUT" \
		|| ! prlctl snapshot "$VM_NAME" -n "$VM_BASESNAPSHOT" >"$DEBUGOUT"; then
		warnError "Unable to set up virtual machine"
		return 1
	fi
}
description() {
	echo "options:"
	echo "	-h	print this summarized help message and quit"
	echo "	-v	print more information; specify twice for debug output"
}
exitError() {
	error="${*:-Unable to continue. Exiting.}"
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
getCanonicalDir() {
	if [ -n "${1:-}" ] && [ -d "$1" ] && (cd "$1" && pwd); then
		return 0
	else
		warnError "Unable to access directory '${1:-}'"
		return 1
	fi
}
getOptions() {
	vees=0
	while getopts 'hv' option; do
		case "$option" in
			h)
				summary
				usage
				description
				exit 0
				;;
			v)
				vees=$((vees + 1))
				;;
			?)
				usage >&2
				exit 1
				;;
		esac
	done
	shift $((OPTIND - 1))
	if [ "$#" != "0" ]; then
		usage >&2
		exit 1
	fi
	if [ "$vees" -gt 1 ]; then DEBUG=y; fi
	if [ "$vees" -gt 0 ]; then VERBOSE=y; fi
	if [ -n "$DEBUG" ]; then
		set -x
		set -u
		VERBOSE=y
		DEBUGOUT="${DEBUGOUT:-/dev/stdout}"
	fi
	DEBUGOUT="${DEBUGOUT:-/dev/null}"
	if [ -n "$VERBOSE" ]; then
		VERBOSEOUT="${VERBOSEOUT:-/dev/stdout}"
	fi
	VERBOSEOUT="${VERBOSEOUT:-/dev/null}"
}
getRelease() {
	# puts release files in $MFFER_TEST_TMPDIR/mffer-macos/
	scp -q "$VM_HOSTNAME":built-on-macos.tar.gz "$MFFER_TEST_TMPDIR" || exit 1
	{
		tar -xf "$MFFER_TEST_TMPDIR"/built-on-macos.tar.gz \
			-C "$MFFER_TEST_TMPDIR" >"$DEBUGOUT" \
			&& mkdir -p "$MFFER_TEST_TMPDIR"/mffer-macos \
			&& unzip "$MFFER_TEST_TMPDIR"/built-on-macos/mffer-*-osx-x64.zip \
				-d "$MFFER_TEST_TMPDIR"/mffer-macos >"$DEBUGOUT"
	} || {
		warnError "Unable to extract built project on local machine."
		return 1
	}
}
getSourceDir() {
	# getSourceDir() returns the canonicalized path of MFFER_SOURCE_DIR if that
	# variable is already nonempty; otherwise, it tries to identify the local
	# directory based on the location of this running script.
	if [ -z "$MFFER_SOURCE_DIR" ]; then
		if isHostMachine; then
			searchDir="$PROGRAMDIR"/..
		else
			searchDir="$PROGRAMDIR"/mffer
		fi
		if ! projectFile="$(find "$searchDir" -name mffer.csproj -print -quit)" \
			|| ! MFFER_SOURCE_DIR="$(dirname "$projectFile")"; then
			warnError "Unable to find local mffer repository"
			return 1
		fi
	fi
	getCanonicalDir "$MFFER_SOURCE_DIR" || return 1
}
installCommandLineTools() {
	notify "Installing Xcode Command Line Tools..."
	CMDLINETOOLTMP="/tmp/.com.apple.dt.CommandLineTools.installondemand.in-progress"
	touch "$CMDLINETOOLTMP"
	CMDLINETOOLS="$(softwareupdate -l 2>/dev/null \
		| sed -n \
			-e '/Command Line Tools/!d' \
			-e '/[Bb][Ee][Tt][Aa]/d' \
			-e '/^[ \*]*Label: */{s///;p;}' \
		| sort -V \
		| tail -n1)"
	if ! isRoot; then
		warnError "${CMDLINETOOLS}\n must be installed as root. Using sudo..."
	fi
	if ! output="$(sudo softwareupdate -i "$CMDLINETOOLS" 2>&1)"; then
		echo "$output" >&2
		warnError "Unable to install $CMDLINETOOLS"
		return 1
	fi
	rm "$CMDLINETOOLTMP"
}
installDotNet() {
	notify "Installing .NET SDK 5.0..."
	curl -Ss -OL "https://dot.net/v1/dotnet-install.sh" \
		&& sh ./dotnet-install.sh --channel 5.0 >"$DEBUGOUT"
}
installGhidra() {
	notify "Installing Ghidra 10.1.2..."
	curl -Ss -OL https://github.com/NationalSecurityAgency/ghidra/releases/download/Ghidra_10.1.2_build/ghidra_10.1.2_PUBLIC_20220125.zip \
		&& unzip ghidra_10.1.2_PUBLIC_20220125.zip >"$DEBUGOUT"
}
installNodeJs() {
	notify "Installing Node.js 16.13.2..."
	if curl -Ss -O https://nodejs.org/dist/v16.13.2/node-v16.13.2.pkg >"$DEBUGOUT"; then
		if ! isRoot; then
			warnError "Node.js must be installed as root. Using sudo..."
		fi
		if { sudo installer -pkg ./node-v16.13.2.pkg -target /; } >"$DEBUGOUT"; then
			return 0
		fi
	fi
	warnError "Unable to install Node.js"
	return 1
}
installPython() {
	notify "Installing Python $PYTHON_VERSION"
	if ! curl -Ss -OL "https://www.python.org/ftp/python/$PYTHON_VERSION/python-$PYTHON_VERSION-macos11.pkg" >"$DEBUGOUT"; then
		echo "Unable to download Python $PYTHON_VERSION" >&2
		return 1
	fi
	if ! isRoot; then
		warnError "Python must be installed as root. Will use sudo."
	fi
	if ! { sudo installer -pkg "./python-$PYTHON_VERSION-macos11.pkg" -target /; } >"$DEBUGOUT"; then
		echo "Unable to install Python" >&2
		return 1
	fi
	return 0
}
installTemurin() {
	notify "Installing Temurin JRE 11.0.14.1_1..."
	if curl -Ss -OL https://github.com/adoptium/temurin11-binaries/releases/download/jdk-11.0.14.1%2B1/OpenJDK11U-jre_x64_mac_hotspot_11.0.14.1_1.pkg >"$DEBUGOUT"; then
		if ! isRoot; then
			warnError "Temurin must be installed as root. Using sudo..."
		fi
		if { sudo installer -pkg ./OpenJDK11U-jre_x64_mac_hotspot_11.0.14.1_1.pkg -target /; } >"$DEBUGOUT"; then
			return 0
		fi
	fi
	warnError "Unable to install Temurin JRE"
	return 1
}
installsudotools() {
	installCommandLineTools
	installNodeJs
}
isHostMachine() {
	# We assume prlctl is available only on the host machine
	which prlctl >/dev/null 2>&1
}
isRoot() {
	[ 0 = "$(id -u)" ]
}
notify() {
	if [ -n "$*" ]; then
		echo "$*" >"$VERBOSEOUT"
	fi
}
openParallelsGui() {
	if ! open -a "Parallels Desktop"; then
		warnError "Unable to open Parallels Desktop GUI"
		return 1
	fi
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
	until scp -q -o ConnectTimeout=30 "$0" "$VM_HOSTNAME": || [ "$tries" -lt 1 ]; do
		sleep 5
		tries="$((tries - 1))"
	done
	if [ "$tries" -lt 1 ]; then
		warnError "Unable to reconnect to virtual machine"
		return 1
	fi
}
runAutoanalyze() {
	{
		installCommandLineTools \
			&& installTemurin \
			&& installDotNet \
			&& installGhidra
	} || return 1
	GHIDRA="$(find ./ghidra* -name analyzeHeadless)" ./autoanalyze -h
}
runApkdl() {
	if ! installPython \
		|| ! "$PROGRAMDIR"/apkdl ${VERBOSE:+-v} ${DEBUG:+-v} -o "$PROGRAMDIR/mffer-download" \
		|| ! apksdir="$(find "$PROGRAMDIR/mffer-download" -depth 1 -type d -name 'mff-apks-*')" \
		|| [ -z "$(basename "$apksdir")" ] \
		|| ! tar -cf "$PROGRAMDIR/mffer-apks.tar" -C "$PROGRAMDIR"/mffer-download "$(basename "$apksdir")"; then
		return 1
	else
		return 0
	fi
}
runMffer() {
	./mffer -h
}
setup() {
	trap cleanup EXIT
	if ! setSources; then
		warnError "Unable to determine source to test"
		return 1
	fi
	echo "Testing mffer" >"$VERBOSEOUT"
	if [ -n "$MFFER_SOURCE_COMMIT" ]; then
		echo "at commit '$MFFER_SOURCE_COMMIT'" >"$VERBOSEOUT"
	fi
	if [ -n "$MFFER_LOCAL_TREE" ]; then
		echo "from the local tree at" >"$VERBOSEOUT"
		echo "$MFFER_SOURCE_DIR" >"$VERBOSEOUT"
	else
		echo "from the repository at" >"$VERBOSEOUT"
		echo "$MFFER_REPO" >"$VERBOSEOUT"
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
setSources() {
	# Users may specify a commit (or any other object that resolves with git
	# rev-parse), a directory tree, and/or a repository. If the directory is
	# specified explicitly, (i.e., before reaching this function), no
	# checking of the repository is performed and testing is performed directly
	# from the directory tree.
	#
	# setSources() will ensure MFFER_SOURCE_COMMIT, MFFER_SOURCE_DIR,
	# MFFER_REPO, and MFFER_LOCAL_TREE are set appropriately, unless invalid
	# settings arise, in which case the function returns a nonzero status.
	#
	# WIP
	#
	if [ -n "$MFFER_SOURCE_DIR" ]; then
		error="Local source tree explicitly provided as\n"
		error="$error $MFFER_SOURCE_DIR;\n"
		if ! MFFER_SOURCE_DIR="$(getSourceDir)"; then
			error="$error however, this directory could not be accessed."
			warnError "$error"
			return 1
		fi
		error="$error will use this without checking remote repository."
		warnError "$error"
		MFFER_LOCAL_TREE=y
		# MFFER_SOURCE_DIR is nonempty & working, MFFER_LOCAL_TREE=y
	elif [ -n "$MFFER_LOCAL_TREE" ]; then
		error="Local source tree testing explicitly requested;\n"
		if ! MFFER_SOURCE_DIR="$(getSourceDir)"; then
			error="$error however, the local source tree could not be found."
			warnError "$error"
			return 1
		fi
		error="$error will use it without checking remote repository."
		warnError "$error"
		# MFFER_SOURCE_DIR is nonempty & working, MFFER_LOCAL_TREE=y
	elif ! MFFER_SOURCE_DIR="$(getSourceDir)"; then
		error="Unable to identify local source tree;\n"
		if [ -z "$MFFER_REPO" ]; then
			error="$error however, no repository was provided."
			warnError "$error"
			return 1
		elif ! git ls-remote "$MFFER_REPO" >"$DEBUGOUT" 2>&1; then
			error="$error however, the provided repository\n"
			error="$error ($MFFER_REPO)\n "
			error="$error is not valid."
			warnError "$error"
			return 1
		elif [ -n "$MFFER_SOURCE_COMMIT" ]; then
			if ! git ls-remote --exit-code "$MFFER_REPO" "$MFFER_SOURCE_COMMIT" >"$DEBUGOUT" 2>&1; then
				error="$error however, the requested revision identifier\n"
				error="$error ($MFFER_SOURCE_COMMIT)\n"
				error="$error was not found in the provided repository\n"
				error="$error ($MFFER_REPO)."
				warnError "$error"
				return 1
			else
				error="$error will use the provided repository\n ($MFFER_REPO)\n alone."
				warnError "$error"
			fi
		else
			error="$error will use the provided repository\n ($MFFER_REPO)\n alone."
			warnError "$error"
			warnError "No revision identifier provided; will default to HEAD"
			MFFER_SOURCE_COMMIT=HEAD
		fi
		# At the end of this elif clause (if we haven't returned):
		# MFFER_SOURCE_DIR & MFFER_LOCAL_TREE are empty as there is no working
		# local tree, but MFFER_REPO is working and nonempty and contains
		# MFFER_SOURCE_COMMIT (or is assumed to, in the case MFFER_SOURCE_COMMIT=HEAD)
	else
		# At the beginning of this else clause,
		# MFFER_SOURCE_DIR was not provided but is now nonempty and working
		# MFFER_LOCAL_TREE was not requested and is now empty
		if [ -z "$MFFER_REPO" ]; then
			if ! MFFER_REPO="$(git -C "$MFFER_SOURCE_DIR" ls-remote --get-url)" 2>"$DEBUGOUT" \
				|| [ -z "$MFFER_REPO" ]; then
				error="Unable to identify a remote repository associated with the local tree at\n"
				error="$error $MFFER_SOURCE_DIRECTORY;\n"
				error="$error will use the local tree alone."
				warnError "$error"
				MFFER_LOCAL_TREE=y
			fi
		fi
		if [ -z "$MFFER_SOURCE_COMMIT" ]; then
			# if the local tree is clean and HEAD of the active branch is tagged, use that,
			# and use the remote iff that commit exists on the repo
			# otherwise use nothing, and use only the local tree
			if ! dirtyTree="$(git -C "$MFFER_SOURCE_DIR" status --porcelain)" 2>"$DEBUGOUT" \
				|| [ -n "$dirtyTree" ]; then
				error="The local source tree at \n"
				error="$error $MFFER_SOURCE_DIR\n"
				error="$error has uncommitted changes; will use the tree as is."
				warnError "$error"
				MFFER_LOCAL_TREE=y
			elif ! headTag="$(git -C "$MFFER_SOURCE_DIR" tag --points-at)" 2>"$DEBUGOUT" \
				|| [ -z "$headTag" ]; then
				error="No tag is associated with HEAD in the local source tree at\n"
				error="$error $MFFER_SOURCE_DIR;\n"
				error="$error will use the tree as is."
				warnError "$error"
				MFFER_LOCAL_TREE=y
			else
				error="Using the tag $headTag\n"
				error="$error from the HEAD of the local repository at\n"
				error="$error $MFFER_SOURCE_DIR"
				warnError "$error"
				MFFER_SOURCE_COMMIT="$headTag"
			fi
		fi
		if [ -n "$MFFER_SOURCE_COMMIT" ] && [ -z "$MFFER_LOCAL_TREE" ] && [ -n "$MFFER_REPO" ]; then
			if ! remoteCommit="$(
				git -C "$MFFER_SOURCE_DIR" ls-remote \
					--exit-code "$MFFER_REPO" "$MFFER_SOURCE_COMMIT"
			)" 2>"$DEBUGOUT"; then
				error="The remote repository at \n"
				error="$error $MFFER_REPO\n"
				error="$error does not contain a revision with identifier\n"
				error="$error $MFFER_SOURCE_COMMIT;\n"
				error="$error will use the local source tree only."
				warnError "$error"
				MFFER_LOCAL_TREE=y
			fi
		fi
		if [ -n "$MFFER_SOURCE_COMMIT" ] && [ -z "$MFFER_LOCAL_TREE" ]; then
			localCommit="$(
				git -C "$MFFER_SOURCE_DIR" rev-parse --verify --end-of-options "$MFFER_SOURCE_COMMIT"
			)" 2>"$DEBUGOUT"
			if [ "$remoteCommit" = "${remoteCommit#"$localCommit"}" ]; then
				error="The revisions identified as \n"
				error="$error $MFFER_SOURCE_COMMIT\n"
				error="$error are different in the local tree\n"
				error="$error ($localCommit)\n"
				error="$error and the remote repository\n"
				error="$error (${remoteCommit%%[ 	]*})."
				warnError "$error"
				return 1
			fi
		fi
	fi
}
summary() {
	echo "$(basename "$0"): test mffer building and running"
}
testAutoanalyze() {
	resetVm || return 1
	echo "Testing autoanalyze on $VM_NAME..." >"$VERBOSEOUT"
	if ! scp -q "$MFFER_TEST_TMPDIR"/mffer-macos/autoanalyze \
		"$MFFER_TEST_TMPDIR"/mffer-apks.tar \
		"$VM_HOSTNAME": \
		|| ! ssh -qt "$VM_HOSTNAME" "sh ./$PROGRAMNAME ${VERBOSE:+-v} ${DEBUG:+-v}"; then
		warnError "Unable to configure virtual machine to test autoanalyze."
		return 1
	fi
	notify "autoanalyze run successfully."
}
testApkdl() {
	resetVm || return
	echo "Testing apkdl on $VM_NAME..." >"$VERBOSEOUT"
	notify "Reconfiguring VM..."

	if ! scp -q "$MFFER_TEST_TMPDIR"/mffer-macos/apkdl \
		"$VM_HOSTNAME":; then
		warnError "Unable to configure virtual machine to test apkdl."
		return 1
	fi
	# because the apkdl script needs a username and password, we should allocate
	# a tty using ssh -t
	notify "apkdl requires a Google account and app password"
	ssh -qt "$VM_HOSTNAME" "sh ./$PROGRAMNAME ${VERBOSE:+-v} ${DEBUG:+-v}" || return 1
	if ! scp -q "$VM_HOSTNAME":mffer-apks.tar "$MFFER_TEST_TMPDIR"; then
		warnError "Unable to get apkdl-downloaded files from the virtual machine"
		return 1
	fi

	notify "apkdl run successfully."
}
testBuild() {
	resetVm || return 1
	notify "Building mffer on $VM_NAME..."
	if [ -n "$MFFER_LOCAL_TREE" ]; then
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
	# because the buildrelease function may need sudo, we should allocate a tty
	# using ssh -t
	if ! ssh -qt "$VM_HOSTNAME" "MFFER_SOURCE_COMMIT='$MFFER_SOURCE_COMMIT' sh './$PROGRAMNAME' ${VERBOSE:+-v} ${DEBUG:+-v}"; then
		failError "build"
		return 1
	fi
	notify "mffer built successfully."
}
testMffer() {
	resetVm || return 1
	echo "Testing mffer on $VM_NAME..." >"$VERBOSEOUT"
	scp -q "$MFFER_TEST_TMPDIR"/mffer-macos/mffer "$VM_HOSTNAME": || return 1
	ssh -q "$VM_HOSTNAME" "sh ./$PROGRAMNAME ${VERBOSE:+-v} ${DEBUG:+-v}" || return 1
	notify "mffer run successfully."
}
usage() {
	echo "usage: $(basename "$0") [-v]"
	echo "       $(basename "$0") -h"
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

main "$@"
