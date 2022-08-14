using System;
using System.Collections.Generic;
using System.IO;

namespace Mffer {
	/// <summary>
	/// Represents a single version of a <see cref="Game"/>
	/// </summary>
	/// <remarks>
	/// Different versions of a game may vary in
	/// nearly all data; almost all game data are contained within a
	/// <see cref="Version"/>, including the various <see cref="Component"/>s
	/// and <see cref="AssetBundle"/>s. Methods allow loading data from a
	/// <see cref="DataSource"/>, writing the version's
	/// <see cref="Asset"/> and <see cref="Component"/> data to an existing
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
			// AddComponent( new Catalog() );
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
		/// Get the dynamic object with the given name from a particular data
		/// file
		/// </summary>
		/// <param name="objectName">The name of the object to seek</param>
		/// <param name="dataFile">The name of the data file to search</param>
		/// <returns>The object with the given name from the given data
		/// file</returns>
		/// <exception cref="ArgumentNullException">if the <paramref
		/// name="dataFile"/> name is null</exception>
		/// <exception cref="KeyNotFoundException">if no object with the given
		/// name is found in the data file with the given name</exception>
		dynamic GetData( string objectName, string dataFile ) {
			if ( dataFile is null ) {
				throw new ArgumentNullException( "dataFile" );
			}
			if ( !Data.DataFiles.ContainsKey( dataFile ) ) {
				throw new KeyNotFoundException( $"Unable to find asset file named {dataFile}" );
			}
			GameObject file = Data.DataFiles[dataFile];
			if ( file is AssetBundle bundle ) {
				return bundle.GetAsset( objectName );
			} else if ( file is PreferenceFile && objectName == ( (PreferenceFile)file ).Name ) {
				return (PreferenceFile)file;
			} else {
				throw new KeyNotFoundException( $"Unable to find asset '{objectName}' in '{dataFile}'" );
			}
		}
		/// <summary>
		/// Get the dynamic data object with the given name in any data file
		/// </summary>
		/// <param name="objectName">The name of the object to seek</param>
		/// <returns>The dynamic object with the given name</returns>
		/// <exception cref="KeyNotFoundException">if no object with the given
		/// name is found in any data file</exception>
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
		/// <summary>
		/// Attempt to load the asset with the given object name
		/// </summary>
		/// <param name="objectName">The name of the asset to seek</param>
		/// <param name="data">The <see cref="Asset"/> object to load</param>
		/// <returns><c>true</c> if the asset was found and loaded into
		/// <paramref name="data"/> successfully, <c>false</c>
		/// otherwise</returns>
		bool TryGetAsset( string objectName, out Asset data ) {
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
		/// <see cref="AssetBundle"/>s. This is usually necessary only for
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
		/// <see cref="AssetBundle"/>s named in
		/// <see cref="Component.BackingData"/>. (The assets will be loaded
		/// if they aren't already.) If data has already been loaded into
		/// <paramref name="component"/>, it will not be changed.
		/// </remarks>
		/// <param name="component">The <see cref="Component"/> to load with data</param>
		/// <exception cref="System.ApplicationException">Thrown if a required
		/// <see cref="AssetBundle"/> from <paramref name="component"/>'s
		/// <see cref="Component.BackingData"/> or a required <see cref="Component"/>
		/// from <see cref="Component.Dependencies"/> is not found or cannot be
		/// loaded.</exception>
		public void LoadComponent( Component component ) {
			if ( component.IsLoaded() ) return;

			// Determine which dependencies (other components) need to be loaded first
			Dictionary<string, Component> componentsToLoad = new Dictionary<string, Component>();
			Dictionary<string, Component> newDependencies = component.Dependencies;
			Dictionary<string, Component> toAdd = new();
			while ( newDependencies.Count != 0 ) {
				toAdd.Clear();
				foreach ( string componentName in newDependencies.Keys ) {
					if ( newDependencies[componentName] is null ) {
						if ( Components.ContainsKey( componentName ) && Components[componentName] is not null ) {
							newDependencies[componentName] = Components[componentName];
						} else {
							throw new ApplicationException( $"Unable to load component '{component.Name}': version does not have this component" );
						}
					}
					if ( newDependencies[componentName].Name == component.Name )
						throw new ApplicationException( $"Unable to load component '{component.Name}': cyclic dependency detected." );
					if ( !componentsToLoad.ContainsKey( componentName )
						&& !newDependencies[componentName].IsLoaded() ) {
						componentsToLoad.Add( componentName, newDependencies[componentName] );
						foreach ( string newComponentName in newDependencies[componentName].Dependencies.Keys ) {
							if ( !toAdd.ContainsKey( newComponentName ) )
								toAdd.Add( newComponentName, newDependencies[componentName].Dependencies[newComponentName] );
						}
					}
				}
				newDependencies = toAdd;
			}
			foreach ( Component newComponent in componentsToLoad.Values ) LoadComponent( newComponent );

			// Get the assets that are required for this component
			Dictionary<string, GameObject> loadedBackingData = new();
			foreach ( KeyValuePair<string, GameObject> entry in component.BackingData ) {
				if ( loadedBackingData.ContainsKey( entry.Key ) ) continue;
				Asset asset = null;
				if ( entry.Value is not null ) {
					loadedBackingData.Add( entry.Key, entry.Value );
				} else {
					if ( TryGetAsset( entry.Key, out asset ) ) {
						loadedBackingData.Add( entry.Key, asset );
					} else if ( entry.Key.Contains( "||" ) ) {
						bool found = false;
						foreach ( string possibleAssetName in entry.Key.Split( "||" ) ) {
							string possibleName = possibleAssetName.Trim();
							if ( String.IsNullOrEmpty( possibleName ) ) continue;
							if ( component.BackingData.ContainsKey( possibleName )
								|| loadedBackingData.ContainsKey( possibleName ) ) {
								found = true;
								break;
							}
							if ( TryGetAsset( possibleName, out asset ) ) {
								loadedBackingData.Add( possibleName, asset );
								found = true;
								break;
							}
						}
						if ( !found ) {
							string assetsString = String.Join( ", ", entry.Key.Split( "||" ) );
							throw new ApplicationException( $"Unable to find any of the possible assets ({assetsString}) for component '{component.Name}'" );
						}
					} else if ( entry.Key == "Preferences.xml" || entry.Key == "Preferences" ) {
						loadedBackingData[entry.Key] = GetData( "com.netmarble.mherosgb.v2.playerprefs.xml", "com.netmarble.mherosgb.v2.playerprefs.xml" );
					} else {
						throw new ApplicationException( $"Unable to load asset '{entry.Key}' for component '{component.Name}'" );
					}
				}
			}
			component.BackingData = loadedBackingData;
			component.Load();
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
