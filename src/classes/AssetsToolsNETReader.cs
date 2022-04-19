using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AssetsTools.NET;
using AssetsTools.NET.Extra;

namespace Mffer {
	/// <summary>
	/// <see cref="IAssetReader"/> implementation using AssetsTools.NET
	/// </summary>
	public class AssetsToolsNETReader : IAssetReader {
		private AssetsManager assetsManager { get; set; }
		/// <summary>
		/// A dictionary of <see cref="AssetBundle"/> objects loaded by this <see cref="AssetsToolsNETReader"/>
		/// </summary>
		private Dictionary<string, AssetsToolsNETBundle> assetBundles { get; set; }
		public AssetsToolsNETReader() {
			assetsManager = new AssetsManager();
			assetBundles = new();
		}
		public bool Contains( string name, AssetBundle assetBundle ) {
			CheckAssetBundle( assetBundle );
			return assetBundles[assetBundle.Path].AssetInfo.ContainsKey( name );
		}
		public AssetBundle LoadAssetBundle( string path ) {
			if ( String.IsNullOrEmpty( path ) ) throw new ArgumentNullException( "path" );
			if ( !File.Exists( path ) ) throw new ArgumentException( "The file does not exist.", nameof( path ) );
			// AssetsTools.NET's AssetManager already caches loaded assets by
			// case-insensitive full path name, but requires opening a file
			// stream before checking, so we add our own caching layer without
			// major overhead.
			path = Path.GetFullPath( path );
			if ( !assetBundles.ContainsKey( path ) ) {
				BundleFileInstance assetsBundleInstance = assetsManager.LoadBundleFile( path );
				if ( assetsBundleInstance.file.NumFiles != 1 ) {
					throw new NotSupportedException( $"Unable to evaluate asset bundle '{path}'; its format is unusual." );
				}
				AssetsToolsNETBundle assetsToolsNETBundle = new(
					path,
					assetsBundleInstance,
					new AssetBundle( this )
				);
				assetBundles.Add( path, assetsToolsNETBundle );

				// Load the individual assets info; may as well do this here
				// since we never load an asset bundle without looking for
				// assets
				AssetsFileInstance assetFileInst = assetsManager.LoadAssetsFileFromBundle( assetsBundleInstance, 0 );
				List<AssetFileInfoEx> assetBundleInfos = assetFileInst.table.GetAssetsOfType( (int)AssetClassID.AssetBundle );
				if ( assetBundleInfos.Count != 1 ) throw new NotSupportedException( $"Unable to evaluate asset bundle '{path}'; bundle catalog is invalid." );
				AssetTypeValueField assetInfoArray = assetsManager.GetTypeInstance( assetFileInst, assetBundleInfos[0] ).GetBaseField().Get( "m_Container" ).Get( "Array" );
				Dictionary<long, string> assetIDs = new();
				foreach ( AssetTypeValueField asset in assetInfoArray.children ) {
					assetIDs.Add( asset[1].Get( "asset" ).Get( "m_PathID" ).GetValue().AsInt64(), asset[0].GetValue().AsString() );
				}
				foreach ( AssetFileInfoEx asset in assetFileInst.table.assetFileInfo ) {
					if ( assetIDs.ContainsKey( asset.index ) ) {
						assetsToolsNETBundle.AssetInfo.Add(
							assetIDs[asset.index],
							asset
						);
					}
					// probably still need to do something here with MonoBehaviors & MonoScripts
				}
			}
			return assetBundles[path].AssetBundle;
		}
		/// <summary>
		///
		/// </summary>
		/// <param name="assetName"></param>
		/// <param name="assetBundle"></param>
		/// <returns></returns>
		/// <exception cref="KeyNotFoundException"></exception>
		public Asset GetAsset( string assetName, AssetBundle assetBundle ) {
			CheckAssetBundle( assetBundle );
			if ( !assetBundle.Contains( assetName ) )
				throw new KeyNotFoundException( $"Asset bundle {assetBundle.Path} does not contain an asset named '{assetName}'." );
			if ( !assetBundle.Assets.ContainsKey( assetName ) || assetBundle.Assets[assetName] is null ) {
				assetBundle.Assets[assetName] = new Asset( assetBundles[assetBundle.Path].AssetInfo[assetName].index );
			}
			Asset asset = assetBundle.Assets[assetName];
			AssetsFileInstance assetsFileInstance = assetsManager.LoadAssetsFileFromBundle( assetBundles[assetBundle.Path].AssetBundleInstance, 0 );
			AssetTypeInstance assetInstance = assetsManager.GetTypeInstance( assetsFileInstance, assetBundles[assetBundle.Path].AssetInfo[assetName] );
			asset.Name = assetInstance.GetBaseField().Get( "m_Name" ).GetValue().AsString();
			asset.PathID = assetBundles[assetBundle.Path].AssetInfo[assetName].index;
			SortedDictionary<string, GameObject> children = new();
			foreach ( AssetTypeValueField child in assetInstance.GetBaseField().GetChildrenList() ) {
				children.Add( child.GetName(), child.ToGameObject() );
			}
			asset.Value = children.ToGameObject().Value;
			return asset;
		}
		public List<string> GetAllAssetNames( AssetBundle assetBundle ) {
			CheckAssetBundle( assetBundle );
			return assetBundles[assetBundle.Path].AssetInfo.Keys.ToList();
		}
		public List<Asset> GetAllAssets( AssetBundle assetBundle ) {
			CheckAssetBundle( assetBundle );
			AssetsFileInstance assetsFileInstance =
				assetsManager.LoadAssetsFileFromBundle( assetBundles[assetBundle.Path].AssetBundleInstance, 0 );
			foreach ( KeyValuePair<string, AssetFileInfoEx> entry in assetBundles[assetBundle.Path].AssetInfo ) {
				if ( !assetBundle.Assets.ContainsKey( entry.Key ) || assetBundle.Assets[entry.Key] is null ) {
					assetBundle.Assets[entry.Key] = new Asset( entry.Value.index );
				}
				Asset asset = assetBundle.Assets[entry.Key];
				if ( asset.Value is null ) {
					AssetTypeInstance assetInstance = assetsManager.GetTypeInstance( assetsFileInstance, entry.Value );
					asset.Name = assetInstance.GetBaseField().Get( "m_Name" ).GetValue().AsString();
					asset.PathID = entry.Value.index;
					SortedDictionary<string, GameObject> assetDictionary = new SortedDictionary<string, GameObject>();
					foreach ( AssetTypeValueField child in assetInstance.GetBaseField().GetChildrenList() ) {
						assetDictionary.Add( child.GetName(), child.ToGameObject() );
					}
					asset.Value = assetDictionary.ToGameObject().Value;
				}
			}
			return assetBundle.Assets.Values.ToList();
		}
		private void CheckAssetBundle( AssetBundle assetBundle ) {
			if ( assetBundle is null ) throw new ArgumentNullException( nameof( assetBundle ) );
			if ( String.IsNullOrEmpty( assetBundle.Path ) ) throw new InvalidOperationException( "No path found for asset bundle" );
			string path = Path.GetFullPath( assetBundle.Path );
			if ( !assetBundles.ContainsKey( assetBundle.Path ) ) throw new InvalidOperationException( "This asset bundle was not loaded by this reader." );
		}
		private class AssetsToolsNETBundle {
			internal AssetBundle AssetBundle { get; }
			internal BundleFileInstance AssetBundleInstance { get; }
			internal Dictionary<string, AssetFileInfoEx> AssetInfo { get; }
			internal AssetsToolsNETBundle( string path, BundleFileInstance bundleFileInstance, AssetBundle assetBundle ) {
				AssetBundle = assetBundle;
				AssetBundle.Path = path;
				AssetBundleInstance = bundleFileInstance;
				AssetInfo = new();
			}
		}
	}
	public static class AssetsToolsNETExtensions {
		public static GameObject ToGameObject( this AssetTypeValueField assetField ) {
			if ( assetField.templateField.isArray == true ) {
				List<GameObject> array = new();
				foreach ( AssetTypeValueField child in assetField.GetChildrenList() ) {
					array.Add( child.ToGameObject() );
				}
				return array.ToGameObject();
			} else if ( assetField.GetChildrenCount() == 0 ) {
				if ( assetField.GetValue() is null ) {
					return new GameObject();
				} else {
					return assetField.GetValue().AsString().ToGameObject();
				}
			} else if ( assetField.GetChildrenCount() < 0 ) {
				throw new InvalidOperationException( $"Unable to convert field '{assetField.GetName()}' to a GameObject" );
			} else if ( assetField.GetChildrenCount() == 1 && assetField.GetChildrenList()[0].templateField.isArray == true ) {
				return assetField.GetChildrenList()[0].ToGameObject();
			} else {
				SortedDictionary<string, GameObject> dictionary = new();
				foreach ( AssetTypeValueField child in assetField.GetChildrenList() ) {
					dictionary.Add( child.GetName(), child.ToGameObject() );
				}
				return dictionary.ToGameObject();
			}
		}
	}
}
