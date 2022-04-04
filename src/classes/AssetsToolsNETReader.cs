using System;
using System.Collections.Generic;
using System.IO;
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
		private Dictionary<string, AssetBundle> assetBundles { get; set; }
		public AssetsToolsNETReader() {
			assetsManager = new AssetsManager();
			assetBundles = new();
		}
		public bool Contains( string name, AssetBundle assetBundle ) {
			return GetAllAssetNames( assetBundle ).Contains( name );
		}
		public AssetBundle LoadAssetBundle( string path ) {
			if ( String.IsNullOrEmpty( path ) ) throw new ArgumentNullException( "path" );
			if ( !File.Exists( path ) ) throw new ArgumentException( "The file does not exist.", nameof( path ) );
			// LoadBundleFile automatically checks to see if the asset bundle is already loaded;
			// AssetsTools.NET indexes them by path, so we implement only with the canonical path
			path = Path.GetFullPath( path );
			BundleFileInstance assetsBundleInstance = assetsManager.LoadBundleFile( path );
			if ( !assetBundles.ContainsKey( path ) ) {
				if ( assetsBundleInstance.file.NumFiles != 1 ) {
					throw new NotSupportedException( $"Unable to evaluate asset bundle '{path}'; its format is unusual." );
				}
				AssetBundle assetBundle = new AssetBundle( this );
				assetBundle.Name = assetsBundleInstance.name;
				assetBundles[path] = assetBundle;
			}
			return assetBundles[path];
		}
		public Asset GetAsset( string assetName, AssetBundle assetBundle ) {
			CheckAssetBundle( assetBundle );
			BundleFileInstance assetBundleInst = assetsManager.LoadBundleFile( assetBundle.Path );
			AssetsFileInstance assetFileInst = assetsManager.LoadAssetsFileFromBundle( assetBundleInst, assetName );

			throw new NotImplementedException();
		}
		public List<string> GetAllAssetNames( AssetBundle assetBundle ) {
			CheckAssetBundle( assetBundle );
			List<string> assetNames = new();
			foreach ( AssetTypeValueField assetInfo in GetAllAssetInfos( assetBundle ) ) {
				assetNames.Add( assetInfo[0].GetValue().AsString() );
			}
			return assetNames;
		}
		public List<Asset> GetAllAssets( AssetBundle assetBundle ) {
			CheckAssetBundle( assetBundle );
			List<Asset> assets = new();
			foreach ( AssetTypeValueField asset in GetAllAssetInfos( assetBundle ) ) {

				throw new NotImplementedException();
			}
			return assets;
		}
		private AssetTypeValueField[] GetAllAssetInfos( AssetBundle assetBundle ) {
			CheckAssetBundle( assetBundle );
			BundleFileInstance assetBundleInst = assetsManager.LoadBundleFile( assetBundle.Path );
			AssetsFileInstance assetFileInst = assetsManager.LoadAssetsFileFromBundle( assetBundleInst, 0 );
			List<AssetFileInfoEx> assetBundleInfos = assetFileInst.table.GetAssetsOfType( (int)AssetClassID.AssetBundle );
			if ( assetBundleInfos.Count != 1 ) throw new NotSupportedException( $"Unable to evaluate asset bundle '{assetBundle.Path}'; bundle catalog is invalid." );
			AssetTypeValueField assetInfoArray = assetsManager.GetTypeInstance( assetFileInst, assetBundleInfos[0] ).GetBaseField().Get( "m_Container" ).Get( "Array" );
			return assetInfoArray.children;
		}
		private void CheckAssetBundle( AssetBundle assetBundle ) {
			if ( assetBundle is null ) throw new ArgumentNullException( nameof( assetBundle ) );
			if ( String.IsNullOrEmpty( assetBundle.Path ) ) throw new InvalidOperationException( "No path found for asset bundle" );
			assetBundle.Path = Path.GetFullPath( assetBundle.Path );
			if ( !assetBundles.ContainsKey( assetBundle.Path ) ) throw new InvalidOperationException( "This asset bundle was not loaded by this reader." );
		}
	}
}
