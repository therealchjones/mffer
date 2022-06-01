# `mffer` documentation

## Marvel Future Fight

[Marvel Future Fight](http://www.marvelfuturefight.com/) (MFF) is a mobile role-playing game by [NetMarble](https://company.netmarble.com/) set in the extended [Marvel](https://www.marvel.com/) multiverse. It is made with many industry-standard tools, including programming in Java and C# for Unity (using il2cpp), packaged (or at least delivered) as split APKs for Android from the Google Play Store, and using Facebook and NetMarble servers for user and game data storage. As such, even if you don't play MFF, the descriptions of techniques used in this project for exploring those many components may contain some useful knowledge.

[Marvel Future Fight](http://www.marvelfuturefight.com/) is a mobile (iOS & Android) online role-playing game by [NetMarble](https://company.netmarble.com/). It's set in the extended [Marvel](https://www.marvel.com/) multiverse and has more than 200 characters to collect and modify with dozens of different resources, and enough game modes to make mastering all of them nigh impossible.

As such, the game has a large amount of data about the characters, resources, stores, game modes and levels, and actions, even before taking into account the variations between individual players. Although there is information available from those who have significant experience playing the game, objective quantitative data is rarely documented well and is of uncertain provenance.

## This Project

This project is intended to facilitate analysis of [Marvel Future Fight](#marvel-future-fight) and provide access to the data it uses for game play. This is almost certainly against the [NetMarble Terms of Service](https://help.netmarble.com/terms/terms_of_service_en?locale=&lcLocale=en) as well as those of multiple affiliates.

The project currently includes multiple components:

-   a [shell script](apkdl.md) to obtain the Marvel Future Fight program files
-   a [shell script](autoanalyze.md) to decompile and evaluate the
    program files
-   a [.NET console app](mffer.md) to obtain and parse the Marvel Future Fight data files into an open and usable format
-   a [web app](webapp.md) to present and use the game data

The objectives of this umbrella project are:

-   obtain verifiable objective quantitative data about the game, typically using reverse engineering and related methods
-   make the data easily usable for decision making necessary to play the game effectively and efficiently
-   compare changes in the data between different releases/versions of the game
-   easily track important player-specific data to evaluate progress and plan modifications

## Versioning

## Requirements

## Installation

### Downloading a Release

"Releases" of `mffer` correspond to sets of files that are designed to be
"complete" in that any changes in them are designed to work together, have
documentation that appropriately describes them, and have pre-built
[versions](#versioning) that can be downloaded and run without further building
or customization at the source code level.
[Download the latest release from GitHub](https://github.com/therealchjones/mffer/releases)
for your platform of choice:

|                                     |                                     |
| ----------------------------------- | ----------------------------------- |
| `mffer-`_`version`_`-linux-x64.zip` | Linux binary release                |
| `mffer-`_`version`_`-net5.0.zip`    | platform-independent binary release |
| `mffer-`_`version`_`-osx-x64.zip`   | macOS/OS X binary release           |
| `mffer-`_`version`_`-win-x64.zip`   | Windows binary release              |

Unzip the files into a directory of your choice, and run as described in [usage](#usage).

### Cloning the GitHub Repository

The most up-to-date changes can be obtained by cloning the git repository and
building the software yourself. Details and requirements for doing so are
described in the [Development guide](./Development.md).

## Usage

The project is currently likely to be of utility only to developers (however you may define that). Detailed usage instructions and explanations for the individual components are documented in [the above component documents](#this-project), with an overall workflow in [USAGE](USAGE.md). Briefly:

```shell
$ mffer --outputdir output_directory
```

Then import the resulting CSV file into the webapp.

Full options and further descriptions of individual commands can be found in
their corresponding pages: [`apkdl`](apkdl.md),
[`autoanalyze`](autoanalyze.md), [`mffer`](mffer.md), and
[the webapp](webapp.md).

### Obtaining and Extracting the Data Files

Follow the workflow in [USAGE](USAGE.md) to obtain and extract the data files into `data/mff-device-files-`_`version`_ directories.

### Exploring the Data

The files in `data/mff-device-files`_`version`_ are copied directly from an emulated Android device, and exploring these are the best way to identify previously unprocessed data.

### Exploring the Code

While a great deal of information may be accessible via the raw files in
`data/mff-device-files`_`version`_, the majority of code for running the game,
including algorithms and use of the data, are not easily evaluated directly.
While more details and specifics are given in
[The Structure of Marvel Future Fight](mff.md), much of the data you'll want
to review is in a file deep within the device files directory structure named
`libil2cpp.so`, and this must be further processed before being further
evaluated.

### Using and Presenting the Data

Upload the results using the webapp.

## Contributing

I welcome outside contributions, comments, questions, concerns, pull requests,
and so forth. You can learn about the best ways to contribute by reading
[CONTRIBUTING](CONTRIBUTING.md). You can also email me at <chjones@aleph0.com>.

## Problems & possible workarounds

### Android emulator does not recognize internet connection

This may occur when the host machine is connected to certain shared networks; it
happens when I attempt to use a "guest" network in one workplace. Switching to a
different (perhaps, more properly configured) network corrects the problem for
me. It may also be useful to set the _host_ machine's DNS server to a known IP
(such as 8.8.8.8), as suggested in
[this StackOverflow question](https://stackoverflow.com/questions/42736038/android-emulator-not-able-to-access-the-internet).
