using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;

namespace Mffer {
	/// <summary>
	/// The primary user-facing program class
	/// </summary>
	/// <remarks>
	/// Methods for working with specific areas are in separate <see cref="Program"/> subclasses.
	/// </remarks>
	/// <seealso cref="Program.Alliances"/>
	public static partial class Program {
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
			RootCommand command = SetupCommandLine();
			Game = new Game();
			// NetworkData.DownloadFiles();
			// Alliances.GetProspectiveAlliances();
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
			RootCommand command = new RootCommand( Description );
			Option<DirectoryInfo> dataDirOption = new Option<DirectoryInfo>(
				"--datadir", "source directory"
			) {
				IsRequired = true,
				Arity = ArgumentArity.ExactlyOne
			};
			dataDirOption.ExistingOnly();
			Option<DirectoryInfo> outputDirOption = new Option<DirectoryInfo>(
				name: "--outputdir",
				description: "output directory"
			) {
				IsRequired = true,
				Arity = ArgumentArity.ExactlyOne,
			};
			outputDirOption.AddValidator( ( OptionResult result ) => {
				// Thanks to the above Arity setting of ExactlyOne, we know that if we've
				// made it to this point this token exists:
				if ( String.IsNullOrEmpty( result.Children[0].Tokens[0].Value ) )
					result.ErrorMessage = "Output directory name must not be empty.";
			} );
			outputDirOption.LegalFilePathsOnly();
			command.AddOption( dataDirOption );
			command.AddOption( outputDirOption );
			command.SetHandler(
				( DirectoryInfo dataDir, DirectoryInfo outputDir ) =>
					OptionsHandler( dataDir, outputDir ),
					dataDirOption, outputDirOption
			);
			return command;
		}
	}
}
