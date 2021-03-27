# mffer: Marvel Future Fight Extraction & Reporting

This is a _comprehensive_ overview of the [mffer](https://github.com/therealchjones/mffer) project. A more concise [README document](../README.md) is available in the [root directory](../).

## Marvel Future Fight

[Marvel Future Fight](http://www.marvelfuturefight.com/) (MFF) is a mobile role-playing game by [NetMarble](https://company.netmarble.com/) set in the extended [Marvel](https://www.marvel.com/) multiverse. It is (or appears to be) made with many industry-standard tools, including programming in Java and C# for Unity (using il2cpp), packaged (or at least delivered) as split APKs for Android from the Google Play Store, and using Facebook and NetMarble servers for user and game data storage. As such, even if you don't play MFF, the descriptions of techniques used in this project for exploring those many components may contain some useful knowledge.

## About mffer

This project is intended to facilitate analysis of [Marvel Future Fight](#marvel-future-fight) and provide access to the data it uses for game play. This is almost certainly against the [NetMarble Terms of Service](https://help.netmarble.com/terms/terms_of_service_en?locale=&lcLocale=en) as well as those of multiple affiliates.

The project currently includes multiple components:

-   a [shell script](docs/autoextract.md) to obtain the Marvel Future Fight data files
-   a [.NET console app](docs/mffer.md) to parse the data files into an open and usable format
-   a [Google Sheet and web app](docs/webapp.md) to present and use the game data

## Usage

The project is currently likely to be of utility only to developers (however you may define that). Detailed usage instructions and explanations for the individual components are documented in [the above component documents](#about-mffer). Briefly:

```
$ autoextract [-v] -o data_directory
$ dotnet run -- --datadir data_directory --outputdir output_directory
```

Then import the resulting CSV file(s) to a Google Sheet for further work.

## Contributing

I welcome outside contributions, comments, questions, concerns, pull requests, and so forth. At least, I would if this were a public project in a public repository, but because I prefer not to be booted from my favorite game, you'll likely never hear about it. However, in the hypothetical case you'd like to contribute to a project you've never heard of, you can hypothetically learn about the best way to do so by hypothetically reading [CONTRIBUTING](docs/CONTRIBUTING.md), to which you also don't have access. You can also hypothetically email me at <chjones@aleph0.com>.

## Table of Contents

-   [Marvel Future Fight](#marvel-future-fight)
-   [About mffer](#about-mffer)
-   [Usage](#usage)
-   [Contributing](#contributing)
-   [Marvel Future Fight](#marvel-future-fight-1)
-   [This Project](#this-project)
    -   [Versioning](#versioning)
-   [Requirements](#requirements)
-   [Usage](#usage-1)
    -   [Obtaining and Extracting the Data Files](#obtaining-and-extracting-the-data-files)
    -   [Exploring the Data](#exploring-the-data)
    -   [Exploring the Code](#exploring-the-code)
        -   [Il2CppDumper](#il2cppdumper)
        -   [Ghidra](#ghidra)
    -   [Using and Presenting the Data](#using-and-presenting-the-data)

## Marvel Future Fight

[Marvel Future Fight](http://www.marvelfuturefight.com/) is a mobile (iOS & Android) online role-playing game by [NetMarble](https://company.netmarble.com/). It's set in the extended [Marvel](https://www.marvel.com/) multiverse and has more than 200 characters to collect and modify with dozens of different resources, and enough game modes to make mastering all of them nigh impossible.

As such, the game has a large amount of data about the characters, resources, stores, game modes and levels, and actions, even before taking into account the variations between individual players. Although there is information available from those who have significant experience playing the game, objective quantitative data is rarely documented well and is of uncertain provenance.

The objectives of this umbrella project are:

-   obtain verifiable objective quantitative data about the game, typically using reverse engineering and related methods
-   make the data easily usable for decision making necessary to play the game effectively and efficiently
-   compare changes in the data between different releases/versions of the game
-   easily track important player-specific data to evaluate progress and plan modifications

As this project includes evaluation of the binary distributions of the game, which may be prohibited by the company's terms of service, this is a private project not to be concurrently shared with the public. Where data and methods can be shared safely without compromising the future of the project, they should be.

## This Project

### Versioning

Semantic versioning (from [#52](https://github.com/therealchjones/mffer/issues/52)) https://semver.org
initial version, per recommendations, will be 0.1.0

Using Semantic Versioning 2.0.0

"If all of this sounds desirable, all you need to do to start using Semantic Versioning is to declare that you are doing so and then follow the rules. Link to this website from your README so others know the rules and can benefit from them."

"Major version zero (0.y.z) is for initial development. Anything MAY change at any time. The public API SHOULD NOT be considered stable."

Briefly summarize (using exactly or a slight modification of the summary)

## Requirements

## Usage

### Obtaining and Extracting the Data Files

### Exploring the Data

### Exploring the Code

#### Il2CppDumper

    $ dotnet ./Il2CppDumper.dll ~/Development/Marvel\ Future\ Fight/device-files/MFF-device-6.7.0/data/app/\~~bEMNFRBZWig1c0nTBK2-Pg==/com.netmarble.mherosgb-XbORIH4ZtkJYZrkO7UlUOg==/lib/arm/libil2cpp.so ~/Development/Marvel\ Future\ Fight/device-files/MFF-device-6.7.0/data/media/0/Android/data/com.netmarble.mherosgb/files/il2cpp/Metadata/global-metadata.dat ~/Development/Marvel\ Future\ Fight/il2cppdumper/MFF-il2cppdumper-6.7.0

#### Ghidra

Options for ghidra:

-   MAXMEM=16G (or whatever, 2 default) ghidra\_<version>/ghidraRun [args]
-   ghidra\_<version>/support/launch.sh bg Ghidra "$MAXMEM" "" ghidra.GhidraRun "$@"
-   ghidra\_<version>/support/analyzeHeadless (!!!): see https://ghidra.re/ghidra_docs/analyzeHeadlessREADME.html

In ghidra:

-   new project->~/Development/Marvel Future Fight/ghidra/mff-ghidra-6.7.0
-   file->import file->~/Development/Marvel\ Future\ Fight/device-files/MFF-device-6.7.0/data/app/\~~bEMNFRBZWig1c0nTBK2-Pg==/com.netmarble.mherosgb-XbORIH4ZtkJYZrkO7UlUOg==/lib/arm/libil2cpp.so
-   options->check all except external symbols, set offset to 0
-   OK for results, then double click on libil2cpp.so; when asked to auto-analyze, say no
-   analysis-Auto Analyze, deselect everything, hit analyze (but won't do anything)
-   close all sub-windows of ghidra to avoid screen update pauses (listing, data type manager, Functions)
-   file->parse c source-> "eraser" (clear profile) (doesn't matter what it is, won't save that way) -> "+" -> ~/Development/Marvel Future Fight/il2cppdumper/mff-il2cppdumper-6.7.0/il2cpp.h -> parse to program -> continue, continue, dismiss, and wait a while (like, hours) (any way to speed this up, maybe no animation display?)
-   ghidra_9.2.2_PUBLIC/Ghidra/fFeatures/Decompiler/os/osx64/decompile->right-click, open, Open
    window->script manager. Manage script directories->add il2cppdumper/bin/release/netcoreapp3.1
-   In script manager, refresh, then run ghidra_with_structs.py script; Select il2cppdumper/MFF-il2cppdumper-6.7.0/script.json when prompted. This will likely automatically start decompilation
-   Auto-Analyze: select all that's not red, Analyze.
-   had to open a couple of binaries in ghidra to get them to work with osx security (demangler, decompiler)
-   Ghidra->file->save

### Using and Presenting the Data