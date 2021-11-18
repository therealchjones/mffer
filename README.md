# `mffer`

Marvel Future Fight extraction & reporting

This project develops software that creates and updates the
[mffer webapp](https://mffer.org). It is not necessary to review any of this to
[use the webapp](https://mffer.org).

This is a _brief_ overview of the
[`mffer`](https://github.com/therealchjones/mffer) project. A more
[comprehensive README document](docs/README.md) is available in the
[docs directory](docs/).

## Marvel Future Fight

[Marvel Future Fight](http://www.marvelfuturefight.com/) (MFF) is a mobile
role-playing game created with many industry-standard tools including Java, C#,
Unity, and IL2CPP. It's packaged as split APKs for Android for the Play Store,
and it uses Facebook and proprietary servers for user and game data storage and
communication. Even if you don't play MFF, the techniques used in this project
for exploring the game may contain some useful information.

## About `mffer`

This project facilitates analysis of [Marvel Future Fight](#marvel-future-fight)
and provides access to the data it uses for game play. This is almost certainly
against [NetMarble](https://netmarble.com)'s
[Terms of Service](https://help.netmarble.com/terms/terms_of_service_en) as
well as those of multiple affiliates.

The project includes:

-   a [shell script](docs/autoextract.md) to obtain the Marvel Future Fight
    program and data files
-   a [shell script](docs/autoanalyze.md) to decompile and evaluate the program
    files
-   a [.NET console app](docs/mffer.md) to parse the data files, analyze the
    data, and output information in an open and usable format
-   a [web app](docs/webapp.md) to present and use the game data

## Usage

Download
[the latest release](https://github.com/therealchjones/mffer/releases/latest)
for your platform and unzip the files into a convenient directory.

1.  ```shell
    autoextract -o data_directory
    ```
2.  ```shell
    mffer --datadir data_directory --outputdir output_directory
    ```
3.  Import the resulting CSV file into the webapp.

Detailed usage instructions for the individual components are documented in
[the above component documents](#about-mffer). More detailed instructions and
workflows are in [the comprehensive README](docs/README.md).

## Contributing

Contributions of all kinds are welcome. [CONTRIBUTING](docs/CONTRIBUTING.md) has
information for contributors with any level or variety of experience. Use
[the issues tracker](https://github.com/therealchjones/mffer/issues) for any
and all questions and comments, or email <chjones@aleph0.com>.
