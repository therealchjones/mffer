using System.IO;

namespace MFFDataApp
{
    public class Program
    {
        // prolly remove this
        public static AssetBundle Assets;
        const string gameName = @"Marvel Future Fight";
        const string dataDir = @"/Users/chjones/Development/Marvel Future Fight/MffData/data";
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