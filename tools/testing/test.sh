#!/bin/sh

set -e
set -u

# Run mffer build & function tests. This script is expected to be run from
# within the mffer development environment (see the mffer Development Guide for
# details).

usage() {
	echo "Usage: sh $0 [ local | linux | macos | windows | all ]"
}

# A few environment variables are used, if present:
# MFFER_TREE_ROOT: root directory of the mffer repository
# MFFER_TEST_INCLUDE_LOCAL
# MFFER_TEST_INCLUDE_LINUX
# MFFER_TEST_INCLUDE_MACOS
# MFFER_TEST_INCLUDE_WINDOWS:
# Setting any of these to a nonempty value results in the same behavior as
# including the related arguments on the command line
# MFFER_TEST_VM_SNAPSHOT: name of the "base" snapshot to which VMs should be reset

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
if [ -r "$MFFER_TREE_ROOT"/tools/testing/common/framework.sh ]; then
	export MFFER_TEST_FRAMEWORK="$MFFER_TREE_ROOT/tools/testing/common/framework.sh"
else
	echo "Error: Unable to locate testing framework" >&2
fi
# shellcheck source=./common/framework.sh
if ! . "$MFFER_TEST_FRAMEWORK"; then
	echo "Error: Unable to load testing framework" >&2
	exit 1
fi

MFFER_TEST_VM_SNAPSHOT="${MFFER_TEST_VM_SNAPSHOT:-Base Installation}" # Name of the "clean install" snapshot on the testing VM
if [ ! -r "$(getTestDir)"/common/parallels.sh ] \
	|| ! . "$(getTestDir)"/common/parallels.sh; then
	echo "Warning: Unable to load Parallels Desktop definitions" >&2
fi

