using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

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
				version.LoadData();
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
			JsonSerializerOptions serialOptions = new JsonSerializerOptions( JsonSerializerDefaults.General );
			JsonWriterOptions writeOptions = new JsonWriterOptions() { Indented = true, SkipValidation = true };
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
		/// <summary>
		/// Represents a single version of a <see cref="Game"/>
		/// </summary>
		/// <remarks>
		/// Different versions of a game may vary in
		/// nearly all data; almost all game data are contained within a
		/// <see cref="Version"/>, including the various <see cref="Component"/>s
		/// and <see cref="AssetFile"/>s. Methods allow loading data from a
		/// <see cref="DataSource"/>, writing the version's
		/// <see cref="AssetObject"/> and <see cref="Component"/> data to an existing
		/// stream, and writing individual <see cref="Component"/> data in CSV
		/// format to an existing stream.
		/// </remarks>
		public class Version : GameObject {
			/// <summary>
			/// Gets or sets the name of the <see cref="Version"/>
			/// </summary>
			public string Name { get; set; }
			/// <summary>
			/// Gets or sets the list of included <see cref="Component"/>s,
			/// indexed by Component name
			/// </summary>
			public Dictionary<string, Component> Components { get; set; }
			/// <summary>
			/// Gets or sets the group of data files associated with
			/// this <see cref="Version"/>
			/// </summary>
			public DataBundle Data { get; set; }
			/// <summary>
			/// Initializes a new instance of the <see cref="Version"/> class
			/// </summary>
			public Version() : base() {
				Name = "";
				Components = new Dictionary<string, Component>();
				Data = null;
				AddComponent( new Catalog() );
				AddComponent( new Localization() );
				AddComponent( new Roster() );
			}
			/// <summary>
			/// Initializes a new instance of the <see cref="Version"/>
			/// class and sets its name
			/// </summary>
			/// <param name="versionName">The name of the version</param>
			/// <seealso cref="Version.Version()"/>
			public Version( string versionName ) : this() {
				Name = versionName;
			}
			dynamic GetData( string objectName, string dataFile ) {
				if ( dataFile is null ) {
					throw new ArgumentNullException( "dataFile" );
				}
				if ( !Data.DataFiles.ContainsKey( dataFile ) ) {
					throw new KeyNotFoundException( $"Unable to find asset file named {dataFile}" );
				}
				GameObject file = Data.DataFiles[dataFile];
				if ( file is AssetFile ) {
					return ( (AssetFile)file ).GetAsset( objectName );
				} else if ( file is PreferenceFile && objectName == ( (PreferenceFile)file ).Name ) {
					return (PreferenceFile)file;
				} else {
					throw new KeyNotFoundException( $"Unable to find asset '{objectName}' in '{dataFile}'" );
				}
			}
			dynamic GetData( string objectName ) {
				dynamic data = null;
				foreach ( string dataFile in Data.DataFiles.Keys ) {
					try {
						data = GetData( objectName, dataFile );
					} catch ( KeyNotFoundException ) {
						continue;
					}
					return data;
				}
				throw new KeyNotFoundException( $"Unable to find asset '{objectName}'" );
			}
			bool TryGetObject( string objectName, out dynamic data ) {
				data = null;
				try {
					data = GetData( objectName );
				} catch ( KeyNotFoundException ) {
					return false;
				}
				return true;
			}
			/// <summary>
			/// Loads all data for this <see cref="Version"/> from the
			/// <see cref="DataSource"/>
			/// </summary>
			/// <remarks>
			/// <see cref="LoadData()"/> loads all available data,
			/// including data which is not required by any defined
			/// <see cref="Component"/>s,
			/// from the <see cref="DataSource"/>'s identified
			/// <see cref="AssetFile"/>s. This is usually necessary only for
			/// extensive cataloging and exploration rather than for creating
			/// usable data for the <see cref="Component"/>s.
			/// </remarks>
			public void LoadData() {
				Data.LoadAll();
			}
			/// <summary>
			/// Adds a <see cref="Component"/> to this <see cref="Version"/>
			/// </summary>
			/// <param name="component"><see cref="Component"/> to associate with
			/// this <see cref="Version"/></param>
			void AddComponent( Component component ) {
				if ( String.IsNullOrEmpty( component.Name ) ) throw new Exception( "The name of the component must be set" );
				Components.Add( component.Name, component );
			}
			/// <summary>
			/// Loads data into all of the included <see cref="Component"/>s
			/// </summary>
			public void LoadComponents() {
				foreach ( Component component in Components.Values ) {
					LoadComponent( component );
				}
			}
			/// <summary>
			/// Loads data into the <see cref="Component"/> named
			/// <paramref name="componentName"/>
			/// </summary>
			/// <remarks>
			/// Will load available data into the <see cref="Component"/> named
			/// <paramref name="componentName"/> if it has already been added to
			/// the <see cref="Version.Components"/> list.
			/// </remarks>
			/// <param name="componentName">The name of the
			/// <see cref="Component"/></param>
			/// <exception cref="System.ArgumentException">Thrown when no component
			/// named <paramref name="componentName"/> is loaded</exception>
			/// <seealso cref="Version.AddComponent(Component)"/>
			/// <seealso cref="Version.LoadComponent(Component)"/>
			public void LoadComponent( string componentName ) {
				if ( Components.ContainsKey( componentName ) ) {
					LoadComponent( Components[componentName] );
				} else {
					throw new Exception( $"Unable to load; no component named '{componentName}'" );
				}
			}
			/// <summary>
			/// Loads data into the given <see cref="Component"/>
			/// </summary>
			/// <remarks>
			/// Will load available data into <paramref name="component"/> from
			/// <see cref="AssetFile"/>s named in
			/// <see cref="Component.BackingData"/>. (The assets will be loaded
			/// if they aren't already.) If data has already been loaded into
			/// <paramref name="component"/>, it will not be changed.
			/// </remarks>
			/// <param name="component">The <see cref="Component"/> to load with data</param>
			/// <exception cref="System.ApplicationException">Thrown if a required
			/// <see cref="AssetFile"/> from <paramref name="component"/>'s
			/// <see cref="Component.BackingData"/> or a required <see cref="Component"/>
			/// from <see cref="Component.Dependencies"/> is not found or cannot be
			/// loaded.</exception>
			public void LoadComponent( Component component ) {
				if ( !component.IsLoaded() ) {
					foreach ( string assetName in component.BackingData.Keys.ToList<string>() ) {
						if ( component.BackingData[assetName] == null ) {
							dynamic asset = null;
							if ( TryGetObject( assetName, out asset ) ) {
								component.BackingData[assetName] = asset;
							} else if ( assetName.Contains( "||" ) ) {
								foreach ( string possibleAssetName in assetName.Split( "||" ) ) {
									string possibleName = possibleAssetName.Trim();
									if ( String.IsNullOrEmpty( possibleName ) ) continue;
									if ( component.BackingData.ContainsKey( possibleName ) ) {
										component.BackingData.Remove( assetName );
										break;
									}
									if ( TryGetObject( possibleName, out asset ) ) {
										component.BackingData.Add( possibleName, asset );
										component.BackingData.Remove( assetName );
										break;
									}
								}
								if ( component.BackingData.ContainsKey( assetName ) ) {
									string assetsString = String.Join( ", ", assetName.Split( "||" ) );
									throw new ApplicationException( $"Unable to find any of the possible assets ({assetsString}) for component '{component.Name}'" );
								}
							} else if ( assetName == "Preferences.xml" || assetName == "Preferences" ) {
								component.BackingData[assetName] = GetData( "com.netmarble.mherosgb.v2.playerprefs.xml", "com.netmarble.mherosgb.v2.playerprefs.xml" );
							} else {
								throw new ApplicationException( $"Unable to load asset '{assetName}' for component '{component.Name}'" );
							}
						}
					}
					foreach ( string componentName in component.Dependencies.Keys.ToList<string>() ) {
						if ( Components.ContainsKey( componentName ) ) {
							LoadComponent( componentName );
							component.Dependencies[componentName] = Components[componentName];
						} else {
							throw new ApplicationException( $"Unable to load dependencies for component {component.Name}: could not find component named {componentName}." );
						}
					}
					component.Load();
				}
			}
			/// <summary>
			/// Write usable <see cref="Component"/> data to a file
			/// </summary>
			/// <param name="directory">The name of a directory into which files
			/// will be written</param>
			public void WriteCSVs( DirectoryInfo directory ) {
				foreach ( Component component in Components.Values ) {
					string fileName = Path.Combine( directory.FullName, component.Name + "-" + Name + ".csv" );
					component.WriteCSV( fileName );
				}
			}
		}
	}
}
