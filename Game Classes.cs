using System;
using System.Collections.Generic;
using System.IO;

namespace MFFDataApp
{
    public class Game
    {
        public string Name { get; set; }
        DataDirectory dataDir { get; set; }
        List<Version> Versions { get; set; }
        public Game(DirectoryInfo dir) {
            dataDir = new DataDirectory(dir);
            Versions = new List<Version>();
        }
        public Game(string gameName, DirectoryInfo dir) : this( dir ) {
            Name = gameName;
        }
        public void LoadAssets(Version version) {
            Version newVersion = new Version();
            newVersion.LoadAssets();
        }
        public void LoadAllAssets() {
            foreach ( Version version in Versions ) {
                LoadAssets(version);
            }
        }
        public void LoadComponents(Version version) {
            version.LoadComponents();
        }
         public void LoadAllComponents() {
            foreach ( Version version in Versions ) {
                LoadComponents(version);
            }
        }
        public void LoadAllVersions() {
            LoadAllAssets();
            LoadAllComponents();
        }
        public void LoadAllData() {
            Versions = dataDir.GetVersions();
            LoadAllVersions();
        }
    }
    public class Version
    {
        public string Name { get; set; }
        DirectoryInfo dir { get; set; }
        List<Component> Components { get; set; }
        AssetBundle Assets { get; set; }
        
