#!/bin/sh

# autoextract.sh
#
#
# Create a package of files for the latest release of Marvel Future Fight
#
# By default, does not modify system beyond temporary directories; this 
# therefore requires new downloads of all components upon each run.
#
# Requirements:
# - POSIX-compliant Unix-like environment for which all the used
#   programs are available (likely MacOS/OS X, Windows with Cygwin or 
#   another POSIX layer, or Linux). Of specific note, the Android Virtual
#   Devices used may not run correctly on an emulated system such as
#   Parallels or VirtualBox.
# - An installed version of Android command-line tools (also included in
#   Android Studio)

MFFTEMPDIR="$(mktemp -d)"
if [ "$?" == "0" ]; then
	if [ -d "$MFFTEMPDIR" ]; then
		for file in "$MFFTEMPDIR"/*; do
			if [ -e "$file" ]; then
				echo "Temporary directory $MFFTEMPDIR is not empty. Exiting." >&2
				exit 1
			fi
		done
		trap 'rm -rf "$MFFTEMPDIR"' EXIT HUP INT QUIT TERM
	fi
else
	echo "Unable to create temporary directory. Exiting" >&2
	exit 1
fi

if [ -z "$HOME" ]; then 
	echo 'Warning: HOME environment variable is not set.' >&2
fi
ANDROID_SDK_ROOT="${ANDROID_SDK_ROOT:=$HOME/Library/Android/sdk}"
ANDROID_HOME="$ANDROID_SDK_ROOT"
export ANDROID_SDK_ROOT

ANDROID_SDKMANAGER="${ANDROID_SDKMANAGER:=$( find "$ANDROID_SDK_ROOT" \( -type f -o -type l \) -name sdkmanager )}"
if [ ! -x "$ANDROID_SDKMANAGER" ]; then
	echo 'Unable to find sdkmanager.' >&2
	echo 'If sdkmanager is installed, set ANDROID_SDK_ROOT properly' >&2
	echo 'If not, download Android command line tools from' >&2
	echo 'https://developer.android.com/studio#command-tools' >&2
	echo "and unpack into $ANDROID_SDK_ROOT/cmdline-tools/latest/" >&2
	exit 1
fi
echo 'Getting Android command line tools'
mkdir -p "$MFFTEMPDIR/sdk"
"$ANDROID_SDKMANAGER" --sdk_root="$MFFTEMPDIR/sdk" --install \
	'cmdline-tools;latest' \
	'platform-tools' \
	emulator \
	'system-images;android-30;google_apis_playstore;x86_64' \
	'system-images;android-30;google_apis;x86_64' \
	'platforms;android-30'
ANDROID_SDK_ROOT="$MFFTEMPDIR/sdk"
ANDROID_AVDMANAGER="$(find "$ANDROID_SDK_ROOT" \( -type f -o -type l \) -name avdmanager )"
ANDROID_EMULATOR="$(find "$ANDROID_SDK_ROOT" \( -type f -o -type l \) -name emulator )"
ANDROID_ADB="$(find "$ANDROID_ADB" \( -type f -o -type l \) -name adb)"
echo 'Creating Android virtual devices'
mkdir -p "$MFFTEMPDIR/avd"
"$ANDROID_AVDMANAGER" create avd --package 'system-images;android-30;google_apis_playstore;x86_64' --name 'Google_Play' --device "7in WSVGA (Tablet)" --path "$MFFTEMPDIR/avd"
"$ANDROID_AVDMANAGER" create avd --package 'system-images;android-30;google_apis;x86_64' --name 'No_Google_Play' --device "7in WSVGA (Tablet)" --path "$MFFTEMPDIR/avd"
for name in Google_Play No_Google_Play; do
	sed -E -e 's/^hw.keyboard[ =]+.*$/hw.keyboard = yes/' \
		"$MFFTEMPDIR"/"$NAME".avd/config.ini > "$MFFTEMPDIR/newconfig" &&
		mv "$MFFTEMPDIR/newconfig" "$MFFTEMPDIR/avd/$NAME.avd/config.ini"
done
echo 'Starting Android virtual device.'
echo ''
echo 'Open the Google Play Store app, sign in,'
echo 'and install the latest version of Marvel Future Fight.'
echo 'Press <enter> or <return> when that is complete.'
"$ANDROID_EMULATOR" -avd 'Google_Play' -no-boot-anim -no-audio -datadir "$MFFTEMPDIR/avd/Google_Play.avd" >/dev/null 2>&1 &
emulator_pid="$!"
read -r 
echo 'Getting installation files'
mkdir "$MFFTEMPDIR/new-apks"
"$ANDROID_ADB" shell pm path com.netmarble.mherosgb | \
	while read pathline; do
		path="$( echo "$pathline" | sed 's/^package:[[:space:]]*//' )"
		file="$( basename "$path" )"
		"$ANDROID_ADB" pull "$path" "$MFFTEMPDIR/new-apks/$file"
	done
echo 'Stopping emulator'
kill "$emulator_pid"
echo 'Starting new Android virtual device'
"$ANDROID_EMULATOR" -avd 'No_Google_Play' -no-boot-anim -no-audio -delay-adb -datadir "$MFFTEMPDIR/avd/No_Google_Play.avd" 2>&1 &
emulator_pid="$!"
echo 'Connecting to emulator'
while ! adb -e shell echo 2>&1 >/dev/null; do
	sleep 1
done
echo 'Installing Marvel Future Fight'
"$ANDROID_ADB" install-multiple "$MFFTEMPDIR/new-apks/"*
echo ''
echo '1. Open Marvel Future Fight.'
echo '2. Press "Install" if prompted to install the Google Play Games app)'
echo '3. Download updates when prompted.'
echo '4. Interrupt the tutorial by pressing the "Gear" icon and "Settings".'
echo '5. Go to the options tab and "Download All Data".'
echo '6. Go to the account tab and sign in to Facebook.'
echo '7. When the app restarts, swipe up and close the app.'
echo 'Press <enter> or <return> when that is complete.'
read -r
echo 'Cataloging virtual device files'
cat <<'EndOfScript' > "$MFFTEMPDIR/getfiles"
cd /sdcard/Download 
INODE=0
find / -name '*com.netmarble*' -type d -print0 2>/dev/null | 
	xargs -0 ls -id | 
	sort -n | 
	while read LINE; do 
		OLDINODE=$INODE
		INODE="$( echo $LINE | 
			sed -E 's/^[[:space:]]*([0-9]+)[[:space:]].*$/\1/' )" 
		if [ $INODE = $OLDINODE ]; then 
			true
		else 
			echo $LINE
		fi
	done | 
	cut -f2 -d' ' > alldirs
tar -czf device-files.tar.gz -T alldirs
EndOfScript
adb push "$MFFTEMPDIR/getfiles" /sdcard/Download
"$ANDROID_ADB" shell su root '/bin/sh /sdcard/Download/getfiles'
echo 'Downloading virtual device files'
"$ANDROID_ADB" pull /sdcard/Download/device-files.tar.gz "$MFFTEMPDIR"
kill emulator_pid
echo 'Extracting virtual device files'
mkdir -p "$MFFTEMPDIR"/release/device-files
# consider changing path in subshell, using pax instead of tar
cd "$MFFTEMPDIR"/release/device-files && tar xzf "$MFFTEMPDIR/device-files.tar.gz"

# get version name
# move "release" to an appropriate destination directory and
# rename all subdirectories
# export assets via UABE
