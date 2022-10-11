#!/bin/sh

# Test mffer build and operation on a Windows virtual machine

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
	. "$SCRIPT_DIR/../common/parallels.sh"
fi

set -e
set -u

if [ 0 != "$#" ]; then
	echo "$(basename "$0") does not accept arguments." >&2
	echo "Usage: sh '$0'" >&2
	exit 1
fi

MFFER_TEST_VM="${MFFER_TEST_VM:-Windows Testing}"
SSH_IDENTITY="${SSH_IDENTITY:-$HOME/.ssh/id_ecdsa}"
WINDOWS_INSTALLER="${WINDOWS_INSTALLER:-}"
# We use an unofficial URL so we don't need to fill out a form for the download
WINDOWS_INSTALLER_URL="${WINDOWS_INSTALLER_URL:-https://www.itechtics.com/?dl_id=151}"
# The checksum is intentionally hardcoded to the value given on the official
# download site at
# https://www.microsoft.com/en-us/software-download/windows10ISO under the
# heading "Verify your download"
WINDOWS_INSTALLER_CKSUM="7f6538f0eb33c30f0a5cbbf2f39973d4c8dea0d64f69bd18e406012f17a8234f"

PROGRAMDIR="$(dirname "$0")"
USERNAME="$(id -un)"

MFFER_TEST_VM_HOSTNAME="$(getVmHostname "$MFFER_TEST_VM")"

if vmExists "${MFFER_TEST_VM:=}"; then
	echo "virtual machine '$MFFER_TEST_VM' already exists; consider removing" >&2
	exit 1
fi
echo "Creating virtual machine '${MFFER_TEST_VM:=}'" >"$VERBOSEOUT"