main() {
	getArgs "$@" || return 1
	trap cleanup EXIT
	setTmpdir
	if [ -n "$MFFER_TEST_INCLUDE_LOCAL" ]; then
		echo "building on local system"
		if ! runBuild 'local'; then
			echo "FAILED building on local system"
			return 1
		else
			echo "PASSED building on local system"
		fi
		echo "testing on local system"
		if ! runTest 'local'; then
			echo "FAILED testing on local system"
			return 1
		else
			echo "PASSED testing on local system"
		fi
	fi
	if [ -n "$MFFER_TEST_INCLUDE_LINUX" ]; then
		buildOn linux || true
	fi
	if [ -n "$MFFER_TEST_INCLUDE_MACOS" ]; then
		buildOn macos || true
	fi
	if [ -n "$MFFER_TEST_INCLUDE_WINDOWS" ]; then
		buildOn windows || true
	fi
	if [ -n "$MFFER_TEST_INCLUDE_LINUX" ]; then
		testOn linux || true
	fi
	if [ -n "$MFFER_TEST_INCLUDE_MACOS" ]; then
		testOn macos || true
	fi
	if [ -n "$MFFER_TEST_INCLUDE_WINDOWS" ]; then
		testOn windows || true
	fi
}
# buildOn ( macos | linux | windows )
#
#
buildOn() (
	if [ "$#" -ne 1 ]; then
		echo "Error: buildOn() requires a single argument" >&2
		return 1
	fi
	case "$1" in
		macos | linux | windows)
			echo "building release on $1 using VM '$(getVm "$1")'"
			if ! resetVm "$(getVm "$1")" "${MFFER_TEST_VM_SNAPSHOT:-}" \
				|| ! setBuildEnv "$(getVm "$1")"; then
				echo "SKIPPED building release on $1"
				return 1
			fi
			if ! remoteBuild "$(getVmHostname "$1")" 'local'; then
				echo "FAILED building release on $1" >&2
				return 1
			fi
			if ! scp -qr "$(getVmHostname "$1"):mffer/release" "$MFFER_TEST_TMPDIR/built-on-$1"; then
				echo "Warning: Unable to obtain releases built on $1" >&2
			fi
			echo "PASSED building release on $1"
			return 0
			;;
		*)
			echo "Error: Unknown system '$1'; unable to build" >&2
			return 1
			;;
	esac
)
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
# cleanup
#
# function to be run just before exiting the script (that is, the one sourcing this one)
cleanup() (
	exitstatus="$?"
	trap - EXIT
	echo "Cleaning up"
	if [ -n "${MFFER_TEST_TMPDIR:-}" ]; then
		if ! rm -rf "$MFFER_TEST_TMPDIR" \
			&& ! { chmod -R u+w "$MFFER_TEST_TMPDIR" && rm -rf "$MFFER_TEST_TMPDIR"; }; then
			echo "Error: Unable to delete temporary directory '$MFFER_TEST_TMPDIR'" >&2
			if [ "$exitstatus" -eq 0 ]; then exitstatus=1; fi
		fi
	fi
	exit "$exitstatus"
)
getArgs() {
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
				return 1
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
installOnVm() (
	if ! hostname="$(getVmHostname "${1:-}")"; then
		echo "Error: Unable to get hostname for VM '${1:-}'" >&2
		return 1
	fi
	os="$(getVmOs "$1")"
	hostname="$(getVmHostname "$1")"
	case "${2:-}" in
		shell)
			if [ "$os" = "windows" ]; then
				if ! scp -q "$MFFER_TREE_ROOT/tools/testing/windows/disable-uac.bat" "$hostname": \
					|| ! ssh -q "$hostname" cmd.exe /C disable-uac.bat \
					|| ! ssh -q "$hostname" cmd.exe /C shutdown /s \
					|| ! waitForShutdown "$(getVm "$1")" \
					|| ! startVm "$(getVm "$1")" \
					|| ! waitForStartup "$(getVm "$1")" \
					|| ! scp -q "$MFFER_TREE_ROOT/tools/testing/windows/install-shell.bat" "$hostname": \
					|| ! ssh -q "$hostname" cmd.exe /C install-shell.bat; then
					echo "Error: Unable to install shell on Windows" >&2
					return 1
				fi
			fi
			;;
		dotnet)
			if ! runOnVm "$hostname" install-dotnet; then
				echo "Error: Unable to install .NET SDK" >&2
				return 1
			fi
			;;
		node)
			if ! runOnVm "$hostname" install-node; then
				echo "Error: unable to install Node.js" >&2
				return 1
			fi
			;;
		git)
			if [ "$os" = "macos" ]; then
				runOnVm "$hostname" install-commandlinetools || return 1
			elif [ "$os" = "linux" ]; then
				runOnVm "$hostname" install-git || return 1
			fi
			true
			;;
		python)
			if ! runOnVm "$hostname" install-python; then
				echo "Error: unable to install Python" >&2
				return 1
			fi
			true
			;;
		doxygen)
			if ! runOnVm "$hostname" install-doxygen; then
				echo "Error: unable to install Doxygen" >&2
				return 1
			fi
			true
			;;
		java)
			if [ "$os" = "macos" ]; then
				runOnVm "$hostname" install-temurin || return 1
			fi
			;;
		ghidra)
			if ! runOnVm "$hostname" install-ghidra; then
				echo "Error: unable to install Ghidra" >&2
				return 1
			fi
			;;
		*)
			echo "Error: No recipe to install '$2' on virtual machine '$1'" >&2
			return 1
			;;
	esac
)
# isRoot
#
# Returns 0 if the effective user id is 0, i.e., the root user; returns 1 otherwise
isRoot() {
	[ 0 = "$(id -u)" ]
}
# isSudo
#
# Evaluates whether the effective user is root and sudo user is not root
# (regular). Returns 0 if the real (sudo) user is a regular user and the
# effective user is root, otherwise returns 1.
isSudo() {
	if ! isRoot || [ -z "$SUDO_UID" ] || [ "$SUDO_UID" = "0" ]; then
		return 1
	else
		return 0
	fi
}
runOnVm() (
	# TODO: this finds the wrong script if VM and host have different OS
	# Can probably fix by having a wrapper script we run from common/ like
	# `install-build-environment`.
	script=""
	if ! script="$(getScript "${2:-}")" || [ -z "$script" ]; then
		echo "Error: Unable to find script for '$1'" >&2
		return 1
	fi
	vmscript="mffer/${script#"$MFFER_TREE_ROOT"}"
	if [ "$script" != "${script%.bat}" ]; then
		shell="cmd.exe"
		scriptfile="$vmscript"
	else
		shell="sh"
		scriptfile="runscript.sh"
		# shellcheck disable=SC2087 # allow expansion of the below variables on the client side
		ssh -q "$(getVmHostname "$1")" "sh -c 'cat > $scriptfile'" <<-EOF
			#!/bin/sh

			export PATH="$PATH:$HOME/.dotnet"
			# For MSYS2 (e.g., Git Bash), disable the "automatic path mangling".
			# See https://www.msys2.org/wiki/Porting/#filesystem-namespaces.
			export MSYS2_ARG_CONV_EXCL='*'
			sh "$vmscript"
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

	ssh -q "$(getVmHostname "${1:-}")" "$shell $scriptfile"
)
setBuildEnv() (
	hostname="$(getVmHostname "${1:-}")"
	if ! dotnet clean "$MFFER_TREE_ROOT" \
		|| ! tar -cf "$MFFER_TEST_TMPDIR"/mffer-tree.tar -C "$MFFER_TREE_ROOT" . \
		|| ! scp -q "$MFFER_TEST_TMPDIR/mffer-tree.tar" "$hostname": >"$DEBUGOUT" \
		|| ! ssh -q "$hostname" mkdir -p mffer \
		|| ! ssh -q "$hostname" tar -xf mffer-tree.tar -m -C mffer; then
		echo "Error: Unable to copy source code to VM '$hostname'" >&2
		return 1
	fi
	installOnVm "$1" buildenv || return 1
)
# setTmpdir
#
# If MFFER_TEST_TMPDIR doesn't already point to an existing directory, create a
# temporary directory and set MFFER_TEST_TMPDIR to its name
setTmpdir() {
	if [ -n "${MFFER_TEST_TMPDIR:=}" ]; then
		if [ ! -d "$MFFER_TEST_TMPDIR" ] \
			|| ! output="$(ls "$MFFER_TEST_TMPDIR")"; then
			echo "$output"
			echo "Error: 'MFFER_TEST_TMPDIR' is set to '$MFFER_TEST_TMPDIR'," >&2
			echo "       but that isn't working." >&2
			return 1
		fi
		return 0
	fi
	cmd="mktemp -d -t mffer-test"
	if isSudo; then cmd="sudo -u $SUDO_USER $cmd"; fi
	if ! MFFER_TEST_TMPDIR="$($cmd)" \
		|| [ -z "$MFFER_TEST_TMPDIR" ]; then
		echo "Error: Unable to create temporary directory" >&2
		return 1
	fi
	return 0
}
# sshIsRunning
#
# Returns 0 if able to connect to MFFER_TEST_VM_HOSTNAME on
# port 22, nonzero otherwise
sshIsRunning() {
	if ! command -v nc; then
		echo "Error: 'nc' command not found" >&2
		return 1
	fi
	nc -z "$MFFER_TEST_VM_HOSTNAME" 22 2>&1
}
# sshWithDebugging
#
# Adds the -q flag if DEBUG is empty or null, the -v flag otherwise.
sshWithDebugging() {
	if [ -n "${DEBUG:-}" ]; then
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
	case "$1" in
		local)
			echo "Testing on the local system" >"$VERBOSEOUT"
			MFFER_BUILD_OS=""
			setBuildOs
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

main "$@" || exit 1
