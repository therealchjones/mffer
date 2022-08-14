using System.Collections.Generic;

namespace Mffer {
	/// <summary>
	/// All obtainable items in the game
	/// </summary>
	public class Catalog : Component {
		/// <summary>
		/// A list of all available shops where items can be obtained
		/// </summary>
		List<Shop> Shops { get; set; }
		/// <summary>
		/// Initializes a new instance of the <see cref="Catalog"/> class
		/// </summary>
		public Catalog() : base() {
			Name = "Catalog";
			AddBackingData( "ExchangeItemDataList||text/data/exchange_item.asset" );
		}

		/// <summary>
		/// Object representing a specific shop or store in the <see cref="Game"/>
		/// </summary>
		class Shop : GameObject {
			/// <summary>
			/// A list of shops within this <see cref="Shop"/>
			/// </summary>
			List<Shop> SubShops { get; set; }
			/// <summary>
			/// A list of products available from this <see cref="Shop"/>
			/// </summary>
			List<ShopEntry> Products { get; set; }
			/// <summary>
			/// A single entry in a <see cref="Shop"/>, including one or more <see
			/// cref="Item"/>s and exchange or pricing data
			/// </summary>
			class ShopEntry : GameObject {
				/// <summary>
				/// A list of items within this product
				/// </summary>
				List<ItemBundle> Items { get; set; }
				ShopEntry() : base() {
					Items = new List<ItemBundle>();
				}
				/// <summary>
				/// An object representing a defined quantity of a specific <see
				/// cref="Item"/>
				/// </summary>
				class ItemBundle : GameObject {
					/// <summary>
					/// The specific item in this bundle
					/// </summary>
					Item item { get; set; }
					/// <summary>
					/// The number of <see cref="item"/>s in this bundle
					/// </summary>
					int quantity { get; set; }
				}
			}
		}
	}
}
