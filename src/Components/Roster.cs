using System;
using System.Collections.Generic;
using System.IO;
using AssetsTools.Dynamic;

namespace Mffer {
	/// <summary>
	/// Represents a collection of all playable characters in the
	/// <see cref="Game"/>
	/// </summary>
	/// <remarks>
	/// <para><see cref="Roster"/> is derived from the <see cref="Component"/>
	/// class and includes methods to load and present data about the
	/// <see cref="Game"/>'s <see cref="Character"/>s.</para>
	/// <para>The data model for the <see cref="Roster"/> is hierarchical: each
	/// <see cref="Character"/> has one or more <see cref="Uniform"/>s, each of
	/// which has different properties associated with different
	/// <see cref="CharacterLevel"/>s. Each type has several properties that
	/// do not vary between descendants of that type. For instance, the
	/// <see cref="Uniform.Gender"/> of a given <see cref="Character"/> in a
	/// given <see cref="Uniform"/> is the same regardless of
	/// <see cref="CharacterLevel"/>.</para>
	/// </remarks>
	public class Roster : Component {
		/// <summary>
		/// Gets or sets a list of the <see cref="Game"/>'s <see
		/// cref="Character"/>s indexed by the <see cref="Character"/>s' <see
		/// cref="Character.GroupId"/>s.
		/// </summary>
		public Dictionary<string, Character> Characters { get; set; }
		/// <summary>
		/// Initializes a new instance of the <see cref="Roster"/> class
		/// </summary>
		/// <seealso cref="Component.Component()"/>
		public Roster() : base() {
			Name = "Roster";
			Characters = new Dictionary<string, Character>();
			AddBackingData( "IntHeroDataDictionary||text/data/hero_list.asset" );
			AddDependency( "Localization" );
		}
		/// <summary>
		/// Determines whether the <see cref="Roster"/> has been
		/// loaded.
		/// </summary>
		/// <returns><c>true</c> if the <see cref="Roster"/> already
		/// contains loaded data, <c>false</c> otherwise.</returns>
		/// <seealso cref="Component.IsLoaded()"/>
		public override bool IsLoaded() {
			return Characters.Count != 0;
		}
		/// <summary>
		/// Loads data into this <see cref="Roster"/>
		/// </summary>
		/// <seealso cref="Component.Load()"/>
		public override void Load() {
			base.Load();
			if ( IsLoaded() ) return;
			if ( BackingData.Count != 1 ) {
				throw new InvalidDataException();
			}
			DynamicAssetArray asset = null;
			foreach ( Asset entry in BackingData.Values ) {
				asset = entry.RawAsset.AsDynamic().values;
			}
			int assetLength = asset.Count();
			Localization LocalDictionary = (Localization)Dependencies["Localization"];
			List<string> AllHeroIds = new List<string>();
			for ( int i = 0; i < assetLength; i++ ) {
				dynamic entry = asset[i];
				if ( entry.isVisible == 1 ) {
					Character character;
					string groupId = entry.groupId.ToString();
					if ( Characters.ContainsKey( groupId ) ) {
						character = Characters[groupId];
					} else {
						character = new Character();
						character.GroupId = groupId;
						Characters.Add( groupId, character );
					}
					string heroId = entry.heroId.ToString();
					if ( AllHeroIds.Contains( heroId ) ) {
						throw new Exception( $"HeroID {heroId} has already been used." );
					} else {
						AllHeroIds.Add( heroId );
					}
					CharacterLevel newLevel = new CharacterLevel();
					newLevel.HeroId = heroId;
					newLevel.Rank = Int32.Parse( entry.grade.ToString() );
					newLevel.Tier = Int32.Parse( entry.tier.ToString() );
					string baseId = newLevel.BaseId;
					Uniform uniform;
					if ( character.Uniforms.ContainsKey( baseId ) ) {
						uniform = character.Uniforms[baseId];
					} else {
						uniform = new Uniform();
						character.Uniforms.Add( baseId, uniform );
						uniform.BaseId = baseId;
						uniform.Camps = LocalDictionary.GetString( "HERO_SUBTYPE_" + entry.stCamps );
						uniform.CharacterName = LocalDictionary.GetString( $"HERO_{baseId}" );
						uniform.ClassType = LocalDictionary.GetString( "HEROCLASS_" + entry.classType.ToString() );
						uniform.Gender = LocalDictionary.GetString( "HERO_SUBTYPE_" + entry.stGender );
						uniform.UniformGroupId = entry.uniformGroupId.ToString();
						uniform.UniformName = LocalDictionary.GetString( $"HERO_COSTUME_{baseId}" );
						switch ( entry.mainAtk.ToString() ) {
							case "0": uniform.MainAtk = "Physical"; break;
							case "1": uniform.MainAtk = "Energy"; break;
						}
						if ( entry.ability_raid.ToString() != "0" ) {
							uniform.RaidAbility = LocalDictionary.GetString( "HERO_SUBTYPE_" + entry.ability_raid.ToString() );
						}
						foreach ( int ability in entry.abilitys ) {
							if ( ability != 0 ) {
								uniform.Abilities.Add( LocalDictionary.GetString( "HERO_SUBTYPE_" + ability.ToString() ) );
							}
						}
						if ( entry.ability_hidden.ToString() != "0" ) {
							uniform.Abilities.Add( LocalDictionary.GetString( "HERO_SUBTYPE_" + entry.ability_hidden.ToString() ) );
						}
					}
					uniform.CharacterLevels.Add( heroId, newLevel );
					newLevel.Skills.Add( new Skill( entry.leaderSkillId.ToString() ) );
					foreach ( int skill in entry.skillIds ) {
						Skill newSkill = new Skill( skill.ToString() );
						newLevel.Skills.Add( newSkill );
					}
					newLevel.Skills.Add( new Skill( entry.uniformSkillId.ToString() ) );
					if ( String.IsNullOrEmpty( character.BaseName ) ) {
						if ( uniform.UniformGroupId == "0" ) {
							character.BaseName = LocalDictionary.GetString( $"HERO_{baseId}" );
						}
					}
					character.Species = LocalDictionary.GetString( "HERO_SUBTYPE_" + entry.species.ToString() );
					character.StartGrade = Int32.Parse( entry.startGrade.ToString() );
					character.GrowType = Int32.Parse( entry.growType.ToString() );
				}
			}
		}
		/// <summary>
		/// Outputs select data from this <see cref="Roster"/> in CSV format
		/// </summary>
		/// <remarks>
		/// <see cref="Roster.WriteCSV(string)"/> writes a CSV
		/// containing a flat representation of all playable characters and
		/// different uniforms, and the properties associated with each. It
		/// necessarily contains multiple redundant entries and is intended
		/// for use in spreadsheet applications rather than as a manipulatable
		/// data store.
		/// </remarks>
		/// <param name="fileName">The name of a file to which to
		/// write</param>
		/// <seealso cref="Component.WriteCSV(string)"/>
		public override void WriteCSV( string fileName ) {
			using StreamWriter file = new StreamWriter( fileName );
			char delimiter = '|';
			List<string> header = new List<string> {
				"Group ID",
				"Base ID",
				"BaseName",
				"Character Name",
				"Uniform Name",
				"Uniform Group Id",
				"Primary Attack",
				"Type",
				"Gender",
				"Side",
				"Allies",
				"Max Tier",
				"Growth Type",
				"Abilities",
				"World Boss Ability",
				"Leader Skill",
				"Skill 1",
				"Skill 2",
				"Skill 3",
				"Passive Skill",
				"Skill 4",
				"Skill 5",
				"T2 Passive Skill",
				"T3 Skill",
				"Awakened Skill",
				"Uniform Skill"
			};
			file.WriteLine( String.Join( delimiter, header ) );
			foreach ( Character character in Characters.Values ) {
				foreach ( Uniform uniform in character.Uniforms.Values ) {
					List<string> entries = new List<string> {
						character.GroupId,
						uniform.BaseId,
						character.BaseName,
						uniform.CharacterName,
						uniform.UniformName,
						uniform.UniformGroupId,
						uniform.MainAtk,
						uniform.ClassType,
						uniform.Gender,
						uniform.Camps,
						character.Species,
						character.MaxTier.ToString(),
						character.GrowType.ToString()
					};
					int size = uniform.Abilities.Count;
					string abilities = "";
					for ( int i = 0; i < size; i++ ) {
						abilities += uniform.Abilities[i];
						if ( i < size - 1 ) abilities += ",";
					}
					entries.Add( abilities );
					entries.Add( uniform.RaidAbility );
					for ( int i = 0; i < 11; i++ ) {
						if ( i < uniform.Skills.Count && uniform.Skills[i].SkillId != "0" ) {
							entries.Add( uniform.Skills[i].SkillId );
						} else {
							entries.Add( String.Empty );
						}
					}
					foreach ( string entry in entries ) {
						if ( entry.Contains( delimiter ) ) {
							throw new FormatException( "Error: CSV delimiter is included in CSV data" );
						}
					}
					file.WriteLine( String.Join( delimiter, entries ) );
				}
			}
		}
	}

