# Developing mffer

## Introduction

[Marvel Future Fight](http://www.marvelfuturefight.com/) (MFF) is a mobile (iOS
& Android) online role-playing game by
[Netmarble](https://company.netmarble.com/). It is made with many
industry-standard tools, including programming in Java and C# for Unity (using
IL2CPP); is delivered as split APKs for Android from the
Google Play Store; uses Google, Apple, and Facebook systems for authentication;
and uses proprietary servers for user and game
data storage. As such, even if you don't play MFF, the
techniques used in this project for exploring those many components may contain
some useful knowledge for tackling similar projects.

The mffer project obtains, extracts, parses, and reports data from Marvel
Future Fight. Its scope ranges from providing basic game play tips to static
reverse engineering of the game and development of automatic processing of
extracted information to present data intuitively and understandably, enhancing
player decision making.

The objectives of this umbrella project are to:

-   obtain verifiable objective quantitative data about the game, typically using reverse engineering and related methods
-   make the data easily usable for decision making necessary to play the game effectively and efficiently
-   compare changes in the data between different releases/versions of the game
-   easily track important player-specific data to evaluate progress and plan modifications

These goals are ongoing ones and may never be reached. They may change. The
game may be discontinued by its publisher at any time, or its creators could
implement protections against extracting data that are not feasible to work
around. Netmarble may decide that working on mffer is against their Terms of
Service and kick all the project's contributors from the game, or even claim
criminality.

Since it is similarly certain one of these or countless other events will cause
the project to end, working on it must be a valuable experience in and of itself
rather than merely a path to the goals. Regardless of the many relatively (or
completely) arbitrary guidelines in this document, enjoy the work or do
something else. Respect others' rights to make the same choice, and to change
their minds (or change their minds back) at any time. Abide by the
[contributing guidelines](contributing.rst) and the accompanying
[Contributor Covenant](conduct.rst). Enjoy yourself, and provide a
community in which others can do the same.

And, on a somewhat more technical note, read on for details on just a few of the
myriad ways you can help develop the mffer project as part of that community.

## About this guide

