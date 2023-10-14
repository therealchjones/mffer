using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

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
		/// Gets or sets data about abilities
		/// </summary>
		/// <remarks>These include items referred to as "hero subtypes" by MFF</remarks>
		Dictionary<int, Ability> Abilities { get; set; }
		/// <summary>
		/// Initializes a new instance of the <see cref="Roster"/> class
		/// </summary>
		/// <seealso cref="Component.Component()"/>
		public Roster() : base() {
			Name = "Roster";
			Characters = new Dictionary<string, Character>();
			Abilities = new();
			AddBackingData( "IntHeroDataDictionary||text/data/hero_list.asset" );
			AddBackingData( "text/data/hero_skill.asset" );
			// from HeroSubtypeData.LoadCSV:
			AddBackingData( "text/data/mob_subtype_skill.csv" );
			AddBackingData( "text/data/hero_subtype_skill.csv" );
			AddBackingData( "text/data/hero_subtype_to_subtype.csv" );
			AddBackingData( "text/data/hero_ability_to_subtype.csv" );

			AddBackingData( "IntSkillDataDictionary" );
			AddBackingData( "IntAbilityGroupDataDictionary" );

			// from DBTable.get_skillTable:
			AddBackingData( "IntSkillDataDictionary" );

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
			LoadAbilities();
			dynamic asset = BackingData.First().Value;
			Localization LocalDictionary = (Localization)Dependencies["Localization"];
			List<string> AllHeroIds = new List<string>();
			List<GameObject> entries = asset.values.Value;
			foreach ( dynamic entry in entries ) {
				if ( entry.isVisible.ToString() == "1" ) {
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
						foreach ( GameObject ability in entry.abilitys.Value ) {
							if ( ability.GetValue() != "0" ) {
								uniform.Abilities.Add( LocalDictionary.GetString( "HERO_SUBTYPE_" + ability.GetValue().ToString() ) );
							}
						}
						if ( entry.ability_hidden.GetValue().ToString() != "0" ) {
							uniform.Abilities.Add( LocalDictionary.GetString( "HERO_SUBTYPE_" + entry.ability_hidden.ToString() ) );
						}
					}
					uniform.CharacterLevels.Add( heroId, newLevel );
					newLevel.Skills.Add( new Skill( entry.leaderSkillId.ToString() ) );
					foreach ( GameObject skill in entry.skillIds.Value ) {
						Skill newSkill = new Skill( skill.GetValue().ToString() );
						newLevel.Skills.Add( newSkill );
					}
					if ( newLevel.Skills.Count > 11 ) {
						throw new ApplicationException( "Too many skills found for character entry" );
					}
					while ( newLevel.Skills.Count < 11 ) {
						newLevel.Skills.Add( new Skill( "0" ) );
					}
					newLevel.Skills.Add( new Skill( entry.uniformSkillId.ToString() ) );
					if ( String.IsNullOrEmpty( character.BaseName ) ) {
						if ( uniform.UniformGroupId == "0" ) {
							character.BaseName = LocalDictionary.GetString( $"HERO_{baseId}" );
						}
					}
					try {
						newLevel.Instinct = LocalDictionary.GetString( "SPECIAL_TYPE_" + entry.specialType.ToString() );
					} catch ( KeyNotFoundException ) {
						newLevel.Instinct = "";
					}
					character.Species = LocalDictionary.GetString( "HERO_SUBTYPE_" + entry.species.ToString() );
					character.StartGrade = Int32.Parse( entry.startGrade.ToString() );
					character.GrowType = Int32.Parse( entry.growType.ToString() );
				}
			}
		}
		void LoadAbilities() {
			if ( Abilities.Count != 0 ) return;
			List<GameObject> abilityToSubtypeList = new();
			using ( JsonDocument abilityToSubtype
				= JsonDocument.Parse( CSVtoJson( ( (Asset)BackingData["text/data/hero_ability_to_subtype.csv"] ).GetCsv() ) ) ) {
				foreach ( JsonElement item in abilityToSubtype.RootElement.EnumerateArray() ) {
					Dictionary<string, int> entry = new();
					entry["groupId"] = Int32.Parse( item.GetProperty( "GROUP_ID" ).GetString() );
					entry["abilityGroupId"] = Int32.Parse( item.GetProperty( "ACTION_ABILITY_ID" ).GetString() );
					entry["subtypeId"] = Int32.Parse( item.GetProperty( "HERO_SUBTYPE_ID" ).GetString() );
					abilityToSubtypeList.Add( entry.ToGameObject() );
				}
			}
			// This CSV is empty in current versions; will throw exception if that changes
			List<GameObject> subtypeToSubtypeList = new();
			using ( JsonDocument subtypeToSubtype
				= JsonDocument.Parse( CSVtoJson( ( (Asset)BackingData["text/data/hero_subtype_to_subtype.csv"] ).GetCsv() ) ) ) {
				foreach ( JsonElement item in subtypeToSubtype.RootElement.EnumerateArray() ) {
					Dictionary<string, int> entry = new();
					entry["heroGroupId"] = Int32.Parse( item.GetProperty( "HERO_GROUP_ID" ).GetString() );
					entry["subtypeId"] = Int32.Parse( item.GetProperty( "HERO_SUBTYPE_ID" ).GetString() );
					entry["autoAbilityCondition"] = Int32.Parse( item.GetProperty( "AUTO_ABILITY_ID" ).GetString() );
					string autoAbilityParam = item.GetProperty( "AUTO_ABILITY_PARAM" ).GetString();
					entry["autoAbilityRate"] = Int32.Parse( item.GetProperty( "AUTO_ABILITY_RATE" ).GetString() );
					float coolTime = float.Parse( item.GetProperty( "COOLTIME" ).GetString() );
					entry["abilityGroupId"] = Int32.Parse( item.GetProperty( "ACTION_ABILITY_ID" ).GetString() );
					throw new NotImplementedException( "hero_subtype_to_subtype.csv now includes data" );
				}
			}
			Dictionary<string, GameObject> mobSubtypeSkillDictionary = new();
			using ( JsonDocument mobSkills
				= JsonDocument.Parse( CSVtoJson( ( (Asset)BackingData["text/data/mob_subtype_skill.csv"] ).GetCsv() ) ) ) {
				foreach ( JsonElement item in mobSkills.RootElement.EnumerateArray() ) {
					List<int> entry = new();
					entry.Add( Int32.Parse( item.GetProperty( "SKILL_ID_1" ).GetString() ) );
					entry.Add( Int32.Parse( item.GetProperty( "SKILL_ID_2" ).GetString() ) );
					entry.Add( Int32.Parse( item.GetProperty( "SKILL_ID_3" ).GetString() ) );
					mobSubtypeSkillDictionary.Add( item.GetProperty( "SUBTYPE_ID" ).GetString(), entry.ToGameObject() );
				}
			}
			Dictionary<string, GameObject> heroSubtypeSkillDictionary = new();
			using ( JsonDocument heroSkills
				= JsonDocument.Parse( CSVtoJson( ( (Asset)BackingData["text/data/hero_subtype_skill.csv"] ).GetCsv() ) ) ) {
				foreach ( JsonElement item in heroSkills.RootElement.EnumerateArray() ) {
					Dictionary<string, GameObject> entry = new();
					List<string> skillIds = new();
					List<string> raidSkillIds = new();
					skillIds.Add( item.GetProperty( "SKILL_ID_1" ).GetString() );
					skillIds.Add( item.GetProperty( "SKILL_ID_2" ).GetString() );
					skillIds.Add( item.GetProperty( "SKILL_ID_3" ).GetString() );
					entry.Add( "skillIds", skillIds.ToGameObject() );
					raidSkillIds.Add( item.GetProperty( "RAID_SKILL_ID_1" ).GetString() );
					raidSkillIds.Add( item.GetProperty( "RAID_SKILL_ID_2" ).GetString() );
					raidSkillIds.Add( item.GetProperty( "RAID_SKILL_3" ).GetString() );
					entry.Add( "skillIds_raid", raidSkillIds.ToGameObject() );
					entry.Add( "raid_ability_type", item.GetProperty( "RAID_ABILITY_SORT" ).GetString().ToGameObject() );
					entry.Add( "shadowland_subtype_filter", item.GetProperty( "SHADOWLAND_SUBTYPE_FILTER" ).GetString().ToGameObject() );
					heroSubtypeSkillDictionary.Add(
						item.GetProperty( "SUBTYPE_ID" ).GetString(), entry.ToGameObject() );
				}
			}
			// From MFF's HeroSubTypeData.GetSkillsDesc()
			Dictionary<string, GameObject> subtypeSkillDescDictionary = new();
			dynamic skillData = (Asset)BackingData["IntSkillDataDictionary"];
			dynamic abilityGroupData = (Asset)BackingData["IntAbilityGroupDataDictionary"];
			List<GameObject> skillGroupList = new();
			foreach ( dynamic entry in heroSubtypeSkillDictionary ) {
				List<GameObject> skills = entry.Value.Value["skillIds"].Value;
				// isMob in true / false, isRaid in true / false ) {
				foreach ( dynamic singleSkillData in skills ) {
					int skillId = Int32.Parse( singleSkillData.Value );
					int aniSkillAbilityKey = skillId / 100 * 100;
					if ( ( (Localization)Dependencies["Localization"] ).TryGetString( "SKILL_ABIL_" + aniSkillAbilityKey, out string localString ) ) {
						int skillLevel = skillId % 100;
						foreach ( string substring in localString.Split( ',' ) ) {
							string[] colonParts = substring.Split( ':' );
							int newSkillId;
							var thisSkillData = singleSkillData;
							if ( colonParts.Length == 2 ) {
								newSkillId = Int32.Parse( colonParts[1] );
								// newSkillId = skillData.GetSkillIdWithLevel( newSkillId, skillLevel );
								newSkillId = newSkillId / 100 * 100 + skillLevel;
								thisSkillData = skillData[newSkillId];
							}
							int abilityGroupId = Int32.Parse( colonParts[0] );
							// AbilityGroupData.GetAbilityGroupIdWithSkillLevel( abilityGroupId, skillLevel );
							abilityGroupId = abilityGroupId / 100 * 100 + abilityGroupId % 10 + skillLevel * 10;
							var thisAbilityGroupData = abilityGroupData[abilityGroupId];
							Dictionary<string, GameObject> thisSkillGroup = new();
							thisSkillGroup.Add( abilityGroupData, thisAbilityGroupData.ToGameObject() );
							thisSkillGroup.Add( skillData, thisSkillData.ToGameObject() );
							skillGroupList.Add( thisSkillGroup.ToGameObject() );
						}
					}
					// var descDataList = DBTable.GetSkillDescFromAddAbility( skillGroupList, skillId );
					// string desc = skillData.GetDescImpl();
					/*
					{
						StringBuilder description = new();
						if ( skillData.autoAbilityId == 48 )
							description.AppendLine( Localization.GetString( "AUTO_ABILITY_DESC_48_add" ) );

					}
					*/
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
				"Instinct",
				"Abilities",
				"World Boss Ability",
				"Leader Skill",
				"Skill 1",
				"Skill 2",
				"Skill 3",
				"Passive Skill",
				"Skill 4",
				"Skill 5",
				"T2 Passive Skill", // from HeroData_get_tier2SkillId
				"T3 Skill", // from HeroData_get_tier3SkillId
				"Awakened Skill", // from HeroData_get_awakeSkillId
				"T4 Skill", // from HeroData_get_tier4SkillId
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
						character.GrowType.ToString(),
						character.Instinct
					};
					int size = uniform.Abilities.Count;
					string abilities = "";
					for ( int i = 0; i < size; i++ ) {
						abilities += uniform.Abilities[i];
						if ( i < size - 1 ) abilities += ",";
					}
					entries.Add( abilities );
					entries.Add( uniform.RaidAbility );
					for ( int i = 0; i < uniform.Skills.Count; i++ ) {
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
		/// Gets the instinct type of the <see cref="Character"/>
		/// </summary>
		public string Instinct {
			get {
				string instinct = null;
				foreach ( Uniform uniform in Uniforms.Values ) {
					if ( instinct == null ) instinct = uniform.Instinct;
					else if ( instinct != uniform.Instinct ) throw new Exception( "Not all character level instincts are the same." );
				}
				return instinct;
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
		/// Gets or sets the instinct type of the <see cref="Character"/> when
		/// wearing this <see cref="Uniform"/>
		/// </summary>
		public string Instinct {
			get {
				string instinct = null;
				foreach ( CharacterLevel level in CharacterLevels.Values ) {
					if ( instinct == null ) instinct = level.Instinct;
					else if ( instinct != level.Instinct ) throw new Exception( "Not all character level instincts are the same." );
				}
				return instinct;
			}
		}
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
		/// Gets or sets the instinct type for this <see cref="CharacterLevel"/>
		/// </summary>
		public string Instinct { get; set; }
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
}
