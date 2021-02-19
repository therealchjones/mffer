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

VERSIONSTRING='6.8.1'

DEBUG=Y
VERBOSE=Y

VERBOSEOUT=/dev/null

OUTPUTDIR=~/Downloads

if [ "$DEBUG" = "Y" ]; then set -x; VERBOSEOUT=/dev/stdout; fi
if [ "$VERBOSE" = "Y" ]; then VERBOSEOUT=/dev/stdout; fi

exec 1>"$VERBOSEOUT"

MFFTEMPDIR="$(mktemp -d)"
if [ "$?" == "0" ] && [ -d "$MFFTEMPDIR" ]; then
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
echo 'Getting Android command line tools'
mkdir -p "$MFFTEMPDIR/sdk"
echo 'Accepting licenses'
i=0; while [ $i -lt 100 ]; do echo y; i=$(( $i + 1 )); done |
	"$ANDROID_SDKMANAGER" --sdk_root="$MFFTEMPDIR/sdk" --licenses |
	sed '/^y$/d'
"$ANDROID_SDKMANAGER" --sdk_root="$MFFTEMPDIR/sdk" --install \
	'cmdline-tools;latest'
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
"$ANDROID_SDKMANAGER" --install \
	'platform-tools' \
	emulator \
	'platforms;android-30'
# We do the system images separately because otherwise they don't
# recognize that emulator (a dependency of theirs) is already 
# installed and install a second copy which messes things up.
echo 'Getting Android system images'
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
	echo 'Unable to get abd. Exiting.' >&2
	exit 1
fi
echo 'Creating Android virtual devices'
mkdir -p "$MFFTEMPDIR/avd"
"$ANDROID_AVDMANAGER" create avd --package 'system-images;android-30;google_apis_playstore;x86' --name 'mff_google_play' --device "7in WSVGA (Tablet)"
"$ANDROID_AVDMANAGER" create avd --package 'system-images;android-30;google_apis;x86' --name 'mff_no_google_play' --device "7in WSVGA (Tablet)"
for name in mff_google_play mff_no_google_play; do
	sed -E -e 's/^hw.keyboard[ =]+.*$/hw.keyboard = yes/' \
		"$MFFTEMPDIR"/avd/"$name".avd/config.ini > "$MFFTEMPDIR/newconfig" &&
		mv "$MFFTEMPDIR/newconfig" "$MFFTEMPDIR/avd/$name.avd/config.ini"
done
echo 'Starting Android virtual device.'
exec 1>/dev/stdout
echo ''
echo '************* USER INTERACTION REQUIRED *************'
echo 'On the emulator, open the Google Play Store app, sign'
echo 'in, and install the latest version of Marvel Future '
echo 'Fight. Leave the emulator running.'
echo '******************************************************'
echo ''
echo 'Press <enter> or <return> when that is complete.'
exec 1>"$VERBOSEOUT"
if [ "$DEBUG" = "Y" ]; then EMULATOR_VERBOSETAG="-verbose"; fi
"$ANDROID_EMULATOR" $EMULATOR_VERBOSETAG -avd mff_google_play -memory 3583 -no-boot-anim -no-audio &
emulator_pid="$!"
read -r 
if ! ps -p "$emulator_pid" >/dev/null; then
	echo "Emulator is no longer running. Exiting." >&2
	exit 1
fi
echo 'Getting installation files'
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
			"$ANDROID_ADB" pull "$path" "$MFFTEMPDIR/new-apks/$file"
		done 
} || {
	echo "Unable to obtain installation files. Exiting." >&2 
	exit 1 
}
echo 'Stopping emulator'
kill "$emulator_pid"
echo 'Starting new Android virtual device'
"$ANDROID_EMULATOR" $EMULATOR_VERBOSETAG -avd mff_no_google_play -no-boot-anim -no-audio 2>&1 &
emulator_pid="$!"
echo 'Connecting to emulator'
serial="$(get_avd_serial mff_no_google_play)" || exit 1
"$ANDROID_ADB" -s "$serial" wait-for-device
until "$ANDROID_ADB" -s "$serial" shell pm list users >/dev/null 2>&1; do
	sleep 30
done
echo 'Installing Marvel Future Fight'
"$ANDROID_ADB" -s "$serial" install-multiple "$MFFTEMPDIR/new-apks/"* ||
	{ 
		echo "Unable to install. Exiting." >&2
		exit 1
	}
exec 1>/dev/stdout
echo ''
echo '************* USER INTERACTION REQUIRED *************'
echo '1. Open Marvel Future Fight.'
echo '2. Press "Cancel" if prompted to install the Google '
echo '   Play Games app'
echo '3. Download updates when prompted.'
echo '4. Interrupt the tutorial by pressing the "Gear" icon '
echo '   and "Settings".'
echo '5. Go to the options tab and "Download All Data"; this'
echo '   may already be in progress with the button disabled.'
echo '   Wait for the downloads to finish.'
echo '6. Go to the account tab and sign in to Facebook; if'
echo '   prompted for Chrome sign in and sync, "No Thanks".'
echo '7. When the app restarts, and returns to the main'
echo '   lobby screen, press the square button and swipe up'
echo '   to close the app.'
echo '8. Leave the emulator running and...'
echo '******************************************************'
echo ''
echo 'Press <enter> or <return> when that is complete.'
exec 1>"$VERBOSEOUT"
read -r
if ! ps -p "$emulator_pid" >/dev/null; then
	echo "Emulator is no longer running. Exiting." >&2
	exit 1
fi
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
"$ANDROID_ADB" -s "$serial" push "$MFFTEMPDIR/getfiles" /sdcard/Download
"$ANDROID_ADB" -s "$serial" shell su root '/bin/sh /sdcard/Download/getfiles'
echo 'Downloading virtual device files'
"$ANDROID_ADB" -s "$serial" pull /sdcard/Download/device-files.tar.gz "$MFFTEMPDIR" ||
	{ 
		echo 'Unable to obtain device files. Exiting.' >&1 
		exit 1 
	}
kill emulator_pid
echo 'Extracting virtual device files'
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
while [ -d "$OUTPUTDIR"/"$DEVICEFILEDIR" ] && [ "$i" -lt 99 ]; do
	DEVICEFILEDIR="${DEVICEFILEDIR%-$i}"
	i=$(( $i + 1 ))
	DEVICEFILEDIR="${DEVICEFILEDIR}-$i"
done

mv "$MFFTEMPDIR"/release/device-files "$OUTPUTDIR"/"$DEVICEFILEDIR" || {
	echo "Unable to move files to output directory '$OUTPUTDIR'." >&2
	echo 'Stopping here so as to avoid deleting them all.' >&2
	echo 'Press <enter> or <return> to end the script after moving them maually.' >&2
	read -r
}

# get version name
# find an appropriate behavior for when installation doesn't work, e.g., x86_64
#  for 6.8.0-6.8.1
# increase memory on emulators, make configurable? (Max is 4000, may not be
#  ussable on a machine that can't handle that anyway)
# do as much in the background as possible when using emulators
# move "release" to an appropriate destination directory and
# rename all subdirectories
# export assets via UABE
# customize downloads directory
# get rid of emulator version warning
# run without DEBUG, also without VERBOSE, to review output
# consider optional HAXM installation when doing "permanent" install
# more testing (and resulting exiting) for when things don't work
# make architecture configurable? at the moment (6.8.0-6.8.1), x86_64 doesn't
#  seem to work, changing to x86
# figure out how to do "manual" parts in a more automated way
# when complete and working, go through DEBUG output to see if
#  I'm missing any thing
