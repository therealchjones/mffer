using System.Collections.Generic;

namespace Mffer {
	/// <summary>
	/// Interface for accessing Unity asset data, abstracted from the specific
	/// tools used
	/// </summary>
	public interface IAssetReader {
		/// <summary>
		/// Reports whether a given <see cref="Asset"/> is in a given <see
		/// cref="AssetBundle"/>
		/// </summary>
		/// <param name="name">name of <see cref="Asset"/> to seek</param>
		/// <param name="assetBundle"><see cref="AssetBundle"/> to
		/// search</param>
		/// <returns><c>true</c> if <paramref name="assetBundle"/> contains an
		/// asset named <paramref name="name"/> or an asset representing a class
		/// named <paramref name="name"/>, <c>false</c> otherwise</returns>
		public bool Contains( string name, AssetBundle assetBundle );
		/// <summary>
		/// Loads data from a file into a new <see cref="AssetBundle"/>
		/// </summary>
		/// <param name="path">the path of the file to load</param>
		/// <returns>the <see cref="AssetBundle"/> containing data from the
		/// file</returns>
		public AssetBundle LoadAssetBundle( string path );
		/// <summary>
		/// Obtain an <see cref="Asset"/> from an <see cref="AssetBundle"/>
		/// </summary>
		/// <param name="assetName">name of the <see cref="Asset"/> or a class name represented by the asset</param>
		/// <param name="assetBundle"><see cref="AssetBundle"/> containing the
		/// <see cref="Asset"/></param>
		/// <returns></returns>
		public Asset GetAsset( string assetName, AssetBundle assetBundle );
		/// <summary>
		/// Obtain a <see cref="List{String}"/> of the names of all <see
		/// cref="Asset"/>s in the given <see cref="AssetBundle"/>
		/// </summary>
		/// <param name="assetBundle"><see cref="AssetBundle"/> to read</param>
		/// <returns><see cref="List{String}"/> of the names of all <see
		/// cref="Asset"/>s in <paramref name="assetBundle"/></returns>
		public List<string> GetAllAssetNames( AssetBundle assetBundle );
		/// <summary>
		/// Obtain a <see cref="List{Asset}"/> of of all the <see
		/// cref="Asset"/>s in the given <see cref="AssetBundle"/>
		/// </summary>
		/// <param name="assetBundle"><see cref="AssetBundle"/> to read</param>
		/// <returns><see cref="List{Asset}"/> of of all the <see
		/// cref="Asset"/>s in <paramref name="assetBundle"/></returns>
		public List<Asset> GetAllAssets( AssetBundle assetBundle );
	}
}
