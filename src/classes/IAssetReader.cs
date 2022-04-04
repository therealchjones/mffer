using System.Collections.Generic;

namespace Mffer {
	/// <summary>
	/// Interface for accessing Unity asset data, abstracted from the specific
	/// tools used
	/// </summary>
	public interface IAssetReader {
		public bool Contains( string name, AssetBundle assetBundle );
		public AssetBundle LoadAssetBundle( string path );
		public Asset GetAsset( string assetName, AssetBundle assetBundle );
		public List<string> GetAllAssetNames( AssetBundle assetBundle );
		public List<Asset> GetAllAssets( AssetBundle assetBundle );
	}
}
