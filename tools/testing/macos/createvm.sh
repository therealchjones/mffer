#!/bin/sh

# Test mffer build and operation on a macOS virtual machine
# Options are set via environment variables; there are no command-line
# flags or switches

# This script can only be run by a process that sets "$0" to the real (relative)
# path of the script. This limits, for instance, the ability to run via stdin or
# (theoretically) some shell interpreters. See also
# https://mywiki.wooledge.org/BashFAQ/028
SCRIPT_FILE=""
if [ -n "${0:-}" ]; then
	if [ -z "${0%%/*}" ]; then # $0 is the full path
		SCRIPT_FILE="$0"
	elif [ -n "${PWD:-}" ]; then # $0 is a relative path
		SCRIPT_FILE="$PWD/$0"
	fi
fi
if [ -z "$SCRIPT_FILE" ] || [ ! -f "$SCRIPT_FILE" ]; then
	echo "Error: Unable to determine script location" >&2
	echo "\$0: $0" >&2
	echo "\$PWD: $PWD" >&2
	echo "\$SCRIPT_FILE: $SCRIPT_FILE" >&2
	exit 1
fi
if ! SCRIPT_DIR="$(dirname "$SCRIPT_FILE")" \
	|| ! MFFER_TEST_DIR="$SCRIPT_DIR"/.. \
	|| ! MFFER_TREE_ROOT="$MFFER_TEST_DIR/../.." \
	|| [ ! -d "$MFFER_TREE_ROOT" ] \
	|| [ ! -d "$MFFER_TEST_DIR" ] \
	|| [ ! -f "$MFFER_TEST_DIR"/common/base.sh ]; then
	echo "Error: mffer source tree has unknown structure" >&2
	exit 1
fi
. "$SCRIPT_DIR/../common/base.sh"

if [ -z "${MFFER_TEST_VM_SYSTEM:-}" ]; then
	if ! . "$SCRIPT_DIR/../common/parallels.sh"; then
		echo "Error: Unable to load Parallels Desktop script definitions" >&2
		exit 1
	fi
fi

set -e
set -u

if [ 0 != "$#" ]; then
	echo "$(basename "$0") does not accept arguments." >&2
	echo "Usage: sh '$0'" >&2
	exit 1
fi

MFFER_TEST_VM="macOS Testing"
MKMACVM_VERSION=0.3.2

if vmExists "${MFFER_TEST_VM:=}"; then
	echo "virtual machine '$MFFER_TEST_VM' already exists; consider removing" >&2
	exit 1
fi
echo "Creating virtual machine '${MFFER_TEST_VM:=}'" >"$VERBOSEOUT"
setTmpdir || exit 1
if ! curl -sS -L -o "${MFFER_TEST_TMPDIR:=}"/mkmacvm-"${MKMACVM_VERSION:=}".tar.gz \
	https://github.com/therealchjones/mkmacvm/archive/v"$MKMACVM_VERSION".tar.gz \
	|| ! tar -xzf "$MFFER_TEST_TMPDIR"/mkmacvm-"$MKMACVM_VERSION".tar.gz -C "$MFFER_TEST_TMPDIR" \
	|| ! chown -R "${SUDO_UID:=}" "$MFFER_TEST_TMPDIR/mkmacvm-$MKMACVM_VERSION"; then
	echo "Error: Unable to download mkmacvm" >&2
	exit 1
fi
if ! isSudo; then
	echo "Warning: mkmacvm requires sudo. Password may be required." >&2
fi
if ! sudo DEBUG="${DEBUG:-}" VERBOSE="${VERBOSE:-}" "$MFFER_TEST_TMPDIR"/mkmacvm-"$MKMACVM_VERSION"/mkmacvm; then
	echo "Error: Unable to build virtual machine '$MFFER_TEST_VM'" >&2
	exit 1
fi
if ! renameVm macvm "$MFFER_TEST_VM" >"$DEBUGOUT" \
	|| ! startVm "$MFFER_TEST_VM" \
	|| ! MFFER_TEST_VM_HOSTNAME="$(getVmHostname "$MFFER_TEST_VM")" \
	|| ! setPasswordlessSudo \
	|| ! saveSnapshot "$MFFER_TEST_VM" "$MFFER_TEST_SNAPSHOT" >"$DEBUGOUT"; then
	echo "Error: Unable to configure virtual machine '${MFFER_TEST_VM}'"
	exit 1
fi
