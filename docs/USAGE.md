# Using `mffer`

There are several possible uses for the `mffer` project. A few are readily
apparent, and the workflows for those are described here, with references to
related documents as needed. In brief, these are:

-   [Using the `mffer` webapp](#using-the-mffer-webapp) to review Marvel Future Fight data
-   [Using the `mffer` command line tools](#using-the-mffer-command-line-tools) to extract and report Marvel Future Fight
    data
-   [Using the `mffer` library](#using-the-mffer-library) to develop a custom program

Additionally, `mffer` code may be useful to those trying to explore Marvel
Future Fight, explore similar apps, or contribute to `mffer` itself. For these
topics, refer to the [Development guide](Development.md).

## Using the `mffer` webapp

The `mffer` webapp is at https://mffer.org.

The webapp should be intuitive. If additional explanation is required here for
proper use, that is due to limitations of the developers, not the users; please
consider [filing an issue](https://github.com/therealchjones/mffer/issues) if
something is unclear.

Please see also the [brief `mffer` webapp manual](webapp.md)

## Using the `mffer` command line tools

The `mffer` command line tools obtain the latest version of Marvel Future Fight,
extract its usable data, process the data into a format suitable for human
review or computer use, and deploy a webapp that presents the data to users for
review and interaction. This may be useful for putting a copy of the webapp on a
different server, for reviewing the raw data, or for testing when changing the
underlying code. It is not necessary to use the command line tools to [just use
the webapp yourself](https://mffer.org).

### Obtaining the `mffer` command line tools

The `mffer` command line tools can be downloaded from GitHub and built; see the
[Development guide](Development.md) for details.

### Installation

No installation is needed. After cloning the GitHub repository into a directory
_`mffer`_ and building the tools (as described in the [Development
guide](Development.md)), the tools are available at the following paths:

|               |                                               |
| ------------- | --------------------------------------------- |
| `autoextract` | _`mffer`_`/src/autoextract`                   |
| `mffer`       | _`mffer`_`/bin/Debug/netcoreapp3.1/mffer.dll` |

### Requirements

-   POSIX sh and typical development environment
-   [Android Studio](https://developer.android.com/studio/) or standalone
    [Android command-line
    tools](https://developer.android.com/studio/#command-tools)
-   Java runtime or SDK
    (required by standalone Android command-line tools but included in Android Studio)
-   [UABE](https://github.com/DerPopo/UABE)
-   .NET Core 3.1 SDK

macOS and most Linux distributions satisfy the needs for the initial
environment. (In addition to the defined [POSIX
utilities](https://pubs.opengroup.org/onlinepubs/9699919799/), `tar` and
`mktemp` are used.) They may
require installation of a Java runtime (or SDK) if one is
not already installed. The Java requirement is for the Android command line
tools (unless using the command line tools within Android Studio), but Java may be useful for other items of interest to those using mffer,
such as the [ghidra disassembler](https://github.com/NationalSecurityAgency/ghidra). We recommend the OpenJDK 11 distribution freely
available from (AdoptOpenJDK)[AdoptOpenJDK.com] at
https://adoptopenjdk.net/releases.html?variant=openjdk11&jvmVariant=hotspot.

The [Android command-line
tools](https://developer.android.com/studio/command-line] are part of Android
Studio, but may also be obtained separately from the larger application at
https://developer.android.com/studio#command-tools. This basic package includes
`sdkmanager` which `autoextract` will automatically use to update the
command-line tools themselves as well as obtain temporary copies of all the
other Android SDK packages required to run the Android emulator and extract the
Marvel Future Fight data from it. `autoextract` will run an Android emulator
automatically; unfortunately, this will not likely work within another emulator
or virtual machine such as VirtualBox or Parallels. UABE is required for the
initial processing of the MFF data after it's been extracted from the Android
systems. UABE requires a Microsoft Windows environment, but works adequately
within a virtual machine.

### Workflow

1. Use `autoextract` to download and extract the latest Marvel Future Fight data
   files:

    ```shell
    $ autoextract -o output_directory
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
    including echoing all the shell commands and printing the "verbose" output
    for other utilities that are called.

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

    Similar steps will occur a few more times; follow the directions to complete
    obtaining and extracting the Marvel Future Fight files, which will be placed
    into the _`output_directory`_`/MFF-data-`_`version`_ directory.

2. Use `mffer` to process the extracted files:

    ```shell
    $ dotnet run mffer.dll --datadir data_directory --outputdir output_directory
    ```

    where _`data_directory`_ is the directory containing the files created by
    `autoextract` (which was, of course, labelled _`output_directory`_ in that
    step).

    `mffer` will take a potentially great deal of time to load the files from
    _`data_directory`_, process them, and write new files to
    _`output_directory`_. When complete, _`output_directory`_ will contain
    `Marvel Future Fight.json`, a large file with the amalgamated data from all
    the _`data_directory`_ files. It will also have one or more
    `roster-`_`version`_`.csv` files containing information about the playable
    characters in the game.

3. Import `roster-`_`version`_`.csv` into Google Sheets to explore and use it in
   a webapp.

## Using the `mffer` library

`mffer` is not built as a shareable library, but the source can be used for
development of other tools. See the [Development guide](Development.md) for details.

## Reviewing & changing `mffer` code

Source code for the `mffer` project is available [on
GitHub](https://github.com/therealchjones/mffer). Details regarding the code,
from high-level design to appropriate indentation (tabs), are in the
[Development guide](Development.md).

## See also

### Brief manuals

-   [`autoextract`](autoextract.md)
-   [`mffer`](mffer.md)
-   [`mffer` webapp](webapp.md)
