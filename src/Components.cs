using System;
using System.Collections.Generic;
using System.IO;

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
	public class Component {
		/// <summary>
		/// Gets or sets the name of the <see cref="Component"/>
		/// </summary>
		public string Name { get; set; }
		/// <summary>
		/// Gets or sets a collection of <see cref="AssetFile"/>s storing
		/// data to be loaded into the <see cref="Component"/>, indexed by
		/// name.
		/// </summary>
		/// <remarks>
		/// Required <see cref="AssetFile"/>s should be named in the keys of
		/// <see cref="BackingAssets"/> when the derived instance
		/// is initialized. When the parent <see cref="Version"/> loads data
		/// into the <see cref="Component"/>, it must first load the named
		/// <see cref="AssetFile"/>s and place them into the associated values of
		/// <see cref="BackingAssets"/>.
		/// </remarks>
		public Dictionary<string, AssetFile> BackingAssets { get; set; }
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
		public Component() {
			BackingAssets = new Dictionary<string, AssetFile>();
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
		/// Adds the name of an asset to the list of
		/// <see cref="BackingAssets"/> for this <see cref="Component"/>
		/// </summary>
		/// <remarks>
		/// No validation or checking of the <paramref name="assetName"/>
		/// parameter is performed at the time of adding the
		/// <see cref="AssetFile"/> name to the <see cref="BackingAssets"/> list.
		/// This is deferred until attempting to load data into the
		/// <see cref="Component"/> as the <see cref="BackingAssets"/> list may
		/// be created before all <see cref="AssetFile"/>s are loaded.
		/// </remarks>
		/// <param name="assetName">The name of the <see cref="AssetFile"/> to
		/// add</param>
		public virtual void AddBackingAsset( string assetName ) {
			if ( !BackingAssets.ContainsKey( assetName ) ) {
				BackingAssets.Add( assetName, null );
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
		public virtual void WriteJson( StreamWriter file, int tabs = 0 ) {
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
		/// <see cref="Component.Load()"/> uses objects loaded into
		/// <see cref="Component.BackingAssets"/> and
		/// <see cref="Component.Dependencies"/> to load data into
		/// <see cref="Component"/>'s other properties. As the
		/// <see cref="Component"/> does not have access to the overall
		/// sets of <see cref="Game.Version.Assets"/> and
		/// <see cref="Game.Version.Components"/>, both
		/// <see cref="BackingAssets"/> and <see cref="Dependencies"/> must be
		/// loaded by an ancestor instance (e.g., via
		/// <see cref="Game.Version.LoadComponent(Component)"/>) before
		/// <see cref="Component.Load()"/> can successfully run.
		/// </remarks>
		/// <exception cref="System.ApplicationException">Thrown if objects
		/// have not been loaded into <see cref="BackingAssets"/> or
		/// <see cref="Dependencies"/> before running
		/// <see cref="Load()"/></exception>
		public virtual void Load() {
			if ( IsLoaded() ) return;
			if ( BackingAssets.Count != 0 ) {
				foreach ( KeyValuePair<string, AssetFile> item in BackingAssets ) {
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
		/// <see cref="Component.Load()"/>). Note that this does not imply that
		/// if <see cref="Component.Load()"/> were run again the properties
		/// would be unchanged. In practice, <see cref="Component.Load()"/>
		/// should only be run after all <see cref="BackingAssets"/>  and
		/// <see cref="Dependencies"/> have been loaded, so the property
		/// loading should be reproducible at any point afterward.
		/// </remarks>
		/// <returns><c>true</c> if the <see cref="Component"/> contains
		/// loaded data, <c>false</c> otherwise</returns>
		public virtual bool IsLoaded() {
			return true;
		}
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
}
