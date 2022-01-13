using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Text.Json;

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
			FileInfo alliancesFile = new FileInfo( "alliances.json" );
			List<Alliance> importedAlliances = new List<Alliance>();
			JsonSerializerOptions jsonOptions = new JsonSerializerOptions();
			jsonOptions.Converters.Add( new GameObjectJsonConverter() );
			if ( alliancesFile.Exists ) {
				String fileContents = File.ReadAllText( alliancesFile.FullName );
				JsonDocument jsonDocument = JsonDocument.Parse( fileContents );
				JsonElement jsonList = jsonDocument.RootElement.GetProperty( "Value" );
				String jsonListString = jsonList.ToString();
				importedAlliances = (List<Alliance>)JsonSerializer.Deserialize( jsonListString, typeof( List<Alliance> ), jsonOptions );
			}
			int startSize = importedAlliances.Count;
			int endSize = startSize;
			if ( startSize > 0 ) {
				Console.WriteLine( $"Checking {startSize} monitored alliances" );
				importedAlliances = NetworkData.CheckProspectiveAlliances( importedAlliances );
				endSize = importedAlliances.Count;
				if ( endSize < startSize ) Console.WriteLine( $"{startSize - endSize} alliances had activity and were discarded." );
				if ( endSize > 0 ) Console.WriteLine( $"{endSize} alliances will continue to be monitored." );
			}
			List<Alliance> newAlliances = new List<Alliance>();
			if ( endSize < 100 ) {
				int searchSize = ( 100 - endSize ) * 1000;
				Console.Write( "Finding new alliances to monitor" );
				if ( searchSize > 1000 ) Console.WriteLine( "... this will take a little time." );
				else Console.WriteLine();
				newAlliances = NetworkData.FindProspectiveAlliances( searchSize );
				if ( newAlliances.Count > 0 ) {
					Console.WriteLine( $"Found {newAlliances.Count} new alliances to monitor." );
				} else {
					Console.WriteLine( "No new alliances identified." );
				}
			}
			newAlliances.AddRange( importedAlliances );
			if ( newAlliances.Count > 0 ) {
				List<long> allianceIds = new List<long>();
				List<Alliance> prospectiveAlliances = new List<Alliance>();
				foreach ( Alliance alliance in newAlliances ) {
					if ( !allianceIds.Contains( alliance.Id ) ) {
						allianceIds.Add( alliance.Id );
						prospectiveAlliances.Add( alliance );
					}
				}
				if ( prospectiveAlliances.Count < newAlliances.Count )
					Console.WriteLine( $"Removing {newAlliances.Count - prospectiveAlliances.Count} duplicates" );
				prospectiveAlliances.Sort( delegate ( Alliance x, Alliance y ) {
					if ( x.LastLoginTime == null || x.LastLoginTime == default ) {
						if ( y.LastLoginTime == null || y.LastLoginTime == default ) return 0;
						else return 1;
					}
					if ( y.LastLoginTime == null | y.LastLoginTime == default ) return -1;
					if ( x.LastLoginTime < y.LastLoginTime ) return 1;
					if ( x.LastLoginTime == y.LastLoginTime ) return 0;
					return -1;
				} );
				prospectiveAlliances.Reverse();
				Console.WriteLine( "Currently monitored alliances:" );
				int currentDays = -1;
				foreach ( Alliance alliance in prospectiveAlliances ) {
					double daysInactive = alliance.GetDaysInactive();
					if ( currentDays == -1 || daysInactive < currentDays ) {
						currentDays = (int)daysInactive;
						Console.WriteLine( $"Inactive more than {currentDays} days:" );
					}
					Console.WriteLine( $"{alliance.Name}: level {alliance.Level}, shop level {alliance.ShopLevel}, {alliance.Players.Count} members, inactive {daysInactive} days." );
				}
				GameObject alliances = prospectiveAlliances.ToGameObject();
				JsonWriterOptions writeOptions = new JsonWriterOptions() { Indented = true, SkipValidation = true };
				using ( Stream file = new FileStream( alliancesFile.FullName, FileMode.Create ) )
					alliances.ToJson( file, jsonOptions, writeOptions );
			} else {
				Console.WriteLine( "No alliances to monitor." );
				alliancesFile.Delete();
			}
		}
	}
}
