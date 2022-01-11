using System.Collections.Generic;

namespace Mffer {
	/// <summary>
	/// Extensions to the Add() method
	/// </summary>
	public static class AddExtensions {
		/// <summary>
		/// Add a new <see cref="Alliance"/> instance to a <see cref="System.Collections.Generic.List{T}"/> using only an alliance name
		/// </summary>
		/// <param name="list">the list to which to add the new <see cref="Alliance"/></param>
		/// <param name="allianceName">the name of the new <see cref="Alliance"/> to create and add</param>
		public static void Add( this List<Alliance> list, string allianceName ) {
			Alliance alliance = new Alliance( allianceName );
			list.Add( alliance );
		}
	}
}
