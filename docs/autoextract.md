# `mffer` `autoextract`

Obtaining and extracting Marvel Future Fight data files

## Synopsis

```shell
$ autoextract [-v] [-v] -o output_directory
$ autoextract -h
```

## Description

`autoextract` semi-automates the complicated process of installing the latest version of Marvel Future Fight from the Play Store, reinstalling it on a rootable emulated Android device, and downloading the game-related files that can be processed by `mffer` to extract usable data.

## Options

|                           |                                                                                                                   |
| ------------------------- | ----------------------------------------------------------------------------------------------------------------- |
| `-o `_`output_directory`_ | Specify the directory in which to place the extracted files. Existing files of the same name will be overwritten. |
| `-v`                      | Output more information when running. May be specified 0, 1, or 2 times.                                          |
| `-h`                      | Output brief usage instructions but take no other action.                                                         |

## Extended Description

`autoextract` semi-automatically creates a package of data files from the latest
release of Marvel Future Fight. It requires user interaction at some points in
the process to perform functions on Android emulators. With a minimum of
pre-installed software, `autoextract` will obtain the remainder of necessary
software. `autoextract` installs software into temporary directories in an
attempt to minimize changes to its host system, but does not use a `chroot` jail
or other mechanisms to truly isolate itself.

By default, `autoextract` prints information only when giving instructions for user interaction. To add brief informational messages about the current step in the process, add the `-v` option. Adding the option again enables "debug" output that includes echoing all shell commands in `autoextract` and printing the default output from each individual tool called. Adding further `-v` options has no effect.

The final product created by `autoextract` is a directory named
`MFF-device-files-`_`version`_, a copy of all Marvel Future Fight files
installed by the game on Android, in a directory hierarchy copied directly from
the Android emulator's filesystem.

## Requirements

-   POSIX-compliant Unix-like environment for which all the used
    programs are available (likely macOS/OS X, Windows with Cygwin or
    another POSIX layer, or Linux). Of specific note, the Android Virtual
    Devices used may not run correctly on emulated systems such as
    Parallels or VirtualBox.
-   Internet connection with access to Google developer tools, Google
    Play Store, Netmarble servers, and Facebook
-   An installed version of Android command-line tools (also included in
    Android Studio)
-   Java (required by Android command-line tools)
-   A Google account (to log into the Play store)
-   A Facebook or Google account to which your Marvel Future Fight game
    is synchronized (to download personal game data)
-   A reasonable machine upon which to run these; the emulators require
    a few GB of RAM just by themselves, for instance

## See Also

Other concepts, examples, and workflows including `autoexec` are in the [Usage guide](USAGE.md).
