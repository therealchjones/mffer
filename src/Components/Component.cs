using System;
using System.Collections.Generic;
using System.IO;
using AssetsTools.Dynamic;

namespace Mffer {
	/// <summary>
	/// Represents a generic part of a game's content
	/// </summary>
	/// <remarks>
	/// Major game content is represented by derivatives of the
	/// <see cref="Component"/> class. This class includes the base properties
	/// and methods applicable to all derivatives, including lists of the
	/// <see cref="AssetFile"/>s and other <see cref="Component"/>s required
	/// for loading data into the instance or evaluating or reporting the data.
	/// </remarks>
	public class Component : GameObject {
		/// <summary>
		/// Gets or sets the name of the <see cref="Component"/>
		/// </summary>
		public string Name { get; set; }
		/// <summary>
		/// Gets or sets a collection of files storing data to be loaded into
		/// the <see cref="Component"/>, indexed by name.
		/// </summary>
		/// <remarks>
		/// Required files should be named in the keys of <see
		/// cref="BackingData"/> when the derived instance is initialized. When
		/// the parent <see cref="Version"/> loads data into the <see
		/// cref="Component"/>, it must first load the named files and place
		/// them into the associated values of <see cref="BackingData"/>.
		/// </remarks>
		public Dictionary<string, GameObject> BackingData { get; set; }
		/// <summary>
		/// Gets or sets a collection of <see cref="Component"/>s referred to
		/// by this <see cref="Component"/>, indexed by name.
		/// </summary>
		/// <remarks>
		/// Required <see cref="Component"/>s should be named in the keys of
		/// <see cref="Component.Dependencies"/> when the derived instance
		/// is initialized. When the parent <see cref="Version"/> loads data
		/// into this <see cref="Component"/>, it must first load the named
		/// <see cref="Component"/>s and place them into the associated values of
		/// <see cref="Dependencies"/>.
		/// </remarks>
		public Dictionary<string, Component> Dependencies { get; set; }
		/// <summary>
		/// Initializes a new instance of the <see cref="Component"/> class
		/// </summary>
		public Component() {
			BackingData = new Dictionary<string, GameObject>();
			Dependencies = new Dictionary<string, Component>();
		}
		/// <summary>
		/// Initializes a new instance of the <see cref="Component"/> class
		/// with name <paramref name="componentName"/>
		/// </summary>
		/// <param name="componentName">The name of the
		/// <see cref="Component"/></param>
		public Component( string componentName ) : this() {
			Name = componentName;
		}
		/// <summary>
		/// Adds the name of an asset to the list of <see cref="BackingData"/>
		/// for this <see cref="Component"/>
		/// </summary>
		/// <remarks>
		/// No validation or checking of the <paramref name="assetName"/>
		/// parameter is performed at the time of adding the <see
		/// cref="AssetFile"/> name to the <see cref="BackingData"/> list. This
		/// is deferred until attempting to load data into the <see
		/// cref="Component"/> as the <see cref="BackingData"/> list may be
		/// created before all <see cref="AssetFile"/>s are loaded.
		/// </remarks>
		/// <param name="assetName">The name of the <see cref="AssetFile"/> to
		/// add</param>
		public virtual void AddBackingData( string assetName ) {
			if ( !BackingData.ContainsKey( assetName ) ) {
				BackingData.Add( assetName, null );
			}
		}
		/// <summary>
		/// Adds the name of a <see cref="Component"/> to the list of
		/// <see cref="Dependencies"/> for this <see cref="Component"/>
		/// </summary>
		/// <remarks>
		/// No validation or checking of the <paramref name="componentName"/>
		/// parameter is performed at the time of adding the
		/// <see cref="Component"/> name to the <see cref="Dependencies"/> list.
		/// This is deferred until attempting to load data into the
		/// <see cref="Component"/> as the <see cref="Dependencies"/> list may
		/// be created before all <see cref="Component"/>s are loaded.
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
		/// <param name="file"><see cref="System.IO.StreamWriter"/> stream to
		/// which to write</param>
		/// <param name="tabs">Baseline number of tab characters to insert
		/// before each line of output</param>
		/// <seealso cref="Game.Version.WriteJson(StreamWriter, int)"/>
		public override void WriteJson( StreamWriter file, int tabs = 0 ) {
		}
		/// <summary>
		/// Outputs select data from this <see cref="Component"/> in CSV format
		/// </summary>
		/// <remarks>
		/// <see cref="WriteCSV( StreamWriter )"/> writes data from the
		/// <see cref="Component"/> to <paramref name="file"/> in a format
		/// useful for importing into a spreadsheet.
		/// <see cref="WriteCSV( StreamWriter )"/> is not intended to
		/// losslessly output all of the <see cref="Component"/>'s
		/// data, but rather to present select data in a usable format for
		/// further processing. For the former purpose, use
		/// <see cref="WriteJson(StreamWriter,int)"/>.
		/// </remarks>
		/// <param name="file">The <see cref="StreamWriter"/> stream to which
		/// to write</param>
		public virtual void WriteCSV( StreamWriter file ) {
		}
		/// <summary>
		/// Loads data into this <see cref="Component"/>
		/// </summary>
		/// <remarks>
		/// <para><see cref="Component.Load()"/> uses objects loaded into <see
		/// cref="Component.BackingData"/> and <see
		/// cref="Component.Dependencies"/> to load data into <see
		/// cref="Component"/>'s other properties. As the <see
		/// cref="Component"/> does not have access to the overall sets of <see
		/// cref="Game.Version.Data"/> and <see
		/// cref="Game.Version.Components"/>, both <see cref="BackingData"/> and
		/// <see cref="Dependencies"/> must be loaded by an ancestor instance
		/// (e.g., via <see cref="Game.Version.LoadComponent(Component)"/>)
		/// before <see cref="Component.Load()"/> can successfully run.</para>
		/// <para>Note that the base <see cref="Component.Load()"/> only checks
		/// to ensure that backing data and dependencies are loaded; individual
		/// derived classes must implement any storing of that data in other
		/// members.</para>
		/// </remarks>
		/// <exception cref="System.Exception">Thrown if objects have not been
		/// loaded into <see cref="BackingData"/> or <see cref="Dependencies"/>
		/// before running <see cref="Load()"/></exception>
		public virtual void Load() {
			if ( IsLoaded() ) return;
			if ( BackingData.Count != 0 ) {
				foreach ( KeyValuePair<string, GameObject> item in BackingData ) {
					if ( String.IsNullOrWhiteSpace( item.Key ) ) {
						BackingData.Remove( item.Key );
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
		/// <see cref="Component.Load()"/>). Note that this does not imply that
		/// if <see cref="Component.Load()"/> were run again the properties
		/// would be unchanged. In practice, <see cref="Component.Load()"/>
		/// should only be run after all <see cref="BackingData"/>  and
		/// <see cref="Dependencies"/> have been loaded, so the property
		/// loading should be reproducible at any point afterward.
		/// </remarks>
		/// <returns><c>true</c> if the <see cref="Component"/> contains
		/// loaded data, <c>false</c> otherwise</returns>
		public virtual bool IsLoaded() {
			return true;
		}
	}
}
