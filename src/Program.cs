using System.Collections.Generic;
using System.IO;
using CommandLine;

namespace Mffer {
	/// <summary>
	/// The primary user-facing program class
	/// </summary>
	public static class Program {
		/// <summary>
		/// The name of the game
		/// </summary>
		const string GameName = "Marvel Future Fight";
		static Game Game { get; set; }
		/// <summary>
		/// Gets or sets a dictionary of the command line arguments,
		/// indexed by option name
		/// </summary>
		static Dictionary<string, string> Arguments { get; set; }
		/// <summary>
		/// Primary entry point of the program
		/// </summary>
		static void Main() {
			LoadAll();
			WriteAll();
		}
		/// <summary>
		/// Loads command line arguments/options
		/// </summary>
		/// <remarks>
		/// <see cref="GetArguments()"/> uses the <see cref="CommandLine"/> class
		/// to build a dictionary of command-line arguments and their associated
		/// options, flags, or parameters. This is saved in the static
		/// <see cref="Arguments"/> property.
		/// </remarks>
		static void GetArguments() {
			Arguments = new Dictionary<string, string>();
			Parser cmdLine = new Parser();
			Arguments.Add( "datadir", cmdLine.GetOption( "datadir" ) );
			Arguments.Add( "outputdir", cmdLine.GetOption( "outputdir" ) );
		}
		/// <summary>
		/// Loads all available game data
		/// </summary>
		/// <remarks>
		/// <see cref="LoadAll()"/> uses <see cref="Arguments"/>'s
		/// "datadir" value to load game information from a game data directory
		/// into the <see cref="Program.Game"/> static property. All
		/// information that is extractable from the data directory (including
		/// all version and all assets and components within those versions)
		/// will be loaded.
		/// </remarks>
		static void LoadAll() {
			string dataDirName = null;
			if ( Arguments is null ) {
				GetArguments();
			}
			if ( Arguments.ContainsKey( "datadir" ) ) {
				dataDirName = Arguments["datadir"];
			} else {
				// TODO: #103 build CommandLineException class into CommandLine
				System.Console.WriteLine( "You must provide the name of a data directory." );
			}
			if ( !Directory.Exists( dataDirName ) ) {
				throw new DirectoryNotFoundException( $"Unable to access directory '${dataDirName}'" );
			}
			DirectoryInfo dataDir = new DirectoryInfo( dataDirName );
			Game = new Game( GameName );
			Game.LoadAll( dataDir.FullName );
		}
		/// <summary>
		/// Outputs all loaded <see cref="Game"/> data
		/// </summary>
		/// <remarks>
		/// <see cref="WriteAll()"/> saves all data that has been loaded into the
		/// <see cref="Program.Game"/> static property into the output directory
		/// available from the <see cref="Arguments"/><c>["outputdir"]</c> value.
		/// If no data has been loaded prior to calling <see cref="WriteAll()"/>,
		/// <see cref="LoadAll()"/> will be run first.
		/// </remarks>
		static void WriteAll() {
			string saveDirName = null;
			if ( Arguments is null ) {
				GetArguments();
			}
			if ( Arguments.ContainsKey( "outputdir" ) ) {
				saveDirName = Arguments["outputdir"];
			} else {
				System.Console.WriteLine( "You must provide the name of an output directory." );
			}
			DirectoryInfo saveDir = new DirectoryInfo( saveDirName );
			if ( !saveDir.Exists ) {
				saveDir.Create();
			}
			string saveFile = $"{saveDir}/{GameName}.json";
			if ( Game is null ) {
				LoadAll();
			}
			Game.WriteJson( saveFile );
			foreach ( Game.Version version in Game.Versions ) {
				string filename = $"{saveDir}/roster-{version.Name}.csv";
				using ( StreamWriter file = new StreamWriter( filename ) ) {
					version.Components["Roster"].WriteCSV( file );
				}
			}
		}
	}
}
