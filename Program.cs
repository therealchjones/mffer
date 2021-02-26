using System.IO;

/*
    Coding best practices:
    - user-modifiable variables should be in the Program class
    - validation of Program class settings should be done by called methods rather than by the user
    - the Program class should only interact with the Game class
    - interaction with the Game classes should be done via the Game class/object
    - interaction with the Data classes should be done via the DataDirectory class/object
    - this can be done by placing all classes within their associated "top level" class?
      (probably not, and more importantly this would likely give member classes access to top-
      level "global" fields)
 */

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
