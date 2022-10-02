#!/bin/sh

. ../common/testfxns.sh

MFFER_TEST_VM="macOS Testing"
MKMACVM_VERSION=0.3.2

main() {
	trap cleanup EXIT
	if ! createVm; then
		echo "Error: Unable to create virtual machine '$MFFER_TEST_VM'" >&2
		exit 1
	fi
}
cleanup() {
	exitcode="$?"
	exit "$exitcode"
}
createVm() {
	echo "Creating virtual machine '${MFFER_TEST_VM:=}'" >"$VERBOSEOUT"
	setTmpdir || return 1
	if ! curl -sS -L -o "${MFFER_TEST_TMPDIR:=}"/mkmacvm-"${MKMACVM_VERSION:=}".tar.gz \
		https://github.com/therealchjones/mkmacvm/archive/v"$MKMACVM_VERSION".tar.gz \
		|| ! tar -xf "$MFFER_TEST_TMPDIR"/mkmacvm-"$MKMACVM_VERSION".tar.gz -C "$MFFER_TEST_TMPDIR"; then
		echo "Error: Unable to get mkmacvm" >"$MFFER_TEST_TMPDIR"
		return 1
	fi
	if ! isRoot; then
		echo "Warning: Creating the virtual machine requires admin access; using sudo." >&2
	fi
	if ! sudo VERBOSE="${VERBOSE}" \
		"$MFFER_TEST_TMPDIR"/mkmacvm-"$MKMACVM_VERSION"/mkmacvm; then
		echo "Error: Unable to build virtual machine '$MFFER_TEST_VM'" >&2
		return 1
	fi
	if ! setParallels \
		|| ! "$PRLCTL" set macvm --name "$MFFER_TEST_VM" \
		|| ! "$PRLCTL" set "$MFFER_TEST_VM" --startup-view headless >"$DEBUGOUT" \
		|| ! startVm \
		|| ! setPasswordlessSudo \
		|| ! "$PRLCTL" snapshot "$MFFER_TEST_VM" -n "$MFFER_TEST_SNAPSHOT" >"$DEBUGOUT"; then
		echo "Error: Unable to configure virtual machine '${MFFER_TEST_VM}'"
		return 1
	fi
}
main
