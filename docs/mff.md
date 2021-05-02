# The Structure of Marvel Future Fight

Or, On the Many Details of How I Wasted Massive Amounts of Time and the
Existentially Inconsequential Things I Learned

- [Introduction](#introduction)
- [Exploration Techniques](#exploration-techniques)
	- [Static analysis](#static-analysis)
		- [External files](#external-files)
			- [File changes](#file-changes)
				- [Version 7.0.0, change in time, personal account, not running](#version-700-change-in-time-personal-account-not-running)
- [Unity](#unity)
	- [Assets & Asset Bundles](#assets--asset-bundles)
	- [IL2CPP](#il2cpp)
- [Android](#android)
- [Marvel Future Fight](#marvel-future-fight)
	- [Useful functions](#useful-functions)
- [The `mffer` Model](#the-mffer-model)
	- [Assumptions in `mffer`](#assumptions-in-mffer)
		- [Roster & Character model](#roster--character-model)
		- [Character ID models](#character-id-models)
		- [Localization changes](#localization-changes)

## Introduction

[Marvel Future Fight](http://www.marvelfuturefight.com/) is a mobile (iOS and
Android) online role playing game published by
[NetMarble](http://netmarble.com). It is developed using
[Unity](https://unity.com) and compiled into native code for its platforms using
[IL2CPP](https://docs.unity3d.com/Manual/IL2CPP.html). The Android version is
available from the Play Store.

The programming and delivery of Marvel Future Fight are complex but accessible,
and this document attempts to gather a great deal of information describing the
exploration of Marvel Future Fight code, as well as to explicitly list some
assumptions made in the development of
[mffer](https://github.com/therealchjones/mffer). Where information is unknown
but thought to be true, such assumption is written explicitly. For definitive
information, evaluation of the code defining the information is given where
possible.

This is (or strives to be) a development document, not gameplay instructions or
advice. Although knowing minutiae of expertly playing Marvel Future Fight is not
necessary, general knowledge of the game is expected and may be necessary for
understanding the programmed mechanics of the game.

## Exploration Techniques

Reverse engineering techniques are varied. Some are described here, with
findings noted both here and in the [Marvel Future Fight
section](#marvel-future-fight).

### Static analysis

#### External files

##### File changes

Evaluate files changed between different runs of the same version on the same
account (change in time only), different versions (change in time and version),
different accounts (before and after signing in), running application versus
closed.

Findings:

###### Version 7.0.0, change in time, personal account, not running

-   app directory: vdex & odex differences
-   multiple authentication token differences
-   shared_prefs:
    -   com.netmarble.mherosgb.v2.playerprefs.xml: a few strings & keys,
        possibly different but not grossly; need to evaluate these with parsed
        XML/strings
    -   ff_openudid.xml: different device IDs
    -   marblePush.ko_Kr.real.xml: different registration IDs
-   /data/system_ce/0/shortcut_service/packages/com.netmarble.mherosgb.xml

## Unity

### Assets & Asset Bundles

### IL2CPP

-   [An introduction to IL2CPP internals](https://blogs.unity3d.com/2015/05/06/an-introduction-to-ilcpp-internals/)

-   [How To Data Mine Unity Apps](https://critical.gg/how-to-datamine-unity-apps/)

## Android

On the Android filesystem, there is a great deal of overlapping mounting and
linking of directories. The `autoextract` script mitigates this somewhat by
checking the inode number (serial number) of each directory and only including a
single one from the list of identical directories. The remaining directory tree
that includes all directories whose names case-insensitively include the string
`netmarble` is extracted for each release into a directory called
`mff-device-files-`_`version_string`_. As of this writing (version
7.0.1-170126-20210423), that tree is:

```
/data
  /misc
    /iorapd/com.netmarble.mherosgb/170126/com.netmarble.mherosgb.SRNativeActivity/raw_traces
    /profiles/cur/0/com.netmarble.mherosgb
  /app/~~[string1]==/com.netmarble.mherosgb-[string2]==
    /oat/x86
    /lib/arm
  /system/graphicsstats
    /[string3]/com.netmarble.mherosgb/170126
    /[string4]/com.netmarble.mherosgb/170126
  /system_ce/0/shortcut_service/packages
  /data/com.netmarble.mherosgb
    /databases
    /app_webview
      /Default
        /GPUCache
		  /index-dir
    /cache/WebView
	  /Crashpad
    /shared_prefs
    /no_backup
    /files
      /nmscrash
        /lib
        /bc_current
  /media/0/Android/data/com.netmarble.mherosgb/files
    /bundle
    /il2cpp
      /Resources
        /etc
          /mono
            /1.0
            /2.0
              /Browsers
            /mconfig
      /Metadata
    /bundle_each
    /Cookies
```

For the above tree, directories are concatenated (like
`/system_ce/0/shortcut_service/packages`) if none of the intermediate
directories contains any other files or directories; thus, each line in this tree
contains at least two items, which may be files or directores, or contains no
other directories. Indented directories are children of the first directory
above them that is indented less. `[string`_`n`_`]` represents a string that
varies by installation.

## Marvel Future Fight

### Useful functions

Important functions with lots of info to explore:

-   `TableUtility$$LoadAll`

## The `mffer` Model

-   `Game` (Marvel Future Fight)
    -   contains one or more `Version`s
        -   each of which has several `Component`s such as:
            -   `Roster`, the group of playable `Character`s
            -   `Shadowland` and other game styles
            -   A `Localization` dictionary to translate strings
    -   has one or more `Player`s
    -   gets its data from a `DataStore`, a set of directories on a filesystem
        that contain
        -   `AssetFiles` (each of which is associated with a `Version`), which
            recursively contain many
            -   `AssetObject`s

A fully detailed description of the types (and their associated members) is
available in the API reference. Of note, while these are quite clearly arranged
hierarchically in `mffer` conceptually, this does not imply that the types
themselves are nested; they are generally not nested in Marvel Future Fight
code.

### Assumptions in `mffer`

Best practices in evaluating the data available from Marvel Future Fight include
answering questions the same way the game's code answers them. When there is a
defined algorithm for converting one piece of information to another, that's
what should be followed by `mffer` in making the same conversion. For instance,
the conversion from `heroId` to `baseId` in Marvel Future Fight code is an odd
and highly specific calculation, but ensuring this same calculation is the one
used in `mffer` makes sure the data remains consistent.

However, not all information is accessible in the Marvel Future Fight code, and
some may not even be present. As such, multiple assumptions are needed when
extracting, importing, and reporting data. There are likely many such implicit
assumptions, but where explicitly made we attempt to test those assumptions at
the time of import or processing. Additionally, to ensure both transparency and
fidelity, we report those assumptions in this section.

#### Roster & Character model

Marvel Future Fight's characters appear to primarily be differentiated via
object properties rather than the hierarchy used in `mffer`, and we only assume
that the hierarchical model fits reality. Specifically, multiple character
properties are associated with different levels in the hierarchy, and the
assumptions we make in which property is associated with a given level is based
upon both gameplay experience and how that property is evaluated in Marvel
Future Fight code. (This also relates to the below [Character ID
models](#character-id-models) assumptions.)

The prototypical example of this sort of assumption is the character's `gender`
property. A game character's genders may be different depending upon the
equipped uniform, as is the case with Deadpool's default and "Lady Deadpool"
uniforms. However, given a character and uniform, no character's (as of this
writing) gender changes based upon promotion, advancement, skill changes, or
non-uniform equipment changes. We therefore make `gender` a property of the
`Uniform` type in the hierarchy.

An exhaustive list of similar assumptions is obtainable by examining essentially
all the properties of the `Character`, `Uniform`, and `CharacterLevel` types in
the API reference.

#### Character ID models

The identifiers Marvel Future Fight uses for individual characters are complex
and appear to be intertwined. Many of the qualities and relationships of these
identifiers are not explicitly defined in the Marvel Future Fight code, and thus
several assumptions are made and tested when importing and processing data about
the characters in loading the `Roster` component:

-   A given character equipped with a given uniform at a given tier and rank is
    represented by a single unique `heroId`.
-   A given character equipped with a given uniform (regardless of tier, rank,
    or other customization) is represented by a single unique `baseId`.
-   A given character (regardless of uniform, tier, rank, or other
    customization) is represented by a single unique `groupId`.
-   An additional identifier, `UniformGroupId` is unique only among the uniforms
    for a given character; specifically, we assume the default uniform for each
    character has `uniformId` `0`.

#### Localization changes

In earlier versions of Marvel Future Fight, string localization dictionaries
were primarily delivered as serialized CSV files. However, more recent version
are serialized dictionary-like objects, made additionally complicated by using a
hashed version of the internal string rather than the raw string value as the
key. We assume that if the hash algorithm or dictionary structure changes again,
loading the `Localization` component will simply fail with an exception rather
than having a more subtle error.