	/// <summary>
	/// Represents a playable character
	/// </summary>
	/// <seealso cref="Roster"/>
	public class Character {
		/// <summary>
		/// Gets or sets the unique Group ID of the <see cref="Character"/>
		/// </summary>
		/// <remarks>
		/// Associated with the hierarchical model of the <see cref="Roster"/>
		/// are multiple identifiers for the different object levels. A
		/// <see cref="Character"/> equipped with a given <see cref="Uniform"/>
		/// at a specific rank (i.e., number of stars) is uniquely identified
		/// by a <see cref="CharacterLevel.HeroId"/>. Regardless of rank, the
		/// <see cref="Character"/> in that <see cref="Uniform"/> is identified
		/// by the <see cref="Uniform.BaseId"/>, and regardless of
		/// <see cref="Uniform"/> the <see cref="Character"/> is identified by
		/// a <see cref="Character.GroupId"/>. An additional identifier,
		/// <see cref="Uniform.UniformGroupId"/> is only unique among the
		/// <see cref="Uniform"/>s available for a given
		/// <see cref="Character"/>.
		/// </remarks>
		public string GroupId { get; set; }
		/// <summary>
		/// Gets or sets the list of <see cref="Uniform"/>s available for the
		/// <see cref="Character"/>, indexed by <see cref="Uniform.BaseId"/>
		/// </summary>
		public Dictionary<string, Uniform> Uniforms { get; set; } // by BaseId
		/// <summary>
		/// Gets or sets the name of the <see cref="Character"/> in the default
		/// <see cref="Uniform"/>
		/// </summary>
		public string BaseName { get; set; }
		/// <summary>
		/// Gets or sets the growth type of the <see cref="Character"/>
		/// </summary>
		public int GrowType { get; set; }
		/// <summary>
		/// Gets or sets the starting level (grade) of the
		/// <see cref="Character"/>
		/// </summary>
		public int StartGrade { get; set; }
		/// <summary>
		/// Gets or sets the allies (species) of the <see cref="Character"/>
		/// </summary>
		public string Species { get; set; }
		/// <summary>
		/// Gets the maximum tier of the <see cref="Character"/>
		/// </summary>
		/// <remarks>
		/// This is determined automatically by the
		/// <see cref="CharacterLevel"/>s available for each
		/// <see cref="Uniform"/>.
		/// </remarks>
		public int MaxTier {
			get {
				foreach ( Uniform uniform in Uniforms.Values ) {
					foreach ( CharacterLevel level in uniform.CharacterLevels.Values ) {
						if ( level.Tier == 3 ) return 3;
					}
					return 2;
				}
				throw new Exception( $"No uniforms found for character {BaseName} (groupId {GroupId})" );
			}
		}
		/// <summary>
		/// Initializes a new instance of the <see cref="Character"/> class
		/// </summary>
		public Character() {
			Uniforms = new Dictionary<string, Uniform>();
		}
	}
	/// <summary>
	/// Represents a uniform available to a playable <see cref="Character"/>
	/// </summary>
	public class Uniform {
		/// <summary>
		/// Gets or sets the name of the <see cref="Uniform"/>
		/// </summary>
		public string UniformName { get; set; }
		/// <summary>
		/// Gets or sets the name of the <see cref="Character"/> when wearing
		/// this <see cref="Uniform"/>
		/// </summary>
		public string CharacterName { get; set; }
		/// <summary>
		/// Gets or sets this <see cref="Uniform"/>'s Uniform Group ID
		/// </summary>
		/// <remarks>
		/// Note that this is not the <see cref="Character.GroupId"/>.
		/// </remarks>
		public string UniformGroupId { get; set; }
		/// <summary>
		/// Gets or sets the list of <see cref="CharacterLevel"/>s, indexed
		/// by Hero ID
		/// </summary>
		/// <seealso cref="Character.GroupId"/>
		public Dictionary<string, CharacterLevel> CharacterLevels { get; set; } // by heroId
		/// <summary>
		/// Gets or sets the allies (camps) of the <see cref="Character"/> when
		/// wearing this <see cref="Uniform"/>
		/// </summary>
		public string Camps { get; set; }
		/// <summary>
		/// Gets or sets the gender of the <see cref="Character"/> when wearing
		/// this <see cref="Uniform"/>
		/// </summary>
		public string Gender { get; set; }
		/// <summary>
		/// Gets or sets the BaseId of the <see cref="Character"/> when wearing
		/// this <see cref="Uniform"/>
		/// </summary>
		public string BaseId { get; set; }
		/// <summary>
		/// Gets or sets the Class of the <see cref="Character"/> when
		/// wearing this <see cref="Uniform"/>
		/// </summary>
		public string ClassType { get; set; }
		/// <summary>
		/// Gets or sets the Ally ability of the <see cref="Character"/> when
		/// wearing this <see cref="Uniform"/>
		/// </summary>
		public string RaidAbility { get; set; }
		/// <summary>
		/// Gets or sets the main attack type of the <see cref="Character"/>
		/// when wearing this <see cref="Uniform"/>
		/// </summary>
		public string MainAtk { get; set; }
		/// <summary>
		/// Gets or sets the list of abilities of the <see cref="Character"/>
		/// when wearing this <see cref="Uniform"/>
		/// </summary>
		public List<string> Abilities { get; set; }
		/// <summary>
		/// Gets the full list of skills of the <see cref="Character"/> when
		/// wearing this <see cref="Uniform"/>
		/// </summary>
		/// <remarks>
		/// The list of <see cref="Skill"/>s available to the
		/// <see cref="Character"/> increases as the <see cref="Character"/>'s
		/// rank increases; <see cref="Skills"/> is the full list available
		/// at maximum rank.
		/// </remarks>
		public List<Skill> Skills {
			get {
				List<Skill> maxSkillSet = new List<Skill>();
				int maxCount = 0;
				foreach ( CharacterLevel level in CharacterLevels.Values ) {
					int count = 0;
					foreach ( Skill skill in level.Skills ) {
						if ( skill.SkillId != "0" ) {
							count++;
						}
					}
					if ( count > maxCount ) {
						maxSkillSet = level.Skills;
					}
				}
				return maxSkillSet;
			}
		}
		/// <summary>
		/// Initializes a new instance of the <see cref="Uniform"/> class
		/// </summary>
		public Uniform() {
			Abilities = new List<string>();
			CharacterLevels = new Dictionary<string, CharacterLevel>();
		}
	}
	/// <summary>
	/// Represents a <see cref="Character"/> equipped with a particular
	/// <see cref="Uniform"/> at a particular rank
	/// </summary>
	public class CharacterLevel {
		/// <summary>
		/// Gets or sets the hero ID for this <see cref="CharacterLevel"/>
		/// </summary>
		/// <seealso cref="Character.GroupId"/>
		public string HeroId { get; set; }
		/// <summary>
		/// Gets or sets the rank (stars) for this <see cref="CharacterLevel"/>
		/// </summary>
		public int Rank { get; set; }
		/// <summary>
		/// Gets or sets the tier for this <see cref="CharacterLevel"/>
		/// </summary>
		public int Tier { get; set; }
		/// <summary>
		/// Gets or sets the list of skills available at this
		/// <see cref="CharacterLevel"/>
		/// </summary>
		public List<Skill> Skills { get; set; }
		/// <summary>
		/// Gets the <see cref="BaseId"/> for the <see cref="Character"/> /
		/// <see cref="Uniform"/> combination associated with this
		/// <see cref="CharacterLevel"/>
		/// </summary>
		///	<remarks>
		///	There is a many-to-one mapping of
		///	<see cref="CharacterLevel.HeroId"/> to
		///	<see cref="Uniform.BaseId"/> that is calculatable. For a given
		///	<see cref="CharacterLevel"/>, then, properties of the
		///	<see cref="Character"/> and <see cref="Uniform"/> that do not vary
		///	with <see cref="CharacterLevel"/> can be
		///	quickly found.
		///</remarks>
		public string BaseId {
			get {
				Int64 heroIdNumber = Int64.Parse( HeroId );
				Int64 heroIdNumber1 = ( heroIdNumber * 0x51eb851f ) >> 32;
				Int64 heroIdNumber2 = heroIdNumber1 >> 31;
				heroIdNumber1 = heroIdNumber1 >> 5;
				heroIdNumber = heroIdNumber1 + heroIdNumber2;
				heroIdNumber = heroIdNumber * 100 + 1;
				return heroIdNumber.ToString();
			}
		}
		/// <summary>
		/// Initializes a new instance of the <see cref="CharacterLevel"/>
		/// class
		/// </summary>
		public CharacterLevel() {
			Skills = new List<Skill>();
		}
	}
	/// <summary>
	/// Represents a <see cref="Character"/> skill
	/// </summary>
	public class Skill {
		/// <summary>
		/// Gets or sets the skill ID for this <see cref="Skill"/>
		/// </summary>
		public string SkillId { get; set; }
		/// <summary>
		/// Gets or sets the name of this <see cref="Skill"/>
		/// </summary>
		public string Name { get; set; }
		/// <summary>
		/// Initializes a new instance of the <see cref="Skill"/> class
		/// </summary>
		/// <param name="skillId">The skill ID</param>
		public Skill( String skillId ) {
			SkillId = skillId;
		}
	}
	/// <summary>
	/// Represents a skill (ability) available for a <see cref="Character"/>
	/// </summary>
	public class AbilityGroup {
		/// <summary>
		/// Gets or sets the ability group ID for this
		/// <see cref="AbilityGroup"/>
		/// </summary>
		public int groupId { get; set; }
		/// <summary>
		/// Gets or sets the ability ID for this <see cref="AbilityGroup"/>
		/// </summary>
		public int abilityId { get; set; }
		/// <summary>
		/// Gets or sets the time of action for this <see cref="AbilityGroup"/>
		/// </summary>
		public long time { get; set; }
		/// <summary>
		/// Gets or sets the "tick" for this <see cref="AbilityGroup"/>
		/// </summary>
		public long tick { get; set; }
		/// <summary>
		/// Gets or sets whether this <see cref="AbilityGroup"/>'s action
		/// continues when tagging a new <see cref="Character"/>
		/// </summary>
		public bool keepWhenTagging { get; set; }
		/// <summary>
		/// Geets or sets whether this <see cref="AbilityGroup"/>'s effect is
		/// disabled
		/// </summary>
		public bool isEffectDisable { get; set; }
		/// <summary>
		/// Loads data into this <see cref="AbilityGroup"/> instance
		/// </summary>
		/// <param name="assetObject"><see cref="AssetObject"/> containing the
		/// data to be loaded</param>
		public void Load( dynamic assetObject ) {
			// List<AssetObject> assetObjects = Program.Assets.AssetFiles["text/data/action_ability.asset"].Properties["values"].Array;
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
