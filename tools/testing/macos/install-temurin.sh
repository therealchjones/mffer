#!/bin/sh

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
