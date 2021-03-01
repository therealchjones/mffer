using System.IO;

namespace MFFDataApp {
	public class Program {
		const string gameName = "Marvel Future Fight";
		const string saveDir = "/Users/chjones/Development/Marvel Future Fight/MffData/data";
		const string dataDir = "/Users/chjones/Development/Marvel Future Fight/data";
		public static void Main() {
			if ( !Directory.Exists( dataDir ) ) {
				throw new DirectoryNotFoundException();
			}
			if ( !Directory.Exists( saveDir ) ) {
				throw new DirectoryNotFoundException();
			}
			Game game = new Game( gameName );
			game.LoadAllData( dataDir );
			string saveFile = $"{saveDir}/{gameName}.json";
			game.SaveAllData( saveFile );
			foreach ( Version version in game.Versions ) {
				string filename = $"{saveDir}/roster-{version.Name}.csv";
				using ( StreamWriter file = new StreamWriter( filename ) ) {
					version.Components["Roster"].WriteCSV( file );
				}
			}
		}
	}
}
