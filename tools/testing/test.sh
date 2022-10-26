#!/bin/sh

# Run mffer build & function tests. This script is expected to be run from
# within the mffer development environment (see the mffer Development Guide for
# details). This script is not intended to be run as a GitHub Action (though it
# may be, with the caveat that Paralells Desktop installation would also be
# required). For the usual GitHub Action automated tests, see .github/workflows/
# in the repository root.

# This script evaluates no command line arguments or options. Some settings may
# be modified using environment variables.

VERBOSE="${VERBOSE:-y}" # Include brief progress updates and PASSED notices

# This is a simplified way to guess the location of the script given the
# limitations noted in https://mywiki.wooledge.org/BashFAQ/028 and thus
# requiring $0 to be a path (absolute or relative to $PWD) to this script.
# (Hence, for instance, this script will not run properly if piped.) This is
# necessary to note where to build source or obtain built releases, so we also
# use it for modularizing scripts.
MFFER_TREE_ROOT="${MFFER_TREE_ROOT:-}"
if [ -z "$MFFER_TREE_ROOT" ]; then
	if [ -z "${0:-}" ] || [ ! -e "$0" ]; then
		echo "Error: Unable to determine script location. Try setting MFFER_TREE_ROOT." >&2
		exit 1
	fi
	MFFER_TREE_ROOT="$(
		if ! cd "$(dirname "$0")/../.." || ! pwd; then
			echo "Error: Unable to access script location." >&2
			exit 1
		fi
	)"
fi
if [ -z "$MFFER_TREE_ROOT" ]; then
	echo "Error: Unable to find script location. Try setting MFFER_TREE_ROOT." >&2
	exit 1
fi
MFFER_TEST_DIR="$MFFER_TREE_ROOT/tools/testing"
if [ ! -r "$MFFER_TEST_DIR"/common/base.sh ] \
	|| ! . "$MFFER_TEST_DIR"/common/base.sh; then
	echo "Error: Unable to load script definitions" >&2
	exit 1
fi
if [ ! -r "$MFFER_TEST_DIR"/common/parallels.sh ] \
	|| ! . "$MFFER_TEST_DIR"/common/parallels.sh; then
	echo "Warning: Unable to load Parallels Desktop definitions" >&2
