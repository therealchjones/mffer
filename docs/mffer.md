# mffer

Marvel Future Fight exploration & reporting

```
mffer --download-assets --outputdir output_directory
mffer --datadir data_directory --outputdir output_directory
mffer -h
```

## Description

mffer is the command-line workhorse of the
[mffer project](https://github.com/therealchjones/mffer). It can obtain the
Marvel Future Fight data files from Netmarble, parse the files into
JSON-formatted data files that (though quite large) are human-readable and
machine-evaluable, and produce CSV files of limited data for each version that
can be viewed in spreadsheet applications (such as
[Google Sheets](https://sheets.google.com)) or used in the
[mffer webapp](https://mffer.org).

## Options

|                                        |                                                                 |
| -------------------------------------- | --------------------------------------------------------------- |
| `--download-assets`, `-D`              | Download new data files, rather than processing data            |
| `--datadir ` _`data_directory`_        | Specify the directory containing Marvel Future Fight data files |
| `--outputdir ` _`output_directory`_    | Specify the directory in which to place created files           |
| `-h`, `--help`, `-?`, `/?`, `/h`, `/H` | Print brief usage instructions.                                 |

## Extended Description

mffer can be used to download data files from Netmarble or to process data files
already on the local system. In download mode (`--download-assets`, `-D`), mffer
downloads data files for the latest version of Marvel Future Fight from
Netmarble servers and places them in the
_`output_directory`_`/mff-assets-`_`version_number`_ directory. The `--datadir`
option should not be used in download mode.

In processing mode, mffer evaluates files from one or more versions of Marvel
Future Fight under _`data_directory`_. Though some flexibility is tolerated,
either _`data_directory`_ or its immediate subdirectories should be named for
versions of Marvel Future Fight. Each directory containing a version should
include somewhere beneath it the data files for that version of Marvel Future
Fight. These directories are typically those produced by the now-obsolete
autoextract program, those created by mffer in download mode, or those extracted
from the filesystem of an Android device on which Marvel Future Fight is
installed.

Importing and processing the data takes some time (a few minutes to a few hours per version). Once
completed, mffer will create _`version`_`.json` in _`output_directory`_, a
JSON-formatted file including the usable data from each version's data files.
This file is very large, but is human-readable. mffer will also create
`Roster-`_`version`_`.csv` for each version, character-delimited files including
the data for the playable characters in the game. The CSV files can be imported
into the mffer webapp. Both the JSON files and the CSV
files will overwrite any files in _`output_directory`_ with the same names.

## Requirements

mffer is built with .NET 5 and requires a system on which the
[.NET 5 runtime will work](https://github.com/dotnet/core/blob/main/release-notes/5.0/5.0-supported-os.md),
though the runtime itself does not need to be installed.

## See Also

Other concepts, examples, and workflows including mffer are in the
[User Guide](USAGE.md).
