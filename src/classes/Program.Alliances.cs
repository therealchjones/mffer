using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Mffer {
	partial class Program {
		/// <summary>
		/// <see cref="Program"/> properties and methods for user-facing work with <see cref="Alliance"/>s
		/// </summary>
		static class Alliances {
			/// <summary>
			/// Update a list of inactive alliances
			/// </summary>
			public static void GetProspectiveAlliances() {
				FileInfo alliancesFile = new FileInfo( "alliances.json" );
				List<Alliance> importedAlliances = new List<Alliance>();
				JsonSerializerOptions jsonOptions = new JsonSerializerOptions();
				jsonOptions.Converters.Add( new GameObjectJsonConverter() );
				if ( alliancesFile.Exists ) {
					String fileContents = File.ReadAllText( alliancesFile.FullName );
					JsonDocument jsonDocument = JsonDocument.Parse( fileContents );
					JsonElement jsonList = jsonDocument.RootElement.GetProperty( "Value" );
					String jsonListString = jsonList.ToString();
					importedAlliances = (List<Alliance>)JsonSerializer.Deserialize( jsonListString, typeof( List<Alliance> ), jsonOptions );
				}
				int startSize = importedAlliances.Count;
				int endSize = startSize;
				if ( startSize > 0 ) {
					Console.WriteLine( $"Checking {startSize} monitored alliances" );
					importedAlliances = CheckProspectiveAlliances( importedAlliances );
					endSize = importedAlliances.Count;
					if ( endSize < startSize ) Console.WriteLine( $"{startSize - endSize} alliances had activity or no longer exist and were discarded." );
					if ( endSize > 0 ) Console.WriteLine( $"{endSize} alliances will continue to be monitored." );
				}
				List<Alliance> newAlliances = new List<Alliance>();
				if ( endSize < 100 ) {
					int searchSize = ( 100 - endSize );
					Console.Write( "Finding new alliances to monitor" );
					Console.WriteLine( "... this will take a little time." );
					newAlliances = FindProspectiveAlliances( searchSize );
					if ( newAlliances.Count > 0 ) {
						Console.WriteLine( $"Found {newAlliances.Count} new alliances to monitor." );
					} else {
						Console.WriteLine( "No new alliances identified." );
					}
				}
				newAlliances.AddRange( importedAlliances );
				if ( newAlliances.Count > 0 ) {
					List<long> allianceIds = new List<long>();
					List<Alliance> prospectiveAlliances = new List<Alliance>();
					foreach ( Alliance alliance in newAlliances ) {
						if ( !allianceIds.Contains( alliance.Id ) ) {
							allianceIds.Add( alliance.Id );
							prospectiveAlliances.Add( alliance );
						}
					}
					if ( prospectiveAlliances.Count < newAlliances.Count )
						Console.WriteLine( $"Removing {newAlliances.Count - prospectiveAlliances.Count} duplicates" );
					prospectiveAlliances.Sort( delegate ( Alliance x, Alliance y ) {
						if ( x.LastLoginTime == default ) {
							if ( y.LastLoginTime == default ) return 0;
							else return 1;
						}
						if ( y.LastLoginTime == default ) return -1;
						if ( x.LastLoginTime < y.LastLoginTime ) return 1;
						if ( x.LastLoginTime == y.LastLoginTime ) return 0;
						return -1;
					} );
					prospectiveAlliances.Reverse();
					Console.WriteLine( "Currently monitored alliances:" );
					int currentDays = -1;
					foreach ( Alliance alliance in prospectiveAlliances ) {
						double daysInactive = alliance.GetDaysInactive();
						if ( currentDays == -1 || daysInactive < currentDays ) {
							currentDays = (int)daysInactive;
							Console.WriteLine( $"Inactive more than {currentDays} days:" );
						}
						Console.WriteLine( $"{alliance.Name}: level {alliance.Level}, shop level {alliance.ShopLevel}, {alliance.Players.Count} members, inactive {daysInactive} days." );
					}
					GameObject alliances = prospectiveAlliances.ToGameObject();
					JsonWriterOptions writeOptions = new JsonWriterOptions() { Indented = true, SkipValidation = true };
					using ( Stream file = new FileStream( alliancesFile.FullName, FileMode.Create ) )
						alliances.ToJson( file, jsonOptions, writeOptions );
				} else {
					Console.WriteLine( "No alliances to monitor." );
					alliancesFile.Delete();
				}
			}
			/// <summary>
			/// Check a list of <see cref="Alliance"/>s for new activity
			/// </summary>
			/// <remarks>
			/// Given a <see cref="List{Alliance}"/> of <see cref="Alliance"/>s,
			/// <see cref="CheckProspectiveAlliances"/> checks each for logins by the
			/// <see cref="Alliance"/>'s <see cref="Player"/>s that have occurred
			/// since the last time the <see cref="Alliance"/> was checked, and
			/// checks for any weekly experience points. (<see cref="Alliance"/>s
			/// that cannot be found are discarded.) If there are neither, the
			/// <see cref="Alliance"/> is added to a new <see
			/// cref="List{Alliance}"/> to be returned.
			/// </remarks>
			/// <param name="alliances">The <see cref="List{Alliance}"/> of <see
			/// cref="Alliance"/>s to check</param>
			/// <returns>A new <see cref="List{Alliance}"/> of the <see
			/// cref="Alliance"/>s that have had no activity since last checked</returns>
			static public List<Alliance> CheckProspectiveAlliances( List<Alliance> alliances ) {
				List<Alliance> newList = new List<Alliance>();
				DateTimeOffset now = DateTimeOffset.Now;
				TimeSpan sinceLastCheck;
				foreach ( Alliance alliance in alliances ) {
					if ( alliance.LastUpdateTime != default ) {
						sinceLastCheck = now - alliance.LastUpdateTime;
					} else {
						sinceLastCheck = now - now;
					}
					if ( !NetworkData.TryGetAllianceData( alliance )
						|| alliance.WeeklyExperience != 0
						|| alliance.Players.Count >= alliance.MaxMembers )
						continue;
					if ( alliance.LastLoginTime == default
						|| now - alliance.LastLoginTime > sinceLastCheck )
						newList.Add( alliance );
				}
				return newList;
			}
			/// <summary>
			/// Find available alliances
			/// </summary>
			/// <remarks>
			/// Rework/reimplementation of PacketTransfer.GetRecommendedAllianceList()
			/// and PacketTransfer.GetRecommendedAllianceListOk()
			/// </remarks>
			static List<Alliance> FindInactiveAlliances( int alliancesToFind, int daysInactive ) {
				int pulledAlliances = 0;
				List<string> checkedAllianceNames = new List<string>();
				List<Alliance> qualifyingAlliances = new List<Alliance>();
				double allianceDaysInactive = 0;
				while ( qualifyingAlliances.Count < alliancesToFind ) {
					List<Alliance> alliances = NetworkData.FindSuggestedAlliances();
					foreach ( Alliance alliance in alliances ) {
						pulledAlliances++;
						if ( checkedAllianceNames.Contains( alliance.Name ) ) continue;
						else checkedAllianceNames.Add( alliance.Name );
						if ( alliance.WeeklyExperience > 0
							|| alliance.IsPublic == false )
							continue;
						if ( !NetworkData.TryGetAllianceData( alliance )
							|| alliance.Players.Count >= alliance.MaxMembers )
							continue;
						allianceDaysInactive = alliance.GetDaysInactive();
						if ( allianceDaysInactive > daysInactive ) qualifyingAlliances.Add( alliance );
					}
				}
				return qualifyingAlliances;
			}

			/// <summary>
			/// Obtain a list of <see cref="Alliance"/>s without weekly activity
			/// </summary>
			/// <remarks>
			/// <see cref="FindProspectiveAlliances(int)"/> attempts to identify
			/// a number of alliances without any weekly contribution points.
			/// (Weekly contribution points reset to 0 at the weekly reset at
			/// 0100 Friday UTC.) Alliances are obtained from Netmarble servers
			/// without an obvious order or meeting configurable properties, and
			/// include previously returned alliances, so increasing the number
			/// of alliances to be sought can increase the number of alliances
			/// which need to be searched far more. Once at least
			/// <see paramref="toSearch"/> alliances without weekly
			/// contribution points are found, they are returned in a
			/// <see cref="List{Alliance}"/>.
			/// </remarks>
			/// <param name="toSearch">the number of <see cref="Alliance"/>s to
			/// find</param>
			/// <returns></returns>
			public static List<Alliance> FindProspectiveAlliances( int toSearch ) {
				return FindInactiveAlliances( toSearch, 0 );
			}
		}
	}
}
