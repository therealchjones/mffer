# Meta-documentation

## README

-   Two versions:
    -   "At-a-glance" in /
    -   "Comprehensive"/index in docs
-   [shields](https://shields.io)/badges etc are nice but not great for a readme, more "marketing"; would probably be good for a website though not for the readme. I get that these are decent quick "status" markers for builds, etc., but probably not a good resource for a reference. Great comment from [How to write a kickass README](https://dev.to/scottydocs/how-to-write-a-kickass-readme-5af9):
    > Take a cue from those same old school manuals you reference as to what they include. Who cares about badges and emojis. That's marketing. Put marketing on your site. This is about getting shit done
-   Both versions

### At-a-glance

-   What the project does ([How to write a kickass README](https://dev.to/scottydocs/how-to-write-a-kickass-readme-5af9))
-   Who the project is for
-   How to use the project
-   https://hpbl.github.io/WRITEME/#/
    -   WHAT: An introduction on what your project does.
    -   HOW: Instructions on how to use the project.
-   https://github.com/noffle/art-of-readme
    -   "Ideally, someone who's slightly familiar with your module should be able to refresh their memory without hitting 'page down'"
    -   Name, one-liner, usage, api, installation, license
-   (Like above) Think man page?
-   https://github.com/18F/open-source-guide/blob/18f-pages/pages/making-readmes-readable.md
    -   Description
        -   What is this repo or project?
    -   Licensing
    -   Contact

### Comprehensive

-   anything from [At-a-glance](#at-a-glance)
-   name the thing ([How to write a kickass README](https://dev.to/scottydocs/how-to-write-a-kickass-readme-5af9))
-   introduction or summary (2 or 3 lines, what it does and who it is for, without "Introduction", "Summary", or "Overview")
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
    -   WHO: The people responsible for the project, license information, code of conduct.
    -   REFERENCES: External documentation, support, and related projects.
    -   CONTRIBUTION: Instructions on how to contribute to the project (sometimes a stand-alone file).
    -   OTHER: Any type of content that does not fit any of the above categories.
-   https://github.com/18F/open-source-guide/blob/18f-pages/pages/making-readmes-readable.md
    -   Description
        -   How does it work?
        -   Who will use this repo or project?
        -   What is the goal of this project?
    -   Instructions for how to use/develop/test
    -   Contributing - "How You Can Help" - also [guide to welcoming non-coders to hackathons](https://18f.gsa.gov/2015/04/03/how-to-welcome-new-coders-to-a-civic-hackathon/) and [ Contributor’s Guide](https://github.com/18F/midas/blob/dev/CONTRIBUTING.md)

### Associated info

-   https://github.com/RichardLitt/standard-readme/blob/master/spec.md
-   checklist: https://github.com/noffle/art-of-readme#bonus-the-readme-checklist

## CONTRIBUTING

-   https://github.com/18F/open-source-guide/blob/18f-pages/pages/making-readmes-readable.md
    -   Contributing - "How You Can Help" - also [guide to welcoming non-coders to hackathons](https://18f.gsa.gov/2015/04/03/how-to-welcome-new-coders-to-a-civic-hackathon/) and [ Contributor’s Guide](https://github.com/18F/midas/blob/dev/CONTRIBUTING.md)
-   https://github.com/rust-lang/rust/blob/master/CONTRIBUTING.md
    -   super simple
    -   title, about the developers guide (link to super-intensive documentation), getting help, bug reports
    -   should have a "developer's guide"
-   https://mozillascience.github.io/working-open-workshop/contributing/
    -   put in root directory
    -   should be applicable to:
        -   project owners/maintaners
        -   project contributors: what and how they can contribute and interact
        -   project consumers: how to build off of mine and make their own project
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
    -   asking for help (I think it should be higher, or maybe repeated at beginning and end)
-   https://opensource.com/life/16/1/8-ways-contribute-open-source-without-writing-code
    -   "8 ways to contribute without writing code": list but web page is broken and/or ugly
-   https://github.com/18F/open-source-guide/blob/18f-pages/pages/making-readmes-readable.md#instructions-for-how-people-can-help
    -   If there are any additional setup steps specific for development.
    -   Whether there are explicit Instructions for running tests before contributions are accepted.
    -   If there are any requirements for contribution, if any, e.g. A Contributor License Agreement
    -   Whether there is a specific coding style to adhere to. (Generally contributors will match what they see within a project.)
    -   Whether potential contributors should ask before they make significant changes.
    -   Whether work-in-progress pull requests are ok.
    -   What Code of Conduct states
-   See also great example at https://github.com/atom/atom/blob/master/CONTRIBUTING.md
