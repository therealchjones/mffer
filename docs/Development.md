# Developing `mffer`

## Highlights

-   [Building](#building-mffer)
-   [`Program.cs`](#making-a-custom-programcs)
-   [`Component`s](#making-a-custom-component)
-   [APIs](#the-mffer-apis)

## Full contents

- [Highlights](#highlights)
- [Full contents](#full-contents)
- [Introduction](#introduction)
- [Copyright & licensing](#copyright--licensing)
- [Setting up a development environment](#setting-up-a-development-environment)
	- [Build requirements](#build-requirements)
	- [Program requirements](#program-requirements)
	- [Recommendations](#recommendations)
	- [Setup](#setup)
		- [In Visual Studio Code](#in-visual-studio-code)
		- [At the command line](#at-the-command-line)
	- [Included tools](#included-tools)
- [Writing documentation](#writing-documentation)
	- [Source tree](#source-tree)
	- [README](#readme)
		- [At-a-glance README](#at-a-glance-readme)
		- [Comprehensive README](#comprehensive-readme)
		- [More about READMEs](#more-about-readmes)
	- [CONTRIBUTING](#contributing)
- [Writing code](#writing-code)
	- [Coding Style](#coding-style)
		- [Whitespace](#whitespace)
		- [Code Style](#code-style)
		- [Comments](#comments)
	- [Tools](#tools)
		- [Visual Studio Code](#visual-studio-code)
		- [Formatters](#formatters)
		- [Linters](#linters)
- [Making a custom `Program.cs`](#making-a-custom-programcs)
- [Making a custom `Component`](#making-a-custom-component)
- [Changing `mffer` internals](#changing-mffer-internals)
	- [`mffer` best practices](#mffer-best-practices)
	- [Models & designs](#models--designs)
		- [The repository directory tree](#the-repository-directory-tree)
		- [Code structure](#code-structure)
- [The `mffer` APIs](#the-mffer-apis)
- [Building `mffer`](#building-mffer)
	- [Building a release](#building-a-release)
- [Testing `mffer`](#testing-mffer)
	- [Testing environments](#testing-environments)
	- [Testing on macOS](#testing-on-macos)
	- [Linux](#linux)
	- [Windows](#windows)
	- [Testing releases](#testing-releases)
- [Releasing `mffer`](#releasing-mffer)
- [The `mffer` webapp](#the-mffer-webapp)
	- [Description](#description)
	- [Deploying the webapp](#deploying-the-webapp)
		- [Requirements](#requirements)
		- [Setting Up Google Cloud Platform](#setting-up-google-cloud-platform)
		- [Uploading and configuring the webapp](#uploading-and-configuring-the-webapp)
- [See also](#see-also)

## Introduction

The `mffer` project obtains, extracts, parses, and reports data from Marvel
Future Fight. Its scope ranges from providing basic game play tips to static
reverse engineering of the game and development of automatic processing of
extracted information to present data intuitively and understandably, enhancing
player decision making.

These goals are ongoing ones and may never be reached. They may change. The
game may be discontinued by its publisher at any time, or its creators could
implement protections against extracting data that are not feasible to work
around. Netmarble may decide that working on `mffer` is against their Terms of
Service and kick all the project's contributors from the game, or even claim
criminality. One day, we will die.

Since it is similarly certain one of these or countless other events will cause
the project to end, working on it must be a valuable experience in and of itself
rather than merely a path to the goals. Regardless of the many relatively (or
completely) arbitrary guidelines in this document, enjoy the work or do
something else. Respect others' rights to make the same choice, and to change
their minds (or change their minds back) at any time. Abide by the
[contributing guidelines](CONTRIBUTING.md) and the accompanying
[Contributor Covenant](../CODE_OF_CONDUCT.md). Enjoy yourself, and provide a
community in which others can do the same.

And, on a somewhat more technical note, read on for details on just a few of the
myriad ways you can help develop the `mffer` project as part of that community.

The primary goal of this document is to inform development of the `mffer`
project specifically, though many details will be applicable to other projects.
However, the document is no more self-contained than GitHub contains the whole
of knowledge on programming. A great deal of familiarity with a great many
technical subjects is assumed both explicitly and implicitly. Where considered,
links to further information or instruction are provided, but readers at most
skill levels will benefit from their own research into topics with which they
have less experience and more interest. In addition, please ask questions on the
[issues list](https://github.com/therealchjones/mffer/issues) anytime; it
doesn't have to be a "real issue" (though those are welcome as well).

## Copyright & licensing

Though some files adapted from other projects are released under more
restrictive licensing, most of `mffer` is in the public domain. (See
[LICENSE](../LICENSE).) This means that you're free to do with it what you want,
without other copyright restrictions or requirements of your own work, unless
you adapt one of those other files; they are clearly identified in the contents
of the affected files themselves and are additionally
[listed in the README](README.md).

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
requirements and recommendations for full development of `mffer`, not
necessarily just for building from source files, and certainly not just for
running the programs.

Details regarding the specific uses or purposes of the below are also documented
in the [Tools](#tools) section of [Writing code](#writing-code).

### Build requirements

-   a vaguely POSIX-compatible development environment (and some near-ubiquitous
    POSIX-like tools that aren't strictly in the POSIX standard, like `tar`, and
    `mktemp`)
-   [.NET 5.0 SDK](https://dotnet.microsoft.com/download/dotnet/5.0)
-   [Node.js with npm](https://nodejs.dev) (with the `npm` command in your path)
-   [Google account](https://myaccount.google.com/) with access to [Google Apps Script](https://script.google.com/)
-   [git](https://git-scm.com)
-   Python 3
-   a vaguely modern computer with an undetermined minimum quantity of RAM that
    is probably several gigabytes

Specific configurations on which the build process is tested are noted in the
[Testing `mffer` section](#testing-mffer).

### Program requirements

Though not strictly required for development of the `mffer` tools, the
requirements for running the programs themselves additionally include:

-   [Ghidra](https://github.com/NationalSecurityAgency/ghidra)
    (required for `autoanalyze`)
-   Java 11 runtime or SDK (required for `autoextract` and Ghidra); consider
    [Temurin 11](https://adoptium.net/?variant=openjdk11&jvmVariant=hotspot)
-   Python 3 (required for `apkdl`)

### Recommendations

-   [Visual Studio Code](https://code.visualstudio.com)

### Setup

#### In Visual Studio Code

If you choose to use Visual Studio Code, open the Source Control (SCM) panel and
choose "Clone Repository" and enter https://github.com/therealchjones/mffer.git.
Choose a folder to place the new `mffer` directory within. Open it when
prompted, read the warning and choose the option to enable all features.

When prompted to "execute the restore command to continue", press "Restore". (If
no such prompt appears, open the terminal and run `dotnet restore`.)

When prompted to install recommended extensions, choose "Install" or "Show
Recommendations". (If no such prompt appears, you can open the Command Palette
to run "Extensions: Show Recommended Extensions" and install those listed under
"Workspace Recommendations".) In contrast to the rest of the tools installed in
this process, VS Code extensions will be installed globally, not within the
`mffer` directory hierarchy.

#### At the command line

If you choose not to use Visual Studio Code, setting up the environment can be
done at the command line. This will have the same results as
[above](#in-visual-studio-code) with the exception of the installation of Visual
Studio Code extensions and settings. All new tools will be installed within the
`mffer` directory hierarchy (but may still have effects like including settings
outside this).

1. Clone the `mffer` repository into a new directory:
    ```shell
    $ git clone https://github.com/therealchjones/mffer.git
    ```
2. Enter the directory and restore extensions and packages that come from other
   sources:
    ```shell
    $ cd mffer
    $ dotnet restore
    ```
3. Optionally, add the following directories to your `PATH`:
    ```shell
    mffer/src
    mffer/tools
    mffer/tools/node_modules/.bin
    ```

### Included tools

The setup process installs several tools not included within the `mffer` project
itself. In addition to the VS Code extensions installed in the
[VS Code setup process](#in-visual-studio-code), some of the items added by
"restore" are:

-   xmldocmd
-   clasp
-   MessagePack
-   AssetsTools
-   @types/google-apps-script
-   stream-json

All of these tools can be removed (along with their installed dependencies and the
`mffer/bin`, `mffer/obj`, and `mffer/release` directories) by running:

```shell
$ dotnet clean
```

## Writing documentation

Documentation is important. Below are a few guidelines in writing the
documentation associated with `mffer`.

### Source tree

With few exceptions (notably, a [brief README](../README.md), the
[License](../LICENSE), and the [Contributor Covenant](../CODE_OF_CONDUCT.md)),
documentation should be in the [`docs` directory](./). [`docs/api`](api/) is home
to the auto-generated API reference and should generally not be edited.

### README

There are two different README files in the project:

-   An ["at-a-glance" version](../README.md) in [/](../)
-   A ["Comprehensive" version/index](README.md) in [docs](./)

For both versions,

-   [shields](https://shields.io)/badges etc are nice but not great for a
    readme, more "marketing"; would probably be good for a website though not
    for the README. I get that these are decent quick "status" markers for
    builds, etc., but probably not a good resource for a reference. Great
    comment from [How to write a kickass
    README](https://dev.to/scottydocs/how-to-write-a-kickass-readme-5af9):
    > Take a cue from those same old school manuals you reference as to what
    > they include. Who cares about badges and emojis. That's marketing. Put
    > marketing on your site. This is about getting shit done

#### At-a-glance README

-   What the project does ([How to write a kickass
    README](https://dev.to/scottydocs/how-to-write-a-kickass-readme-5af9))
-   Who the project is for
-   How to use the project
-   https://hpbl.github.io/WRITEME/#/
    -   WHAT: An introduction on what your project does.
    -   HOW: Instructions on how to use the project.
-   https://github.com/noffle/art-of-readme
    -   "Ideally, someone who's slightly familiar with your module should be
        able to refresh their memory without hitting 'page down'"
    -   Name, one-liner, usage, api, installation, license
-   (Like above) Think man page?
-   https://github.com/18F/open-source-guide/blob/18f-pages/pages/making-readmes-readable.md
    -   Description
        -   What is this repo or project?
    -   Licensing
    -   Contact

#### Comprehensive README

-   anything from [At-a-glance](#at-a-glance)
-   name the thing ([How to write a kickass
    README](https://dev.to/scottydocs/how-to-write-a-kickass-readme-5af9))
-   introduction or summary (2 or 3 lines, what it does and who it is for,
    without "Introduction", "Summary", or "Overview")
-   Prerequisites (knowledge, tools)
-   How to install
-   How to use (CLI options, etc, link to another file)
-   link to CONTRIBUTING
-   contributors, acknowledgments
-   contact info
-   license
-   https://hpbl.github.io/WRITEME/#/
    -   WHY:The motivation behind your project, it's advantages.
    -   WHENThe status of the project, it's versions and roadmap.
    -   WHO: The people responsible for the project, license information, code
        of conduct.
    -   REFERENCES: External documentation, support, and related projects.
    -   CONTRIBUTION: Instructions on how to contribute to the project
        (sometimes a stand-alone file).
    -   OTHER: Any type of content that does not fit any of the above
        categories.
-   https://github.com/18F/open-source-guide/blob/18f-pages/pages/making-readmes-readable.md
    -   Description
        -   How does it work?
        -   Who will use this repo or project?
        -   What is the goal of this project?
    -   Instructions for how to use/develop/test
    -   Contributing - "How You Can Help" - also [guide to welcoming non-coders
        to
        hackathons](https://18f.gsa.gov/2015/04/03/how-to-welcome-new-coders-to-a-civic-hackathon/)
        and [ Contributor’s
        Guide](https://github.com/18F/midas/blob/dev/CONTRIBUTING.md)

#### More about READMEs

-   https://github.com/RichardLitt/standard-readme/blob/master/spec.md
-   checklist:
    https://github.com/noffle/art-of-readme#bonus-the-readme-checklist

### CONTRIBUTING

-   https://github.com/18F/open-source-guide/blob/18f-pages/pages/making-readmes-readable.md
    -   Contributing - "How You Can Help" - also [guide to welcoming non-coders
        to
        hackathons](https://18f.gsa.gov/2015/04/03/how-to-welcome-new-coders-to-a-civic-hackathon/)
        and [ Contributor’s
        Guide](https://github.com/18F/midas/blob/dev/CONTRIBUTING.md)
-   https://github.com/rust-lang/rust/blob/master/CONTRIBUTING.md
    -   super simple
    -   title, about the developers guide (link to super-intensive
        documentation), getting help, bug reports
    -   should have a "developer's guide"
-   https://mozillascience.github.io/working-open-workshop/contributing/
    -   put in root directory
    -   should be applicable to:
        -   project owners/maintaners
        -   project contributors: what and how they can contribute and interact
        -   project consumers: how to build off of mine and make their own
            project
-   https://mozillascience.github.io/working-open-workshop/contributing/
    -   TOC
    -   Links:
        -   docs
        -   issues
        -   other
    -   testing
    -   development environment
    -   how to report a bug (bug report template?)
    -   how to submit changes
    -   style guide/coding conventions
    -   asking for help (I think it should be higher, or maybe repeated at
        beginning and end)
-   https://opensource.com/life/16/1/8-ways-contribute-open-source-without-writing-code
    -   "8 ways to contribute without writing code": list but web page is broken
        and/or ugly
-   https://github.com/18F/open-source-guide/blob/18f-pages/pages/making-readmes-readable.md#instructions-for-how-people-can-help
    -   If there are any additional setup steps specific for development.
    -   Whether there are explicit Instructions for running tests before
        contributions are accepted.
    -   If there are any requirements for contribution, if any, e.g. A
        Contributor License Agreement
    -   Whether there is a specific coding style to adhere to. (Generally
        contributors will match what they see within a project.)
    -   Whether potential contributors should ask before they make significant
        changes.
    -   Whether work-in-progress pull requests are ok.
    -   What Code of Conduct states
-   See also great example at
    https://github.com/atom/atom/blob/master/CONTRIBUTING.md

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
[EditorConfig file](../.editorconfig); the nonstandard extension EditorConfig settings are
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

### Tools

The easiest way to ensure coding style is consistent throughout the project is
to use tools that enforce this style wherever possible. None of the below is
required to begin contributing to the project, but may be exceedingly helpful to
those doing so with any frequency. As such, certain files and settings are
included in the project to ease the consistent use of these tools by all
contributors. These are also covered in
[Setting up a development environment](#setting-up-a-development-environment).

#### Visual Studio Code

Much of the initial work on the project has been done in [Visual Studio
Code](https://code.visualstudio.com). While this is in no way required for
contributing to the project, it is relatively easy to use VS Code to set up an
environment that automatically mimics much of the style used throughout the
project. If you clone or fork the current project repository, your new one will
include a [`.vscode` directory](.vscode/) that stores
[settings](.vscode/settings.json) and
[extension recommendations](.vscode/extensions.json) to use for this project in particular.
If you use a different editor, you should use one that allows you to set formats
that are applied automatically, and they should match those set in this project.

#### Formatters

Formatters for individual code types are often available as both standalone
tools and as extensions for Visual Studio Code. Where appropriate, specific
settings that are different than the defaults are kept in settings files
included in the repository.

| Formatter    | VS Code Extension                                                                                          | Configuration                                                                         |
| ------------ | ---------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------- |
| EditorConfig | [editorconfig.editorconfig](https://marketplace.visualstudio.com/items?itemName=EditorConfig.EditorConfig) | [.editorconfig](../.editorconfig)                                                     |
| OmniSharp    | [ms-dotnettools.csharp](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp)         | [.editorconfig](../.editorconfig)                                                     |
| Prettier     | [esbenp.prettier-vscode](https://marketplace.visualstudio.com/items?itemName=esbenp.prettier-vscode)       | None                                                                                  |
| shfmt        | [foxundermoon.shell-format](https://marketplace.visualstudio.com/items?itemName=foxundermoon.shell-format) | in [VS Code Settings](../.vscode/settings.json): `"shellformat.flag": "-bn -ci -i 0"` |

#### Linters

In addition, a linter is strongly recommended to quickly identify errors and
deviations from best practices. All code is expected to avoid both "errors" and
"informational" warnings from linters with rare exceptions for clear reasons.
Linters recommended for this project include:

| Linter     | VS Code Extension                                                                                  | Configuration |
| ---------- | -------------------------------------------------------------------------------------------------- | ------------- |
| OmniSharp  | [ms-dotnettools.csharp](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp) |
| shellcheck | [timonwong.shellcheck](https://marketplace.visualstudio.com/items?itemName=timonwong.shellcheck)   | None          |

## Making a custom `Program.cs`

`mffer` isn't (yet) built as a library, but you can still change the main entry
point in [Program.cs](../src/Program.cs) as you see fit, rather than using the
default one with multiple command line options that automatically reads and
reports all supported data.

## Making a custom `Component`

The next step in customizing `mffer` for your own needs is to create a custom
derivative of the [`Component` type](api//Mffer/ComponentType.md). This
can be done in a [Custom `Program.cs`](#making-a-custom-programcs) or in a
separate file. The derivative type should include a
[constructor](api/mffer/../Mffer/Component/Component.md) that establishes the
needed assets or other requirements and override a
[Load() method](api/Mffer/Component/Load.md) that parses imported assets into
the component members. More details are available in the
[`Component` API entry](api//Mffer/ComponentType.md).

Accessing the newly designed component will typically involve
[customizing `Program.cs`](#making-a-custom-programcs) or
[changing more internals](#changing-mffer-internals) to customize the
[`Version` type](api/Mffer/VersionType.md) so that your component is loaded
along with all the others.

## Changing `mffer` internals

Changing the underlying workings of `mffer` may be necessary to alter
calculations, change how data is extracted or reported for existing
`Component`s, or to improve performance or other conditions. Every attempt has
been made to keep code appropriately encapsulated and abstracted so that
modifying one area need not break another, but this must be assumed to be far
from perfect.

### `mffer` best practices

In an effort to "compartmentalize" the code and continue to make
it customizable, please attempt to use the following "best practices":

-   Anything that is "user-facing", such as filesystem pathnames or command line
    options, should be customizable by modifying only `Program.cs`.
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
  docs/
    api/
  src/
    webapp/
  tools/
    .config/
	.nuget/
    node_modules/
```

The root `mffer` directory contains project-specific settings like
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
`.nuget` for NuGet packages, and `.config/dotnet-tools.json` for dotnet local
tools.

#### Code structure

Corresponding to the above [best practices](#mffer-best-practices), the design
of `mffer` is based on the principles of abstraction, encapsulation, and
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

The `src` directory additionally includes the `autoextract` script and the
`webapp` directory, which are planned to be internalized into the main `mffer`
code at some point.

Due to the reverse-engineering nature of the software and what documentation is
available regarding Unity file formats, several assumptions are made in order to
have a starting point for programming. Generally these are tested when used in
the code itself, but many are tested in the AssetFileTest class. More about the
assumptions about how Marvel Future Fight works (from a programming perspective)
are explicitly listed in [The Structure of Marvel Future Fight](mff.md), along
with how they correspond to the design structures of `mffer`. Refer to that
document and the [API](api/) for further detils.

## The `mffer` APIs

All included types and members are included in the API documentation, generated
as part of the build process from the triple-slash XML comments describing them
in the code itself.

## Building `mffer`

1. `apkdl` and `autoanalyze` are shell scripts and require no building.
2. Building the dotnet app: from within the root `mffer` directory,
    ```shell
    $ dotnet build mffer.csproj
    ```
3. Build the API documentation: from within the `tools` directory,
    ```shell
    $ dotnet xmldocmd ../bin/Debug/netcoreapp3.1/mffer.dll ../docs/api --visibility private --source https://github.com/therealchjones/mffer --clean --permalink pretty --namespace-pages
    ```
4. The web app must be uploaded but there's nothing to build; see [Deploying & Releasing](#deploying--releasing)

### Building a release

A "release" is a package of the `mffer` program and the associated scripts.
Creating a release builds the program from source as [above](#building-mffer),
but intentionally leaves out extra debugging information and (in most cases)
results in a single file `mffer` program that will only work on a specific
platform. While "official" releases are
[available for download](https://github.com/therealchjones/mffer/releases/), you
can build a customized version or test changes with your own copy of the source
code.

To choose a "name" for your release, tag the HEAD of your git repository:

```shell
$ git tag v0.1.0-pre
```

The name must be a string that starts with `v`. Official releases use the
[Semantic Versioning](README.md#versioning) conventions, but you can use any
string starting with `v`. (If you don't want to tag the git repository, you can
alternatively set the environment variable `VersionString`.)

To build the release packages, use

```shell
$ dotnet publish -c release
```

The result will be files placed in the `release` directory of the source tree.
There are files are named `mffer-`_`version`_`-`_`platform`_`.zip` for each of the
built platforms (by default, `win-x64`, `osx-x64`, and `linux-x64`). These files
contain the `mffer` executable file and its associated scripts, `apkdl`
and `autoanalyze`, and may contain other supporting files. A
platform-independent file `mffer-`_`version`_`-net5.0.zip` includes several
other files needed to run the `mffer` program using the .NET 5.0 runtime (not
included).

## Testing `mffer`

### Testing environments

In order to ensure [software requirements](USAGE.md#requirements) are minimal
and known, formal testing of `mffer` is performed on basic virtual machines
created in a reproducible way. Where possible, output is then compared to "known
good" output from prior builds. There are standardized methods for creating the
virtual machines and for testing `mffer` on them. Scripts are provided to create
virtual machines for Parallels Desktop and test `mffer` on the virtual machines,
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

```shell
sh tools/testmac.sh
```

This script:

1. Creates a macOS virtual machine if needed
2. Installs Xcode Command Line Tools, Node.js, and .NET SDK
3. Builds `mffer`
4. Resets the virtual machine
5. Tests `apkdl` (which requires manual interaction)
6. Resets the virtual machine
7. Installs Temurin, .NET SDK, Ghidra, and Xcode Command Line Tools
8. Tests `autoanalyze`
9. Tests `mffer`

### Linux

1. Install Ubuntu 20.04 & apply all available updates
2. Install Parallels Tools
3. Test `mffer`
4. Test `apkdl`
5. Test `autoanalyze`

### Windows

1. Install Windows 10 & apply all available updates
2. Install Parallels Tools
3. Test `mffer`
4. Install
   [Temurin 11](https://adoptium.net/?variant=openjdk11&jvmVariant=hotspot)
5. Install Git (with Git Bash)
6. Test `apkdl`
7. Install [Ghidra](https://github.com/NationalSecurityAgency/ghidra/releases)
8. Install [.NET 5.0](https://dotnet.microsoft.com/download/dotnet/5.0)
9. Test `autoanalyze`

### Testing releases

"Semi-automated" testing of [releases](#building-a-release) is currently done
using virtual machines as noted above. Testing is simply ensuring the programs
run as expected; output files are not strictly compared due to expected minor
variations.

In order to ensure bugs are not the result of building on different systems, and
to ensure the minimum of additional software is sufficient, the aforementioned
scripts are each used to build the virtual machines and then build the candidate
release versions of the software on each system. Each of those builds is then
tested on each reference system, resulting in a testing checklist such as:

> #### `apkdl`
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
> #### `mffer`
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

## Releasing `mffer`

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

## The `mffer` webapp

### Description

The `mffer` webapp is based on Google Apps Script, uses Google Sheets/Google
Drive, and is deployed at https://mffer.org via the
[Google Cloud Platform](https://cloud.google.com). This method of deployment is
not especially straightforward, and other better options may be more readily
available to other users. These will, however, require significant code
modification, as the `mffer` webapp code makes heavy use of Apps Script
(transpiled from TypeScript) and its associated APIs, the Google Picker, and
Google's OAuth 2.0 authentication.

### Deploying the webapp

#### Requirements

-   [Google Account](https://google.com/account) with access to
    [Google Apps Script](https://script.google.com), Google Drive, and Google
    Cloud Platform (the free tiers are all acceptable).
-   POSIX-like development system (such as macOS, Linux, or Windows with Cygwin)
-   [Node.js](Node.js) & npm

#### Setting Up Google Cloud Platform

GCP is somewhat complex to configure, and configuration within an existing GCP
account is beyond the scope of this document (and may be beyond the abilities of
this author). However, you may be able to create a basic project usable for
`mffer` webapp deployment in a few (relatively) simple steps. More in-depth
resources for setting up Apps Script in a Google Cloud Platform account include:

-   https://developers.google.com/apps-script/guides/cloud-platform-projects#switching_to_a_different_standard_gcp_project
-   https://github.com/google/clasp/blob/master/docs/run.md#setup-instructions
-   https://developers.google.com/picker/docs#appreg
-   https://cloud.google.com/resource-manager/docs/creating-managing-projects

In an effort to consolidate the above into a simple(r) set of instructions,
follow the below set of instructions to set up a project for `mffer`.

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
2. In the `mffer` repository's `tools` directory, install `clasp` and its
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
10. Choose "Setup `mffer`", then enter the OAuth 2.0 Client ID and OAuth 2.0
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
13. When the app reloads, under "Upload new `mffer` data" select a CSV file
    created by the `mffer` command line application and then "Confirm" it for upload.
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

-   [The Structure of Marvel Future Fight](mff.md)
-   [Contributing to `mffer`](CONTRIBUTING.md)
-   [`mffer` APIs](api/)
