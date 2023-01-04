#!/bin/sh

# Run mffer build & function tests. This script is expected to be run from
# within the mffer development environment (see the mffer Development Guide for
# details).

usage() {
	echo "Usage: sh $0 [ local | linux | macos | windows | all ]"
}

# General flow of this and accompanying scripts:
# sh tools/testing/test.sh
# - source ${MFFER_TEST_ENV:=$HOME/mffer_test_env.sh} if it exists
# - find MFFER_TREE_ROOT
# - source common/base.sh, which will
# - - define helpers and determine most MFFER_* values
# - evaluate arguments and environment variables to determine test system
# - - check for VM framework and alter tests or error as needed
# - create a clean source tree from which to build and test
# - build on local if appropriate
# - - if on GitHub actions or VM (defined in mffer_test_env) install dependencies as needed
# - test on local if appropriate
# - for each requested system:
# - - if VM, reset
# - - set mffer_test_env.sh variables to describe system and next step (build)
# - - copy source tree if needed and run system version of this script, which will
# - - - install dependencies if not local system and build
# - - save release builds
# - for each requested system, for each available release, for each test:
# - - reset VM, set mffer_test_env.sh variables to describe system and next test
# - - copy release and copy source tree if needed, run system version of this script, which will
# - - - install dependencies and run test
# - clean up any local changes (no need on GitHub Actions or VMs)

if [ -r "${MFFER_TEST_ENV:=$HOME/mffer_test_env.sh}" ]; then
	# shellcheck disable=SC1090 # The env script won't be included in shellcheck parsing
	. "$MFFER_TEST_ENV"
	MFFER_TEST_NESTED=y
fi

# Though this might be better defined in the base.sh script, we want to be able
# to debug as early as possible
VERBOSE="${VERBOSE:-y}" # Include brief progress updates and PASSED notices
DEBUG="${DEBUG:-}"      # Include trace, exit on errors, error on undefined variables
DEBUGOUT="${DEBUGOUT:-/dev/null}"
VERBOSEOUT="${VERBOSEOUT:-/dev/null}"
if [ -n "$DEBUG" ]; then
	set -x
	set -e
	set -u
	VERBOSE=y
	DEBUGOUT="/dev/stdout"
fi
if [ -n "$VERBOSE" ]; then
	VERBOSEOUT="/dev/stdout"
fi

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
	)" || exit 1
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

# Parameters used throughout the program. The program will try to determine
# them dynamically if not explicitly set in the environment
MFFER_REPO="${MFFER_REPO:-https://github.com/therealchjones/mffer}" # url
MFFER_TEST_COMMIT="${MFFER_TEST_COMMIT:-}"                          # which commit to test
MFFER_TEST_DIR="${MFFER_TEST_DIR:-}"                                # directory containing `test.sh` and the subdirectories of the testing tree
MFFER_TREE_ROOT="${MFFER_TREE_ROOT:-}"                              # Root directory of the local mffer repository

# Intentionally global parameters set within the program. These are dynamically
# configured and should not be user-configurable.
MFFER_TEST_BINDIR="" # where built binaries for the currrent test are
MFFER_TEST_SOURCE="" # where the local tree to test is
MFFER_TEST_TMPDIR="" # disposable temporary directory
MFFER_TEST_OS=""     # OS currently being tested

# Variables that should be available to child processes or job processes
MFFER_EXPORT_VARS='
	DEBUG
	DEBUGOUT
	VERBOSE
	VERBOSEOUT
	MFFER_BUILD_OS
	MFFER_EXPORT_VARS
	MFFER_TEST_DIR
	MFFER_TEST_NESTED
	MFFER_TEST_OS
	MFFER_TEST_BINDIR
	MFFER_TEST_SOURCE
	MFFER_TEST_TMPDIR
'

