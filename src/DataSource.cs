using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AssetsTools;
using AssetsTools.Dynamic;

namespace Mffer {
	/// <summary>
	/// Provides a store of <see cref="Game"/> data
	/// </summary>
	/// <remarks>
	/// <para>All filesystem interaction (with the possible exception of
	/// validating command-line arguments) should be performed via this class. A
	/// <see cref="DataSource"/> is built from <see cref="DataBundle"/>
	/// objects that are each associated with a given <see cref="Version"/> of
	/// the <see cref="Game"/>. Data from each <see cref="DeviceDirectory"/> are
	/// loaded into the <see cref="DataBundle"/> associated with the same
	/// version when requested by the a <see cref="Game"/> instance.</para>
	/// <para>The <see cref="DataSource"/> class includes these definitions and
	/// methods to build instances of them and access their data.</para>
	/// </remarks>
	public class DataSource : GameObject {
		/// <summary>
		/// Gets or sets the imported data, indexed by version name
		/// </summary>
		public Dictionary<string, DataBundle> VersionData { get; set; }
		/// <summary>
		/// Initializes a new <see cref="DataSource"/> instance
		/// </summary>
		DataSource() : base() {
			VersionData = new Dictionary<string, DataBundle>();
		}
		/// <summary>
		/// Initializes a new <see cref="DataSource"/> instance containing a
		/// directory
		/// </summary>
		/// <remarks>The <paramref name="pathName"/> is validated and examined
		/// for appropriate directories to import.</remarks>
		/// <seealso cref="Add(string)"/>
		/// <param name="pathName">The full path name of a device directory or
		/// parent of device directories</param>
		public DataSource( string pathName ) : this() {
			Add( pathName );
		}
		/// <summary>
		/// Adds a directory to the <see cref="DataSource"/>
		/// </summary>
		/// <remarks>he <paramref name="pathName"/> is validated and examined
		/// for appropriate directories to import. The given <paramref
		/// name="pathName"/> may be a <see cref="DeviceDirectory"/> or a parent
		/// of one or more <see cref="DeviceDirectory"/>s.</remarks>.
		/// <param name="pathName">The full path name of a device directory or
		/// parent of device directories</param>
		public void Add( string pathName ) {
			if ( String.IsNullOrEmpty( pathName ) ) {
				throw new ArgumentNullException( "pathName" );
			}
			if ( !Directory.Exists( pathName ) ) {
				throw new ArgumentException( "Directory does not exist", "pathName" );
			}
			DirectoryInfo directory = new DirectoryInfo( pathName );
			if ( !DeviceDirectory.IsDeviceDirectory( directory ) ) {
				List<DirectoryInfo> subdirs = directory.GetDirectories().ToList();
				List<DirectoryInfo> deviceDirs = new List<DirectoryInfo>();
				foreach ( DirectoryInfo subdir in subdirs ) {
					if ( DeviceDirectory.IsDeviceDirectory( subdir ) ) {
						deviceDirs.Add( subdir );
						Add( subdir.FullName );
					}
				}
				if ( deviceDirs.Count == 0 ) {
					throw new ArgumentException( "Directory is neither a device directory nor a parent of a device directory", "pathName" );
				}
				return;
			}
			DeviceDirectory deviceDirectory = new DeviceDirectory( directory );
			string version = deviceDirectory.VersionName;
			if ( VersionData.ContainsKey( version ) ) {
				throw new FileLoadException( $"Unable to load device directory '{deviceDirectory.FullName}': already loaded assets for version {version}" );
			}
			DataBundle assetBundle = new DataBundle( deviceDirectory );
			VersionData.Add( version, assetBundle );
		}
		/// <summary>
		/// Creates a list of the identified version names
		/// </summary>
		/// <returns>The list of loaded version names</returns>
		public List<string> GetVersionNames() {
			return VersionData.Keys.ToList();
		}
		/// <summary>
		/// Provides the <see cref="DataBundle"/> associated with the given version name
		/// </summary>
		/// <param name="versionName">Name of the version</param>
		/// <returns>The <see cref="DataBundle"/> for version <paramref name="versionName"/></returns>
		public DataBundle GetData( string versionName ) {
			if ( String.IsNullOrEmpty( versionName ) ) {
				throw new ArgumentNullException( "versionName", "The version name must not be empty." );
			}
			if ( !VersionData.ContainsKey( versionName ) ) {
				throw new ArgumentException( $"No data loaded for version {versionName}", "versionName" );
			}
			return VersionData[versionName];
		}
	}
	/// <summary>
	/// Represents a collection of data associated with a single <see cref="Version"/>
	/// </summary>
	/// <remarks>
	/// An <see cref="DataBundle"/> includes all parsed (or parseable) files
	/// from a given <see cref="DeviceDirectory"/>, and therefore all associated
	/// with a given <see cref="Version"/>.
	/// </remarks>
	public class DataBundle : GameObject {
		// TODO: #105 Change AssetBundle to DataBundle to differentiate from (real) AssetBundles
		/// <summary>
		/// Gets or sets the <see cref="DeviceDirectory"/> from which this
		/// <see cref="DataBundle"/> loads its data
		/// </summary>
		DeviceDirectory BackingDirectory { get; set; }
		/// <summary>
		/// Sets or gets a dictionary of files containing <see cref="Version"/>
		/// data, indexed by filename
		/// </summary>
		/// <remarks>
		/// <see cref="DataBundle.DataFiles"/> is a link to the <see
		/// cref="DataBundle.BackingDirectory"/>'s <see
		/// cref="DeviceDirectory.DataFiles"/> property for convenience.
		/// </remarks>
		public Dictionary<string, GameObject> DataFiles {
			get => BackingDirectory.DataFiles;
		}
		/// <summary>
		/// Initializes a new <see cref="DataBundle"/> instance
		/// </summary>
		DataBundle() : base() {

		}
		/// <summary>
		/// Initializes a new <see cref="DataBundle"/> instance based on the
		/// given <see cref="DeviceDirectory"/>
		/// </summary>
		/// <param name="backingDirectory"><see cref="DeviceDirectory"/> from
		/// which this <see cref="DataBundle"/> will load its data</param>
		public DataBundle( DeviceDirectory backingDirectory ) : this() {
			if ( backingDirectory is null ) {
				throw new ArgumentNullException( "backngDirectory" );
			}
			BackingDirectory = backingDirectory;
		}
		/// <summary>
		/// Writes a JSON-formatted representaton of the
		/// <see cref="DataBundle"/> to a <see cref="StreamWriter"/> stream
		/// </summary>
		/// <param name="file">The <see cref="StreamWriter"/> stream to which
		/// to write</param>
		/// <param name="tabs">The number of tabs with which to prepend each
		/// line</param>
		/// <seealso cref="Game.Version.WriteJson(StreamWriter, int)"/>
		public override void WriteJson( StreamWriter file, int tabs = 0 ) {
			List<string> Keys = DataFiles.Keys.ToList<string>();
			Keys.Sort();
			int counter = 0;
			foreach ( string key in Keys ) {
				DataFiles[key].WriteJson( file, tabs );
				if ( counter < Keys.Count() - 1 ) {
					file.Write( "," );
				}
				file.WriteLine();
				counter++;
			}
		}
		/// <summary>
		/// Loads all available data into the <see cref="DataFiles"/>
		/// </summary>
		public override void LoadAll() {
			BackingDirectory.LoadAll();
		}
	}
	/// <summary>
	/// Represents a stored AssetBundle file from the game
	/// </summary>
	/// <remarks>
	/// AssetBundle files contain multiple <see cref="Asset"/>s. The <see
	/// cref="AssetFile"/> class includes methods for loading the data from the
	/// filesystem and outputting the data in JSON format.
	/// </remarks>
	public class AssetFile : GameObject {
		/// <summary>
		/// Gets or sets the <see cref="AssetsFile"/> instance containing
		/// data for this <see cref="AssetFile"/>
		/// </summary>
		public AssetsFile DynamicFile { get; set; }
		/// <summary>
		/// Gets or sets the dictionary of assets, indexed by asset name
		/// </summary>
		Dictionary<string, Asset> Assets { get; set; }
		/// <summary>
		/// Initializes a new <see cref="AssetFile"/> instance
		/// </summary>
		AssetFile() : base() {
			Assets = new Dictionary<string, Asset>();
		}
		/// <summary>
		/// Initializes a new <see cref="AssetFile"/> instance based on the
		/// given <paramref name="file"/>
		/// </summary>
		/// <param name="file"><see cref="FileInfo"/> instance from which to
		/// load data into this <see cref="AssetFile"/></param>
		public AssetFile( FileInfo file ) : this() {
			if ( file is null ) {
				throw new ArgumentNullException( "file" );
			}
			if ( !file.Exists ) {
				throw new FileNotFoundException( "Unable to access file", file.FullName );
			}
			AssetFileTest.Test( file.FullName );
			DynamicFile = AssetBundleFile.LoadFromFile( file.FullName ).Files[0].ToAssetsFile(); ;
			LoadManifest();
		}
		/// <summary>
		/// Initializes the information in the <see cref="Assets"/> catalog with
		/// data from the <see cref="File"/>
		/// </summary>
		void LoadManifest() {
			if ( DynamicFile is null ) {
				throw new Exception( "No asset file is loaded." );
			}
			List<AssetsFile.ObjectType> manifests = DynamicFile.ObjectsWithClass( ClassIDType.AssetBundle ).ToList();
			if ( manifests.Count == 0 ) {
				throw new FileLoadException( $"Unable to identify AssetBundle manifest" );
			}
			if ( manifests.Count > 1 ) {
				throw new FileLoadException( $"Multiple AssetBundle manifests found" );
			}
			dynamic manifest = GetDynamicAsset( manifests[0] ).AsDynamic();
			foreach ( string assetName in manifest.m_Container.Keys ) {
				long assetPathId = manifest.m_Container[assetName].asset.m_PathID;
				Asset asset = new Asset( assetPathId );
				Assets.Add( assetName, asset );
			}
		}
		/// <summary>
		/// Retrieves the <see cref="DynamicAsset"/> of the given name
		/// </summary>
		/// <param name="assetName">Name of the asset to obtain</param>
		/// <returns>The asset named <paramref name="assetName"/></returns>
		public dynamic GetRawAsset( string assetName ) {
			Asset asset = GetAsset( assetName );
			return asset.RawAsset.AsDynamic();
		}
		/// <summary>
		/// Retrieves the <see cref="Asset"/> of the given name
		/// </summary>
		/// <param name="assetName">Name of the <see cref="Asset"/> to obtain</param>
		/// <returns>The asset named <paramref name="assetName"/></returns>
		public Asset GetAsset( string assetName ) {
			string foundName = null;
			if ( Assets.ContainsKey( assetName ) ) {
				foundName = assetName;
			} else if ( Assets.ContainsKey( assetName + ".asset" ) ) {
				foundName = assetName + ".asset";
			}
			if ( foundName is null ) {
				throw new KeyNotFoundException( $"Unable to find asset '{assetName}'" );
			}
			if ( Assets[foundName].RawAsset is null ) {
				LoadAsset( foundName );
			}
			return Assets[foundName];
		}
		/// <summary>
		/// Load the <paramref name="asset"/> with data from the given sources
		/// </summary>
		/// <param name="asset"><see cref="Asset"/> to load with data</param>
		/// <param name="rawAsset"><see cref="DynamicAsset"/> containing data to load into <paramref name="asset"/></param>
		/// <param name="type"><see cref="SerializedType"/> defining the structure of the <see cref="DynamicAsset"/></param>
		private void LoadAsset( Asset asset, DynamicAsset rawAsset, SerializedType type ) {
			if ( rawAsset.TypeName == "MonoBehaviour" ) {
				asset.Load( rawAsset, GetClassName( rawAsset ), type );
			} else {
				asset.Load( rawAsset, null, type );
			}
		}
		/// <summary>
		/// Loads data into the <see cref="Asset"/> with the given name
		/// </summary>
		/// <param name="assetName">Name of the <see cref="Asset"/> to load with
		/// data</param>
		void LoadAsset( string assetName ) {
			if ( String.IsNullOrEmpty( assetName ) ) {
				throw new ArgumentNullException( "assetName" );
			}
			if ( !Assets.ContainsKey( assetName ) ) {
				throw new KeyNotFoundException( $"Unable to find asset named '{assetName}'" );
			}
			if ( Assets[assetName] is null ) {
				throw new ApplicationException( $"Asset {assetName} was not properly initialized" );
			}
			foreach ( AssetsFile.ObjectType assetData in DynamicFile.Objects ) {
				if ( assetData.PathID == Assets[assetName].PathID ) {
					SerializedType type = DynamicFile.Types[assetData.TypeID];
					DynamicAsset dynamicAsset = GetDynamicAsset( assetData );
					LoadAsset( Assets[assetName], dynamicAsset, type );
					return;
				}
			}
			throw new ApplicationException( $"Unable to find asset data for '{assetName}'" );
		}
		/// <summary>
		/// Obtain a dynamic representation of the asset data
		/// </summary>
		/// <remarks>
		/// While <see
		/// cref="AssetsTools.Dynamic.Extensions.ToDynamicAsset"/><c>()</c> is
		/// the obvious way to do this, that method can make errors due to
		/// caching when <see cref="DynamicAsset"/>s are created from more than
		/// one <see cref="AssetsFile"/>. This somewhat more convoluted method
		/// for MonoBehaviours (which appears to be the only object in which the
		/// error occurs) is thus used instead.
		/// </remarks>
		/// <param name="assetData"></param>
		/// <returns></returns>
		public DynamicAsset GetDynamicAsset( AssetsFile.ObjectType assetData ) {
			DynamicAsset dynamicAsset = null;
			if ( DynamicFile.Types[assetData.TypeID].ClassID == (int)ClassIDType.MonoBehaviour ) {
				Func<UnityBinaryReader, DynamicAsset> deserialize = DynamicAsset.GenDeserializer( DynamicFile.Types[assetData.TypeID].TypeTree.Nodes );
				dynamicAsset = deserialize( new UnityBinaryReader( assetData.Data ) );
			} else {
				dynamicAsset = assetData.ToDynamicAsset();
			}
			return dynamicAsset;
		}
		/// <summary>
		/// Loads all available data from <see cref="File"/> into the
		/// individual <see cref="Assets"/>
		/// </summary>
		public override void LoadAll() {
			if ( Assets.Count == 0 ) {
				LoadManifest();
			}
			Dictionary<long, Asset> pathIDIndex = new Dictionary<long, Asset>();
			foreach ( KeyValuePair<string, Asset> entry in Assets ) {
				pathIDIndex.Add( entry.Value.PathID, entry.Value );
			}
			foreach ( AssetsFile.ObjectType assetData in DynamicFile.Objects ) {
				DynamicAsset dynamicAsset = GetDynamicAsset( assetData );
				if ( pathIDIndex.ContainsKey( assetData.PathID ) ) {
					LoadAsset( pathIDIndex[assetData.PathID], dynamicAsset, DynamicFile.Types[assetData.TypeID] );
				} else {
					if ( dynamicAsset.TypeName != "MonoScript" && dynamicAsset.TypeName != "AssetBundle" ) {
						throw new InvalidDataException( "Path ID of object not found in manifest" );
					}
				}
			}
		}
		/// <summary>
		/// Determines the class name of a MonoBehaviour
		/// </summary>
		/// <param name="dynamicAsset">MonoBehaviour asset to identify</param>
		/// <returns>The name of the MonoBehaviour's class</returns>
		string GetClassName( DynamicAsset dynamicAsset ) {
			if ( dynamicAsset.TypeName != "MonoBehaviour" ) {
				throw new ArgumentException( "Not a MonoBehaviour asset" );
			}
			long ClassPathID = dynamicAsset.AsDynamic().m_Script.m_PathID;
			foreach ( AssetsFile.ObjectType monoScript in
				DynamicFile.ObjectsWithClass( ClassIDType.MonoScript ) ) {
				if ( monoScript.PathID == ClassPathID ) {
					return GetDynamicAsset( monoScript ).AsDynamic().m_ClassName;
				}
			}
			throw new InvalidDataException( "Unable to determine class name for MonoBehaviour" );
		}
		/// <summary>
		/// Writes the data from the <see cref="AssetFile"/> to a
		/// <see cref="StreamWriter"/> stream
		/// </summary>
		/// <param name="file">The name of the <see cref="StreamWriter"/>
		/// stream to which to write</param>
		/// <param name="tabs">The number of tab characters to prepend to each
		/// line</param>
		/// <seealso cref="Game.Version.WriteJson(StreamWriter, int)"/>
		public override void WriteJson( StreamWriter file, int tabs = 0 ) {
			for ( int i = 0; i < tabs; i++ ) {
				file.Write( "\t" );
			}
			file.WriteLine( "{" );
			for ( int i = 0; i < tabs + 1; i++ ) {
				file.Write( "\t" );
			}
			file.WriteLine( "\"Assets\" : {" );
			for ( int j = 0; j < Assets.Keys.Count; j++ ) {
				string assetName = Assets.Keys.ToList()[j];
				for ( int i = 0; i < tabs + 2; i++ ) {
					file.Write( "\t" );
				}
				file.WriteLine( $"\"{assetName}\" : " );
				Assets[assetName].WriteJson( file, tabs + 3 );
				if ( j < Assets.Keys.Count - 1 ) {
					file.Write( "," );
				}
				file.WriteLine();
			}
			for ( int i = 0; i < tabs + 1; i++ ) {
				file.Write( "\t" );
			}
			file.WriteLine( "}," );
			for ( int i = 0; i < tabs + 1; i++ ) {
				file.Write( "\t" );
			}
			file.WriteLine( "\"Value\" : " );
			base.WriteJson( file, tabs + 2 );
			file.WriteLine();
			for ( int i = 0; i < tabs + 1; i++ ) {
				file.Write( "\t" );
			}
			file.WriteLine( "}" );
			for ( int i = 0; i < tabs; i++ ) {
				file.Write( "\t" );
			}
			file.Write( "}" );
		}
	}
	/// <summary>
	/// Represents a single asset used by Marvel Future Fight
	/// </summary>
	public class Asset : AssetObject {
		/// <summary>
		/// Gets or sets the <see cref="DynamicAsset"/> containing unparsed data
		/// for the <see cref="Asset"/>
		/// </summary>
		public DynamicAsset RawAsset { get; set; }
		/// <summary>
		/// Gets or sets the <see cref="Asset"/>'s path ID
		/// </summary>
		public long PathID { get; set; }
		/// <summary>
		/// Gets or sets the class name of this <see cref="Asset"/>
		/// </summary>
		/// <remarks>
		/// <see cref="ClassName"/> may be null if this asset is not a
		/// MonoBehaviour
		/// </remarks>
		public string ClassName { get; set; }
		/// <summary>
		/// Gets or sets the <see cref="SerializedType"/> of this <see
		/// cref="Asset"/>
		/// </summary>
		/// <remarks>
		/// <see cref="ClassType"/> may be a new (empty) <see
		/// cref="SerializedType"/> if this asset is not a MonoBehavior
		/// </remarks>
		public SerializedType ClassType { get; set; }
		/// <summary>
		/// Gets or sets the name of this <see cref="Asset"/>
		/// </summary>
		public string Name { get; set; }
		Asset() : base() {

		}
		/// <summary>
		/// Initialize a new <see cref="Asset"/> instance with the given
		/// <paramref name="pathID"/>
		/// </summary>
		/// <param name="pathID">Path ID of the asset</param>
		public Asset( long pathID ) : this() {
			PathID = pathID;
		}
		/// <summary>
		/// Loads the <see cref="Asset"/> with data from the given <see
		/// cref="DynamicAsset"/>
		/// </summary>
		/// <param name="dynamicAsset"><see cref="DynamicAsset"/> from which to
		/// load data</param>
		/// <param name="className">Optional name of the class this asset
		/// represents</param>
		/// <param name="type">Optional type structure of the class this asset
		/// represents</param>
		public void Load( DynamicAsset dynamicAsset, string className = null, SerializedType type = new SerializedType() ) {
			RawAsset = dynamicAsset;
			if ( !String.IsNullOrEmpty( className ) ) ClassName = className;
			if ( type.ScriptID is not null ) ClassType = type;
			dynamic asset = dynamicAsset.AsDynamic();
			if ( asset.HasMember( "m_Name" ) ) {
				if ( asset.m_Name is string ) {
					Name = asset.m_Name;
				} else {
					throw new NotImplementedException( $"Don't know what to do with m_Name of type {asset.m_Name.GetType()}" );
				}
			} else {
				throw new NotImplementedException( "Unable to find name of asset" );
			}
			Value = dynamicAsset.ToGameObject( type.TypeTree.Nodes ).Value;
		}
		/// <summary>
		/// Load this <see cref="Asset"/> from the given <see cref="DynamicAsset"/>
		/// using the given <see cref="SerializedType"/> definition
		/// </summary>
		/// <param name="dynamicAsset"><see cref="DynamicAsset"/> containing data to load</param>
		/// <param name="type"><see cref="SerializedType"/> containing information
		/// about the structure of <paramref name="dynamicAsset"/></param>
		/// <remarks>This method is not yet implemented.</remarks>
		public void Load( DynamicAsset dynamicAsset, SerializedType type ) {
			throw new NotImplementedException();
		}
		/// <summary>
		/// Writes the data from the <see cref="Asset"/> to a <see
		/// cref="StreamWriter"/> stream
		/// </summary>
		/// <param name="file">The name of the <see cref="StreamWriter"/> stream
		/// to which to write</param>
		/// <param name="tabs">The number of tab characters to prepend to each
		/// line</param>
		/// <seealso cref="Game.Version.WriteJson(StreamWriter, int)"/>
		public override void WriteJson( StreamWriter file, int tabs ) {
			throw new NotImplementedException();
		}
	}
	/// <summary>
	/// Represents a single object within an <see cref="Asset"/>
	/// </summary>
	/// <remarks>
	/// Each <see cref="AssetFile"/> represents data loaded from a file. The
	/// included members are represented as <see cref="Asset"/>s, and nested
	/// objects within this are <see cref="AssetObject"/>s. The class includes
	/// methods for parsing the objects and writing the objects to a JSON
	/// stream.
	/// </remarks>
	public class AssetObject : GameObject {
		/// <summary>
		/// Initializes a new instance of the <see cref="AssetObject"/> class
		/// </summary>
		public AssetObject() : base() {
		}
		/// <summary>
		/// Initializes a new instance of the <see cref="AssetObject"/> class
		/// and loads data from a string
		/// </summary>
		/// <param name="assetString">String containing data to parse and load</param>
		public AssetObject( string assetString ) : this() {
			Value = DecodeString( assetString );
		}
		/// <summary>
		/// Initializes a new instance of the <see cref="AssetObject"/> class
		/// and loads data from an imported object
		/// </summary>
		/// <param name="dynamicAsset"></param>
		public AssetObject( DynamicAsset dynamicAsset ) : this() {
			throw new NotImplementedException();
		}
		/// <summary>
		/// Initializes a new instance of the <see  cref="AssetObject"/> class
		/// and loads data from an imported array of objects
		/// </summary>
		/// <param name="assetArray"></param>
		public AssetObject( DynamicAssetArray assetArray ) : this() {
			throw new NotImplementedException();
		}
	}
}
