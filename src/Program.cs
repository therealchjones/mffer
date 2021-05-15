using System.IO;
using CommandLine;

namespace Mffer {
	/// <summary>
	/// The primary application class
	/// </summary>
	public class Program {
		/// <summary>
		/// The name of the game
		/// </summary>
		const string gameName = "Marvel Future Fight";
		/// <summary>
		/// Marvel Future Fight data extraction and reporting
		/// </summary>
		/// <returns>Nonzero on error, 0 otherwise</returns>
		static int Main() {
			Parser cmdLine = new Parser();
			string dataDir = cmdLine.GetOption( "datadir" );
			string saveDir = cmdLine.GetOption( "outputdir" );
			if ( string.IsNullOrEmpty( dataDir ) || string.IsNullOrEmpty( saveDir ) ) {
				System.Console.Error.WriteLine( "Usage: mffer --datadir data_directory --outputdir output_directory" );
				return 1;
			}
			if ( !Directory.Exists( dataDir ) ) {
				throw new DirectoryNotFoundException();
			}
			if ( !Directory.Exists( saveDir ) ) {
				throw new DirectoryNotFoundException();
			}
			Game game = new Game( gameName );
			game.LoadAllData( dataDir );
			string saveFile = $"{saveDir}/{gameName}.json";
			game.WriteJson( saveFile );
			foreach ( Game.Version version in game.Versions ) {
				string filename = $"{saveDir}/roster-{version.Name}.csv";
				using ( StreamWriter file = new StreamWriter( filename ) ) {
					version.Components["Roster"].WriteCSV( file );
				}
			}
			return 0;
		}
	}
}