# In an effort to have some consistency, functions should be named in
# camelCase using the following conventions:
#
# isSomething, hasSomething, or somethingExists - returns 0 or 1, no output
# checkSomething - ensures consistency or appropriate setting, returns 0 or 1,
#                  no output
# createSomething - make changes to a persistent store of some kind, like the
#                   filesystem or Parallels Desktop VM registry, potentially
#                   setting the appropriate environment variable if not already;
#                   usually should be called by getSomething rather than
#                   directly
# setSomething - sets one or more environment variables, no output
# getSomething - prints value, potentially also determining that value and
#                setting the appropriate environment variable(s) if not already

MFFER_TEST_VM_SNAPSHOT="${MFFER_TEST_VM_SNAPSHOT:-Base Installation}" # Name of the "clean install" snapshot on the testing VM
MFFER_TEST_VM="${MFFER_TEST_VM:-}"                                    # Name of the VM on which to test
MFFER_TEST_VM_SYSTEM=""                                               # set by the appropriate script when virtual machine functions are loaded
if [ ! -r "$MFFER_TEST_DIR"/common/parallels.sh ] \
	|| ! . "$MFFER_TEST_DIR"/common/parallels.sh; then
	echo "Warning: Unable to load Parallels Desktop definitions" >&2
fi

# The new framework
export MFFER_TEST_FRAMEWORK="${MFFER_TREE_ROOT}/tools/testing/common/framework.sh"
# shellcheck disable=SC1090 # source a non-constant file
. "$MFFER_TEST_FRAMEWORK"

