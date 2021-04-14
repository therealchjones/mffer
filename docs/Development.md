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
- [Setting up an environment](#setting-up-an-environment)
	- [Requirements](#requirements)
	- [Recommendations](#recommendations)
- [Writing documentation](#writing-documentation)
	- [README](#readme)
		- [At-a-glance](#at-a-glance)
		- [Comprehensive](#comprehensive)
		- [Associated info](#associated-info)
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
	- [Models & designs](#models--designs)
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

## Setting up an environment

### Requirements

-   .NET Core 3.1 SDK
-   POSIX-compatible development environment

### Recommendations

-   Visual Studio Code with several extensions
-   Node.js with several modules via npm
-   Google account with access to Google Apps Script
-   git
-   tar
-   mktemp
-   at least a few gigabytes of RAM

## Writing documentation

### README

-   Two versions:
    -   "At-a-glance" in /
    -   "Comprehensive"/index in docs
-   [shields](https://shields.io)/badges etc are nice but not great for a
    readme, more "marketing"; would probably be good for a website though not
    for the readme. I get that these are decent quick "status" markers for
    builds, etc., but probably not a good resource for a reference. Great
    comment from [How to write a kickass
    README](https://dev.to/scottydocs/how-to-write-a-kickass-readme-5af9):
    > Take a cue from those same old school manuals you reference as to what
    > they include. Who cares about badges and emojis. That's marketing. Put
    > marketing on your site. This is about getting shit done
-   Both versions

#### At-a-glance

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

#### Comprehensive

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

#### Associated info

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
associated with specific circumstances can be gathered from the [EditorConfig
file](.editorconfig); the nonstandard extensions EditorConfig settings are
documented in Microsoft's [.NET & C# Formatting Rules
Reference](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/formatting-rules).

#### Code Style

Different code structures and algorithms are widely acceptable, as long as the
function of the code is clear and doesn't introduce additional risk for error.
In general (whitespace and choice of programming languages notwithstanding),
excellent guides for coding practices to use and to avoid can be found in:

-   [GitLab Style
    Guides](https://docs.gitlab.com/ee/development/contributing/style_guides.html)
-   [Google Style Guides](https://google.github.io/styleguide/)
-   [Microsoft C# Coding
    Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/inside-a-program/coding-conventions)

Where necessary, coding style for individual pull requests can be discussed
along with the content of the code submitted. If needed, more specific
guidelines may be added to this document in the future.

#### Comments

### Tools

The easiest way to ensure coding style is consistent throughout the project is
to use tools that enforce this style wherever possible. None of the below is
required to begin contributing to the project, but may be exceedingly helpful to
those doing so with any frequency. As such, certain files and settings are
included in the project to ease the consistent use of these tools by all
contributors.

#### Visual Studio Code

Much of the initial work on the project has been done in [Visual Studio
Code](https://code.visualstudio.com). While this is in no way required for
contributing to the project, it is relatively easy to use VS Code to set up an
environment that automatically mimics much of the style used throughout the
project. If you clone or fork the current project repository, your new one will
include a [`.vscode` directory](.vscode/) that stores
[settings](.vscode/settings.json) and [extension
recommendations](.vscode/extensions.json) to use for this project in particular.
If you use a different editor, you should use one that allows you to set formats
that are applied automatically, and they should match those set in this project.

#### Formatters

Formatters for individual code types are often available as both standalone
tools and as extensions for Visual Studio Code. Where appropriate, specific
settings that are different than the defaults are kept in settings files
included in the repository.

| Formatter    | VS Code Extension                                                                                          | Configuration                                                                      |
| ------------ | ---------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------- |
| EditorConfig | [editorconfig.editorconfig](https://marketplace.visualstudio.com/items?itemName=EditorConfig.EditorConfig) | [.editorconfig](.editorconfig)                                                     |
| OmniSharp    | [ms-dotnettools.csharp](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp)         | [.editorconfig](.editorconfig)                                                     |
| Prettier     | [esbenp.prettier-vscode](https://marketplace.visualstudio.com/items?itemName=esbenp.prettier-vscode)       | None                                                                               |
| shfmt        | [foxundermoon.shell-format](https://marketplace.visualstudio.com/items?itemName=foxundermoon.shell-format) | in [VS Code Settings](.vscode/settings.json): `"shellformat.flag": "-bn -ci -i 0"` |

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

## Making a custom `Component`

## Changing `mffer` internals

Coding best practices:

-   user-modifiable variables should be in the Program class
-   validation of Program class settings should be done by called methods rather
    than by the user
-   the Program class should only interact with the Game class
-   interaction with the Game classes should be done via the Game class/object
-   interaction with the Data classes should be done via the DataDirectory
    class/object
-   this can be done by placing all classes within their associated "top level"
    class

### Models & designs

## The `mffer` APIs

## Building `mffer`

## Deploying & releasing

## See also

-   [The Structure of Marvel Future Fight](mff.md)
