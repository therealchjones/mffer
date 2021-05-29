using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
		/// <summary>
		/// Initializes a new instance of the <see cref="Game"/> class
		/// </summary>
		public Game() : base() {
			Versions = new List<Version>();
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
				version.Assets = dataSource.GetAssets( versionName );
				version.LoadAssets();
				version.LoadComponents();
				Versions.Add( version );
			}
		}
		/// <summary>
		/// Write all loaded data to a file
		/// </summary>
		/// <remarks>
		/// <para>This method saves all loaded data from the <see cref="Game"/>
		/// to a single file in JSON format, hierarchically arranged by game,
		/// version, and components and assets. <paramref name="fileName"/>
		/// is created if it does not exist (but its parent directory does);
		/// <paramref name="fileName"/> is overwritten if it already
		/// exists.</para>
		/// <para>File access is obtained via the
		/// <see cref="System.IO.StreamWriter.StreamWriter(string)"/> method;
		/// see that method's description for exceptions that may be
		/// thrown.</para>
		/// </remarks>
		/// <param name="fileName">The file path in which to save game
		/// data</param>
		public void WriteJson( string fileName ) {
			// implemented as streamwriter at all levels because using a string or
			// similar uses up all memory, same with JsonSerializer
			using ( StreamWriter file = new StreamWriter( fileName ) ) {
				file.WriteLine( "{" );
				file.WriteLine( $"\t\"{Name}\" : " + "{" );
				int versionCounter = 0;
				foreach ( Version version in Versions ) {
					// WriteJson should consistently write the instance as one or more
					// JSON members (string: element) without a bare root element, and without
					// a newline on the last line. It is on the caller to provide appropriate
					// wrapping. The (optional) second argument prepends each line of
					// the JSON output with that number of tabs
					version.WriteJson( file, 2 );
					versionCounter++;
					if ( versionCounter < Versions.Count ) {
						file.Write( "," );
					}
					file.WriteLine( "" );
				}
				file.WriteLine( "\t}" );
				file.WriteLine( "}" );
			}
			return;
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
			/// To Remove, placeholder
			/// </summary>
			public PreferenceFile Preferences { get; set; }
			/// <summary>
			/// Gets or sets the name of the <see cref="Version"/>
			/// </summary>
			public string Name { get; set; }
			/// <summary>
			/// Gets or sets the list of included <see cref="Component"/>, indexed
			/// by Component name
			/// </summary>
			public Dictionary<string, Component> Components { get; set; }
			/// <summary>
			/// Gets or sets the group of <see cref="AssetFile"/>s associated with
			/// this <see cref="Version"/>
			/// </summary>
			// TODO: #106 change to a better name
			public AssetBundle Assets { get; set; }
			/// <summary>
			/// Initializes a new instance of the <see cref="Version"/> class
			/// </summary>
			public Version() : base() {
				Name = "";
				Components = new Dictionary<string, Component>();
				Assets = null;
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
			dynamic GetAsset( string assetName, string assetFile ) {
				if ( assetFile is null ) {
					throw new ArgumentNullException( "assetFile" );
				}
				if ( !Assets.DataFiles.ContainsKey( assetFile ) ) {
					throw new KeyNotFoundException( $"Unable to find asset file named {assetFile}" );
				}
				GameObject file = Assets.DataFiles[assetFile];
				if ( file is AssetFile ) {
					return ( (AssetFile)file ).GetAsset( assetName );
				} else if ( file is PreferenceFile && assetName == ( (PreferenceFile)file ).Name ) {
					return (PreferenceFile)file;
				} else {
					throw new KeyNotFoundException( $"Unable to find asset '{assetName}' in '{assetFile}'" );
				}
			}
			dynamic GetAsset( string assetName ) {
				dynamic asset = null;
				foreach ( string assetFile in Assets.DataFiles.Keys ) {
					try {
						asset = GetAsset( assetName, assetFile );
					} catch ( KeyNotFoundException ) {
						continue;
					}
					return asset;
				}
				throw new KeyNotFoundException( $"Unable to find asset '{assetName}'" );
			}
			bool TryGetAsset( string assetName, out dynamic asset ) {
				asset = null;
				foreach ( string fileName in Assets.DataFiles.Keys ) {
					try {
						asset = GetAsset( assetName, fileName );
					} catch ( KeyNotFoundException ) {
						continue;
					}
					break;
				}
				return asset != null;
			}
			/// <summary>
			/// Loads all data for this <see cref="Version"/> from the
			/// <see cref="DataSource"/>
			/// </summary>
			/// <remarks>
			/// <see cref="LoadAssets()"/> loads all available data,
			/// including data which is not required by any defined
			/// <see cref="Component"/>s,
			/// from the <see cref="DataSource"/>'s identified
			/// <see cref="AssetFile"/>s. This is usually necessary only for
			/// extensive cataloging and exploration rather than for creating
			/// usable data for the <see cref="Component"/>s.
			/// </remarks>
			public void LoadAssets() {
				Assets.LoadAll();
			}
			/// <summary>
			/// Adds a <see cref="Component"/> to this <see cref="Version"/>
			/// </summary>
			/// <param name="component"><see cref="Component"/> to associate with
			/// this <see cref="Version"/></param>
			void AddComponent( Component component ) {
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
							if ( TryGetAsset( assetName, out asset ) ) {
								component.BackingData[assetName] = asset;
							} else if ( assetName.Contains( "||" ) ) {
								foreach ( string possibleAssetName in assetName.Split( "||" ) ) {
									string possibleName = possibleAssetName.Trim();
									if ( String.IsNullOrEmpty( possibleName ) ) continue;
									if ( component.BackingData.ContainsKey( possibleName ) ) {
										component.BackingData.Remove( assetName );
										break;
									}
									if ( TryGetAsset( possibleName, out asset ) ) {
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
								component.BackingData[assetName] = GetAsset( "com.netmarble.mherosgb.v2.playerprefs.xml", "com.netmarble.mherosgb.v2.playerprefs.xml" );
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
			/// Writes <see cref="Version"/> data to an existing stream in JSON format
			/// </summary>
			/// <remarks>
			/// <para><see cref="Version.WriteJson(StreamWriter, int)"/> outputs all
			/// data from this <see cref="Version"/> to the
			/// <see cref="System.IO.StreamWriter"/> stream
			/// <paramref name="file"/> in JSON format. In order to accomodate
			/// writing this data as part of a larger JSON document while
			/// maintaining readability, the optional <paramref name="tabs"/>
			/// parameter indicates a number of tab characters to insert at the
			/// beginning of each line.</para>
			/// <para><see cref="Version.WriteJson(StreamWriter, int)"/> works by
			/// building the text for a <c>Version</c> JSON object and then
			/// calling the <c>WriteJson()</c> method for each
			/// <see cref="Component"/> and <see cref="AssetObject"/> associated
			/// with this version. The generic <c>WriteJson()</c> is called in
			/// turn for each property of each descendant of the <c>Version</c>.
			/// <c>WriteJson()</c> outputs a single JSON value (string, array, or
			/// object) without a trailing newline.</para>
			/// </remarks>
			/// <param name="file"><see cref="System.IO.StreamWriter"/> stream to which to write</param>
			/// <param name="tabs">Baseline number of tab characters to insert
			/// before each line of output</param>
			/// <seealso href="https://json.org">JSON.org</seealso>
			public override void WriteJson( StreamWriter file, int tabs = 0 ) {
				for ( int i = 0; i < tabs; i++ ) {
					file.Write( "\t" );
				}
				file.WriteLine( $"\"{Name}\" : " + "{" );
				for ( int i = 0; i < tabs + 1; i++ ) {
					file.Write( "\t" );
				}
				file.WriteLine( "\"Assets\" : {" );
				Assets.WriteJson( file, tabs + 2 );
				file.WriteLine();
				for ( int i = 0; i < tabs + 1; i++ ) {
					file.Write( "\t" );
				}
				file.Write( "}" );
				if ( Components.Count > 0 ) {
					file.WriteLine( "," );
					for ( int i = 0; i < tabs + 1; i++ ) {
						file.Write( "\t" );
					}
					file.WriteLine( "\"Components\" : {" );
					int componentCounter = 0;
					List<string> components = Components.Keys.ToList<string>();
					components.Sort();
					foreach ( string key in components ) {
						Component component = Components[key];
						component.WriteJson( file, tabs + 2 );
						componentCounter++;
						if ( componentCounter < Components.Count - 1 ) {
							file.WriteLine( "," );
						}
					}
					file.WriteLine();
					for ( int i = 0; i < tabs + 1; i++ ) {
						file.Write( "\t" );
					}
					file.Write( "}" );
				}
				if ( !( Preferences is null ) && !String.IsNullOrEmpty( Preferences.Name ) ) {
					file.WriteLine( "," );
					for ( int i = 0; i < tabs + 1; i++ ) {
						file.Write( "\t" );
					}
					file.WriteLine( "\"Preferences\" : {" );
					Preferences.WriteJson( file, tabs + 2 );
					file.WriteLine();
					for ( int i = 0; i < tabs + 1; i++ ) {
						file.Write( "\t" );
					}
					file.Write( "}" );
				}
				file.WriteLine();
				for ( int i = 0; i < tabs; i++ ) {
					file.Write( "\t" );
				}
				file.Write( "}" );
			}
		}
	}
}
