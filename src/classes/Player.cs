using System.Collections.Generic;

namespace Mffer {
	/// <summary>
	/// Represents the settings and data for an individual user
	/// </summary>
	public class Player : Component {
		/// <summary>
		/// Gets or sets stored <see cref="PreferenceFile"/>s, indexed by
		/// date
		/// </summary>
		Dictionary<string, PreferenceFile> PreferenceFiles { get; set; }
		/// <summary>
		/// Gets or sets the <see cref="Alliance"/> of which the
		/// <see cref="Player"/> is a member
		/// </summary>
		public Alliance alliance { get; set; }
		/// <summary>
		/// Gets or sets the list of <see cref="Character"/>s currently owned
		/// by the <see cref="Player"/>
		/// </summary>
		public List<MyCharacter> MyRoster { get; set; }
		/// <summary>
		/// Represents the settings and data for the current state of a
		/// <see cref="Character"/> owned by a <see cref="Player"/>
		/// </summary>
		public class MyCharacter {
			/// <summary>
			/// Gets or sets the <see cref="Character"/> to whiche the settings in
			/// this <see cref="MyCharacter"/> instance apply
			/// </summary>
			public Character BaseCharacter { get; set; }
		}
	}
	/// <summary>
	/// Represents a <see cref="Player"/> alliance
	/// </summary>
	public class Alliance {
		/// <summary>
		/// Gets or sets the name of the <see cref="Alliance"/>
		/// </summary>
		public string Name { get; set; }
		/// <summary>
		/// Gets or sets the list of <see cref="Player"/>s in the
		/// <see cref="Alliance"/>
		/// </summary>
		public List<Player> Players { get; set; }
		/// <summary>
		/// Gets or sets the <see cref="Player"/> who is the leader of the
		/// <see cref="Alliance"/>
		/// </summary>
		public Player Leader { get; set; }
		/// <summary>
		/// Gets or sets the list of <see cref="Player"/>s who have Class 1
		/// status in the <see cref="Alliance"/>
		/// </summary>
		public List<Player> Class1Players { get; set; }
		/// <summary>
		/// Gets or sets the list of <see cref="Player"/>s who have Class 2
		/// status in the <see cref="Alliance"/>
		/// </summary>
		public List<Player> Class2Players { get; set; }
	}
}
