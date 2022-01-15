using System;
using System.Collections.Generic;
using System.Text;
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
		/// <summary>
		/// Gets or sets the unique unchanging identifier for the <see cref="Alliance"/>
		/// </summary>
		public long Id { get; set; }
		/// <summary>
		/// Gets or sets the name of the <see cref="Alliance"/>
		/// </summary>
		public string Name { get; set; }
		/// <summary>
		/// Gets or sets whether the <see cref="Alliance"/> is public
		/// </summary>
		public bool IsPublic { get; set; }
		/// <summary>
		/// Gets or sets the level of the <see cref="Alliance"/>
		/// </summary>
		public int Level { get; set; }
		/// <summary>
		/// Gets or sets the level of the <see cref="Alliance"/>'s shop
		/// </summary>
		public int ShopLevel { get; set; }
		/// <summary>
		/// Gets or sets the level required for a <see cref="Player"/> to join the <see cref="Alliance"/>
		/// </summary>
		public int RequiredLevel { get; set; }
		/// <summary>
		/// Gets or sets the maximum number of <see cref="Player"/>s that can be in the <see cref="Alliance"/>
		/// </summary>
		public int MaxMembers { get; set; }
		/// <summary>
		/// Gets or sets the list of <see cref="Player"/>s in the <see cref="Alliance"/>
		/// </summary>
		public List<Player> Players {
			get {
				if ( Value.GetType().IsGenericType
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
		/// <summary>
		/// Gets or sets the time <see cref="Alliance"/> data was last obtained
		/// from the servers
		/// </summary>
		public DateTimeOffset LastUpdateTime { get; set; }
		/// <summary>
		/// Gets or sets the most recent time an <see cref="Alliance"/> member
		/// logged into the game
		/// </summary>
		public DateTimeOffset LastLoginTime {
			get {
				DateTimeOffset lastLogin = new DateTimeOffset();
				foreach ( Player player in Players ) {
					if ( player.LastLogin > lastLogin ) lastLogin = player.LastLogin;
				}
				return lastLogin;
			}
		}
		/// <summary>
		/// Gets or sets the weekly points contributed by <see cref="Alliance"/>
		/// members. This number resets to 0 at the time of the weekly reset at
		/// 0100 Friday UTC.
		/// </summary>
		public int WeeklyExperience { get; set; }
		/// <summary>
		/// Creates a new instance of an <see cref="Alliance"/> object
		/// </summary>
		public Alliance() : base() {
			Players = new List<Player>();
			MaxMembers = 40; // for now; should get real number by level after importing appropriate asset table
		}
		/// <summary>
		/// Creates a new instance of the <see cref="Alliance"/> class with
		/// the given name
		/// </summary>
		/// <param name="allianceName">the <see cref="Name"/> of the <see cref="Alliance"/> to create</param>
		/// <exception cref="ArgumentNullException">Thrown if the <paramref name="allianceName"/> is null or the empty string</exception>
		public Alliance( string allianceName ) : this() {
			if ( String.IsNullOrEmpty( allianceName ) ) throw new ArgumentNullException( allianceName );
			Name = allianceName;
		}
		/// <summary>
		/// Creates a new instance of the <see cref="Alliance"/> class with the given <see cref="Name"/>
		/// and <see cref="Id"/>
		/// </summary>
		/// <param name="allianceId">the <see cref="Id"/> of the <see cref="Alliance"/> to create</param>
		/// <param name="allianceName">the <see cref="Name"/> of the <see cref="Alliance"/> to create</param>
		public Alliance( long allianceId, string allianceName ) : this( allianceName ) {
			Id = allianceId;
		}
		/// <summary>
		/// Creates a new instance of the <see cref="Alliance"/> class with data from the given <see cref="JsonElement"/>
		/// </summary>
		/// <param name="json"><see cref="JsonElement"/> containing data about the <see cref="Alliance"/> to create</param>
		public Alliance( JsonElement json ) : this() {
			Load( json );
		}
		/// <summary>
		/// Loads data from the given <see cref="JsonElement"/> into the <see cref="Alliance"/> instance
		/// </summary>
		/// <param name="json"><see cref="JsonElement"/> containing data about the <see cref="Alliance"/></param>
		public override void Load( JsonElement json ) {
			if ( json.TryGetProperty( "now", out JsonElement serverTime )
				&& serverTime.ValueKind == JsonValueKind.Number
				&& serverTime.TryGetInt64( out long updateTime ) ) {
				LastUpdateTime = DateTimeOffset.FromUnixTimeSeconds( updateTime );
			}
			if ( json.TryGetProperty( "desc", out JsonElement desc ) )
				json = desc;
			if ( json.TryGetProperty( "mems", out JsonElement mems )
				&& mems.ValueKind == JsonValueKind.Array ) {
				List<Player> newPlayerList = new List<Player>();
				foreach ( JsonElement player in mems.EnumerateArray() ) {
					Player newPlayer = new Player( player );
					if ( newPlayer.Id != default ) {
						newPlayerList.Add( newPlayer );
					}
				}
				if ( newPlayerList.Count > 0 ) Players = newPlayerList;
			}
			if ( json.TryGetProperty( "pgld", out JsonElement pgld ) )
				json = pgld;
			if ( json.TryGetProperty( "gname", out JsonElement gname )
				&& gname.ValueKind == JsonValueKind.String )
				Name = Encoding.UTF8.GetString( Convert.FromBase64String( gname.GetString() ) );
			if ( json.TryGetProperty( "guID", out JsonElement guID )
				&& guID.ValueKind == JsonValueKind.Number
				&& guID.TryGetInt64( out long id ) )
				Id = id;
			if ( json.TryGetProperty( "wExp", out JsonElement wExp )
				&& wExp.ValueKind == JsonValueKind.Number
				&& wExp.TryGetInt32( out int experience ) )
				WeeklyExperience = experience;
			if ( json.TryGetProperty( "autoJoinYN", out JsonElement publicJson )
				&& publicJson.ValueKind == JsonValueKind.Number
				&& publicJson.TryGetInt32( out int publicNumber ) ) {
				if ( publicNumber == 1 ) IsPublic = true;
				else IsPublic = false;
			}
			if ( json.TryGetProperty( "glv", out JsonElement levelJson )
				&& levelJson.ValueKind == JsonValueKind.Number
				&& levelJson.TryGetInt32( out int level ) )
				Level = level;
			if ( json.TryGetProperty( "sLv", out levelJson )
				&& levelJson.ValueKind == JsonValueKind.Number
				&& levelJson.TryGetInt32( out level ) )
				ShopLevel = level;
			if ( json.TryGetProperty( "lvt", out levelJson )
				&& levelJson.ValueKind == JsonValueKind.Number
				&& levelJson.TryGetInt32( out level ) )
				RequiredLevel = level;
		}
		/// <summary>
		/// Determines the number of days since an <see cref="Alliance"/> member last
		/// logged in
		/// </summary>
		/// <returns>the number of days since the last login</returns>
		public double GetDaysInactive() {
			TimeSpan timeSinceLogin = DateTimeOffset.UtcNow - LastLoginTime;
			return timeSinceLogin.TotalDays;
		}
	}
}
