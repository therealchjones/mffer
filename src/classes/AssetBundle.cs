using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

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
		/// <summary>
		/// Gets or sets the <see cref="IAssetReader"/> used to access this <see
		/// cref="AssetBundle"/>
		/// </summary>
		[JsonIgnore]
		internal IAssetReader assetReader { get; set; }
		/// <summary>
		/// Gets or sets the path of the file storing this <see
		/// cref="AssetBundle"/>'s data
		/// </summary>
		public string Path { get; set; }
		/// <summary>
		/// Dictionary of <see cref="Asset"/>s in this <see cref="AssetBundle"/>
		/// </summary>
		/// <remarks>
		/// This dictionary is not guaranteed to have all the <see
		/// cref="Asset"/>s that are in the <see cref="AssetBundle"/> on disk
		/// and should seldom be used directly; access via the <see
		/// cref="AssetBundle.GetAsset(string)"/> or <see
		/// cref="AssetBundle.GetAllAssets()"/> methods instead.
		/// </remarks>
		internal SortedDictionary<string, Asset> Assets { get; set; }
		/// <summary>
		/// Creates an instance of an <see cref="AssetBundle"/> associated with
		/// the given <see cref="IAssetReader"/>
		/// </summary>
		/// <param name="reader">The <see cref="IAssetReader"/> used to access the new <see cref="AssetBundle"/></param>
		/// <exception cref="ArgumentNullException">if the parameter is <c>null</c></exception>
		internal AssetBundle( IAssetReader reader ) : base() {
			if ( reader is null ) throw new ArgumentNullException( nameof( reader ) );
			assetReader = reader;
			Assets = new();
		}
		/// <summary>
		/// Loads the data for all <see cref="Asset"/>s included in this <see
		/// cref="AssetBundle"/>
		/// </summary>
		public override void LoadAll() {
			assetReader.GetAllAssets( this );
		}
		/// <summary>
		/// Reports whether this <see cref="AssetBundle"/> includes an <see
		/// cref="Asset"/> with the given name
		/// </summary>
		/// <remarks>
		/// This method does not report whether data for the requested <see
		/// cref="Asset"/> has been loaded into the instance, only whether the
		/// <see cref="Asset"/> is among those accessible within the <see
		/// cref="AssetBundle"/>
		/// </remarks>
		/// <param name="name"><see cref="String"/> naming the desired <see
		/// cref="Asset"/></param>
		/// <returns><c>true</c> if an <see cref="Asset"/> named <paramref
		/// name="name"/> is included in this <see cref="AssetBundle"/>,
		/// <c>false</c> otherwise</returns>
		/// <exception cref="ArgumentNullException"> if <paramref name="name"/>
		/// is null or empty</exception>
		public bool Contains( string name ) {
			if ( String.IsNullOrEmpty( name ) ) throw new ArgumentNullException( nameof( name ) );
			if ( Assets.ContainsKey( name ) ) return true;
			GetAllAssetNames();
			return Assets.ContainsKey( name );
		}
		/// <summary>
		/// Obtains a list of the names of all <see cref="Assets"/>s included in
		/// this <see cref="AssetBundle"/>
		/// </summary>
		/// <remarks>
		/// This method does not report whether data for the listed <see
		/// cref="Asset"/>s has been loaded into the instance, only which <see
		/// cref="Asset"/>s are accessible within the <see cref="AssetBundle"/>
		/// </remarks>
		/// <returns><see cref="List{String}"/> containing all <see
		/// cref="Asset"/> names accessible within this <see
		/// cref="AssetBundle"/></returns>
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
		/// <param name="name">Name of the <see cref="Asset"/> to obtain</param>
		/// <returns>The asset named <paramref name="name"/></returns>
		public Asset GetAsset( string name ) {
			if ( String.IsNullOrEmpty( name ) ) throw new ArgumentNullException( nameof( name ) );
			if ( !Assets.ContainsKey( name ) || Assets[name] is null || Assets[name].Value is null ) Assets[name] = assetReader.GetAsset( name, this );
			return Assets[name];
		}
		/// <summary>
		/// Obtains all data accessible within this <see cref="AssetBundle"/> as
		/// a <see cref="List{Asset}"/> of <see cref="Asset"/>s
		/// </summary>
		/// <returns><see cref="List{Asset}"/> of all assets included in this
		/// <see cref="AssetBundle"/>, with all their data fully
		/// loaded</returns>
		public List<Asset> GetAllAssets() {
			GetAllAssetNames();
			foreach ( string key in Assets.Keys ) {
				if ( Assets[key] is null || Assets[key].Value is null ) {
					Assets[key] = GetAsset( key );
				}
			}
			return Assets.Values.ToList();
		}
	}
}
