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
#   Devices used may not run correctly on emulated systems such as
#   Parallels or VirtualBox.
# - Internet connection with access to Google developer tools, Google
#   Play Store, Netmarble servers, and Facebook
# - An installed version of Android command-line tools (also included in
#   Android Studio)
# - Java (required by Android command-line tools)
# - A Google account (to log into the Play store)
# - A Facebook or Google account to which your Marvel Future Fight game
#   is synchronized (to download personal game data)
# - A reasonable machine upon which to run these; the emulators require
#   a few GB of RAM just by themselves, for instance

DEBUG=N
VERBOSE=Y
OUTPUTDIR=~/"Development/Marvel Future Fight"

DEBUGOUT=/dev/null
VERBOSEOUT=/dev/null
if [ "$DEBUG" = "Y" ]; then
	set -x
	DEBUGOUT=/dev/stdout
	VERBOSE=Y
fi
if [ "$VERBOSE" = "Y" ]; then
	VERBOSEOUT=/dev/stdout
fi

MFFTEMPDIR="$(mktemp -d)"
if [ "$?" = "0" ] && [ -d "$MFFTEMPDIR" ]; then
	if [ -n "$( find "$MFFTEMPDIR" ! -path "$MFFTEMPDIR" )" ]; then
		echo "Temporary directory '$MFFTEMPDIR' is not empty. Exiting." >&2
		exit 1
	fi
	trap 'rm -rf "$MFFTEMPDIR"; kill -- -$$' EXIT HUP INT QUIT TERM
else
	echo "Unable to create temporary directory. Exiting" >&2
	exit 1
fi
if [ -z "$HOME" ]; then
	echo 'Warning: HOME environment variable is not set.' >&2
fi
ANDROID_SDK_ROOT="${ANDROID_SDK_ROOT:-$HOME/Library/Android/sdk}"
ANDROID_HOME="$ANDROID_SDK_ROOT"
export ANDROID_SDK_ROOT

ANDROID_SDKMANAGER="${ANDROID_SDKMANAGER:-$( find "$ANDROID_SDK_ROOT" \( -type f -o -type l \) -name sdkmanager )}"
if [ ! -x "$ANDROID_SDKMANAGER" ]; then
	echo 'Unable to find sdkmanager.' >&2
	echo 'If sdkmanager is installed, set ANDROID_SDK_ROOT properly.' >&2
	echo 'If not, download Android command line tools from' >&2
	echo 'https://developer.android.com/studio#command-tools' >&2
	echo "and unpack into $ANDROID_SDK_ROOT/cmdline-tools/latest/" >&2
	exit 1
fi
mkdir -p "$MFFTEMPDIR/sdk"
echo 'Accepting Android command line tool licenses' >"$VERBOSEOUT"
i=0; while [ $i -lt 100 ]; do echo y; i=$(( $i + 1 )); done |
	"$ANDROID_SDKMANAGER" --sdk_root="$MFFTEMPDIR/sdk" --licenses |
	sed '/^y$/d' >"$DEBUGOUT"
echo 'Getting updated Android command line tools' >"$VERBOSEOUT"
"$ANDROID_SDKMANAGER" --sdk_root="$MFFTEMPDIR/sdk" --install \
	'cmdline-tools;latest' >"$VERBOSEOUT"
ANDROID_SDK_ROOT="$MFFTEMPDIR/sdk"
ANDROID_HOME="$ANDROID_SDK_ROOT"
ANDROID_EMULATOR_HOME="$MFFTEMPDIR"
ANDROID_SDK_HOME="$MFFTEMPDIR"
ANDROID_PREFS_ROOT="$MFFTEMPDIR"
ANDROID_AVD_HOME="$MFFTEMPDIR/avd"
export ANDROID_SDK_ROOT ANDROID_HOME ANDROID_EMULATOR_HOME
export ANDROID_SDK_HOME ANDROID_PREFS_ROOT ANDROID_AVD_HOME
ANDROID_SDKMANAGER="$( find "$ANDROID_SDK_ROOT" \( -type f -o -type l \) -name sdkmanager )"
if [ ! -x "$ANDROID_SDKMANAGER" ]; then
	echo 'Unable to get sdkmanager. Exiting.' >&2
	exit 1
