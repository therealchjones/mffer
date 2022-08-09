namespace Mffer {
	/// <summary>
	/// Represents a single asset used by Marvel Future Fight
	/// </summary>
	/// <remarks>As Unity's Asset class is primarily for using an asset within
	/// the Unity Editor, this class is modeled on the needs for <see
	/// cref="Asset"/> use within games and in the mffer project. The class
	/// includes members used to access and manipulate data from Unity assets.
	/// </remarks>
	public class Asset : GameObject {
		/// <summary>
		/// Gets or sets the <see cref="Asset"/>'s path ID
		/// </summary>
		public long PathID { get; set; }
		/// <summary>
		/// Gets or sets the name of this <see cref="Asset"/>
		/// </summary>
		public string Name { get; set; }
		/// <summary>
		/// Initialize a new <see cref="Asset"/> instance with the given
		/// <paramref name="pathID"/>
		/// </summary>
		/// <param name="pathID">Path ID of the asset</param>
		public Asset( long pathID ) : base() {
			PathID = pathID;
		}
	}
}
