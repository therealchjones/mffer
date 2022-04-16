using System;
using System.Collections.Generic;
using System.Linq;

namespace Mffer {
	/// <summary>
	/// AssetBundle class abstracted from the  specific tools to access asset data
	/// </summary>
	/// <remarks>
	/// <para>AssetBundle files contain multiple <see cref="Asset"/>s. The <see
	/// cref="AssetBundle"/> class includes methods for loading the data from the
	/// filesystem and outputting the data in JSON format.</para>
	/// <para>Modeled after but the official public class
	/// specification for the AssetBundle class at
	/// https://docs.unity3d.com/ScriptReference/AssetBundle.html, but limited
	/// to those members expected to be useful in this project.</para>
	/// </remarks>
	public class AssetBundle : GameObject {
		internal IAssetReader assetReader { get; set; }
		public string Path { get; set; }
		public string Name { get; set; }
		/// <summary>
		/// List of <see cref="Asset"/>s in this <see cref="AssetBundle"/>
		/// </summary>
		/// <remarks>
		/// This dictionary is not guaranteed to have all the <see
		/// cref="Asset"/>s that are in the <see cref="AssetBundle"/> on disk
		/// and should seldom be used directly; access via the <see
		/// cref="AssetBundle.GetAsset(string)"/> or <see
		/// cref="AssetBundle.GetAllAssets()"/> methods instead.
		/// </remarks>
		internal Dictionary<string, Asset> Assets { get; set; }
		internal AssetBundle( IAssetReader reader ) : base() {
			if ( reader is null ) throw new ArgumentNullException( nameof( reader ) );
			assetReader = reader;
			Assets = new();
		}
		public override void LoadAll() {
			base.LoadAll();
			assetReader.GetAllAssets( this );
		}
		public bool Contains( string name ) {
			if ( String.IsNullOrEmpty( name ) ) throw new ArgumentNullException( nameof( name ) );
			if ( Assets.ContainsKey( name ) ) return true;
			GetAllAssetNames();
			return Assets.ContainsKey( name );
		}
		public List<string> GetAllAssetNames() {
			List<string> assetNames = assetReader.GetAllAssetNames( this );
			foreach ( string name in assetNames ) {
				if ( !Assets.ContainsKey( name ) ) Assets.Add( name, null );
			}
			return assetNames;
		}
		/// <summary>
		/// Retrieves the <see cref="Asset"/> of the given name
		/// </summary>
		/// <param name="assetName">Name of the <see cref="Asset"/> to obtain</param>
		/// <returns>The asset named <paramref name="assetName"/></returns>
		public Asset GetAsset( string name ) {
			if ( String.IsNullOrEmpty( name ) ) throw new ArgumentNullException( nameof( name ) );
			if ( !Assets.ContainsKey( name ) || Assets[name] is null || Assets[name].Value is null ) Assets[name] = assetReader.GetAsset( name, this );
			return Assets[name];
		}
		public List<Asset> GetAllAssets() {
			GetAllAssetNames();
			foreach ( KeyValuePair<string, Asset> entry in Assets ) {
				if ( entry.Value is null ) {
					Assets[entry.Key] = GetAsset( entry.Key );
				}
			}
			return Assets.Values.ToList();
		}
	}
}
