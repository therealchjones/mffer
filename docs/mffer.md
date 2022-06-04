# mffer

Marvel Future Fight exploration & reporting

```shell
$ mffer --datadir data_directory --outputdir output_directory
$ mffer -h
```

## Description

mffer is the command-line workhorse of the
[mffer project](https://github.com/therealchjones/mffer). It can obtain the
Marvel Future Fight program and data files from Netmarble, parse the files into
JSON-formatted data files that (though quite large) are human-readable and
machine-evaluable, and produce CSV files of limited data for each version that
can be viewed in spreadsheet applications (such as
[Google Sheets](https://sheets.google.com)) or used in the
[mffer webapp](webapp.md).

## Options

|                                        |                                                                 |
| -------------------------------------- | --------------------------------------------------------------- |
| `--datadir ` _`data_directory`_        | Specify the directory containing Marvel Future Fight data files |
| `--outputdir ` _`output_directory`_    | Specify the directory in which to place created files.          |
| `-h`, `--help`, `-?`, `/?`, `/h`, `/H` | Print brief usage instructions.                                 |

## Extended Description

mffer evaluates files from one or more versions of Marvel Future Fight under
_`data_directory`_. Though some flexibility is tolerated, the directory
hierarchy beneath _`data_directory`_ is expected to consist of one or more
directories named for a version of Marvel Future Fight, each of which is a
subset of an Android device filesystem where Marvel Future Fight is installed.
(Not coincidentally, this corresponds to the directory trees created by mffer
when obtaining data files or by the now-obsolete `autoextract`.)

Importing and processing the data takes some time (minutes to hours). Once
completed, mffer will create _`version`_`.json` in _`output_directory`_, a
JSON-formatted file including the usable data from each version's data files.
This file is very large, but is human-readable. mffer will also create
`Roster-`_`version`_`.csv` for each version, character-delimited files including
the data for the playable characters in the game. The CSV files can be imported
for use into the [mffer webapp](webapp.md). Both the JSON files and the CSV
files will overwrite any files in _`output_directory`_ with the same names.

## Requirements

mffer is built with .NET 5 and requires a system on which the
[.NET 5 runtime](https://dotnet.microsoft.com/download/dotnet/5.0) will work,
though the runtime itself does not need to be installed.

## See Also

Other concepts, examples, and workflows including mffer are in the
[User Guide](USAGE.md).
