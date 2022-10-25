#!/bin/sh

notify "Installing Ghidra 10.1.2..."
curl -Ss -OL https://github.com/NationalSecurityAgency/ghidra/releases/download/Ghidra_10.1.2_build/ghidra_10.1.2_PUBLIC_20220125.zip \
	&& unzip ghidra_10.1.2_PUBLIC_20220125.zip >"$DEBUGOUT"
