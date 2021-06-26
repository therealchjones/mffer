using System;
using AssetsTools.Dynamic;

namespace Mffer {
	/// <summary>
	/// Represents a single item
	/// </summary>
	public class Item : GameObject {
		/// <summary>
		/// Gets or sets the name of the <see cref="Item"/>
		/// </summary>
		public string Name { get; set; }
	}
	/// <summary>
	/// Represents a reward given by this <see cref="Game.Version"/> of the
	/// <see cref="Game"/>
	/// </summary>
	public class Reward {
		/// <summary>
		/// Gets or sets the <see cref="Item"/> in this <see cref="Reward"/>
		/// </summary>
		public Item item { get; set; }
		/// <summary>
		/// Gets or sets the quantity of the <see cref="Item"/> in this
		/// <see cref="Reward"/>
		/// </summary>
		public int Quantity { get; set; }
		/// <summary>
		/// Gets or sets the Value of the <see cref="Item"/> in this
		/// <see cref="Reward"/>
		/// </summary>
		public int Value { get; set; }
		/// <summary>
		/// Gets or sets the type of the type of this <see cref="Reward"/>
		/// </summary>
		public int Type { get; set; }
	}
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
