# Using mffer

## Introduction

The mffer project develops software that creates and updates the
[mffer webapp](https://mffer.org). It is not necessary to review any of this
to [use the webapp](https://mffer.org).

The mffer tools obtain, extract, and analyze
[Marvel Future Fight](#marvel-future-fight) (MFF) program and data files,
process the resulting information into formats that are easily read by humans,
and presents that information interactively to assist with MFF game play. This
mffer User Guide describes the mffer tools and how to use them.

While even using the mffer tools may be of interest only to software
developers, data analysts, and others inclined to take apart systems and see how
they work, this guide does not discuss doing so with mffer itself. If you're
interested in extending, modifying, or improving the mffer tools, you may
prefer reading the [mffer Development Guide](Development.md).

## Marvel Future Fight

[Marvel Future Fight](http://www.marvelfuturefight.com/) (MFF) is a mobile (iOS
& Android) online role-playing game by
[Netmarble](https://company.netmarble.com/). It's set in the extended
[Marvel](https://www.marvel.com/) multiverse and has more than 200 characters to
collect and modify with dozens of different resources, and enough game modes to
make mastering all of them nigh impossible.

As such, the game has a large amount of data about the characters, resources,
stores, game modes and levels, and actions, even before taking into account the
variations between individual players. Although there is information available
from those who have significant experience playing the game, objective
quantitative data is rarely documented well and is of uncertain provenance.

## The mffer project

This project is intended to facilitate analysis of
[Marvel Future Fight](#marvel-future-fight) and provide access to the data it
uses for game play. This is almost certainly against the
[Netmarble Terms of Service](https://help.netmarble.com/terms/terms_of_service_en?locale=&lcLocale=en)
as well as those of multiple affiliates.

The project currently includes multiple components:

-   a [shell script](apkdl.md) to obtain the Marvel Future Fight program files
-   a [shell script](autoanalyze.md) to decompile and evaluate the program files
-   a [command line program](mffer.md) to obtain and parse the MFF data files
    into an open and usable format
-   a [web app](webapp.md) to present and use the game data for game play
    decision making

## Introduction

There are several possible uses for the mffer project. A few are readily
apparent, and the workflows for those are described here, with references to
related documents as needed. In brief, these are:

-   [Using the mffer webapp](#using-the-mffer-webapp) to review Marvel Future
    Fight data
-   [Using the mffer command line tools](#using-the-mffer-command-line-tools)
    to extract, analyze, or summarize Marvel Future Fight data

Additionally, mffer code may be useful to those trying to explore Marvel
Future Fight, explore similar apps, deploy a custom version of the webapp, or
contribute to mffer itself. For these topics, refer to the [development
guide](Development.md).

## Using the mffer webapp

The mffer webapp is at https://mffer.org.

The webapp should be intuitive. If additional explanation is required for proper
use, that is due to limitations of the developers, not the users; please
consider [filing an issue](https://github.com/therealchjones/mffer/issues) if
something is unclear.

## Using the mffer command line tools

The mffer command line tools obtain the latest version of Marvel Future Fight,
extract its usable data, process the data into a format suitable for human
review or computer use, and provide the data to the webapp. It is not necessary
to use the command line tools to
[just use the webapp yourself](https://mffer.org).

### Obtaining the mffer command line tools

"Releases" of mffer correspond to sets of files that are designed to be
"complete" in that any changes in them are designed to work together, they have
documentation that appropriately describes them, and they have pre-built
versions that can be downloaded and run without further building
or customization at the source code level.
[Download the latest release from GitHub](https://github.com/therealchjones/mffer/releases)
for your platform of
choice:

|                                     |                           |
| ----------------------------------- | ------------------------- |
| `mffer-`_`version`_`-linux-x64.zip` | Linux binary release      |
| `mffer-`_`version`_`-osx-x64.zip`   | macOS/OS X binary release |
| `mffer-`_`version`_`-win-x64.zip`   | Windows binary release    |

### Installation

No installation is needed or provided. Release packages include all necessary
files in a single directory. Unzip the package into a directory of your choice.

### Requirements

The mffer tool itself does not require any other specific software. It will
run on a system that
[supports .NET 5.0](https://github.com/dotnet/core/blob/main/release-notes/5.0/5.0-supported-os.md),
but no .NET or Mono runtime needs to be separately installed.

The other tools, `apkdl` and `autoanalyze`, have a few other requirements:

-   POSIX-like typical development environment (required for `apkdl` and
    `autoanalyze`)
-   Python 3 (required for `apkdl`)
-   [Ghidra](https://github.com/NationalSecurityAgency/ghidra)
    (required for `autoanalyze`)
-   [.NET 5.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/5.0)
    (required for `autoanalyze`)
-   Java 11 runtime or SDK
    (required for Ghidra)

macOS and most Linux distributions satisfy the needs for the "typical
development environment"; Windows requires additional POSIX-like software such
as Git Bash or Cygwin. (In addition to the defined
[POSIX utilities](https://pubs.opengroup.org/onlinepubs/9699919799/),
`tar`, `mktemp`, `git`, `python3`, and other common utilities are used.) Most modern systems
require installation of a Java runtime (or SDK); we recommend the "Temurin"
OpenJDK 11 distribution freely available from
[Adoptium.net](https://adoptium.net/?variant=openjdk11&jvmVariant=hotspot).

Additionally, other programs are obtained and run by the `apkdl` and
`autoanalyze` scripts, so the system on which they are run must support these
programs, though the programs themselves do not need to be separately installed.

## Usage

### Obtaining and processing the data files

```shell
$ mffer --outputdir output_directory
```

### Exploring the data

Files in the _`output_directory`_ directory include `Roster-`_`version`_`.csv`
and `mffer-`_`version`_`.json` for the current version of Marvel Future Fight.
The first is a character-delimited spreadsheet of the many characters you can
choose in MFF and their different uniforms, all with stats at different skill
levels. The JSON file is a large human- and machine-readable file that includes
all the data mffer knows how to process (such as characters, uniforms,
dictionaries, and skills) as well as readable versions of all the other
text-based data that is downloadable. The files in the
_`output_directory`_`/files` subdirectory are the raw [Unity](https://unity.com)
data files called
"[AssetBundle](https://docs.unity3d.com/Manual/AssetBundlesIntro.html)s" used by
Marvel Future Fight. Exploring these are the best way to identify previously
unprocessed data. Those not processed by mffer (including graphics, level
data, and background music) can be explored with tools like [AssetStudio](https://github.com/Perfare/AssetStudio).

### Using and presenting the data

Upload the results using the webapp.

### Exploring the code

A great deal of information may be accessible via the raw files in
_`output_directory`_`/files`, but the majority of code for running the game,
including algorithms and use of the data, are less easily evaluated directly.
More details and specifics of how the program works are given in
[The structure of Marvel Future Fight](mff.md), but much of the code you'll want to review is in a
file deep within
the program files' directory structure named `libil2cpp.so`. The mffer tools
can help facilitate this review by processing this file before you manually
evaluate it further:

1.  Use `apkdl` to download and extract the latest Marvel Future Fight program
    files:

    ```shell
    $ ./apkdl -o apk_directory
    ```

    It will likely be several minutes before any output is displayed in the
    terminal; if you'd like a few brief "status" messages while waiting to report
    the current steps in the process, add the `-v` option. For example:

    ```shell
    $ ./apkdl -v -o ../data
    ```

    ```
    Getting MFF from the Google Play Store...
    Enter a Google account username and password to download MFF.
    (You'll need an app password to allow access to this program.)
    Google Email:
    ```

    Adding `-v` again will add a great deal more output in the "debug" style,
    including echoing all the shell commands and printing the output
    of other utilities that are called.

2.  Use `autoanalyze` to create and populate a Ghidra project with this version
    of Marvel Future Fight's program code.

## See also

### Brief manuals

-   [`apkdl`](apkdl.md)
-   [`autoanalyze`](autoanalyze.md)
-   [mffer](mffer.md)
-   [the mffer webapp](webapp.md)

### Guides

-   [mffer Development Guide](Development.md)
-   [The Structure of Marvel Future Fight](mff.md)
