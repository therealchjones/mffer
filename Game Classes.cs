using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MFFDataApp {
	public class Game {
		public string Name { get; set; }
		public List<Version> Versions { get; set; }
		public Game( string gameName ) {
			Name = gameName;
			Versions = new List<Version>();
		}
		public void LoadAllData( string dir ) {
			DataDirectory dataDir = new DataDirectory( dir );
			List<string> versionNames = dataDir.GetVersionNames();
			foreach ( string versionName in versionNames ) {
				Version version = new Version( versionName );
				version.Assets = dataDir.GetAssets( versionName );
				version.LoadAllComponents();
				Versions.Add( version );
			}
		}
		public void SaveAllData( string fileName ) {
			// implemented as streamwriter at all levels because using a string or
			// similar uses up all memory, same with JsonSerializer
			using ( StreamWriter file = new StreamWriter( fileName ) ) {
				file.WriteLine( "{" );
				file.WriteLine( $"\t\"{Name}\" : " + "{" );
				int versionCounter = 0;
				foreach ( Version version in Versions ) {
					// WriteJson should consistently write the instance as one or more
					// JSON members (string: element) without a bare root element, and without
					// a newline on the last line. It is on the caller to provide appropriate
					// wrapping. The (optional) second argument prepends each line of
					// the JSON output with that number of tabs
					version.WriteJson( file, 2 );
					versionCounter++;
					if ( versionCounter < Versions.Count ) {
						file.Write( "," );
					}
					file.WriteLine( "" );
				}
				file.WriteLine( "\t}" );
				file.WriteLine( "}" );
			}
			return;
		}
	}
	public class Version {
		public string Name { get; set; }
		public Dictionary<string, Component> Components { get; set; }
		public AssetBundle Assets { get; set; }
		public Version() {
			Name = "";
			Components = new Dictionary<string, Component>();
			Assets = new AssetBundle();

			AddComponent( new Localization() );
			AddComponent( new Roster() );
		}
		public Version( string versionName ) : this() {
			Name = versionName;
		}
		AssetObject GetAsset( string assetName ) {
			return null;
		}
		void AddComponent( Component component ) {
			Components.Add( component.Name, component );
		}
		public void LoadAllComponents() {
			foreach ( Component component in Components.Values ) {
				LoadComponent( component );
			}
		}
		public void LoadComponent( string componentName ) {
			if ( Components.ContainsKey( componentName ) ) {
				LoadComponent( Components[componentName] );
			} else {
				throw new Exception( $"Unable to load; no component named '{componentName}'" );
			}
		}
		public void LoadComponent( Component component ) {
			if ( !component.IsLoaded() ) {
				foreach ( string assetName in component.BackingAssets.Keys.ToList<string>() ) {
					if ( component.BackingAssets[assetName] == null ) {
						if ( Assets.AssetFiles.ContainsKey( assetName ) ) {
							component.BackingAssets[assetName] = Assets.AssetFiles[assetName];
						} else if ( assetName.Contains( "||" ) ) {
							foreach ( string possibleAssetName in assetName.Split( "||" ) ) {
								string possibleName = possibleAssetName.Trim();
								if ( String.IsNullOrEmpty( possibleName ) ) continue;
								if ( component.BackingAssets.ContainsKey( possibleName ) ) {
									component.BackingAssets.Remove( assetName );
									break;
								}
								if ( Assets.AssetFiles.ContainsKey( possibleName ) ) {
									component.BackingAssets.Add( possibleName, Assets.AssetFiles[possibleName] );
									component.BackingAssets.Remove( assetName );
									break;
								}
							}
							if ( component.BackingAssets.ContainsKey( assetName ) ) {
								throw new Exception( $"Unable to find any of the assets '{assetName}' for component '{component.Name}'" );
							}
						} else {
							throw new Exception( $"Unable to load asset '{assetName}' for component '{component.Name}'" );
						}
					}
				}
				foreach ( string componentName in component.Dependencies.Keys.ToList<string>() ) {
					if ( Components.ContainsKey( componentName ) ) {
						LoadComponent( componentName );
						component.Dependencies[componentName] = Components[componentName];
					} else {
						throw new Exception( $"Unable to load dependencies for component {component.Name}: could not find component named {componentName}." );
					}
				}
				component.Load();
			}
		}
		public void WriteJson( StreamWriter file, int tabs = 0 ) {
			for ( int i = 0; i < tabs; i++ ) {
				file.Write( "\t" );
			}
			file.WriteLine( $"\"{Name}\" : " + "{" );
			for ( int i = 0; i < tabs + 1; i++ ) {
				file.Write( "\t" );
			}
			file.WriteLine( "\"Assets\" : {" );
			Assets.WriteJson( file, tabs + 2 );
			file.WriteLine();
			for ( int i = 0; i < tabs + 1; i++ ) {
				file.Write( "\t" );
			}
			file.Write( "}" );
			if ( Components.Count > 0 ) {
				file.WriteLine( "," );
				for ( int i = 0; i < tabs + 1; i++ ) {
					file.Write( "\t" );
				}
				file.WriteLine( "\"Components\" : {" );
				int componentCounter = 0;
				List<string> components = Components.Keys.ToList<string>();
				components.Sort();
				foreach ( string key in components ) {
					Component component = Components[key];
					component.WriteJson( file, tabs + 2 );
					componentCounter++;
					if ( componentCounter < Components.Count - 1 ) {
						file.WriteLine( "," );
					}
				}
				file.WriteLine();
				for ( int i = 0; i < tabs + 1; i++ ) {
					file.Write( "\t" );
				}
				file.Write( "}" );
			}
			file.WriteLine();
			for ( int i = 0; i < tabs; i++ ) {
				file.Write( "\t" );
			}
			file.Write( "}" );
		}
	}
	public class Component {
		public string Name;
		public Dictionary<string, AssetObject> BackingAssets { get; set; }
		public Dictionary<string, Component> Dependencies { get; set; }
		protected Component() {
			BackingAssets = new Dictionary<string, AssetObject>();
			Dependencies = new Dictionary<string, Component>();
		}
		public virtual void AddBackingAsset( string assetName ) {
			if ( !BackingAssets.ContainsKey( assetName ) ) {
				BackingAssets.Add( assetName, null );
			}
		}
		public virtual void AddDependency( string componentName ) {
			if ( !Dependencies.ContainsKey( componentName ) ) {
				Dependencies.Add( componentName, null );
			}
		}
		public virtual void WriteJson( StreamWriter file, int tabs = 0 ) {
		}
		public virtual void WriteCSV( StreamWriter file ) {
		}
		public virtual void Load() {
			if ( IsLoaded() ) return;
			if ( BackingAssets.Count != 0 ) {
				foreach ( KeyValuePair<string, AssetObject> item in BackingAssets ) {
					if ( String.IsNullOrWhiteSpace( item.Key ) ) {
						BackingAssets.Remove( item.Key );
					} else {
						if ( item.Value == null ) {
							throw new Exception( $"Unable to load {Name}: backing asset {item.Key} not loaded. Preload needed." );
						}
					}
				}
			}
			if ( Dependencies.Count != 0 ) {
				foreach ( KeyValuePair<string, Component> item in Dependencies ) {
					if ( String.IsNullOrWhiteSpace( item.Key ) ) {
						Dependencies.Remove( item.Key );
					} else {
						if ( item.Value == null || !item.Value.IsLoaded() ) {
							throw new Exception( $"Unable to load {Name}: dependency {item.Key} not loaded. Preload needed." );
						}
					}
				}
			}
		}
		public virtual bool IsLoaded() {
			return true;
		}
	}
	// String localization dictionary
	public class Localization : Component {
		Dictionary<string, string> LocalDictionary { get; set; }
		public string Language { get; set; }
		public Localization() : base() {
			Name = "Localization";
			LocalDictionary = new Dictionary<string, string>();
			Language = "en";
			AddBackingAsset( $"localization/localization_{Language}.csv||LocalizationTable_{Language}" );
		}
		public override bool IsLoaded() {
			return LocalDictionary.Count != 0;
		}
		public override void Load() {
			base.Load();
			AssetObject DictionaryAsset = BackingAssets.First().Value;
			// the localization dictionary was a CSV in 6.2.0, but is in an asset in
			// 6.7.0; will have to manage differently
			if ( BackingAssets.First().Key.EndsWith( ".csv", StringComparison.InvariantCultureIgnoreCase ) ) {
				foreach ( AssetObject entry in DictionaryAsset.Properties["m_Script"].Array ) {
					LocalDictionary[entry.Properties["KEY"].String] = entry.Properties["TEXT"].String;
				}
			} else {
				Dictionary<string, string> keys = new Dictionary<string, string>();
				Dictionary<string, string> values = new Dictionary<string, string>();
				foreach ( int keyNum in Enumerable.Range( 0, DictionaryAsset.Properties["keyTable"].Properties["keys"].Properties["Array"].Array.Count() ) ) {
					keys.Add( DictionaryAsset.Properties["keyTable"].Properties["keys"].Properties["Array"].Array[keyNum].Properties["data"].String,
						DictionaryAsset.Properties["keyTable"].Properties["values"].Properties["Array"].Array[keyNum].Properties["data"].String );
				}
				foreach ( int keyNum in Enumerable.Range( 0, DictionaryAsset.Properties["valueTable"].Properties["keys"].Properties["Array"].Array.Count() ) ) {
					values.Add( DictionaryAsset.Properties["valueTable"].Properties["keys"].Properties["Array"].Array[keyNum].Properties["data"].String,
						DictionaryAsset.Properties["valueTable"].Properties["values"].Properties["Array"].Array[keyNum].Properties["data"].String );
				}
				if ( new HashSet<string>( keys.Values ).Count() == values.Count() ) {
					LocalDictionary = Enumerable.Range( 0, keys.Count() ).ToDictionary(
						i => keys.Keys.ToList()[i],
						i => values[keys.Values.ToList()[i]] );
				} else {
					throw new Exception( "Unable to build localization dictionary; invalid entries" );
				}
			}
		}
		public string GetString( string input ) {
			if ( BackingAssets.First().Key.EndsWith( ".csv", StringComparison.InvariantCultureIgnoreCase ) ) {
				return LocalDictionary[input];
			} else {
				return LocalDictionary[MakeHash( input )];
			}
		}
		string MakeHash( string input ) {
			int result = 0;
			char[] textBytes = input.ToCharArray();
			int i = 0;
			int length = textBytes.Length;
			int thisCharIndex = 0;
			if ( i < length - 1 ) {
				int nextCharIndex = 1;
				do {
					byte thisChar = Convert.ToByte( textBytes[thisCharIndex] );
					byte nextChar = Convert.ToByte( textBytes[nextCharIndex] );
					int subresult = ( ( ( result << 5 ) - result ) + Convert.ToInt32( thisChar ) );
					result = ( subresult << 5 ) - subresult + Convert.ToInt32( nextChar );
					i = i + 2;
					thisCharIndex = i;
					nextCharIndex = i + 1;
				}
				while ( i < length - 1 );
			}
			if ( i < length ) {
				result = ( ( result << 5 ) - result ) + Convert.ToInt32( textBytes[thisCharIndex] );
			}
			return result.ToString();
		}
		public override void WriteJson( StreamWriter file, int tabs = 0 ) {

		}
	}
	// List of all available playable characters in the game
	public class Roster : Component {
		public Dictionary<string, Character> Characters { get; set; } // by groupId
		public Roster() : base() {
			Name = "Roster";
			Characters = new Dictionary<string, Character>();
			AddBackingAsset( "IntHeroDataDictionary" );
			AddDependency( "Localization" );
		}
		public override bool IsLoaded() {
			return Characters.Count != 0;
		}
		// See also explanation of HeroId, BaseId, GroupId, and UniformGroupId in comments for
		// Character class. This is all based on certain assumptions, which should probably all
		// be tested here. For instance, when adding info, ensure it's not redundant or inconsistent.
		public override void Load() {
			base.Load();
			AssetObject asset = BackingAssets["IntHeroDataDictionary"].Properties["values"].Properties["Array"];
			Localization LocalDictionary = (Localization)Dependencies["Localization"];
			List<string> AllHeroIds = new List<string>();
			foreach ( AssetObject entry in asset.Array ) {
				if ( entry.Properties["data"].Properties["isVisible"].String == "1" ) {
					Character character;
					string groupId = entry.Properties["data"].Properties["groupId"].String;
					if ( Characters.ContainsKey( groupId ) ) {
						character = Characters[groupId];
					} else {
						character = new Character();
						character.GroupId = groupId;
						Characters.Add( groupId, character );
					}
					string heroId = entry.Properties["data"].Properties["heroId"].String;
					if ( AllHeroIds.Contains( heroId ) ) {
						throw new Exception( $"HeroID {heroId} has already been used." );
					} else {
						AllHeroIds.Add( heroId );
					}
					CharacterLevel newLevel = new CharacterLevel();
					newLevel.HeroId = heroId;
					newLevel.Rank = Int32.Parse( entry.Properties["data"].Properties["grade"].String );
					newLevel.Tier = Int32.Parse( entry.Properties["data"].Properties["tier"].String );
					string baseId = newLevel.BaseId;
					Uniform uniform;
					if ( character.Uniforms.ContainsKey( baseId ) ) {
						uniform = character.Uniforms[baseId];
					} else {
						uniform = new Uniform();
						character.Uniforms.Add( baseId, uniform );
						uniform.BaseId = baseId;
						uniform.Camps = LocalDictionary.GetString( "HERO_SUBTYPE_" + entry.Properties["data"].Properties["stCamps"].String );
						uniform.CharacterName = LocalDictionary.GetString( $"HERO_{baseId}" );
						uniform.ClassType = LocalDictionary.GetString( "HEROCLASS_" + entry.Properties["data"].Properties["classType"].String );
						uniform.Gender = LocalDictionary.GetString( "HERO_SUBTYPE_" + entry.Properties["data"].Properties["stGender"].String );
						uniform.UniformGroupId = entry.Properties["data"].Properties["uniformGroupId"].String;
						uniform.UniformName = LocalDictionary.GetString( $"HERO_COSTUME_{baseId}" );
						switch ( entry.Properties["data"].Properties["mainAtk"].String ) {
							case "0": uniform.MainAtk = "Physical"; break;
							case "1": uniform.MainAtk = "Energy"; break;
						}
						if ( entry.Properties["data"].Properties["ability_raid"].String != "0" ) {
							uniform.RaidAbility = LocalDictionary.GetString( "HERO_SUBTYPE_" + entry.Properties["data"].Properties["ability_raid"].String );
						}
						foreach ( AssetObject ability in entry.Properties["data"].Properties["abilitys"].Properties["Array"].Array ) {
							if ( ability.Properties["data"].String != "0" ) {
								uniform.Abilities.Add( LocalDictionary.GetString( "HERO_SUBTYPE_" + ability.Properties["data"].String ) );
							}
						}
						if ( entry.Properties["data"].Properties["ability_hidden"].String != "0" ) {
							uniform.Abilities.Add( LocalDictionary.GetString( "HERO_SUBTYPE_" + entry.Properties["data"].Properties["ability_hidden"].String ) );
						}
					}
					uniform.CharacterLevels.Add( heroId, newLevel );
					newLevel.Skills.Add( new Skill( entry.Properties["data"].Properties["leaderSkillId"].String ) );
					foreach ( AssetObject skill in entry.Properties["data"].Properties["skillIds"].Properties["Array"].Array ) {
						Skill newSkill = new Skill( skill.Properties["data"].String );
						newLevel.Skills.Add( newSkill );
					}
					newLevel.Skills.Add( new Skill( entry.Properties["data"].Properties["uniformSkillId"].String ) );
					if ( String.IsNullOrEmpty( character.BaseName ) ) {
						if ( uniform.UniformGroupId == "0" ) {
							character.BaseName = LocalDictionary.GetString( $"HERO_{baseId}" );
						}
					}
					character.Species = LocalDictionary.GetString( "HERO_SUBTYPE_" + entry.Properties["data"].Properties["species"].String );
					character.StartGrade = Int32.Parse( entry.Properties["data"].Properties["startGrade"].String );
					character.GrowType = Int32.Parse( entry.Properties["data"].Properties["growType"].String );
				}
			}
		}
		public override void WriteCSV( StreamWriter file ) {
			file.Write( "|Group ID|Base ID|BaseName|Character Name|Uniform Name|Uniform Group Id|" );
			file.Write( "Primary Attack|Type|Gender|Side|Allies|Max Tier|Growth Type|Abilities|World Boss Ability|Leader Skill|" );
			file.Write( "Skill 1|Skill 2|Skill 3|Passive Skill|Skill 4|Skill 5|T2 Passive Skill|T3 Skill|Awakened Skill|" );
			file.Write( "Uniform Skill" );
			file.WriteLine();
			foreach ( Character character in Characters.Values ) {
				foreach ( Uniform uniform in character.Uniforms.Values ) {
					file.Write( $"|{character.GroupId}|{uniform.BaseId}|{character.BaseName}|{uniform.CharacterName}|" );
					file.Write( $"{uniform.UniformName}|{uniform.UniformGroupId}|{uniform.MainAtk}|{uniform.ClassType}|" );
					file.Write( $"{uniform.Gender}|{uniform.Camps}|{character.Species}|{character.MaxTier}|{character.GrowType}|" );
					int size = uniform.Abilities.Count;
					for ( int i = 0; i < size; i++ ) {
						file.Write( uniform.Abilities[i] );
						if ( i < size - 1 ) file.Write( "," );
					}
					file.Write( $"|{uniform.RaidAbility}" );
					for ( int i = 0; i < 11; i++ ) {
						file.Write( "|" );
						if ( i < uniform.Skills.Count && uniform.Skills[i].SkillId != "0" ) {
							file.Write( uniform.Skills[i].SkillId );
						}
					}
					file.WriteLine();
				}
			}
		}
		public override void WriteJson( StreamWriter file, int tabs = 0 ) {

		}
	}
	public class Character {
		public string GroupId { get; set; }
		public Dictionary<string, Uniform> Uniforms { get; set; } // by baseId
		public string BaseName { get; set; }
		public int GrowType { get; set; }
		public int StartGrade { get; set; }
		public string Species { get; set; }
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
		public Character() {
			Uniforms = new Dictionary<string, Uniform>();
		}
	}
	public class Uniform {
		public string UniformName { get; set; }
		public string CharacterName { get; set; }
		public string UniformGroupId { get; set; }
		public Dictionary<string, CharacterLevel> CharacterLevels { get; set; } // by heroId
		public string Camps { get; set; }
		public string Gender { get; set; }
		public string BaseId { get; set; }
		public string ClassType { get; set; }
		public string RaidAbility { get; set; }
		public string MainAtk { get; set; }
		public List<string> Abilities { get; set; }
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
		public Uniform() {
			Abilities = new List<string>();
			CharacterLevels = new Dictionary<string, CharacterLevel>();
		}
	}
	public class CharacterLevel {
		public string HeroId { get; set; }
		public int Rank { get; set; }
		public int Tier { get; set; }
		public List<Skill> Skills { get; set; }
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
		public CharacterLevel() {
			Skills = new List<Skill>();
		}
	}
	public class Skill {
		public string SkillId { get; set; }
		public string Name { get; set; }
		public Skill( String skillId ) {
			SkillId = skillId;
		}
	}
	public class Alliance {
		public string Name { get; set; }
		public List<Player> Players { get; set; }
		public Player Leader { get; set; }
		public List<Player> Class1Players { get; set; }
		public List<Player> Class2Players { get; set; }
	}
	public class Item {
		public string Name { get; set; }
	}
	class Shadowland : Component {
		ShadowlandFloor[] BaseFloors;
		public void Load( AssetBundle Assets ) {
			BaseFloors = new ShadowlandFloor[35];
			List<AssetObject> shadowlandFloors = Assets.AssetFiles["text/data/shadowland_floor.csv"].Properties["m_Script"].Array;
			List<AssetObject> shadowlandRewards = Assets.AssetFiles["text/data/shadowland_reward.csv"].Properties["m_Script"].Array;
			for ( int floorNum = 0; floorNum < BaseFloors.Length; floorNum++ ) {
				ShadowlandFloor floor = new ShadowlandFloor();
				floor.FloorNumber = floorNum + 1;
				floor.BaseFloor = floor;
				foreach ( AssetObject value in shadowlandRewards ) {
					if ( value.Properties["REWARD_GROUP"].String == shadowlandFloors[floorNum].Properties["REWARD_GROUP"].String ) {
						List<ShadowlandReward> rewards = new List<ShadowlandReward>();
						for ( int i = 1; i <= 2; i++ ) {
							ShadowlandReward reward = new ShadowlandReward();
							reward.RewardValue = Int32.Parse( value.Properties[$"REWARD_VALUE_{i}"].String );
							reward.RewardQuantity = Int32.Parse( value.Properties[$"REWARD_QTY_{i}"].String );
							reward.RewardType = Int32.Parse( value.Properties[$"REWARD_TYPE_{i}"].String );
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

		public class ShadowlandFloor {
			public int FloorNumber;
			public ShadowlandFloor BaseFloor;
			public int RewardGroup;
			public int StageGroup;
			public int StageSelectCount;
		}
		public class Opponent {
		}
		public class OpponentChoice {

		}
		public class ShadowlandReward {
			public int RewardQuantity;
			public int RewardValue;
			public int RewardType;
		}
	}
	public class ComicCard {
		public string cardId;
	}
	public class ComicCardCollection {
		public ComicCard[] Cards { get; set; }
		public string cardGroup;
		public string abilityId;
		public string abilityParam;

		private void LoadById( string id ) {

		}
	}
	public class Reward {
		public Item item { get; set; }
		public int Quantity { get; set; }
	}
	public class FuturePass : Component {
		public List<FuturePassSeason> Seasons { get; set; }
		public FuturePassStep[] Steps { get; set; }
		public Dictionary<int, int> StagePoints { get; set; }

		public void Load( AssetBundle Assets ) {
			string seasonAssetName = "text/data/future_pass.asset";
			List<FuturePassSeason> seasons = new List<FuturePassSeason>();
			foreach ( AssetObject seasonAsset in Assets.AssetFiles[seasonAssetName].Properties["list"].Array ) {
				FuturePassSeason season = new FuturePassSeason();
				season.Load( seasonAsset );
				seasons.Add( season );
			}
			string stepAssetName = "text/data/future_pass_step.asset";
			Steps = new FuturePassStep[50];
			foreach ( AssetObject stepAsset in Assets.AssetFiles[stepAssetName].Properties["list"].Array ) {
				FuturePassStep step = new FuturePassStep();
				step.passPoint = Int32.Parse( stepAsset.Properties["data"].Properties["passPoint"].String );
				step.step = Int32.Parse( stepAsset.Properties["data"].Properties["step"].String );
				step.Rewards = new Dictionary<FuturePassType, FuturePassReward>();
				Steps[step.step - 1] = step;
			}
			string rewardAssetName = "text/data/future_pass_reward.asset";
			foreach ( AssetObject rewardAsset in Assets.AssetFiles[rewardAssetName].Properties["list"].Array ) {
				FuturePassReward reward = new FuturePassReward();
				reward.Load( rewardAsset );
				FuturePassType level = (FuturePassType)Int32.Parse( rewardAsset.Properties["data"].Properties["grade"].String );
				int step = Int32.Parse( rewardAsset.Properties["data"].Properties["step"].String );
				Steps[step - 1].Rewards[level] = reward;
			}
			string stageAssetName = "text/data/future_pass_contents.asset";
			StagePoints = new Dictionary<int, int>();
			foreach ( AssetObject stageAsset in Assets.AssetFiles[stageAssetName].Properties["list"].Array ) {
				int sceneId = Int32.Parse( stageAsset.Properties["data"].Properties["sceneId"].String );
				int stagePoints = Int32.Parse( stageAsset.Properties["data"].Properties["passPoint"].String );
				StagePoints.Add( sceneId, stagePoints );
			}
		}
		public class FuturePassReward : Reward {
			// text/data/future_pass_reward.asset->list->Array[x]->data
			private int rewardId;
			private int rewardGroupId;
			private int rewardType;
			private int rewardValue;
			public void Load( AssetObject asset ) {
				this.rewardId = Int32.Parse( asset.Properties["data"].Properties["rewardId"].String );
				this.rewardGroupId = Int32.Parse( asset.Properties["data"].Properties["rewardGroupId"].String );
				this.rewardType = Int32.Parse( asset.Properties["data"].Properties["rewardType"].String );
				this.rewardValue = Int32.Parse( asset.Properties["data"].Properties["rewardValue"].String );
			}
		}
		public class FuturePassSeason {
			// text/data/future_pass.asset->list->Array[x]->data
			string endTime { get; set; }
			string startTime { get; set; }
			int rewardGroupId { get; set; }
			public void Load( AssetObject asset ) {
				this.endTime = asset.Properties["data"].Properties["endTime_unused"].String;
				this.startTime = asset.Properties["data"].Properties["startTime_unused"].String;
				this.rewardGroupId = Int32.Parse( asset.Properties["data"].Properties["rewardGroupId"].String );
			}
		}
		public class FuturePassStep {
			// text/data/future_pass_step.asset->list->Array[x]->data
			public int step { get; set; } // 1-50
			public int passPoint { get; set; } // total points to get to this step
			public Dictionary<FuturePassType, FuturePassReward> Rewards { get; set; }
		}
		public enum FuturePassType {
			Normal,
			Legendary,
			Mythic
		}
	}
	public class AbilityGroup {
		public int groupId;
		public int abilityId;
		public long time;
		public long tick;
		public bool keepWhenTagging;
		public bool isEffectDisable;
		public static string assetFile = "text/data/action_ability.asset";
		public void Load( AssetObject assetObject ) {
			// List<AssetObject> assetObjects = Program.Assets.AssetFiles[assetFile].Properties["values"].Array;
			AssetObject abilityGroup = assetObject.Properties["data"];
			this.groupId = Int32.Parse( abilityGroup.Properties["groupId"].String );
			this.abilityId = Int32.Parse( abilityGroup.Properties["abilityId"].String );
			this.time = Int64.Parse( abilityGroup.Properties["time"].String );
			this.tick = Int64.Parse( abilityGroup.Properties["tick"].String );
			this.keepWhenTagging = Boolean.Parse( abilityGroup.Properties["keepWhenTagging"].String );
			this.isEffectDisable = Boolean.Parse( abilityGroup.Properties["isEffectDisable"].String );
		}
	}
	public class AbilityAttribute {
		public int key;
		public int paramType;
		public string commonEffect;

	}
}
