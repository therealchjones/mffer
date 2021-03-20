# mffer

Marvel Future Fight Extraction & Reporting

## Marvel Future Fight

[Marvel Future Fight](http://www.marvelfuturefight.com/) is a mobile (iOS & Android) online role-playing game by [NetMarble](https://company.netmarble.com/). It's set in the extended [Marvel](https://www.marvel.com/) multiverse and has more than 200 characters to collect and modify with dozens of different resources, and enough game modes to make mastering all of them nigh impossible.

As such, the game has a large amount of data about the characters, resources, stores, game modes and levels, and actions, even before taking into account the variations between individual players. Although there is information available from those who have significant experience playing the game, objective quantitative data is rarely documented well and is of uncertain provenance.

The objectives of this umbrella project are:

-   obtain verifiable objective quantitative data about the game, typically using reverse engineering and related methods
-   make the data easily usable for decision making necessary to play the game effectively and efficiently
-   compare changes in the data between different releases/versions of the game
-   easily track important player-specific data to evaluate progress and plan modifications

As this project includes evaluation of the binary distributions of the game, which may be prohibited by the company's terms of service, this is a private project not to be concurrently shared with the public. Where data and methods can be shared safely without compromising the future of the project, they should be.

## Usage

### Extracting the Data

### Exploring the Data

### Exploring the Code

    $ dotnet ./Il2CppDumper.dll ~/Development/Marvel\ Future\ Fight/device-files/MFF-device-6.7.0/data/app/\~~bEMNFRBZWig1c0nTBK2-Pg==/com.netmarble.mherosgb-XbORIH4ZtkJYZrkO7UlUOg==/lib/arm/libil2cpp.so ~/Development/Marvel\ Future\ Fight/device-files/MFF-device-6.7.0/data/media/0/Android/data/com.netmarble.mherosgb/files/il2cpp/Metadata/global-metadata.dat ~/Development/Marvel\ Future\ Fight/il2cppdumper/MFF-il2cppdumper-6.7.0

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
