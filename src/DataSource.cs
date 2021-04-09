using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Mffer {
	/// <summary>
	/// Provides a store of extracted <see cref="Game"/> data
	/// </summary>
	/// <remarks>
	/// <para>A <see cref="DataDirectory"/> is made up of a list of
	/// filesystem <see cref="DirectoryInfo"/> (directory) objects
	/// <see cref="dirs"/>, each of which is either a "version directory" or
	/// a parent of one or more version directories. Version directories are
	/// filesystem directories (presented as a list of
	/// <see cref="DirectoryInfo"/> objects, <see cref="versionDirs"/>) that
	/// store <see cref="Game"/> <see cref="AssetFile"/>s and (possibly)
	/// other <see cref="Game"/> data.</para>
	/// <para>Multiple directories may be associated with the same
	/// <see cref="Version"/>; the <see cref="AssetFile"/>s and other data
	/// files from all the directories combined form the data examined for that
	/// <see cref="Version"/>.</para>
	/// <para>The <see cref="DataDirectory"/> class includes these definitions,
	/// methods to add directories to the lists and verify their structures,
	/// and methods to access the <see cref="AssetFile"/>s and other data
	/// within the <see cref="versionDirs"/>.</para>
	/// </remarks>
	public class DataDirectory {
		/// <summary>
		/// Gets or sets the list of directories included in this
		/// <see cref="DataDirectory"/>
		/// </summary>
		/// <seealso cref="DataDirectory.Add(string)"/>
		List<DirectoryInfo> dirs { get; set; }
		/// <summary>
		/// Gets or sets the list of version directories represented by this
		/// <see cref="DataDirectory"/>
		/// </summary>
		/// <seealso cref="DataDirectory.AddVersionDirectory(DirectoryInfo)"/>
		Dictionary<string, List<DirectoryInfo>> versionDirs { get; set; }
		/// <summary>
		/// Initializes a new <see cref="DataDirectory"/> instance containing a
		/// directory
		/// </summary>
		/// <remarks>The <paramref name="pathName"/> is validated and added to
		/// the list of directories</remarks>
		/// <param name="pathName">The full path name of a version directory or
		/// parent of one or more version directories</param>
		public DataDirectory( string pathName ) {
			dirs = new List<DirectoryInfo>();
			versionDirs = new Dictionary<string, List<DirectoryInfo>>();
			Add( pathName );
		}
		/// <summary>
		/// Adds a directory to the <see cref="DataDirectory"/>
		/// </summary>
		/// <remarks>The <paramref name="pathName"/> is validated before
		/// adding.</remarks>
		/// <param name="pathName">The full path name of a version directory or
		/// parent of one or more version directories</param>
		public void Add( string pathName ) {
			if ( !Directory.Exists( pathName ) ) {
				throw new DirectoryNotFoundException( $"Unable to access directory {pathName}" );
			} else {
				DirectoryInfo dir = new DirectoryInfo( pathName );
				if ( !IsIncluded( dir, dirs ) ) {
					if ( IsVersionDirectory( dir ) ) {
						AddVersionDirectory( dir );
					} else {
						var newVersionDirs = new List<DirectoryInfo>();
						foreach ( DirectoryInfo subdir in dir.GetDirectories() ) {
							if ( IsVersionDirectory( subdir ) ) {
								newVersionDirs.Add( subdir );
							}
						}
						if ( newVersionDirs.Count() == 0 ) {
							ThrowBadDataDir();
						} else {
							foreach ( DirectoryInfo versionDir in newVersionDirs ) {
								AddVersionDirectory( versionDir );
							}
						}
					}
					dirs.Add( dir );
				}
			}
		}
		/// <summary>
		/// Adds a directory to the <see cref="versionDirs"/> list
		/// </summary>
		/// <remarks>
		/// <para><see cref="DataDirectory.AddVersionDirectory(DirectoryInfo)"/>
		/// determines the name of the <see cref="Version"/> whose
		/// <see cref="AssetFile"/>s are stored in the <c>assets</c>
		/// subdirectory of <paramref name="dir"/> and adds
		/// <paramref name="dir"/> to the list of version directories.</para>
		/// <para>No validation of <paramref name="dir"/> is performed except
		/// to ensure its name contains a version string.</para>
		/// </remarks>
		/// <param name="dir">The version directory to add</param>
		void AddVersionDirectory( DirectoryInfo dir ) {
			if ( IsIncluded( dir, versionDirs ) ) return;
			string versionString = GetVersionName( dir.Name );
			if ( String.IsNullOrEmpty( versionString ) ) {
				ThrowBadDataDir();
			} else {
				if ( !versionDirs.ContainsKey( versionString ) ) {
					versionDirs.Add( versionString, new List<DirectoryInfo>() );
				}
				versionDirs[versionString].Add( dir );
			}
		}
		/// <summary>
		/// Gets a <see cref="Version"/> name from a directory name
		/// </summary>
		/// <remarks>
		/// Version directories should have a name that ends in the name of the
		/// <see cref="Version"/>. A <see cref="Version"/> name should be a
		/// string that starts with a digit. Given the name of a version
		/// directory, this returns the <see cref="Version"/> name, or null or
		/// the empty string if the name doesn't contain one.
		/// </remarks>
		/// <param name="dirname">The name of the version directory</param>
		/// <returns>The name of the <see cref="Version"/></returns>
		string GetVersionName( string dirname ) {
			char[] digits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
			int firstDigit = dirname.IndexOfAny( digits );
			if ( firstDigit == -1 ) return null;
			return dirname.Substring( firstDigit );
		}
		/// <summary>
		/// Determines whether a given directory is in a directory list
		/// </summary>
		/// <remarks>The
		/// <see cref="DataDirectory.IsIncluded(DirectoryInfo, List{DirectoryInfo})"/>
		/// utility method searches the full path names of the directories in
		/// <paramref name="dirList"/> for a match to the full path name of the
		/// directory <paramref name="directory"/>. In order to match, two
		/// directories do not need to be the same object, but must have the
		/// same full path name.</remarks>
		/// <param name="directory">the directory to match</param>
		/// <param name="dirList">the list of directories to search</param>
		/// <returns><c>true</c> if <paramref name="directory"/>'s full path
		/// name matches one from <paramref name="dirList"/>, <c>false</c>
		/// otherwise</returns>
		/// <seealso cref="DataDirectory.IsIncluded(DirectoryInfo, Dictionary{string, List{DirectoryInfo}})"/>
		bool IsIncluded( DirectoryInfo directory, List<DirectoryInfo> dirList ) {
			foreach ( DirectoryInfo dir in dirList ) {
				if ( dir.FullName == directory.FullName ) return true;
			}
			return false;
		}
		/// <summary>
		/// Determines whether a given directory is in a directory list
		/// dictionary
		/// </summary>
		/// <remarks>The
		/// <see cref="DataDirectory.IsIncluded(DirectoryInfo, Dictionary{string, List{DirectoryInfo}})"/>
		/// utility method searches the full path names of the directories in
		/// all values of the <paramref name="versionList"/> dictionary for a
		/// match to the full path name of the directory
		/// <paramref name="directory"/>. In order to match, two directories do
		/// not need to be the same object, but must have the same full path
		/// name.</remarks>
		/// <param name="directory">the directory to match</param>
		/// <param name="versionList">the dictionary of directory lists to
		/// search</param>
		/// <returns><c>true</c> if <paramref name="directory"/>'s full path
		/// name matches one from <paramref name="versionList"/>, <c>false</c>
		/// otherwise</returns>
		/// <seealso cref="DataDirectory.IsIncluded(DirectoryInfo, List{DirectoryInfo})"/>
		bool IsIncluded( DirectoryInfo directory, Dictionary<string, List<DirectoryInfo>> versionList ) {
			foreach ( List<DirectoryInfo> dirlist in versionList.Values ) {
				if ( IsIncluded( directory, dirlist ) ) return true;
			}
			return false;
		}
		/// <summary>
		/// Determines whether a directory is a valid version directory
		/// </summary>
		/// <remarks>
		/// A version directory must have a name that ends in a version string
		/// (any string starting with a digit) and have a subdirectory named
		/// <c>assets</c>. The
		/// <see cref="DataDirectory.IsVersionDirectory(DirectoryInfo)"/>
		/// utility method determines whether <paramref name="directory"/>
		/// meets these criteria.
		/// </remarks>
		/// <param name="directory">The directory to validate</param>
		/// <returns><c>true</c> if <paramref name="directory"/> is a valid
		/// version directory, <c>false</c> otherwise</returns>
		bool IsVersionDirectory( DirectoryInfo directory ) {
			string versionName = GetVersionName( directory.Name );
			if ( String.IsNullOrEmpty( versionName ) ) {
				return false;
			}
			DirectoryInfo[] assetDirs = directory.GetDirectories( "assets" );
			if ( assetDirs.Length == 1 ) {
				return true;
			} else {
				return false;
			}
		}
		/// <summary>
		/// Throws an exception identifying an invalid <see cref="DataDirectory"/>
		/// </summary>
		void ThrowBadDataDir() {
			throw new ApplicationException( $"Unable to define structure of data directory." );
		}
		/// <summary>
		/// Creates a list of the identified version names
		/// </summary>
		/// <returns>The list of identified version names</returns>
		public List<string> GetVersionNames() {
			return versionDirs.Keys.ToList();
		}
		/// <summary>
		/// Creates a collection of the identified assets for a particular
		/// <see cref="Version"/>
		/// </summary>
		/// <param name="versionName">The name of the <see cref="Version"/> for
		/// which to create the collection</param>
		/// <returns>The collection of assets associated with the given
		/// <see cref="Version"/></returns>
		public AssetBundle GetAssets( string versionName ) {
			AssetBundle assets = new AssetBundle();
			assets.LoadFromVersionDirectory( versionDirs[versionName] );
			return assets;
		}
		/// <summary>
		/// Gets the loaded <see cref="PreferenceFile"/> for a particular
		/// <see cref="Version"/>
		/// </summary>
		/// <param name="versionName">The name of the <see cref="Version"/> for
		/// which to create the <see cref="PreferenceFile"/></param>
		/// <returns>The <see cref="PreferenceFile"/> with information loaded for
		/// the given <see cref="Version"/></returns>
		public PreferenceFile GetPreferences( string versionName ) {
			if ( String.IsNullOrEmpty( versionName ) ) {
				throw new ArgumentNullException( "Version name cannot be empty." );
			}
			List<FileInfo> preferenceFiles = new List<FileInfo>();
			foreach ( DirectoryInfo dir in dirs ) {
				DirectoryInfo[] deviceDirs =
					dir.GetDirectories( $"*device*-{versionName}" );
				foreach ( DirectoryInfo deviceDir in deviceDirs ) {
					FileInfo[] files =
						deviceDir.GetFiles( "com.netmarble.mherosgb.v2.playerprefs.xml", SearchOption.AllDirectories );
					preferenceFiles.AddRange( files );
				}
			}
			if ( preferenceFiles.Count == 0 ) {
				return null;
			} else if ( preferenceFiles.Count > 1 ) {
				ThrowBadDataDir();
			}
			return new PreferenceFile( preferenceFiles.First().FullName );
		}
	}
	/// <summary>
	/// Represents a collection of <see cref="AssetFile"/>s
	/// </summary>
	public class AssetBundle {
		/// <summary>
		/// Sets or gets a dictionary of <see cref="AssetFile"/>s indexed by
		/// <see cref="AssetFile"/> name
		/// </summary>
		public Dictionary<string, AssetFile> AssetFiles { get; set; }
		/// <summary>
		/// Initializes a new <see cref="AssetBundle"/> instance
		/// </summary>
		public AssetBundle() {
			AssetFiles = new Dictionary<string, AssetFile>();
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
		public void WriteJson( StreamWriter file, int tabs = 0 ) {
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
				AssetFile manifestAsset = new AssetFile();
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
				AssetFile scriptAsset = new AssetFile();
				scriptAsset.LoadFromFile( scriptFile );
				string pathID = Regex.Replace( scriptFile, "^.*-([0-9a-f]*)-([0-9]*)-[^-]*$", "$1-$2" );
				string scriptClass = scriptAsset.Name;
				manifest.Add( pathID, scriptClass );
			}
			foreach ( string behaviorFile in behaviorFiles ) {
				AssetFile behaviorAsset = new AssetFile();
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
				AssetFile asset = new AssetFile();
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
		/// Gets or sets the name of the <see cref="AssetFile"/>
		/// </summary>
		public string AssetName { get; set; }
		/// <summary>
		/// Gets or sets the type of the <see cref="AssetFile"/>
		/// </summary>
		public string AssetType { get; set; }
		/// <summary>
		/// Loads data into the <see cref="AssetFile"/> object from the file
		/// on disk
		/// </summary>
		/// <param name="filename">The name of the file from which to load
		/// data</param>
		public void LoadFromFile( string filename ) {
			string jsonText = File.ReadAllText( filename );
			JsonDocument json = GetJson( jsonText );
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
							JsonDocument jsonDocument = GetJson( valueString );
							value = jsonDocument.RootElement.Clone();
							jsonDocument.Dispose();
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
		public AssetObject() {
			Type = new JsonValueKind();
			Properties = new Dictionary<string, AssetObject>();
			Array = new List<AssetObject>();
			Name = null;
			String = null;
		}
		/// <summary>
		/// Loads a JSON-formatted string into a <see cref="JsonDocument"/>
		/// </summary>
		/// <param name="json">JSON-formatted string to prepare for
		/// parsing</param>
		/// <returns><see cref="JsonDocument"/> loaded with data from the
		/// <paramref name="json"/> string</returns>
		protected JsonDocument GetJson( string json ) {
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
