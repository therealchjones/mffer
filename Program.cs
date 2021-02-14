using System.IO;

/*
    Coding best practices:
    - user-modifiable variables should be in the Program class. When not requiring other
      non-System classes, validation of these variables' values should be done in the Program
      class.
    - the Program class should only contain these variables and associated functions to manipulate them
      and interact with the Game class.
    - the Game class should interact only with other classes in the Game Classes.cs file and 
      the DataDirectory class
    - only the Program and DataDirectory classes should manipulate the file system
    - this is mainly taken care of by separating the classes in the hierarchy, e.g., 
      DataDirectory methods can create objects described in Game Classes.cs but can't access 
      members of Game
 */

namespace MFFDataApp
{
    public class Program
    {
        const string gameName = "Marvel Future Fight";
        const string saveDir = "/Users/chjones/Development/Marvel Future Fight/MffData/data";
        const string dataDir = "/Users/chjones/Development/Marvel Future Fight/data";
        public static void Main()
        {
            if ( ! Directory.Exists(dataDir) ) {
              throw new DirectoryNotFoundException();
            }
            if ( ! Directory.Exists(saveDir) ) {
              throw new DirectoryNotFoundException();
            }
            Game game = new Game( gameName );
            game.LoadAllData( dataDir );
            string saveFile = $"{saveDir}/{gameName}.json";
            game.SaveAllData(saveFile);
        }
    }
}