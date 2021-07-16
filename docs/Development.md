# `mffer` Development

## Highlights

-   [Building](#building-mffer)
-   [`Program.cs`](#making-a-custom-programcs)
-   [`Component`s](#making-a-custom-component)
-   [APIs](#the-mffer-apis)

## Full contents

- [Highlights](#highlights)
- [Full contents](#full-contents)
- [Introduction](#introduction)
- [Setting up a development environment](#setting-up-a-development-environment)
	- [Requirements](#requirements)
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
- [Deploying & releasing](#deploying--releasing)
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

### Requirements

-   a vaguely POSIX-compatible development environment (and some near-ubiquitous
    POSIX-like tools that aren't strictly in the POSIX standard, like `tar`, and
    `mktemp`)
-   [.NET 5.0 SDK](https://dotnet.microsoft.com/download/dotnet/5.0)
-   [Node.js with npm](https://nodejs.dev) (with the `npm` command in your path)
-   [Google account](https://myaccount.google.com/) with access to [Google Apps Script](https://script.google.com/)
-   [git](https://git-scm.com)
-   a vaguely modern computer with an undetermined minimum quantity of RAM that
    is probably several gigabytes

### Recommendations

-   [Visual Studio Code](https://code.visualstudio.com)

### Setup

#### In Visual Studio Code

If you choose to use Visual Studio Code, open the Source Control (SCM) panel and
choose "Clone Repository" and enter https://github.com/therealchjones/mffer.git.
Choose a folder to place the new `mffer` directory within it. Open it when
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

All included types and membes are included in the API documentation, generated
as part of the build process from the triple-slash XML comments describing them
in the code itself.

## Building `mffer`

1. `autoextract` is a shell script and requires no building.
2. Building the dotnet app: from within the root `mffer` directory,
    ```shell
    $ dotnet build mffer.csproj
    ```
3. Build the API documentation: from within the `tools` directory,
    ```shell
    $ dotnet xmldocmd ../bin/Debug/netcoreapp3.1/mffer.dll ../docs/api --visibility private --source https://github.com/therealchjones/mffer --clean --permalink pretty --namespace-pages
    ```
4. `webapp` must be uploaded but there's nothing to build

## Deploying & releasing

Honestly, I've never done it. I've got some ideas, but we'll see how it goes
before I document them.

## See also

-   [The Structure of Marvel Future Fight](mff.md)
-   [Contributing to `mffer`](CONTRIBUTING.md)
-   [`mffer` APIs](api/)
