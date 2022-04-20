# Using `mffer`

## Introduction

This project develops software that creates and updates the [`mffer` webapp](https://mffer.org). It is not necessary to review any of this to [use the webapp](https://mffer.org).

There are several possible uses for the `mffer` project. A few are readily
apparent, and the workflows for those are described here, with references to
related documents as needed. In brief, these are:

-   [Using the `mffer` webapp](#using-the-mffer-webapp) to review Marvel Future
    Fight data
-   [Using the `mffer` command line tools](#using-the-mffer-command-line-tools)
    to extract, analyze, or summarize Marvel Future Fight data
-   [Using the `mffer` library](#using-the-mffer-library) to develop a custom
    program

Additionally, `mffer` code may be useful to those trying to explore Marvel
Future Fight, explore similar apps, deploy a custom version of the webapp, or
contribute to `mffer` itself. For these topics, refer to the [development
guide](Development.md).

- [Introduction](#introduction)
- [Marvel Future Fight](#marvel-future-fight)
- [This Project](#this-project)
- [Using the `mffer` webapp](#using-the-mffer-webapp)
- [Using the `mffer` command line tools](#using-the-mffer-command-line-tools)
	- [Obtaining the `mffer` command line tools](#obtaining-the-mffer-command-line-tools)
	- [Downloading a Release](#downloading-a-release)
	- [Installation](#installation)
	- [Requirements](#requirements)
- [Usage](#usage)
	- [Obtaining and Extracting the Data Files](#obtaining-and-extracting-the-data-files)
	- [Exploring the Data](#exploring-the-data)
	- [Exploring the Code](#exploring-the-code)
	- [Using and Presenting the Data](#using-and-presenting-the-data)
	- [Data Workflow](#data-workflow)
	- [Analysis Workflow](#analysis-workflow)
- [Using the `mffer` library](#using-the-mffer-library)
- [Reviewing & changing `mffer` code](#reviewing--changing-mffer-code)
- [See also](#see-also)
	- [Brief manuals](#brief-manuals)
	- [Guides & References](#guides--references)

## Marvel Future Fight

[Marvel Future Fight](http://www.marvelfuturefight.com/) (MFF) is a mobile role-playing game by [NetMarble](https://company.netmarble.com/) set in the extended [Marvel](https://www.marvel.com/) multiverse. It is made with many industry-standard tools, including programming in Java and C# for Unity (using il2cpp), packaged (or at least delivered) as split APKs for Android from the Google Play Store, and using Facebook and NetMarble servers for user and game data storage. As such, even if you don't play MFF, the descriptions of techniques used in this project for exploring those many components may contain some useful knowledge.

[Marvel Future Fight](http://www.marvelfuturefight.com/) is a mobile (iOS & Android) online role-playing game by [NetMarble](https://company.netmarble.com/). It's set in the extended [Marvel](https://www.marvel.com/) multiverse and has more than 200 characters to collect and modify with dozens of different resources, and enough game modes to make mastering all of them nigh impossible.

As such, the game has a large amount of data about the characters, resources, stores, game modes and levels, and actions, even before taking into account the variations between individual players. Although there is information available from those who have significant experience playing the game, objective quantitative data is rarely documented well and is of uncertain provenance.

## This Project

This project is intended to facilitate analysis of [Marvel Future Fight](#marvel-future-fight) and provide access to the data it uses for game play. This is almost certainly against the [NetMarble Terms of Service](https://help.netmarble.com/terms/terms_of_service_en?locale=&lcLocale=en) as well as those of multiple affiliates.

The project currently includes multiple components:

-   a [shell script](apkdl.md) to obtain the Marvel Future Fight program files
-   a [shell script](autoanalyze.md) to decompile and evaluate the
    program files
-   a [.NET console app](mffer.md) to obtain and parse the Marvel Future Fight data files into an open and usable format
-   a [web app](webapp.md) to present and use the game data

The objectives of this umbrella project are:

-   obtain verifiable objective quantitative data about the game, typically using reverse engineering and related methods
-   make the data easily usable for decision making necessary to play the game effectively and efficiently
-   compare changes in the data between different releases/versions of the game
-   easily track important player-specific data to evaluate progress and plan modifications

## Using the `mffer` webapp

The `mffer` webapp is at https://mffer.org.

The webapp should be intuitive. If additional explanation is required here for
proper use, that is due to limitations of the developers, not the users; please
consider [filing an issue](https://github.com/therealchjones/mffer/issues) if
something is unclear.

Using a version of the webapp you deploy yourself (rather than the one at
https://mffer.org) is described in the
[Deploying the webapp](Development.md#deploying-the-webapp) section of the
[Development guide](Development.md).

Please see also the [brief `mffer` webapp manual](webapp.md)

## Using the `mffer` command line tools

The `mffer` command line tools obtain the latest version of Marvel Future Fight,
extract its usable data, process the data into a format suitable for human
review or computer use, and deploy a webapp that presents the data to users for
review and interaction. This may be useful for putting a copy of the webapp on a
different server, for reviewing the raw data, or for testing when changing the
underlying code. It is not necessary to use the command line tools to
[just use the webapp yourself](https://mffer.org).

### Obtaining the `mffer` command line tools

While the `mffer` command line tools can be downloaded from GitHub and built (see the
[development guide](Development.md) for details), it is probably easier to
download the [latest release of `mffer`](https://github.com/therealchjones/mffer/releases/latest) for your platform.

### Downloading a Release

"Releases" of `mffer` correspond to sets of files that are designed to be
"complete" in that any changes in them are designed to work together, have
documentation that appropriately describes them, and have pre-built
[versions](#versioning) that can be downloaded and run without further building
or customization at the source code level.
[Download the latest release from GitHub](https://github.com/therealchjones/mffer/releases)
for your platform of choice:

|                                     |                                     |
| ----------------------------------- | ----------------------------------- |
| `mffer-`_`version`_`-linux-x64.zip` | Linux binary release                |
| `mffer-`_`version`_`-net5.0.zip`    | platform-independent binary release |
| `mffer-`_`version`_`-osx-x64.zip`   | macOS/OS X binary release           |
| `mffer-`_`version`_`-win-x64.zip`   | Windows binary release              |

Unzip the files into a directory of your choice, and run as described in [usage](#usage).

### Installation

No installation is needed or provided. Release packages include all necessary
files in a single directory. Alternatively, cloning the GitHub repository into
a directory _`mffer`_ and building the tools results in the individual tools residing at the following
paths:

|               |                                      |
| ------------- | ------------------------------------ |
| `apkdl`       | _`mffer`_`/src/scripts/apkdl`        |
| `autoanalyze` | _`mffer`_`/src/scripts/autoanalyze`  |
| `mffer`       | _`mffer`_`/bin/Debug/net5/mffer.dll` |

### Requirements

The `mffer` tool itself does not require any other specific software. It will
run on a system that
[supports .NET 5.0](https://github.com/dotnet/core/blob/main/release-notes/5.0/5.0-supported-os.md),
but no .NET (or Mono) runtime needs to be separately installed.

The other tools, `apkdl` and `autoanalyze`, have a few other requirements:

-   POSIX-like typical development environment (required for `apkdl` and
    `autoanalyze`)
-   Python 3 (required for `apkdl`)
-   [Ghidra](https://github.com/NationalSecurityAgency/ghidra)
    (required for `autoanalyze`)
-   Java 11 runtime or SDK
    (required for Ghidra)

macOS and most Linux distributions satisfy the needs for the "typical
development environment"; Windows requires additional POSIX-like software such
as Git Bash or Cygwin. (In addition to the defined
[POSIX utilities](https://pubs.opengroup.org/onlinepubs/9699919799/), `tar`,
`mktemp`, `git`, and other common utilities are used.) Most modern systems
require installation of a Java runtime (or SDK); we recommend the "Temurin" OpenJDK 11
distribution freely available from
[Adoptium.net](https://adoptium.net/?variant=openjdk11&jvmVariant=hotspot).

Additionally, other programs are obtained and run by the `apkdl` and
`autoanalyze` scripts, so the system on which they are run must support these
programs, though the programs themselves do not need to be separately installed.

## Usage

The project is currently likely to be of utility only to developers (however you may define that). Detailed usage instructions and explanations for the individual components are documented in [the above component documents](#about-mffer), with an overall workflow in [USAGE](USAGE.md). Briefly:

```shell
$ mffer --outputdir output_directory
```

Then import the resulting CSV file into the webapp.

Full options and further descriptions of individual commands can be found in
their corresponding pages: [`apkdl`](apkdl.md),
[`autoanalyze`](autoanalyze.md), [`mffer`](mffer.md), and
[the webapp](webapp.md).

### Obtaining and Extracting the Data Files

Follow the workflow in [USAGE](USAGE.md) to obtain and extract the data files into `data/mff-device-files-`_`version`_ directories.

### Exploring the Data

The files in `data/mff-device-files`_`version`_ are copied directly from an emulated Android device, and exploring these are the best way to identify previously unprocessed data.

### Exploring the Code

While a great deal of information may be accessible via the raw files in
`data/mff-device-files`_`version`_, the majority of code for running the game,
including algorithms and use of the data, are not easily evaluated directly.
While more details and specifics are given in
[The Structure of Marvel Future Fight](mff.md), much of the data you'll want
to review is in a file deep within the device files directory structure named
`libil2cpp.so`, and this must be further processed before being further
evaluated.

### Using and Presenting the Data

Upload the results using the webapp.

### Data Workflow

1.  Use `mffer` to download the latest Marvel Future Fight data files

2.  Use `mffer` to process the downloaded files:

    ```shell
    $ cd ..
    $ dotnet run mffer.dll --datadir data --outputdir output
    ```

    `mffer` will take a potentially great deal of time to load the files from
    the `data` directory, process them, and write new files to the
    `output`directory. When complete, the `output` directory will contain
    _`version`_`.json` for each version of the game found in `data`, large files
    with amalgamated data from each version's files. It will also have one or
    more `roster-`_`version`_`.csv` files containing information about the
    playable characters in the game.

3.  Import `roster-`_`version`_`.csv` into Google Sheets to explore and use it in
    a webapp.

### Analysis Workflow

1.  Use `apkdl` to download and extract the latest Marvel Future Fight program
    files:

    ```shell
    $ cd mffer/src
    $ ./apkdl -o ../data
    ```

    It will likely be several minutes before any output is displayed in the
    terminal; if you'd like a few brief "status" messages while waiting to report
    the current steps in the process, add the `-v` option. For example:

    ```shell
    $ ./apkdl -v -o ../data
    Getting MFF from the Google Play Store...
    Enter a Google account username and password to download MFF.
    (You'll need an app password to allow access to this program.)
    Google Email:
    ```

    Adding `-v` again will add a great deal more output in the "debug" style,
    including echoing all the shell commands and printing the output
    of other utilities that are called.

2.  Use `autoanalyze` to create and populate a ghidra project with this version
    of Marvel Future Fight's program code. More details are available in [The
    Structure of Marvel Future Fight](mff.md).

## Using the `mffer` library

`mffer` is not built as a shareable library, but the source can be used for
development of other tools. See the [development guide](Development.md) for details.

## Reviewing & changing `mffer` code

Source code for the `mffer` project is available [on
GitHub](https://github.com/therealchjones/mffer). Details regarding the code,
from high-level design to appropriate indentation (tabs), are in the
[Development guide](Development.md).

## See also

### Brief manuals

-   [`autoanalyze`](autoanalyze.md)
-   [`apkdl`](apkdl.md)
-   [`mffer`](mffer.md)
-   [`mffer` webapp](webapp.md)

### Guides & References

-   [The Structure of Marvel Future Fight](mff.md)
