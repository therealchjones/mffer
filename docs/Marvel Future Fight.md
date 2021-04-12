# The Structure of Marvel Future Fight

## Introduction

(Marvel Future Fight)[http://www.marvelfuturefight.com/] is a mobile (iOS and
Android) online role playing game published by
(NetMarble)[http://netmarble.com]. It is developed using
(Unity)[https://unity.com] and compiled into native code for its platforms using
(IL2CPP)[https://docs.unity3d.com/Manual/IL2CPP.html]. The Android version is
available from the Play Store.

The programming and delivery of Marvel Future Fight are complex but accessible,
and this document attempts to gather a great deal of information describing the
exploration of Marvel Future Fight code, as well as to explicitly list some
assumptions made in the development of
(mffer)[https://github.com/therealchjones/mffer]. Where information is unknown
but thought to be true, such assumption is written explicitly. For definitive
information, evaluation of the code defining the information is given where possible.

This is (or strives to be) a development document, not gameplay instructions or
advice. Although knowing minutiae of expertly playing Marvel Future Fight is not
necessary, general knowledge of the game is expected and may be necessary for
understanding the programmed mechanics of the game.

## Unity

### Assets & Asset Bundles

### IL2CPP

## The `mffer` Model

-   `Game` (Marvel Future Fight)
    -   contains one or more `Version`s
        -   each of which has several `Component`s such as:
            -   `Roster`, the group of playable `Character`s
            -   `Shadowland` and other game styles
            -   A `Localization` dictionary to translate strings
    -   has one or more `Player`s
    -   gets its data from a `DataStore`, a set of directories on a filesystem that contain
        -   `AssetFiles` (each of which is associated with a `Version`), which recursively contain many
            -   `AssetObject`s

A fully detailed description of the types (and their associated members) is
available in the API reference. Of note, while these are quite clearly arranged
hierarchically in `mffer` conceptually, this does not imply that the types
themselves are nested; they are generally not nested in Marvel Future Fight code.
