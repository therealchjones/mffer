using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Mffer {
	/// <summary>
	/// Represents the settings and data for an individual user
	/// </summary>
	public class Player : GameObject {
		DateTimeOffset LastUpdated { get; set; }
		public long Id { get; set; }
		public string Name { get; set; }
		/// <summary>
		/// Gets or sets the <see cref="Alliance"/> of which the
		/// <see cref="Player"/> is a member
		/// </summary>
		public Alliance alliance { get; set; }
		public DateTimeOffset LastLogin { get; set; }
		/// <summary>
		/// Gets or sets the list of <see cref="Character"/>s currently owned
		/// by the <see cref="Player"/>
		/// </summary>
		public List<MyCharacter> MyRoster { get; set; }
		public Player() : base() {

		}
		public Player( JsonElement json ) : this() {
			Load( json );
		}
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
