using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;

namespace Mffer {
	/// <summary>
	/// The primary user-facing program class
	/// </summary>
	public static class Program {
		static Game Game { get; set; }
		static readonly string Description = "Marvel Future Fight extraction & reporting";
		static readonly Dictionary<string[], string> Options = new Dictionary<string[], string> {
			{new[]{"--datadir","-d"}, "directory containing Marvel Future Fight data files"},
			{new[]{"--outputdir","-o"}, "directory in which to place created files"}
		};
		/// <summary>
		/// Primary entry point of the program
		/// </summary>
		static int Main( string[] args ) {
			RootCommand command = SetupCommandLine();
			Game = new Game();
			return command.Invoke( args );
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
			Game = new Game();
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
		static RootCommand SetupCommandLine() {
			// Due to a limitation of System.CommandLine in evaluating empty
			// strings, we first parse options as strings then re-parse properly
			// as DirectoryInfo objects.
			var command = new RootCommand( Description );
			ParseArgument<DirectoryInfo> DirectoryInfoParser = result => {
				if ( String.IsNullOrEmpty( result.ToString() ) ) throw new ArgumentException( "Directory name must not be an empty string" );
				return new DirectoryInfo( result.ToString() );
			};
			foreach ( var entry in Options ) {
				Option option = new Option<DirectoryInfo>( entry.Key, DirectoryInfoParser, false, entry.Value ) { IsRequired = true, };
				option.AddValidator(
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
				// if ( entry.Key[0] == "--datadir" ) option.ExistingOnly();
				if ( entry.Key[0] == "--outputdir" ) option.LegalFilePathsOnly();
				command.AddOption( option );
			}
			command.SetHandler(
				( DirectoryInfo dataDir, DirectoryInfo outputDir ) => OptionsHandler( dataDir, outputDir ), command.Options
				);
			return command;
		}
	}
}
