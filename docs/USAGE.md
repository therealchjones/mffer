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
-   il2cppdumper
-   UABE (Windows)

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
