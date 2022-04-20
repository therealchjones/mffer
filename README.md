# `mffer`

Marvel Future Fight exploration & reporting

This project develops software that creates and updates the
[mffer webapp](https://mffer.org).

## Marvel Future Fight

[Marvel Future Fight](http://www.marvelfuturefight.com/) (MFF) is a mobile game
for Android and iOS created with Java, C#, Unity, and IL2CPP. It uses multiple
common services for authentication and proprietary servers for data storage and
communication. Techniques used in this project may be useful for exploring
similar software.

## About `mffer`

This project facilitates analysis of [Marvel Future Fight](#marvel-future-fight)
and provides access to the data it uses for game play. This is almost certainly
against [NetMarble](https://netmarble.com)'s
[Terms of Service](https://help.netmarble.com/terms/terms_of_service_en) as well
as those of its affiliates.

The project includes:

-   a [shell script](docs/apkdl.md) to obtain the Marvel Future Fight
    program files
-   a [shell script](docs/autoanalyze.md) to decompile and evaluate the program
    files
-   a [.NET console app](docs/mffer.md) to obtain and parse the data files,
    analyze the data, and output information in an open and usable format
-   a [web app](docs/webapp.md) to present and use the game data

## Usage

Download
[the latest release](https://github.com/therealchjones/mffer/releases/latest)
for your platform and unzip the files into a convenient directory.

1.  ```shell
    mffer --datadir data_directory --outputdir output_directory
    ```
2.  Upload the resulting CSV file into the webapp.

More detailed instructions and other workflows are in the
[User Guide](docs/USAGE.md). Information about modifying the program is in the
[Development Guide](docs/Development.md).

## Contributing

Contributions of all kinds are welcome. [CONTRIBUTING](docs/CONTRIBUTING.md) has
information for contributors with any level or variety of experience. Use
[the issues tracker](https://github.com/therealchjones/mffer/issues) for any
and all questions and comments, or email <chjones@aleph0.com>.
