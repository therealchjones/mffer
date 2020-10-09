using System.IO;

/*
    Coding notes:
    - user-modifiable variables should be in the Program class
    - the Program class should only contain these and associated functions to manipulate them
      and interact with the Game class
    - the Game class should interact only with other classes in the Game Classes.cs file and 
      the DataDirectory class
    - only the Program and DataDirectory classes should manipulate the file system
 */

namespace MFFDataApp
{
    public class Program
    {
        public static AssetBundle Assets; // here just until the global is no longer needed
        const string gameName = "Marvel Future Fight";
        const string dataDir = "/Users/chjones/Development/Marvel Future Fight/MffData/data";
        const string outputDir = dataDir;
        public static void Main()
        {
            if ( ! Directory.Exists(dataDir) ) {
                throw new DirectoryNotFoundException();
            }
            Game game = new Game( gameName, new DirectoryInfo(dataDir) );
            game.LoadAllData();
            // some data class thing to json output
        }
    }
}