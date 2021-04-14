# mffer

Marvel Future Fight extraction & reporting

```shell
$ dotnet run mffer --datadir data_directory --outputdir output_directory
$ dotnet run mffer -h
```

- [Description](#description)
- [Options](#options)
- [Extended Description](#extended-description)
- [Requirements](#requirements)

## Description

`mffer` is the command-line workhorse of the [`mffer`
project](https://github.com/therealchjones/mffer). Given files extracted from an
installation of [Marvel Future Fight](http://www.marvelfuturefight.com) (i.e.,
by [`autoextract`](autoextract.md)), `mffer` will parse the files into a single
JSON-formatted data file that (though quite large) is human-readable and
machine-evaluable. `mffer` additionally produces CSV files of limited data for
each version that can be viewed in spreadsheet applications (such as [Google
Sheets](https://sheets.google.com)) or used in the [`mffer` webapp](webapp.md).

## Options

|                                     |                                                                                                                    |
| ----------------------------------- | ------------------------------------------------------------------------------------------------------------------ |
| `--datadir ` _`data_directory`_     | Specify the directory containing Marvel Future Fight data files to be processed                                    |
| `--outputdir ` _`output_directory`_ | Specify the directory in which to place created files. If files of the same names exist, they will be overwritten. |
| -h, --help, -?, /?, /h, /H          | Print brief usage instructions.                                                                                    |

## Extended Description

`mffer` evaluates asset files from one or more versions of Marvel Future Fight
under _`data_directory`_. Though some flexibility is tolerated, the directory
hierarchy beneath _`data_directory`_ is expected to consist of one or more
directories named for a version of Marvel Future Fight, each of which in turn
contains a directory named `assets` containing JSON-formatted files representing
Unity assets for that version. (Not coincidentally, this corresponds to the
directory tree created by [`autoextract`](autoextract.md).) [N.B.: No validation
of the "version name" in the parent directory of `assets` is performed; the
version is considered to be the longest string in the directory name starting
with a digit.]

Importing and processing the assets can take a long time. Once completed,
`mffer` will create `Marvel Future Fight.json` in _`output_directory`_, a
JSON-formatted file including the usable data from all versions' asset files.
This file is very large, but is human-readable. `mffer` will also create
`roster-`_`version`_`.csv` for each version _`version`_, character-delimited
files including the data for the playable characters in the game. The CSV files
can be imported for use into the [`mffer` webapp](webapp.md). Both the JSON file
and the CSV files will overwrite any files in _`output_directory`_ of the same
names.

## Requirements

`mffer` is built with .NET Core 3.1 and requires the .NET Core 3.1 SDK to be
installed. Namely, the `dotnet` command line interface is required to run the
program. Additionally, `mffer` is most useful in processing output from
`autoextract`. While `autoextract` is not strictly required for the
functionality of `mffer` (and thus neither are its many requirements), obtaining
and structuring the data from Marvel Future Fight to match the output of
`autoextract` is somewhat more cumbersome.