fi
echo 'Getting Android emulator and platform tools' >"$VERBOSEOUT"
"$ANDROID_SDKMANAGER" --install \
	'platform-tools' \
	emulator \
	'platforms;android-30' >"$DEBUGOUT"
# We do the system images separately because otherwise they don't
# recognize that emulator (a dependency of theirs) is already
# installed and install a second copy which messes things up.
echo 'Getting Android system images' >"$VERBOSEOUT"
"$ANDROID_SDKMANAGER" --install \
	'system-images;android-30;google_apis_playstore;x86' \
	'system-images;android-30;google_apis;x86'
ANDROID_AVDMANAGER="$(find "$ANDROID_SDK_ROOT" \( -type f -o -type l \) -name avdmanager )"
if [ ! -x "$ANDROID_AVDMANAGER" ]; then
	echo 'Unable to get avdmanager. Exiting.' >&2
	exit 1
fi
ANDROID_EMULATOR="$(find "$ANDROID_SDK_ROOT" \( -type f -o -type l \) -name emulator )"
if [ ! -x "$ANDROID_EMULATOR" ]; then
	echo 'Unable to get Android emulator. Exiting.' >&2
	exit 1
fi
ANDROID_ADB="$(find "$ANDROID_SDK_ROOT" \( -type f -o -type l \) -name adb)"
if [ ! -x "$ANDROID_ADB" ]; then
	echo 'Unable to get adb. Exiting.' >&2
	exit 1
fi
echo 'Creating Android virtual devices' >"$VERBOSEOUT"
mkdir -p "$MFFTEMPDIR/avd"
"$ANDROID_AVDMANAGER" create avd --package 'system-images;android-30;google_apis_playstore;x86' --name 'mff_google_play' --device "7in WSVGA (Tablet)" >"$DEBUGOUT"
"$ANDROID_AVDMANAGER" create avd --package 'system-images;android-30;google_apis;x86' --name 'mff_no_google_play' --device "7in WSVGA (Tablet)" >"$DEBUGOUT"
for name in mff_google_play mff_no_google_play; do
	sed -E -e 's/^hw.keyboard[ =]+.*$/hw.keyboard = yes/' \
		"$MFFTEMPDIR"/avd/"$name".avd/config.ini > "$MFFTEMPDIR/newconfig" &&
		mv "$MFFTEMPDIR/newconfig" "$MFFTEMPDIR/avd/$name.avd/config.ini"
done
echo 'Starting Google Play Android virtual device.' >"$VERBOSEOUT"
echo ''
echo '************* USER INTERACTION REQUIRED *************'
echo 'On the emulator, open the Google Play Store app, sign'
echo 'in, and install the latest version of Marvel Future '
echo 'Fight. Leave the emulator running.'
echo '******************************************************'
echo ''
echo 'Press <enter> or <return> when that is complete.'
if [ "$DEBUG" = "Y" ]; then EMULATOR_VERBOSETAG="-verbose"; fi
"$ANDROID_EMULATOR" $EMULATOR_VERBOSETAG -avd mff_google_play -memory 3583 -no-boot-anim -no-audio >"$DEBUGOUT" 2>&1 &
emulator_pid="$!"
read -r
if ! ps -p "$emulator_pid" >/dev/null; then
	echo "Emulator is no longer running. Exiting." >&2
	exit 1
fi
echo 'Getting installation files' >"$VERBOSEOUT"
mkdir "$MFFTEMPDIR/new-apks"

