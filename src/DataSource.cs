using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using AssetsTools;
using AssetsTools.Dynamic;

namespace Mffer {
	/// <summary>
	/// Provides a store of <see cref="Game"/> data
	/// </summary>
	/// <remarks>
	/// <para>All filesystem interaction from the <see cref="Game"/> class
	/// should be performed via this class. A <see cref="DataSource"/> is built
	/// from <see cref="DeviceDirectory"/> objects that are each associated with
	/// a given <see cref="Version"/> of the <see cref="Game"/>. Data from the
	/// <see cref="DeviceDirectory"/> are loaded into the <see
	/// cref="AssetBundle"/> associated with the same version when requested by
	/// the a <see cref="Game"/> instance.</para>
	/// <para>The <see cref="DataSource"/> class includes these definitions and
	/// methods to build instances of them and access their data.</para>
	/// </remarks>
	public class DataSource : GameObject {
		/// <summary>
		/// Gets or sets the parsed assets, indexed by version name
		/// </summary>
		public Dictionary<string, AssetBundle> Assets { get; set; }
		/// <summary>
		/// Initializes a new <see cref="DataSource"/> instance
		/// </summary>
		DataSource() : base() {
			Assets = new Dictionary<string, AssetBundle>();
		}
		/// <summary>
		/// Initializes a new <see cref="DataSource"/> instance containing a
		/// directory
		/// </summary>
		/// <remarks>The <paramref name="pathName"/> is validated and added to
		/// the list of directories</remarks>
		/// <param name="pathName">The full path name of a device directory</param>
		public DataSource( string pathName ) : this() {
			Add( pathName );
		}
		/// <summary>
		/// Adds a device directory to the <see cref="DataSource"/>
		/// </summary>
		/// <remarks>The <paramref name="pathName"/> is validated before
		/// adding.</remarks>
		/// <param name="pathName">The full path name of a device directory</param>
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
			if ( Assets.ContainsKey( version ) ) {
				throw new FileLoadException( $"Unable to load device directory '{deviceDirectory.FullName};: already loaded assets for version {version}" );
			}
			AssetBundle assetBundle = new AssetBundle( deviceDirectory );
			Assets.Add( version, assetBundle );
		}
		/// <summary>
		/// Creates a list of the identified version names
		/// </summary>
		/// <returns>The list of identified version names</returns>
		public List<string> GetVersionNames() {
			return Assets.Keys.ToList();
		}
		/// <summary>
		/// Provides the <see cref="AssetBundle"/> associated with the given version name
		/// </summary>
		/// <param name="versionName">Name of the version</param>
		/// <returns>The <see cref="AssetBundle"/> for version <paramref name="versionName"/></returns>
		public AssetBundle GetAssets( string versionName ) {
			if ( String.IsNullOrEmpty( versionName ) ) {
				throw new ArgumentNullException( "versionName", "The version name must not be empty." );
			}
			if ( !Assets.ContainsKey( versionName ) ) {
				throw new ArgumentException( $"There is no asset for version {versionName}.", "versionName" );
			}
			return Assets[versionName];
		}
	}
	/// <summary>
	/// Represents a collection of <see cref="AssetFile"/>s
	/// </summary>
	/// <remarks>
	/// An <see cref="AssetBundle"/> includes all parsed (or parseable) files
	/// from a given <see cref="DeviceDirectory"/>, and therefore all associated
	/// with a given <see cref="Version"/>.
	/// </remarks>
	public class AssetBundle : GameObject {
		/// <summary>
		/// Gets or sets the <see cref="DeviceDirectory"/> from which this
		/// <see cref="AssetBundle"/> loads its data
		/// </summary>
		DeviceDirectory BackingDirectory { get; set; }
		/// <summary>
		/// Sets or gets a dictionary of <see cref="AssetFile"/>s indexed by
		/// <see cref="AssetFile"/> name
		/// </summary>
		/// <remarks>
		/// <see cref="AssetBundle.AssetFiles"/> is a link to the
		/// <see cref="AssetBundle.BackingDirectory"/>'s
		/// <see cref="DeviceDirectory.AssetFiles"/> property for
		/// convenience.
		/// </remarks>
		public Dictionary<string, GameObject> AssetFiles {
			get => BackingDirectory.AssetFiles;
		}
		/// <summary>
		/// Initializes a new <see cref="AssetBundle"/> instance
		/// </summary>
		AssetBundle() : base() {

		}
		/// <summary>
		/// initializes a new <see cref="AssetBundle"/> instance based on the
		/// given <see cref="DeviceDirectory"/>
		/// </summary>
		/// <param name="backingDirectory"><see cref="DeviceDirectory"/> from
		/// which this <see cref="AssetBundle"/> will load its data</param>
		public AssetBundle( DeviceDirectory backingDirectory ) : this() {
			if ( backingDirectory is null ) {
				throw new ArgumentNullException( "backngDirectory" );
			}
			BackingDirectory = backingDirectory;
		}
		/// <summary>
		/// Writes a JSON-formatted representaton of the
		/// <see cref="AssetBundle"/> to a <see cref="StreamWriter"/> stream
		/// </summary>
		/// <param name="file">The <see cref="StreamWriter"/> stream to which
		/// to write</param>
		/// <param name="tabs">The number of tabs with which to prepend each
		/// line</param>
		/// <seealso cref="Game.Version.WriteJson(StreamWriter, int)"/>
		public override void WriteJson( StreamWriter file, int tabs = 0 ) {
			List<string> Keys = AssetFiles.Keys.ToList<string>();
			Keys.Sort();
			int counter = 0;
			foreach ( string key in Keys ) {
				AssetFiles[key].WriteJson( file, tabs );
				if ( counter < Keys.Count() - 1 ) {
					file.Write( "," );
				}
				file.WriteLine();
				counter++;
			}
		}
		/// <summary>
		/// Loads all asset data into the <see cref="AssetFiles"/>
		/// </summary>
		public void LoadAll() {
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
		/// Gets or sets the <see cref="FileInfo"/> instance containing
		/// data for this <see cref="AssetFile"/>
		/// </summary>
		FileInfo File { get; set; }
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
			File = file;
			LoadManifest();
		}
		/// <summary>
		/// Initializes the information in the <see cref="Assets"/> catalog with
		/// data from the <see cref="File"/>
		/// </summary>
		void LoadManifest() {
			if ( File is null ) {
				throw new Exception( "No asset file is loaded." );
			}
			AssetsFile assetsFile = AssetBundleFile.LoadFromFile( File.FullName ).Files[0].ToAssetsFile();
			List<AssetsFile.ObjectType> manifests = assetsFile.ObjectsWithClass( ClassIDType.AssetBundle ).ToList();
			if ( manifests.Count == 0 ) {
				throw new FileLoadException( $"Unable to identify AssetBundle manifest in {File.FullName}" );
			}
			if ( manifests.Count > 1 ) {
				throw new FileLoadException( $"Multiple AssetBundle manifests found in {File.FullName}" );
			}
			dynamic manifest = manifests[0].ToDynamicAsset().AsDynamic();
			foreach ( string assetName in manifest.m_Container.Keys ) {
				long assetPathId = manifest.m_Container[assetName].asset.m_PathID;
				Asset asset = new Asset( assetPathId );
				Assets.Add( assetName, asset );
			}
		}
		/// <summary>
		/// Loads all available data from <see cref="File"/> into the
		/// individual <see cref="Assets"/>
		/// </summary>
		public void LoadAll() {
			if ( Assets.Count == 0 ) {
				LoadManifest();
			}
			Dictionary<long, Asset> pathIDIndex = new Dictionary<long, Asset>();
			foreach ( KeyValuePair<string, Asset> entry in Assets ) {
				pathIDIndex.Add( entry.Value.PathID, entry.Value );
			}
			AssetsFile assetsFile = AssetBundleFile.LoadFromFile( File.FullName ).Files[0].ToAssetsFile();
			foreach ( AssetsFile.ObjectType assetData in assetsFile.Objects ) {
				// first, get appropriate type tree from the asset file
				DynamicAsset dynamicAsset = assetData.ToDynamicAsset();
				if ( pathIDIndex.ContainsKey( assetData.PathID ) ) {
					// then pass it as an argument to the Load for MonoBehaviours and
					// use the appropriate level for each assetobject
					// use dynamic GetMemberBinder to get the member with TryGetMember or similar
					// see also https://stackoverflow.com/questions/2634858/how-do-i-reflect-over-the-members-of-dynamic-object
					// or more likely just treat it as the object it is without trying to
					// change it into a GameObject?
					if ( dynamicAsset.TypeName == "MonoBehaviour" ) {
						SerializedType type = assetsFile.Types[assetData.TypeID];
						pathIDIndex[assetData.PathID].ClassName = GetClassName( dynamicAsset );
						pathIDIndex[assetData.PathID].Load( dynamicAsset, type );
					} else {
						pathIDIndex[assetData.PathID].Load( dynamicAsset );
					}
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
				AssetBundleFile.LoadFromFile( File.FullName ).Files[0].ToAssetsFile().ObjectsWithClass( ClassIDType.MonoScript ) ) {
				if ( monoScript.PathID == ClassPathID ) {
					return monoScript.ToDynamicAsset().AsDynamic().m_ClassName;
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
			file.WriteLine( $"\"File\" : \"{File.FullName}\"," );
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
		/// Gets or sets the <see cref="Asset"/>'s path ID
		/// </summary>
		public long PathID { get; set; }
		/// <summary>
		/// Gets or sets the class name of this <see cref="Asset"/>
		/// </summary>
		/// <remarks>
		/// <see cref="ClassName"/> is null if this asset is not a
		/// MonoBehaviour
		/// </remarks>
		public string ClassName { get; set; }
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
		/// Loads the <see cref="Asset"/> with data from the given
		/// <see cref="DynamicAsset"/>
		/// </summary>
		/// <param name="dynamicAsset"><see cref="DynamicAsset"/> from which
		/// to load data</param>
		public void Load( DynamicAsset dynamicAsset ) {
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
			switch ( dynamicAsset.TypeName ) {
				case "TextAsset":
					if ( asset.HasMember( "m_Script" ) ) {
						if ( asset.m_Script is string ) {
							Value = DecodeString( asset.m_Script );
						} else {
							throw new NotImplementedException( $"Don't know what to do with m_Script of type {asset.m_Script.GetType()}" );
						}
					} else {
						throw new NotImplementedException( "Unable to determine value of this asset." );
					}
					break;
				case "MonoBehaviour":
					// this way of doing it is untenable. Need to get all properties/members of objects array
					// as a dictionary
					Dictionary<string, AssetObject> properties = new Dictionary<string, AssetObject>();
					if ( asset.HasMember( "keys" ) ) {
						AssetObject keys = new AssetObject( asset.keys );
						properties.Add( "keys", keys );
					}
					if ( asset.HasMember( "values" ) ) {
						AssetObject values = new AssetObject( asset.values );
						properties.Add( "values", values );
					}
					if ( asset.HasMember( "counts" ) ) {
						AssetObject counts = new AssetObject( asset.counts );
						properties.Add( "counts", counts );
					}
					if ( asset.HasMember( "list" ) ) {
						AssetObject list = new AssetObject( asset.list );
						properties.Add( "list", list );
					}
					if ( asset.HasMember( "table" ) ) {
						AssetObject list = new AssetObject( asset.table );
						properties.Add( "table", list );
					}
					if ( properties.Count == 0 ) {
						throw new NotImplementedException( "Unable to get meaningful data from asset" );
					}
					Value = properties;
					break;
				// Ignore these types when loading assets
				case "MonoScript":
				case "AssetBundle":
					break;
				default:
					throw new NotImplementedException( $"Unable to handle asset of type {dynamicAsset.TypeName}" );
			}
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
		/// Initializes a new instance of the <see  cref="AssetObject"/> class
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
