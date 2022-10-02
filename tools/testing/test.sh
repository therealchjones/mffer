#!/bin/sh

# Run mffer build & function tests. This script is expected to be run from
# within the mffer development environment (see the mffer Development Guide for
# details).

# This script accepts no command line arguments or options. Some settings may be
# modified using environment variables.

MFFER_TEST_PROGRAM="$0"
MFFER_TEST_DIR="$(basename "$MFFER_TEST_PROGRAM")"
VERBOSE="${VERBOSE:-y}"
. "$MFFER_TEST_DIR"/common/testfxns.sh

MFFER_TEST_NESTED="${MFFER_TEST_NESTED:=}"
MFFER_TEST_SNAPSHOT="${MFFER_TEST_SNAPSHOT:-Base Installation}"
MFFER_TEST_TMPDIR="${MFFER_TEST_TMPDIR:=}"

main() {
	if [ -n "${MFFER_TEST_NESTED:=}" ]; then
		runNested "$MFFER_TEST_NESTED"
	else
		setMfferSource || exitFromError
		trap cleanup EXIT
		setTmpdir || exitFromError
		buildOn local || exitFromError
		testOn local || exitFromError
		for os in macos linux windows; do
			buildOn "$os"
		done
		for os in macos linux windows; do
			testOn "$os"
		done
	fi
	exit
}
cleanup() {
	if [ -n "${MFFER_TEST_TMPDIR:=}" ]; then
		rm -rf "${MFFER_TEST_TMPDIR:=}"
	fi
}
buildDocs() {
	echo "building documentation" >"$VERBOSEOUT"
	python3 -m venv "$MFFER_TEST_TMPDIR"/python
	if ! (
		. "$MFFER_TEST_TMPDIR"/python/bin/activate \
			&& pip3 install --upgrade pip >"$DEBUGOUT" \
			&& pip3 install -r "$MFFER_TREE_ROOT"/tools/requirements.txt >"$DEBUGOUT" \
			&& cd "$MFFER_TREE_ROOT" \
			&& sh tools/mkdocs.sh >"$DEBUGOUT"
	); then
		echo "FAILED building documentation" >&2
		FAILS="${FAILS:-0}"
		FAILS="$((FAILS + 1))"
		return 1
	fi
	echo "PASSED building documentation" >"$VERBOSEOUT"
}
buildMffer() {
	echo "building mffer" >"$VERBOSEOUT"
	setCommit || return 1
	if ! dotnet restore "$MFFER_TREE_ROOT"/mffer.csproj >"$DEBUGOUT" \
		|| ! dotnet clean "$MFFER_TREE_ROOT"/mffer.csproj >"$DEBUGOUT" \
		|| ! VersionString="$MFFER_SOURCE_COMMIT" dotnet publish -c Release "$MFFER_TREE_ROOT"/mffer.csproj >"$DEBUGOUT"; then
		FAILS="${FAILS:-0}"
		FAILS="$((FAILS + 1))"
		echo "FAILED building mffer" >"$VERBOSEOUT"
		return 1
	fi
	echo "PASSED building mffer" >"$VERBOSEOUT"
}
buildOn() {
	if [ "$#" -ne 1 ]; then
		echo "Error: buildOn() requires a single argument" >&2
		return 1
	fi
	FAILS=0
	case "$1" in
		local)
			echo "Building release on the local system" >"$VERBOSEOUT"
			if ! buildMffer; then
				echo "Error: Failed building mffer on the local system" >&2
			fi
			if ! buildDocs; then
				echo "Error: Failed building documentation on the local system" >&2
			fi
			if [ "${FAILS:=0}" -gt 0 ]; then
				echo "FAILED building release on the local system" >&2
				return 1
			fi
			echo "PASSED building release on the local system" >"$VERBOSEOUT"
			return 0
			;;
		macos | linux | windows)
			if ! setVm "$1"; then
				echo "SKIPPED building release on $1" >"$VERBOSEOUT"
				return 1
			else
				echo "Building release on $1 using VM '$MFFER_TEST_VM'" >"$VERBOSEOUT"
				if ! setBuildEnv "$1"; then
					echo "SKIPPED building release on $1" >"$VERBOSEOUT"
					return 1
				fi
				runOnVm buildMffer \
					|| echo "Error: Failed building mffer on $1" >&2
				runOnVm buildDocs \
					|| echo "Error: Failed building documentation on $1" >&2
				if [ "${FAILS:=0}" -gt 0 ]; then
					echo "FAILED building release on $1" >&2
					return 1
				fi
				echo "PASSED building release on $1" >"$VERBOSEOUT"
				return 0
			fi
			;;
		*)
			echo "Error: Unknown system '$1'; unable to build" >&2
			return 1
			;;
	esac
}
# checkVm
# ensures a VM named MFFER_TEST_VM exists and includes a base installation
# snapshot and sets MFFER_TEST_SNAPSHOT_ID
checkVm() {
	if ! vmExists "$MFFER_TEST_VM"; then
		echo "Error: VM '$MFFER_TEST_VM' was not found" >&2
		return 1
	fi
	if ! setSnapshotId; then
		echo "Error: VM '$MFFER_TEST_VM' is not properly configured" >&2
		return 1
	fi
}
exitFromError() {
	echo "Testing is unable to continue due to errors." >&2
	exit 1
}
installOnVm() {
	if [ "$#" -ne "1" ] || [ -z "$1" ]; then
		echo "Error: installOnVm() requires a single argument" >&2
		return 1
	fi
	if [ -z "${MFFER_TEST_VM:=}" ] \
		|| [ -z "${MFFER_TEST_OS:=}" ]; then
		echo "Error: MFFER_TEST_VM and MFFER_TEST_OS are empty. Run setVm() before installOnVm()" >&2
		return 1
	fi
	case "$1" in
		shell)
			if [ "$MFFER_TEST_OS" = "windows" ]; then
				if ! ssh "$MFFER_TEST_VM:" curl -LSsO https://github.com/git-for-windows/git/releases/download/v2.37.3.windows.1/Git-2.37.3-64-bit.exe >"$DEBUGOUT" \
					|| ! ssh "$MFFER_TEST_VM:" Git-2.37.3-64-bit.exe >"$DEBUGOUT"; then
					echo "Error: Unable to install '$1' on $MFFER_TEST_VM" >&2
					return 1
				fi
			fi
			;;
		dotnet)
			true
			;;
		node)
			true
			;;
		git)
			true
			;;
		python)
			true
			;;
		doxygen)
			true
			;;
		*)
			echo "Error: No recipe to install '$1' on virtual machine" >&2
			return 1
			;;
	esac
}
# resetVm
# resets the VM named MFFER_TEST_VM to the snapshot with ID MFFER_TEST_SNAPSHOT_ID and
# ensures the VM is running and accepting ssh connections. If either of the variables are empty, if the reset fails, or
# if it cannot be confirmed that the VM is running and accepting ssh connections, returns 1.
# Additionally sets MFFER_TEST_VM_HOSTNAME.
resetVm() {
	if [ -z "$MFFER_TEST_VM" ]; then
		echo "MFFER_TEST_VM is empty. Use setVm() before resetVm()." >&2
		return 1
	fi
	if [ -z "$MFFER_TEST_SNAPSHOT_ID" ]; then
		echo "MFFER_TEST_SNAPSHOT_ID is empty. Use setSnapshotId() before resetVm()." >&2
		return 1
	fi
	echo "Resetting virtual machine '$MFFER_TEST_VM'" >"$VERBOSEOUT"
	if ! prlctl snapshot-switch "$MFFER_TEST_VM" --id "$MFFER_TEST_SNAPSHOT_ID" >"$DEBUGOUT"; then
		echo "Error: Unable to reset VM '$MFFER_TEST_VM' to snapshot '$MFFER_TEST_SNAPSHOT'" >&2
		return 1
	fi
	tries=5
	until [ "$tries" -lt 1 ]; do
		vm_status=""
		if ! vm_status="$(prlctl status "$MFFER_TEST_VM")" \
			|| [ -z "$vm_status" ]; then
			echo "Error: Unable to get status of VM '$MFFER_TEST_VM'" >&2
			return 1
		fi
		vm_status="${vm_status#VM "$MFFER_TEST_VM" exist }"
		case "$vm_status" in
			stopped | suspended)
				if ! prlctl start "$MFFER_TEST_VM" >"$DEBUGOUT"; then
					echo "Error: Unable to start VM '$MFFER_TEST_VM'" >&2
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
		echo "Error: VM '$MFFER_TEST_VM' did not start" >&2
		return 1
	fi
	# wait for the VM to start
	tries=12
	MFFER_TEST_VM_HOSTNAME="$(echo "$MFFER_TEST_VM" | tr 'A-Z ' 'a-z-')"
	until scp -q -o ConnectTimeout=30 "$0" "$MFFER_TEST_VM_HOSTNAME": || [ "$tries" -lt 1 ]; do
		sleep 5
		tries="$((tries - 1))"
	done
	if [ "$tries" -lt 1 ]; then
		echo "Error: Unable to connect to reset virtual machine '$MFFER_TEST_VM' ('$MFFER_TEST_VM_HOSTNAME')" >&2
		return 1
	fi
}
runApkdl() {
	echo "Running apkdl" >"$VERBOSEOUT"
	if ! setApkdl \
		|| ! "${APKDL:=}" -h >"$DEBUGOUT"; then
		echo "FAILED running apkdl" >&2
		return 1
	fi
	echo "PASSED running apkdl"
}
runAutoanalyze() {
	echo "Running autoanalyze" >"$VERBOSEOUT"
	if ! setAutoanalyze \
		|| ! "${AUTOANALYZE:=}" -h >"$DEBUGOUT"; then
		echo "FAILED running autoanalyze" >&2
		return 1
	fi
	echo "PASSED running autoanalyze"
}
runMffer() {
	echo "Running mffer" >"$VERBOSEOUT"
	setMffer || return 1
	if ! "$MFFER" -h >"$DEBUGOUT"; then
		echo "Error: Unable to run mffer at '$MFFER'" >"$VERBOSEOUT"
		return 1
	fi
	echo "PASSED running mffer" >"$VERBOSEOUT"
}
runNested() {
	echo "Error: runNested() not yet implemented" >&2
	return 1
}
runOnVm() {
	if [ "$#" -eq 0 ]; then
		echo "Error: runOnVm() requires one or more arguments" >&2
		return 1
	fi
	echo "Error: runOnVm() not yet implemented" >&2
	return 1
}
setApkdl() {
	if [ -n "${APKDL:=}" ] \
		&& [ -x "$APKDL" ]; then
		return 0
	fi
	if [ -z "${MFFER_RUN_DIR:=}" ] \
		|| [ ! -d "$MFFER_RUN_DIR" ]; then
		echo "MFFER_RUN_DIR is not set properly. Run setMffer() before setApkdl()" >&2
		return 1
	fi
	if [ ! -x "$MFFER_RUN_DIR/apkdl" ]; then
		echo "Unable to locate apkdl at '$MFFER_RUN_DIR/apkdl'" >&2
		return 1
	fi
	APKDL="$MFFER_RUN_DIR/apkdl"
}
setAutoanalyze() {
	if [ -n "${AUTOANALYZE:=}" ] \
		&& [ -x "$AUTOANALYZE" ]; then
		return 0
	fi
	if [ -z "${MFFER_RUN_DIR:=}" ] \
		|| [ ! -d "$MFFER_RUN_DIR" ]; then
		echo "MFFER_RUN_DIR is not set properly. Run setMffer() before setAutoanalyze()" >&2
		return 1
	fi
	if [ ! -x "$MFFER_RUN_DIR/autoanalyze" ]; then
		echo "Unable to locate autoanalyze at '$MFFER_RUN_DIR/autoanalyze'" >&2
		return 1
	fi
	AUTOANALYZE="$MFFER_RUN_DIR/autoanalyze"
}
setBuildEnv() {
	if [ -z "${MFFER_TEST_VM:=}" ] \
		|| [ -z "${MFFER_TEST_OS:=}" ]; then
		echo "Error: MFFER_TEST_VM or MFFER_TEST_OS is empty. Run setVm() before setBuildEnv()." >&2
		return 1
	fi
	if ! tar cf "$MFFER_TEST_TMPDIR"/mffer-tree.tar -C "$MFFER_TREE_ROOT" . \
		|| ! scp -q "$MFFER_TEST_TMPDIR/mffer-tree.tar" "$MFFER_TEST_VM_HOSTNAME": >"$DEBUGOUT" \
		|| ! ssh -q "$MFFER_TEST_VM_HOSTNAME:" mkdir -p mffer-tree \
		|| ! ssh -q "$MFFER_TEST_VM_HOSTNAME:" tar xf mffer-tree.tar -C mffer-tree; then
		echo "Error: Unable to copy source code to VM '$MFFER_TEST_VM'" >&2
		return 1
	fi
	if ! installOnVm shell \
		|| ! installOnVm dotnet \
		|| ! installOnVm node \
		|| ! installOnVm git \
		|| ! installOnVm python \
		|| ! installOnVm doxygen; then
		echo "Error: Unable to set up development environment on VM '$MFFER_TEST_VM'" >&2
		return 1
	fi
}
setBuildOs() {
	if [ -n "${MFFER_BUILD_OS:=}" ]; then
		return 0
	fi
	output=""
	if ! output="$(uname -s)" || [ -z "$output" ]; then
		echo "Error: Unable to determine operating system" >&2
		return 1
	fi
	case "$output" in
		Darwin)
			MFFER_BUILD_OS="macos"
			;;
		Linux)
			MFFER_BUILD_OS="linux"
			;;
		*)
			echo "Warning: Operating system '$output' not recognized; assuming Windows" >&2
			MFFER_BUILD_OS="windows"
			;;
	esac
}
setCommit() {
	MFFER_SOURCE_COMMIT='prerelease-testing'
}
setMffer() {
	if [ -n "${MFFER_RUN_DIR:=}" ] \
		&& [ -d "$MFFER_RUN_DIR" ] \
		&& [ -n "${MFFER:=}" ] \
		&& [ -f "$MFFER" ]; then
		return 0
	fi
	setTmpdir || return 1
	setCommit || return 1
	file=""
	case "${MFFER_TEST_OS:=}" in
		linux)
			file="mffer-${MFFER_SOURCE_COMMIT:=}-linux-x64.zip"
			;;
		macos)
			file="mffer-${MFFER_SOURCE_COMMIT:=}-osx-x64.zip"
			;;
		windows)
			file="mffer-${MFFER_SOURCE_COMMIT:=}-win-x64.zip"
			;;
		*)
			echo "Error: MFFER_TEST_OS is not set properly" >&2
			return 1
			;;
	esac
	file="${MFFER_TREE_ROOT:=}/release/${file}"
	if [ ! -r "$file" ]; then
		echo "Error: Unable to find file '${MFFER_TREE_ROOT:=}/release/${file}'" >&2
		return 1
	fi
	rundir="${MFFER_TEST_TMPDIR:=/tmp}/mffer-${MFFER_TEST_OS}-built-on-${MFFER_BUILD_OS}"
	mkdir -p "$rundir" || return 1
	unzip "$file" -d "$rundir" >"$DEBUGOUT" || return 1
	MFFER_RUN_DIR="$rundir"
	MFFER="$MFFER_RUN_DIR/mffer"
	if [ "$MFFER_TEST_OS" = "windows" ]; then MFFER="$MFFER.exe"; fi
	if [ ! -x "$MFFER" ]; then
		echo "Error: Unable to open mffer executable '$MFFER' for testing" >&2
		return 1
	fi
}
setMfferSource() {
	MFFER_TREE_ROOT="${MFFER_TREE_ROOT:-$(dirname "$MFFER_TEST_PROGRAM")/..}"
	if [ ! -d "$MFFER_TREE_ROOT" ] \
		|| [ ! -f "$MFFER_TREE_ROOT"/mffer.csproj ]; then
		echo "Error: $0 does not appear to be in the proper location." >&2
		echo "       Run 'mffer/tools/test.sh' (where 'mffer' is the root of the" >&2
		echo "       mffer source repository) instead." >&2
		exit 1
	fi
}
# setSnapshotid
# Sets MFFER_TEST_SNAPSHOT_ID to the ID of a snapshot named MFFER_TEST_SNAPSHOT
# in the VM named MFFER_TEST_VM. If no such VM exists or it does not contain such
# a snapshot, prints an error message and returns 1.
setSnapshotId() {
	if [ -z "${MFFER_TEST_VM:=}" ] || ! vmExists "$MFFER_TEST_VM"; then
		echo "Error: Unable to identify VM '$MFFER_TEST_VM'" >&2
		return 1
	fi
	output=""
	if ! output="$(prlctl snapshot-list "$MFFER_TEST_VM" -j)"; then
		echo "Error: Unable to get the list of snapshots for VM '$MFFER_TEST_VM'" >&2
		return 1
	fi
	if [ -z "$output" ]; then
		return 1
	fi
	snapshots=""
	snapshots="$(plutil -create xml1 - -o - | plutil -insert snapshots -json "$output" - -o -)"
	if [ -z "$snapshots" ]; then
		echo "Error: Unable to parse the list of snapshots for VM '$MFFER_TEST_VM'" >&2
		return 1
	fi
	for snapshotid in $(echo "$snapshots" | plutil -extract snapshots raw -expect dictionary - -o -); do
		snapshotname="$(echo "$snapshots" | plutil -extract "snapshots.$snapshotid.name" raw -expect string - -o -)"
		if [ "${MFFER_TEST_SNAPSHOT:-Base Installation}" = "$snapshotname" ]; then
			MFFER_TEST_SNAPSHOT_ID="$snapshotid"
			return 0
		fi
	done
	return 1
}
setTmpdir() {
	if [ -n "${MFFER_TEST_TMPDIR:=}" ] && [ -d "$MFFER_TEST_TMPDIR" ]; then
		return 0
	fi
	if ! MFFER_TEST_TMPDIR="$(mktemp -d -t mffer-test)" || [ -z "$MFFER_TEST_TMPDIR" ] || [ ! -d "$MFFER_TEST_TMPDIR" ]; then
		echo "Error: Unable to create temporary directory" >&2
		return 1
	fi
}
# setVm [os]
# Sets MFFER_TEST_OS to os, ensures there is a virtual machine running os that is appropriate for testing
# and/or building mffer, sets MFFER_TEST_VM and MFFER_TEST_SNAPSHOT_ID, then
# resets the VM named MFFER_TEST_VM to the snapshot MFFER_TEST_SNAPSHOT_ID
setVm() {
	if [ "$#" -ne 1 ]; then
		echo "Error: setVm() requires a single argument" >&2
		return 1
	fi
	MFFER_TEST_VM=""
	MFFER_TEST_SNAPSHOT_ID=""
	case "$1" in
		linux)
			MFFER_TEST_VM="Linux Testing"
			;;
		macos)
			MFFER_TEST_VM="macOS Testing"
			;;
		windows)
			MFFER_TEST_VM="Windows Testing"
			;;
		*)
			echo "Error: '$1' is not a valid argument to setVm()" >&2
			return 1
			;;
	esac
	MFFER_TEST_OS="$1"
	checkVm || return 1
	resetVm || return 1
}
testOn() {
	if [ "$#" -ne 1 ]; then
		echo "Error: testOn() requires a single argument" >&2
		return 1
	fi
	FAILED=0
	MFFER=""
	APKDL=""
	AUTOANALYZE=""
	MFFER_TEST_VM=""
	MFFER_TEST_SNAPSHOT_ID=""
	MFFER_TEST_OS=""
	case "$1" in
		local)
			echo "Testing on the local system" >"$VERBOSEOUT"
			FAILED="0"
			setBuildOs || return 1
			MFFER_TEST_OS="${MFFER_BUILD_OS:=}"
			MFFER_BUILD_OS=local
			setMffer || return 1
			if ! runMffer; then
				echo "FAILED running mffer on the local system" >&2
				FAILED="$((FAILED + 1))"
			fi
			if ! runApkdl; then
				echo "FAILED running apkdl on the local system" >&2
				FAILED="$((FAILED + 1))"
			fi
			if ! runAutoanalyze; then
				echo "FAILED running autoanalyze on the local system" >&2
				FAILED="$((FAILED + 1))"
			fi
			if [ "${FAILED:=0}" -gt 0 ]; then
				echo "FAILED ${FAILED} tests on the local system" >&2
				return 1
			else
				echo "PASSED testing on the local system" >"$VERBOSEOUT"
			fi
			;;
		linux | macos | windows)
			setVm "$1" || return 1
			echo "Error: testOn $1 is not yet implemented" >&2
			return 1
			;;
		*)
			echo "Error: '$1' is not a valid argument to testOn()" >&2
			return 1
			;;
	esac
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
main