get_avd_serial() {
	if [ -z "$1" ] || [ -n "$2" ]; then
		echo Usage: get_avd_serial avd_name >&2
		exit 1
	fi
	avd_name="$1"
	avd_serial=""
	adb_timeout=30
	adb_times=0
	while [ -z "$avd_serial" ] && [ "$adb_times" -lt "$adb_timeout" ]; do
		# pipelines can execute components within subshells, so may not
		# be able to set variable values that persist after the command.
		# We work around this with command substitution and echo instead.
		avd_serial="$( "$ANDROID_ADB" devices | \
			sed -e '/^List of devices/d' \
				-e '/^[[:space:]]*$/d' \
				-e 's/^[[:space:]]*\([^[:space:]]*\)[[:space:]].*$/\1/' |
				( # inner subshell for pipeline workaround
				while read serial; do
					checkname="$( "$ANDROID_ADB" -s "$serial" emu avd name | \
						sed -e 's/[[:space:]]*$//' -e '$d' )"
					if [ "$checkname" = "$avd_name" ]; then
						if  [ -n "$avd_serial" ]; then
							echo "Multiple devices named '$avd_name' found." >&2
							return 1
						fi
						avd_serial="$serial"
					fi
				done
				if [ -n "$avd_serial" ]; then
					echo "$avd_serial"
				fi ) )"
		if [ -z "$avd_serial" ]; then
			sleep 1
			adb_times="$(( $adb_times + 1 ))"
		fi
	done
	if [ -z "$avd_serial" ]; then
		echo "Unable to find device named '$avd_name'. Exiting." >&2
		return 1
	fi
	echo "$avd_serial"
}
serial="$(get_avd_serial mff_google_play)" || exit 1
{
	"$ANDROID_ADB" -s "$serial" shell pm path com.netmarble.mherosgb |
		while read pathline; do
			path="$( echo "$pathline" | sed 's/^package:[[:space:]]*//' )"
			file="$( basename "$path" )"
			"$ANDROID_ADB" pull "$path" "$MFFTEMPDIR/new-apks/$file" >"$DEBUGOUT"
		done
} || {
	echo "Unable to obtain installation files. Exiting." >&2
	exit 1
}
echo 'Stopping Google Play emulator' >"$VERBOSEOUT"
kill "$emulator_pid"
echo 'Starting rootable Android virtual device' >"$VERBOSEOUT"
"$ANDROID_EMULATOR" $EMULATOR_VERBOSETAG -avd mff_no_google_play -no-boot-anim -no-audio >"$DEBUGOUT" 2>&1 &
emulator_pid="$!"
echo 'Connecting to emulator' >"$VERBOSEOUT"
serial="$(get_avd_serial mff_no_google_play)" || exit 1
"$ANDROID_ADB" -s "$serial" wait-for-device
until "$ANDROID_ADB" -s "$serial" shell pm list users >/dev/null 2>&1; do
	sleep 30
done
echo 'Installing Marvel Future Fight' >"$VERBOSEOUT"
"$ANDROID_ADB" -s "$serial" install-multiple "$MFFTEMPDIR/new-apks/"* >"$DEBUGOUT" ||
	{
		echo "Unable to install. Exiting." >&2
		exit 1
	}
{
	VERSIONSTRING="$(
			"$ANDROID_ADB" -s "$serial" shell dumpsys package com.netmarble.mherosgb |
				grep versionName | cut -f2 -d'='
		)"
}
echo ''
echo '************* USER INTERACTION REQUIRED *************'
echo '1. Open Marvel Future Fight.'
echo '2. Press "Cancel" if prompted to install the Google '
echo '   Play Games app'
echo '3. Download updates when prompted.'
echo '4. Interrupt the tutorial by pressing the "Gear" icon '
echo '   and "Settings".'
echo '5. Wait for any automatic downloads shown in progress to'
echo '   complete. Go to the "Option" tab and "Download All'
echo '   Data". Wait for the downloads to finish.'
echo '6. Go to the "Account" tab and sign in to Facebook; if'
echo '   prompted for Chrome sign in and sync, "No Thanks".'
echo '7. When the app restarts and returns to the main'
echo '   lobby screen, press the square button and swipe up'
echo '   to close the app.'
echo '8. Leave the emulator running and...'
echo '******************************************************'
echo ''
echo 'Press <enter> or <return> when that is complete.'
read -r
if ! ps -p "$emulator_pid" >/dev/null; then
	echo "Emulator is no longer running. Exiting." >&2
	exit 1
fi
echo 'Cataloging virtual device files' >"$VERBOSEOUT"
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
"$ANDROID_ADB" -s "$serial" push "$MFFTEMPDIR/getfiles" /sdcard/Download >"$DEBUGOUT"
"$ANDROID_ADB" -s "$serial" shell su root '/bin/sh /sdcard/Download/getfiles' >"$DEBUGOUT"
echo 'Downloading virtual device files' >"$VERBOSEOUT"
"$ANDROID_ADB" -s "$serial" pull /sdcard/Download/device-files.tar.gz "$MFFTEMPDIR" >"$DEBUGOUT"||
	{
		echo 'Unable to obtain device files. Exiting.' >&2
		exit 1
	}
