# `mffer` `autoanalyze`

Creating a framework for further Marvel Future Fight exploration

## Synopsis

```shell
$ autoanalyze [-v] [-v] -i input_directory -o output_directory
$ autoanalyze -h
```

## Description

`autoanalyze` automates the complicated process of extracting data structure
information from the Marvel Future Fight binary code, properly importing both
into a new ghidra project, and performing ghidra auto-analysis to prepare for
further (manual) code exploration and analysis.

## Options

|                           |                                                                          |
| ------------------------- | ------------------------------------------------------------------------ |
| `-i`_`input_directory`_   | Specify the directory in which device files are stored.                  |
| `-o `_`output_directory`_ | Specify the directory in which to store the new ghidra project.          |
| `-v`                      | Output more information when running. May be specified 0, 1, or 2 times. |
| `-h`                      | Output brief usage instructions but take no other action.                |

## Extended Description

`autoanalyze` uses Il2CppInspector to prepare data structure information (C
types and function signatures) from the device files extracted by
[`autoextract`](autoextract.md), even downloading and building Il2CppInspector
into a temporary directory to do so. It then creates a new ghidra project,
imports the binary application data, applies the information from
Il2CppInspector, and performs a ghidra auto-analysis.

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
created during the import and processing steps. `mff-jadx-`_`version`_ contains
decompiled java code from the device-independent portion of the application.

## Requirements

-   POSIX-compliant Unix-like environment for which all the used
    programs are available (likely macOS/OS X, Windows with Cygwin or
    another POSIX layer, or Linux).
-   ghidra
-   Java (required by ghidra)
-   A reasonable machine upon which to run these; ghidra can be quite resource
    intensive.

## See also

-   [`autoextract`](autoextract.md)