The primary goal of this document is to inform development of the mffer
project specifically, though many details will be applicable to other projects.
However, the document is no more self-contained than GitHub contains the whole
of knowledge on programming. A great deal of familiarity with a great many
technical subjects is assumed both explicitly and implicitly. Where considered,
links to further information or instruction are provided, but readers at most
skill levels will benefit from their own research into topics with which they
have less experience and more interest. In addition, please ask questions on the
[issues list](https://github.com/therealchjones/mffer/issues) anytime; it
doesn't have to be a "real issue" (though those are welcome as well).

This guide is roughly organized by stages of the development process, preceeded
by several sections which apply more broadly. This means a great many
cross-references are necessary to avoid duplication and its inevitable result,
conflicting information.

This guide does not attempt to instruct the reader in specific programming
languages or algorithms, or in the "best practices" for using them. Where
possible, instructions that apply specifically to the mffer project are noted.
These are generally in regards to choosing a specific way to do something that
really doesn't matter (see, for instance, [the section on whitespace](#whitespace)),
and aren't necessarily the best way, much less the only way.

Finally, the mffer tools facilitate extracting information from and about MFF, and can
be used without a great deal of knowledge of the game's technical aspects and
inner workings. However, developing the tools may require more understanding of
software created with Unity in general and of Marvel Future Fight in particular.
That information---and how it was gathered---is explored in
[The Structure of Marvel Future Fight](mff.md).

## Copyright & licensing

Though some files adapted from other projects are released under more
restrictive licensing, most of mffer is in the public domain. (See
[the license](license.rst).) This means that you're free to do with it what you want,
without other copyright restrictions or requirements of your own work, unless
you adapt one of those other files. If there are any in the current version of
the product, they contain the appropriate license
notifications within the files themselves, and are also listed below with links
to the license requirements.

| file   | original project | license |
| ------ | ---------------- | ------- |
| _None_ |                  |         |

If you adapt some of the mffer code and want to contribute it _back_ into
mffer, it should be similarly released into the public domain. In addition,
contributing code to the project from other sources requires careful examination
of the licensing of those sources, and contributing original code requires
developers to specifically note the license (or release) under which their code
is provided. Pull requests with more restrictive licensing are complicated.

Note that the binary releases likely include a greater variety of
copyright-protected content, as included by the build process.

## Versioning

mffer uses a (slightly restricted) version of
[Semantic Versioning 2.0.0](https://semver.org) for version numbers.
Specifically, mffer versions are all of the form _major_._minor_._patch_,
without prefixes or suffixes of other formats.

While no stable release (and thus no stable API) has been completed, the major
version will remain 0. The minor version will continue to be incremented for any
changes to what is _expected to be_ the API. The patch version will change with
any other "releases". The first (unstable) release (without a stable API) will
be version 0.1.0.

## Writing documentation

Documentation is important.

### Source tree

With few exceptions (notably, a brief `README`, the `LICENSE`, `CONTRIBUTING`,
and the `CODE_OF_CONDUCT`), documentation sources should be in the `docs`
directory. The files in the root directory are intended to be complete and to be
read as is, or rendered as Markdown on GitHub. Documentation sources in `docs`
need not be as easily viewable. `docs/api/` is the placeholder home to the
auto-generated API reference and should generally not be edited.

### README

README files in mffer are designed to be read on GitHub with reference to the
more formal documentation (and are not included in the formal documentation). In
the root directory of the project, the `README` file is meant to be information
used "at a glance". Other `README` files are typically placeholders intended to
notify the reader that the "real" documentation is elsewhere and that the
directories in which they reside are used for source files, not for complete
readable docs.

### Generating the docs

Documentation is generated from source files, with the exception of the files in
the root directory of the repository and files named README.md. The script
`tools/mkdocs.sh` can be used to generate the documentation tree for testing
(and is used by the automated system at https://readthedocs.io). Briefly, Read
the Docs processes the repository as follows:

1. Upon receiving notice that a pull request has been made, a commit has been
   tagged, or a branch has been added, Read the Docs makes a shallow git clone
   of the repository.
2. Read the docs reads /.readthedocs.yaml, which describes the virtual machine
   used for generating the documentation. This configuration instructs Read the
   Docs to then install the necessary software before
3. Read the Docs runs the `mkdocs.sh` script with the `--prebuild` option, which
   in turn modifies the source tree to be something more amenable to Read the
   Docs's automated process and builds API documentation from the mffer source
   code using Doxygen before exiting.
4. Read the Docs then uses the `tools/conf.py` configuration (which has been
   moved into the `docs` directory by `mkdocs.sh`) to generate the documentation
   from `docs` using Sphinx.
5. Finally, Sphinx copies the API files generated by Doxygen into the same
   directory tree as its own output, and Read the Docs uploads the resulting
   tree to a web server.

## Writing code

### Coding Style

Consistent coding styles, even if mostly arbitrary (as the ones here certainly
are) allow easier reading and review of code, more rapid improvement, and better
project integration. As the project uses multiple file types and programming
languages, styles that they can all share are recommended. Some of the specifics
are listed below.

#### Whitespace

The overarching message is that whitespace is good. It adds to readability in
many ways. The initial indenting whitespace (where needed) in each file is a tab
character, not multiple spaces. Additionally, spaces should be used liberally to
separate surrounding parentheses, braces, brackets, and operators from nearby
content. The major exception to this rule is that an opening bracket of any kind
should almost always be on the same line as the function or method call, class
or struct definition, or other label associated with it. A space need not be
between the function or method call label and its associated parentheses, but
can be if it increases readability. More detailed descriptions of spacing
associated with specific circumstances can be gathered from the
EditorConfig file; the nonstandard extension EditorConfig settings for OmniSharp are
documented in Microsoft's
[.NET & C# Formatting Rules Reference](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/formatting-rules).

#### Code Style

Different code structures and algorithms are widely acceptable, as long as the
function of the code is clear and doesn't introduce additional risk for error.
In general (whitespace and choice of programming languages notwithstanding),
excellent guides for coding practices to use and to avoid can be found in:

-   [GitLab Style Guides](https://docs.gitlab.com/ee/development/contributing/style_guides.html)
-   [Google Style Guides](https://google.github.io/styleguide/)
-   [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/inside-a-program/coding-conventions)

Where necessary, coding style for individual pull requests can be discussed
along with the content of the code submitted. If needed, more specific
guidelines may be added to this document in the future.

#### Comments

Use XML commenting to document all types and members, not just public.

## Setting up a development environment

Using exactly the same development tools as other developers is neither
appropriate nor desired. Some overlap when working on the same project is
needed, such as the expectation to use identical (or at least quite similar)
runtimes. Others are personal preference, and somewhere between these extremes
are tools that are not required but may make the work easier. When a tool is
purely a matter of preference, it is included in these recommendations only if
the project has included data to somehow enhance the use of that tool---for
instance, the many extensions recommended for Visual Studio Code are not at all
required, but the recommendations and associated settings are in the repository
itself, so they're recommended here as well. Finally, remember that these are
requirements and recommendations for full development of mffer, not
necessarily just for building from source files, and certainly not just for
running the programs.

### Build requirements

-   a vaguely POSIX-compatible development environment (and some near-ubiquitous
    POSIX-like tools that aren't strictly in the POSIX standard, like `tar` and
    `mktemp`)
-   [.NET 5.0 SDK](https://dotnet.microsoft.com/download/dotnet/5.0)
-   [Node.js with npm](https://nodejs.dev) (with the `npm` command in your path)
-   [Google account](https://myaccount.google.com/) with access to [Google Apps Script](https://script.google.com/)
-   [git](https://git-scm.com)
-   Python 3
-   a vaguely modern computer with an undetermined minimum quantity of RAM that
    is probably several gigabytes

Specific configurations on which the build process is tested are noted in the
[Testing mffer section](#testing-mffer).

### Program requirements

Though not strictly required for development of the mffer tools, the
requirements for running the programs themselves additionally include:

-   [Ghidra](https://github.com/NationalSecurityAgency/ghidra)
    (required for autoanalyze)
-   Java 11 runtime or SDK (required for Ghidra); consider
    [Temurin 11](https://adoptium.net/?variant=openjdk11&jvmVariant=hotspot)

### Recommendations

-   [Visual Studio Code](https://code.visualstudio.com)

### Tools

The easiest way to ensure coding style is consistent throughout the project is
to use tools that enforce this style wherever possible. None of the below is
required to begin contributing to the project, but may be exceedingly helpful to
those doing so with any frequency. As such, certain files and settings are
included in the project to ease the consistent use of these tools by all
contributors.

#### Visual Studio Code

Much of the initial work on the project has been done in
[Visual Studio Code](https://code.visualstudio.com). While this is in no way
required for contributing to the project, it is relatively easy to use VS Code
to set up an environment that automatically mimics much of the style used
throughout the project. If you clone or fork the current project repository,
your new one will include a `.vscode` directory that stores settings and
extension recommendations to use for this project in particular. If you use a
different editor, you should use one that allows you to set formats that are
applied automatically, and they should match those set in this project.

#### Formatters

Formatters for individual code types are often available as both standalone
tools and as extensions for Visual Studio Code. Where appropriate, specific
settings that are different than the defaults are kept in settings files
included in the repository.

| Formatter    | VS Code Extension                                                                                          | Configuration                                                    |
| ------------ | ---------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------- |
| EditorConfig | [editorconfig.editorconfig](https://marketplace.visualstudio.com/items?itemName=EditorConfig.EditorConfig) | `.editorconfig`                                                  |
| OmniSharp    | [ms-dotnettools.csharp](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp)         | `.editorconfig`                                                  |
| Prettier     | [esbenp.prettier-vscode](https://marketplace.visualstudio.com/items?itemName=esbenp.prettier-vscode)       | None                                                             |
| shfmt        | [foxundermoon.shell-format](https://marketplace.visualstudio.com/items?itemName=foxundermoon.shell-format) | in `.vscode/settings.json`: `"shellformat.flag": "-bn -ci -i 0"` |

#### Linters

In addition, a linter is strongly recommended to quickly identify errors and
deviations from best practices. All code is expected to avoid both "errors" and
"informational" warnings from linters with rare exceptions for clear reasons.
Linters recommended for this project include:

| Linter     | VS Code Extension                                                                                  | Configuration |
| ---------- | -------------------------------------------------------------------------------------------------- | ------------- |
| OmniSharp  | [ms-dotnettools.csharp](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp) |
| shellcheck | [timonwong.shellcheck](https://marketplace.visualstudio.com/items?itemName=timonwong.shellcheck)   | None          |

### Setup

#### In Visual Studio Code

If you choose to use Visual Studio Code, open the Source Control (SCM) panel and
choose "Clone Repository" and enter https://github.com/therealchjones/mffer.git.
Choose a folder to place the new mffer directory within. Open it when
prompted, read the warning and choose the option to enable all features.

When prompted to "execute the restore command to continue", press "Restore". (If
no such prompt appears, open the terminal and run `dotnet restore`.)

When prompted to install recommended extensions, choose "Install" or "Show
Recommendations". (If no such prompt appears, you can open the Command Palette
to run "Extensions: Show Recommended Extensions" and install those listed under
"Workspace Recommendations".) In contrast to the rest of the tools installed in
this process, VS Code extensions will be installed globally, not within the
mffer directory hierarchy.

#### At the command line

If you choose not to use Visual Studio Code, setting up the environment can be
done at the command line. This will have the same results as
[above](#in-visual-studio-code) with the exception of the installation of Visual
Studio Code extensions and settings. All new tools will be installed within the
mffer directory hierarchy (but may still have effects like including settings
outside this).

1. Clone the mffer repository into a new directory:
    ```
    git clone https://github.com/therealchjones/mffer.git
    ```
2. Enter the directory and complete setup of the development environment:
    ```
    cd mffer
    dotnet restore
    ```

#### What happens in setup

Whether using Visual Studio Code or not, the `dotnet restore` command does the
bulk of setup of the multiple tools used in development of mffer. (The obvious
exception are those tools which are themselves extensions of Visual Studio
Code.) The following tools are set up within the `mffer/tools` directory tree:

-   NuGet packages
    -   MessagePack
    -   AssetsTools.NET
-   node.js tools
    -   clasp
    -   @types/google-apps-script
    -   stream-json

All of these tools can be removed (along with their installed dependencies and the
`mffer/build` and `mffer/release` directories) by running:

```
dotnet clean
```

## Changing mffer

### Making a custom `Program.cs`

mffer isn't (yet) built as a library, but you can still change the main entry
point in `Program.cs` as you see fit, rather than using the
default one with multiple command line options that automatically reads and
reports all supported data.

### Making a custom `Component`

The next step in customizing mffer for your own needs is to create a custom
derivative of the `Component` type. This
can be done in a [Custom `Program.cs`](#making-a-custom-programcs) or in a
separate file. The derivative type should include a
constructor that establishes the
needed assets or other requirements and override a
`Load()` method that parses imported assets into
the component members. More details are available in the
`Component` [API](api/index.rst) entry.

Accessing the newly designed component will typically involve
[customizing `Program.cs`](#making-a-custom-programcs) or
[changing more internals](#changing-mffer-internals) to customize the
`Version` type so that your component is loaded
along with all the others.

### Changing mffer internals

Changing the underlying workings of mffer may be necessary to alter
calculations, change how data is extracted or reported for existing
`Component`s, or to improve performance or other conditions. Every attempt has
been made to keep code appropriately encapsulated and abstracted so that
modifying one area need not break another, but this must be assumed to be far
from perfect.

### mffer best practices

In an effort to "compartmentalize" the code and continue to make
it customizable, please attempt to use the following "best practices":

-   Anything that is "user-customizable", such as filesystem pathnames or command line
    options, should be changeable by modifying only `Program.cs`.
-   _Validation_ of those options set in `Program.cs` should be done in called
    methods rather than in `Program.cs` itself wherever possible. For instance,
    if a directory _`data_directory`_ is passed as a command line option to set
    the directory containing data to extract, then passed to a method
    `GetData()` to read from the directory, _`data_directory`_ should be set
    within `Program.cs` but then `GetData()` should accept _`data_directory`_ as
    a string and perform validation (for instance, to ensure the directory
    exists and is readable), before using it, rather than expecting it to be
    done by `Program.Main()`. This keeps `Program.cs` to a minimum and allows
    simpler customization.
-   The `Program` class should interact only with the `Game` class. (This
    additionally means that `Game` should provide appropriate access to any
    internals that may be needed so that `Program` can reach them via `Game`.)
    For example, within `Program.Main` do not use:
    ```csharp
    foreach ( Version version in game.Versions ) {
    	string filename = $"{saveDir}/roster-{version.Name}.csv";
    	using ( StreamWriter file = new StreamWriter( filename ) ) {
    		version.Components["Roster"].WriteCSV( file );
    	}
    }
    ```
    Instead, avoid use of the `Version` class and exposing further internals of
    `Game` by writing an appropriate method `WriteAllRosters` that can be used like:
    ```csharp
    string filename = $"{saveDir}/roster-{version.Name}.csv";
    game.WriteAllRosters( filename );
    ```
    This same style, making best use of encapsulation and abstraction, should be
    used wherever possible in interactions between types.
-   Do not relax the "visibility" of types without very good reason. For
    instance, `Component` and `Game` should likely be `public` for the purposes of
    externally accessing them from `Program.Main` or externally creating new
    `Component` derivatives. However, limiting access of other types to
    `protected` or `private` will prevent both unnecessary clutter in the public
    API and breaking classes not used as designed.
-   Though not strictly required, much of the above can be promoted by including
    types as subclasses of others. For instance, rather than a separate
    `Version` class within the `Mffer` namespace, `Version` is a subclass of
    `Game`. This alone does not require `Version` to be `private` or
    `protected`, but the cumbersome nature of instantiating a `Game.Version`
    object encourages seeking a different way of accessing version informtion.

### Models & designs

#### The repository directory tree

```
mffer/
  .vscode/
  build/
  docs/
    api/
  release/
  src/
    classes/
    scripts/
    webapp/
  tools/
    autoextract/
    jython/
    node_modules/
    nuget/
    python/
```

The root mffer directory contains project-specific settings like
`.editorconfig` for formatting, `nuget.config` for tool and dependent package
management, `mffer.csproj` for build settings, and the brief "at-a-glance" README.

`.vscode` includes specific Visual Studio Code settings to provide consistent
formatting, recommend extensions, and make building, debugging, and other tasks
easier.

`docs` contains most in-depth documentation, including this file. The `api`
subdirectory is an empty one used for generating API documentation during the
build process.

`src` contains most of the source code for the project, with the Google Apps
Script project housed in the `webapp` subdirectory.

`tools` houses items used only for development and testing, including
`package.json` and an empty `node_modules` directory for Node.js-based programs,
`nuget` for NuGet packages, and `jython` and `python` directories for their
respective virtual environments. It also contains a directory for the deprecated
`autoextract` tool in case this is again needed for a future version of MFF.

#### Code structure

Corresponding to the above [best practices](#mffer-best-practices), the design
of mffer is based on the principles of abstraction, encapsulation, and
polymorphism. Much of the code is arranged in a classic object oriented fashion
with little functional or static typing.

Though described in far more detail in the [API](#the-mffer-apis), the basic
structure of the included code is:

-   CommandLine namespace (`CommandLine.cs`), a simple command-line parser
-   Mffer namespace
    -   Program class (`Program.cs`), user-facing code
    -   Game class (`Game Classes.cs`), dealing with all aspects of in-game data
    -   DataDirectory class (`Data Classes.cs`), interfacing between asset object and
        the filesystem

The `src` directory additionally includes the `scripts` and
`webapp` directories, which are planned to be internalized into the main mffer
code at some point.

Due to the reverse-engineering nature of the software and what documentation is
available regarding Unity file formats, several assumptions are made in order to
have a starting point for programming. Generally these are tested when used in
the code itself, but many are tested in the AssetFileTest class. More about the
assumptions about how Marvel Future Fight works (from a programming perspective)
are explicitly listed in [The Structure of Marvel Future Fight](mff.md), along
with how they correspond to the design structures of mffer. Refer to that
document and the [API](api/index.rst) for further detils.

### The mffer APIs

All included types and members are included in the [API documentation](api/index.rst), generated
as part of the build process from the triple-slash XML comments describing them
in the code itself.

## Building mffer

1. apkdl and autoanalyze are shell scripts and require no building.
2. To build the mffer program, from within the root directory of the repository,
    ```
    dotnet build
    ```
3. To build the documentation, from within the root directory of the repository,
    ```
    sh tools/mkdocs.sh
    ```
4. The web app must be uploaded but there's nothing to build; see
   [Deploying the webapp](#deploying-the-webapp)

## Testing mffer

### Testing environments

In order to ensure [software requirements](USAGE.md#requirements) are minimal
and known, formal testing of mffer is performed on basic virtual machines
created in a reproducible way. Where possible, output is then compared to "known
good" output from prior builds. There are standardized methods for creating the
virtual machines and for testing mffer on them. Scripts are provided to create
virtual machines for Parallels Desktop and test mffer on the virtual machines,
all running on a macOS host machine with only the addition of Parallels Desktop
Pro required to build the virtual machines. These scripts are available in the
`tools/` directory.

(Further information on using the command line to build and interact with
Paralells Desktop is available
[on the Parallels website](https://download.parallels.com/desktop/v17/docs/en_US/Parallels%20Desktop%20Pro%20Edition%20Command-Line%20Reference/);
the latest version should be available
[here](https://www.parallels.com/products/desktop/resources/).)

### Testing on macOS

Software used to fulfill the [build requirements](#build-requirements) and
runtime requirements is installed automatically on the virtual machine as needed
for the various phases of testing. The current testing environment on macOS
uses:

-   macOS 12.2.1 Monterey
-   Xcode Command Line Tools
-   Node.js 16.13.2
-   .NET 5.0 SDK
-   Temurin JRE 11.0.14.1_1
-   Ghidra

```
sh tools/testmac.sh
```

This script:

1. Creates a macOS virtual machine if needed
2. Installs Xcode Command Line Tools, Node.js, and .NET SDK
3. Builds mffer
4. Resets the virtual machine
5. Tests apkdl (which requires manual interaction)
6. Resets the virtual machine
7. Installs Temurin, .NET SDK, Ghidra, and Xcode Command Line Tools
8. Tests autoanalyze
9. Resets the virtual machine
10. Tests mffer

### Linux

1. Install Ubuntu 20.04 & apply all available updates
2. Install Parallels Tools
3. Test mffer
4. Test apkdl
5. Test autoanalyze

### Windows

1. Install Windows 10 & apply all available updates
2. Install Parallels Tools
3. Test mffer
4. Install
   [Temurin 11](https://adoptium.net/?variant=openjdk11&jvmVariant=hotspot)
5. Install Git (with Git Bash)
6. Test apkdl
7. Install [Ghidra](https://github.com/NationalSecurityAgency/ghidra/releases)
8. Install [.NET 5.0](https://dotnet.microsoft.com/download/dotnet/5.0)
9. Test autoanalyze

### Testing releases

"Semi-automated" testing of [releases](#releasing-mffer) is currently done
using virtual machines as noted above. Testing is simply ensuring the programs
run as expected; output files are not strictly compared due to expected minor
variations.

In order to ensure bugs are not the result of building on different systems, and
to ensure the minimum of additional software is sufficient, the aforementioned
scripts are each used to build the virtual machines and then build the candidate
release versions of the software on each system. Each of those builds is then
tested on each reference system, resulting in a testing checklist such as:

> #### apkdl
>
> -   [ ] windows
> -   [ ] macOS
> -   [ ] linux
>
> #### `autoanalyze`
>
> -   [ ] windows
> -   [ ] macOS
> -   [ ] linux
>
> #### mffer
>
> |                | build on windows | build on macOS | build on linux |
> | -------------- | ---------------- | -------------- | -------------- |
> | run on windows | [ ]              | [ ]            | [ ]            |
> | run on macOS   | [ ]              | [ ]            | [ ]            |
> | run on linux   | [ ]              | [ ]            | [ ]            |
>
> #### webapp deployment
>
> -   [ ] windows
> -   [ ] macOS
> -   [ ] linux
>
> #### webapp setup
>
> -   [ ] gmail
> -   [ ] google workspace

## Releasing mffer

1. Merge all code for the release into the main branch
2. Declare a "feature freeze" and create a new branch from main named for the release
3. Serially test and modify the release branch, building with the environment
   variable `VersionString=`_`releasename`_`-pre`.
4. Once testing is complete (including full testing _one last time_), `git tag -a `_`releasename`_ on the release branch.
5. Test yet again, this time without the environment variable. If more needs to
   be changed, increment the release name appropriately until a tagged commit
   completes testing as expected.
6. If appropriate (e.g., there are no intervening changes to main), merge the
   version branch back into main with this tag.
7. Create a GitHub release from the final tagged commit.
8. If later patches need to be made, apply them (separately to multiple release
   branches, if supported), test, and increment the tag on the branch as needed,
   then create the new release on GitHub.

### mffer versioning

mffer uses [Semantic Versioning 2.0.0](https://semver.org) for version
numbers. While no stable release (and thus no stable API) has been completed,
the major version will remain 0. The minor version will continue to be
incremented for any changes to what is _expected to be_ the API. The patch
version will change with any other "releases". The first (unstable) release
(without a stable API) will be version 0.1.0.

### Building a release

A "release" is a package of the mffer program and the associated scripts.
Creating a release builds the program from source as [above](#building-mffer),
but intentionally leaves out extra debugging information and (in most cases)
results in a single file mffer program that will only work on a specific
platform. While "official" releases are
[available for download](https://github.com/therealchjones/mffer/releases/), you
can build a customized version or test changes with your own copy of the source
code.

To choose a "name" for your release, tag the HEAD of your git repository:

```
git tag -a v<version_number> -m <release_message>
```

The name must be a string that starts with `v`. Official releases use the
[Semantic Versioning](#mffer-versioning) conventions, but you can use any
string starting with `v`. (If you don't want to tag the git repository, you can
alternatively set the environment variable `VersionString`.)

To build the release packages, use

```
dotnet publish -c release
```

The result will be files placed in the `release` directory of the source tree.
There are files are named `mffer-`_`version`_`-`_`platform`_`.zip` for each of the
built platforms (by default, `win-x64`, `osx-x64`, and `linux-x64`). These files
contain the mffer executable file and its associated scripts, apkdl
and `autoanalyze`, and may contain other supporting files. A
platform-independent file `mffer-`_`version`_`-net5.0.zip` includes several
other files needed to run the mffer program using the .NET 5.0 runtime (not
included).

## Developing the mffer webapp

The mffer webapp uses Google Apps Script, Google Sheets/Google Drive, and the
[Google Cloud Platform](https://cloud.google.com), and the "front end" may
optionally be hosted on a different web server. This method of deployment is
not especially straightforward, and other better options may be more readily
available to some users. This method is supported as that most easily available
to all users.

### Deploying the webapp

Deploying a version of the webapp without customizations is not itself a
"development" activity. However, it is included in this guide (rather than the
[User guide](USAGE.md)) as it requires obtaining the files via `git` rather than
in a release package and is, at best, not a strictly reproducible experience due
to the many confounding factors involved.

#### Requirements

-   [Google Account](https://google.com/account) with access to
    [Google Apps Script](https://script.google.com),
    [Google Drive](https://drive.google.com), and
    [Google Cloud Platform](https://cloud.google.com); the free tiers of these
    products are all sufficient. We recommend all the below steps are performed
    with a "testing" Google account rather than a primary or organizational
    account, but you should use the same account for all parts of the
    deployment.
-   POSIX-like development system (such as Linux, macOS/OS X, or Windows with
    Cygwin or another POSIX layer)
-   [Node.js](https://nodejs.org) & npm
-   Modern graphical web browser such as Google Chrome, Edge, Firefox, or Safari

#### Setting up the webapp development environment

1. Clone the [mffer Repository](https://github.com/therealchjones/mffer). We
   recommend then checking out a specific version from which to deploy the
   webapp rather than using the latest code. For instance, to create a new
   branch named "new-deployment" starting with the release code for version 0.1.0:

    ```
    [~] $ git clone https://github.com/therealchjones/mffer
    [~] $ cd mffer
    [mffer] $ git checkout -b new-deployment v0.1.0
    ```

2. The tools needed for webapp development are all included in the general
   [development environment](#setting-up-a-development-environment) section, and
   the `dotnet restore` step will install them all. However, if you are only
   interested in deploying the webapp, most of the other tools aren't necessary,
   and if desired you can manage with only `clasp` and its dependencies. You can
   limit your environment to those and a few other Node.js packages with:

    ```
    [mffer] $ cd tools
    [mffer/tools] $ npm install
    ```

#### Setting Up Google Cloud Platform

General Google Cloud Platform (GCP) configuration is beyond the scope of this
document (and may be beyond the abilities of this author). However, you may be
able to create a project usable for mffer webapp deployment relatively easily
following the below steps. More comprehensive resources for setting up Apps
Script in a Google Cloud Platform account include:

-   [Google Cloud: Creating and managing projects](https://cloud.google.com/resource-manager/docs/creating-managing-projects)
-   [Google Apps Script: Google Cloud Platform Projects](https://developers.google.com/apps-script/guides/cloud-platform-projects#switching_to_a_different_standard_gcp_project)

1. Login to https://console.cloud.google.com/projectcreate and enter a project
   name (and other info if desired). Press "Create".
2. Visit
   https://console.cloud.google.com/home/dashboard. Ensure the correct
   project is chosen in the project drop-down. Find the project number on the
   "Project Info" card and make a note of it.
3. Enable necessary APIs for your project by visiting the following links,
   ensuring the correct project is selected in the project drop-down, and
   pressing the "Enable" button. If prompted to create credentials, you don't
   need to do that at this time; we'll do so in a later step.
    - [Apps Script API](https://console.cloud.google.com/apis/library/script.googleapis.com)
    - [Drive API](https://console.developers.google.com/apis/library/drive.googleapis.com)
    - [Sheets API](https://console.developers.google.com/apis/library/sheets.googleapis.com)
4. Create an OAuth Consent Screen by visiting
   https://console.cloud.google.com/apis/credentials/consent, choosing
   "External" user type, and pressing "Create". Enter the required information
   for the "App information" and "Developer contact information", and press
   "Save and continue". Choose "Add or remove scopes" and enter "https://www.googleapis.com/auth/drive.appdata",
   "https://www.googleapis.com/auth/drive.file", and "openid" under "Manually add
   scopes". Press "Add to table" and "Update", then press "Save and continue". Add your own account
   as a "Test user", then press "Save and continue" one more time.
5. Visit https://console.cloud.google.com/apis/credentials/wizard and again
   ensure the correct project is shown in the project drop-down. Choose
   "Apps Script API" for "Which API are you using?" and select "User data"
   before pressing "Next". Don't add anything in "Scopes", just "Save and
   continue". For the "OAuth Client ID" section's "Application
   type", choose "Web application", and enter a name like "mffer". Press
   "Create" and make a note of the Client ID before pressing "Done".
6. Visit https://console.cloud.google.com/apis/credentials and click on the name
   of your newly-created OAuth 2.0 Client ID. Make a note of the "client secret"
   on the right side of the screen.

#### Uploading and configuring the webapp

1. Ensure you are signed out of all Google accounts except the one you used for
   your Google Cloud Platform project (and which you added as a "Test user")
   above.
2. In
   [Google Apps Script Settings](https://script.google.com/home/usersettings),
   enable "Google Apps Script API"
3. Using the same Google account you used for your Google Cloud Platform
   project above, login to Google with `clasp`:
    ```
    [mffer/tools] $ ./node_modules/.bin/clasp login
    ```
4. Create the Google Apps project and upload the files:
    ```
    [mffer/tools] $ sh ./webdeploy.sh -vN
    ```
5. Open the Google Apps Script IDE:
    ```
    [mffer/tools] $ ./node_modules/.bin/clasp -P ../src/webapp/gas open
    ```
6. Switch to using a standard Google Cloud Project by opening "Project Settings"
   (the gear icon), pressing the "Change project" button,
   and entering the project number you noted from step 2 of
   [Setting up Google Cloud Platform](#setting-up-google-cloud-platform) (or
   visit the [GCP Dashboard](https://console.cloud.google.com/home/dashboard)
   again if you need to copy it).
7. Open "Editor" (the &lt; &gt; icon), select "Code.gs" from the file list and
   press the "Run" button, which will prompt you to "Review Permissions" and
   approve access to your Google account. If prompted that "Google hasn't
   verified this app", select "Continue".
8. Open the webapp:
    ```
    [mffer/tools] $ ./node_modules/.bin/clasp -P ../src/webapp/gas open --webapp
    ```
    If prompted for which deployment to use, select the one labelled with the
    date and time of deployment and the version number "@1".
9. Choose "Setup mffer", then enter the OAuth 2.0 Client ID and OAuth 2.0
   secret you made a note of in the
   [Setting up Google Cloud Platform](#setting-up-google-cloud-platform)
   section (or obtain them again from
   https://console.cloud.google.com/apis/credentials using the provided
   links).
10. Visit the OAuth client ID page using the provided link, and in the
    "Authorized redirect URIs" section, add the URI given in the webapp; press
    "Save".
11. Back on the webapp, use the "validate these settings" button to check them,
    then press "Authorize & Submit" to authenticate with Google once more,
    again "Continue" if the "Google hasn't verified this app" prompt appears,
    and when prompted authorize the access to the app's files in Google Drive.
    This will lock the above settings and take the app out of "setup mode".
12. Use the webapp's "Login" button to authenticate once again with Google
    (which will create a "user" account for you), then choose the "Admin" link.
13. Under "Upload new mffer data" select a CSV file
    created by the mffer command line application and then "Confirm" it for
    upload.
14. To visit the deployed test version of the web app, use `clasp` at the
    command line:
    ```
    [mffer/tools] $ ./node_modules/.bin/clasp -P ../src/webapp open --webapp
    ```

### Testing the webapp

The webapp is now available for testing, but is not fully usable by everyone.
Site configuration settings (like the OAuth client secret) are stored in your Google
account, and everyone who accesses the webapp must be able to access these, so
the webapp "runs as" you. However, you don't want everyone using the webapp to
have access to your Google Drive, so this is very limited access; further access
requires the user to authorize the mffer webapp to connect to their own account.

While the app is in "test mode", anyone can access it, but only users you
specifically designate as "test users" will be allowed to log in (see
"[Test a web app deployment](https://developers.google.com/apps-script/guides/web#test_a_web_app_deployment)").
To end this restriction, the webapp must be submitted for
[verification](https://developers.google.com/apps-script/guides/client-verification)
by Google.

### Changing the webapp

Make the changes you desire to the webapp in the files and directories under the
repository's `src/webapp/` directory. To push a new version of the webapp to
the Google Apps Script servers for testing, again in the `tools/` directory, run

```
[mffer/tools] $ sh ./webdeploy -vO
```

If there are major changes to the code, the new deployment may require that you
re-verify permissions for the webapp as the developer. This can be done by again
opening the Apps Script console

```
[mffer/tools] $ ./node_modules/.bin/clasp -P ../src/webapp/gas open
```

and then in the IDE, open "Editor" (the `<>` icon), select "Code.gs" from the
file list and press the "Run" button, which if necessary will prompt you to
"Review Permissions" and approve access to your Google account.

You can then visit the new version of the webapp just like the old one:

```
[mffer/tools] $ ./node_modules/.bin/clasp -P ../src/webapp/gas open --webapp
```

To deploy multiple versions (for instance, a "production" one and another for
testing), see
[Create and manage deployments](https://developers.google.com/apps-script/concepts/deployments)
and the
[clasp deployment instructions](https://developers.google.com/apps-script/guides/clasp#deploy_a_published_project).
However, we recommend using a different GCP project and even a different Google
account for a "production" site; messing them up could be difficult to reverse.

### Hosting the webapp at a custom domain

Visiting the webapp for testing during development is most easily done from the
command line via

```
[mffer/tools] $ ./node_modules/.bin/clasp -P ../src/webapp/gas open --webapp
```

The webapp can also be visited by entering the URL this command uses into the
browser or bookmarking it. However, if you wish to share the site with others, a
more user-friendly address may be beneficial. If you have access to a web server
that allows you to upload an HTML document that will be served at a URL of your
choice, use the following steps to use that address to access the webapp.

1. [Deploy the webapp to Google Apps Script](#deploying-the-webapp) as described
   above.

2. Deploy the app again, this time to your personal site:

    ```
    [mffer/tools] $ sh ./webdeploy.sh -vO -w https://example.org -p example.org:index.html
    ```

    where the url of your final site is the argument to the `-w` option and the
    argument to the `-p` option is the destination host and file that will be
    used by the `scp` program to transfer the file.

3. Add your newly uploaded URL to the "Authorized Redirects"
   list for your OAuth client ID.

(Note that deploying a new version of the webapp to the custom url will again
require the full command listed in step 2.)

### Webapp limitations

The mffer webapp has some limitations imposed when serving the page using Google
Apps Script. These are are not problems when
[hosting the webapp at a custom domain](#hosting-the-webapp-at-a-custom-domain).

#### Persistent storage & user logins

Google Apps Scripts serves user-developed content from
[within a sandboxed iframe](https://developers.google.com/apps-script/guides/html/restrictions#top.html),
that uses the domain googleusercontent.com rather than google.com. This may
improve security for Google and its users. However, this means cookies and
other persistent storage used by the webapp is considered "third-party content"
and is blocked or quickly removed by some browsers. This means user logins for
the webapp won't last restarting the browser, and may even be deleted before the
browser is restarted. User information is stored in their Google Drive, so it's
not lost, but frequent logins may be annoying.

## See also

-   [The Structure of Marvel Future Fight](mff.md)
-   [Contributing to mffer](contributing.rst)
-   [The mffer API](api/index.rst)
