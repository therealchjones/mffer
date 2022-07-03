using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Mffer {
	/// <summary>
	/// Represents the settings and data for an individual user
	/// </summary>
	public class Player : GameObject {
		/// <summary>
		/// Gets or sets the unique unchanging identifier for the <see cref="Player"/>
		/// </summary>
		public long Id { get; set; }
		/// <summary>
		/// Gets or sets the <see cref="Player"/>-customizable in-game name
		/// </summary>
		public string Name { get; set; }
		/// <summary>
		/// Gets or sets the <see cref="Alliance"/> of which the
		/// <see cref="Player"/> is a member
		/// </summary>
		public Alliance alliance { get; set; }
		/// <summary>
		/// Gets or sets the most recent login time for the <see cref="Player"/>
		/// </summary>
		public DateTimeOffset LastLogin { get; set; }
		/// <summary>
		/// Gets or sets the list of <see cref="Character"/>s currently owned
		/// by the <see cref="Player"/>
		/// </summary>
		public List<MyCharacter> MyRoster { get; set; }
		/// <summary>
		/// Creates an instance of the <see cref="Player"/> class
		/// </summary>
		public Player() : base() {
		}
		/// <summary>
		/// Creates an instance of the <see cref="Player"/> class with data from the
		/// provided <see cref="JsonElement"/>
		/// </summary>
		/// <param name="json"><see cref="JsonElement"/> containing data about the <see cref="Player"/></param>
		public Player( JsonElement json ) : this() {
			Load( json );
		}
		/// <summary>
		/// Loads data from the provided <see cref="JsonElement"/> into the <see cref="Player"/> instance
		/// </summary>
		/// <param name="json"><see cref="JsonElement"/> containing data about the <see cref="Player"/></param>
		public override void Load( JsonElement json ) {
			if ( json.TryGetProperty( "llTime", out JsonElement loginTime )
				&& loginTime.TryGetInt64( out long thisLogin ) ) {
				LastLogin = DateTimeOffset.FromUnixTimeSeconds( thisLogin );
			}
			if ( json.TryGetProperty( "nick", out JsonElement tempJson ) )
				Name = Encoding.UTF8.GetString( Convert.FromBase64String( tempJson.GetString() ) );
			if ( json.TryGetProperty( "uID", out tempJson )
				&& tempJson.ValueKind == JsonValueKind.Number
				&& tempJson.TryGetInt64( out long tempLong ) )
				Id = tempLong;
		}
		/// <summary>
		/// Represents the settings and data for the current state of a
		/// <see cref="Character"/> owned by a <see cref="Player"/>
		/// </summary>
		public class MyCharacter : Character {
			/// <summary>
			/// Gets or sets the <see cref="Character"/> to which the settings in
			/// this <see cref="MyCharacter"/> instance apply
			/// </summary>
			public Character BaseCharacter { get; set; }
		}
	}
}
