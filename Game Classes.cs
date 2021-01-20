using System;
using System.Collections.Generic;
using System.IO;

namespace MFFDataApp
{
    public class Game
    {
        public string Name { get; set; }
        public List<Version> Versions { get; set; }
        public Game(string gameName )  {
            Name = gameName;
            Versions = new List<Version>();
        }
        public void LoadAllData(string dir) {
            DataDirectory dataDir = new DataDirectory(dir);
            List<string> versionNames = dataDir.GetVersionNames();
            foreach ( string versionName in versionNames ) {
                Version version = new Version(versionName);
                version.Assets = dataDir.GetAssets(versionName);
                version.LoadComponents();
                Versions.Add(version); // if not already included?
            }
        }
        public void SaveAllData( string fileName ) {
            // implemented as streamwriter at all levels because using a string or
            // similar uses up all memory, same with JsonSerializer
            using (StreamWriter file = new StreamWriter(fileName))
            {
                file.WriteLine("{");
                file.WriteLine($"\t\"{Name}\" : " +"{");
                int versionCounter = 0;
                foreach ( Version version in Versions ) {
                    // WriteJson should consistently write the instance as one or more
                    // JSON members (string: element) without a bare root element, and without 
                    // a newline on the last line. It is on the caller to provide appropriate 
                    // wrapping. The (optional) second argument prepends each line of
                    // the JSON output with that number of tabs
                    version.WriteJson(file,2);
                    versionCounter++;
                    if ( versionCounter < Versions.Count - 1 ) {
                        file.Write(",");
                    }
                    file.WriteLine("");
                }
                file.WriteLine("\t}");
                file.WriteLine("}");
            }
            return;
        }
    }
    public class Version
    {
        public string Name { get; set; }
        public List<Component> Components { get; set; }
        public AssetBundle Assets { get; set; }
        public Version() {
            // should have a predefined list/dictionary of components/assetobject names
            // that a version constructor can use to create the appropriate component
            // list, which can then be filled with LoadComponents() or similar?
            Name = "";
            Components = new List<Component>();
            Assets = new AssetBundle();

            Roster roster = new Roster(); Components.Add(roster);
        }
        public Version(string versionName) : this () {
            Name = versionName;
        }
        AssetObject GetAsset( string assetName )  {
            return null;
        }
        public void LoadComponents() {
            Dictionary<string,string> strings = new Dictionary<string, string>();
            AssetObject localization = Assets.AssetFiles["localization/localization_en.csv"].Properties["m_Script"];
            foreach ( AssetObject entry in localization.Array ) {
                strings[entry.Properties["KEY"].String] = entry.Properties["TEXT"].String;
            }
            foreach (Component component in Components) {
                LoadComponent(component,strings);
            }
        }
        public void LoadComponent(Component component, Dictionary<string,string> strings) {
            component.Load( Assets.AssetFiles["IntHeroDataDictionary"].Properties["values"].Properties["Array"], strings );
        }
        public void WriteJson( StreamWriter file, int tabs = 0 ) {
            for ( int i = 0; i < tabs; i++ ) {
                file.Write("\t");
            }
            file.WriteLine($"\"{Name}\" : " + "{");
            Assets.WriteJson(file,tabs+1);
            if ( Components.Count > 0 ) {
                file.WriteLine(",");
                for ( int i = 0; i < tabs+1; i++ ) {
                    file.Write("\t");
                }                
                file.WriteLine("\"Components\" : {");
                int componentCounter = 0;
                foreach (Component component in Components) {
                    component.WriteJson(file,tabs+1);
                    componentCounter++;
                    if (componentCounter < Components.Count - 1) {
                        file.Write(",");
                    }
                }
            }
            file.WriteLine();
            for ( int i = 0; i < tabs; i++ ) {
                file.Write("\t");
            }                
            file.Write("}");
        }
    }
    public class Component
    {
        protected List<string> BackingAssets { get; set; }
        protected Component() {
            BackingAssets = new List<string>();
        }
        public void WriteJson(StreamWriter file, int tabs = 0) {

        }
        public virtual void WriteCSV( StreamWriter file ) {

        }
        public virtual void Load( AssetObject asset, Dictionary<string,string> strings ) {

        }
    }
    // List of all available playable characters in the game
    public class Roster : Component
    {
        public Dictionary<string,Character> Characters { get; set; } // by groupId
        public Roster() : base() {
            Characters = new Dictionary<string, Character>();
            BackingAssets.Add("IntHeroDataDictionary");
        }
        // See also explanation of HeroId, BaseId, GroupId, and UniformGroupId in comments for
        // Character class. This is all based on certain assumptions, which should probably all
        // be tested here. For instance, when adding info, ensure it's not redundant or inconsistent.
        public override void Load(AssetObject asset, Dictionary<string,string> strings) {
            List<string> AllHeroIds = new List<string>();
            foreach ( AssetObject entry in asset.Array ) {
                if ( entry.Properties["data"].Properties["isVisible"].String == "1" ) {
                    // Consider a note or warning if there's one that's not visible?
                    Character character;
                    string groupId = entry.Properties["data"].Properties["groupId"].String;
                    if ( Characters.ContainsKey(groupId) ) {
                        character = Characters[groupId];
                    } else {
                        character = new Character();
                        character.GroupId=groupId;
                        Characters.Add(groupId, character);
                    }
                    string heroId = entry.Properties["data"].Properties["heroId"].String;
                    if ( AllHeroIds.Contains(heroId) ) {
                        throw new Exception($"HeroID {heroId} has already been used.");
                    } else {
                        AllHeroIds.Add(heroId);
                    }
                    character.Levels[heroId] = new CharacterLevel();
                    character.Levels[heroId].HeroId = heroId;
                    character.Levels[heroId].Rank = Int32.Parse( entry.Properties["data"].Properties["grade"].String );
                    character.Levels[heroId].Tier = Int32.Parse( entry.Properties["data"].Properties["tier"].String );
                    string baseId = character.Levels[heroId].BaseId;
                    if ( ! character.Uniforms.ContainsKey(baseId) ) {
                        Uniform uniform = new Uniform();
                        uniform.BaseId = baseId;
                        uniform.Camps = strings[ "HERO_SUBTYPE_" + entry.Properties["data"].Properties["stCamps"].String ];
                        uniform.CharacterName = strings[$"HERO_{baseId}"];
                        uniform.ClassType = strings[ "HEROCLASS_" + entry.Properties["data"].Properties["classType"].String ];
                        uniform.Gender = strings[ "HERO_SUBTYPE_" + entry.Properties["data"].Properties["stGender"].String ];
                        uniform.UniformGroupId = entry.Properties["data"].Properties["uniformGroupId"].String;
                        uniform.UniformName = strings[$"HERO_COSTUME_{baseId}"];
                        if ( entry.Properties["data"].Properties["ability_raid"].String != "0" ) {
                            uniform.RaidAbility = strings["HERO_SUBTYPE_" + entry.Properties["data"].Properties["ability_raid"].String ];
                        }
                        foreach ( AssetObject ability in entry.Properties["data"].Properties["abilitys"].Properties["Array"].Array ) {
                            if ( ability.Properties["data"].String != "0" ) {
                                uniform.Abilities.Add( strings["HERO_SUBTYPE_" + ability.Properties["data"].String]);
                            }
                        }
                        character.Uniforms.Add(baseId,uniform);
                        if ( String.IsNullOrEmpty( character.BaseName ) ) {
                            if ( uniform.UniformGroupId == "0" ) {
                                character.BaseName = strings[$"HERO_{baseId}"];
                            }
                        }
                    }
                    character.Species = strings[ "HERO_SUBTYPE_" + entry.Properties["data"].Properties["species"].String ];
                    character.StartGrade = Int32.Parse( entry.Properties["data"].Properties["startGrade"].String );
                    // other things to consider including: max level, tier, grade/rank (all of which are likely calculable from above),
                    // skills/abilities/stats (some of which are already included)
                }
            }
        }
        public override void WriteCSV(StreamWriter file) {
            file.WriteLine("Group ID|Base ID|BaseName|Character Name|Uniform Name|Uniform Group Id|Type|Gender|Side|Allies|Abilities|World Boss Ability");
            foreach ( Character character in Characters.Values ) {
                foreach ( Uniform uniform in character.Uniforms.Values ) {
                    file.Write($"{character.GroupId}|{uniform.BaseId}|{character.BaseName}|{uniform.CharacterName}|");
                    file.Write($"{uniform.UniformName}|{uniform.UniformGroupId}|{uniform.ClassType}|");
                    file.Write($"{uniform.Gender}|{uniform.Camps}|{character.Species}|");
                    int size = uniform.Abilities.Count;
                    for ( int i=0; i<size; i++ ) {
                        file.Write( uniform.Abilities[i] );
                        if ( i<size-1 ) file.Write(",");
                    }
                    file.Write($"|{uniform.RaidAbility}");
                    file.WriteLine();
                }
            }
        }
    }
    // Some findings/assumptions about the multiple identifiers associated with a character and their
    // settings/equipment follow. These should likely be tested at the time of import to ensure 
    // they continue to hold.
    // groupId and Character map to each other one-to-one
    // baseId (calculated from heroId) and Uniform map one-to-one, but many-to-one Character
    // heroId many-to-one baseId
    // uniformGroupId is not unique between characters; the "default" (non-uniformed state) for each
    // Character has uniformGroupId 0.
    // Characteristics/properties are arranged at the level at which they may vary. For instance,
    // Species is a property of Character, while Gender is a property of Uniform
    public class Character
    {
        public string GroupId { get; set; }
        public Dictionary<string,CharacterLevel> Levels { get; set; } // by heroId
        public Dictionary<string,Uniform> Uniforms { get; set; } // by baseId
        public string BaseName { get; set; }
        public int StartGrade { get; set; }
        public string Species { get; set; }
        public Character() {
            Levels = new Dictionary<string, CharacterLevel>();
            Uniforms = new Dictionary<string, Uniform>();
        }
    }
    public class CharacterLevel {
        public string HeroId { get; set; }
        public int Rank { get; set; }
        public int Tier { get; set; }
        public string BaseId { 
            get {
                Int64 heroIdNumber = Int64.Parse(HeroId);
                Int64 heroIdNumber1 = (heroIdNumber*0x51eb851f) >> 32;
                Int64 heroIdNumber2 = heroIdNumber1 >> 31;
                heroIdNumber1 = heroIdNumber1 >> 5;
                heroIdNumber = heroIdNumber1 + heroIdNumber2;
                heroIdNumber = heroIdNumber*100+1;
                return heroIdNumber.ToString();
            }
        } 
        public CharacterLevel() {
        }
    }
    public class Ability
    {
        public string abilityId;
    }
    public class Uniform
    {
        public string UniformName { get; set; }
        public string CharacterName { get; set; }
        public string UniformGroupId { get; set; }
        public string Camps { get; set; }
        public string Gender { get; set; }
        public string BaseId { get; set; }
        public string ClassType { get; set; }
        public string RaidAbility { get; set; }
        public List<string> Abilities { get; set; }
        public Uniform() {
            Abilities = new List<string>();
        }
    }
    public class Skill
    {
        public string Name { get; set; }
    }
    public class Alliance
    {
        public string Name { get; set; }
        public List<Player> Players { get; set; }
        public Player Leader { get; set; }
        public List<Player> Class1Players { get; set; }
        public List<Player> Class2Players { get; set; }
    }
    public class Item
    {
        public string Name { get; set; }
    }
    class Shadowland : Component
    {
        ShadowlandFloor[] BaseFloors;
        public void Load( AssetBundle Assets )
        {
            BaseFloors = new ShadowlandFloor[35];
            List<AssetObject> shadowlandFloors = Assets.AssetFiles["text/data/shadowland_floor.csv"].Properties["m_Script"].Array;
            List<AssetObject> shadowlandRewards = Assets.AssetFiles["text/data/shadowland_reward.csv"].Properties["m_Script"].Array;
            for (int floorNum = 0; floorNum < BaseFloors.Length; floorNum++)
            {
                ShadowlandFloor floor = new ShadowlandFloor();
                floor.FloorNumber = floorNum + 1;
                floor.BaseFloor = floor;
                foreach (AssetObject value in shadowlandRewards)
                {
                    if (value.Properties["REWARD_GROUP"].String == shadowlandFloors[floorNum].Properties["REWARD_GROUP"].String)
                    {
                        List<ShadowlandReward> rewards = new List<ShadowlandReward>();
                        for (int i = 1; i <= 2; i++)
                        {
                            ShadowlandReward reward = new ShadowlandReward();
                            reward.RewardValue = Int32.Parse(value.Properties[$"REWARD_VALUE_{i}"].String);
                            reward.RewardQuantity = Int32.Parse(value.Properties[$"REWARD_QTY_{i}"].String);
                            reward.RewardType = Int32.Parse(value.Properties[$"REWARD_TYPE_{i}"].String);
                            rewards[i] = reward;
                        }
                    }
                    floor.RewardGroup = Int32.Parse(shadowlandFloors[floorNum].Properties["REWARD_GROUP"].String);
                    floor.StageGroup = Int32.Parse(shadowlandFloors[floorNum].Properties["STAGE_GROUP"].String);
                    floor.StageSelectCount = Int32.Parse(shadowlandFloors[floorNum].Properties["STAGE_SELECT_COUNT"].String);
                    BaseFloors[floorNum] = floor;
                }
            }
        }

