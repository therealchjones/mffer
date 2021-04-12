# mffer: Marvel Future Fight Extraction & Reporting

mffer update
mffer new
mffer record (add new raw data)
mffer component componentName (eval/print componentName)

Usage instructions: (should guide the design of the code)

-   How to use the GUI/Webapp (if it’s not inuitive, make it better)
-   “ command line (verbs, options, arguments)
-   “ Library (via program.cs) (Library API similar to command line)
-   How to add a component via program.cs (basic component class API)
-   How to add a component via a new class (standard class inheritance)
-   How to contribute to the base (basic coding guidelines/formatting)

## Requirements

## Installation

## Workflow

### autoextract

download and extract game data

#### Requirements

-   POSIX sh and typical development environment (in addition to POSIX, at least mktemp and tar are needed)
-   Android SDK or Android Studio with command line tools
    -   command-line tools: sdkmanager, avdmanager
    -   emulator
    -   platform tools: adb
    -   platforms: android-30
    -   system-images: android-30;google_apis_playstore;x86 and android-30;google_apis;x86'
-   Java runtime or SDK (required by Android command line tools)
-   il2cppdumper
-   UABE (Windows)

At least macOS and most Linux distributions satisfy the needs for the initial
environment. They may require installation of a Java runtime (or SDK) if one is
not already installed. The Java requirement is for the Android command line
tools, but Java may be useful for other items of interest to those using mffer, such as the ghidra disassembler. We recommend the OpenJDK 11 distribution freely available from (AdoptOpenJDK)[AdoptOpenJDK.com] at [https://adoptopenjdk.net/releases.html?variant=openjdk11&jvmVariant=hotspot].

The (Android command-line tools)[https://developer.android.com/studio/command-line] are part of Android Studio, but may also be obtained separately from the larger application at [https://developer.android.com/studio#command-tools]. This basic package includes `sdkmanager` which `autoextract` will automatically use to update the command-line tools themselves as well as obtain temporary copies of all the other Android SDK packages required to run the Android emulator and extract the Marvel Future Fight data from it.

il2cppdumper and UABE are required for the initial processing of the MFF data after it's been extracted from the Android systems. Terminal instructions for using these are provided when appropriate during `autoextract`'s run.

#### Usage

Run `autoextract -o ~/mff-data` or similar, then follow the instructions on the terminal.

```
autoextract [-v] -o output_directory
autoextract -h
```

```
mandatory arguments:
	-o output_directory
	    place device-files/ and data/ directories into output_directory
options:
	-h	print this summarized help message
	-v	print progress information; specify twice for debug output
```

### mffer

process Marvel Future Fight data

#### Requirements

-   .NET Core 3.1 or .NET 5.0 or later

#### Usage

Run `dotnet mffer.dll --datadir ~/mff-data --outputdir ~/mff-output` and wait. A while.

(It may be necessary to customize settings in Program.cs and build first, and is
certainly necessary to download and extract data first. See
(autoextract)[#autoextract]. )

```
mffer --datadir data_directory --outputdir output_directory
```

### Google Apps Script & G Suite

use Marvel Future Fight data for game decisions

## See Also
