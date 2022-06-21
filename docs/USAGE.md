# Using mffer

## Introduction

The mffer project develops software that creates and updates the
[mffer webapp](https://mffer.org). It is not necessary to read any of this
to [use the webapp](https://mffer.org).

The mffer tools obtain, extract, and analyze
[Marvel Future Fight](#marvel-future-fight) (MFF) program and data files,
process the resulting information into formats that are easily read by humans,
and presents that information interactively to assist with MFF game play. This
mffer User Guide describes the mffer tools and how to use them.

While even using the mffer tools may be of interest only to software
developers, data analysts, and others inclined to take apart systems and see how
they work, this guide does not discuss doing so with mffer itself. If you're
interested in extending, modifying, or improving the mffer tools, you may
prefer reading the [mffer Development Guide](Development.md).

## Marvel Future Fight

[Marvel Future Fight](http://www.marvelfuturefight.com/) (MFF) is a mobile (iOS
& Android) online role-playing game by
[Netmarble](https://company.netmarble.com/). It's set in the extended
[Marvel](https://www.marvel.com/) multiverse and has more than 200 characters to
collect and modify with dozens of different resources, and enough game modes to
make mastering all of them nigh impossible.

As such, the game has a large amount of data about the characters, resources,
stores, game modes and levels, and actions, even before taking into account the
variations between individual players. Although there is information available
from those who have significant experience playing the game, objective
quantitative data is rarely documented well and is of uncertain provenance.

## The mffer project

This project is intended to facilitate analysis of
[Marvel Future Fight](#marvel-future-fight) and provide access to the data it
uses for game play. This may be against the
[Netmarble Terms of Service](https://help.netmarble.com/terms/terms_of_service_en?locale=&lcLocale=en)
as well as those of multiple affiliates. The maintainer of this project has no
affiliation with NetMarble or its affiliates.

The project currently includes multiple components:

-   a [shell script](apkdl.md) to obtain the Marvel Future Fight program files
-   a [shell script](autoanalyze.md) to decompile and evaluate the program files
-   a [command line program](mffer.md) to obtain and parse the MFF data files
    into an open and usable format
-   a web app to present and use the game data for game play
    decision making

## Using the mffer webapp

The mffer webapp is at https://mffer.org.

The mffer webapp presents [mffer](mffer.md)-extracted data in a format to
help with in-game decision making.

The webapp should be intuitive. If additional explanation is required for proper
use, that is due to limitations of the developers, not the users; please
consider [filing an issue](https://github.com/therealchjones/mffer/issues) if
something is unclear.

### Requirements

The mffer webapp is built on Google Apps Script and uses Google Sheets as a
data store. It requires a web browser with robust JavaScript support for most
functionality. An Internet connection is required to use the webapp; it does not
have an "offline" mode.

### See also

For information about deploying the webapp rather than
using it, see the [Development Guide](Development.md#deploying-the-webapp).

## Using the mffer command line tools

The mffer command line tools obtain the latest version of Marvel Future Fight,
extract its usable data, process the data into a format suitable for human
review or computer use, and provide the data to the webapp. It is not necessary
to use the command line tools to
[just use the webapp yourself](https://mffer.org).

### Obtaining the mffer command line tools

"Releases" of mffer correspond to sets of files that are designed to be
"complete" in that any changes in them are designed to work together, they have
documentation that appropriately describes them, and they have pre-built
versions that can be downloaded and run without further building
or customization at the source code level.
[Download the latest release from GitHub](https://github.com/therealchjones/mffer/releases)
for your platform of
choice:

|                                     |                           |
| ----------------------------------- | ------------------------- |
| `mffer-`_`version`_`-linux-x64.zip` | Linux binary release      |
| `mffer-`_`version`_`-osx-x64.zip`   | macOS/OS X binary release |
| `mffer-`_`version`_`-win-x64.zip`   | Windows binary release    |

### Installation

No installation is needed or provided. Release packages include all necessary
files in a single directory. Unzip the package into a directory of your choice.

### Requirements

The mffer tool itself does not require any other specific software. It will
run on a system that
[supports .NET 5.0](https://github.com/dotnet/core/blob/main/release-notes/5.0/5.0-supported-os.md),
but no .NET or Mono runtime needs to be separately installed.

The other tools, apkdl and autoanalyze, have a few other requirements:

-   POSIX-like typical development environment (required for apkdl and
    autoanalyze)
-   Python 3 (required for apkdl)
-   [Ghidra](https://github.com/NationalSecurityAgency/ghidra)
    (required for autoanalyze)
-   [.NET 5.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/5.0)
    (required for autoanalyze)
-   Java 11 runtime or SDK
    (required for Ghidra)

macOS and most Linux distributions satisfy the needs for the "typical
development environment"; Windows requires additional POSIX-like software such
as Git Bash or Cygwin. (In addition to the defined
[POSIX utilities](https://pubs.opengroup.org/onlinepubs/9699919799/), `tar`,
`mktemp`, `git`, and other common utilities are used.) Most modern systems
require installation of a Java runtime (or SDK); we recommend the "Temurin" OpenJDK 11
distribution freely available from
[Adoptium.net](https://adoptium.net/temurin/releases/?version=11).

Additionally, other programs are downloaded and run by the `apkdl` and
`autoanalyze` scripts, so the system on which they are run must support these
programs, though the programs themselves do not need to be separately installed.

### The mffer workflow

#### Obtaining and processing the data files

```
mffer --outputdir output_directory
```

#### Exploring the data

Files in the _`output_directory`_ directory include `Roster-`_`version`_`.csv`
and `mffer-`_`version`_`.json` for the current version of Marvel Future Fight.
The first is a character-delimited spreadsheet of the many characters you can
choose in MFF and their different uniforms, all with stats at different skill
levels. The JSON file is a large human- and machine-readable file that includes
all the data mffer knows how to process (such as characters, uniforms,
dictionaries, and skills) as well as readable versions of all the other
text-based data that is downloadable. The files in the
_`output_directory`_`/files` subdirectory are the raw [Unity](https://unity.com)
data files called
"[AssetBundle](https://docs.unity3d.com/Manual/AssetBundlesIntro.html)s" used by
Marvel Future Fight. Exploring these are the best way to identify previously
unprocessed data. Those not processed by mffer (including graphics, level
data, and background music) can be explored with tools like
[AssetStudio](https://github.com/Perfare/AssetStudio).

#### Using and presenting the data

Upload the results using the webapp.

#### Exploring the code

A great deal of information may be accessible via the raw files in
_`output_directory`_`/files`, but the majority of code for running the game,
including algorithms and use of the data, are less easily evaluated directly.
More details and specifics of how the program works are given in
[The structure of Marvel Future Fight](mff.md), but much of the code you'll want to review is in a
file that is part of MFF's installation named `libil2cpp.so`. The mffer tools
can help facilitate this review by automatically processing this file before you manually
evaluate it further:

1.  Use [apkdl](apkdl.md) to download the latest Marvel Future
    Fight program files:

    ```
    apkdl -o output_directory
    ```

    It may be several minutes before you are prompted for a Google username and
    [app password](https://support.google.com/accounts/answer/185833).

2.  Use [autoanalyze](autoanalyze.md) to create and populate a Ghidra project
    with this version of Marvel Future Fight's program code:

    ```
    autoanalyze -i output_directory -o output_directory
    ```

    This may take several hours to complete.

3.  Reverse engineer as desired. Many more details are available in
    [The Structure of Marvel Future Fight](mff.md) and elsewhere.

## The mffer webapp

### Description

The mffer webapp is based on Google Apps Script, uses Google Sheets/Google
Drive, and is deployed at https://mffer.org via the
[Google Cloud Platform](https://cloud.google.com). This method of deployment is
not especially straightforward, and other better options may be more readily
available to other users. These will, however, require significant code
modification, as the mffer webapp code makes heavy use of Apps Script
(transpiled from TypeScript) and its associated APIs, the Google Picker, and
Google's OAuth 2.0 authentication.

### Deploying the webapp

#### Requirements

-   [Google Account](https://google.com/account) with access to
    [Google Apps Script](https://script.google.com), Google Drive, and Google
    Cloud Platform (the free tiers are all acceptable).
-   POSIX-like development system (such as macOS, Linux, or Windows with Cygwin)
-   [Node.js](https://nodejs.org) & npm

#### Setting Up Google Cloud Platform

GCP is somewhat complex to configure, and configuration within an existing GCP
account is beyond the scope of this document (and may be beyond the abilities of
this author). However, you may be able to create a basic project usable for
mffer webapp deployment in a few (relatively) simple steps. More in-depth
resources for setting up Apps Script in a Google Cloud Platform account include:

-   https://developers.google.com/apps-script/guides/cloud-platform-projects#switching_to_a_different_standard_gcp_project
-   https://github.com/google/clasp/blob/master/docs/run.md#setup-instructions
-   https://developers.google.com/picker/docs#appreg
-   https://cloud.google.com/resource-manager/docs/creating-managing-projects

In an effort to consolidate the above into a simple(r) set of instructions,
follow the below set of instructions to set up a project for mffer.

1. Login to https://console.cloud.google.com/projectcreate and enter a project
   name (and other info if desired). Press "Create".
2. Visit
   https://console.cloud.google.com/home/dashboard. Ensure the correct
   project is chosen in the project drop-down. Find the project number on the
   "Project Info" card and make a note of it.
3. Enable necessary APIs for your project by visiting the following links,
   ensuring the correct project is selected in the project drop-down, and
   pressing the "Enable" button:
    - [Apps Script API](https://console.cloud.google.com/apis/library/script.googleapis.com)
    - [Drive API](https://console.developers.google.com/apis/library/drive.googleapis.com)
    - [Picker API](https://console.cloud.google.com/apis/library/picker.googleapis.com)
    - [Sheets API](https://console.developers.google.com/apis/library/sheets.googleapis.com)
4. Create an OAuth Consent Screen by visiting
   https://console.cloud.google.com/apis/credentials/consent, choosing
   "External" user type, and pressing "Create". Enter the required information
   for the "App information" and "Developer contact information", and press
   "Save and continue". Choose "Add or remove scopes" and enter "https://www.googleapis.com/auth/drive.appdata",
   "https://www.googleapis.com/auth/drive.file", and "openid" under "Manually add
   scopes". Again press "Update" and "Save and continue". Add your own account
   as a "Test user", then press "Save and continue" one more time.
5. Visit https://console.cloud.google.com/apis/credentials/wizard and again
   ensure the correct project is shown in the project drop-down. First choose
   "Apps Script API" for "Which API are you using?" and select "User data"
   before pressing "Next". Don't add anything in "Scopes", just "Save and
   continue". For the "OAuth Client ID" section's "Application
   type", choose "Web application", and enter a name like "mffer". Press
   "Create" and make a note of the Client ID before pressing "Done".
6. Return to https://console.cloud.google.com/apis/credentials/wizard and again
   ensure the correct project is shown in the project drop-down. Choose
   "Google Picker API" for "Which API are you using?" and select "Public data"
   before pressing "Next". Make a note of the API Key and press "Done".

#### Uploading and configuring the webapp

1. In
   [Google Apps Script Settings](https://script.google.com/home/usersettings),
   enable "Google Apps Script API"
2. In the mffer repository's `tools` directory, install `clasp` and its
   dependencies:
    ```shell
    [mffer] $ cd tools
    [mffer/tools] $ npm install
    ```
3. Using the same Google account you used for your Google Cloud Platform
   project above, login to Google with `clasp`:
    ```shell
    [mffer/tools] $ ./node_modules/.bin/clasp login
    ```
4. Create the Google Apps project:
    ```shell
    [mffer/tools] $ ./node_modules/.bin/clasp -P ../src/webapp create --type sheets --title mffer
    ```
5. Add the webapp files to the project:
    ```shell
    [mffer/tools] $ ./node_modules/.bin/clasp -P ../src/webapp push -f
    ```
6. Open the Google Apps Script IDE:
    ```shell
    [mffer/tools] $ ./node_modules/.bin/clasp -P ../src/webapp open
    ```
7. Switch to using a standard Google Cloud Project by opening "Project Settings"
   (the gear icon), pressing the "Change project" button,
   and entering the project number you noted from step 2 of
   [Setting up Google Cloud Platform](#setting-up-google-cloud-platform) (or
   visit the [GCP Dashboard](https://console.cloud.google.com/home/dashboard)
   again if you need to copy it).
8. Open "Editor" (the &lt; &gt; icon), select "Code.gs" from the file list and
   press the "Run" button, which will prompt you to "Review Permissions" and
   approve access to your Google account. If prompted that "Google hasn't
   verified this app", select "Continue".
9. Open the webapp:
    ```shell
    [mffer/tools] $ ./node_modules/.bin/clasp -P ../src/webapp open --webapp
    ```
    If prompted for which deployment to use, press `<enter>` or `<return>`.
10. Choose "Setup mffer", then enter the OAuth 2.0 Client ID and OAuth 2.0
    secret you made a note of in the
    [Setting up Google Cloud Platform](#setting-up-google-cloud-platform)
    section (or obtain them again from
    https://console.cloud.google.com/apis/credentials using the provided
    links).
11. Visit the OAuth client ID page, and in the "Authorized redirect
    URIs" section, add the URI given in the webapp; press "Save".
12. Back on the webapp, use the "Authorize Google & save these settings" button to authenticate with Google once
    more; this will additionally lock the above settings and take the app out of
    "setup mode".
13. When the app reloads, under "Upload new mffer data" select a CSV file
    created by the mffer command line application and then "Confirm" it for upload.
14. To visit the deployed test version of the web app, use `clasp` at the
    command line:
    ```shell
    [mffer/tools] $ ./node_modules/.bin/clasp -P ../src/webapp open --webapp
    ```

The webapp is now set up for access but
[available only for testing, not to the general public](https://developers.google.com/apps-script/guides/web#test_a_web_app_deployment),
and will therefore work only for the test users you designated.
To deploy widely, first ensure privacy, restrictions, and access are secured in the
GCP project, then submit your app for [verification](https://developers.google.com/apps-script/guides/client-verification) by Google.

## See also

### Brief manuals

-   [apkdl](apkdl.md)
-   [autoanalyze](autoanalyze.md)
-   [mffer](mffer.md)

### Guides

-   [The mffer Development Guide](Development.md)
-   [The Structure of Marvel Future Fight](mff.md)
