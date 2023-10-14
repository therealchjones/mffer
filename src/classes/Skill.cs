using System;

namespace Mffer {
	/// <summary>
	/// Represents a <see cref="Character"/> skill
	/// </summary>
	public class Skill : GameObject {
		/// <summary>
		/// Gets or sets the skill ID for this <see cref="Skill"/>
		/// </summary>
		public string SkillId { get; set; }
		/// <summary>
		/// Gets or sets the name of this <see cref="Skill"/>
		/// </summary>
		public string Name { get; set; }
		/// <summary>
		/// Gets or sets the description text for this <see cref="Skill"/>
		/// </summary>
		public string Description { get; set; }
		/// <summary>
		/// Initializes a new instance of the <see cref="Skill"/> class
		/// </summary>
		/// <param name="skillId">The skill ID</param>
		public Skill( String skillId ) {
			SkillId = skillId;
		}
	}
}
