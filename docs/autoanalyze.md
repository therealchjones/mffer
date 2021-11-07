# `mffer` `autoanalyze`

Creating a framework for further Marvel Future Fight exploration

## Synopsis

```shell
$ autoanalyze [-v] [-v] -i input_directory -o output_directory
$ autoanalyze -h
```

## Description

`autoanalyze` automates the complicated process of extracting data structure
information from the Marvel Future Fight program code, importing it
into a new ghidra project and performing ghidra auto-analysis, as well as
decompiling Java bytecode, to prepare for
further (manual) code exploration and analysis.

## Options

|                            |                                                                          |
| -------------------------- | ------------------------------------------------------------------------ |
| `-i` _`input_directory`_   | Specify the directory in which device files are stored.                  |
| `-o ` _`output_directory`_ | Specify the directory in which to store the new ghidra project.          |
| `-v`                       | Output more information when running. May be specified 0, 1, or 2 times. |
| `-h`                       | Output brief usage instructions but take no other action.                |

## Extended Description

`autoanalyze` uses [Il2CppInspector](https://github.com/djkaty/Il2CppInspector)
to prepare data structure information (C types and function signatures) from the
device files extracted by [`autoextract`](autoextract.md). It then creates a
new [ghidra](https://ghidra-sre.org) project, imports the binary application
data, applies the information from Il2CppInspector, and performs a ghidra
auto-analysis. Finally, `autoextract` uses
[JADX](https://github.com/skylot/jadx) to decompile the Java bytecode used for
small parts of the Marvel Future Fight package into source files.

`autoanalyze` uses ghidra's `analyzerHeadless` mode to perform these processes
without a GUI, and this ends up being significantly faster than importing these
items manually, even if the point-and-click tasks themselves are minimal.

With a minimum of pre-installed software, `autoanalyze` will obtain the
remainder of necessary software. `autoanalyze` installs software into temporary
directories in an attempt to minimize changes to its host system, but does not
use a `chroot` jail or other mechanisms to truly isolate itself.

By default, `autoanalyze` prints only errors. To add brief informational
messages about the current step in the process, add the `-v` option. Adding the
option again enables "debug" output that includes echoing all shell commands in
`autoanalyze` and printing the default output from each individual tool called.
Adding further `-v` options has no effect.

`autoanalyze` evaluates files within _`input_directory`_, which is expected to
contain some subset of an Android filesystem as created by the `autoextract`
program, and is likely named `mff-device-files-`_`version`_ for the version of
Marvel Future Fight it contains.

The final product created by `autoanalyze` are directories named
`mff-ghidra-`_`version`_ and `mff-jadx-`_`version`_ within the directory
_`output_directory`_, where _`version`_ is determined from the version of Marvel
Future Fight evaluated. Within `mff-ghidra-`_`version`_ are files and
directories used by the new ghidra project, as well as multiple log files
created during the import and processing steps. `mff-jadx-`_`version`_ contains the
decompiled Java code from the device-independent portion of the application.

## Requirements

-   POSIX-compliant Unix-like environment for which all the used
    programs are available (likely macOS/OS X, Windows with Cygwin or
    another POSIX layer, or Linux).
-   [ghidra](https://ghidra-sre.org)
-   Java runtime (required by ghidra); consider
    [AdoptOpenJDK](https://adoptopenjdk.net)
-   A reasonable machine upon which to run these; ghidra can be quite resource
    intensive.

## See also

-   [`autoextract`](autoextract.md)
