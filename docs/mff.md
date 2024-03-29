# The Structure of Marvel Future Fight

Or, On the Many Details of How I Wasted Massive Amounts of Time and the
Existentially Inconsequential Things I Learned

## Introduction

[Marvel Future Fight](http://www.marvelfuturefight.com/) is a mobile (iOS and
Android) online role playing game published by
[Netmarble](http://netmarble.com). It is developed using
[Unity](https://unity.com) and compiled into native code for its platforms using
[IL2CPP](https://docs.unity3d.com/Manual/IL2CPP.html). The Android version is
available from the Play Store.

The programming and delivery of Marvel Future Fight are complex but accessible,
and this document attempts to conglomerate a great deal of information describing the
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
findings noted both here and in the
[Marvel Future Fight section](#marvel-future-fight).

### Static analysis

#### External files

##### File changes

Analyzing files changed with different changes to the app allow otherwise
"blind" evaluation of the app and where it may store specific data. Because of
the large number of changes that can quickly happen, however, controlling this
experiment as much as possible is important. Among other changes, simply
reinstalling the app may change some tokens, essentially without importance, so
where possible testing should even be done on the same installation. The steps
below describe at which point in the autoextract process (or its equivalent)
files were retrieved; the changes listed with each step are in comparison to the
files obtained after the prior step.

Methods:

mff_no_google_play AVD was created manually but with the same parameters as in
autoextract, as was /sdcard/Download/getfiles. APK files for version
7.2.0-174314-20210713 were obtained from a prior run of autoextract and
installed on the AVD in the same fashion as in autoextract. Steps similar to
the manual steps in autoextract (and listed below) were taken followed by
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

    _unknown, likely regarding Netmarble Security System_

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
disk (or a stream), in a defined format. Though it's marked as "deprecated",
[Unity's Assets, Resources and AssetBundles tutorial](https://learn.unity.com/tutorial/assets-resources-and-assetbundles)
is a useful guide to this confusing (and often contradictory) terminology. The
tutorial itself notes that some words it uses even differs from the
terminology used in Unity's public APIs. In order to clarify as much as
possible, consider the following example and tables.

#### Example: data for a Unity game

In your glamorous job as a Netmarble Marvel Future Fight creative developer,
you've had the honor of designing a new uniform (with a whole new set of
skills!) for Captain Marvel herself. There's already a huge database of
information on the other characters and uniforms in the game, and it's time to
add this new data. You'll need information on all the different skills, the
stats for the character at different levels and tiers, and (of course) the name
of the new uniform, not to mention all the beautiful graphics and animations to
go with it.

Let's just concentrate on your most interesting work, the text-based
information. That huge database of all the other characters and uniforms can be
updated by adding lines for the new uniform at different levels, ranks, and
tiers. To get those lines into the giant Unity project that is Marvel Future
Fight, you put those lines at then end of your spreadsheet named
`HERO_LIST.csv`. You then import the spreadsheet to the Unity project (to
overwrite the older version), and Unity saves it as an _asset_. You can probably
still see it just as `HERO_LIST.csv`, but Unity has changed it in the background
to something called `text/data/hero_list.asset`, maybe converting it from a
comma-delimited file to some other kind of storage along the way. You can do
something similar with the graphics, sounds, and so on.

To get the new information integrated into the game, however, the software has
to parse the CSV-equivalent file. Since most of your programing for the game
will be in C#, using spreadsheet functions and `A1` notation to look up
character information is probably not the most efficient way to refer to the
data. Instead, the Unity framework parses the _asset_ into an _object_, in this
case called `IntHeroDataDictionary` since it's a dictionary used to look up
`HeroData` instances (each representing lines of the original CSV) indexed by
`int`s (which we'll later find out are also called `HeroId`s). Of course, any
modern programmer knows `HeroData`, `HeroId`, and even `int` (in some languages)
are also objects, and now you know why this terminology gets confusing.

`HERO_LIST.csv` and `text/data/hero_list.asset` may each be called an _asset_,
an _asset file_, or a _file_. However, when Unity packages the game to be
downloaded, most of the thousands such files are packed together much like
source code is packed into zip or .tar.gz files---or perhaps more like how the
code is compiled into a running executable. `text/data/hero_list.asset` is
combined with `text/data/exchange_item.asset` and others into a single _AssetBundle_ (or
_AssetBundle File_) named `text` that gets downloaded with a bunch of other
assetbundles the first time you run Future Fight on your device (as well as when
there's an update). `text` also includes information about the associated
_object_ structures, a catalog of all the individual files, and other _resource_
information.

Let's make it more complicated. The layout of the AssetBundle file may include
another layer, a single file that in turn contains all the assets. This may be
invisible to Unity programmers, but especially when trying to open AssetBundle
files with other tools may become more apparent. Between an _AssetBundle_ and an
_Asset_ (or _AssetFile_), then, is this _AssetsFile_ (note the extra
pluralization). mffer generally tries to encapsulate this away within an
`IAssetReader` implementation, but it may be necessary to make note of it
(especially if trying to create such an implementation).

#### AssetsTools.NET

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
mffer are in
`/data/media/0/Android/data/com.netmarble.mherosgb/files/bundle/`. Additionally,
"preferences" from `/data/data/com.netmarbe.mherosgb/shared_prefs/` are
extracted, though these do not store true individualized preferences but rather
more frequently updated data such as news, events, and achievements.

## Marvel Future Fight

### Useful functions

Important functions with lots of info to explore:

-   `TableUtility$$LoadAll`

## The mffer Model

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
            -   `GameObject`s or `PreferenceObject`s, which may recursively
                contain further `GameObject`s or `PreferenceObject`s

A detailed description of the types (and their associated members) is
available in the API reference. Of note, while these are quite clearly arranged
hierarchically in mffer conceptually, this does not imply that the types
themselves are nested; they are generally not nested in Marvel Future Fight
code.

### Assumptions in mffer

Best practices in evaluating the data available from Marvel Future Fight include
answering questions the same way the game's code answers them. When there is a
defined algorithm for converting one piece of information to another, that's
what should be followed by mffer in making the same conversion. For instance,
the conversion from `heroId` to `baseId` in Marvel Future Fight code is an odd
and highly specific calculation, but ensuring this same calculation is the one
used in mffer makes sure the data remains consistent.

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
object properties rather than the hierarchy used in mffer, and we only assume
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

## Exploration tools

Multiple tools have been used to explore Marvel Future Fight. Most are not
directly associated with the mffer project, but a few were used historically
or continue to be used to promote its ongoing development and further
exploration of the game. These remain in the `tools/` directory of the mffer
git repository.

### `autoextract`

Formerly a part of the mffer workflow, `autoextract` uses Android device
emulation and user interaction to obtain game program and data files. This may
work when other completely automated methods for downloading the data no longer
succeed due to Marvel Future Fight changes. It requires Java and cannot be
reliably run within a virtual machine.

## Various notes

```
Development Notes

I started trying to reverse engineer Marvel Future Fight somewhere around
version 6.2.0, or at least those are the first archives I have of anything even
remotely useful. I started with the x86 build, which I probably didn't put much
thought into choosing as it would clearly be the one with which I had the most
experience (regardless of that experience not mattering in the least). As it
happens, sometime between 6.2.0 and 6.7.0, the gap which spanned from my first
attempts to get usable information to the first time I was ready to generalize
those attempts into extracting information from a new version, Netmarble stopped
releasing x86 builds; installing Future Fight from the Play Store on an x86
emulator installed the armeabi-v7a build instead, since it was compatible with
the x86 devices. As the assembly language for ARM appeared to be quite different
from that for x86, I hoped I could continue to use the x86 assembly knowledge
which I'd had to learn by using the x86_64 build instead.

Unfortunately, the x86_64 APKs I downloaded from the Google Play Store would not
run on the emulators when I installed them. They crashed due to an inability to
locate libnmsssa.so, a security library Netmarble installed---or was supposed
to. As it turned out, all the x86_64 APKs, at least back to 6.2.0, included none
of the actual architecture-specific libraries for the game. Hoping this was some
sort of difficulty with how the Play Store "repackaged" APKs from large bundles
that had been uploaded, I tried using different accounts, changing to different
emulator devices, and even researching how to change the identifiers for the
emulator so that the Play Store wouldn't recognize it as a device that had
previously installed the software. I couldn't get it to work. Turning to the APK
sharing sites like APKmirror.com, I found all their x86_64 APKs had the same
problem. I had written to in-game support for assistance, only to essentially
receive a form letter saying everything installed fine, and if there was a
problem it was because I was using an emulator rather than a physical device.
And, of course, I couldn't come up with any ideas for proving this incorrect
rather than actually obtaining an x86_64 physical device.

I'd love to ask for help, but the situation is too niche for StackOverflow (and
not exactly a *developer* problem), and probably far too technical for the
reddit Future Fight forum. Maybe Google/Android/Play Store would be interested
in the unusable APK being distributed for certain architectures.


Posted variations to Google Play Store help request & community:

Marvel Future Fight x86_64 APK does not include libraries

Using Android SDK emulator with x86_64 system image with Google Play API (system-images;android-30;google_apis_playstore;x86_64), open Play Store app, login, install Marvel Future Fight (com.netmarble.mherosgb). Open Marvel Future Fight, almost immediate crash. Further investigation reveals the crash is an UnsatisfiedLinkError due to a missing libnmsssa.so. No such file exists in the delivered (and surprisingly small) split_config.x86_64.apk as part of the installation. However, this file (and many other architecture-dependent libraries) are included in the armeabi-v7a APK (which is also used on x86) and the arm64-v8a; they just don't seem to be included in the x86_64 APK, making running on that platform impossible. Reviewing this split APK on several (admittedly less reputable) sites suggests this same problem goes back several versions/releases. Developer states this is due to emulator use, but it appears the package does not properly support x86_64.

I have brought this problem to the attention of the developer via in-game issue filing, but they state that it is due to using an emulator, and that all works properly on physical devices. Unfortunately, I am unable to confirm this, and thus unable to determine if this is a bug in the Play Store delivery or in the packaging uploaded to the Play Store. Either way, it appears that x86_64 devices are not properly supported as the Play Store package description would suggest. Please determine whether there is an easy fix or if x86_64 compatibility should be removed from the package.

I'm happy to provide any further information that may be of use.

--


installed ghidra
changed ghidra.bat to use 2048MB memory
installed amazon coretto 11 for windows
used adoptopenjdk on mac

Ghidra -> New Project -> Batch Import ->
        device/data/app/*/*/lib/x86/* -> open libil2cpp.so
(or maybe just load il2cppdumper then others until symbols are resolved?)
(or maybe libil2cpp.so is all I need, and accept unresolved symbols)

When to analyze?
When Ghidra has completed analysis,
        Window -> Script Manager -> Script Directories ->
                add il2cppdumper directory, run ghidra.py and select the
                        requested files from the il2cppdumper directory
(turn off analyzers while running script, close all windows including listing, leaving just Console and script manager open)

got all files findable by root in a netmarble path, zipped to allfiles.tar.gz
(some nmss sockets unable to be taken)
compared base & split apk from google play avd to new one in allfiles, same, deleted the former
(also made sure there were no other files with user u0_a149, group u0_a149, or group u0_a149_cache)
pruned subtrees that were identical, got down to just the /data directory

decompiled .apk files and the .dex files within them to decompiled/, combined (mostly) the split and base dirs, combined the classes dirs

need to decompile odex and vdex files; base.vdex contained only (optimized) copies of the classes.dex and classes2.dex files. (used vdexExtractor) Used baksmali, then smali, then decompiler.com to decompile base.odex->out(smalis)->out.dex->odex-sources/, merged new files (constants et al.) with decompiled/base/sources. All the rest are at least cosmetically different (fail cmp), left in decompiled/base-odex/sources. All the rest are at least cosmetically different (fail cmp), left in decompiled/base-odex/sources.

need to check (a separate copy of) decompiled and/or device for assets/bundles

Asset Studio:
decompiled/ : a few text assets including localization, RPC?, sounds, scenes?, fonts (including confirmation primary font is Exo Condensed and (maybe) Arial, Visitor TT1 Extended, and Exo CJKs)
device/data/media/0/Android/data/com.netmarble.mherosgb/files/
	bundle_each: audio, promo images
	bundle: lots and lots, haven't yet loaded all successfully
		_common*: mostly crap textures, a couple of head shots
		_scene*: crap textures
		_unit*: crap textrues
		effect*: nothing useful
		fx,item,localization*: localization_en TextAsset with strings
		monster, scene_*: useless textures
		sound: well, sound
		stringTable_en.csv: extracted version of Localization_en.csz
		text: mostly encoded(?) textassets
		ui_*: various icons, backgrounds
			ui_card: small comic card images (128x128)
			ui_characterani: special moving icon images
			ui_character: head shots
			ui_comicscard: larger comic cards in combined atlas-like images
		unit_*: character textures

Looking for: text, comic cards, headshots, shadowland headshots, uniform headshots

obtained apktool, used to decompile base.apk, should use this and compare to decompiler.com

loaded device/data/media/0/Android/data/com.netmarble.mherosgb/files/bundle into AssetStudioGUI (which took a very long time)

Discovery utilities:
Il2CppDumper
IlSpy
DnSpy?
VC_redist?
android studio (not emulated)
apktool?
vdexextractor?
AssetStudioGUI
Decompiler.com?

Ongoing utilities:
Android Studio (device emulator, java, adb)
Asset extractor

--
grep -Rl 'CO-OP PLAY' device/ > co-op_play_files
# only device/data/media/0/Android/data/com.netmarble.mherosgb/files/bundle/
# localization_{in,ar,en} and stringTable_en.csv
# Opening localization_en in AssetStudioGUI yields only one asset/file,
# localization/localization_en.csv. Export this, cmp exactly the same as
# stringTable_en.csv
# reading file, header is just "prevent clipping" and "dummy"
# interesting places 'CO-OP PLAY' comes in:
# MULTI_03 CO-OP PLAY
# FUTURE_PASS_CONTENTS_19 [CO-OP PLAY] Acquire a reward
# ACHIEVE_1016140 AVENGERS ASSEMBLE! #1
# ACHIEVE_DESC_1016140 [CO-OP PLAY] Participate 1 time
# ACHIEVE_2001060 [DAILY] THE IMPORTANCE OF TEAMWORK
# ACHIEVE_DESC_2001060 [CO-OP PLAY] 5 Successful Completions

# Looking for associated strings in multiple areas: see MFF spreadsheet
# "String RE" tab
--
Need:
Android Studio, Android NDK
device-netmarble files

mkdir lib-android/
cp -a ~/Library/Android/sdk/ndk/21.3.6528147/toolchains/llvm/prebuilt/darwin-x86_64/sysroot/usr/lib/i686-linux-android/28/*.so lib-android/

ghidra make new project
	import libil2cpp.so from device-netmarble files
		options:
			changing start/base to 0
			load libraries, add lib-android/ directory
	open libil2cpp.so from ghidra project tree
		autoanalyze when prompted
			include all default options except:
				discovery of nonreturn functions
				embedded media
				call convention identification
			add elf scalar operand references
			(check if this is right for defaults:
				all except red, nonreturning discovery,
				call convention, decompiler parameter id,
				and embedded media)
		(lots of LSDA errors in FDE territory with address
			overflow in subtract; this may be related to the
			above elf thing; should we allow the different/default
			base?)

use il2cpp-hmaker to make il2cpp-fixed.h
ghidra: explore and follow DBTable classes
(of note, libil2cpp.so seems to have had all relocation data stripped from it as well, resulting in all the e8 00 00 00 00 calls.)
Oh, no, that's not the deal, it's just a really weird way to figure out where some of the plt is found
then, Parse struct file, run ghidra script, auto-analyze again(?), and follow DBTable class functions
had to expand ghidra memory to 4gb, vm to 8gb

maybe il2cppdumper script would work better without "const" in function definitions?
--
Unzipped *.apk to apk/
Loaded *.apk to decompiler.com, downloaded zip, unzipped to apk-decompiled/
Unzipped prior decompiler.com version of *.apk to apk-decompiled-2/
Compared those final two versions (diff -qr apk-decomp*), copied two files that differed from apk-decompiled-2 to <path/filename-2> under apk-decompiled
Removed apk-decompiled-2/
mv -i apk-decompiled/* apk/
( apk/resources/classes*.dex are the same as apk/classes*.dex )
loaded apk/classes*.dex into decompiler.com, downloaded zips, extracted to apk/classes*/
mv -i classes*/ apk/
used Android Studio, profile or debug apk, loaded *.apk
- download & install android platform 28 sdk
- download plugin updates when prompted, restarted android studio when promptedo- built virtual device Pixel XL using Q atom (no google API or Google play), named AVD
since unable to run virtual x86 image within android studio within parallels, uninstalled android studio, reinstalled, no help
installed Android 7 as a Parallels virtual machine
in AVM, Settings->Developer Options->USB Debugging on
set windows path to include %LOCALAPPDATA%\Android\sdk\platform-tools
restart android studio
in android studio terminal, adb connect 192.168.64.3
When android studio connected to device, run apk, which will install future fight and start it; download update files when recommended.
however, unable to click on "Download" button, so tried again with Android-x86 9

could not get mff to run in parallels android emulator (download button never worked), so installed Google Play version on Mac Android Studio, signed in, used Gplay to install future fight, restored account from Facebook, installed all patches when prompted, downloaded all Data from settings, exit game
got files/directories: via adb shell, adb shell pm list packages | grep netmarble, adb shell pm path com.netmarble.mherosgb:
/data/app/~~493DhfzldxyauWfofN2Syg==/com.netmarble.mherosgb-5h4QgwLp0OjncIsIOkxU4w==/base.apk
/data/app/~~493DhfzldxyauWfofN2Syg==/com.netmarble.mherosgb-5h4QgwLp0OjncIsIOkxU4w==/split_config.x86.apk

built new avd with all the same except not google play
adb install-multiple base.apk split_config.x86.apk
started, updated and connected as prompted, connected to account
settings-->download all
exit game
got all files findable by root in a netmarble path, zipped to allfiles.tar.gz
(some nmss sockets unable to be taken)
compared base & split apk from google play avd to new one in allfiles, same, deleted the former
compared base & split apk to downloaded one, same except for manifests and splits0.xml files
(also made sure there were no other files with user u0_a149, group u0_a149, or group u0_a149_cache)
unpacked allfiles.tar.gz to device/
pruned subtrees that were identical, got down to just the /data directory

decompiled .apk files and the .dex files within them to decompiled/, combined (mostly) the split and base dirs, combined the classes dirs

need to decompile odex and vdex files; base.vdex contained only (optimized) copies of the classes.dex and classes2.dex files. (used vdexExtractor) Used baksmali, then smali, then decompiler.com to decompile base.odex->out(smalis)->out.dex->odex-sources/, merged new files (constants et al.) with decompiled/base/sources. All the rest are at least cosmetically different (fail cmp), left in decompiled/base-odex/sources.

need to check (a separate copy of) decompiled and/or device for assets/bundles

Asset Studio:
decompiled/ : a few text assets including localization, RPC?, sounds, scenes?, fonts (including confirmation primary font is Exo Condensed and (maybe) Arial, Visitor TT1 Extended, and Exo CJKs)
device/ :

(uTinyRipper doesn't seem to have functionality not in AssetStudio, UAE crashes a lot but gives idea hex editor may be useful) DevXUnity tools may be useful for regenerating Unity packages, but this is not in the free versions.

obtained apktool, used to decompile base.apk, should use this and compare to decompiler.com
unzipped apk files to base.apk-unzipped and split.apk-unzipped. Used Il2CppDumper, selected split.apk-unzipped/lib/x86/libil2cpp.so then base.apk-unzipped/assets/bin/Data/Managed/Metadata/global-metadata.dat; (could also use command line Il2CppDumper.exe libil2cpp.so global-metadata.dat output-directory/

loaded device/data/media/0/Android/data/com.netmarble.mherosgb/files/bundle into AssetStudioGUI (which took a very long time)

Discovery utilities:
Il2CppDumper
IlSpy
DnSpy?
VC_redist?
android studio (not emulated)
apktool?
vdexextractor?
AssetStudioGUI
Decompiler.com?

When libil2cpp.so calls PluginsCommon, retype as PluginsCommonForAndroid to get
proper method name/function, then check jadx decompiled
sources/com/seed9/common/Common.java

We make assumptions where needed that we're using a final "distribution" version
of the software, in the "global" (i.e., not china or tencent) setting

Common dead ends: GameObject_AddComponent...

Reverse Engineered Program Flow:

SceneTitle.Start() {
	SceneTitle.CheckServer() {
		PacketTransfer.SetServerData( scene SceneTitle ) {
			url = PatchSystem.get * url() {
				PatchSystem.CreateUrl() {
					BuildType = Define.get_BuildType = BUILD_TYPE * *Enum.GLOBAL( 0 )
					BuildOptions = Define.get_BuildOptions = BUILD_OPTIONS * *Enum.DIST( 4 )
					baseUrl = PatchSystem.get_base_url() = PatchSystem.GetBaseUrl() = http://mheroesgb.gcdn.netmarble.com/mheroesgb/
return baseUrl + DIST / Android /
				}
			}
			filepath = ServerInfo.GetRemoteFilePath() {
				filepath = "v" + bundleVersion + "/"
				filepath = filepath + ServerInfo.GetFileName() { return "server_info.txt" }
				filepath = filepath + "?p=" + CommonUtil.GetRandomInt()
				return filepath
			}
			WWWUtil.Get( param = url + filepath, scene, successMethod = "SetServerDataOK", failMethod = "", isLock = true, sendOption = 0, sendtotargetscene = false, timeout, \_retry = false, receiveresultwhenunlock = true, useFastJson = false, forceNotSequence = false, disableProgressUI = false ) {
				wwwType = 1
				retry = true
				islock = true
				parameter = param
				key = parameter
				if !parameter starts with http:// {
					if !parameter starts with https:// {
						WWWUtil.AddDefaultPacketParameter( parameter ) {
							if parameter contains?
								append &
							else
								append ?
							append "uID=" + userId
							append "&cKey=" + uptime
						}
						key = CryptUtil.getPacketKey()
						if key is nullorempty
							key = CryptUtil.get_aesKey() + CryptUtil.get_aesKey()
						parambytes = Encoding.UTF8.GetBytes(parameter)
						encryptedParam = CryptUtil.AESEncrypt( parambytes, key, IsKeyIvSame = true )
						key = sessionId
						header = WWWUtil.MakeHeader( key, encryptedParam.length )
						contents = header + encryptedParam
						key = ServerInfo.get_URL() + "FF"
						url = key
						param = parameter
						isGamePacket = true
						isSequenceProcess = true
						encoded = true
						form = new WWWForm()
						form.AddBinaryData( fieldname = bin, contents, filename = bin, mimetype = application / octet - stream )
					}
				} else {
					url = key [parameter]
					param = parameter
					isGamePacket = false
					isSequenceProcess = false
				}
				wwwType = 1
				islock = islock
				sendOption = options
				sendtotargetscene = sendtoTargetScene
				failedReason = 0
				MyData.RestoreAllPrevData()
				WWWUtil.GetRoutine_1() {
					// via MonoBehaviour.StartCoroutine(): WWWUtil.GetRoutine_c_Iterator1.MoveNext() { // this is a guess
					if isSequenceProcess {
						// add the request( WWWData ) to the WWWUtil data stack then do the next request in the stack via WWWUtil.GetRountine_internal like below
					} else {
						// just do the request via
						WWWUtil.GetRountine_internal() {
							// via MonoBehaviour.StartCoroutine() WWWUtil.GetRountine_internal_c_Iterator0.MoveNext() { // this is a guess
							// specifically for WWWType = 1
							// BestHTTP package HTTPRequest( url, POST )
							HTTPRequest.SetFields( form )
							HTTPRequest.Send()
							bytes = HTTPRequest.get_Response().get_Data()
							WWWUtil.OnRequestFinished( data, error, bytes ) {
								if failedReason == 0 {
									if error is nullorempty {
										if IsGamePacket {
											errors = WWWUtil.Parse( CryptUtil.PacketDecode( bytes, WWWUtil.UsePacketKeyFirst( param ) ), data, descRef )
											if errors is nullorempty {
												WWWUtil.ResultOK( data, packetdecode, fastjson ) {
													WWWUtil.SendResult( scene, successmethod, packetdecode, sendoption, sendtotargetscene, data, fastjson, byte	[] bytes = null, bool isCache = false ) {
													If sendtoTargetScene == 0 {
														PacketTransfer( aka Component_1 ).SendMessage( successmethod, WWWResult, SendMessageOptions.Enum.DontRequireReceiver ) // just guessing this calls PacketTransfer.successMethod()
													} else {
														Scene( aka GameObject ).SendMessage( successmethod, wwwresult, sendmessageoptions \* \*enum.DontRequireReceiver) // just guessing this calls scene.successMethod
													}
												}
											} else WWWUtil.ResultFail()
											return;
										}
										string = ByteToString(bytes)
										string = UnEscapeURL(string)
										WWWUtil.ResultOK(data,string,null)
										return;
									}
								} else if failedReason != 2 {
									failedReason = 1
									WWWUtil.Retry()
									return;
								} else throw NullReferenceException
							}
						}
					}
				}
				return data;
			}
			return;
		}
		return;
	}
	TasSDK.init
	TASEventListener.RegisterEvent()
	return;
}


HeroList_UpdateAll()
}
} else {
	WWW.ResultFail( data, errors, fastjson, packetdecode )
}
} else {
	byteString = CryptUtil.ByteToString( bytes )
byteString = WWW.UnEscapeURL( byteString )
WWWUtil.ResultOK( data, resultString, FastJsonObject desc = null )
}
}
}
}
}
}
}
}
}
}
PacketTransfer.SetServerDataOK( WWWResult result ) {
	PacketTransfer.NotifyResult( scene, "SetServerDataOK", result->text ) {
		scene.sendmessage( "SetServerDataOK", text, SendMessageOptions\_\_Enum_DontRequireReceiver ) // guessing this calls
SceneTitle.SetServerDataOK( text ) {
			ServerInfo.ParseVersionFile( text ) {
				ServerInfo.ParseVersionFileCDN( text ) {
					serverResponse = JsonParser.Parse( text, typeof Server_Response )
					PatchConfig.Init() // Configures low device resource usage if necessary
ServerInfo.set_data( null ) { ServerInfo.staticfields.data * = null }
					serverList = serverResponse.server * list
				  foreach server in serverList {
						selectServer = serverResponse.select_server
				  if server.type == selectServer.type {
							ServerInfo.set_data( server );
							break;
						}
					}
					serverData = ServerInfo.get_data()
				  if serverData == null ServerInfo.set_data( serverList[0] ) {
						ServerInfo.data = server
					ServerInfo.contentServer = server.detail.content
					}
					ServerInfo.get_contentBuild() {
						ServerInfo.contentBuild * = new ContentOnOffForBuild() // Includes some hardcoded settings
}
				}
			}
			SceneTitle.NextUpdateInfoViewState() // continue on to further startup
}
	}
}

CryptUtil.get_aesKey() {
	PluginsCommonForAndroid.get_aesKey() {
		Java::UnityPluginCommon::getAesKey() { return "!YJKLNGD"; }
	}
}

PluginsNetmarbleSForAndroid.get_gameToken() {
	Java::unityplugins.NetmarbleS.java:getGameToken() {
		com.netmarble.Session.java:getGameToken() {
			com.netmarble.core.SessionImpl.getInstance().getGameToken() {
				AuthDataManager.INSTANCE.getGameToken( applicationContext ) {
					return gameToken; // set in same with setGameToken, but this isn't used;
									  // instead, see below
				}
			}
		}
	}
}

SceneTitle.SignIn() {
	callback = new MyDelegate.Callback_1.FastJsonObject( SceneTitle.SignIn_m * *1 )
PluginsNetmarbleSForAndroid.GameCenterAuthenticate( callback ) {
		callback
  }
}
SceneTitle.SignIn_m * *1() {
	SceneTitle.SignInNetmarble() {
		PluginsNetmarbleSForAndroid.SignIn( scene ) {
			UnityPluginNetmarbleS.signIn() {
				r0 = new Session.SignInListener() {
onSignIn(Result result) {
UnityPluginNetmarbleS.printChannelConnectOption(result)
UnityPluginNetmarbleS.InitializePromotionView()
str = UnityPluginNetmarbleS.session.getPlayerID()
UnityPluginNetmarbleSIAP.RequestSkuList(str)
Common.SendMessageToUnity("OnSignIn", str) {
PluginsCallback.OnSignIn(str) {
value = PluginsNetmarbleSForAndroid.ChangePlayerIdWithCondition(str)
PluginsNetmarbleS.set_PlayerId(value)
(Plugins.get_thirdparty).OnSignIn(value)
SceneTitle.OnSignIn()
}
}
			}
		}
		Netmarble.Session.signIn( r0 ) {
			r1 = new SignInListener() {
onSignIn(Result result) {
r0.onSignIn(result)
}
}
		  core / SessionImpl.signIn( r1 ) {
				if ( checkSessionStatus( r1, false ) ) {
					doSignIn( r1 ) {
						r2 = new Function() {
invoke(Result result, JSONObject jSONObject) {
if result.isSuccess() || result.getCode() == 65538 {
							int optInt = jSONObject.optInt( "errorCode", -1 ); // org.json.JSONObject
							if optInt == 0 {
								JSONObject jSONObject2 = jSONObject.getJSONObject( "resultData" );
								String string = jSONObject2.getString( ItemKeys.ACCESS_TOKEN );
								HashMap hashMap = new HashMap(); // java.util.HashMap
								hashMap.put( AuthDataManager.KEY_GAME_TOKEN, string )
							responseOnSignIn( r1, result ) {
									r1.onSignIn( result )
							   }
							}
						}
					}
				}
				Utils.getAdvertisingId( applicationContext, new Utils.AdvertisingIdCallback() {
onReceived(str) {
SessionNetwork.signIn(PlatformDetails.getGateWayUrl(), SessionImpl.getPlayerID(),SessionImpl.getDeviceKey(),Configuration.getGameCode(),Utils.getAndroidID(),SessionImpl.getCountryCode(),str,r2) {
url = gateWayUrl + SERVICE_NAME + "v2" + Games.EXTRA_PLAYER_IDS + playerID + "deviceKeys" + deviceKey + ItemKeys.ACCESS_TOKEN + ? + IAPConsts.KEY_NMDEVICE_KEY"="androidID + & + "countryCode=" + countryCode + & + "adId=";
				NetworkHelper( uri, "GET", getCONVERTER_JSON_OBJECT );
				NetworkHelper.AddHeaders {
				Accept: "application/json",
Content - Type: "application/json",
GameCode: gameCode
				}
				NetworkHelper.execute( r2 );
			}
		}) {
			onReceived( new String() )
	 }
	}
}
}
}
}
}
Utils.getAndroidID() {
	// ANDROID_ID is a user-, app-, and device-specific 64-bit number
	// expressed as a hexadecimal string
}
SessionImpl.getCountryCode() {
	return PlatformDetails.getCountryCode( applicationContext ) {
		return getSharedPreferences( context ).getString( "NetmarbleS.CountryCode", null ) {
			// from NetmarbleS.Auth.xml
			return "US"
			}
	}
}
PlatformDetails.getGateWayUrl() {
	return gateWayUrl = REAL_GATE_WAY_URL = "https://apis.netmarble.com";
}
SessionImpl.getPlayerID() {
	return checkPlayerID() {
		playerID = AuthDataManager.getPlayerID( context )
	if playerID is null or empty {
	playerID = UUID.randomUUID().toString().replace( "-", "" ).toUpperCase( Locale.ENGLISH )
	AuthDataManager.setPlayerID( playerID )
	}
	return playerID
}
}
SessionImpl.getDeviceKey() {
	deviceKey = AuthDataManager.getDeviceKey( context )
if deviceKey is null or empty {
deviceKey = = UUID.randomUUID().toString().replace( "-", "" ).toUpperCase( Locale.ENGLISH );
	AuthDataManager.setDeviceKey( context, deviceKey )
}
return deviceKey
}
Configuration.getGameCode() {
	ConfigurationImpl.getGameCode() {
		return gameCode;
	}
}
SceneTitle.OnSignIn() {
	SceneTitle.RequestPermission()
SceneTitle.NextStepByNetmarbleSignIn()
}
Session.initialize() {
	ConfigurationImpl.initWithXml() {
		int identifier = applicationContext.getResources().getIdentifier( "nmconfiguration", "xml", applicationContext.getPackageName() );
		loadXml( applicationContext.getResources().getXml( identifier ) ) {
			String str = null;
			int eventType = xmlPullParser.getEventType();
			while ( eventType != 1 ) {
				if ( eventType == 2 ) {
					str = xmlPullParser.getName();
				} else if ( eventType == 4 ) {
					if ( true == str.equalsIgnoreCase( "gameCode" ) ) {
						this.gameCode = xmlPullParser.getText();
					}
				}
			}
		}
	}
}
// in JADX/base/resources/res/xml/nmconfiguration.xml,
// <gameCode>mherosgb</gameCode>

// (Back up to SceneTitle**SetServerDataOK)—>SceneTitle**ShowUpdateInfoView
// (Callback)SceneTitle.<ShowUpdateInfoView>m_0();
// Showtermsofservice
// packettransfer**checkaccount
// packettransfer$$Checkaccountok
// sceneTitle$$oncheckaccountok
// scenetitle**showtermsofservice (again)
// SceneTitle$$SignIn
// Plugins\_\_signin
// - Get access token

// Back up the chain to SceneTitle$$OnSignIn
// SceneTitle\$\$NextStepByNetmarbleSignIn->
// SceneTitle__NextStepByConnect->
// SceneTitle$$CheckCertificationWithCondition->
// SceneTitle\$\$PreLogin->
// ServerDetail**get_WebServerSSL: returns websvr_ssl (https://mherosgb.netmarble.com/NM/)
// ServerInfo**get_SslURL: calls get_WebServerSSL
// URL: concat surl, “PreLogin”: https://mherosgb.netmarble.com/NM/PreLogin
// PacketTransfer**PreLogin: gathers form data information, calls WWWUtil_PostSSL
// Form data:
// - cID: PluginsNetmarbleS$$get_PlayerId; set_PlayerId (general: random UUID with -s removed, all upper case)
// - dID: GetDeviceId2; mine is in ff_openudid.xml, general:
//     - str=android.os.SystemProperties.ro.serialno or 0
//     - str2=same
//     - string2=UnityPlayer.currentActivity.getContentResolver() android_id or 0
//     - str3=0,0,string2
//     - str4=str,str2,string2 if all 0, randomUUID()
//     - deviceName = Devices.getDeviceName() = e.g., “HTC One”
//     - md5(str3+deviceName)”-“md5(str4+deviceName)
//     - Simple valid = md5(“0,0,0HTC One”)”-“md5(randomUUID+”HTC One”)
// - gameToken: (from above apis.netmarble.com URL)
// - platform: android
// - ver: 6.2.0
// - lang: en(?)
// - country: US(?)
// - ds: CommonUtil$$IsDaylightSavingTime 1(?)
// - client_ip: get_ipAddress 127.0.0.1(?)
// - srvPush: get_allowGame(get_PushNotification) 1(?)
// - de: get_deviceModel “HTC One” (?)
// - pan: Panho$$isEnableLimit(0,1,0) 0(?)
// - pan2: Panho$$isEnable(0,1,0) 0(?)
// - timeZone: -08:00(?)
// WWWUtil**PostSSL: processes & submits form data; on success passes result to PacketTransfer\_\_PreLoginOK
// Form processing:
// base url=Https://mheroesgb.netmarble.com/NM/PreLogin?cKey=fRealtimeSinceStartup(sec)

SceneTitle.PreLogin() {
	PacketTransfer.PreLogin( scene SceneTitle ) {
		playerId = PluginsNetmarbleS.get_PlayerId()
	  WWWForm.AddField( "cID", playerId )
	  dID = PluginsCommonForAndroid.GetDeviceId2()
	  WWWForm.AddField( "dID", dID )
	  gameToken = PluginsCommonForAndroid.get_gameToken()
	  WWWForm.AddField( "gameToken", gameToken )
	  platform = PluginsCommonForAndroid.get_platform()
	  WWWForm.AddField( "platform", platform )
	  ver = PluginsCommonForAndroid.get_bundleVersion()
	  WWWForm.AddField( "ver", ver )
	  lang = LocalizationModule.GetLanguageCode()
	  WWWForm.AddField( "lang", lang )
	  country = PluginsNetmarbleS.GetCountryCode()
	  WWWForm.AddField( "country", country )
	  ds = CommonUtil.IsDaylightSavingTime()
	  WWWForm.AddField_2( "ds", ds )
	  ip = PluginsCommonForAndroid.get_ipAddress()
	  WWWForm.AddField( "client_ip", ip )
	  push = PushNotification.get_allowGame()
	  WWWForm.AddField_2( "srvPush", push )
	  WWWForm.AddField( "de", SystemInfo.get_deviceModel() )
	  WWWForm.AddField_2( "pan", Panho.IsEnableLimit() )
	  WWWForm.AddField_2( "pan2", Panho_isEnable() )
	  WWWForm.AddField( "timeZone", PluginsNetmarbleS.GetTimeZone() )
	  url = ServerInfo.get_SslURL() + "PreLogin"
	  WWWUtil.PostSSL( url, form, scene, "PreLoginOK", "PreLoginFail", UseFastJson = true ) {
			if url contains? append "&"
else append "?"
			 append "ckey=" + Time.get_realtimeSinceStartup()
			 retry = true
			 islock = true
			 wwwType = 2
			 param = url
			 isGamePacket = true
			 isSequenceProcess = true
			 useFastJson = true
			 WWWUtil.GetRoutine_1( data ) {
				// as above to GetRountine_internal_c_Iterator0.MoveNext(),
				// but with wwwType = 2, same as wwwType = 1
			}
		}
	}
}
WWWUtil.Parse( CryptUtil.PacketDecode( bytes, WWWUtil.UsePacketKeyFirst( param ) ), data, descRef )
WWWUtil.UsePacketKeyFirst( param ) {
	if param is nullorempty return true
	if param contains "GetVersion?" return false
	if param contains "GetNotice?" return false
	else return true
}
CrypUtil.PacketDecode( encBytes, usePacketKeyFirst ) {
	if usePacketKeyFirst {
		key = CryptUtil.get_packetKey()
		if key is not nullorempty {
			bin = CryptUtil.Decrypt( encBytes, key ) {
				rij = new RijndaelManaged()
				rij.set_Mode( 1 )
				rij.set_Padding( 2 )
				rij.set_KeySize( key.length << 3 )
				rij.set_BlockSize( 0x80 )
				keyBytes = Encoding.UTF8.GetBytes( key )
				rij.set_Key = keyBytes
				rij.set_IV = keyBytes
				decryptor = rij.CreateDecryptor()
				ICryptoTransform( decryptor, encBytes, 0, encBytes.Length)
				// ... more, but I'll defer to my previous CoreAesDecrypt code
			}
			if bin is not null {
				text = CryptUtil.Decompress(bin,bin.Length)
				if text is not nullorempty
					return text
		} // e.g., if packetkey is nullorempty, bin is null, or text is nullorempty
		key = CryptUtil.get_aesKey() + CryptUtil.get_aesKey()
		bin = CryptUtil.Decrypt( encBytes, key)
		if bin is null
			return CryptUtil.ByteToString( encBytes )
	} else { // usepacketkeyfirst == false
		key = CryptUtil.get_aesKey() + CryptUtil.get_aesKey()
		bin = CryptUtil.Decrypt( encBytes, key )
		if bin == null {
			key = CryptUtil.get_packetKey()
			if key is nullorempty {
				return CryptUtil.ByteToString( encBytes ) {
					return Encoding.UTF8.GetString( encBytes, 0, encBytes.length )
  				}
			} else { // packetkey is not null
				bin = CryptUtil.Decrypt( encBytes, key )
				if bin == null
					return CryptUtil.ByteToString( encBytes )
			}
		}
	}
	string = CryptUtil.Decompress( bin, bin.length ) {
		decompressor = CryptUtil.get_decompressor()
		bytes = decompressor.Decompress( bin, 0, bin.length )
		string = Encoding.UTF8.GetString( bytes )
		return string;
	}
	if string is not nullorempty return string
	else return CryptUtil.ByteToString( encBytes )
}
PacketTransfer.GetRecommendAllianceList() {
	param = "GetSuggestionAllianceList?" + "lang="
	WWWData data = WWWUtil.Get( param, scene, "GetRecommendedAllianceListOk", "", islock = true, option = 0, sendtotargetscene = false, timeout, \_retry = false, receiveresultwhenunlock = true, usefastjson = false, forcenotsequence = false, disableprogressui = false )
}
PacketTransfer.GetRecommendAllianceListOk() {
	key = "sgs"
	if data.hash["country"] is not nullorempty key = "sgl"
	if data.hash["country"] == "all" key = "sgs"
}
PacketTransfer.PreLoginOK( result ) {


}
}

// PacketTransfer**PreLogin: WWWUtil**PostSSL
// - Get text key
// —>PreLoginOK:
// PacketTransfer**PreLoginOK: sets:
// textKey: WWWResult->Json->key=tek
// packetKey: concat(WWWresult—>JSON->key=sessID->last 8 characters if length > 19, WWWresult->Json->key=cID->last 8 characters)
// CryptUtil**set*textKey(,tek,):
// pk = CryptUtil\_\_get_packetKey
// textKey* = CryptUtil**AESEncrypt(,tek,pk,)-->
// textKey\_ = CryptUtil**XOREncode(,textKey\_,)
// - Get asset
// - Decrypt asset
// - Format csv vs load into dbtable

// Maybe: - SceneTitle**Login_c**Iterator1**MoveNext (maybe from SceneTitle::Login)
// - DBTable**LoadDB
// - Various DBTable**get\_\*Table
// - TableUtility**Load\*\*\*
// - TableUtility**GetAssetPath (by type)
// - TableUtility**GetPathWithoutExtension + “.asset”
// - Which seems to dynamically determine loader to call, <type>$$LoadCSV

/************\*\*************\*************\*\*************
// Eventual goal: parse supplied text assets to CSV
// ************\*\*************\*************\*\*************/

// ISO8Set**LoadCSV() -->
// CSVLoader**Load(,,text/data/ISO8*SET.csv,) -->
// AssetBundleLoader\_\_Load_TextAsset*(,text/data/ISO8-SET.csv,) -->
// AssetBundleLoader**LoadAsset(, text/data/ISO8_SET.csv,typeof(TextAsset),) -->
// AssetBundleMgr**LoadAsset(,text/data/ISO*SET.csv,type,) (or UnityEngine_Resources**Load if not found)-->
// AssetBundleMrg**LoadAsset(,text,text/data/ISO8_SET.csv,type,false,) -->
// AssetBundleMgr_AssetBundleData**LoadAsset(assetBundle, text/data/ISO8_SET.csv,type,) -->
// UnityEngine_AssetBundle**LoadAsset(assetBundle, text/data/ISO8_SET.csv,type,), returning the TextAsset back
// up the chain to CSVLoader**Load:
// str = UnityEngine_TextAsset**get_text(textAsset,);
// CSVLoader**LoadFromString(textAsset,readvalue,str,0)-->
// CryptUtil**AESDecryptText(,str,) [0x010a1fc9]-->
// key = CryptUtil\_\_get_textKey(,)
// textKey* = CryptUtil*TypeInfo->CryptUtil_c-->CryptUtil_StaticFields-->textKey*
// packetKey = CryptUtil**get_packetKey(,) -->
// PluginsCommonForAndroid-->get_packetKey -->
// (decompiled) getPacketKey
// textKey = CryptUtil**XORDecode(,textKey*,)
// textKey = CryptUtil\_\_AESDecrypt(,textKey,packetKey,) (or, if packetKey is null, just textKey*)
// CryptUtil\_\_AESDecrypt(,str,key,) [0x010a055b] ->

// from (decompiled) classes/sources/com/seed9/unityplugins/UnityPluginCommon.java:
// AesKey = "!YJKLNGD"

// packetKey:

// PacketTransfer**PreLoginOK: (along with setting userId, sessionId, isEmailRegistered, cID, isNewAccount,apkToken,admit,textKey)
// If sessionId length >=20, take last 8 chars only; for
// (Last8 of sessionId)^2->
// CryptUtil**set_packetKey -> PluginsCommonForAndroid**set_PacketKey —>decompiled setPacketKey
// When getting packetKey, if null, concat(aesKey,aesKey) is used instead in WWWUtil**Get, but not in CryptUtil\_\_get/set_textKey. CryptUtil$$PacketDecode tries the AESkey^2 first.

// CryptUtil\_\_set_textKey(,string,) (from PacketTransfer$$PreLoginOK)
// Setting textKey\_ in CryptUtil_TypeInfo:
// Start at CSVLoader\_\_LoadFromString with CryptUtil_TypeInfo initializer (43ef37c, in .bss) (starting at 10a45d7):
// XOREncode/XORDecode uses xor_table in CryptUtil_TypeInfo, but since it uses it for each,
// can't I just use any table?

// For this program, reorganizing based on dependencies, so the idea is just to
// call GetCSV(Type) or something similar (maybe even an umbrella GetAllCSV or the like)
// and determine which parts need to be called to simulate a login and download or
// otherwise obtain needed data. Would also make a "force" flag to update all the date

// Where reasonable below, methods have the same name as the function's basename
// in libil2cpp.so (without having the namespace, it may combine parts from multiple
// namespaces)

//TablePath = text/data/
//TableName = TableNameAttribute/CSVTableNameAttribute
//ext = csv
// \*/

/\* Most are simple base64-encoded strings the base-64 strings when decoded have a ^@ (null) before every character after every character, presumably due to the use of 16-bit characters? Should effectively strip when possible or otherwise work around Many are within the device/data/media/0/Android/data/com.netmarble.mherosgb/files/bundle/text asset bundle (These have already been decoded to output/) Others appear to be identified by TableName/TableNameAttribute/CSVTableNameAttribute but I don't yet know where they're stored TableNameAttribute and CSVTableNameAttribute appear to be set by their respective ctors called by various anonymous functions with names/"filenames" strings from .rodata

The simple \*.csv textassets can be exported and (when necessary), base64 decoded with only command line base64 -D -i filename Without rigorous testing, the TableNames appear to be MonoBehavior/MonoScript pairs in bundle/text, though I'm not sure yet about decoding; need to better eval, e.g., loading IntAbilityGroupDataDictionary from text/data/action_ability.asset

Appears UABE can extract these to JSON files Perhaps these are the ones that are too large for CSVs?

Some are: TableNameAttribute: ACTION_AUTO_ABILITY ALLIANCE_EMBLEM_BG ALLIANCE_EMBLEM_BORDER ALLIANCE_EMBLEM_SYMBOL APPLY_OTHER_BY_TARGET ARENA_PARTICIPATION_RANK_REWARD HERO_SKILL MOB_SKILL GAME_CONFIG TOURNAMENT_EVENT_BATTLE_CONFIG SHADOWLAND_BATTLE_CONFIG SUPER_COOP_BATTLE_CONFIG PVP_BALANCE DOMINATION_BATTLE_CONFIG DANGER_ROOM_CONFIG ACHIEVEMENTS ALLIANCE_ACHIEVEMENTS INTRUSION_TEAMUP INTRUSION_BOSS LOADING_TIP LOADING_TIP_LIST WORLD_BOSS_REWARD WORLD_BOSS_HAVE_BONUS URU_PREMIUM URU_COMPOSE TOURNAMENT_EVENT_REWARD SUPER_COOP_QUEST SUMMON SUBTYPE_GROUP_ID COUNTRY_LATLON ERR_PROCESS DOMINATION_MOB STORY_CAMPAIGN_TRAIT STORY_CAMPAIGN_LEVEL STORY_CAMPAIGN_HERO STAGE_FIRST_CLEAR_REWARD StageEnterBundleList TEAM_LEAGUE_RANK_REWARD TEAM_LEAGUE TEAM_LEAGUE_CONDITION SPECIAL_GEAR_ICON ... and lots more

CSVTableNameAttribute: ACTION_ABILITY ALLIANCE_EMBLEM ARENA_RANK_REWARD HERO_SKILL MOB_SKILL RANDOM_OPTION ADD_ABILITY_LIST

TableUtility**GetCSVPaths: text/data/ + TableName + .csv TableUtility**GetAssetPath: text/data/ + TableName + .asset

\*/

/_ Other interesting things: GlobalConstants\_\_\_ctor Maybe everything in DBTable->Fields _/

## Simplified Sequential Startup Review

SceneTitle()
SceneTitle.Awake()
SceneTitle.Start()
SceneTitle.CheckServer() [ LOADING DATA... ]
PacketTransfer.SetServerData()
PatchSystem.get_url()
ServerInfo.GetRemoteFilePath()
WWWUtil.Get()
WWWUtil.GetRoutine_1()
WWWUtil.GetRoutine_c_Iterator1_MoveNext()
WWWUtil.GetRountine_internal()
WWWUtil.GetRountine_internal_c_Iterator0_MoveNext()
HTTPRequest.Send()
HTTPRequest.get_Response()
HTTPRequest.get_Data()
WWWUtil.OnRequestFinished()
WWWUtil.ResultOK()
HeroList.UpdateAll()
PacketTransfer.SetServerDataOK()
SceneTitle.SetServerDataOK()
ServerInfo.ParseVersionFile()
ServerInfo.ParseVersionFileCDN()
ServerInfo.GetContentBuild()
ContentOnOffForBuild()
SceneTitle.NextUpdateInfoViewState()
ServerInfo.InitNetmarbleSDK()
PluginsThirdParty.InitADTracking()
SceneTitle.ShowUpdateInfoView()
PluginsNetmarbleS.AddView()
PluginsNetmarbleS.NextView()
PluginsNetmarbleSForAndroid.ShowPromotionView()
AndroidJavaObject.CallStatic("registerDeviceCookie")
AndroidJavaObject.CallStatic("showPromotionView")
SceneTitle._ShowUpdateInfoView_m__0()
ServerInfo.get_IsMaintenance()
PopupUI.ShowUnderMaintenance()
ServerInfo.get_data() [ detail->maintenance ]
SceneTitle.CheckVersion()
PluginsCommonForAndroid.get_bundleVersion()
ServerInfo.GetVersionFloat()
ServerInfo.get_MinVersion()
ServerInfo.get_MaxVersion()
SceneTitle.showTermsOfService()
PacketTransfer.CheckAccount()
PluginsNetmarbleSForAndroid.GetPlayerID()
AndroidJavaObject.getPlayerID()
PacketTransfer.CheckAccountOK()
SceneTitle.OnCheckAccountOK()
SceneTitle.CheckBlockUser()
SceneTitle.ShowTermsOfServiceView()
PluginsNetmarbleSForAndroid.ShowTermsOfServiceView()
AndroidJavaObject.showTermsOfServiceView()
SceneTitle.OnTermsOfServiceView()
PluginsNetmarbleSForAndroid.GetCoppaStatus()
AndroidJavaObject.getCoppaStatus()
SceneTitle.ShowTermsOfServiceCoppaView()
SceneTitle.OnTermsofServiceCoppaView()
SceneTitle.SignIn()
PluginsNetmarbleS.GameCenterAuthenticate()
SceneTitle._SignIn_m__1() [ LOADING... ]
PluginsNetmarbleSForAndroid.SignIn()
AndroidJavaObject.signIn()
[Java]UnityPluginNetmarbleS.printChannelConnectOption(result);
[Java]UnityPluginNetmarbleS.InitializePromotionView();
[Java](UnityPluginNetmarbleS.session.getPlayerID()
[Java]UnityPluginNetmarbleSIAP.RequestSkuList(str);
PluginsNetmarbleSForAndroid.OnSignIn()
PluginsNetmarbleS.set_PlayerId()
PluginsThirdPartyForAndroid.OnSignIn()
SceneTitle.OnSignIn()
SceneTitle.NextStepByNetmarbleSignIn()
SceneTitle.NextStepByConnect()
Global.set_BeAppStartedAndSignIn()
SceneTitle.CheckCertificationWithCondition()
SceneTitle.PreLogin() [ CONNECTING TO THE SERVER... ]
PacketTransfer.PreLogin()
PluginsNetmarbleS.get_hasPlayerId()
PluginsNetmarbleS.get_PlayerId()
ServerInfo.get_SslURL()
WWWUtil.PostSSL()
WWWUtil.GetRoutine_1()
WWWUtil.GetRoutine_c_Iterator1_MoveNext()
WWWUtil.GetRountine_internal()
WWWUtil.GetRountine_internal_c_Iterator0_MoveNext()
HTTPRequest.AddBinaryData_2()
HTTPRequest.Send()
HTTPRequest.get_Response()
HTTPResponse.get_Data()
WWWUtil.OnRequestFinished()
WWWUtil.UsePacketKeyFirst()
CryptUtil.PacketDecode()
WWWUtil.Parse()
WWWUtil.ParseDefault()
WWWUtil.ResultOK()
WWWUtil.SendResult()
PacketTransfer.PreLoginOK()
MyMailInfo.ParseNoticeList()
CryptUtil.set_packetKey()
CryptUtil.set_textKey()
PacketTransfer.NotifyResult()
SceneTitle.PreLoginOK()
SceneTitle.PreLoginOKRoutine()
SceneTitle.GetCertValueAsync()
ContentOnOffForServer.get_CERT_VALUE_LOGIN_ASYNC()
Global.get_sceneMgr()
Global.AddComponent_1()
SceneTitle.ShowPromotionView()
PluginsNetmarbleS.AddView()
PluginsNetmarbleS.NextView()
PluginsNetmarbleSForAndroid.ShowPromotionView()
[Java] UnityPluginNetmarbleS.showPromotionView()
[Java] UnityPluginNetmarbleS.showWebView()
[Java] Common.SendMessageToUnity()
PluginsCallback.OnViewOpened()
SceneTitle.ShowPromotionView_c_AnonStorey2___m__0()
SceneTitle.StartPatch()
Define.get_UseCDN()
SceneTitle.SetProgressText() [ Checking Update Info. ]
PatchSystem()
PatchSystem.StartPatch_1()
PatchSystem.DeleteUnusedFolderOnce()
PatchSystem.DeleteUnusedBundleFolder()
PatchSystem.DeleteUnusedBundleEachFolder()
PatchSystem.Clear()
PatchSystem.SendGetVersionInternal()
PacketTransfer.GetVersion()
ServerInfo.get_DownloadVersion()
ServerInfo.get_URL()
ServerDetail.get_WebServer()
WWWUtil.PostSSL()
WWWUtil.GetRoutine_1()
WWWUtil.GetRoutine_c_Iterator1_MoveNext()
WWWUtil.GetRountine_internal()
WWWUtil.GetRountine_internal_c_Iterator0_MoveNext()
HTTPRequest()
HTTPRequest.SetFields()
HTTPRequest.Send()
HTTPManager.SendRequest()
HTTPManager.SendRequest_4()
HTTPManager.SendRequestImpl()
HTTPRequest.get_Response()
HTTPResponse.get_Data()
WWWUtil.OnRequestFinished()
WWWUtil.UsePacketKeyFirst()
CryptUtil.PacketDecode()
CryptUtil.get_aesKey()
CryptUtil.Decrypt()
CryptUtil.Decompress()
WWWUtil.Parse()
WWWUtil.ParseDefault()
WWWUtil.ResultOK()
WWWUtil.SendResult()
PacketTransfer.OnGetVersionOk()
PatchSystem.OnGetVersionOk()
AssetBundleManager.GetBundleEachLocalPath()
AssetBundleManager.GetBundleLocalPath()
PatchSystem.CreateDirectory()
PatchSystem.PatchCheckRoutine()
PatchSystem.PatchCheckRoutine_c_Iterator0_MoveNext()
PatchSystem.DeleteUnusedBundleFiles()
PatchSystem.CheckDownloadList()
LocalizationModule.GetLanguageCode()
VersionInfo.IsEqualHash()
AssetBundleMgr.IsEqualCacheHash_1()
AssetBundleMgr.IsEqualCacheHash()
PatchSystem.SkipEnable()
PatchSystem.DownloadData_Set()
PatchSystem.get_bundle_url()
PatchSystem.get_url()
PatchSystem.CreateUrl()
PatchSystem.get_base_url()
PatchSystem.GetBaseUrl()
ServerInfo.get_DownloadVersion()
AssetBundleMgr.GetBundleLocalPath()
CommonUtil.GetCachePath()
UnityEngine.Application.get_PersistentDataPath()
PatchSystem.get_bundle_each_url()
AssetBundleMgr.GetBundleEachLocalPath()
Extensions_2.Shuffle_11()
PatchConfig.get_instance()
PatchConfig()
List_1_PatchSystem_DownloadData__GetEnumerator(PatchSystem->downloadList)
DownloadAskPopup.Show()
SceneTitle.OnPatchAsked()
PatchSystem.Download()
PatchSystem.DownloadBegin()
SceneTitle.OnPatchStarted()
PatchSystem.DownloadRoutine()
PatchSystem.DownloadRoutine_c_Iterator2_MoveNext()
PatchSystem.GetDownloadData()
PatchSystem.GetStatusCount()
PatchSystem.RequestDownload()
PatchSystem.RequestDownloadRoutine()
PatchSystem.RequestDownloadRoutine_c_Iterator5_MoveNext()
Uri()
HTTPRequest()
HTTPRequest.SetRangeHeader()
HTTPRequest.SetHeader()
HTTPRequest.set_Tag()
HTTPRequest.set_UseStreaming()
HTTPRequest.set_StreamFragmentSize()
HTTPRequest.Send()
HTTPRequest.get_State()
HTTPRequest.get_Response()
HTTPResponse.get_IsSuccess()
PatchSystem_RequestDownloadRoutine_c_Iterator5_RequestDownloadRoutine_c_AnonStoreyE___m()
PatchSystem.OnDownloaded()
PatchSystem.get_unZipAfterDownload()
PatchSystem.RequestUnzip()
PatchSystem.UnzipRoutine()
PatchSystem.UnzipRoutine_c_Iterator1_MoveNext()
Zipper()
Zipper.UnzipRequest()
ZipperManager.UnzipRequest()
Zipper.UnzipwBegin()
Zipper.Unzipw()
Zipper.OnUnziped()
VersionInfo.SetHash()
AssetBundleMgr.GetBundleLocalPath_1()
PatchSystem.UpdateDownloadProgress()
SceneTitle.OnPatchDownloadProgress()
SceneTitle.UpdateProgress()
SceneTitle.OnPatchDownloadFinished()
PatchSystem.RequestDownloadRoutineForBackgroundDownload()
PatchSystem.RequestDownloadRoutineForBackgroundDownload_c_Iterator4_MoveNext()
PatchSystem.DownloadData_get_localRelativePath()
OnRequestFinished()
BackgroundDownload.Start()
BackgroundDownload.Start_1()
BackgroundDownload.LoadDownloads()
BackgroundDownload.SaveDownloads()
BackgroundDownload.OnRequestFinished()
OnRequestFinished.Invoke()
PatchSystem.OnDownloaded()
CommonUtil.GetFileSize_1()
PatchSystem.OnRequestFinished() [ Download Complete. ]
PatchSystem.OnDownloadCompleted()
PatchSystem.OnDownloadCompletedInTitle()
PatchSystem.Load()
PatchSystem.DeleteZipFiles()
PatchSystem.LoadNextFile()
SceneTitle.OnPatchEnded()
SceneTitle.Login()
SceneTitle.Login_c_Iterator1_MoveNext() [ Logging in to the server. ]
PacketTransfer.Login()
PacketTransfer.LoginOK()
MyData.OnLoginOK()
SceneTitle.OnLoginOK()
SceneTitle.GetCommonLogElements()
PluginsNetmarbleS.GetCommonLogElements()
PluginsNetmarbleSForAndroid.GetCommonLogElements()
AndroidJavaObject.CallStatic("getCommonLogElements")
PluginsCallback.OnGetCommonLogElements()
SceneTitle.OnGetCommonLogElements()
PacketTransfer.Login2()
PluginsNetmarbleSForAndroid_SetCommonLogEventListener()
PacketTransfer.Login2OK()
MyData.OnLogin2OK()
SceneTitle.OnLogin2OK()
SceneTitle.MoveToLobbyImpl() [ PLAY ]
PacketTransfer.IamAlive()


For downloading from the "Settings" screen:
ConfigUIGamePanel_OnClickDownloadStage()
BatchDownloadPopup_Show()
BatchDownloadPopup_OnClickDownload()
PatchSystem.BatchDownload()

Serially checked the filesystems for android emulator at different stages and compared them to the above.
After installation, but before running:
/data/app/*/com.netmarble.mherosgb-*/
/data/misc/profiles/cur/0/com.netmarble.mherosgb/primary.prof
After running, before downloading any patches:
+/data/data/com.netmarble.mherosgb/{app_webview, databases, files, no_backup, shared_prefs}
+/data/media/0/Android/data/com.netmarble.mherosgb/files/{Cookies,il2cpp}
+/data/system_ce/0/shortcut_service/packages/com.netmarble.mherosgb.xml
After installing the mandatory starting patches and the auto-downloads at the beginning:
+/data/data/com.netmarble.mherosgb/files/Netmarble_Fail_Log
+/data/media/0/Android/data/com.netmarble.mherosgb/files/bundle{,_each}, but
bundle_each has only the bgm_stage_downtown file and bundle has 707 files, including all playable characters, Temp, and text
After completing the batch download (e.g., the usual output from autoextract):
more in bundle/ and bundle_each, but no additional playable characters
All of bundle/ is available from mffer (without autoextract) currently
- need to figure out how to get the version available/downloaded, maybe just from the Google Play Store, or some download from NM? (Doesn't ServerInfo have a max-version or something?)
- not currently using anything not downloaded by mffer except maybe player_prefs
- may be able to only download content without using the play store at all for most things, just use that with autoanalyze (or equivalent)?
- consider moving autoextract to tools/, moving the download of the apks to autoanalyze, and getting everything needed via mffer (until autoanalyze can be moved in there as well)
- Can probably just download "text" and "localization_en" when needed and avoid everything else except when doing more exploration
- probably useful to do more accepting and reporting of errors from NM servers like needs app update or similar
- probably just abandon 0.1.0 in favor of current changes and move toward next version
- may need: mkmacvm as separate project, port of google-play-api to c# package as separate project
- would like: testing without sudo (which may not be possible with Xcode command line tools, but could probably use "insecure" sudo), everything except testing integrated into mffer, maybe old stuff that could still be useful (like autoextract using emulators) still in tools/


```
