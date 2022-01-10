using System.Collections.Generic;

namespace Mffer {
	public static class AddExtensions {
		public static void Add( this List<Alliance> list, string allianceName ) {
			Alliance alliance = new Alliance( allianceName );
			list.Add( alliance );
		}
	}
}
