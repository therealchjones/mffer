# mffer: Marvel Future Fight Extraction & Reporting

This is a _brief_ overview of the [mffer](https://github.com/therealchjones/mffer) project. A more comprehensive [README document](docs/README.md) is available with [the other documentation](docs/) in the [docs directory](docs/).

## Marvel Future Fight

[Marvel Future Fight](http://www.marvelfuturefight.com/) (MFF) is a mobile role-playing game by [NetMarble](https://company.netmarble.com/) set in the extended [Marvel](https://www.marvel.com/) multiverse. It is (or appears to be) made with many industry-standard tools, including programming in Java and C# for Unity (using il2cpp), packaged (or at least delivered) as split APKs for Android from the Google Play Store, and using Facebook and NetMarble servers for user and game data storage. As such, even if you don't play MFF, the descriptions of techniques used in this project for exploring those many components may contain some useful knowledge.

## About mffer

This project is intended to facilitate analysis of [Marvel Future Fight](#marvel-future-fight) and provide access to the data it uses for game play. This is almost certainly against the [NetMarble Terms of Service](https://help.netmarble.com/terms/terms_of_service_en?locale=&lcLocale=en) as well as those of multiple affiliates.

The project currently includes multiple components:

-   a [shell script](docs/autoextract.md) to obtain the Marvel Future Fight data files
-   a [.NET console app](docs/mffer.md) to parse the data files into an open and usable format
-   a [Google Sheet and web app](docs/webapp.md) to present and use the game data

## Usage

The project is currently likely to be of utility only to developers (however you may define that). Detailed usage instructions and explanations for the individual components are documented in [the above component documents](#about-mffer). Briefly:

```
$ autoextract [-v] -o data_directory
$ dotnet run -- --datadir data_directory --outputdir output_directory
```

Then import the resulting CSV file(s) to a Google Sheet for further work.

## Contributing

I welcome outside contributions, comments, questions, concerns, pull requests, and so forth. At least, I would if this were a public project in a public repository, but because I prefer not to be booted from my favorite game, you'll likely never hear about it. However, in the hypothetical case you'd like to contribute to a project you've never heard of, you can hypothetically learn about the best way to do so by hypothetically reading [CONTRIBUTING](docs/CONTRIBUTING.md), to which you also don't have access. You can also hypothetically email me at <chjones@aleph0.com>.
