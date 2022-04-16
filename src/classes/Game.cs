using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mffer {
	/// <summary>
	/// Represents a game, including one or more versions
	/// </summary>
	/// <remarks>
	/// This is the primary class of the Mffer namespace, and public
	/// interaction should be via this class. Abstractly, each game instance
	/// includes individual player data as well as one or more versions of the
	/// game, each of which includes zero or more game components (such as a
	/// character roster or list of story stages). Methods allow loading game
	/// data from the filesystem, saving consolidated data to a file, and
	/// saving individual CSV files for use elsewhere.
	/// </remarks>
	public class Game : GameObject {
		/// <summary>
		/// Gets or sets the name of the game
		/// </summary>
		public string Name { get; set; }
		/// <summary>
		/// Gets or sets the included versions of the game
		/// </summary>
		public List<Version> Versions { get; set; }
		List<Player> Players { get; set; }
		List<Alliance> Alliances { get; set; }
		/// <summary>
		/// Initializes a new instance of the <see cref="Game"/> class
		/// </summary>
		public Game() : base() {
			Versions = new List<Version>();
			Players = new List<Player>();
			Alliances = new List<Alliance>();
		}
		/// <summary>
		/// Initializes a new instance of the <see cref="Game"/> class and
		/// sets its name
		/// </summary>
		/// <param name="gameName">game name</param>
		public Game( string gameName ) : this() {
			Name = gameName;
		}
		/// <summary>
		/// Loads all game data from a directory into the <see cref="Game"/>
		/// instance
		/// </summary>
		/// <param name="dir">path of a directory containing game data</param>
		public void LoadAll( string dir ) {
			DataSource dataSource = new DataSource( dir );
			List<string> versionNames = dataSource.GetVersionNames();
			foreach ( string versionName in versionNames ) {
				Version version = new Version( versionName );
				version.Data = dataSource.GetData( versionName );
				// version.LoadData();
				version.LoadComponents();
				Versions.Add( version );
			}
		}
		/// <summary>
		/// Writes all loaded data to files, separated by version
		/// </summary>
		/// <param name="directory">Name of a directory into which to write the files</param>
		public void ToJsonFiles( DirectoryInfo directory ) {
			if ( !directory.Exists ) directory.Create();
			JsonSerializerOptions serialOptions = new JsonSerializerOptions();
			JsonWriterOptions writeOptions = new JsonWriterOptions() { Indented = true, SkipValidation = false };
			foreach ( Version version in Versions ) {
				string fileName = Path.Join( directory.FullName, version.Name + ".json" );
				using ( Stream file = new FileStream( fileName, FileMode.Create ) ) {
					version.ToJson( file, serialOptions, writeOptions );
				}
			}
		}
		/// <summary>
		/// Write data from each <see cref="Version"/> to a usable file
		/// </summary>
		/// <param name="directory">The name of a directory into which to write
		/// the files</param>
		public void WriteCSVs( DirectoryInfo directory ) {
			if ( !directory.Exists ) directory.Create();
			foreach ( Version version in Versions ) {
				version.WriteCSVs( directory );
			}
		}
	}
}
