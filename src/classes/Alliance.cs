using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Mffer {
	/// <summary>
	/// Represents a <see cref="Player"/> alliance
	/// </summary>
	/// <remarks>
	/// An <see cref="Alliance"/> is a group of <see cref="Player"/>s with
	/// various other properties. The <see cref="Alliance"/> class includes
	/// these properties and methods to evaluate and manipulate them.
	/// </remarks>
	public class Alliance : GameObject {
		public long Id { get; set; }
		/// <summary>
		/// Gets or sets the name of the <see cref="Alliance"/>
		/// </summary>
		public string Name { get; set; }
		public List<Player> Players {
			get {
				if ( Value.GetType().IsGenericType()
					&& Value.GetType().GetGenericTypeDefinition() == typeof( List<> )
					&& typeof( Player ).IsAssignableFrom( Value.GetType().GenericTypeArguments[0] ) ) {
					return Value;
				} else if ( Value is null ) {
					return null;
				} else {
					throw new InvalidOperationException( "Property 'Players' is not an object of type 'List<Player>'" );
				}
			}
			set {
				Value = value;
			}
		}
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
		public double DaysInactive {
			get {
				DateTimeOffset lastLogin = new DateTimeOffset();
				foreach ( Player player in Players ) {
					if ( player.LastLogin > lastLogin ) lastLogin = player.LastLogin;
				}
				TimeSpan timeSinceLogin = DateTimeOffset.UtcNow - lastLogin;
				return timeSinceLogin.TotalDays;
			}
		}
		public DateTimeOffset LastUpdateTime { get; set; }
		public DateTimeOffset LastLoginTime { get; set; }
		public int WeeklyExperience { get; set; }
		/// <summary>
		/// Creates a new instance of an <see cref="Alliance"/> object
		/// </summary>
		public Alliance() : base() {
			Players = new List<Player>();
		}
		public Alliance( string allianceName ) : this() {
			if ( String.IsNullOrEmpty( allianceName ) ) throw new ArgumentNullException( allianceName );
			Name = allianceName;
		}
		public Alliance( long allianceId, string allianceName ) : this( allianceName ) {
			Id = allianceId;
		}
		void Load( JsonElement json ) {
			if ( json.TryGetProperty( "now", out JsonElement serverTime )
				&& serverTime.ValueKind == JsonValueKind.Number
				&& serverTime.TryGetInt64( out long updateTime ) ) {
				LastUpdateTime = DateTimeOffset.FromUnixTimeSeconds( updateTime );
			}
			JsonElement pgld = new JsonElement();
			if ( json.TryGetProperty( "desc", out JsonElement desc ) ) {
				if ( desc.TryGetProperty( "pgld", out pgld )
					&& pgld.TryGetProperty( "guID", out JsonElement guID )
					&& guID.ValueKind == JsonValueKind.Number
					&& guID.TryGetInt64( out long id ) )
					Id = id;
				JsonElement mems = new JsonElement();
				if ( desc.TryGetProperty( "mems", out mems )
					|| ( desc.TryGetProperty( "pgld", out pgld ) )
						&& pgld.TryGetProperty( "mems", out mems ) ) {
					if ( mems.ValueKind == JsonValueKind.Array ) {
						List<Player> newPlayerList = new List<Player>();
						foreach ( JsonElement player in mems.EnumerateArray() ) {
							Player newPlayer = new Player( player );
							if ( newPlayer.Id != default ) {
								newPlayerList.Add( newPlayer );
							}
						}
						if ( newPlayerList.Count > 0 ) Players = newPlayerList;
					}
				}
			}
		}

	}
}