main() {
	setTmpdir || exit 1
	getWindowsVirtualMachine || exit 1

}
createWindowsVirtualMachine() { # builds a new windows VM, errors if name exists
	setTmpdir || return 1
	getWindowsInstaller || return 1
	WINDOWS_SETUP_DIR="$MFFER_TEST_TMPDIR/WindowsSetup"
	WINDOWS_SETUP_IMG="$MFFER_TEST_TMPDIR/WindowsSetup.iso"
	if ! mkdir -p "$WINDOWS_SETUP_DIR"; then
		echo "Error: Unable to prepare for Windows installation" >&2
		return 1
	fi
	for file in Autounattend.xml WinSetup.ps1; do
		if [ -r "$PROGRAMDIR/$file" ]; then
			echo "Warning: Using separate $file from $PROGRAMDIR/$file" >&2
			cp "$PROGRAMDIR/Autounattend.xml" "$WINDOWS_SETUP_DIR/Autounattend.xml" || return 1
		fi
	done
	if [ ! -r "$WINDOWS_SETUP_DIR/Autounattend.xml" ]; then
		printAutounattend >"$WINDOWS_SETUP_DIR/Autounattend.xml" || return 1
	fi
	if [ ! -r "$WINDOWS_SETUP_DIR/WinSetup.ps1" ]; then
		printWinSetup >"$WINDOWS_SETUP_DIR/WinSetup.ps1" || return 1
	fi
	if [ ! -f "$SSH_IDENTITY" ]; then
		if [ -f "$SSH_IDENTITY".pub ]; then
			echo "Error: '$SSH_IDENTITY' not found but '$SSH_IDENTITY.pub' exists." >&2
			echo '       Stopping before we break something.' >&2
			return 1
		fi
		echo "Warning: '$SSH_IDENTITY' not found. Creating new key." >&2
		if ! mkdir -p "$(dirname "$SSH_IDENTITY")" \
			|| ! ssh-keygen -q -f "$SSH_IDENTITY" -N ""; then
			echo "Error: Unable to create new key '$SSH_IDENTITY'." >&2
		fi
	fi
	if [ ! -f "$SSH_IDENTITY".pub ]; then
		echo "Warning: '$SSH_IDENTITY' exists but '$SSH_IDENTITY.pub' does not." >&2
		echo "         Regenerating public key." >&2
		if ! ssh-keygen -e "$SSH_IDENTITY" >"$SSH_IDENTITY.pub"; then
			echo "Error: Unable to regenerate '$SSH_IDENTITY.pub'" >&2
			return 1
		fi
	fi
	if ! cp "$SSH_IDENTITY.pub" "$WINDOWS_SETUP_DIR/administrators_authorized_keys"; then
		echo "Error: Unable to prepare VM for SSH communication" >&2
		return 1
	fi
	if ! makehybrid_output="$(hdiutil makehybrid -o "$WINDOWS_SETUP_IMG" "$WINDOWS_SETUP_DIR" >"$DEBUGOUT" 2>&1)"; then
		if [ -n "$makehybrid_output" ]; then echo "$makehybrid_output" >&2; fi
		echo "Error: Unable to configure Windows installation" >&2
		return 1
	fi
	if ! "$PRLCTL" create "$MFFER_TEST_VM" -d win-10 >"$DEBUGOUT" \
		|| ! "$PRLCTL" set "$MFFER_TEST_VM" --isolate-vm on >"$DEBUGOUT"; then
		echo "Error: Unable to create virtual machine '$MFFER_TEST_VM'" >&2
		return 1
	fi
	if ! "$PRLCTL" set "$MFFER_TEST_VM" --device-set "cdrom0" \
		--image "$WINDOWS_INSTALLER" --connect >"$DEBUGOUT" \
		|| ! "$PRLCTL" set "$MFFER_TEST_VM" --device-add cdrom \
			--image "$WINDOWS_SETUP_IMG" --connect >"$DEBUGOUT" \
		|| ! "$PRLCTL" set "$MFFER_TEST_VM" \
			--device-bootorder 'hdd0 cdrom0' >"$DEBUGOUT"; then
		echo "Error: Unable to configure new virtual machine '$MFFER_TEST_VM'" >&2
		return 1
	fi
	echo "Installing Windows on new virtual machine" >"$VERBOSEOUT"
	if ! "$PRLCTL" start "$MFFER_TEST_VM" >"$DEBUGOUT"; then
		echo "Error: Unable to install Windows" >&2
		return 1
	fi
	echo 'Waiting for installation to finish' >"$VERBOSEOUT"
	waitForInstallation
	echo "Removing old SSH authorization keys" >"$VERBOSEOUT"
	ssh-keygen -R "$MFFER_TEST_VM_HOSTNAME" >"$DEBUGOUT" 2>&1 || true
	ssh-keygen -R "$MFFER_TEST_VM_HOSTNAME".shared >"$DEBUGOUT" 2>&1 || true
	echo "Setting up new SSH authorization" >"$VERBOSEOUT"
	if ! ssh -o StrictHostKeyChecking=no -i "$SSH_IDENTITY" "$USERNAME"@"$MFFER_TEST_VM_HOSTNAME" \
		shutdown /s >"$VERBOSEOUT"; then
		echo "Error: Unable to connect to VM via SSH" >&2
		return 1
	fi
	# Need to remove installation
	# media & 2nd cdrom, restart, then save snapshot; maybe download latest
	# cumulative update package and include in offlineservice component of the
	# unattend file
	echo "completing configuration of VM '$MFFER_TEST_VM'" >"$VERBOSEOUT"
	if ! waitForShutdown \
		|| ! prlctl set "$MFFER_TEST_VM" --device-set cdrom0 --image '' >"$DEBUGOUT" \
		|| ! prlctl set "$MFFER_TEST_VM" --device-del cdrom1 >"$DEBUGOUT" \
		|| ! prlctl set "$MFFER_TEST_VM" --device-bootorder 'hdd0 cdrom0' >"$DEBUGOUT" \
		|| ! prlctl start "$MFFER_TEST_VM" >"$DEBUGOUT" \
		|| ! waitForStartup; then
		echo "Error: Unable to complete configuration of VM '$MFFER_TEST_VM'" >&2
		return 1
	fi
	echo "Saving VM snapshot '$MFFER_TEST_SNAPSHOT'" >"$VERBOSEOUT"
	if ! prlctl snapshot "$MFFER_TEST_VM" -n "$MFFER_TEST_SNAPSHOT" \
		-d "Initial installation without additional software. User $USERNAME, no password. Public key SSH enabled." \
		>"$DEBUGOUT"; then
		echo "Error: Unable to save VM snapshot '$MFFER_TEST_SNAPSHOT'" >&2
		return 1
	fi
}
getParallels() { # sets PRLCTL if not already
	if [ -n "$PRLCTL" ]; then
		if ! "$PRLCTL" --version >"$DEBUGOUT" 2>&1; then
			echo "Error: PRLCTL is set to '$PRLCTL', which doesn't work" >&2
			return 1
		fi
		return 0
	fi
	for file in prlctl /usr/local/bin/prlctl "/Applications/Parallels Desktop.app/Contents/MacOS/prlctl"; do
		if "$file" --version >"$DEBUGOUT" 2>&1; then
			PRLCTL="$file"
			return 0
		fi
	done
	echo "Error: Unable to find 'prlctl'." >&2
	echo "       Ensure Parallels Desktop is installed and activated." >&2
	return 1
}
getVMBaseSnapshotId() { # sets MFFER_TEST_SNAPSHOT_ID if not already
	getParallels || return 1
	if [ -n "$MFFER_TEST_SNAPSHOT_ID" ]; then
		if [ -z "$("$PRLCTL" snapshot-list "$MFFER_TEST_VM" -i "$MFFER_TEST_SNAPSHOT_ID")" ]; then
			echo "Error: 'MFFER_TEST_SNAPSHOT_ID' is set to '$MFFER_TEST_SNAPSHOT_ID'," >&2
			echo "       which isn't working." >&2
			return 1
		fi
		return 0
	fi
	if ! "$PRLCTL" list "$MFFER_TEST_VM" >"$DEBUGOUT" 2>&1; then
		echo "Error: Unable to find virtual machine '$MFFER_TEST_VM'" >&2
		return 1
	fi
	if ! SNAPSHOTLIST="$(prlctl snapshot-list "$MFFER_TEST_VM" -j)" \
		|| ! SNAPSHOTS="$(
			plutil -create xml1 - -o - \
				| plutil -insert snapshots \
					-json "$SNAPSHOTLIST" - -o -
		)" \
		|| [ -z "$SNAPSHOTS" ]; then
		echo 'Error: Unable to obtain list of virtual machine snapshots.' >&2
		echo "       The virtual machine '$MFFER_TEST_VM'" >&2
		echo "       may be invalid; consider deleting it." >&2
		return 1
	fi
	MFFER_TEST_SNAPSHOT_ID="$(
		echo "$SNAPSHOTS" \
			| plutil -extract snapshots raw - -o - \
			| while read -r snapshotid; do
				if snapshots="$(echo "$SNAPSHOTS" | plutil -extract snapshots xml1 - -o -)" \
					&& snapshot="$(echo "$snapshots" | plutil -extract "$snapshotid" xml1 - -o -)" \
					&& snapshotname="$(echo "$snapshot" | plutil -extract name raw - -o -)" \
					&& [ "$snapshotname" = "$MFFER_TEST_SNAPSHOT" ]; then
					snapshotid="${snapshotid#\{}"
					snapshotid="${snapshotid%\}}"
					echo "$snapshotid"
					return 0
				fi
			done
	)"
	if [ -z "$MFFER_TEST_SNAPSHOT_ID" ]; then
		echo "Error: virtual machine '$MFFER_TEST_VM'" >&2
		echo "       does not include snapshot '$MFFER_TEST_SNAPSHOT'" >&2
		echo "       Consider deleting this VM; we can rebuild it." >&2
		return 1
	fi
	return 0
}
getWindowsInstaller() { # downloads and sets WINDOWS_INSTALLER if not already
	setTmpdir || return 1
	if [ -z "$WINDOWS_INSTALLER" ]; then
		WINDOWS_INSTALLER="$MFFER_TEST_TMPDIR/Win10_21H2_English_x64.iso"
	fi
	if [ ! -f "$WINDOWS_INSTALLER" ]; then
		echo "Downloading Windows installation image" >"$VERBOSEOUT"
		if ! curl -LSs -o "$WINDOWS_INSTALLER" "$WINDOWS_INSTALLER_URL"; then
			echo "Error: Unable to download Windows installation image" >&2
			return 1
		fi
	fi
	echo "Checking Windows installation image" >"$VERBOSEOUT"
	if ! {
		echo "$WINDOWS_INSTALLER_CKSUM"' *'"$WINDOWS_INSTALLER" \
			| shasum -b -a256 -c - >"$DEBUGOUT"
	}; then
		echo "Error: Windows installation image is invalid" >&2
		return 1
	fi
}
getWindowsVirtualMachine() { # creates virtual machine if not already, validates
	getParallels || return 1
	if "$PRLCTL" list "$MFFER_TEST_VM" >"$DEBUGOUT" 2>&1; then
		echo "Using virtual machine '$MFFER_TEST_VM'" >"$VERBOSEOUT"
	else
		echo "Creating virtual machine '$MFFER_TEST_VM'" >"$VERBOSEOUT"
		createWindowsVirtualMachine || return 1
	fi
	getVMBaseSnapshotId || return 1
}
printAutounattend() {
	cat <<-EOF
		<?xml version="1.0" encoding="utf-8"?>
		<unattend xmlns="urn:schemas-microsoft-com:unattend">
			<settings pass="windowsPE">
				<component name="Microsoft-Windows-International-Core-WinPE" processorArchitecture="amd64" publicKeyToken="31bf3856ad364e35" language="neutral" versionScope="nonSxS" xmlns:wcm="http://schemas.microsoft.com/WMIConfig/2002/State" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
					<SetupUILanguage>
						<UILanguage>en-US</UILanguage>
					</SetupUILanguage>
					<InputLocale>en-US</InputLocale>
					<SystemLocale>en-US</SystemLocale>
					<UILanguage>en-US</UILanguage>
					<UserLocale>en-US</UserLocale>
				</component>
				<component name="Microsoft-Windows-Setup" processorArchitecture="amd64" publicKeyToken="31bf3856ad364e35" language="neutral" versionScope="nonSxS" xmlns:wcm="http://schemas.microsoft.com/WMIConfig/2002/State" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
					<DiskConfiguration>
						<WillShowUI>OnError</WillShowUI>
						<Disk wcm:action="add">
							<CreatePartitions>
								<CreatePartition wcm:action="add">
									<Order>1</Order>
									<Size>500</Size>
									<Type>Primary</Type>
								</CreatePartition>
								<CreatePartition wcm:action="add">
									<Order>2</Order>
									<Size>100</Size>
									<Type>EFI</Type>
								</CreatePartition>
								<CreatePartition wcm:action="add">
									<Order>3</Order>
									<Size>16</Size>
									<Type>MSR</Type>
								</CreatePartition>
								<CreatePartition wcm:action="add">
									<Extend>true</Extend>
									<Order>4</Order>
									<Type>Primary</Type>
								</CreatePartition>
							</CreatePartitions>
							<ModifyPartitions>
								<ModifyPartition wcm:action="add">
									<Format>NTFS</Format>
									<Label>WinRE</Label>
									<Order>1</Order>
									<PartitionID>1</PartitionID>
									<TypeID>DE94BBA4-06D1-4D40-A16A-BFD50179D6AC</TypeID>
								</ModifyPartition>
								<ModifyPartition wcm:action="add">
									<Format>FAT32</Format>
									<Label>System</Label>
									<PartitionID>2</PartitionID>
									<Order>2</Order>
								</ModifyPartition>
								<ModifyPartition wcm:action="add">
									<Order>3</Order>
									<PartitionID>3</PartitionID>
								</ModifyPartition>
								<ModifyPartition wcm:action="add">
									<Format>NTFS</Format>
									<Label>Windows</Label>
									<Letter>C</Letter>
									<Order>4</Order>
									<PartitionID>4</PartitionID>
								</ModifyPartition>
							</ModifyPartitions>
							<DiskID>0</DiskID>
							<WillWipeDisk>true</WillWipeDisk>
						</Disk>
					</DiskConfiguration>
					<ImageInstall>
						<OSImage>
							<InstallTo>
								<DiskID>0</DiskID>
								<PartitionID>4</PartitionID>
							</InstallTo>
						</OSImage>
					</ImageInstall>
					<UserData>
						<ProductKey>
							<!-- Windows 10/11 Pro KMS key -->
							<Key>W269N-WFGWX-YVC9B-4J6C9-T83GX</Key>
							<WillShowUI>Never</WillShowUI>
						</ProductKey>
						<AcceptEula>true</AcceptEula>
					</UserData>
				</component>
			</settings>
			<settings pass="oobeSystem">
				<component name="Microsoft-Windows-International-Core" processorArchitecture="amd64" publicKeyToken="31bf3856ad364e35" language="neutral" versionScope="nonSxS" xmlns:wcm="http://schemas.microsoft.com/WMIConfig/2002/State" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
					<InputLocale>en-US</InputLocale>
					<SystemLocale>en-US</SystemLocale>
					<UILanguage>en-US</UILanguage>
					<UserLocale>en-US</UserLocale>
				</component>
				<component name="Microsoft-Windows-Shell-Setup" processorArchitecture="amd64" publicKeyToken="31bf3856ad364e35" language="neutral" versionScope="nonSxS" xmlns:wcm="http://schemas.microsoft.com/WMIConfig/2002/State" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
					<OOBE>
						<HideEULAPage>true</HideEULAPage>
						<HideOEMRegistrationScreen>true</HideOEMRegistrationScreen>
						<HideOnlineAccountScreens>true</HideOnlineAccountScreens>
						<HideWirelessSetupInOOBE>true</HideWirelessSetupInOOBE>
						<ProtectYourPC>1</ProtectYourPC>
					</OOBE>
					<UserAccounts>
						<LocalAccounts>
							<LocalAccount wcm:action="add">
								<Password>
									<Value></Value>
									<PlainText>true</PlainText>
								</Password>
								<Group>Administrators</Group>
								<DisplayName>$USERNAME</DisplayName>
								<Name>$USERNAME</Name>
							</LocalAccount>
						</LocalAccounts>
					</UserAccounts>
					<AutoLogon>
						<Password>
							<Value></Value>
							<PlainText>true</PlainText>
						</Password>
						<Enabled>true</Enabled>
						<LogonCount>999999999</LogonCount>
						<Username>$USERNAME</Username>
					</AutoLogon>
					<FirstLogonCommands>
						<SynchronousCommand wcm:action="add">
							<CommandLine>powershell.exe -ExecutionPolicy Bypass -File E:\WinSetup.ps1</CommandLine>
							<Description>Run system customization scripts</Description>
							<Order>1</Order>
						</SynchronousCommand>
					</FirstLogonCommands>
				</component>
			</settings>
			<settings pass="specialize">
				<component name="Microsoft-Windows-Shell-Setup" processorArchitecture="amd64" publicKeyToken="31bf3856ad364e35" language="neutral" versionScope="nonSxS" xmlns:wcm="http://schemas.microsoft.com/WMIConfig/2002/State" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
					<ComputerName>$MFFER_TEST_VM_HOSTNAME</ComputerName>
				</component>
			</settings>
		</unattend>
	EOF
}
printWinSetup() {
	cat <<-"EOF"
		# Disable the prompt for Network Discovery
		REG ADD "HKLM\System\CurrentControlSet\Control\Network\NewNetworkWindowOff" /F

		# Enable SSH server and setup administrator account
		Add-WindowsCapability -Online -Name "OpenSSH.Client~~~~0.0.1.0"
		Add-WindowsCapability -Online -Name "OpenSSH.Server~~~~0.0.1.0"
		# Making the startup type automatic, but not starting---will be started after reboot
		Set-Service -Name sshd -StartupType 'Automatic'
		$authorizedKey = Get-Content -Path E:\administrators_authorized_keys
		Add-Content -Force -Path $env:ProgramData\ssh\administrators_authorized_keys -Value "$authorizedKey";icacls.exe "$env:ProgramData\ssh\administrators_authorized_keys" /inheritance:r /grant "Administrators:F" /grant "SYSTEM:F"
		New-NetFirewallRule -Name 'OpenSSH-Server-In-TCP' -DisplayName 'OpenSSH Server (sshd)' -Enabled True -Direction Inbound -Protocol TCP -Action Allow -LocalPort 22

		# Download and attempt to add updates
		Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -Force
		Set-PSRepository PSGallery -InstallationPolicy Trusted
		Install-Module PSWindowsUpdate
		Get-WindowsUpdate -AcceptAll -AutoReboot -Install
		Restart-Computer
	EOF
}
waitForInstallation() {
	starttime="$(getTime)"
	maxtime="$((4 * 60 * 60))" # 4 hours, in seconds
	until sshIsRunning; do
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
waitForShutdown() {
	starttime="$(getTime)"
	maxtime="$((10 * 60))" # 10 minutes, in seconds
	until ! vmIsRunning "$MFFER_TEST_VM"; do
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
	until sshIsRunning; do
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
main
