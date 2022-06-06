# apkdl

Obtaining Marvel Future Fight program files

## Synopsis

```shell
$ apkdl [-v] [-v] -o output_directory
$ apkdl -h
```

## Description

apkdl downloads the latest version of the Marvel Future Fight program from the
Google Play Store. This is primarily useful for further analysis and exploration
of the game program files; it does not download most of the game data used by
mffer.

## Options

|                           |                                                                          |
| ------------------------- | ------------------------------------------------------------------------ |
| `-o `_`output_directory`_ | Specify the directory in which to place the extracted files.             |
| `-v`                      | Output more information when running. May be specified 0, 1, or 2 times. |
| `-h`                      | Output brief usage instructions but take no other action.                |

## Extended Description

apkdl downloads the APK installation files for the latest
release of Marvel Future Fight. With a minimum of
pre-installed software, apkdl will obtain the remainder of necessary
software. apkdl installs software into temporary directories in an
attempt to minimize changes to its host system, but does not use a `chroot` jail
or other mechanisms to truly isolate itself.

By default, apkdl prints information only when giving instructions for
user interaction. To add brief informational messages about the current step in
the process, add the `-v` option. Adding the `-v` option again enables "debug"
output that includes echoing all shell commands in apkdl and printing
the verbose output from each individual tool called. Adding further `-v` options
has no effect.

The final product created by `apkdl` is a directory named `mff-apks-`_`version`_
within _`output_directory`_. This directory contains the `.apk` installation
files used to install the game onto an Android device.

## Requirements

-   POSIX-like environment (macOS/OS X, Windows with Cygwin or
    another POSIX layer, or Linux).
-   Python
-   git
-   Internet connection with access to the Google Play Store
-   A Google account with an
    [app password](https://support.google.com/accounts/answer/185833) (to log
    into the Play store)

## See Also

-   Other concepts, examples, and workflows including apkdl are in the
    [User Guide](USAGE.md).
