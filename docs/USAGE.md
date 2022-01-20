# Using `mffer`

## Introduction

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
- [Using the `mffer` webapp](#using-the-mffer-webapp)
- [Using the `mffer` command line tools](#using-the-mffer-command-line-tools)
	- [Obtaining the `mffer` command line tools](#obtaining-the-mffer-command-line-tools)
	- [Installation](#installation)
	- [Requirements](#requirements)
	- [Workflow](#workflow)
- [Using the `mffer` library](#using-the-mffer-library)
- [Reviewing & changing `mffer` code](#reviewing--changing-mffer-code)
- [See also](#see-also)
	- [Brief manuals](#brief-manuals)
	- [Guides & References](#guides--references)

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

### Installation

No installation is needed or provided. Release packages include all necessary
files in a single directory. Alternatively, cloning the GitHub repository into
a directory _`mffer`_ and building the tools results in the individual tools residing at the following
paths:

|               |                                      |
| ------------- | ------------------------------------ |
| `autoextract` | _`mffer`_`/src/autoextract`          |
| `autoanalyze` | _`mffer`_`/src/autoanalyze`          |
| `mffer`       | _`mffer`_`/bin/Debug/net5/mffer.dll` |

### Requirements

The `mffer` tool itself does not require any other specific software. It will
run on a system that
[supports .NET 5.0](https://github.com/dotnet/core/blob/main/release-notes/5.0/5.0-supported-os.md),
but no .NET (or Mono) runtime needs to be separately installed.

The other tools, `autoextract` and `autoanalyze`, have a few other requirements:

-   POSIX-like typical development environment (required for `autoextract` and
    `autoanalyze`)
-   [Ghidra](https://github.com/NationalSecurityAgency/ghidra)
    (required for `autoanalyze`)
-   Java 11 runtime or SDK
    (required for `autoextract` and Ghidra)

macOS and most Linux distributions satisfy the needs for the "typical
development environment"; Windows requires additional POSIX-like software such
as Git Bash or Cygwin. (In addition to the defined
[POSIX utilities](https://pubs.opengroup.org/onlinepubs/9699919799/), `tar`,
`mktemp`, `git`, and other common utilities are used.) Most modern systems
require installation of a Java runtime (or SDK); we recommend the "Temurin" OpenJDK 11
distribution freely available from
[Adoptium.net](https://adoptium.net/?variant=openjdk11&jvmVariant=hotspot).

Additionally, other programs are obtained and run by the `autoextract` and
`autoanalyze` scripts, so the system on which they are run must support these
programs, though the programs themselves do not need to be separately installed.
`autoextract` will run an Android emulator to obtain and install Marvel Future
Fight. This must be run on a system that supports virtualization to allow the
emulator to run and supports running the emulator QEMU. These requirements are
met with most modern Windows, macOS/OS X, and Linux systems, but the emulator
may not run on such systems installed in virtual machines that do not support
"nested virtualization".

### Workflow

1. Use `autoextract` to download and extract the latest Marvel Future Fight data
   files:

    ```shell
    $ cd mffer/src
    $ ./autoextract -o ../data
    ```

    It will likely be several minutes before any output is displayed in the
    terminal; if you'd like a few brief "status" messages while waiting to report
    the current steps in the process, add the `-v` option. For example:

    ```shell
    $ ./autoextract -v -o ../data
    Accepting Android command line tool licenses
    Getting updated Android command line tools
    Getting Android emulator and platform tools
    Getting Android system images
    ```

    Adding `-v` again will add a great deal more output in the "debug" style,
    including echoing all the shell commands and printing the output
    of other utilities that are called.

    Once tools have been downloaded and set up, an Android emulator will start,
    and the terminal will direct you in the next steps:

    ```shell
    ************* USER INTERACTION REQUIRED *************
    On the emulator, open the Google Play Store app, sign
    in, and install the latest version of Marvel Future
    Fight. Leave the emulator running.
    ******************************************************

    Press <enter> or <return> when that is complete.
    ```

    Similar steps will occur again; follow the directions to complete
    obtaining and extracting the Marvel Future Fight files, which will be placed
    into the _`data_directory`_`/mff-device-files-`_`version`_ directory.

2. Use `mffer` to process the extracted files:

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

3. Import `roster-`_`version`_`.csv` into Google Sheets to explore and use it in
   a webapp.

4. (Optional) For further exploration of Marvel Future Fight code, use
   `autoanalyze` to create and populate a ghidra project with this version of
   Marvel Future Fight's program code. More details are available in
   [The Structure of Marvel Future Fight](mff.md).

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
-   [`autoextract`](autoextract.md)
-   [`mffer`](mffer.md)
-   [`mffer` webapp](webapp.md)

### Guides & References

-   [The Structure of Marvel Future Fight](mff.md)
