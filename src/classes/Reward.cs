namespace Mffer {
	/// <summary>
	/// Represents a reward given by this <see cref="Version"/> of the
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
}
