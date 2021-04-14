# `mffer` Development

## Introduction

## Making a custom `Program.cs`

## Making a custom `Component`

## Changing `mffer` internals

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

## Application Purpose and Design

## Programming Details

# Building mffer

## Requirements

-   .NET Core 3.1 SDK
-   POSIX-compatible development environment

## Recommendations

-   Visual Studio Code with several extensions
-   Node.js with several modules via npm
-   Google account with access to Google Apps Script
-   git
-   tar
-   mktemp
-   at least a few gigabytes of RAM
