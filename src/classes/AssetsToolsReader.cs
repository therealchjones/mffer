using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AssetsTools;
using AssetsTools.Dynamic;

namespace Mffer {
	/// <summary>
	/// Implementation of IAssetReader using AssetsTools
	/// </summary>
	/// <remarks>
	/// AssetsTools works with a relatively limited subset of Unity Asset formats.
	public class AssetsToolsReader : IAssetReader {
		private Dictionary<string, AssetsToolsBundle> AssetBundles { get; set; }
		public AssetsToolsReader() {
			AssetBundles = new();
		}
		public AssetBundle LoadAssetBundle( string path ) {
			if ( String.IsNullOrEmpty( path ) ) throw new ArgumentNullException( nameof( path ) );
			path = Path.GetFullPath( path );
			if ( AssetBundles.ContainsKey( path ) ) return AssetBundles[path].AssetBundle;
			if ( !File.Exists( path ) ) throw new Exception( $"Unable to load file '{path}'" );
			AssetBundle bundle = new AssetBundle( this );
			bundle.Path = path;
			AssetBundles[path] = new AssetsToolsBundle( bundle );
			return bundle;
		}
		public bool Contains( string name, AssetBundle assetBundle ) {
			CheckAssetBundle( assetBundle );
			return GetAllAssetNames( assetBundle ).Contains( name );
		}
		public List<string> GetAllAssetNames( AssetBundle assetBundle ) {
			CheckAssetBundle( assetBundle );
			LoadAssetBundleManifest( assetBundle );
			return assetBundle.Assets.Keys.ToList();
		}
		public List<Asset> GetAllAssets( AssetBundle assetBundle ) {
			CheckAssetBundle( assetBundle );
			LoadAssetBundleManifest( assetBundle );
			foreach ( KeyValuePair<string, Asset> entry in assetBundle.Assets ) {
				if ( entry.Value is null || entry.Value.Value is null ) {
					assetBundle.Assets[entry.Key] = GetAsset( entry.Key, assetBundle );
				}
			}
			return assetBundle.Assets.Values.ToList();
		}
		public Asset GetAsset( string assetName, AssetBundle assetBundle ) {
			CheckAssetBundle( assetBundle );
			string newAssetName = assetName;
			if ( !assetBundle.Assets.ContainsKey( assetName ) && assetBundle.Assets.ContainsKey( assetName + ".asset" ) ) {
				newAssetName = assetName + ".asset";
			}
			if ( assetBundle.Assets.ContainsKey( newAssetName ) && assetBundle.Assets[newAssetName] is not null && assetBundle.Assets[newAssetName].Value is not null ) {
				return assetBundle.Assets[newAssetName];
			}
			LoadAssetBundleManifest( assetBundle );
			if ( !assetBundle.Assets.ContainsKey( newAssetName ) ) throw new KeyNotFoundException( $"Asset '{assetName}' was not found in asset bundle '{assetBundle.Path}'" );
			LoadAsset( newAssetName, assetBundle );
			return assetBundle.Assets[newAssetName];
		}
		/// <summary>
		/// Initializes the information in the <see cref="Assets"/> catalog with
		/// data from the file
		/// </summary>
		private void LoadAssetBundleManifest( AssetBundle assetBundle ) {
			CheckAssetBundle( assetBundle );
			List<AssetsFile.ObjectType> manifests = AssetBundles[assetBundle.Path].DynamicFile.ObjectsWithClass( ClassIDType.AssetBundle ).ToList();
			if ( manifests.Count == 0 ) {
				throw new FileLoadException( $"Unable to identify AssetBundle manifest" );
			}
			if ( manifests.Count > 1 ) {
				throw new FileLoadException( $"Multiple AssetBundle manifests found" );
			}
			dynamic manifest = GetDynamicAsset( assetBundle, manifests[0] ).AsDynamic();
			SortedDictionary<string, Asset> assets = assetBundle.Assets;
			if ( assets is null ) assets = new();
			foreach ( string assetName in manifest.m_Container.Keys ) {
				if ( !assets.ContainsKey( assetName ) || assets[assetName] is null ) {
					long assetPathId = manifest.m_Container[assetName].asset.m_PathID;
					assetBundle.Assets[assetName] = new Asset( assetPathId );
				}
			}
		}
		/// <summary>
		/// Loads data into the <see cref="Asset"/> with the given name
		/// </summary>
		/// <param name="assetName">Name of the <see cref="Asset"/> to load with
		/// data</param>
		public void LoadAsset( string assetName, AssetBundle assetBundle ) {
			CheckAssetBundle( assetBundle );
			if ( String.IsNullOrEmpty( assetName ) ) throw new ArgumentNullException( nameof( assetName ) );
			if ( !Contains( assetName, assetBundle ) ) throw new Exception( $"Asset '{assetName}' was not found in asset bundle at {assetBundle.Path}" );
			Asset asset = assetBundle.Assets[assetName];
			AssetsFile dynamicFile = AssetBundles[assetBundle.Path].DynamicFile;
			foreach ( AssetsFile.ObjectType assetData in dynamicFile.Objects ) {
				if ( assetData.PathID == asset.PathID ) {
					SerializedType type = dynamicFile.Types[assetData.TypeID];
					DynamicAsset dynamicAsset = GetDynamicAsset( assetData, dynamicFile );
					LoadAsset( asset, dynamicAsset, type, dynamicFile );
					return;
				}
			}
			throw new ApplicationException( $"Unable to find asset data for asset '{assetName}'" );
		}
		/// <summary>
		/// Load the <paramref name="asset"/> with data from the given sources
		/// </summary>
		/// <param name="asset"><see cref="Asset"/> to load with data</param>
		/// <param name="rawAsset"><see cref="DynamicAsset"/> containing data to load into <paramref name="asset"/></param>
		/// <param name="type"><see cref="SerializedType"/> defining the structure of the <see cref="DynamicAsset"/></param>
		public void LoadAsset( Asset asset, DynamicAsset rawAsset, SerializedType type, AssetsFile dynamicFile ) {
			if ( rawAsset.TypeName == "MonoBehaviour" ) {
				Load( asset, rawAsset, GetClassName( rawAsset, dynamicFile ), type );
			} else {
				Load( asset, rawAsset, null, type );
			}
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
		public void Load( Asset asset, DynamicAsset dynamicAsset, string className = null, SerializedType type = new SerializedType() ) {
			dynamic newAsset = dynamicAsset.AsDynamic();
			if ( newAsset.HasMember( "m_Name" ) ) {
				if ( newAsset.m_Name is string ) {
					asset.Name = newAsset.m_Name;
				} else {
					throw new NotImplementedException( $"Don't know what to do with m_Name of type {newAsset.m_Name.GetType()}" );
				}
			} else {
				throw new NotImplementedException( "Unable to find name of asset" );
			}
			asset.Value = dynamicAsset.ToGameObject( type.TypeTree.Nodes ).Value;
		}
		/// <summary>
		/// Determines the class name of a MonoBehaviour
		/// </summary>
		/// <param name="dynamicAsset">MonoBehaviour asset to identify</param>
		/// <returns>The name of the MonoBehaviour's class</returns>
		public string GetClassName( DynamicAsset dynamicAsset, AssetsFile dynamicFile ) {
			if ( dynamicAsset.TypeName != "MonoBehaviour" ) {
				throw new ArgumentException( "Not a MonoBehaviour asset" );
			}
			long ClassPathID = dynamicAsset.AsDynamic().m_Script.m_PathID;
			foreach ( AssetsFile.ObjectType monoScript in
				dynamicFile.ObjectsWithClass( ClassIDType.MonoScript ) ) {
				if ( monoScript.PathID == ClassPathID ) {
					return GetDynamicAsset( monoScript, dynamicFile ).AsDynamic().m_ClassName;
				}
			}
			throw new InvalidDataException( "Unable to determine class name for MonoBehaviour" );
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
		private DynamicAsset GetDynamicAsset( AssetsFile.ObjectType assetData, AssetsFile dynamicFile ) {
			DynamicAsset dynamicAsset = null;
			if ( dynamicFile.Types[assetData.TypeID].ClassID == (int)ClassIDType.MonoBehaviour ) {
				Func<UnityBinaryReader, DynamicAsset> deserialize = DynamicAsset.GenDeserializer( dynamicFile.Types[assetData.TypeID].TypeTree.Nodes );
				dynamicAsset = deserialize( new UnityBinaryReader( assetData.Data ) );
			} else {
				dynamicAsset = assetData.ToDynamicAsset();
			}
			return dynamicAsset;
		}
		/// <summary>
		/// Ensures the given <see cref="AssetBundle"/> is appropriate for use
		/// with the <see cref="IAssetReader"/>
		/// </summary>
		/// <remarks>
		/// Checks to be sure that the <see cref="AssetBundle"/> is not null,
		/// that it is already associated with <see
		/// cref="AssetBundle.assetReader"/>, and that its <see
		/// cref="AssetBundle.Path"/> value is non-null. Sets the <see
		/// cref="AssetBundle.Path"/> value to be a full directory path.
		/// </remarks>
		/// <param name="assetBundle"><see cref="AssetBundle"/> to check</param>
		/// <exception cref="ArgumentNullException">if <see
		/// paramref="assetBundle"/> is null</exception>
		/// <exception cref="InvalidOperationException">if <see
		/// paramref="assetBundle"/> is not already in the <see
		/// cref="AssetsToolsReader.AssetBundles"/> dictionary</exception>
		internal void CheckAssetBundle( AssetBundle assetBundle ) {
			if ( assetBundle is null ) throw new ArgumentNullException( nameof( assetBundle ) );
			if ( assetBundle.assetReader is null ) throw new InvalidOperationException( "This asset bundle was not loaded correctly." );
			if ( String.IsNullOrEmpty( assetBundle.Path ) ) throw new InvalidOperationException( "No path found for asset bundle" );
			assetBundle.Path = Path.GetFullPath( assetBundle.Path );
			if ( !AssetBundles.ContainsKey( assetBundle.Path ) ) throw new InvalidOperationException( "This asset bundle was not loaded by the expected reader." );
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
		public DynamicAsset GetDynamicAsset( AssetBundle assetBundle, AssetsFile.ObjectType assetData ) {
			CheckAssetBundle( assetBundle );
			if ( assetData is null ) throw new ArgumentNullException( nameof( assetData ) );
			DynamicAsset dynamicAsset = null;
			AssetsFile DynamicFile = AssetBundles[assetBundle.Path].DynamicFile;
			if ( DynamicFile.Types[assetData.TypeID].ClassID == (int)ClassIDType.MonoBehaviour ) {
				Func<UnityBinaryReader, DynamicAsset> deserialize = DynamicAsset.GenDeserializer( DynamicFile.Types[assetData.TypeID].TypeTree.Nodes );
				dynamicAsset = deserialize( new UnityBinaryReader( assetData.Data ) );
			} else {
				dynamicAsset = assetData.ToDynamicAsset();
			}
			return dynamicAsset;
		}
		/// <summary>
		/// Loads all available data from <see paramref="assetBundle"/> into the
		/// individual <see cref="Assets"/>
		/// </summary>
		public void LoadAll( AssetBundle assetBundle ) {
			CheckAssetBundle( assetBundle );
			LoadAssetBundleManifest( assetBundle );
			Dictionary<long, Asset> pathIDIndex = new Dictionary<long, Asset>();
			foreach ( KeyValuePair<string, Asset> entry in assetBundle.Assets ) {
				pathIDIndex.Add( entry.Value.PathID, entry.Value );
			}
			AssetsFile DynamicFile = AssetBundles[assetBundle.Path].DynamicFile;
			foreach ( AssetsFile.ObjectType assetData in DynamicFile.Objects ) {
				DynamicAsset dynamicAsset = GetDynamicAsset( assetData, DynamicFile );
				if ( pathIDIndex.ContainsKey( assetData.PathID ) ) {
					LoadAsset( pathIDIndex[assetData.PathID], dynamicAsset, DynamicFile.Types[assetData.TypeID], DynamicFile );
				} else {
					if ( dynamicAsset.TypeName != "MonoScript" && dynamicAsset.TypeName != "AssetBundle" ) {
						throw new InvalidDataException( "Path ID of object not found in manifest" );
					}
				}
			}
		}
		private class AssetsToolsBundle {
			/// <summary>
			/// Gets or sets the <see cref="AssetBundle"/> instance associated with this <see cref="AssetsToolsBundle"/>
			/// </summary>
			internal AssetBundle AssetBundle { set; get; }
			/// <summary>
			/// Gets or sets the <see cref="AssetsFile"/> instance containing
			/// data for this <see cref="AssetsToolsBundle"/>
			/// </summary>
			internal AssetsFile DynamicFile { set; get; }
			internal AssetsToolsBundle( AssetBundle bundle ) {
				if ( bundle.assetReader is null ) throw new InvalidOperationException( "This asset bundle was not created correctly." );
				AssetBundle = bundle;
				DynamicFile = AssetBundleFile.LoadFromFile( bundle.Path ).Files[0].ToAssetsFile();
			}
		}
	}
}
