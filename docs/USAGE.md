# Using `mffer`

## Introduction

There are several possible uses for the `mffer` project. A few are readily
apparent, and the workflows for those are described here, with references to
related documents as needed. In brief, these are:

-   [Using the `mffer` webapp](#using-the-mffer-webapp) to review Marvel Future
    Fight data
-   [Using the `mffer` command line tools](#using-the-mffer-command-line-tools)
    to extract, analyze, or summarize Marvel Future Fight data

Additionally, `mffer` code may be useful to those trying to explore Marvel
Future Fight, explore similar apps, deploy a custom version of the webapp, or
contribute to `mffer` itself. For these topics, refer to the [development
guide](Development.md).

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
underlying code. It is not necessary to use the command line tools to
[just use the webapp yourself](https://mffer.org).

### Obtaining the `mffer` command line tools

While the `mffer` command line tools can be downloaded from GitHub and built (see the
[development guide](Development.md) for details), it is probably easier to
download the
[latest release of `mffer`](https://github.com/therealchjones/mffer/releases/latest)
for your platform.

### Installation

No installation is needed or provided. Release packages include all necessary
files in a single directory.

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
[Adoptium.net](https://adoptium.net/temurin/releases/?version=11).

Additionally, other programs are downloaded and run by the `apkdl` and
`autoanalyze` scripts, so the system on which they are run must support these
programs, though the programs themselves do not need to be separately installed.

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

1.  Use [`apkdl`](apkdl.md) to download and extract the latest Marvel Future Fight program
    files:

    ```
    $ cd mffer/
    $ ./apkdl -o ../data
    ```

    It may be several minutes before you are prompted for a Google
    username and
    [app password](https://support.google.com/accounts/answer/185833).

2.  Use [`autoanalyze`](autoanalyze.md) to create and populate a ghidra project
    with this version of Marvel Future Fight's program code:

    ```
    $ cd mffer/
    $ ./autoanalyze -i ../data -o ../output
    ```

    This may take several hours to complete.

3.  Reverse engineer as desired. Many more details are available in
    [The Structure of Marvel Future Fight](mff.md) and elsewhere.

## See also

### Brief manuals

-   [`apkdl`](apkdl.md)
-   [`mffer`](mffer.md)
-   [`mffer` webapp](webapp.md)

### Guides & References

-   [The mffer Development Guide](Development.md)
-   [The Structure of Marvel Future Fight](mff.md)
