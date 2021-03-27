# Contributing to mffer

Guidelines, notes, and tips for maintaining and adding to the mffer project

Any questions, comments, or concerns can be posted in the [GitHub repository's issues list](https://github.com/therealchjones/mffer/issues) or emailed to the owner, <chjones@aleph0.com>.

## Q&A

Where should I report bugs, request new or changed features, or ask for more help?
: The [GitHub issues page](https://github.com/therealchjones/mffer/issues/) is just fine for all of these things.

I don't know how to program, so how can I help?
: Answer questions on the [GitHub issues page](https://github.com/therealchjones/mffer/issues/), make edits to [documentation](README.md), or just offer a kind word.

What's a "pull request"?
: Ah, you're new to git and its style of shared programming. Welcome! I'm glad you're here. There's _lots_ of information available to get you started---sometimes so much

## Table of Contents

-   [Q&A](#qa)
-   [Introduction](#introduction)
-   [Behavioral Expectations](#behavioral-expectations)
-   [GitHub](#github)
-   [Application Purpose and Design](#application-purpose-and-design)
-   [Documentation](#documentation)
-   [Coding Style](#coding-style)
    -   [Whitespace Style](#whitespace-style)
    -   [Code Style](#code-style)
    -   [Comments](#comments)
    -   [Tools](#tools)
        -   [Visual Studio Code](#visual-studio-code)
        -   [Formatters](#formatters)
        -   [Linters](#linters)
-   [Development](#development)
    -   [mffer app](#mffer-app)
-   [Contact](#contact)

## Introduction

Thank you for your interest in this project. Contributions of any kind are welcome, but most useful to the project are bug reports and possible fixes. Whether you find a problem that you can fix or not, however, you can send feedback; follow, fork, or star the project on [GitHub](https://github.com); tell others about the project; or just use the project yourself.

This document includes information ranging from [appropriate behavoirs when contributing to the project](#behavioral-expectations) to [specific development tools you might use](#tools) and [uniform code styles](#code-style). It does not include design motivations for the individual components, API documentation, or other notes that apply to only a subset of the project. (For those, try the [see also section](#see-also).) The [detailed table of contents](#table-of-contents) may be useful for locating pertinent data within this document; the [comprehensive README][(readme.md)] includes links to other documents in the project. External resources are linked throughout pertinent sections.

## Behavioral Expectations

[![Contributor Covenant](https://img.shields.io/badge/Contributor%20Covenant-2.0-4baaaa.svg)](/CODE_OF_CONDUCT.md)

The abilities needed to effectively contribute to an open source project are complex, intertwined, and widely varying. None, however, is more important than the ability to work with others kindly. People are different. Respect and celebrate those differences. Failure to do so will result in being asked to cease interactions with the project and reporting your account to [GitHub](https://github.com). Disrespectful comments will be edited or removed. Any concerns may be addressed to the project maintainer, <chjones@aleph0.com>.

While this is the guiding spirit of the contributions to the project, the project has formally adopted the [Contributor Covenant 2.0](../CODE_OF_CONDUCT.md) for this project. In addition, users and visitors are expected to follow the [GitHub Community Guidelines](https://docs.github.com/en/github/site-policy/github-community-guidelines) and the [GitHub Terms of Service](https://docs.github.com/en/github/site-policy/github-terms-of-service). None of these documents is intended to allow or encourage behavior disallowed or discouraged by the others.

Let's do better.

## GitHub

[mffer](https://github.com/therealchjones/mffer) is hosted on [GitHub](https://github.com). Questions, requests, or comments can be left on the [issues page](https://github.com/therealchjones/mffer/issues/); the [list of open issues](https://github.com/therealchjones/mffer/issues?q=is%3Aissue+is%3Aopen+) is a great place to find questions you can answer, requests and comments to which you can respond, bugs you can fix, and enhancements you can implement.

The ideal method for contibuting new content or making changes to existing content for this project is by [making a pull request](https://github.com/therealchjones/mffer/pulls). This isn't always the easiest way, and it has a bit of a learning curve if you haven't done it before, but it allows appropriate history tracking and attribution while ensuring the community is able to review code and make other changes before adding something to the project. If you're not sure how to do it, just post a question on the [issues page](https://github.com/therealchjones/mffer/issues/).

## Application Purpose and Design

## Documentation

## Coding Style

If this is the longest section of this document, that is in inverse relationship to its importance. Reading and understanding the above sections is a significantly better use of your time than memorizing the minutiae herein. However, consistent coding styles, even if mostly arbitrary (as the ones here certainly are) allow easier reading and review of code, more rapid improvement, and better project integration. As the project uses multiple file types and programming languages, styles that they can all share are recommended. Some of the specifics are listed below.

### Whitespace Style

The overarching message is that whitespace is good. It adds to readability in many ways. The initial indenting whitespace (where needed) in each file is a tab character, not multiple spaces. Additionally, spaces should be used liberally to separate surrounding parentheses, braces, brackets, and operators from nearby content. The major exception to this rule is that an opening bracket of any kind should almost always be on the same line as the function or method call, class or struct definition, or other label associated with it. A space need not be between the function or method call label and its associated parentheses, but can be if it increases readability. More detailed descriptions of spacing associated with specific circumstances can be gathered from the [EditorConfig file](.editorconfig); the nonstandard extensions EditorConfig settings are documented in Microsoft's [.NET & C# Formatting Rules Reference](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/formatting-rules).

### Code Style

Apart from whitespace, different code structures and algorithms are widely acceptable, as long as the function of the code is clear and doesn't introduce additional risk for error. In general (whitespace and choice of programming languages notwithstanding), excellent guides for coding practices to use and to avoid can be found in:

-   [GitLab Style Guides](https://docs.gitlab.com/ee/development/contributing/style_guides.html)
-   [Google Style Guides](https://google.github.io/styleguide/)
-   [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/inside-a-program/coding-conventions)

Where necessary, coding style for individual pull requests can be discussed along with the content of the code submitted. If needed, more specific guidelines may be added to this document in the future.

### Comments

### Tools

The easiest way to ensure coding style is consistent throughout the project is to use tools that enforce this style wherever possible. None of the below is required to begin contributing to the project, but may be exceedingly helpful to those doing so with any frequency. As such, certain files and settings are included in the project to ease the consistent use of these tools by all contributors.

#### Visual Studio Code

Much of the initial work on the project has been done in [Visual Studio Code](https://code.visualstudio.com). While this is in no way required for contributing to the project, it is relatively easy to use VS Code to set up an environment that automatically mimics much of the style used throughout the project. If you clone or fork the current project repository, your new one will include a [`.vscode` directory](.vscode/) that stores [settings](.vscode/settings.json) and [extension recommendations](.vscode/extensions.json) to use for this project in particular. If you use a different editor, you should use one that allows you to set formats that are applied automatically, and they should match those set in this project.

#### Formatters

Formatters for individual code types are often available as both standalone tools and as extensions for Visual Studio Code. Where appropriate, specific settings that are different than the defaults are kept in settings files included in the repository.

| Formatter    | VS Code Extension                                                                                          | Configuration                                                                      |
| ------------ | ---------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------- |
| EditorConfig | [editorconfig.editorconfig](https://marketplace.visualstudio.com/items?itemName=EditorConfig.EditorConfig) | [.editorconfig](.editorconfig)                                                     |
| OmniSharp    | [ms-dotnettools.csharp](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp)         | [.editorconfig](.editorconfig)                                                     |
| Prettier     | [esbenp.prettier-vscode](https://marketplace.visualstudio.com/items?itemName=esbenp.prettier-vscode)       | None                                                                               |
| shfmt        | [foxundermoon.shell-format](https://marketplace.visualstudio.com/items?itemName=foxundermoon.shell-format) | in [VS Code Settings](.vscode/settings.json): `"shellformat.flag": "-bn -ci -i 0"` |

#### Linters

In addition, a linter is strongly recommended to quickly identify errors and deviations from best practices. All code is expected to avoid both "errors" and "informational" warnings from linters with rare exceptions for clear reasons. Linters recommended for this project include:

| Linter     | VS Code Extension                                                                                  | Configuration |
| ---------- | -------------------------------------------------------------------------------------------------- | ------------- |
| OmniSharp  | [ms-dotnettools.csharp](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp) |
| shellcheck | [timonwong.shellcheck](https://marketplace.visualstudio.com/items?itemName=timonwong.shellcheck)   | None          |

## Development

### mffer app

Coding best practices:

-   user-modifiable variables should be in the Program class
-   validation of Program class settings should be done by called methods rather than by the user
-   the Program class should only interact with the Game class
-   interaction with the Game classes should be done via the Game class/object
-   interaction with the Data classes should be done via the DataDirectory class/object
-   this can be done by placing all classes within their associated "top level" class

## Contact

Any questions, comments, or concerns can be posted in the [GitHub repository's issues list](https://github.com/therealchjones/mffer/issues) or emailed to the owner, <chjones@aleph0.com>.