using System;
using System.Collections.Generic;

namespace Mffer {
	/// <summary>
	/// Represents the Future Pass <see cref="Component"/> of this
	/// <see cref="Version"/> of the <see cref="Game"/>
	/// </summary>
	/// <remarks>
	/// <see cref="FuturePass"/> is a recurring event in the <see cref="Game"/>
	/// made up of multiple tiers, with a <see cref="Reward"/> at each
	/// <see cref="FuturePassStep"/> from each tier. <see cref="StagePoints"/>
	/// are obtained through regular <see cref="Game"/> activities, and a
	/// given number of points (listed in <see cref="StagePoints"/>) is
	/// needed to reach each <see cref="FuturePassStep"/>.
	/// </remarks>
	public class FuturePass : Component {
		/// <summary>
		/// Gets or sets the different <see cref="FuturePassSeason"/>s
		/// </summary>
		/// <remarks>
		/// Each <see cref="FuturePassSeason"/> is a separate event with
		/// different start and end dates and rewards.
		/// </remarks>
		public List<FuturePassSeason> Seasons { get; set; }
		/// <summary>
		/// Gets or sets the list of <see cref="FuturePassStep"/>s indexed by
		/// step number
		/// </summary>
		public Dictionary<int, FuturePassStep> Steps { get; set; }
		/// <summary>
		/// Gets or sets the number of points needed to reach each
		/// <see cref="FuturePassStep"/>, indexed by step number
		/// </summary>
		public Dictionary<int, int> StagePoints { get; set; }
		/// <summary>
		/// Initializes a new instance of the <see cref="FuturePass"/> class
		/// </summary>
		public FuturePass() : base() {
			Seasons = new List<FuturePassSeason>();
			Steps = new Dictionary<int, FuturePassStep>();
			StagePoints = new Dictionary<int, int>();
			AddBackingAsset( "text/data/future_pass.asset" );
			AddBackingAsset( "text/data/future_pass_step.asset" );
			AddBackingAsset( "text/data/future_pass_reward.asset" );
			AddBackingAsset( "text/data/future_pass_contents.asset" );
		}
		/// <summary>
		/// Load data into this <see cref="FuturePass"/> instance
		/// </summary>
		/// <seealso cref="Component.Load()"/>
		public override void Load() {
			base.Load();
			foreach ( AssetObject seasonAsset in ( (AssetObject)BackingAssets["text/data/future_pass.asset"] ).Properties["list"].Array ) {
				FuturePassSeason season = new FuturePassSeason();
				season.Load( seasonAsset );
				Seasons.Add( season );
			}
			foreach ( AssetObject stepAsset in ( (AssetObject)BackingAssets["text/data/future_pass_step.asset"] ).Properties["list"].Array ) {
				FuturePassStep step = new FuturePassStep();
				step.passPoint = Int32.Parse( stepAsset.Properties["data"].Properties["passPoint"].String );
				step.step = Int32.Parse( stepAsset.Properties["data"].Properties["step"].String );
				step.Rewards = new Dictionary<FuturePassType, FuturePassReward>();
				Steps[step.step - 1] = step;
			}
			foreach ( AssetObject rewardAsset in ( (AssetObject)BackingAssets["text/data/future_pass_reward.asset"] ).Properties["list"].Array ) {
				FuturePassReward reward = new FuturePassReward();
				reward.Load( rewardAsset );
				FuturePassType level = (FuturePassType)Int32.Parse( rewardAsset.Properties["data"].Properties["grade"].String );
				int step = Int32.Parse( rewardAsset.Properties["data"].Properties["step"].String );
				Steps[step - 1].Rewards[level] = reward;
			}
			foreach ( AssetObject stageAsset in ( (AssetObject)BackingAssets["text/data/future_pass_contents.asset"] ).Properties["list"].Array ) {
				int sceneId = Int32.Parse( stageAsset.Properties["data"].Properties["sceneId"].String );
				int stagePoints = Int32.Parse( stageAsset.Properties["data"].Properties["passPoint"].String );
				StagePoints.Add( sceneId, stagePoints );
			}
		}
		/// <summary>
		/// Represents the <see cref="Reward"/> obtained from completing a
		/// <see cref="FuturePassStep"/>
		/// </summary>
		public class FuturePassReward : Reward {
			// text/data/future_pass_reward.asset->list->Array[x]->data
			/// <summary>
			/// The reward ID for this <see cref="FuturePassReward"/>
			/// </summary>
			private int rewardId;
			/// <summary>
			/// The reward group ID for this <see cref="FuturePassReward"/>
			/// </summary>
			private int rewardGroupId;
			/// <summary>
			/// Load data into this <see cref="FuturePassReward"/> instance
			/// </summary>
			/// <param name="asset">Asset containing
			/// <see cref="FuturePassReward"/> data</param>
			public void Load( AssetObject asset ) {
				this.rewardId = Int32.Parse( asset.Properties["data"].Properties["rewardId"].String );
				this.rewardGroupId = Int32.Parse( asset.Properties["data"].Properties["rewardGroupId"].String );
				this.Type = Int32.Parse( asset.Properties["data"].Properties["rewardType"].String );
				this.Value = Int32.Parse( asset.Properties["data"].Properties["rewardValue"].String );
			}
		}
		/// <summary>
		/// Represents a single <see cref="FuturePass"/> event
		/// </summary>
		public class FuturePassSeason {
			// text/data/future_pass.asset->list->Array[x]->data
			/// <summary>
			/// Gets or sets the end time of this
			/// <see cref="FuturePassSeason"/>
			/// </summary>
			string endTime { get; set; }
			/// <summary>
			/// Gets or sets the start time of this
			/// <see cref="FuturePassSeason"/>
			/// </summary>
			string startTime { get; set; }
			/// <summary>
			/// Gets or sets the reward group ID for this
			/// <see cref="FuturePassSeason"/>
			/// </summary>
			int rewardGroupId { get; set; }
			/// <summary>
			/// Loads data into this instance of <see cref="FuturePassSeason"/>
			/// </summary>
			/// <param name="asset">Asset containing
			/// <see cref="FuturePassSeason"/> data</param>
			public void Load( AssetObject asset ) {
				this.endTime = asset.Properties["data"].Properties["endTime_unused"].String;
				this.startTime = asset.Properties["data"].Properties["startTime_unused"].String;
				this.rewardGroupId = Int32.Parse( asset.Properties["data"].Properties["rewardGroupId"].String );
			}
		}
		/// <summary>
		/// Represents a single set of rewards in this
		/// <see cref="FuturePassSeason"/>
		/// </summary>
		public class FuturePassStep {
			// text/data/future_pass_step.asset->list->Array[x]->data
			/// <summary>
			/// Gets or sets the step number for this
			/// <see cref="FuturePassStep"/>
			/// </summary>
			public int step { get; set; } // 1-50
			/// <summary>
			/// Gets or sets the number of points needed to reach this
			/// <see cref="FuturePassStep"/>
			/// </summary>
			public int passPoint { get; set; }
			/// <summary>
			/// Gets or sets the <see cref="FuturePassReward"/> for reaching
			/// this <see cref="FuturePassStep"/>, indexed by
			/// <see cref="FuturePassType"/>
			/// </summary>
			public Dictionary<FuturePassType, FuturePassReward> Rewards { get; set; }
		}
		/// <summary>
		/// Specifies a tier (type) of rewards within a
		/// <see cref="FuturePassSeason"/>
		/// </summary>
		public enum FuturePassType {
			/// <summary>
			/// The free tier of <see cref="FuturePass"/>
			/// </summary>
			Normal,
			/// <summary>
			/// The middle tier of <see cref="FuturePass"/>
			/// </summary>
			Legendary,
			/// <summary>
			/// The top tier of <see cref="FuturePass"/>
			/// </summary>
			Mythic
		}
	}
}
