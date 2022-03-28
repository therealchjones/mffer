using System;
using AssetsTools.Dynamic;

namespace Mffer {
	/// <summary>
	/// Extensions to other classes
	/// </summary>
	public static class Extensions {
		/// <summary>
		/// Calculates the length of a <see cref="DynamicAssetArray"/>
		/// </summary>
		/// <param name="array"><see cref="DynamicAssetArray"/> to measure</param>
		/// <returns>The length of the array</returns>
		public static int Count( this DynamicAssetArray array ) {
			int length = 0;
			dynamic foo = null;
			try {
				while ( true ) {
					foo = array[length];
					length++;
				}
			} catch ( IndexOutOfRangeException ) {
				return length;
			}
		}
	}
}
