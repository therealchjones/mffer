#!/bin/sh

# Run mffer build & function tests. This script is expected to be run from
# within the mffer development environment (see the mffer Development Guide for
# details). This script needs to be self-contained to the extent required for
# running commands on a virtual machine, e.g., so we can copy this script alone
# to a VM for portions of testing.

# This script evaluates no command line arguments or options. Some settings may
# be modified using environment variables.

VERBOSE="${VERBOSE:-y}" # Include brief progress updates and PASSED notices

# This is a ridiculously complex way to guess the location of the script given
# the limitations noted in https://mywiki.wooledge.org/BashFAQ/028 and falling
# back to "Run this script from within the mffer repository" if needed. This is
# necessary to note where to build source or obtain built releases, so we also
# use it for modularizing scripts.
MFFER_TREE_ROOT="${MFFER_TREE_ROOT:-}"
if {
	[ -z "$MFFER_TREE_ROOT" ] && ! MFFER_TREE_ROOT="$(
		findTreeRoot() {
			oldpwd=""
			while [ "${PWD:-}" != "$oldpwd" ] && [ ! -f "${PWD:-}/mffer.csproj" ]; do
				oldpwd="${PWD:-}"
				cd .. || break
			done
			possdir="${PWD:-}"
			# Ensure the directory tree is as we expect it. Not foolproof.
			# See https://mywiki.wooledge.org/BashFAQ/028
			if [ -z "$possdir" ] \
				|| [ ! -f "$possdir/mffer.csproj" ] \
				|| [ ! -d "$possdir/tools/testing" ]; then
				return 1
			fi
			echo "$possdir"
		}
		startdir="${PWD:-}"
		if ! findTreeRoot; then
			cd "$startdir" && cd "$(dirname "$0")" && findTreeRoot
		fi
	)" 2>"${DEBUGOUT:-/dev/null}"
} \
	|| [ -z "${MFFER_TREE_ROOT:=}" ]; then
	message="Error: Unable to find the root of the mffer repository. Please run"
	message="$message\n       this script from within the mffer repository."
	echo "$message" >&2
	exit 1
fi

MFFER_TEST_DIR="$MFFER_TREE_ROOT/tools/testing"
# Allow continuing if these importable scripts don't exist, i.e., if we're
# running this one alone on a virtual machine.
if [ -r "$MFFER_TEST_DIR"/common/base.sh ] \
	&& ! . "$MFFER_TEST_DIR"/common/base.sh; then
	echo "Error: Unable to load script definitions" >&2
	exit 1
fi
MFFER_TEST_VM_SYSTEM=""
if [ -r "$MFFER_TEST_DIR"/common/parallels.sh ] \
	&& ! . "$MFFER_TEST_DIR"/common/parallels.sh; then
	echo "Warning: Unable to load Parallels Desktop definitions" >&2
fi
MFFER_TEST_VM_HOSTNAME="$(echo "$MFFER_TEST_VM" | tr 'A-Z ' 'a-z-')"

main() {
	buildOn local || exitFromError
	testOn local || exitFromError
	if [ -z "$MFFER_TEST_VM_SYSTEM" ]; then
		echo "Warning: No virtual machine system found. Skipping virtual machine tests." >&2
		exit 0
	fi
	for os in macos linux windows; do
		buildOn "$os"
	done
	for os in macos linux windows; do
		testOn "$os"
	done
}
buildDocs() {
	setTmpdir || return 1
	echo "building documentation" >"$VERBOSEOUT"
	python3 -m venv "$MFFER_TEST_TMPDIR"/python || return 1
	if ! (
		# The 'activate' script is present only transiently when this is
		# running; disable shellcheck's complaint about not finding it
		# shellcheck disable=SC1091
		. "$MFFER_TEST_TMPDIR"/python/bin/activate \
			&& pip3 install --upgrade pip >"$DEBUGOUT" \
			&& pip3 install \
				-r "$MFFER_TREE_ROOT"/tools/requirements.txt >"$DEBUGOUT" \
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
# buildOn ( local | macos | linux | windows )
#
#
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
# snapshot
checkVm() {
	if ! vmExists "$MFFER_TEST_VM"; then
		echo "Error: VM '$MFFER_TEST_VM' was not found" >&2
		return 1
	fi
	if ! hasSnapshot "$MFFER_TEST_VM" "$MFFER_TEST_SNAPSHOT"; then
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
	if [ -z "${MFFER_TEST_VM:=}" ] || [ -z "${MFFER_TEST_VM_HOSTNAME:=}" ]; then
		echo "Error: MFFER_TEST_VM or MFFER_TEST_VM_HOSTNAME is empty. Run setVm() before setBuildEnv()." >&2
		return 1
	fi
	if ! tar cf "$MFFER_TEST_TMPDIR"/mffer-tree.tar -C "$MFFER_TREE_ROOT" . \
		|| ! scp -q "$MFFER_TEST_TMPDIR/mffer-tree.tar" "$MFFER_TEST_VM_HOSTNAME": >"$DEBUGOUT" \
		|| ! ssh -q "$MFFER_TEST_VM_HOSTNAME" mkdir -p mffer-tree \
		|| ! ssh -q "$MFFER_TEST_VM_HOSTNAME" tar xf mffer-tree.tar -C mffer-tree; then
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
#
# Sets MFFER_TEST_OS to os, ensures there is a virtual machine running os that is appropriate for testing
# and/or building mffer, sets MFFER_TEST_VM and MFFER_TEST_SNAPSHOT_ID, then
# resets the VM named MFFER_TEST_VM to the snapshot MFFER_TEST_SNAPSHOT_ID and sets MFFER_TEST_VM_HOSTNAME
setVm() {
	if [ "$#" -ne 1 ]; then
		echo "Error: setVm() requires a single argument" >&2
		return 1
	fi
	MFFER_TEST_VM=""
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
	checkVm "$MFFER_TEST_VM" || return 1
	MFFER_TEST_VM_HOSTNAME="$(getVmHostname "$MFFER_TEST_VM")"
	resetVm "$MFFER_TEST_VM" "$MFFER_TEST_SNAPSHOT" || return 1
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
main
