# autoanalyze

Creating a framework for Marvel Future Fight exploration

## Synopsis

```
autoanalyze [-v] [-v] -i input_directory -o output_directory
autoanalyze -h
```

## Description

autoanalyze automates the process of extracting program structure and function
from Marvel Future Fight installation packages and performing automated analysis
of the code while creating project layouts for further exploration and analysis.

## Options

|                           |                                                                          |
| ------------------------- | ------------------------------------------------------------------------ |
| `-i` _`input_directory`_  | Specify the directory in which installation files are stored.            |
| `-o` _`output_directory`_ | Specify the directory in which to store newly created project layouts.   |
| `-v`                      | Output more information when running. May be specified 0, 1, or 2 times. |
| `-h`                      | Output brief usage instructions but take no other action.                |

## Extended Description

autoanalyze uses [Il2CppInspector](https://github.com/djkaty/Il2CppInspector)
to prepare data structure information (C types, function signatures, string
references, and other items) from the Marvel Future Fight installation packages
obtained via [apkdl](apkdl.md) or similar tools. It then uses
[JADX](https://github.com/skylot/jadx) to decompress and decode the packages and
decompile the Java code used for parts of the program into source files.
Finally, autoanalyze creates a new [Ghidra](https://ghidra-sre.org) project,
imports the binary application data, applies the information from
Il2CppInspector, and performs a Ghidra auto-analysis.

autoanalyze uses Ghidra's `analyzeHeadless` mode to perform these processes
without a GUI, and this ends up being significantly faster than importing these
items manually, even if the point-and-click tasks themselves are minimal. If
Ghidra is installed somewhere (and in only one place) under `/usr/local`, this
will be found automatically; otherwise, set the `GHIDRA` environment variable to
the path of the `analyzeHeadless` script (which, in most releases, is in the
`support/` subdirectory).

With a minimum of pre-installed software (see [REQUIREMENTS](#requirements)),
autoanalyze will obtain the remainder of necessary software. autoanalyze
installs software into temporary directories in an attempt to minimize changes
to its host system, but does not use a `chroot` jail or other mechanisms to
truly isolate itself.

By default, autoanalyze prints only errors. To add brief informational
messages about the current step in the process, add the `-v` option. Adding the
option again enables "debug" output that includes echoing all shell commands in
autoanalyze and printing the output from each individual tool called.
Adding further `-v` options has no effect.

autoanalyze evaluates files within _`input_directory`_, which is expected to
contain somewhere beneath it files named `base.apk` and
`config.`_`abi`_`.apk` (for some ABI name). This may be a simple directory
containing these files, such as the `mff-apks-`_`version`_ directory made by
apkdl, some subset of an Android filesystem such as the
`mff-device-files-`_`version`_ directory created by the autoextract program,
or any other searchable file hierarchy containing these two files. If more than
one of each type of file is located, an error message is printed; the easiest
way to fix this is to choose a better subdirectory or relocate the files you
wish to analyze into a directory of their own.

The final products created by autoanalyze are directories named
`mff-ghidra-`_`version`_ and `mff-jadx-`_`version`_ within the directory
_`output_directory`_, where _`version`_ is determined from the version of Marvel
Future Fight evaluated. Within `mff-ghidra-`_`version`_ are files and
directories used by the new Ghidra project, as well as multiple log files
created during the import and processing steps. `mff-jadx-`_`version`_ contains
the decompiled Java code from the device-independent portion of the application.

In many circumstances, autoanalyze will take several hours to complete.

## Requirements

-   POSIX-like environment for which all the used programs are available (Linux,
    macOS/OS X, or Windows with Cygwin or another POSIX layer)
-   git (used to obtain automatically downloaded tools)
-   [.NET 5.0 SDK](https://dotnet.microsoft.com/download/dotnet/5.0) (required
    for building automatically downloaded tools)
-   [Ghidra](https://ghidra-sre.org)
-   Java runtime (required by Ghidra); consider
    [Temurin 11](https://adoptium.net/?variant=openjdk11&jvmVariant=hotspot)
-   A reasonable machine upon which to run these; Ghidra can be quite resource
    intensive.

## See also

-   [apkdl](apkdl.md)
-   Other concepts, examples, and workflows including autoanalyze are in the
    [User Guide](USAGE.md).