        public class ShadowlandFloor
        {
            public int FloorNumber;
            public ShadowlandFloor BaseFloor;
            public int RewardGroup;
            public int StageGroup;
            public int StageSelectCount;
        }
        public class Opponent
        {
        }
        public class OpponentChoice
        {

        }
        public class ShadowlandReward
        {
            public int RewardQuantity;
            public int RewardValue;
            public int RewardType;
        }
    }
    public class ComicCard
    {
        public string cardId;
    }
    public class ComicCardCollection
    {
        public ComicCard[] Cards { get; set; }
        public string cardGroup;
        public string abilityId;
        public string abilityParam;

        private void LoadById(string id)
        {
            
        }
    }
    public class Reward
    {
        public Item item { get; set; }
        public int Quantity { get; set; }
    }
    public class FuturePass : Component
    {
        public List<FuturePassSeason> Seasons { get; set; }
        public FuturePassStep[] Steps { get; set; }
        public Dictionary<int, int> StagePoints { get; set; }

        public void Load( AssetBundle Assets )
        {
            string seasonAssetName = "text/data/future_pass.asset";
            List<FuturePassSeason> seasons = new List<FuturePassSeason>();
            foreach (AssetObject seasonAsset in Assets.AssetFiles[seasonAssetName].Properties["list"].Array)
            {
                FuturePassSeason season = new FuturePassSeason();
                season.Load(seasonAsset);
                seasons.Add(season);
            }
            string stepAssetName = "text/data/future_pass_step.asset";
            Steps = new FuturePassStep[50];
            foreach (AssetObject stepAsset in Assets.AssetFiles[stepAssetName].Properties["list"].Array)
            {
                FuturePassStep step = new FuturePassStep();
                step.passPoint = Int32.Parse(stepAsset.Properties["data"].Properties["passPoint"].String);
                step.step = Int32.Parse(stepAsset.Properties["data"].Properties["step"].String);
                step.Rewards = new Dictionary<FuturePassType, FuturePassReward>();
                Steps[step.step - 1] = step;
            }
            string rewardAssetName = "text/data/future_pass_reward.asset";
            foreach (AssetObject rewardAsset in Assets.AssetFiles[rewardAssetName].Properties["list"].Array)
            {
                FuturePassReward reward = new FuturePassReward();
                reward.Load(rewardAsset);
                FuturePassType level = (FuturePassType)Int32.Parse(rewardAsset.Properties["data"].Properties["grade"].String);
                int step = Int32.Parse(rewardAsset.Properties["data"].Properties["step"].String);
                Steps[step - 1].Rewards[level] = reward;
            }
            string stageAssetName = "text/data/future_pass_contents.asset";
            StagePoints = new Dictionary<int, int>();
            foreach (AssetObject stageAsset in Assets.AssetFiles[stageAssetName].Properties["list"].Array)
            {
                int sceneId = Int32.Parse(stageAsset.Properties["data"].Properties["sceneId"].String);
                int stagePoints = Int32.Parse(stageAsset.Properties["data"].Properties["passPoint"].String);
                StagePoints.Add(sceneId, stagePoints);
            }
        }
        public class FuturePassReward : Reward
        {
            // text/data/future_pass_reward.asset->list->Array[x]->data
            private int rewardId;
            private int rewardGroupId;
            private int rewardType;
            private int rewardValue;
            public void Load(AssetObject asset)
            {
                this.rewardId = Int32.Parse(asset.Properties["data"].Properties["rewardId"].String);
                this.rewardGroupId = Int32.Parse(asset.Properties["data"].Properties["rewardGroupId"].String);
                this.rewardType = Int32.Parse(asset.Properties["data"].Properties["rewardType"].String);
                this.rewardValue = Int32.Parse(asset.Properties["data"].Properties["rewardValue"].String);
            }
        }
        public class FuturePassSeason
        {
            // text/data/future_pass.asset->list->Array[x]->data
            string endTime { get; set; }
            string startTime { get; set; }
            int rewardGroupId { get; set; }
            public void Load(AssetObject asset)
            {
                this.endTime = asset.Properties["data"].Properties["endTime_unused"].String;
                this.startTime = asset.Properties["data"].Properties["startTime_unused"].String;
                this.rewardGroupId = Int32.Parse(asset.Properties["data"].Properties["rewardGroupId"].String);
            }
        }
        public class FuturePassStep
        {
            // text/data/future_pass_step.asset->list->Array[x]->data
            public int step { get; set; } // 1-50
            public int passPoint { get; set; } // total points to get to this step
            public Dictionary<FuturePassType, FuturePassReward> Rewards { get; set; }
        }
        public enum FuturePassType
        {
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
            this.groupId = Int32.Parse(abilityGroup.Properties["groupId"].String);
            this.abilityId = Int32.Parse(abilityGroup.Properties["abilityId"].String);
            this.time = Int64.Parse(abilityGroup.Properties["time"].String);
            this.tick = Int64.Parse(abilityGroup.Properties["tick"].String);
            this.keepWhenTagging = Boolean.Parse(abilityGroup.Properties["keepWhenTagging"].String);
            this.isEffectDisable = Boolean.Parse(abilityGroup.Properties["isEffectDisable"].String);
        }
    }
    public class AbilityAttribute {
        public int key;
        public int paramType;
        public string commonEffect;
        
    }
}