using System;
using System.Collections.Generic;

namespace Mffer {
	/// <summary>
	/// Represents the Shadowland <see cref="Component"/> of this
	/// <see cref="Version"/> of the <see cref="Game"/>
	/// </summary>
	public class Shadowland : Component {

		/// <summary>
		/// Gets or sets the list of <see cref="ShadowlandFloor"/>s upon which
		/// <see cref="Shadowland"/> is based
		/// </summary>
		ShadowlandFloor[] BaseFloors;
		/// <summary>
		/// Initializes a new instance of the <see cref="Shadowland"/> class
		/// </summary>
		public Shadowland() : base() {
			Name = "Shadowland";
			BaseFloors = new ShadowlandFloor[35];
			AddBackingData( "text/data/shadowland_floor.csv" );
			AddBackingData( "text/data/shadowland_reward.csv" );
		}
		/// <summary>
		/// Loads this <see cref="Shadowland"/> instance
		/// </summary>
		/// <seealso cref="Component.Load()"/>
		public override void Load() {
			base.Load();
			dynamic shadowlandFloors = ( (Asset)BackingData["text/data/shadowland_floor.csv"] ).Value["m_Script"];
			dynamic shadowlandRewards = ( (Asset)BackingData["text/data/shadowland_reward.csv"] ).Value["m_Script"];
			for ( int floorNum = 0; floorNum < BaseFloors.Length; floorNum++ ) {
				ShadowlandFloor floor = new ShadowlandFloor();
				floor.FloorNumber = floorNum + 1;
				floor.BaseFloor = floor;
				foreach ( dynamic value in shadowlandRewards ) {
					if ( value.Properties["REWARD_GROUP"].String == shadowlandFloors[floorNum].Properties["REWARD_GROUP"].String ) {
						List<ShadowlandReward> rewards = new List<ShadowlandReward>();
						for ( int i = 1; i <= 2; i++ ) {
							ShadowlandReward reward = new ShadowlandReward();
							reward.Value = Int32.Parse( value.Properties[$"REWARD_VALUE_{i}"].String );
							reward.Quantity = Int32.Parse( value.Properties[$"REWARD_QTY_{i}"].String );
							reward.Type = Int32.Parse( value.Properties[$"REWARD_TYPE_{i}"].String );
							rewards[i] = reward;
						}
					}
					floor.RewardGroup = Int32.Parse( shadowlandFloors[floorNum].Properties["REWARD_GROUP"].String );
					floor.StageGroup = Int32.Parse( shadowlandFloors[floorNum].Properties["STAGE_GROUP"].String );
					floor.StageSelectCount = Int32.Parse( shadowlandFloors[floorNum].Properties["STAGE_SELECT_COUNT"].String );
					BaseFloors[floorNum] = floor;
				}
			}
		}
		/// <summary>
		/// Represents a single floor of the <see cref="Shadowland"/> component
		/// </summary>
		public class ShadowlandFloor {
			/// <summary>
			/// Gets or sets the number of this <see cref="ShadowlandFloor"/>
			/// </summary>
			public int FloorNumber { get; set; }
			/// <summary>
			/// Gets or sets the floor upo which this
			/// <see cref="ShadowlandFloor"/> is based
			/// </summary>
			public ShadowlandFloor BaseFloor { get; set; }
			/// <summary>
			/// Gets or sets the reward group for this
			/// <see cref="ShadowlandFloor"/>
			/// </summary>
			public int RewardGroup { get; set; }
			/// <summary>
			/// Gets or sets the stage group of this
			/// <see cref="ShadowlandFloor"/>
			/// </summary>
			public int StageGroup { get; set; }
			/// <summary>
			/// Gets or sets the stage select count of this
			/// <see cref="ShadowlandFloor"/>
			/// </summary>
			public int StageSelectCount { get; set; }
		}
		/// <summary>
		/// Represents a reward given for completion of a
		/// <see cref="ShadowlandFloor"/>
		/// </summary>
		public class ShadowlandReward : Reward {
		}
	}

}