kill $emulator_pid
echo 'Extracting virtual device files' >"$VERBOSEOUT"
mkdir -p "$MFFTEMPDIR"/release/device-files
# consider changing path in subshell, using pax instead of tar
cd "$MFFTEMPDIR"/release/device-files && tar xzf "$MFFTEMPDIR/device-files.tar.gz"
if [ ! -d "$OUTPUTDIR" ]; then
	mkdir -p "$OUTPUTDIR" || {
		echo "Unable to access output directory '$OUTPUTDIR'. Exiting." >&2
		exit 1
	}
fi

DEVICEFILEDIR="MFF-device-$VERSIONSTRING"
i=1
if [ "x${DEVICEFILEDIR%-$i}" != "x$DEVICEFILEDIR" ]; then
	DEVICEFILEDIR="$DEVICEFILEDIR-$i"
fi
while [ -d "$OUTPUTDIR"/device-files/"$DEVICEFILEDIR" ] && [ "$i" -lt 99 ]; do
	DEVICEFILEDIR="${DEVICEFILEDIR%-$i}"
	i=$(( $i + 1 ))
	DEVICEFILEDIR="${DEVICEFILEDIR}-$i"
done

mv "$MFFTEMPDIR"/release/device-files "$OUTPUTDIR"/device-files/"$DEVICEFILEDIR" || {
	echo "Unable to move device files to output directory " >&2
	echo "'$OUTPUTDIR/device-files'." >&2
	echo 'Stopping here so as to avoid deleting them all.' >&2
	echo 'Press <enter> or <return> to end the script after moving them maually.' >&2
	read -r
	exit 1
}

DATADIR="MFF-data-$VERSIONSTRING"
echo ''
echo '************* USER INTERACTION REQUIRED *************'
echo 'Use UABE (in Windows) to extract assets from the '
echo ' device:'
echo "Open '$OUTPUTDIR/device-files/$DEVICEFILEDIR/data/media/0/Android/data/com.netmarble.mherosgb/files/bundle/text',"
echo " decompress text as prompted to '$OUTPUTDIR/data/$DATADIR/text'"
echo 'Choose Info->Select all (using shift-click)->Export Dump'
echo " ->UABE JSON dump to '$OUTPUTDIR/data/$DATADIR/assets'"
echo "Open $OUTPUTDIR/device-files/$DEVICEFILEDIR/data/media/0/Android/data/com.netmarble.mherosgb/files/bundle/localization_en',"
echo " decompress localization_en as prompted to '$OUTPUTDIR/data/$DATADIR/localization_en'"
echo 'Choose Info->Select all (using shift-click)->Export Dump'
echo " ->UABE JSON dump to '$OUTPUTDIR/data/$DATADIR/assets'"
echo '******************************************************'
echo ''
echo 'Press <enter> or <return> when that is complete.'
read -r

# get version name
# il2cpp, ghidra scripts
# find an appropriate behavior for when installation doesn't work, e.g., x86_64
#  for 6.8.0-6.8.1
# do as much in the background as possible when using emulators
# move "release" to an appropriate destination directory and
#  rename all subdirectories
# test for appropriate directories at the beginning
# export assets via UABE (or python-unitypack?) automatically rather than manually
# add extraction of data via the c# app
# customize downloads directory/directories
# get rid of emulator version warning
# run without DEBUG, also without VERBOSE, to review output; should make framework
#  to do this with each "release"
# more testing (and resulting exiting) for when things don't work
# make architecture configurable? at the moment (6.8.0-6.8.1), x86_64 doesn't
#  seem to work, changing to x86
# figure out how to do "manual" parts in a more automated way
# better response to ^C/cancellation; trapping isn't clean right now
# consider running emulators in low-memory mode?
# consider appropriate removal of tar "errors", maybe just use pax
# when complete and working, go through DEBUG output to see if
#  I'm missing anything
