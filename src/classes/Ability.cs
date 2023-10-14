using System;

namespace Mffer {
	/// <summary>
	/// Represents a skill (ability) available for a <see cref="Character"/>
	/// </summary>
	public class Ability {
		/// <summary>
		/// Gets or sets the ability group ID for this
		/// <see cref="Ability"/>
		/// </summary>
		public int groupId { get; set; }
		/// <summary>
		/// Gets or sets the ability ID for this <see cref="Ability"/>
		/// </summary>
		public int abilityId { get; set; }
		/// <summary>
		/// Gets or sets the time of action for this <see cref="Ability"/>
		/// </summary>
		public long time { get; set; }
		/// <summary>
		/// Gets or sets the "tick" for this <see cref="Ability"/>
		/// </summary>
		public long tick { get; set; }
		/// <summary>
		/// Gets or sets whether this <see cref="Ability"/>'s action
		/// continues when tagging a new <see cref="Character"/>
		/// </summary>
		public bool keepWhenTagging { get; set; }
		/// <summary>
		/// Geets or sets whether this <see cref="Ability"/>'s effect is
		/// disabled
		/// </summary>
		public bool isEffectDisable { get; set; }
		/// <summary>
		/// Loads data into this <see cref="Ability"/> instance
		/// </summary>
		/// <param name="assetObject"><see cref="GameObject"/> containing the
		/// data to be loaded</param>
		public void Load( dynamic assetObject ) {
			dynamic abilityGroup = assetObject.Properties["data"];
			this.groupId = Int32.Parse( abilityGroup.Properties["groupId"].String );
			this.abilityId = Int32.Parse( abilityGroup.Properties["abilityId"].String );
			this.time = Int64.Parse( abilityGroup.Properties["time"].String );
			this.tick = Int64.Parse( abilityGroup.Properties["tick"].String );
			this.keepWhenTagging = Boolean.Parse( abilityGroup.Properties["keepWhenTagging"].String );
			this.isEffectDisable = Boolean.Parse( abilityGroup.Properties["isEffectDisable"].String );
		}
	}
}
