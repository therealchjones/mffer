using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MFFDataApp {
	/// <summary>
	/// Represents a game, including one or more versions
	/// </summary>
	/// <remarks>
	/// This is the primary class of the MffData namespace, and public
	/// interaction should be via this class. Abstractly, each game instance
	/// includes one or more versions of the game, each of which includes
	/// zero or more game components (such as a character roster, list of
	/// story stages, or individual player data. Methods allow loading game
	/// data from the filesystem, saving consolidated data to a file, and
	/// saving individual CSV files of data from game components.
	/// </remarks>
	public class Game {
		/// <summary>
		/// Gets or sets the name of the game
		/// </summary>
		public string Name { get; set; }
		/// <summary>
		/// Gets or sets the included versions of the game
		/// </summary>
		public List<Version> Versions { get; set; }
		/// <summary>
		/// Initializes a new instance of the <see cref="Game"/> class and
		/// sets its name
		/// </summary>
		/// <param name="gameName">game name</param>
		public Game( string gameName ) {
			Name = gameName;
			Versions = new List<Version>();
		}
		/// <summary>
		/// Loads all game data from a directory into the <see cref="Game"/>
		/// instance
		/// </summary>
		/// <param name="dir">path of a directory containing game data</param>
		public void LoadAllData( string dir ) {
			DataDirectory dataDir = new DataDirectory( dir );
			List<string> versionNames = dataDir.GetVersionNames();
			foreach ( string versionName in versionNames ) {
				Version version = new Version( versionName );
				version.Assets = dataDir.GetAssets( versionName );
				version.LoadAllComponents();
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
		public void SaveAllData( string fileName ) {
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
	}
	/// <summary>
	/// Represents a single version of a <see cref="Game"/>
	/// </summary>
	/// <remarks>
	/// This is a private class. Different versions of a game may vary in
	/// nearly all data; almost all game data are contained within a
	/// <see cref="Version"/>, including the various <see cref="Component"/>s
	/// and <see cref="AssetFile"/>s. Methods allow loading data from a
	/// <see cref="DataDirectory"/>, writing the version's
	/// <see cref="AssetObject"/> and <see cref="Component"/> data to an existing
	/// stream, and writing individual <see cref="Component"/> data in CSV
	/// format to an existing stream.
	/// </remarks>
	public class Version {
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
		public AssetBundle Assets { get; set; }
		/// <summary>
		/// Initializes a new instance of the <see cref="Version"/> class
		/// </summary>
		public Version() {
			Name = "";
			Components = new Dictionary<string, Component>();
			Assets = new AssetBundle();

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
		public void LoadAllComponents() {
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
		/// <see cref="Component.BackingAssets"/>. (The assets will be loaded
		/// if they aren't already.) If data has already been loaded into
		/// <paramref name="component"/>, it will not be changed.
		/// </remarks>
		/// <param name="component">The <see cref="Component"/> to load with data</param>
		/// <exception cref="System.ApplicationException">Thrown if a required
		/// <see cref="AssetFile"/> from <paramref name="component"/>'s
		/// <see cref="Component.BackingAssets"/> or a required <see cref="Component"/>
		/// from <see cref="Component.Dependencies"/> is not found or cannot be
		/// loaded.</exception>
		public void LoadComponent( Component component ) {
			if ( !component.IsLoaded() ) {
				foreach ( string assetName in component.BackingAssets.Keys.ToList<string>() ) {
					if ( component.BackingAssets[assetName] == null ) {
						if ( Assets.AssetFiles.ContainsKey( assetName ) ) {
							component.BackingAssets[assetName] = Assets.AssetFiles[assetName];
						} else if ( assetName.Contains( "||" ) ) {
							foreach ( string possibleAssetName in assetName.Split( "||" ) ) {
								string possibleName = possibleAssetName.Trim();
								if ( String.IsNullOrEmpty( possibleName ) ) continue;
								if ( component.BackingAssets.ContainsKey( possibleName ) ) {
									component.BackingAssets.Remove( assetName );
									break;
								}
								if ( Assets.AssetFiles.ContainsKey( possibleName ) ) {
									component.BackingAssets.Add( possibleName, Assets.AssetFiles[possibleName] );
									component.BackingAssets.Remove( assetName );
									break;
								}
							}
							if ( component.BackingAssets.ContainsKey( assetName ) ) {
								string assetsString = String.Join( ", ", assetName.Split( "||" ) );
								throw new ApplicationException( $"Unable to find any of the possible assets ({assetsString}) for component '{component.Name}'" );
							}
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
		public void WriteJson( StreamWriter file, int tabs = 0 ) {
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
			file.WriteLine();
			for ( int i = 0; i < tabs; i++ ) {
				file.Write( "\t" );
			}
			file.Write( "}" );
		}
	}
	/// <summary>
	/// Represents a generic part of a game's content
	/// </summary>
	/// <remarks>
	/// Major game content is represented by derivatives of the
	/// <see cref="Component"/> class. This class includes the base
	/// properties and methods applicable to all derivatives, including
	/// lists of the <see cref="AssetObject"/>s and other
	/// <see cref="Component"/>s
	/// required for loading data into the instance or evaluating or printing
	/// the data.
	/// </remarks>
	public class Component {
		/// <summary>
		/// Gets or sets the name of the <see cref="Component"/>
		/// </summary>
		public string Name { get; set; }
		/// <summary>
		/// Gets or sets a collection of <see cref="AssetObject"/>s storing
		/// data to be loaded into the <see cref="Component"/>, indexed by
		/// name.
		/// </summary>
		/// <remarks>
		/// Required <see cref="AssetObject"/>s should be named in the keys of
		/// <see cref="Component.BackingAssets"/> when the derived instance
		/// is initialized. When the parent <see cref="Version"/> loads data
		/// into the <see cref="Component"/>, it must first load the named
		/// <c>AssetObject</c>s and place them into the associated values of
		/// <c>BackingAssets</c>.
		/// </remarks>
		public Dictionary<string, AssetObject> BackingAssets { get; set; }
		/// <summary>
		/// Gets or sets a collection of <see cref="Component"/>s referred to
		/// by this <see cref="Component"/>, indexed by name.
		/// </summary>
		/// <remarks>
		/// Required <see cref="Component"/>s should be named in the keys of
		/// <see cref="Component.Dependencies"/> when the derived instance
		/// is initialized. When the parent <see cref="Version"/> loads data
		/// into this <see cref="Component"/>, it must first load the named
		/// <c>Component</c>s and place them into the associated values of
		/// <c>Dependencies</c>.
		/// </remarks>
		public Dictionary<string, Component> Dependencies { get; set; }
		/// <summary>
		/// Initializes a new instance of the <see cref="Component"/> class
		/// </summary>
		protected Component() {
			BackingAssets = new Dictionary<string, AssetObject>();
			Dependencies = new Dictionary<string, Component>();
		}
		/// <summary>
		/// Adds the name of an asset to the list of
		/// <see cref="BackingAssets"/> for this <see cref="Component"/>
		/// </summary>
		/// <remarks>
		/// No validation or checking of the <paramref name="assetName"/>
		/// parameter is performed at the time of adding the
		/// <see cref="AssetFile"/> name to the <see cref="BackingAssets"/> list.
		/// This is deferred until attempting to load data into the
		/// <see cref="Component"/> as the <c>BackingAssets</c> list may
		/// be created before all <c>Asset</c>s are loaded.
		/// </remarks>
		/// <param name="assetName">The name of the <see cref="AssetFile"/> to
		/// add</param>
		public virtual void AddBackingAsset( string assetName ) {
			if ( !BackingAssets.ContainsKey( assetName ) ) {
				BackingAssets.Add( assetName, null );
			}
		}
		/// <summary>
		/// Adds the name of an asset to the list of
		/// <see cref="Dependencies"/> for this <see cref="Component"/>
		/// </summary>
		/// <remarks>
		/// No validation or checking of the <paramref name="componentName"/>
		/// parameter is performed at the time of adding the
		/// <see cref="Component"/> name to the <see cref="Dependencies"/> list.
		/// This is deferred until attempting to load data into the
		/// <see cref="Component"/> as the <c>Dependencies</c> list may
		/// be created before all <c>Component</c>s are loaded.
		/// </remarks>
		/// <param name="componentName">The name of the <see cref="Component"/>
		/// to add</param>
		public virtual void AddDependency( string componentName ) {
			if ( !Dependencies.ContainsKey( componentName ) ) {
				Dependencies.Add( componentName, null );
			}
		}
		/// <summary>
		/// Outputs data from this <see cref="Component"/> in JSON format
		/// </summary>
		/// <param name="file"><see cref="System.IO.StreamWriter"/> stream to which to write</param>
		/// <param name="tabs">Baseline number of tab characters to insert
		/// before each line of output</param>
		/// <seealso cref="Version.WriteJson(StreamWriter, int)"/>
		public virtual void WriteJson( StreamWriter file, int tabs = 0 ) {
		}
		/// <summary>
		/// Outputs select data from this <see cref="Component"/> in CSV format
		/// </summary>
		/// <remarks>
		/// <see cref="WriteCSV( StreamWriter )"/> writes data from the
		/// <see cref="Component"/> to <paramref name="file"/> in a format
		/// useful for importing into a spreadsheet. <c>WriteCSV()</c> is not
		/// intended to losslessly output all of the <see cref="Component"/>'s
		/// data, but rather to present select data in usable format for
		/// further processing. For the former purpose, use
		/// <see cref="WriteJson(StreamWriter,int)"/>.
		/// </remarks>
		/// <param name="file"><see cref="StreamWriter"/> stream to which to
		/// write</param>
		public virtual void WriteCSV( StreamWriter file ) {
		}
		/// <summary>
		/// Loads data into this <see cref="Component"/>
		/// </summary>
		/// <remarks>
		/// <see cref="Component.Load()"/> uses objects loaded into
		/// <see cref="Component.BackingAssets"/> and
		/// <see cref="Component.Dependencies"/> to load data into
		/// <see cref="Component"/>'s other properties. As the
		/// <c>Component</c> does not have access to the overall
		/// sets of <see cref="Version.Assets"/> and
		/// <see cref="Version.Components"/>, both
		/// <c>BackingAssets</c> and <c>Dependencies</c> must be loaded by an
		/// ancestor instance (e.g., via
		/// <see cref="Version.LoadComponent(Component)"/>) before
		/// <c>Component.Load()</c> can successfully run.
		/// </remarks>
		/// <exception cref="System.ApplicationException">Thrown if objects
		/// have not been loaded into <see cref="BackingAssets"/> or
		/// <see cref="Dependencies"/> before running
		/// <see cref="Load()"/></exception>
		public virtual void Load() {
			if ( IsLoaded() ) return;
			if ( BackingAssets.Count != 0 ) {
				foreach ( KeyValuePair<string, AssetObject> item in BackingAssets ) {
					if ( String.IsNullOrWhiteSpace( item.Key ) ) {
						BackingAssets.Remove( item.Key );
					} else {
						if ( item.Value == null ) {
							throw new Exception( $"Unable to load {Name}: backing asset {item.Key} not loaded. Preload needed." );
						}
					}
				}
			}
			if ( Dependencies.Count != 0 ) {
				foreach ( KeyValuePair<string, Component> item in Dependencies ) {
					if ( String.IsNullOrWhiteSpace( item.Key ) ) {
						Dependencies.Remove( item.Key );
					} else {
						if ( item.Value == null || !item.Value.IsLoaded() ) {
							throw new Exception( $"Unable to load {Name}: dependency {item.Key} not loaded. Preload needed." );
						}
					}
				}
			}
		}
		/// <summary>
		/// Reports whether the <see cref="Component"/> has already been
		/// loaded
		/// </summary>
		/// <remarks>
		/// <see cref="Component.IsLoaded()"/> analyzes the data in
		/// <c>Component</c>'s properties to determine whether the
		/// <c>Component</c> has already been loaded (e.g., via
		/// <see cref="Component.Load()"/>). Note that this not imply that
		/// if <c>Component.Load()</c> were run again the properties
		/// would be unchanged. In practice, <c>Component.Load()</c> should
		/// only be run after all <see cref="Component.BackingAssets"/> and
		/// <see cref="Component.Dependencies"/> have been loaded, so the
		/// property loading should be reproducible at any point afterward.
		/// </remarks>
		/// <returns><c>true</c> if the <see cref="Component"/> contains
		/// data loaded data, <c>false</c> otherwise</returns>
		public virtual bool IsLoaded() {
			return true;
		}
	}
	/// <summary>
	/// Provides access to the string localiization dictionary
	/// </summary>
	/// <remarks>
	/// The <see cref="Localization"/> class is a derivative of
	/// <see cref="Component"/> to provide access to the
	/// <see cref="Version"/>'s string localization dictionary. This includes
	/// methods to build the dictionary from the appropriate
	/// <see cref="AssetFile"/>, translate encoded strings into localized
	/// strings, and output the full dictionary as a JSON object.
	/// </remarks>
	public class Localization : Component {
		/// <summary>
		/// Gets or sets the dictionary object
		/// </summary>
		Dictionary<string, string> LocalDictionary { get; set; }
		/// <summary>
		/// Gets or sets the name of the localization language
		/// </summary>
		public string Language { get; set; }
		/// <summary>
		/// Initializes a new instance of the <see cref="Localization"/>
		/// <see cref="Component"/>-derived class.
		/// </summary>
		public Localization() : base() {
			Name = "Localization";
			LocalDictionary = new Dictionary<string, string>();
			Language = "en";
			AddBackingAsset( $"localization/localization_{Language}.csv||LocalizationTable_{Language}" );
		}
		/// <summary>
		/// Determines whether the <see cref="Localization"/> has been
		/// loaded.
		/// </summary>
		/// <returns><c>true</c> if the <see cref="Localization"/> already
		/// contains loaded data, <c>false</c> otherwise.</returns>
		/// <seealso cref="Component.IsLoaded()"/>
		public override bool IsLoaded() {
			return LocalDictionary.Count != 0;
		}
		/// <summary>
		/// Loads data into this <see cref="Localization"/>
		/// </summary>
		/// <seealso cref="Component.Load()"/>
		public override void Load() {
			base.Load();
			AssetObject DictionaryAsset = BackingAssets.First().Value;
			// the localization dictionary was a CSV in 6.2.0, but is in an asset in
			// 6.7.0; will have to manage differently
			if ( BackingAssets.First().Key.EndsWith( ".csv", StringComparison.InvariantCultureIgnoreCase ) ) {
				foreach ( AssetObject entry in DictionaryAsset.Properties["m_Script"].Array ) {
					LocalDictionary[entry.Properties["KEY"].String] = entry.Properties["TEXT"].String;
				}
			} else {
				Dictionary<string, string> keys = new Dictionary<string, string>();
				Dictionary<string, string> values = new Dictionary<string, string>();
				foreach ( int keyNum in Enumerable.Range( 0, DictionaryAsset.Properties["keyTable"].Properties["keys"].Properties["Array"].Array.Count() ) ) {
					keys.Add( DictionaryAsset.Properties["keyTable"].Properties["keys"].Properties["Array"].Array[keyNum].Properties["data"].String,
						DictionaryAsset.Properties["keyTable"].Properties["values"].Properties["Array"].Array[keyNum].Properties["data"].String );
				}
				foreach ( int keyNum in Enumerable.Range( 0, DictionaryAsset.Properties["valueTable"].Properties["keys"].Properties["Array"].Array.Count() ) ) {
					values.Add( DictionaryAsset.Properties["valueTable"].Properties["keys"].Properties["Array"].Array[keyNum].Properties["data"].String,
						DictionaryAsset.Properties["valueTable"].Properties["values"].Properties["Array"].Array[keyNum].Properties["data"].String );
				}
				if ( new HashSet<string>( keys.Values ).Count() == values.Count() ) {
					LocalDictionary = Enumerable.Range( 0, keys.Count() ).ToDictionary(
						i => keys.Keys.ToList()[i],
						i => values[keys.Values.ToList()[i]] );
				} else {
					throw new Exception( "Unable to build localization dictionary; invalid entries" );
				}
			}
		}
		/// <summary>
		/// Decodes a string using the <see cref="Localization"/> dictionary
		/// </summary>
		/// <param name="input">An encoded string to be decoded</param>
		/// <returns>The decoded and localized string</returns>
		public string GetString( string input ) {
			if ( BackingAssets.First().Key.EndsWith( ".csv", StringComparison.InvariantCultureIgnoreCase ) ) {
				return LocalDictionary[input];
			} else {
				return LocalDictionary[MakeHash( input )];
			}
		}
		/// <summary>
		/// Creates a reproducible numeric hash from a string
		/// </summary>
		/// <remarks>
		/// Recent versions of Marvel Future Fight use a dictionary with hashed
		/// strings as keys rather than a flat CSV file for the localization
		/// asset. <see cref="Localization.MakeHash(string)"/> calculates that
		/// hash given the non-localized <paramref name="input"/> string.
		/// </remarks>
		/// <param name="input">The string to be hashed</param>
		/// <returns>The hashed string</returns>
		string MakeHash( string input ) {
			int result = 0;
			char[] textBytes = input.ToCharArray();
			int i = 0;
			int length = textBytes.Length;
			int thisCharIndex = 0;
			if ( i < length - 1 ) {
				int nextCharIndex = 1;
				do {
					byte thisChar = Convert.ToByte( textBytes[thisCharIndex] );
					byte nextChar = Convert.ToByte( textBytes[nextCharIndex] );
					int subresult = ( ( ( result << 5 ) - result ) + Convert.ToInt32( thisChar ) );
					result = ( subresult << 5 ) - subresult + Convert.ToInt32( nextChar );
					i = i + 2;
					thisCharIndex = i;
					nextCharIndex = i + 1;
				}
				while ( i < length - 1 );
			}
			if ( i < length ) {
				result = ( ( result << 5 ) - result ) + Convert.ToInt32( textBytes[thisCharIndex] );
			}
			return result.ToString();
		}
		/// <summary>
		/// Outputs data from this <see cref="Localization"/> in JSON format
		/// </summary>
		/// <param name="file"><see cref="System.IO.StreamWriter"/> stream to
		/// which to write</param>
		/// <param name="tabs">Baseline number of tab characters to insert
		/// before each line of output</param>
		/// <seealso cref="Version.WriteJson(StreamWriter, int)"/>
		public override void WriteJson( StreamWriter file, int tabs = 0 ) {

		}
	}
	/// <summary>
	/// Represents a collection of all playable characters in the
	/// <see cref="Game"/>
	/// </summary>
	/// <remarks>
	/// <para><see cref="Roster"/> is derived from the <see cref="Component"/>
	/// class and includes methods to load and present data about the
	/// <see cref="Game"/>'s characters.</para>
	/// <para>The data model for the <c>Roster</c> is hierarchical; each
	/// <see cref="Character"/> has multiple <see cref="Uniform"/>s, each of
	/// which has different properties associated with different
	/// <see cref="CharacterLevel"/>s. Each type has several properties that
	/// do not vary between descendants of that type. For instance, the
	/// <see cref="Uniform.Gender"/> of a given <c>Character</c> and <c>Uniform</c> is
	/// the same regardless of <c>CharacterLevel</c>.</para>
	/// </remarks>
	public class Roster : Component {
		/// <summary>
		/// Gets or sets a list of the <see cref="Game"/>'s
		/// <see cref="Character"/>s indexed by the <c>Character</c>s'
		/// <see cref="Character.GroupId"/>s.
		/// </summary>
		public Dictionary<string, Character> Characters { get; set; } // by groupId
		/// <summary>
		/// Initializes a new instance of the <see cref="Roster"/> class
		/// </summary>
		/// <seealso cref="Component.Component()"/>
		public Roster() : base() {
			Name = "Roster";
			Characters = new Dictionary<string, Character>();
			AddBackingAsset( "IntHeroDataDictionary" );
			AddDependency( "Localization" );
		}
		/// <summary>
		/// Determines whether the <see cref="Roster"/> has been
		/// loaded.
		/// </summary>
		/// <returns><c>true</c> if the <see cref="Roster"/> already
		/// contains loaded data, <c>false</c> otherwise.</returns>
		/// <seealso cref="Component.IsLoaded()"/>
		public override bool IsLoaded() {
			return Characters.Count != 0;
		}
		/// <summary>
		/// Loads data into this <see cref="Roster"/>
		/// </summary>
		/// <seealso cref="Component.Load()"/>
		public override void Load() {
			base.Load();
			AssetObject asset = BackingAssets["IntHeroDataDictionary"].Properties["values"].Properties["Array"];
			Localization LocalDictionary = (Localization)Dependencies["Localization"];
			List<string> AllHeroIds = new List<string>();
			foreach ( AssetObject entry in asset.Array ) {
				if ( entry.Properties["data"].Properties["isVisible"].String == "1" ) {
					Character character;
					string groupId = entry.Properties["data"].Properties["groupId"].String;
					if ( Characters.ContainsKey( groupId ) ) {
						character = Characters[groupId];
					} else {
						character = new Character();
						character.GroupId = groupId;
						Characters.Add( groupId, character );
					}
					string heroId = entry.Properties["data"].Properties["heroId"].String;
					if ( AllHeroIds.Contains( heroId ) ) {
						throw new Exception( $"HeroID {heroId} has already been used." );
					} else {
						AllHeroIds.Add( heroId );
					}
					CharacterLevel newLevel = new CharacterLevel();
					newLevel.HeroId = heroId;
					newLevel.Rank = Int32.Parse( entry.Properties["data"].Properties["grade"].String );
					newLevel.Tier = Int32.Parse( entry.Properties["data"].Properties["tier"].String );
					string baseId = newLevel.BaseId;
					Uniform uniform;
					if ( character.Uniforms.ContainsKey( baseId ) ) {
						uniform = character.Uniforms[baseId];
					} else {
						uniform = new Uniform();
						character.Uniforms.Add( baseId, uniform );
						uniform.BaseId = baseId;
						uniform.Camps = LocalDictionary.GetString( "HERO_SUBTYPE_" + entry.Properties["data"].Properties["stCamps"].String );
						uniform.CharacterName = LocalDictionary.GetString( $"HERO_{baseId}" );
						uniform.ClassType = LocalDictionary.GetString( "HEROCLASS_" + entry.Properties["data"].Properties["classType"].String );
						uniform.Gender = LocalDictionary.GetString( "HERO_SUBTYPE_" + entry.Properties["data"].Properties["stGender"].String );
						uniform.UniformGroupId = entry.Properties["data"].Properties["uniformGroupId"].String;
						uniform.UniformName = LocalDictionary.GetString( $"HERO_COSTUME_{baseId}" );
						switch ( entry.Properties["data"].Properties["mainAtk"].String ) {
							case "0": uniform.MainAtk = "Physical"; break;
							case "1": uniform.MainAtk = "Energy"; break;
						}
						if ( entry.Properties["data"].Properties["ability_raid"].String != "0" ) {
							uniform.RaidAbility = LocalDictionary.GetString( "HERO_SUBTYPE_" + entry.Properties["data"].Properties["ability_raid"].String );
						}
						foreach ( AssetObject ability in entry.Properties["data"].Properties["abilitys"].Properties["Array"].Array ) {
							if ( ability.Properties["data"].String != "0" ) {
								uniform.Abilities.Add( LocalDictionary.GetString( "HERO_SUBTYPE_" + ability.Properties["data"].String ) );
							}
						}
						if ( entry.Properties["data"].Properties["ability_hidden"].String != "0" ) {
							uniform.Abilities.Add( LocalDictionary.GetString( "HERO_SUBTYPE_" + entry.Properties["data"].Properties["ability_hidden"].String ) );
						}
					}
					uniform.CharacterLevels.Add( heroId, newLevel );
					newLevel.Skills.Add( new Skill( entry.Properties["data"].Properties["leaderSkillId"].String ) );
					foreach ( AssetObject skill in entry.Properties["data"].Properties["skillIds"].Properties["Array"].Array ) {
						Skill newSkill = new Skill( skill.Properties["data"].String );
						newLevel.Skills.Add( newSkill );
					}
					newLevel.Skills.Add( new Skill( entry.Properties["data"].Properties["uniformSkillId"].String ) );
					if ( String.IsNullOrEmpty( character.BaseName ) ) {
						if ( uniform.UniformGroupId == "0" ) {
							character.BaseName = LocalDictionary.GetString( $"HERO_{baseId}" );
						}
					}
					character.Species = LocalDictionary.GetString( "HERO_SUBTYPE_" + entry.Properties["data"].Properties["species"].String );
					character.StartGrade = Int32.Parse( entry.Properties["data"].Properties["startGrade"].String );
					character.GrowType = Int32.Parse( entry.Properties["data"].Properties["growType"].String );
				}
			}
		}
		/// <summary>
		/// Outputs select data from this <see cref="Roster"/> in CSV format
		/// </summary>
		/// <remarks>
		/// <see cref="Roster.WriteCSV(StreamWriter)"/> writes a CSV
		/// containing a flat representation of all playable characters and
		/// different uniforms, and the properties associated with each. It
		/// necessarily contains multiple redundant entries and is intended
		/// for use in spreadsheet applications rather than as a manipulatable
		/// data store.
		/// </remarks>
		/// <param name="file"><see cref="StreamWriter"/> stream to which to
		/// write</param>
		/// <seealso cref="Component.WriteCSV(StreamWriter)"/>
		public override void WriteCSV( StreamWriter file ) {
			char delimiter = '|';
			List<string> header = new List<string> {
				"Group ID",
				"Base ID",
				"BaseName",
				"Character Name",
				"Uniform Name",
				"Uniform Group Id",
				"Primary Attack",
				"Type",
				"Gender",
				"Side",
				"Allies",
				"Max Tier",
				"Growth Type",
				"Abilities",
				"World Boss Ability",
				"Leader Skill",
				"Skill 1",
				"Skill 2",
				"Skill 3",
				"Passive Skill",
				"Skill 4",
				"Skill 5",
				"T2 Passive Skill",
				"T3 Skill",
				"Awakened Skill",
				"Uniform Skill"
			};
			file.WriteLine( String.Join( delimiter, header ) );
			foreach ( Character character in Characters.Values ) {
				foreach ( Uniform uniform in character.Uniforms.Values ) {
					List<string> entries = new List<string> {
						character.GroupId,
						uniform.BaseId,
						character.BaseName,
						uniform.CharacterName,
						uniform.UniformName,
						uniform.UniformGroupId,
						uniform.MainAtk,
						uniform.ClassType,
						uniform.Gender,
						uniform.Camps,
						character.Species,
						character.MaxTier.ToString(),
						character.GrowType.ToString()
					};
					int size = uniform.Abilities.Count;
					string abilities = "";
					for ( int i = 0; i < size; i++ ) {
						abilities += uniform.Abilities[i];
						if ( i < size - 1 ) abilities += ",";
					}
					entries.Add( abilities );
					entries.Add( uniform.RaidAbility );
					for ( int i = 0; i < 11; i++ ) {
						if ( i < uniform.Skills.Count && uniform.Skills[i].SkillId != "0" ) {
							entries.Add( uniform.Skills[i].SkillId );
						}
					}
					foreach ( string entry in entries ) {
						if ( entry.Contains( delimiter ) ) {
							throw new FormatException( "Error: CSV delimiter is included in CSV data" );
						}
					}
					file.WriteLine( String.Join( delimiter, entries ) );
				}
			}
		}
		/// <summary>
		/// Outputs data from this <see cref="Roster"/> in JSON format
		/// </summary>
		/// <param name="file"><see cref="System.IO.StreamWriter"/> stream to
		/// which to write</param>
		/// <param name="tabs">Baseline number of tab characters to insert
		/// before each line of output</param>
		/// <seealso cref="Version.WriteJson(StreamWriter, int)"/>
		public override void WriteJson( StreamWriter file, int tabs = 0 ) {

		}
	}
	/// <summary>
	/// Represents a playable character in the <see cref="Version"/>
	/// </summary>
	/// <seealso cref="Roster"/>
	public class Character {
		/// <summary>
		/// Gets or sets the unique Group ID of the <see cref="Character"/>
		/// </summary>
		/// <remarks>
		/// Associated with the hierarchical model of the <see cref="Roster"/>
		/// are multiple identifiers for the different object levels. A
		/// <see cref="Character"/> equipped with a given <see cref="Uniform"/>
		/// at a specific rank (i.e., number of stars) is uniquely identified
		/// by a <see cref="CharacterLevel.HeroId"/>. Regardless of rank, the
		/// <see cref="Character"/> in that <see cref="Uniform"/> is identified
		/// by the <see cref="Uniform.BaseId"/>, and regardless of
		/// <see cref="Uniform"/> the <see cref="Character"/> is identified by
		/// a <see cref="Character.GroupId"/>. An additional identifier,
		/// <see cref="Uniform.UniformGroupId"/> is only unique among the
		/// <see cref="Uniform"/>s available for a given
		/// <see cref="Character"/>.
		/// </remarks>
		public string GroupId { get; set; }
		/// <summary>
		/// Gets or sets the list of <see cref="Uniform"/>s available for the
		/// <see cref="Character"/>, indexed by <see cref="Uniform.BaseId"/>
		/// </summary>
		public Dictionary<string, Uniform> Uniforms { get; set; } // by BaseId
		/// <summary>
		/// Gets or sets the name of the <see cref="Character"/> in the default
		/// <see cref="Uniform"/>
		/// </summary>
		public string BaseName { get; set; }
		/// <summary>
		/// Gets or sets the growth type of the <see cref="Character"/>
		/// </summary>
		public int GrowType { get; set; }
		/// <summary>
		/// Gets or sets the starting level (grade) of the
		/// <see cref="Character"/>
		/// </summary>
		public int StartGrade { get; set; }
		/// <summary>
		/// Gets or sets the allies (species) of the <see cref="Character"/>
		/// </summary>
		public string Species { get; set; }
		/// <summary>
		/// Gets the maximum tier of the <see cref="Character"/>
		/// </summary>
		/// <remarks>
		/// This is determined automatically by the
		/// <see cref="CharacterLevel"/>s available for each
		/// <see cref="Uniform"/>.
		/// </remarks>
		public int MaxTier {
			get {
				foreach ( Uniform uniform in Uniforms.Values ) {
					foreach ( CharacterLevel level in uniform.CharacterLevels.Values ) {
						if ( level.Tier == 3 ) return 3;
					}
					return 2;
				}
				throw new Exception( $"No uniforms found for character {BaseName} (groupId {GroupId})" );
			}
		}
		/// <summary>
		/// Initializes a new instance of the <see cref="Character"/> class
		/// </summary>
		public Character() {
			Uniforms = new Dictionary<string, Uniform>();
		}
	}
	/// <summary>
	/// Represents a uniform available to a playable <see cref="Character"/>
	/// </summary>
	public class Uniform {
		/// <summary>
		/// Gets or sets the name of the <see cref="Uniform"/>
		/// </summary>
		public string UniformName { get; set; }
		/// <summary>
		/// Gets or sets the name of the <see cref="Character"/> when wearing
		/// this <see cref="Uniform"/>
		/// </summary>
		public string CharacterName { get; set; }
		/// <summary>
		/// Gets or sets this <see cref="Uniform"/>'s Uniform Group ID
		/// </summary>
		/// <remarks>
		/// Note that this is not the <see cref="Character.GroupId"/>.
		/// </remarks>
		public string UniformGroupId { get; set; }
		/// <summary>
		/// Gets or sets the list of <see cref="CharacterLevel"/>s, indexed
		/// by Hero ID
		/// </summary>
		/// <seealso cref="Character.GroupId"/>
		public Dictionary<string, CharacterLevel> CharacterLevels { get; set; } // by heroId
		/// <summary>
		/// Gets or sets the allies (camps) of the <see cref="Character"/> when
		/// wearing this <see cref="Uniform"/>
		/// </summary>
		public string Camps { get; set; }
		/// <summary>
		/// Gets or sets the gender of the <see cref="Character"/> when wearing
		/// this <see cref="Uniform"/>
		/// </summary>
		public string Gender { get; set; }
		/// <summary>
		/// Gets or sets the BaseId of the <see cref="Character"/> when wearing
		/// this <see cref="Uniform"/>
		/// </summary>
		public string BaseId { get; set; }
		/// <summary>
		/// Gets or sets the Class of the <see cref="Character"/> when
		/// wearing this <see cref="Uniform"/>
		/// </summary>
		public string ClassType { get; set; }
		/// <summary>
		/// Gets or sets the Ally ability of the <see cref="Character"/> when
		/// wearing this <see cref="Uniform"/>
		/// </summary>
		public string RaidAbility { get; set; }
		/// <summary>
		/// Gets or sets the main attack type of the <see cref="Character"/>
		/// when wearing this <see cref="Uniform"/>
		/// </summary>
		public string MainAtk { get; set; }
		/// <summary>
		/// Gets or sets the list of abilities of the <see cref="Character"/>
		/// when wearing this <see cref="Uniform"/>
		/// </summary>
		public List<string> Abilities { get; set; }
		/// <summary>
		/// Gets the full list of skills of the <see cref="Character"/> when
		/// wearing this <see cref="Uniform"/>
		/// </summary>
		/// <remarks>
		/// The list of <see cref="Skill"/>s available to the
		/// <see cref="Character"/> increases as the <see cref="Character"/>'s
		/// rank increases; <see cref="Skills"/> is the full list available
		/// at maximum rank.
		/// </remarks>
		public List<Skill> Skills {
			get {
				List<Skill> maxSkillSet = new List<Skill>();
				int maxCount = 0;
				foreach ( CharacterLevel level in CharacterLevels.Values ) {
					int count = 0;
					foreach ( Skill skill in level.Skills ) {
						if ( skill.SkillId != "0" ) {
							count++;
						}
					}
					if ( count > maxCount ) {
						maxSkillSet = level.Skills;
					}
				}
				return maxSkillSet;
			}
		}
		/// <summary>
		/// Initializes a new instance of the <see cref="Uniform"/> class
		/// </summary>
		public Uniform() {
			Abilities = new List<string>();
			CharacterLevels = new Dictionary<string, CharacterLevel>();
		}
	}
	/// <summary>
	/// Represents a <see cref="Character"/> equipped with a particular
	/// <see cref="Uniform"/> at a particular rank
	/// </summary>
	public class CharacterLevel {
		/// <summary>
		/// Gets or sets the hero ID for this <see cref="CharacterLevel"/>
		/// </summary>
		/// <seealso cref="Character.GroupId"/>
		public string HeroId { get; set; }
		/// <summary>
		/// Gets or sets the rank (stars) for this <see cref="CharacterLevel"/>
		/// </summary>
		public int Rank { get; set; }
		/// <summary>
		/// Gets or sets the tier for this <see cref="CharacterLevel"/>
		/// </summary>
		public int Tier { get; set; }
		/// <summary>
		/// Gets or sets the list of skills available at this
		/// <see cref="CharacterLevel"/>
		/// </summary>
		public List<Skill> Skills { get; set; }
		/// <summary>
		/// Gets the <see cref="BaseId"/> for the <see cref="Character"/> /
		/// <see cref="Uniform"/> combination associated with this
		/// <see cref="CharacterLevel"/>
		/// </summary>
		///	<remarks>
		///	There is a many-to-one mapping of
		///	<see cref="CharacterLevel.HeroId"/> to
		///	<see cref="Uniform.BaseId"/> that is calculatable. For a given
		///	<see cref="CharacterLevel"/>, then, properties of the
		///	<see cref="Character"/> and <see cref="Uniform"/> that do not vary
		///	with <see cref="CharacterLevel"/> can be
		///	quickly found.
		///</remarks>
		public string BaseId {
			get {
				Int64 heroIdNumber = Int64.Parse( HeroId );
				Int64 heroIdNumber1 = ( heroIdNumber * 0x51eb851f ) >> 32;
				Int64 heroIdNumber2 = heroIdNumber1 >> 31;
				heroIdNumber1 = heroIdNumber1 >> 5;
				heroIdNumber = heroIdNumber1 + heroIdNumber2;
				heroIdNumber = heroIdNumber * 100 + 1;
				return heroIdNumber.ToString();
			}
		}
		/// <summary>
		/// Initializes a new instance of the <see cref="CharacterLevel"/>
		/// class
		/// </summary>
		public CharacterLevel() {
			Skills = new List<Skill>();
		}
	}
	/// <summary>
	/// Represents a <see cref="Character"/> skill
	/// </summary>
	public class Skill {
		/// <summary>
		/// Gets or sets the skill ID for this <see cref="Skill"/>
		/// </summary>
		public string SkillId { get; set; }
		/// <summary>
		/// Gets or sets the name of this <see cref="Skill"/>
		/// </summary>
		public string Name { get; set; }
		/// <summary>
		/// Initializes a new instance of the <see cref="Skill"/> class
		/// </summary>
		/// <param name="skillId">The skill ID</param>
		public Skill( String skillId ) {
			SkillId = skillId;
		}
	}
	/// <summary>
	/// Represents a <see cref="Player"/> alliance
	/// </summary>
	public class Alliance {
		/// <summary>
		/// Gets or sets the name of the <see cref="Alliance"/>
		/// </summary>
		public string Name { get; set; }
		/// <summary>
		/// Gets or sets the list of <see cref="Player"/>s in the
		/// <see cref="Alliance"/>
		/// </summary>
		public List<Player> Players { get; set; }
		/// <summary>
		/// Gets or sets the <see cref="Player"/> who is the leader of the
		/// <see cref="Alliance"/>
		/// </summary>
		public Player Leader { get; set; }
		/// <summary>
		/// Gets or sets the list of <see cref="Player"/>s who have Class 1
		/// status in the <see cref="Alliance"/>
		/// </summary>
		public List<Player> Class1Players { get; set; }
		/// <summary>
		/// Gets or sets the list of <see cref="Player"/>s who have Class 2
		/// status in the <see cref="Alliance"/>
		/// </summary>
		public List<Player> Class2Players { get; set; }
	}
	/// <summary>
	/// Represents a single item
	/// </summary>
	public class Item {
		/// <summary>
		/// Gets or sets the name of the <see cref="Item"/>
		/// </summary>
		public string Name { get; set; }
	}
	/// <summary>
	/// Represents the Shadowland <see cref="Component"/> of this
	/// <see cref="Version"/> of the <see cref="Game"/>
	/// </summary>
	class Shadowland : Component {
		/// <summary>
		/// Gets or sets the list of <see cref="ShadowlandFloor"/>s upon which
		/// <see cref="Shadowland"/> is based
		/// </summary>
		ShadowlandFloor[] BaseFloors;
		/// <summary>
		/// Initializes a new instance of the <see cref="Shadowland"/> class
		/// </summary>
		public Shadowland() : base() {
			Name = "Shadowland";
			BaseFloors = new ShadowlandFloor[35];
			AddBackingAsset( "text/data/shadowland_floor.csv" );
			AddBackingAsset( "text/data/shadowland_reward.csv" );
		}
		/// <summary>
		/// Loads this <see cref="Shadowland"/> instance
		/// </summary>
		/// <seealso cref="Component.Load()"/>
		public override void Load() {
			base.Load();
			List<AssetObject> shadowlandFloors = BackingAssets["text/data/shadowland_floor.csv"].Properties["m_Script"].Array;
			List<AssetObject> shadowlandRewards = BackingAssets["text/data/shadowland_reward.csv"].Properties["m_Script"].Array;
			for ( int floorNum = 0; floorNum < BaseFloors.Length; floorNum++ ) {
				ShadowlandFloor floor = new ShadowlandFloor();
				floor.FloorNumber = floorNum + 1;
				floor.BaseFloor = floor;
				foreach ( AssetObject value in shadowlandRewards ) {
					if ( value.Properties["REWARD_GROUP"].String == shadowlandFloors[floorNum].Properties["REWARD_GROUP"].String ) {
						List<ShadowlandReward> rewards = new List<ShadowlandReward>();
						for ( int i = 1; i <= 2; i++ ) {
							ShadowlandReward reward = new ShadowlandReward();
							reward.Value = Int32.Parse( value.Properties[$"REWARD_VALUE_{i}"].String );
							reward.Quantity = Int32.Parse( value.Properties[$"REWARD_QTY_{i}"].String );
							reward.Type = Int32.Parse( value.Properties[$"REWARD_TYPE_{i}"].String );
							rewards[i] = reward;
						}
					}
					floor.RewardGroup = Int32.Parse( shadowlandFloors[floorNum].Properties["REWARD_GROUP"].String );
					floor.StageGroup = Int32.Parse( shadowlandFloors[floorNum].Properties["STAGE_GROUP"].String );
					floor.StageSelectCount = Int32.Parse( shadowlandFloors[floorNum].Properties["STAGE_SELECT_COUNT"].String );
					BaseFloors[floorNum] = floor;
				}
			}
		}
		/// <summary>
		/// Represents a single floor of the <see cref="Shadowland"/> component
		/// </summary>
		public class ShadowlandFloor {
			/// <summary>
			/// Gets or sets the number of this <see cref="ShadowlandFloor"/>
			/// </summary>
			public int FloorNumber { get; set; }
			/// <summary>
			/// Gets or sets the floor upo which this
			/// <see cref="ShadowlandFloor"/> is based
			/// </summary>
			public ShadowlandFloor BaseFloor { get; set; }
			/// <summary>
			/// Gets or sets the reward group for this
			/// <see cref="ShadowlandFloor"/>
			/// </summary>
			public int RewardGroup { get; set; }
			/// <summary>
			/// Gets or sets the stage group of this
			/// <see cref="ShadowlandFloor"/>
			/// </summary>
			public int StageGroup { get; set; }
			/// <summary>
			/// Gets or sets the stage select count of this
			/// <see cref="ShadowlandFloor"/>
			/// </summary>
			public int StageSelectCount { get; set; }
		}
		/// <summary>
		/// Represents a reward given for completion of a
		/// <see cref="ShadowlandFloor"/>
		/// </summary>
		public class ShadowlandReward : Reward {
		}
	}
	/// <summary>
	/// Represents a reward given by this <see cref="Version"/> of the
	/// <see cref="Game"/>
	/// </summary>
	public class Reward {
		/// <summary>
		/// Gets or sets the <see cref="Item"/> in this <see cref="Reward"/>
		/// </summary>
		public Item item { get; set; }
		/// <summary>
		/// Gets or sets the quantity of the <see cref="Item"/> in this
		/// <see cref="Reward"/>
		/// </summary>
		public int Quantity { get; set; }
		/// <summary>
		/// Gets or sets the Value of the <see cref="Item"/> in this
		/// <see cref="Reward"/>
		/// </summary>
		public int Value { get; set; }
		/// <summary>
		/// Gets or sets the type of the type of this <see cref="Reward"/>
		/// </summary>
		public int Type { get; set; }
	}
	/// <summary>
	/// Represents the Future Pass <see cref="Component"/> of this
	/// <see cref="Version"/> of the <see cref="Game"/>
	/// </summary>
	/// <remarks>
	/// <see cref="FuturePass"/> is a recurring event in the <see cref="Game"/>
	/// made up of multiple tiers, with a <see cref="Reward"/> at each
	/// <see cref="FuturePassStep"/> from each tier. <see cref="StagePoints"/>
	/// are obtained through regular <see cref="Game"/> activities, and a
	/// given number of points (listed in <see cref="StagePoints"/>) is
	/// needed to reach each <see cref="FuturePassStep"/>.
	/// </remarks>
	public class FuturePass : Component {
		/// <summary>
		/// Gets or sets the different <see cref="FuturePassSeason"/>s
		/// </summary>
		/// <remarks>
		/// Each <see cref="FuturePassSeason"/> is a separate event with
		/// different start and end dates and rewards.
		/// </remarks>
		public List<FuturePassSeason> Seasons { get; set; }
		/// <summary>
		/// Gets or sets the list of <see cref="FuturePassStep"/>s indexed by
		/// step number
		/// </summary>
		public Dictionary<int, FuturePassStep> Steps { get; set; }
		/// <summary>
		/// Gets or sets the number of points needed to reach each
		/// <see cref="FuturePassStep"/>, indexed by step number
		/// </summary>
		public Dictionary<int, int> StagePoints { get; set; }
		/// <summary>
		/// Initializes a new instance of the <see cref="FuturePass"/> class
		/// </summary>
		public FuturePass() : base() {
			Seasons = new List<FuturePassSeason>();
			Steps = new Dictionary<int, FuturePassStep>();
			StagePoints = new Dictionary<int, int>();
			AddBackingAsset( "text/data/future_pass.asset" );
			AddBackingAsset( "text/data/future_pass_step.asset" );
			AddBackingAsset( "text/data/future_pass_reward.asset" );
			AddBackingAsset( "text/data/future_pass_contents.asset" );
		}
		/// <summary>
		/// Load data into this <see cref="FuturePass"/> instance
		/// </summary>
		/// <seealso cref="Component.Load()"/>
		public override void Load() {
			base.Load();
			foreach ( AssetObject seasonAsset in BackingAssets["text/data/future_pass.asset"].Properties["list"].Array ) {
				FuturePassSeason season = new FuturePassSeason();
				season.Load( seasonAsset );
				Seasons.Add( season );
			}
			foreach ( AssetObject stepAsset in BackingAssets["text/data/future_pass_step.asset"].Properties["list"].Array ) {
				FuturePassStep step = new FuturePassStep();
				step.passPoint = Int32.Parse( stepAsset.Properties["data"].Properties["passPoint"].String );
				step.step = Int32.Parse( stepAsset.Properties["data"].Properties["step"].String );
				step.Rewards = new Dictionary<FuturePassType, FuturePassReward>();
				Steps[step.step - 1] = step;
			}
			foreach ( AssetObject rewardAsset in BackingAssets["text/data/future_pass_reward.asset"].Properties["list"].Array ) {
				FuturePassReward reward = new FuturePassReward();
				reward.Load( rewardAsset );
				FuturePassType level = (FuturePassType)Int32.Parse( rewardAsset.Properties["data"].Properties["grade"].String );
				int step = Int32.Parse( rewardAsset.Properties["data"].Properties["step"].String );
				Steps[step - 1].Rewards[level] = reward;
			}
			foreach ( AssetObject stageAsset in BackingAssets["text/data/future_pass_contents.asset"].Properties["list"].Array ) {
				int sceneId = Int32.Parse( stageAsset.Properties["data"].Properties["sceneId"].String );
				int stagePoints = Int32.Parse( stageAsset.Properties["data"].Properties["passPoint"].String );
				StagePoints.Add( sceneId, stagePoints );
			}
		}
		/// <summary>
		/// Represents the <see cref="Reward"/> obtained from completing a
		/// <see cref="FuturePassStep"/>
		/// </summary>
		public class FuturePassReward : Reward {
			// text/data/future_pass_reward.asset->list->Array[x]->data
			/// <summary>
			/// The reward ID for this <see cref="FuturePassReward"/>
			/// </summary>
			private int rewardId;
			/// <summary>
			/// The reward group ID for this <see cref="FuturePassReward"/>
			/// </summary>
			private int rewardGroupId;
			/// <summary>
			/// Load data into this <see cref="FuturePassReward"/> instance
			/// </summary>
			/// <param name="asset">Asset containing
			/// <see cref="FuturePassReward"/> data</param>
			public void Load( AssetObject asset ) {
				this.rewardId = Int32.Parse( asset.Properties["data"].Properties["rewardId"].String );
				this.rewardGroupId = Int32.Parse( asset.Properties["data"].Properties["rewardGroupId"].String );
				this.Type = Int32.Parse( asset.Properties["data"].Properties["rewardType"].String );
				this.Value = Int32.Parse( asset.Properties["data"].Properties["rewardValue"].String );
			}
		}
		/// <summary>
		/// Represents a single <see cref="FuturePass"/> event
		/// </summary>
		public class FuturePassSeason {
			// text/data/future_pass.asset->list->Array[x]->data
			/// <summary>
			/// Gets or sets the end time of this
			/// <see cref="FuturePassSeason"/>
			/// </summary>
			string endTime { get; set; }
			/// <summary>
			/// Gets or sets the start time of this
			/// <see cref="FuturePassSeason"/>
			/// </summary>
			string startTime { get; set; }
			/// <summary>
			/// Gets or sets the reward group ID for this
			/// <see cref="FuturePassSeason"/>
			/// </summary>
			int rewardGroupId { get; set; }
			/// <summary>
			/// Loads data into this instance of <see cref="FuturePassSeason"/>
			/// </summary>
			/// <param name="asset">Asset containing
			/// <see cref="FuturePassSeason"/> data</param>
			public void Load( AssetObject asset ) {
				this.endTime = asset.Properties["data"].Properties["endTime_unused"].String;
				this.startTime = asset.Properties["data"].Properties["startTime_unused"].String;
				this.rewardGroupId = Int32.Parse( asset.Properties["data"].Properties["rewardGroupId"].String );
			}
		}
		/// <summary>
		/// Represents a single set of rewards in this
		/// <see cref="FuturePassSeason"/>
		/// </summary>
		public class FuturePassStep {
			// text/data/future_pass_step.asset->list->Array[x]->data
			/// <summary>
			/// Gets or sets the step number for this
			/// <see cref="FuturePassStep"/>
			/// </summary>
			public int step { get; set; } // 1-50
			/// <summary>
			/// Gets or sets the number of points needed to reach this
			/// <see cref="FuturePassStep"/>
			/// </summary>
			public int passPoint { get; set; }
			/// <summary>
			/// Gets or sets the <see cref="FuturePassReward"/> for reaching
			/// this <see cref="FuturePassStep"/>, indexed by
			/// <see cref="FuturePassType"/>
			/// </summary>
			public Dictionary<FuturePassType, FuturePassReward> Rewards { get; set; }
		}
		/// <summary>
		/// Specifies a tier (type) of rewards within a
		/// <see cref="FuturePassSeason"/>
		/// </summary>
		public enum FuturePassType {
			/// <summary>
			/// The free tier of <see cref="FuturePass"/>
			/// </summary>
			Normal,
			/// <summary>
			/// The middle tier of <see cref="FuturePass"/>
			/// </summary>
			Legendary,
			/// <summary>
			/// The top tier of <see cref="FuturePass"/>
			/// </summary>
			Mythic
		}
	}
	/// <summary>
	/// Represents a skill (ability) available for a <see cref="Character"/>
	/// </summary>
	public class AbilityGroup {
		/// <summary>
		/// Gets or sets the ability group ID for this
		/// <see cref="AbilityGroup"/>
		/// </summary>
		public int groupId { get; set; }
		/// <summary>
		/// Gets or sets the ability ID for this <see cref="AbilityGroup"/>
		/// </summary>
		public int abilityId { get; set; }
		/// <summary>
		/// Gets or sets the time of action for this <see cref="AbilityGroup"/>
		/// </summary>
		public long time { get; set; }
		/// <summary>
		/// Gets or sets the "tick" for this <see cref="AbilityGroup"/>
		/// </summary>
		public long tick { get; set; }
		/// <summary>
		/// Gets or sets whether this <see cref="AbilityGroup"/>'s action
		/// continues when tagging a new <see cref="Character"/>
		/// </summary>
		public bool keepWhenTagging { get; set; }
		/// <summary>
		/// Geets or sets whether this <see cref="AbilityGroup"/>'s effect is
		/// disabled
		/// </summary>
		public bool isEffectDisable { get; set; }
		/// <summary>
		/// Loads data into this <see cref="AbilityGroup"/> instance
		/// </summary>
		/// <param name="assetObject"><see cref="AssetObject"/> containing the
		/// data to be loaded</param>
		public void Load( AssetObject assetObject ) {
			// List<AssetObject> assetObjects = Program.Assets.AssetFiles["text/data/action_ability.asset"].Properties["values"].Array;
			AssetObject abilityGroup = assetObject.Properties["data"];
			this.groupId = Int32.Parse( abilityGroup.Properties["groupId"].String );
			this.abilityId = Int32.Parse( abilityGroup.Properties["abilityId"].String );
			this.time = Int64.Parse( abilityGroup.Properties["time"].String );
			this.tick = Int64.Parse( abilityGroup.Properties["tick"].String );
			this.keepWhenTagging = Boolean.Parse( abilityGroup.Properties["keepWhenTagging"].String );
			this.isEffectDisable = Boolean.Parse( abilityGroup.Properties["isEffectDisable"].String );
		}
	}
	/// <summary>
	/// Represents the settings and data for an individual user
	/// </summary>
	public class Player : Component {
		/// <summary>
		/// Gets or sets the <see cref="Alliance"/> of which the
		/// <see cref="Player"/> is a member
		/// </summary>
		public Alliance alliance { get; set; }
		/// <summary>
		/// Gets or sets the list of <see cref="Character"/>s currently owned
		/// by the <see cref="Player"/>
		/// </summary>
		public List<MyCharacter> MyRoster { get; set; }
	}
	/// <summary>
	/// Represents the settings and data for the current state of a
	/// <see cref="Character"/> owned by a <see cref="Player"/>
	/// </summary>
	public class MyCharacter {
		/// <summary>
		/// Gets or sets the <see cref="Character"/> to whiche the settings in
		/// this <see cref="MyCharacter"/> instance apply
		/// </summary>
		public Character BaseCharacter { get; set; }
	}
}
