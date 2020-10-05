using System.Collections.Generic;

namespace MFFDataApp
{
    public class Program
    {
        public Dictionary<string,Component> Components { get; set; }
        public static AssetBundle Assets { get; set; }
        public static void Main()
        {
            Assets = new AssetBundle();
            Assets.Load();
            Assets.ToJsonFile();
        }
    }
}