#!/bin/sh

echo "Installing .NET SDK 5.0..."
curl -Ss -OL "https://dot.net/v1/dotnet-install.sh" \
	&& sh ./dotnet-install.sh --channel 5.0