main() {
	getArgs "$@" || exitFromError
	foo || exit 1
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
			if [ -n "${MFFER_TEST_NESTED:=}" ]; then
				# this is not the master test script, everything is defined, just build
				sh "$MFFER_TEST_DIR/common/build-mffer.sh" || FAILS="$((FAILS + 1))"
				sh "$MFFER_TEST_DIR/common/build-docs.sh" || FAILS="$((FAILS + 1))"
				if [ "$FAILS" -gt 0 ]; then
					return 1
				else
					return 0
				fi
			fi

			echo "building release on the local system" >"$VERBOSEOUT"
			MFFER_BUILD_OS="local"
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
				if ! resetVm "$MFFER_TEST_VM" "$MFFER_TEST_VM_SNAPSHOT" \
					|| ! setBuildEnv "$1"; then
					echo "SKIPPED building release on $1" >"$VERBOSEOUT"
					return 1
				fi
				updateEnv
				runOnVm build-mffer \
					|| FAILS="$((FAILS + 1))"
				runOnVm build-docs \
					|| FAILS="$((FAILS + 1))"
				if [ "${FAILS:=0}" -gt 0 ]; then
					echo "FAILED building release on $1" >&2
					return 1
				fi
				if ! scp -qr "$MFFER_TEST_VM_HOSTNAME:tmpdir/built-on-$1" "$MFFER_TEST_TMPDIR"; then
					echo "Warning: Unable to obtain releases built on $1" >&2
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
# checkVm [ system | name ] [ snapshot ]
#
# ensures a VM running `system` or with the name `name` (default $MFFER_TEST_VM)
# exists and includes a base installation snapshot named `snapshot` (default
# $MFFER_TEST_VM_SNAPSHOT)
checkVm() {
	if [ -z "${MFFER_TEST_VM_SYSTEM:=}" ]; then
		echo "Error: No virtual machine management system is defined." >&2
		return 1
	fi
	system=''
	snapshot=''
	if [ "$#" -gt 2 ]; then
		echo "Exception: checkVm accepts at most 2 arguments" >&2
		return 1
	fi
	if [ "$#" = 0 ]; then
		if [ -z "${MFFER_TEST_VM:=}" ] || [ -z "${MFFER_TEST_VM_SNAPSHOT}" ]; then
			echo "Exception: checkVm without arguments requires MFFER_TEST_VM and MFFER_TEST_VM_SNAPSHOT to be nonempty" >&2
			return 1
		fi
		checkVm "${MFFER_TEST_VM:=}" "${MFFER_TEST_VM_SNAPSHOT:=}"
		return "$?"
	fi
	if [ "$#" = 1 ]; then
		if [ -z "${MFFER_TEST_VM:=}${MFFER_TEST_VM_SNAPSHOT:=}" ]; then
			echo "Exception: checkVm with 1 argument requires MFFER_TEST_VM or MFFER_TEST_VM_SNAPSHOT to be nonempty" >&2
			return 1
		fi
		if [ -n "${MFFER_TEST_VM}" ]; then
			checkVm "${MFFER_TEST_VM}" "$1"
			return "$?"
		else
			checkVm "$1" "$MFFER_TEST_VM_SNAPSHOT"
			return "$?"
		fi
	fi
	system="$1"
	snapshot="$2"
	vmname=''
	if ! vmname="$(getVm "$system")" \
		|| [ -z "$vmname" ] \
		|| ! hasSnapshot "$vmname" "$snapshot"; then
		echo "Error: No usable VM found for system '$1'" >&2
		return 1
	fi
	return 0
}
exitFromError() {
	echo "Testing is unable to continue due to errors." >&2
	exit 1
}
getArgs() {
	if [ -n "${MFFER_TEST_NESTED:=}" ]; then
		if [ -n "${MFFER_TEST_INCLUDE_LINUX:=}${MFFER_TEST_INCLUDE_MACOS:=}${MFFER_TEST_INCLUDE_WINDOWS:=}" ]; then
			echo "Warning: Performing system tests. Ignoring requested VM testing." >&2
		fi
		MFFER_TEST_INCLUDE_LOCAL=y
		return 0
	fi
	nonargs=''
	# If any are already defined from env, pretend they're included explicitly
	# This allows adding but not removing systems on the commandline
	if [ "${MFFER_TEST_INCLUDE_LOCAL:=}" ]; then nonargs="local"; fi
	if [ "${MFFER_TEST_INCLUDE_LINUX:=}" ]; then nonargs="$nonargs linux"; fi
	if [ "${MFFER_TEST_INCLUDE_MACOS:=}" ]; then nonargs="$nonargs macos"; fi
	if [ "${MFFER_TEST_INCLUDE_WINDOWS:=}" ]; then nonargs="$nonargs windows"; fi
	if [ "$#" = 0 ]; then
		if [ -z "$nonargs" ]; then
			nonargs=all
		fi
	fi
	for arg in "$@" $nonargs; do
		case "$arg" in
			'local') MFFER_TEST_INCLUDE_LOCAL=y ;;
			'linux')
				MFFER_TEST_INCLUDE_LINUX=y
				;;
			'macos')
				MFFER_TEST_INCLUDE_MACOS=y
				;;
			'windows')
				MFFER_TEST_INCLUDE_WINDOWS=y
				;;
			'all')
				MFFER_TEST_INCLUDE_LOCAL=y
				MFFER_TEST_INCLUDE_LINUX=y
				MFFER_TEST_INCLUDE_MACOS=y
				MFFER_TEST_INCLUDE_WINDOWS=y
				;;
			*)
				echo "Error: Invalid argument '$arg'" >&2
				usage >&2
				exitFromError
				;;
		esac
	done
	# either there were no arguments (so one was created in $nonargs), there
	# were no valid arguments (so whatever was there caused a usage error), or
	# there was one or more valid arguments (so it was processed).
	if [ -n "${MFFER_TEST_INCLUDE_LINUX:=}" ] && ! checkVm linux; then
		echo "Warning: Unable to test on linux VM; skipping" >&2
		MFFER_TEST_INCLUDE_LINUX=""
	fi
	if [ -n "${MFFER_TEST_INCLUDE_WINDOWS:=}" ] && ! checkVm windows; then
		echo "Warning: Unable to test on windows VM; skipping" >&2
		MFFER_TEST_INCLUDE_WINDOWS=""
	fi
	if [ -n "$MFFER_TEST_INCLUDE_MACOS" ] && ! checkVm macos; then
		echo "Warning: Unable to test on macos VM; skipping" >&2
		MFFER_TEST_INCLUDE_MACOS=""
	fi
	if [ -z "$MFFER_TEST_INCLUDE_LOCAL$MFFER_TEST_INCLUDE_LINUX$MFFER_TEST_INCLUDE_MACOS$MFFER_TEST_INCLUDE_WINDOWS" ]; then
		echo "Error: No systems to test." >&2
		return 1
	fi
	return 0
}
# getVm vmos
#
# Prints the name of an appropriate existing virtual machine, either named
# `vmos` or running the OS `vmos`. Prints an error and returns 1 if a usage
# error occurs. Returns 0 otherwise.
getVm() {
	if [ "$#" -ne 1 ] || [ -z "$1" ]; then
		echo "Error: getVm requires a single argument" >&2
		return 1
	fi
	if vmExists "$1"; then
		echo "$1"
		return 0
	fi
	system=''
	case "$1" in
		macos)
			system="macOS Testing"
			;;
		linux)
			system="Linux Testing"
			;;
		windows)
			system="Windows Testing"
			;;
		*)
			return 1
			;;
	esac
	if vmExists "$system"; then
		echo "$system"
		return 0
	fi
	return 1
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
				if ! scp -q "$MFFER_TEST_DIR/windows/disable-uac.bat" "$MFFER_TEST_VM_HOSTNAME": >"$DEBUGOUT" \
					|| ! ssh -q windows-testing cmd.exe /C disable-uac.bat >"$DEBUGOUT" \
					|| ! ssh -q windows-testing cmd.exe /C shutdown /s >"$DEBUGOUT" \
					|| ! waitForShutdown "$MFFER_TEST_VM" \
					|| ! startVm "$MFFER_TEST_VM" \
					|| ! waitForStartup "$MFFER_TEST_VM" \
					|| ! scp -q "$MFFER_TEST_DIR/windows/install-shell.bat" "$MFFER_TEST_VM_HOSTNAME": >"$DEBUGOUT" \
					|| ! ssh -q windows-testing cmd.exe /C install-shell.bat >"$DEBUGOUT"; then
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
	if ! script="$(getScript "$1")" || [ -z "$script" ]; then
		echo "Error: Unable to find script for '$1'" >&2
		return 1
	fi
	scp "$script" "$MFFER_TEST_VM_HOSTNAME:" >"$DEBUGOUT"
	basename="$(basename "$script")"
	if [ "$script" != "${script%.bat}" ]; then
		shell="cmd.exe"
		scriptfile="runscript.bat"
		# shellcheck disable=SC2087 # allow expansion of the below variables on the client side
		ssh -q "$MFFER_TEST_VM_HOSTNAME" "cmd.exe /C more > $scriptfile" <<-EOF
			${DEBUG:+@echo off}
			set DEBUGOUT=NUL
			${DEBUG:+set DEBUGOUT=CON}
			set VERBOSEOUT=NUL
			${VERBOSE:+set VERBOSEOUT=CON}
			$basename
		EOF
	else
		shell="sh"
		scriptfile="runscript.sh"
		# shellcheck disable=SC2087 # allow expansion of the below variables on the client side
		ssh -q "$MFFER_TEST_VM_HOSTNAME" "sh -c 'cat > $scriptfile'" <<-EOF
			#!/bin/sh

			export DEBUG="$DEBUG"
			export VERBOSE="$VERBOSE"
			if [ -n "$DEBUG" ]; then set -x; fi
			if [ ! -w "/dev/stdout" ]; then
				DEBUGOUT="/dev/null"
				VERBOSEOUT="/dev/null"
			else
				DEBUGOUT="${DEBUGOUT:-/dev/null}"
				VERBOSEOUT="${VERBOSEOUT:-/dev/null}"
			fi
			export DEBUGOUT VERBOSEOUT

			export MFFER_TEST_TMPDIR="tmpdir"
			export MFFER_TEST_SOURCE="mffer-source"
			export MFFER_TEST_BINDIR=mffer
			export MFFER_TEST_OS="$MFFER_TEST_OS"
			export MFFER_TEST_COMMIT="$MFFER_TEST_COMMIT"
			export MFFER_BUILD_OS="$MFFER_BUILD_OS"
			export PYTHON_VERSION="$PYTHON_VERSION"
			export PATH="$PATH:$HOME/.dotnet"
			# For MSYS2 (e.g., Git Bash), disable the "automatic path mangling".
			# See https://www.msys2.org/wiki/Porting/#filesystem-namespaces.
			export MSYS2_ARG_CONV_EXCL='*'
			sh $basename
		EOF
	fi
	# Git Bash (MSYS2) behaves strangely over Windows 10 OpenSSH. It requires
	# the ssh client to demand a pseudoterminal for /dev/stdin, /dev/stdout, and
	# /dev/stderr to work properly. Additionally, quoting is strange as OpenSSH
	# on Windows loses multiple layers of quotes when using a TTY; see
	# https://github.com/PowerShell/Win32-OpenSSH/issues/1082#issuecomment-435626493
	# and
	# https://github.com/bingbing8/openssh-portable/blob/latestw_all/contrib/win32/win32compat/w32-doexec.c.
	# For most OpenSSH servers given a "command string" cstring by an OpenSSH
	# client, the server passes cstring to the default shell as a string to be
	# interpreted, e.g.,
	# sh -c cstring
	# cstring is created from the command line of the OpenSSH client by taking
	# its final arguments and concatenating them with spaces into a single
	# string (which may contain spaces or other normally word-ending characters).
	# Specifically, assuming you're running an OpenSSH client from a POSIX-compatible shell,
	# ssh hostname command arg1 arg2
	# results in the shell expanding command, arg1, and arg2 via its usual
	# rules, the ssh client concatenating them with spaces and sending it all as
	# a single string to the OpenSSH server, and the OpenSSH server running that
	# single string argument to sh -c. Something similar happens with OpenSSH on
	# Windows if and only if a pseudoterminal is allocated. The single string
	# received by the server is used to build a new command with normal space
	# delineation, and without escaping any spaces in the single string. This
	# results in the equivalent of running
	# sh -c command arg1 arg2
	# instead of
	# sh -c "command arg1 arg2"
	# and these two are not equivalent. The first is (roughly) equivalent to running
	# sh -c "command arg2"
	# and setting the name of the process ($0) to arg1

	# Other systems, including Windows when not specifically requesting a TTY,
	# will take ssh -q hostname command arg1 arg2 to be the same as ssh -q
	# hostname command arg1 arg2 which is run on the server as shell optionarg
	# "command arg1 arg2" However, Windows with a TTY runs it as shell optionarg
	# command arg1 arg2 which, in compliance with POSIX, means run command with
	# $0 set to arg1 and $1 set to arg2 When double-quoting on other systems,
	# ssh -q hostname '"command arg1 arg2"'

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
# setVm vmos
#
# Sets the MFFER_TEST_VM variable to the appropriate name for OS `vmos` and
# confirms it exists and has a snapshot named $MFFER_TEST_VM_SNAPSHOT (or "Base
# Installation" if that value is empty). Sets other variables dependent upon
# these, such as MFFER_TEST_VM_HOSTNAME. Prints an error and returns 1 if no VM
# with the appropriate name exists or it doesn't have a snapshot named
# appropriately. Prints an error and returns 255 if a usage error occurs.
# Returns 0 otherwise.
setVm() {
	if [ "$#" -ne 1 ] || [ -z "$1" ]; then
		echo "Error: setVm() requires a single argument" >&2
		return 255
	fi
	case "$1" in
		macos)
			MFFER_TEST_VM="macOS Testing"
			;;
		linux)
			MFFER_TEST_VM="Linux Testing"
			;;
		windows)
			MFFER_TEST_VM="Windows Testing"
			;;
		*)
			echo "Error: unknown argument to setVm() '$1'" >&2
			return 1
			;;
	esac
	MFFER_TEST_OS="$1"
	MFFER_TEST_VM_SNAPSHOT="${MFFER_TEST_VM_SNAPSHOT:-Base Installation}"
	MFFER_TEST_VM_HOSTNAME="$(getVmHostname "$MFFER_TEST_VM")"
	if ! checkVm; then
		echo "Error: Unable to use virtual machine '$1'" >&2
		return 1
	fi
	updateEnv
}
# sshIsRunning
#
# Returns 0 if able to connect to MFFER_TEST_VM_HOSTNAME on
# port 22, nonzero otherwise
sshIsRunning() {
	if ! command -v nc >"$DEBUGOUT" 2>&1; then
		echo "Error: 'nc' command not found" >&2
		return 1
	fi
	nc -z "$MFFER_TEST_VM_HOSTNAME" 22 >"$DEBUGOUT" 2>&1
}
# sshWithDebugging
#
# Adds the -q flag if DEBUG is empty or null, the -v flag otherwise.
sshWithDebugging() {
	if [ -n "$DEBUG" ]; then
		ssh -v "$@"
	else
		ssh -q "$@"
	fi
}
testOn() {
	if [ "$#" -ne 1 ]; then
		echo "Error: testOn requires a single argument" >&2
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
				echo "testing on $1 using VM '$MFFER_TEST_VM'" >"$VERBOSEOUT"
				MFFER_TEST_OS="$1"
				passed=''
				for release in linux macos windows; do
					releasefails=0
					MFFER_BUILD_OS="$release"
					updateEnv
					if ! [ -d "$MFFER_TEST_BINDIR" ]; then
						echo "Warning: unable to find $1 release built on $release" >&2
						echo "SKIPPED testing $1' release built on $release" >"$VERBOSEOUT"
						continue
					fi
					echo "testing release built on $release" >"$VERBOSEOUT"
					if ! resetVm "$MFFER_TEST_VM" "$MFFER_TEST_VM_SNAPSHOT"; then
						echo "Error: could not continue test" >&2
						releasefails="$((releasefails + 1))"
						continue
					fi
					scp -qr "$MFFER_TEST_BINDIR" "$MFFER_TEST_VM_HOSTNAME":mffer/ >"$VERBOSEOUT"
					runOnVm test-mffer || releasefails="$((releasefails + 1))"
					runOnVm test-apkdl || releasefails="$((releasefails + 1))"
					runOnVm test-autoanalyze || releasefails="$((releasefails + 1))"
					if [ "${releasefails:=0}" -gt 0 ]; then
						FAILS="$((FAILS + 1))"
						echo "FAILED testing $1 release built on $release" >&2
						continue
					fi
					passed=y
					echo "PASSED testing $1 release built on $release" >"$VERBOSEOUT"
				done
				if [ "${FAILS:=0}" -gt 0 ]; then
					echo "FAILED testing on $1" >&2
					return 1
				fi
				if [ -z "${passed:-}" ]; then
					echo "SKIPPED testing on $1" >&2
					return 0
				fi
				echo "PASSED testing on $1" >"$VERBOSEOUT"
				return 0
			fi
			;;
		*)
			echo "Error: '$1' is not a valid argument to testOn()" >&2
			return 1
			;;
	esac
}
waitForShutdown() {
	starttime="$(getTime)"
	maxtime="$((10 * 60))" # 10 minutes, in seconds
	until ! vmIsRunning "$1"; do
		time="$(getTime)"
		if [ -z "$starttime" ] || [ -z "$time" ]; then
			echo "Error: Unable to get the waiting time" >&2
			return 1
		fi
		if [ "$((time - starttime))" -ge "$maxtime" ]; then
			echo "Error: Timed out; VM never shut down" >&2
			return 1
		fi
		sleep 5
	done
}
waitForStartup() {
	starttime="$(getTime)"
	maxtime="$((10 * 60))" # 10 minutes, in seconds
	until sshIsRunning "$1"; do
		time="$(getTime)"
		if [ -z "$starttime" ] || [ -z "$time" ]; then
			echo "Error: Unable to get the installation time" >&2
			return 1
		fi
		if [ "$((time - starttime))" -ge "$maxtime" ]; then
			echo "Error: Timed out; VM never made SSH accessible" >&2
			return 1
		fi
		sleep 5
	done
}

main "$@"
