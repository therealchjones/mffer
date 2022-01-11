using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;

namespace Mffer {
	/// <summary>
	/// The primary user-facing program class
	/// </summary>
	public static class Program {
		static Game Game { get; set; }
		const string Description = "Marvel Future Fight extraction & reporting";
		static readonly Dictionary<string[], string> Options = new Dictionary<string[], string> {
			{new[]{"--datadir","-d"}, "directory containing Marvel Future Fight data files"},
			{new[]{"--outputdir","-o"}, "directory in which to place created files"}
		};
		/// <summary>
		/// Primary entry point of the program
		/// </summary>
		static int Main( string[] args ) {
			// Due to a limitation of System.CommandLine in evaluating empty
			// strings, we first parse options as strings then re-parse properly
			// as DirectoryInfo objects.
			var stringCommand = new RootCommand( Description );
			var dirCommand = new RootCommand( Description );
			foreach ( var entry in Options ) {
				Option<string> stringOption = new Option<string>( entry.Key, entry.Value ) { IsRequired = true };
				stringOption.AddValidator(
					optionResult => {
						IEnumerable<string> emptyStrings =
							optionResult.Tokens
								.Select( t => t.Value )
								.Where( s => String.IsNullOrEmpty( s ) );
						if ( emptyStrings.Count() == 0 ) {
							return null;
						} else {
							return $"Option '{entry.Key[0]}' must not be empty.";
						}
					}
				);
				Option<DirectoryInfo> dirOption = new Option<DirectoryInfo>( entry.Key, entry.Value ) { IsRequired = true };
				if ( entry.Key[0] == "--datadir" ) dirOption.ExistingOnly();
				else if ( entry.Key[0] == "--outputdir" ) dirOption.LegalFilePathsOnly();
				stringCommand.AddOption( stringOption );
				dirCommand.AddOption( dirOption );
			}
			stringCommand.Handler = CommandHandler.Create<string, string>( ( datadir, outputdir ) => {
				dirCommand.Handler = CommandHandler.Create<DirectoryInfo, DirectoryInfo>( OptionsHandler );
				return dirCommand.Invoke( args );
			} );
			Game = new Game();
			GetProspectiveAlliances();
			return stringCommand.Invoke( args );
		}
		static void OptionsHandler( DirectoryInfo dataDir, DirectoryInfo outputDir ) {
			LoadAll( dataDir );
			WriteAll( outputDir );
		}
		/// <summary>
		/// Loads all available game data
		/// </summary>
		/// <remarks>
		/// <see cref="LoadAll(DirectoryInfo)"/> loads game information from a
		/// game data directory into the <see cref="Program.Game"/> static
		/// property. All information that is extractable from the data
		/// directory (including all version and all assets and components
		/// within those versions) will be loaded.
		/// </remarks>
		static void LoadAll( DirectoryInfo dataDir ) {
			Game.LoadAll( dataDir.FullName );
		}
		/// <summary>
		/// Outputs all loaded <see cref="Game"/> data
		/// </summary>
		/// <remarks>
		/// <see cref="WriteAll(DirectoryInfo)"/> saves all data that has been
		/// loaded into the <see cref="Program.Game"/> static property into the
		/// output directory. If no data has been loaded prior to calling <see
		/// cref="WriteAll(DirectoryInfo)"/>, <see
		/// cref="LoadAll(DirectoryInfo)"/> will be run first.
		/// </remarks>
		static void WriteAll( DirectoryInfo saveDir ) {
			Game.ToJsonFiles( saveDir );
			Game.WriteCSVs( saveDir );
		}
		static void GetProspectiveAlliances() {
			// List<Alliance> prospectiveAlliances = LoadFromJsonFile( "prospectiveAlliances.json" );
			List<Alliance> prospectiveAlliances = new List<Alliance>();
			prospectiveAlliances.AddRange( new List<Alliance> {
				"81",
				"모여라쉴드",
				"Cikarang SGC",
				"꽁쓰 Family",
				"unit",
				"겨울아이",
				"퓨처원더러",
				"台灣神盾局",
				"Pinoy 2600"
			} );
			prospectiveAlliances = NetworkData.FindProspectiveAlliances( prospectiveAlliances );
			// SaveToJsonFile( prospectiveAlliances, "prospectiveAlliances.json" );
		}
	}
}