fi
main() {
	setSources || exitFromError
	buildOn local || exitFromError
	testOn local || exitFromError
	if [ -z "${MFFER_TEST_VM_SYSTEM:=}" ]; then
		echo "Warning: No virtual machine system found. Skipping virtual machine tests." >&2
		exit 0
	fi
	for os in macos linux windows; do
		buildOn "$os" || true
	done
	for os in macos linux windows; do
		testOn "$os" || true
	done
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
			echo "building release on the local system" >"$VERBOSEOUT"
			MFFER_BUILD_OS="local"
			sh "$MFFER_TEST_DIR/common/build-mffer.sh" || FAILS="$((FAILS + 1))"
			sh "$MFFER_TEST_DIR/common/build-docs.sh" || FAILS="$((FAILS + 1))"
			if [ "$FAILS" -gt 0 ]; then
				echo "FAILED building release on the local system" >&2
				return 1
			fi
			echo "PASSED building release on the local system" >"$VERBOSEOUT"
			return 0
			;;
		macos | linux | windows)
			if ! setVm "$1" || [ -z "$MFFER_TEST_VM" ]; then
				echo "SKIPPED building release on $1" >"$VERBOSEOUT"
				return 1
			else
				echo "building release on $1 using VM '$MFFER_TEST_VM'" >"$VERBOSEOUT"
				MFFER_TEST_OS="$1"
				MFFER_BUILD_OS="$1"
				if ! resetVm "$MFFER_TEST_VM" "$MFFER_TEST_SNAPSHOT" \
					|| ! setBuildEnv "$1"; then
					echo "SKIPPED building release on $1" >"$VERBOSEOUT"
					return 1
				fi
				updateEnv
				runOnVm build-mffer \
					|| echo "Error: Failed building mffer on $1" >&2
				runOnVm build-docs \
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
exitFromError() {
	echo "Testing is unable to continue due to errors." >&2
	exit 1
}
installOnVm() {
	if [ "$#" -ne "1" ] || [ -z "$1" ]; then
		echo "Error: installOnVm() requires a single argument" >&2
		return 1
	fi
	if [ -z "${MFFER_TEST_VM:=}" ]; then
		echo "Error: MFFER_TEST_VM is empty." >&2
		return 1
	fi
	if ! MFFER_TEST_VM_HOSTNAME="$(getVmHostname "$MFFER_TEST_VM")"; then
		echo "Error: Unable to get hostname for VM '$MFFER_TEST_VM'" >&2
		return 1
	fi
	case "$1" in
		shell)
			if [ "$MFFER_TEST_OS" = "windows" ]; then
				if ! scp "$MFFER_TEST_DIR/windows/disable-uac.bat" "$MFFER_TEST_VM_HOSTNAME": \
					|| ! ssh windows-testing cmd.exe /C disable-uac.bat \
					|| ! ssh windows-testing cmd.exe /C shutdown /s \
					|| ! waitForShutdown "$MFFER_TEST_VM" \
					|| ! startVm "$MFFER_TEST_VM" \
					|| ! waitForStartup "$MFFER_TEST_VM" \
					|| ! scp "$MFFER_TEST_DIR/windows/install-shell.bat" "$MFFER_TEST_VM_HOSTNAME": \
					|| ! ssh windows-testing cmd.exe /C install-shell.bat; then
					echo "Error: Unable to install shell on Windows" >&2
					return 1
				fi
			fi
			;;
		dotnet)
			if ! runOnVm install-dotnet; then
				echo "Error: Unable to install .NET SDK" >&2
				return 1
			fi
			;;
		node)
			if ! runOnVm install-node; then
				echo "Error: unable to install Node.js" >&2
				return 1
			fi
			;;
		git)
			if [ "$MFFER_TEST_OS" = "macos" ]; then
				runOnVm install-commandlinetools || return 1
			elif [ "$MFFER_TEST_OS" = "linux" ]; then
				runOnVm install-git || return 1
			fi
			true
			;;
		python)
			if ! runOnVm install-python; then
				echo "Error: unable to install Python" >&2
				return 1
			fi
			true
			;;
		doxygen)
			if ! runOnVm install-doxygen; then
				echo "Error: unable to install Doxygen" >&2
				return 1
			fi
			true
			;;
		java)
			if [ "$MFFER_TEST_OS" = "macos" ]; then
				runOnVm install-temurin || return 1
			fi
			;;
		ghidra)
			if ! runOnVm install-ghidra; then
				echo "Error: unable to install Ghidra" >&2
				return 1
			fi
			;;
		*)
			echo "Error: No recipe to install '$1' on virtual machine" >&2
			return 1
			;;
	esac
}
runOnVm() {
	if [ "$#" -eq 0 ]; then
		echo "Error: runOnVm() requires one or more arguments" >&2
		return 1
	fi
	if [ -z "${MFFER_TEST_OS:=}" ]; then
		echo "Error: MFFER_TEST_OS is empty" >&2
	fi
	script=""
	script="$(getScript "$1")"
	scp "$script" "$MFFER_TEST_VM_HOSTNAME:" >"$DEBUGOUT"
	basename="$(basename "$script")"
	if [ "$script" != "${script%.bat}" ]; then
		shell="cmd.exe"
		scriptfile="runscript.bat"
		# shellcheck disable=SC2087 # allow expansion of the below variables on the client side
		ssh -q "$MFFER_TEST_VM_HOSTNAME" "cmd.exe /C more > $scriptfile" <<-EOF
			${DEBUG:+@echo off}
			$basename
		EOF
	else
		shell="sh"
		scriptfile="runscript.sh"
		# shellcheck disable=SC2087 # allow expansion of the below variables on the client side
		ssh -q "$MFFER_TEST_VM_HOSTNAME" "sh -c 'cat > $scriptfile'" <<-EOF
			#!/bin/sh

			export DEBUG="$DEBUG"
			if [ -n "$DEBUG" ]; then set +x; fi

			export MFFER_TEST_TMPDIR="tmpdir"
			export MFFER_TEST_SOURCE="mffer-source"
			export MFFER_TEST_RUNDIR=mffer
			export MFFER_TEST_OS="$MFFER_TEST_OS"
			export MFFER_TEST_COMMIT="$MFFER_TEST_COMMIT"
			export MFFER_BUILD_OS="$MFFER_BUILD_OS"
			export DEBUGOUT="$DEBUGOUT"
			export PYTHON_VERSION="$PYTHON_VERSION"
			export VERBOSEOUT="$VERBOSEOUT"
			export PATH="$PATH:$HOME/.dotnet"

			sh $basename
		EOF
	fi
	ssh -q "$MFFER_TEST_VM_HOSTNAME" "$shell $scriptfile"
}
setBuildEnv() {
	if [ -z "${MFFER_TEST_VM:=}" ]; then
		echo "Error: MFFER_TEST_VM is empty." >&2
		return 1
	fi
	MFFER_TEST_VM_HOSTNAME="$(getVmHostname "$MFFER_TEST_VM")"
	if ! installOnVm shell \
		|| ! installOnVm dotnet \
		|| ! installOnVm node \
		|| ! installOnVm git \
		|| ! installOnVm python \
		|| ! installOnVm doxygen; then
		echo "Error: Unable to set up development environment on VM '$MFFER_TEST_VM'" >&2
		return 1
	fi
	if ! dotnet clean "$MFFER_TEST_SOURCE" >"$DEBUGOUT" \
		|| ! tar -cf "$MFFER_TEST_TMPDIR"/mffer-tree.tar -C "$MFFER_TEST_SOURCE" . \
		|| ! scp -q "$MFFER_TEST_TMPDIR/mffer-tree.tar" "$MFFER_TEST_VM_HOSTNAME": >"$DEBUGOUT" \
		|| ! ssh -q "$MFFER_TEST_VM_HOSTNAME" mkdir -p mffer-source \
		|| ! ssh -q "$MFFER_TEST_VM_HOSTNAME" tar -xf mffer-tree.tar -m -C mffer-source; then
		echo "Error: Unable to copy source code to VM '$MFFER_TEST_VM'" >&2
		return 1
	fi
}
testOn() {
	if [ "$#" -ne 1 ]; then
		echo "Error: testOn() requires a single argument" >&2
		return 1
	fi
	FAILS=0
	MFFER_TEST_OS=""
	case "$1" in
		local)
			echo "Testing on the local system" >"$VERBOSEOUT"
			MFFER_BUILD_OS=""
			setBuildOs
			MFFER_TEST_OS="$MFFER_BUILD_OS"
			MFFER_BUILD_OS="local"
			updateEnv || return 1
			if ! runTest mffer; then
				echo "FAILED running mffer on the local system" >&2
				FAILS="$((FAILS + 1))"
			fi
			if ! runTest apkdl; then
				echo "FAILED running apkdl on the local system" >&2
				FAILS="$((FAILS + 1))"
			fi
			if ! runTest autoanalyze; then
				echo "FAILED running autoanalyze on the local system" >&2
				FAILS="$((FAILS + 1))"
			fi
			if [ "${FAILS:=0}" -gt 0 ]; then
				echo "FAILED ${FAILS} tests on the local system" >&2
				return 1
			else
				echo "PASSED testing on the local system" >"$VERBOSEOUT"
			fi
			;;
		linux | macos | windows)
			if ! setVm "$1"; then
				echo "SKIPPED building release on $1" >"$VERBOSEOUT"
				return 1
			else
				echo "Error: testOn $1 is not yet implemented" >&2
				return 1
			fi
			;;
		*)
			echo "Error: '$1' is not a valid argument to testOn()" >&2
			return 1
			;;
	esac
}
main
