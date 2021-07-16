# `mffer`

Marvel Future Fight extraction & reporting

```shell
$ dotnet run mffer --datadir data_directory --outputdir output_directory
$ dotnet run mffer -h
```

## Description

`mffer` is the command-line workhorse of the
[`mffer` project](https://github.com/therealchjones/mffer). Given files
extracted from an installation of
[Marvel Future Fight](http://www.marvelfuturefight.com) (i.e., by
[`autoextract`](autoextract.md)), `mffer` will parse the files into
JSON-formatted data files that (though quite large) are human-readable and
machine-evaluable. `mffer` additionally produces CSV files of limited data for
each version that can be viewed in spreadsheet applications (such as
[Google Sheets](https://sheets.google.com)) or used in the
[`mffer` webapp](webapp.md).

## Options

|                                        |                                                                                                                    |
| -------------------------------------- | ------------------------------------------------------------------------------------------------------------------ |
| `--datadir ` _`data_directory`_        | Specify the directory containing Marvel Future Fight data files to be processed                                    |
| `--outputdir ` _`output_directory`_    | Specify the directory in which to place created files. If files of the same names exist, they will be overwritten. |
| `-h`, `--help`, `-?`, `/?`, `/h`, `/H` | Print brief usage instructions.                                                                                    |

## Extended Description

`mffer` evaluates files from one or more versions of Marvel Future Fight under
_`data_directory`_. Though some flexibility is tolerated, the directory
hierarchy beneath _`data_directory`_ is expected to consist of one or more
directories named for a version of Marvel Future Fight, each of which is a
subset of an Android device filesystem where Marvel Future Fight is installed.
(Not coincidentally, this corresponds to the directory tree created by
[`autoextract`](autoextract.md).)

Importing and processing the data can take a long time. Once completed, `mffer`
will create _`version`_`.json` in _`output_directory`_, a JSON-formatted file
including the usable data from each version's data files. This file is very
large, but is human-readable. `mffer` will also create
`roster-`_`version`_`.csv` for each version, character-delimited files including
the data for the playable characters in the game. The CSV files can be imported
for use into the [`mffer` webapp](webapp.md). Both the JSON files and the CSV
files will overwrite any files in _`output_directory`_ with the same names.

## Requirements

`mffer` is built with .NET 5 and requires the
[.NET 5 SDK](https://dotnet.microsoft.com/download) to be installed.
Namely, the `dotnet` command line interface is required to run the program.
Additionally, `mffer` is most useful in processing output from `autoextract`.
While `autoextract` is not strictly required for the functionality of `mffer`
(and thus neither are its many requirements), obtaining the data from a Marvel
Future Fight installation without using `autoextract` is outside the scope of
this document.

## See Also

Other concepts, examples, and workflows including `mffer` are in the [Usage guide](USAGE.md).
