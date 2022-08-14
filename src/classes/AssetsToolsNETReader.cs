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
		/// <summary>
		/// Gets or sets the <see cref="AssetsTools.NET.Extra.AssetsManager"/>
		/// instance used by this <see cref="AssetsToolsNETReader"/>
		/// </summary>
		private AssetsManager assetsManager { get; set; }
		/// <summary>
		/// Gets or sets the dictionary of <see cref="AssetBundle"/> objects
		/// loaded by this <see cref="AssetsToolsNETReader"/> and indexed by the
		/// full path names of their files
		/// </summary>
		private Dictionary<string, AssetsToolsNETBundle> assetBundles { get; set; }
		/// <summary>
		/// Creates a new instance of an <see cref="AssetsToolsNETReader"/>
		/// </summary>
		public AssetsToolsNETReader() {
			assetsManager = new AssetsManager();
			assetBundles = new();
		}
		/// <inheritdoc/>
		public bool Contains( string name, AssetBundle assetBundle ) {
			CheckAssetBundle( assetBundle );
			return assetBundles[assetBundle.Path].AssetInfo.ContainsKey( name );
		}
		/// <inheritdoc/>
		/// <exception cref="ArgumentNullException"> if <paramref name="path"/>
		/// is null or the empty string</exception>
		/// <exception cref="FileNotFoundException"> if <paramref name="path"/>
		/// is not a path to an existing file</exception>
		/// <exception cref="NotSupportedException"> if the asset bundle at <paramref
		/// name="path"/> is in a format that is not readable by this <see
		/// cref="AssetsToolsNETReader"/></exception>
		public AssetBundle LoadAssetBundle( string path ) {
			if ( String.IsNullOrEmpty( path ) ) throw new ArgumentNullException( "path" );
			if ( !File.Exists( path ) ) throw new FileNotFoundException( "The file does not exist.", path );
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
		/// <inheritdoc/>
		/// <exception cref="KeyNotFoundException"> if <paramref
		/// name="assetName"/> is not the name of an <see cref="Asset"/>
		/// within the provided <see cref="AssetBundle"/></exception>
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
		/// <inheritdoc/>
		public List<string> GetAllAssetNames( AssetBundle assetBundle ) {
			CheckAssetBundle( assetBundle );
			return assetBundles[assetBundle.Path].AssetInfo.Keys.ToList();
		}
		/// <inheritdoc/>
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
		/// <summary>
		/// Evaluates the given <see cref="AssetBundle"/> object to ensure its validity
		/// </summary>
		/// <param name="assetBundle"><see cref="AssetBundle"/> to evaluate</param>
		/// <exception cref="ArgumentNullException">if the <see cref="AssetBundle"/> is null</exception>
		/// <exception cref="InvalidOperationException">if the <see cref="AssetBundle"/> has an invalid path or has not been properly loaded by this <see cref="AssetsToolsNETReader"/></exception>
		private void CheckAssetBundle( AssetBundle assetBundle ) {
			if ( assetBundle is null ) throw new ArgumentNullException( nameof( assetBundle ) );
			if ( String.IsNullOrEmpty( assetBundle.Path ) ) throw new InvalidOperationException( "No path found for asset bundle" );
			string path = Path.GetFullPath( assetBundle.Path );
			if ( !assetBundles.ContainsKey( assetBundle.Path ) ) throw new InvalidOperationException( "This asset bundle was not loaded by this reader." );
		}
		/// <summary>
		/// Represents the collected objects associated with a single <see
		/// cref="AssetBundle"/> accessed by this <see
		/// cref="AssetsToolsNETReader"/>
		/// </summary>
		private class AssetsToolsNETBundle {
			/// <summary>
			/// The <see cref="AssetBundle"/> accessed by the reader
			/// </summary>
			internal AssetBundle AssetBundle { get; }
			/// <summary>
			/// The internal representation of the <see cref="AssetBundle"/> within the reader
			/// </summary>
			internal BundleFileInstance AssetBundleInstance { get; }
			/// <summary>
			/// The data on individual assets within this <see cref="AssetBundle"/>
			/// </summary>
			internal Dictionary<string, AssetFileInfoEx> AssetInfo { get; }
			/// <summary>
			/// Creates a new <see cref="AssetsToolsNETBundle"/> from the given objects
			/// </summary>
			/// <param name="path">Filesystem path to the <see cref="AssetBundle"/></param>
			/// <param name="bundleFileInstance"><see cref="BundleFileInstance"/> associated with the <see cref="AssetBundle"/></param>
			/// <param name="assetBundle">The <see cref="AssetBundle"/></param>
			internal AssetsToolsNETBundle( string path, BundleFileInstance bundleFileInstance, AssetBundle assetBundle ) {
				AssetBundle = assetBundle;
				AssetBundle.Path = path;
				AssetBundleInstance = bundleFileInstance;
				AssetInfo = new();
			}
		}
	}
	/// <summary>
	/// Class containing methods extending classes in <see
	/// cref="AssetsTools.NET"/> and <see cref="AssetsTools.NET.Extra"/>
	/// </summary>
	public static class AssetsToolsNETExtensions {
		/// <summary>
		/// Converts the given <see cref="AssetsTools.NET.AssetTypeValueField"/>
		/// into a <see cref="GameObject"/>
		/// </summary>
		/// <param name="assetField">The <see cref="AssetsTools.NET.AssetTypeValueField"/> to convert</param>
		/// <returns>a <see cref="GameObject"/> representation of <paramref name="assetField"/></returns>
		/// <exception cref="ApplicationException"> if the field cannot be converted to a <see cref="GameObject"/></exception>
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
				throw new ApplicationException( $"Unable to convert field '{assetField.GetName()}' to a GameObject" );
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
