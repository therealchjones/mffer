# The Structure of Marvel Future Fight

Or, On the Many Details of How I Wasted Massive Amounts of Time and the
Existentially Inconsequential Things I Learned

- [Introduction](#introduction)
- [Exploration Techniques](#exploration-techniques)
	- [Static analysis](#static-analysis)
		- [External files](#external-files)
			- [File changes](#file-changes)
- [Unity](#unity)
	- [Assets & Asset Bundles](#assets--asset-bundles)
		- [AssetsTools](#assetstools)
	- [IL2CPP](#il2cpp)
- [Android](#android)
- [Marvel Future Fight](#marvel-future-fight)
	- [Useful functions](#useful-functions)
- [The `mffer` Model](#the-mffer-model)
	- [Assumptions in `mffer`](#assumptions-in-mffer)
		- [Asset and AssetBundle Files](#asset-and-assetbundle-files)
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

Analyzing files changed with different changes to the app allow otherwise
"blind" evaluation of the app and where it may store specific data. Because of
the large number of changes that can quickly happen, however, controlling this
experiment as much as possible is important. Among other changes, simply
reinstalling the app may change some tokens, essentially without importance, so
where possible testing should even be done on the same installation. The steps
below describe at which point in the `autoextract` process (or its equivalent)
files were retrieved; the changes listed with each step are in comparison to the
files obtained after the prior step.

Methods:

mff_no_google_play AVD was created manually but with the same parameters as in
`autoextract`, as was /sdcard/Download/getfiles. APK files for version
7.2.0-174314-20210713 were obtained from a prior run of `autoextract` and
installed on the AVD in the same fashion as in `autoextract`. Steps similar to
the manual steps in `autoextract` (and listed below) were taken followed by
running `getfiles` and downloading the resulting file hierarchies into
individual directories. `dfdiff -v` was used to identify changes from each step to
the next, and changed files were manually reviewed to identify interesting or
potentially useful changes. (Of note, `dfdiff` intentionally ignores some
directory trees believed to be system traces, performance data, etc.) Files that
do not appear to be of interest are only mentioned in the below results the
first time they are included in `dfdiff` output.

Steps & Results:

1. Installed MFF, waited until unprompted downloads were complete, and used
   Options->Download to complete optional downloads. Extracted file hierarchy 1.
2. Closed MFF. Extracted file hierarchy 2.

    _There were no differences between file hierarchy 1 and file hierarchy 2._

3. Restarted MFF. Extracted file hierarchy 3.

    _In short, nothing interesting added; specifically:_

    ```
    Δ/data/data/com.netmarble.mherosgb/app_webview/Default/Cookies
    ```

    _Only updated cookies access times_

    ```
    Δ/data/data/com.netmarble.mherosgb/databases/androidx.work.workdb-shm
    Δ/data/data/com.netmarble.mherosgb/databases/google_app_measurement_local.db
    Δ/data/data/com.netmarble.mherosgb/databases/singular-1.db
    ```

    _nothing meaningful_

    ```
    -/data/data/com.netmarble.mherosgb/files/AppEventsLogger.persistedevents
    ```

    _data being logged back to Facebook unlikely of interest_

    ```
    Δ/data/data/com.netmarble.mherosgb/files/nmsslg.nmss
    ```

    _unknown, likely regarding NetMarble Security System_

    ```
    Δ/data/data/com.netmarble.mherosgb/shared_prefs/NetmarbleS.CrashReport.xml
    ```

    _different sessionKey_

    ```
    Δ/data/data/com.netmarble.mherosgb/shared_prefs/NetmarbleS.Log.xml
    Δ/data/data/com.netmarble.mherosgb/shared_prefs/NetmarbleS.Notice.xml
    Δ/data/data/com.netmarble.mherosgb/shared_prefs/Promotion.xml
    ```

    _different counts_

    ```
    Δ/data/data/com.netmarble.mherosgb/shared_prefs/com.google.android.gms.measurement.prefs.xml
    ```

    _pause time & background status_

    ```
    Δ/data/data/com.netmarble.mherosgb/shared_prefs/com.netmarble.mherosgb.v2.playerprefs.xml
    ```

    _heartbeat time, session count, session ID_

    ```
    Δ/data/data/com.netmarble.mherosgb/shared_prefs/com.netmarble.mherosgb_preferences.xml
    ```

    _Facebook session info_

    ```
    Δ/data/data/com.netmarble.mherosgb/shared_prefs/pref-event-index.xml
    ```

    _different indices_

    ```
    Δ/data/misc/profiles/cur/0/com.netmarble.mherosgb/primary.prof
    ```

    _Profile-guided compilation data_

    ```
    Δ/data/system_ce/0/shortcut_service/packages/com.netmarble.mherosgb.xml
    ```

    _different timestamps on shortcuts_

4. Logged in and restored account from Facebook; restarted as prompted and
   returned to main lobby screen. Extracted file hierarchy 4.

    _New "notice" or "promotion" files in asset format to the `bundle_each`
    directory. A bit more info in playerprefs of questionable utility. Other
    than that, little of use, including a few of the above, plus:_

    ```
    Δ/data/data/com.netmarble.mherosgb/shared_prefs/NetmarbleS.Auth.xml
    ```

    _different player ID, unchanged "constants"_

    ```
     Δ/data/data/com.netmarble.mherosgb/shared_prefs/NetmarbleS.Channel.xml
    ```

    _added Facebook ID & token_

    ```
     Δ/data/data/com.netmarble.mherosgb/shared_prefs/NetmarbleS.CrashReport.xml
    ```

    _different user_

    ```
     +/data/data/com.netmarble.mherosgb/shared_prefs/NetmarbleS.Facebook.xml
    ```

    _new file, Facebook "version"_

    ```
     Δ/data/data/com.netmarble.mherosgb/shared_prefs/NetmarbleS.Notice.xml
    ```

    _new skip count with new user ID_

    ```
     Δ/data/data/com.netmarble.mherosgb/shared_prefs/NetmarbleS.Tos.xml
    ```

    _COPPA & buy limits (uncertain usefulness)_

    ```
     Δ/data/data/com.netmarble.mherosgb/shared_prefs/Preference.xml
    ```

    _subscription info (empty, uncertain usefullness)_

    ```
     Δ/data/data/com.netmarble.mherosgb/shared_prefs/Promotion.xml
    ```

    _skip counts and open history_

    ```
     +/data/data/com.netmarble.mherosgb/shared_prefs/com.facebook.AccessTokenManager.SharedPreferences.xml
     +/data/data/com.netmarble.mherosgb/shared_prefs/com.facebook.login.AuthorizationClient.WebViewAuthHandler.TOKEN_STORE_KEY.xml
     +/data/data/com.netmarble.mherosgb/shared_prefs/com.facebook.loginManager.xml
    ```

    _Facebook login information_

    ```
    Δ/data/data/com.netmarble.mherosgb/shared_prefs/com.netmarble.mherosgb.v2.playerprefs.xml
    ```

    - Added `EQ_RECOMMEND_TIME_*`, `UPDATE_ID`, `LOCAL_PUSH_*`, `PUSH_INIT`,
      `DOMINATION_LAST_NOTICE_SEASION_ID_*`, `cinematicbattlenoticeday*` values
    - Added checksums for the below `bundle_each` files
    - Changed `UnityGraphicsQuality`, `MISSION_ACHIEVE_NAVI` values
    - Removed `SEEN_DIALOG` value
    - Update heartbeat time

    ```
     Δ/data/data/com.netmarble.mherosgb/shared_prefs/marblePush.ko_Kr.real.xml
    ```

    _only player ID changed_

    ```
     Δ/data/data/com.netmarble.mherosgb/shared_prefs/singular-pref-session.xml
    ```

    _pause times and IDs_

    ```
     +/data/media/0/Android/data/com.netmarble.mherosgb/files/bundle_each/newcontents194
     +/data/media/0/Android/data/com.netmarble.mherosgb/files/bundle_each/newcontents273
     +/data/media/0/Android/data/com.netmarble.mherosgb/files/bundle_each/newcontents274
     +/data/media/0/Android/data/com.netmarble.mherosgb/files/bundle_each/noticepopup_blackwidow
    ```

    _new asset bundles (not yet explored)_

5. Closed MFF. Extracted file hierarchy 5.

    _Mostly more of the same, with minimal useful information, including some of
    the above plus:_

    ```
    Δ/data/data/com.netmarble.mherosgb/shared_prefs/com.netmarble.mherosgb_preferences.xml
    ```

    _Added the `LOCAL_PUSH_IDS` value_

    ```
    Δ/data/data/com.netmarble.mherosgb/shared_prefs/marblePush.ko_Kr.real.xml
    ```

    _Added the `localNotificationId` value_

6. Created new AVD, installed MFF, waited until unprompted downloads were
   complete, used Options->Download to complete optional downloads, logged in
   and restored account from Facebook, restarted as prompted, returned to main
   lobby screen, and closed MFF. Extracted file hierarchy 6.

    _Mostly inconsequential changes. Long list of files changed, many including
    the above and several others that just seem to change IDs, plus:_

    ```
    Δ/data/app/*/com.netmarble.mherosgb-*/oat/x86/base.odex
    ```

    _Pre-optimized DEX file_

    ```
     Δ/data/data/com.netmarble.mherosgb/app_webview/Default/Cookies
    ```

    _Different device key_

    ```
     Δ/data/data/com.netmarble.mherosgb/files/PersistedInstallation.W0RFRkFVTFRd+MToyNjUyNzk4MjY4MjI6YW5kcm9pZDo3MjY1ZmNhMmM4NTlmNTM0.json
     Δ/data/data/com.netmarble.mherosgb/no_backup/com.google.InstanceId.properties
    ```

    _Different tokens of uncertain significance_

    ```
      Δ/data/data/com.netmarble.mherosgb/shared_prefs/com.facebook.internal.MODEL_STORE.xml
    ```

    _unknown_

    ```
    Δ/data/data/com.netmarble.mherosgb/shared_prefs/com.facebook.login.AuthorizationClient.WebViewAuthHandler.TOKEN_STORE_KEY.xml
    Δ/data/data/com.netmarble.mherosgb/shared_prefs/com.facebook.sdk.appEventPreferences.xml
    Δ/data/data/com.netmarble.mherosgb/shared_prefs/com.facebook.sdk.attributionTracking.xml
    ```

    _Facebook tokens unlikely of any significance_

    ```
     Δ/data/data/com.netmarble.mherosgb/shared_prefs/com.google.android.gms.appid.xml
     Δ/data/data/com.netmarble.mherosgb/shared_prefs/com.google.android.gms.measurement.prefs.xml
    ```

    _Google tokens unlikely of any significance_

    ```
     Δ/data/data/com.netmarble.mherosgb/shared_prefs/com.netmarble.mherosgb.v2.playerprefs.xml
    ```

    _just the additional `hbm_therealchjones` string, which on retrospect
    probably should have been in the last one, too; otherwise only cosmetic
    differences of keys in different orders and such_

    ```
       Δ/data/data/com.netmarble.mherosgb/shared_prefs/pref-singular-id.xml
       Δ/data/data/com.netmarble.mherosgb/shared_prefs/singular-licensing-api.xml
       Δ/data/data/com.netmarble.mherosgb/shared_prefs/singular-pref-session.xml
    ```

    _tokens unlikely to be of any significance_

All in all, very little additional information gained from personal login,
restarting, or even closing the app before extracting files. The notable
exception is the possibility of extracting new "notices" from the `bundle_each`
directory, something that's not currently being done anyway.

## Unity

### Assets & Asset Bundles

Unity programs store much of their data as "assets", either in individual files
or in "asset bundles". These bundles are packed binaries that can be read from
disk (or a stream), in a defined format.

#### AssetsTools

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
    /profiles/cur/0/com.netmarble.mherosgb (profile-guided compilation, probably not useful)
  /app/~~[string1]==/com.netmarble.mherosgb-[string2]== (APK and binary libraries)
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
    /shared_prefs (primary store of "preferences")
    /no_backup
    /files
      /nmscrash
        /lib
        /bc_current
  /media/0/Android/data/com.netmarble.mherosgb/files
    /bundle (primary store of assets/asset bundles)
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

There are various Unity assets and asset bundles throughout this tree (as well
as within the `/data/app/*/*/base.apk` file), but the ones currently used in
`mffer` are in
`/data/media/0/Android/data/com.netmarble.mherosgb/files/bundle/`. Additionally,
"preferences" from `/data/data/com.netmarbe.mherosgb/shared_prefs/` are
extracted, though these do not store true individualized preferences but rather
more frequently updated data such as news, events, and achievements.

## Marvel Future Fight

### Useful functions

Important functions with lots of info to explore:

-   `TableUtility$$LoadAll`

## The `mffer` Model

-   `Game` (Marvel Future Fight)
    -   has zero or more `Player`s
    -   has zero or more current `Event`s
    -   has one or more `Version`s, each of which
        -   has zero or more `Component`s such as
            -   `Roster`, the group of playable `Character`s
            -   `Shadowland` and other game styles
            -   A `Localization` dictionary to translate strings
        -   has an `AssetBundle` associated from the `Game`'s `DataSource`
    -   has a `DataSource` with a dictionary associating `Version` names with
        -   `AssetBundle`s, each based upon a `DeviceDirectory` also associated
            with the given `Version` name, and each of which uses `AssetFile`s and/or
            `PreferenceFile`s to load multiple
            -   `AssetObject`s or `PreferenceObject`s, which may recursively
                contain further `AssetObject`s or `PreferenceObject`s

A detailed description of the types (and their associated members) is
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

#### Asset and AssetBundle Files

-   still need to deal with IsArray yes
-   what about other types nodes?
-   TypeTree level 0 - Base, "MonoBehaviour"
-   1 - m_GameObject - PPtr<GameObject>
-   Some nodes with children can be "leafs", like those with Type == "string" but children Array, size, char
    an array is a level beneath the node it's named for (and is the only child), with size and data beneath that
    probably write a generic object.WriteJson => object.ToString() for leafs
-   Figure out how to deal with DynamicAssetArray type and whether the included DynamicAssets are
-   Okay, dynamic asset array is (probably?) made up of multiple DynamicAssets in
    the TypeTree
-   Assumptions are tested via the AssetFileTest class

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