        public Version() {
            Components = new List<Component>();
            Assets = new AssetBundle();
        }
        public Version(string versionName) : this () {
            Name = versionName;
        }
        public Version(string versionName, DirectoryInfo versionDir) : this (versionName) {
            dir = versionDir;
        }
        public void LoadAssets() {
            Assets.Load();
        }
        public void LoadComponents() {
            foreach (Component component in Components) {
                LoadComponent(component);
            }
        }
        public void LoadComponent(Component component) {
            component.Load();
            Components.Add(component);
        }
    }
    public class Component
    {
        public virtual void Load() {

        }
    }
    public class Roster : Component
    {
        public List<Character> Characters { get; set; }
    }
    public class Character
    {
        public string heroId;
        public string leaderSkillId;
        public string Name { get; set; }
        public List<Uniform> Uniforms { get; set; }
        public Skill[] Skills { get; set; }
        public Ability[] Abilities { get; set; }
        public string species;
        public string WorldBossAbility { get; set; }
        public string Stars { get; set; }
    }
    public class Ability
    {
        public string abilityId;
    }
    public class Uniform
    {
        public Character character { get; set; }
        public string ChangedName { get; set; }
        public Skill[] ChangedSkills { get; set; }
        public string Name { get; set; }
        public Uniform[] BonusUniforms { get; set; }
        public List<Uniform> SynergyUniforms { get; set; }
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
        public override void Load()
        {
            BaseFloors = new ShadowlandFloor[35];
            List<AssetObject> shadowlandFloors = Program.Assets.AssetFiles["text/data/shadowland_floor.csv"].Properties["m_Script"].Array;
            List<AssetObject> shadowlandRewards = Program.Assets.AssetFiles["text/data/shadowland_reward.csv"].Properties["m_Script"].Array;
            for (int floorNum = 0; floorNum < BaseFloors.Length; floorNum++)
            {
                ShadowlandFloor floor = new ShadowlandFloor();
                floor.FloorNumber = floorNum + 1;
                floor.BaseFloor = floor;
                foreach (AssetObject value in shadowlandRewards)
                {
                    if (value.Properties["REWARD_GROUP"].Value == shadowlandFloors[floorNum].Properties["REWARD_GROUP"].Value)
                    {
                        List<ShadowlandReward> rewards = new List<ShadowlandReward>();
                        for (int i = 1; i <= 2; i++)
                        {
                            ShadowlandReward reward = new ShadowlandReward();
                            reward.RewardValue = Int32.Parse(value.Properties[$"REWARD_VALUE_{i}"].Value);
                            reward.RewardQuantity = Int32.Parse(value.Properties[$"REWARD_QTY_{i}"].Value);
                            reward.RewardType = Int32.Parse(value.Properties[$"REWARD_TYPE_{i}"].Value);
                            rewards[i] = reward;
                        }
                    }
                    floor.RewardGroup = Int32.Parse(shadowlandFloors[floorNum].Properties["REWARD_GROUP"].Value);
                    floor.StageGroup = Int32.Parse(shadowlandFloors[floorNum].Properties["STAGE_GROUP"].Value);
                    floor.StageSelectCount = Int32.Parse(shadowlandFloors[floorNum].Properties["STAGE_SELECT_COUNT"].Value);
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
            public List<OpponentChoice> OpponentChoices;
        }
        public class Opponent
        {
            public Character character;
        }
        public class OpponentChoice
        {
            public List<Opponent> OpponentTeam;
            public List<Opponent> OpponentTeamDisplay;
            public List<ShadowlandReward> Rewards;
        }
        public class ShadowlandReward
        {
            public Item RewardItem;
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
        private int rewardGroupId;
        private int rewardType;
        private int rewardValue;
    }
    public class FuturePass : Component
    {
        public List<FuturePassSeason> Seasons { get; set; }
        public FuturePassStep[] Steps { get; set; }
        public Dictionary<int, int> StagePoints { get; set; }

        public override void Load()
        {
            AssetBundle Assets = Program.Assets;
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
                step.passPoint = Int32.Parse(stepAsset.Properties["data"].Properties["passPoint"].Value);
                step.step = Int32.Parse(stepAsset.Properties["data"].Properties["step"].Value);
                step.Rewards = new Dictionary<FuturePassType, FuturePassReward>();
                Steps[step.step - 1] = step;
            }
            string rewardAssetName = "text/data/future_pass_reward.asset";
            foreach (AssetObject rewardAsset in Assets.AssetFiles[rewardAssetName].Properties["list"].Array)
            {
                FuturePassReward reward = new FuturePassReward();
                reward.Load(rewardAsset);
                FuturePassType level = (FuturePassType)Int32.Parse(rewardAsset.Properties["data"].Properties["grade"].Value);
                int step = Int32.Parse(rewardAsset.Properties["data"].Properties["step"].Value);
                Steps[step - 1].Rewards[level] = reward;
            }
            string stageAssetName = "text/data/future_pass_contents.asset";
            StagePoints = new Dictionary<int, int>();
            foreach (AssetObject stageAsset in Assets.AssetFiles[stageAssetName].Properties["list"].Array)
            {
                int sceneId = Int32.Parse(stageAsset.Properties["data"].Properties["sceneId"].Value);
                int stagePoints = Int32.Parse(stageAsset.Properties["data"].Properties["passPoint"].Value);
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
                this.rewardId = Int32.Parse(asset.Properties["data"].Properties["rewardId"].Value);
                this.rewardGroupId = Int32.Parse(asset.Properties["data"].Properties["rewardGroupId"].Value);
                this.rewardType = Int32.Parse(asset.Properties["data"].Properties["rewardType"].Value);
                this.rewardValue = Int32.Parse(asset.Properties["data"].Properties["rewardValue"].Value);
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
                this.endTime = asset.Properties["data"].Properties["endTime_unused"].Value;
                this.startTime = asset.Properties["data"].Properties["startTime_unused"].Value;
                this.rewardGroupId = Int32.Parse(asset.Properties["data"].Properties["rewardGroupId"].Value);
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
            this.groupId = Int32.Parse(abilityGroup.Properties["groupId"].Value);
            this.abilityId = Int32.Parse(abilityGroup.Properties["abilityId"].Value);
            this.time = Int64.Parse(abilityGroup.Properties["time"].Value);
            this.tick = Int64.Parse(abilityGroup.Properties["tick"].Value);
            this.keepWhenTagging = Boolean.Parse(abilityGroup.Properties["keepWhenTagging"].Value);
            this.isEffectDisable = Boolean.Parse(abilityGroup.Properties["isEffectDisable"].Value);
        }
    }
    public class AbilityAttribute {
        public int key;
        public int paramType;
        public string commonEffect;
        
    }
}