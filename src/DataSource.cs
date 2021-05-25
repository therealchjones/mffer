using System;
using System.Collections.Generic;
using System.Dynamic;
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
	/// from <see cref="DeviceDirectory"/> objects that are each associated
	/// a given <see cref="Version"/> of the <see cref="Game"/>. Data from
	/// the <see cref="DeviceDirectory"/> are loaded into the
	/// <see cref="AssetBundle"/> associated with the same version when
	/// requested by the a <see cref="Game"/> instance.</para>
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
			BackingDirectory.LoadAllAssets();
		}
		/// <summary>
		/// Loads data into this <see cref="AssetBundle"/> from a version
		/// directory
		/// </summary>
		/// <param name="dirs"></param>
		public void LoadFromVersionDirectory( List<DirectoryInfo> dirs ) {
			Dictionary<string, string> manifest = new Dictionary<string, string>();
			HashSet<string> manifestFiles = new HashSet<string>();
			HashSet<string> scriptFiles = new HashSet<string>();
			HashSet<string> behaviorFiles = new HashSet<string>();
			HashSet<string> textFiles = new HashSet<string>();
			HashSet<string> jsonFiles = new HashSet<string>();
			HashSet<string> checkFiles = new HashSet<string>();
			foreach ( DirectoryInfo directory in dirs ) {
				string assetDir = directory.FullName + "/assets";
				manifestFiles.UnionWith( Directory.GetFiles( assetDir, "*-AssetBundle.json" ) );
				scriptFiles.UnionWith( Directory.GetFiles( assetDir, "*-MonoScript.json" ) );
				behaviorFiles.UnionWith( Directory.GetFiles( assetDir, "*-MonoBehaviour.json" ) );
				textFiles.UnionWith( Directory.GetFiles( assetDir, "*-TextAsset.json" ) );
				jsonFiles.UnionWith( Directory.GetFiles( assetDir ) );
			}
			checkFiles.UnionWith( manifestFiles );
			checkFiles.UnionWith( scriptFiles );
			checkFiles.UnionWith( behaviorFiles );
			checkFiles.UnionWith( textFiles );
			foreach ( string file in jsonFiles ) {
				if ( !checkFiles.Contains( file ) ) {
					throw new Exception( $"Unable to determine type of file {file}" );
				}
			}
			if ( manifestFiles.Count == 0 ) {
				throw new Exception( "Found no manifest files." );
			}
			foreach ( string manifestFile in manifestFiles ) {
				AssetFile manifestAsset = null;
				string manifestFileCode = Regex.Replace( manifestFile, "^.*-([0-9a-f]*)-[0-9]*-[^-]*$", "$1" );
				manifestAsset.LoadFromFile( manifestFile );
				foreach ( AssetObject entry in manifestAsset.Properties["m_Container"].Properties["Array"].Array ) {
					string pathID = entry.Properties["data"].Properties["second"].Properties["asset"].Properties["m_PathID"].String;
					if ( pathID.StartsWith( '-' ) ) {
						UInt64 newID = System.Convert.ToUInt64( pathID.Substring( 1 ) );
						newID = UInt64.MaxValue - newID + 1;
						pathID = newID.ToString();
					}
					manifest.Add( $"{manifestFileCode}-{pathID}", entry.Properties["data"].Properties["first"].String );
				}
			}
			foreach ( string scriptFile in scriptFiles ) {
				AssetFile scriptAsset = null;
				scriptAsset.LoadFromFile( scriptFile );
				string pathID = Regex.Replace( scriptFile, "^.*-([0-9a-f]*)-([0-9]*)-[^-]*$", "$1-$2" );
				string scriptClass = scriptAsset.Name;
				manifest.Add( pathID, scriptClass );
			}
			foreach ( string behaviorFile in behaviorFiles ) {
				AssetFile behaviorAsset = null;
				behaviorAsset.LoadFromFile( behaviorFile );
				string manifestFileCode = Regex.Replace( behaviorFile, "^.*-([0-9a-f]*)-[0-9]*-[^-]*$", "$1" );
				string scriptID = behaviorAsset.Properties["m_Script"].Properties["m_PathID"].String;
				if ( scriptID.StartsWith( '-' ) ) {
					UInt64 newID = System.Convert.ToUInt64( scriptID.Substring( 1 ) );
					newID = UInt64.MaxValue - newID + 1;
					scriptID = newID.ToString();
				}
				if ( !manifest.ContainsKey( $"{manifestFileCode}-{scriptID}" ) ) {
					throw new Exception( $"Script pathID {scriptID} (from file {behaviorFile}) not found in manifest" );
				}
				behaviorAsset.AssetName = manifest[$"{manifestFileCode}-{scriptID}"];
				behaviorAsset.Name = behaviorAsset.AssetName;
				if ( behaviorAsset.AssetName == null ) {
					throw new Exception( $"Asset file {behaviorFile} has no asset name." );
				}
				if ( AssetFiles.ContainsKey( behaviorAsset.AssetName ) ) {
					throw new Exception( $"Attempted to add asset {behaviorAsset.AssetName} (from file {behaviorFile}) which already exists." );
				}
				AssetFiles.Add( behaviorAsset.AssetName, behaviorAsset );
			}
			foreach ( string jsonFile in textFiles ) {
				AssetFile asset = null;
				string pathID = Regex.Replace( jsonFile, "^.*-([0-9a-f]*)-([0-9]*)-[^-]*$", "$1-$2" );
				if ( manifest.ContainsKey( pathID ) ) {
					asset.AssetName = manifest[pathID];
				} else {
					throw new Exception( $"pathID {pathID} (file {jsonFile}) not in manifest" );
				}
				if ( asset.AssetName == null ) {
					throw new Exception( $"Asset file {jsonFile} has no asset name." );
				}
				asset.LoadFromFile( jsonFile );
				if ( AssetFiles.ContainsKey( asset.AssetName ) ) {
					throw new Exception( $"Attempted to add asset {asset.AssetName} (from file {jsonFile}) which already exists." );
				}
				AssetFiles.Add( asset.AssetName, asset );
			}
		}
	}
	/// <summary>
	/// Represents a stored asset file extracted from the game
	/// </summary>
	/// <remarks>
	/// Asset files (when extracted) are JSON-formatted
	/// files containing nested <see cref="AssetObject"/>s. Each
	/// <see cref="AssetFile"/> is referred to by name for use in loading
	/// data within the <see cref="Game"/> object. The <see cref="AssetFile"/>
	/// class includes methods for loading the data from the filesystem
	/// and outputting the data in JSON format.
	/// </remarks>
	public class AssetFile : AssetObject {
		/// <summary>
		/// Gets or sets the <see cref="FileInfo"/> instance containing
		/// data for this <see cref="AssetFile"/>
		/// </summary>
		FileInfo File { get; set; }
		/// <summary>
		/// To remove, placeholder
		/// </summary>
		public string AssetName { get; set; }
		/// <summary>
		/// To remove, placeholder
		/// </summary>
		public string AssetType { get; set; }
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
		/// Loads data into the <see cref="AssetFile"/> object from the file
		/// on disk
		/// </summary>
		/// <param name="filename">The name of the file from which to load
		/// data</param>
		public void LoadFromFile( string filename ) {
			FileStream jsonFile = System.IO.File.Open( filename, FileMode.Open );
			JsonDocument json = GetJson( jsonFile );
			JsonElement jsonRoot = json.RootElement;
			JsonValueKind jsonType = jsonRoot.ValueKind;
			JsonElement Value = new JsonElement();
			if ( jsonType != JsonValueKind.Object ) {
				throw new JsonException( $"{filename} is not a valid asset file" );
			}
			int propertyCount = 0;
			foreach ( JsonProperty jsonProperty in jsonRoot.EnumerateObject() ) {
				string[] nameArray = jsonProperty.Name.Split( ' ', 3 );
				if ( nameArray.Length != 3 ) {
					throw new JsonException( $"{filename} is not a valid asset file" );
				}
				Name = jsonProperty.Name.Split( ' ', 3 )[2];
				AssetType = jsonProperty.Name.Split( ' ', 3 )[1];
				Value = jsonProperty.Value.Clone();
				Type = jsonProperty.Value.ValueKind;
				propertyCount++;
			}
			json.Dispose();
			jsonFile.Dispose();
			if ( propertyCount == 0 ) {
				throw new JsonException( $"{filename} does not contain any JSON members beyond root." );
			} else if ( propertyCount != 1 ) {
				throw new JsonException( $"{filename} contains more than one top level member." );
			} else if ( Type != JsonValueKind.Object ) {
				throw new JsonException( $"{filename} top level member is type {Type.ToString()} rather than JSON object." );
			} else if ( Name != "Base" ) {
				throw new JsonException( $"{filename} top level member is not Base" );
			}
			switch ( AssetType ) {
				case "AssetBundle":
					foreach ( JsonProperty property in Value.EnumerateObject() ) {
						string[] nameArray = property.Name.Split( ' ', 3 );
						if ( nameArray.Length != 3 ) {
							throw new JsonException( $"{filename} is not a valid asset file" );
						}
						if ( nameArray[2] == "m_AssetBundleName" ) {
							Name = property.Value.GetString();
							AssetName = Name;
						}
						AssetObject newAsset = new AssetObject();
						newAsset.Name = nameArray[2];
						newAsset.ParseJson( property.Value );
						if ( Properties.ContainsKey( newAsset.Name ) ) {
							throw new JsonException( $"Asset {Name} already contains a property named {newAsset.Name}" );
						}
						Properties.Add( newAsset.Name, newAsset );
					}
					break;
				case "MonoScript":
					foreach ( JsonProperty property in Value.EnumerateObject() ) {
						string[] nameArray = property.Name.Split( ' ', 3 );
						if ( nameArray.Length != 3 ) {
							throw new JsonException( $"{filename} is not a valid asset file" );
						}
						if ( nameArray[2] == "m_ClassName" ) {
							Name = property.Value.GetString();
						}
					}
					AssetName = Name;
					return;
				case "MonoBehaviour":
					foreach ( JsonProperty property in Value.EnumerateObject() ) {
						string[] nameArray = property.Name.Split( ' ', 3 );
						if ( nameArray.Length != 3 ) {
							throw new JsonException( $"{filename} is not a valid asset file" );
						}
						AssetObject newAsset = new AssetObject();
						newAsset.Name = nameArray[2];
						newAsset.ParseJson( property.Value );
						if ( Properties.ContainsKey( newAsset.Name ) ) {
							throw new JsonException( $"Asset already contains a property named {newAsset.Name}" );
						}
						Properties.Add( newAsset.Name, newAsset );
					}
					break;
				case "TextAsset":
					foreach ( JsonProperty property in Value.EnumerateObject() ) {
						AssetObject newAsset = new AssetObject();
						// this repeated way of checking the name should be a method
						string[] nameArray = property.Name.Split( ' ', 3 );
						if ( nameArray.Length != 3 ) {
							throw new JsonException( $"{filename} is not a valid asset file" );
						}
						newAsset.Name = nameArray[2];
						JsonElement value = property.Value;
						if ( newAsset.Name == "m_Script" ) {
							string valueString = property.Value.GetString();
							if ( !valueString.Contains( '\t' ) ) { // this is the way it's checked in the program
								valueString = System.Text.Encoding.Unicode.GetString( Convert.FromBase64String( valueString ) );
							}
							if ( AssetName.EndsWith( ".csv", true, null ) ) {
								valueString = CSVtoJson( valueString );
							}
							MemoryStream valueStream = new MemoryStream( System.Text.Encoding.Default.GetBytes( valueString ) );
							JsonDocument jsonDocument = GetJson( valueStream );
							value = jsonDocument.RootElement.Clone();
							jsonDocument.Dispose();
							valueStream.Dispose();
						}
						newAsset.ParseJson( value );
						if ( Properties.ContainsKey( newAsset.Name ) ) {
							throw new JsonException( $"Asset already contains a property named {newAsset.Name}" );
						}
						Properties.Add( newAsset.Name, newAsset );
					}
					break;
			}
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
			file.WriteLine( $"\"{AssetName}\" : " + "{" );
			for ( int i = 0; i < tabs + 1; i++ ) {
				file.Write( "\t" );
			}
			file.Write( $"\"AssetType\" : \"{AssetType}\"" );
			if ( Properties.Count > 0 ) {
				file.WriteLine( "," );
				base.WriteJson( file, tabs + 1 );
			}
			file.WriteLine();
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
	}
	/// <summary>
	/// Represents a single object from an <see cref="AssetFile"/>
	/// </summary>
	/// <remarks>
	/// Each <see cref="AssetFile"/> represents data loaded from a
	/// JSON-formatted file. The nested JSON members are represented in
	/// <see cref="AssetObject"/>s, standardizing each into a JSON-like
	/// <see cref="String"/>, object (<see cref="Properties"/>), or
	/// <see cref="Array"/>. The class includes methods for parsing the
	/// JSON members and writing the objects to a JSON stream.
	/// </remarks>
	public class AssetObject : GameObject {
		/// <summary>
		/// Gets or sets the name of the <see cref="AssetObject"/>
		/// </summary>
		public string Name { get; set; }
		/// <summary>
		/// Gets or sets the type of the JSON member value
		/// </summary>
		public JsonValueKind Type { get; set; }
		/// <summary>
		/// Gets or sets the list of properties (members) of a JSON object
		/// </summary>
		public Dictionary<string, AssetObject> Properties { get; set; }
		/// <summary>
		/// Gets or sets the value of a JSON string
		/// </summary>
		public string String { get; set; }
		/// <summary>
		/// Gets or sets the array data from a JSON array
		/// </summary>
		public List<AssetObject> Array { get; set; }
		/// <summary>
		/// Initializes a new instance of the <see cref="AssetObject"/> class
		/// </summary>
		public AssetObject() : base() {
			Type = new JsonValueKind();
			Properties = new Dictionary<string, AssetObject>();
			Array = new List<AssetObject>();
			Name = null;
			String = null;
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

		}
		/// <summary>
		///
		/// </summary>
		/// <param name="assetArray"></param>
		public AssetObject( DynamicAssetArray assetArray ) : this() {

		}
		/// <summary>
		///
		/// </summary>
		/// <param name="array"></param>
		public AssetObject( Array array ) : this() {

		}
		/// <summary>
		/// Loads a JSON-formatted string into a <see cref="JsonDocument"/>
		/// </summary>
		/// <param name="json">JSON-formatted string to prepare for
		/// parsing</param>
		/// <returns><see cref="JsonDocument"/> loaded with data from the
		/// <paramref name="json"/> string</returns>
		protected JsonDocument GetJson( Stream json ) {
			var options = new JsonDocumentOptions {
				AllowTrailingCommas = true,
				CommentHandling = JsonCommentHandling.Skip
			};
			try {
				JsonDocument jsonDocument = JsonDocument.Parse( json, options );
				return jsonDocument;
			} catch ( JsonException e ) {
				// when there is an error in the json formatting, don't use
				// the file, but don't fail silently either; luckily, a (properly escaped)
				// single string can be an entire json document
				// FWIW, currently only seems to occur in a single asset file that uses some leading 0s in numbers
				string errorString = "ERROR: The original document has JSON formatting errors.\n";
				errorString += "Attempting to parse the document resulted in the exception:\n";
				errorString += e.Message;
				errorString = JsonEncodedText.Encode( errorString ).ToString();
				JsonDocument errorDocument = JsonDocument.Parse( "\"" + errorString + "\"" );
				return errorDocument;
			}
		}
		/// <summary>
		/// Loads a JSON element into the <see cref="AssetObject"/>
		/// </summary>
		/// <param name="Value">A <see cref="JsonElement"/> from a
		/// <see cref="JsonDocument"/></param>
		public void ParseJson( JsonElement Value ) {
			if ( Properties.Count != 0 ) {
				throw new Exception( $"Asset object {Name} already has properties loaded." );
			}
			if ( Array.Count != 0 ) {
				throw new Exception( $"Asset object {Name} already has an array loaded." );
			}
			if ( !String.IsNullOrEmpty( String ) ) {
				throw new Exception( $"Asset object {Name} already has a string loaded." );
			}
			Type = Value.ValueKind;
			switch ( Type ) {
				case JsonValueKind.Object:
					foreach ( JsonProperty jsonProperty in Value.EnumerateObject() ) {
						AssetObject newObject = new AssetObject();
						string[] nameArray = jsonProperty.Name.Split( ' ', 3 );
						if ( nameArray.Length == 3 ) {
							newObject.Name = nameArray[2];
						} else {
							newObject.Name = jsonProperty.Name;
						}
						newObject.Type = jsonProperty.Value.ValueKind;
						newObject.ParseJson( jsonProperty.Value );
						// Use item[] instead of Add() to allow duplicate keys,
						// with later ones overwriting previous, something that
						// occurs sometimes in the level.txt TextAssets
						Properties[newObject.Name] = newObject;
					}
					return;
				case JsonValueKind.Array:
					int arrayCounter = 0;
					foreach ( JsonElement jsonElement in Value.EnumerateArray() ) {
						AssetObject newObject = new AssetObject();
						newObject.Type = jsonElement.ValueKind;
						newObject.ParseJson( jsonElement );
						Array.Insert( arrayCounter, newObject );
						arrayCounter++;
					}
					return;
				case JsonValueKind.Undefined:
					throw new Exception( $"Unable to parse asset object {Name}" );
				default:
					String = Value.ToString();
					Type = JsonValueKind.String;
					return;
			}
		}
		/// <summary>
		/// Converts a CSV-formatted <see cref="AssetObject"/> to JSON format
		/// </summary>
		/// <param name="csv">The data in CSV format</param>
		/// <returns>A string containing the data in JSON format</returns>
		public string CSVtoJson( string csv ) {
			if ( String.IsNullOrWhiteSpace( csv ) ) return null;
			string[] lines = csv.Split( new char[] { '\r', '\n' } );
			int firstLine;
			string[] headers = null;
			for ( firstLine = 0; firstLine < lines.Length; firstLine++ ) {
				if ( !String.IsNullOrWhiteSpace( lines[firstLine] ) ) {
					headers = lines[firstLine].Split( '\t' );
					for ( int cellNum = 0; cellNum < headers.Length; cellNum++ ) {
						string cellText = headers[cellNum];
						string escapechars = "([\\\"\\\\])";
						Regex regex = new Regex( escapechars );
						cellText = regex.Replace( cellText, "\\$1" );
						headers[cellNum] = cellText;
					}
					break;
				}
			}
			if ( headers == null ) { return null; }
			string jsonArray = "[ ";
			for ( int i = firstLine + 1; i < lines.Length; i++ ) {
				if ( String.IsNullOrWhiteSpace( lines[i] ) ) continue;
				string[] line = lines[i].Split( '\t' );
				if ( line.Length != headers.Length ) {
					throw new Exception( $"CSV poorly formed." );
				}
				string lineString = "{";
				for ( int j = 0; j < headers.Length; j++ ) {
					string cellText = line[j];
					string escapechars = "([\\\"\\\\])";
					Regex regex = new Regex( escapechars );
					cellText = regex.Replace( cellText, "\\$1" );
					lineString += $"\"0 string {headers[j]}\": \"{cellText}\"";
					if ( j != headers.Length - 1 ) {
						lineString += ", ";
					}
				}
				lineString += "},";
				jsonArray += lineString;
			}
			jsonArray = jsonArray.TrimEnd( new char[] { ',', ' ', '\t' } );
			jsonArray += " ]";
			return jsonArray;
		}
		/// <summary>
		/// Writes the <see cref="AssetObject"/> data in JSON format to a
		/// <see cref="StreamWriter"/> stream
		/// </summary>
		/// <param name="file">The <see cref="StreamWriter"/> stream to which
		/// to write</param>
		/// <param name="tabs">The number of tabs with which to prepend each
		/// output line</param>
		/// <seealso cref="Game.Version.WriteJson(StreamWriter, int)"/>
		public override void WriteJson( StreamWriter file, int tabs = 0 ) {
			int counter = 0;
			switch ( Type ) {
				case JsonValueKind.Object:
					List<string> keys = Properties.Keys.ToList();
					keys.Sort();
					foreach ( string key in keys ) {
						AssetObject value = Properties[key];
						for ( int i = 0; i < tabs; i++ ) {
							file.Write( "\t" );
						}
						file.Write( "\"" + key + "\" : " );
						if ( value.Type == JsonValueKind.Object ) {
							file.WriteLine( "{" );
							value.WriteJson( file, tabs + 1 );
							file.WriteLine();
							for ( int i = 0; i < tabs; i++ ) {
								file.Write( "\t" );
							}
							file.Write( "}" );
						} else if ( value.Type == JsonValueKind.Array ) {
							file.WriteLine( "[" );
							value.WriteJson( file, tabs + 1 );
							file.WriteLine();
							for ( int i = 0; i < tabs; i++ ) {
								file.Write( "\t" );
							}
							file.Write( "]" );
						} else {
							value.WriteJson( file );
						}
						if ( counter < keys.Count - 1 ) {
							file.WriteLine( "," );
						}
						counter++;
					}
					break;
				case JsonValueKind.Array:
					foreach ( AssetObject item in Array ) {
						for ( int i = 0; i < tabs; i++ ) {
							file.Write( "\t" );
						}
						if ( item.Type == JsonValueKind.Object ) {
							file.WriteLine( "{" );
							item.WriteJson( file, tabs + 1 );
							file.WriteLine();
							for ( int i = 0; i < tabs; i++ ) {
								file.Write( "\t" );
							}
							file.Write( "}" );
						} else if ( item.Type == JsonValueKind.Array ) {
							file.WriteLine( "[" );
							item.WriteJson( file, tabs + 1 );
							file.WriteLine();
							for ( int i = 0; i < tabs; i++ ) {
								file.Write( "\t" );
							}
							file.Write( "]" );
						} else {
							item.WriteJson( file );
						}
						if ( counter < Array.Count - 1 ) {
							file.WriteLine( "," );
						}
						counter++;
					}
					break;
				case JsonValueKind.Undefined:
					throw new Exception( $"Unable to identify appropriate JSON conversion for asset object {Name}." );
				default:
					file.Write( $"\"{JsonEncodedText.Encode( String ).ToString()}\"" );
					break;
			}
		}
		/// <summary>
		///	Obtains a value from a nested <see cref="AssetObject"/>
		/// </summary>
		/// <param name="key">The optional name of an <see cref="AssetObject"/>
		/// for which to search</param>
		/// <returns>The value associated with this <see cref="AssetObject"/>
		/// and (optionally) <paramref name="key"/></returns>
		public string GetValue( string key = null ) {
			switch ( Type ) {
				case JsonValueKind.Array:
					if ( Array.Count == 1 ) {
						return Array[0].GetValue( key );
					} else {
						throw new Exception( "Unable to get unique value: Array has multiple items." );
					}
				case JsonValueKind.Object:
					if ( key != null ) {
						if ( Properties.ContainsKey( key ) ) {
							return Properties[key].GetValue();
						} else if ( Properties.Count() > 1 ) {
							throw new Exception( $"Unable to get unique value: Object has no property '{key}'." );
						}
					}
					if ( Properties.Count() == 1 ) {
						return Properties.First().Value.GetValue( key );
					} else {
						throw new Exception( "Unable to get unique value: Object has multiple properties." );
					}
				case JsonValueKind.Undefined:
					throw new Exception( "Unable to get unique value: asset type is undefined." );
				default:
					if ( key != null ) {
						throw new Exception( $"Unable to get a unique value: identfied string before any key '{key}'." );
					} else {
						return String;
					}
			}
		}
	}
}
