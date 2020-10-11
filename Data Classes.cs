﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace MFFDataApp
{
    // still much off AssetBundle and AssetObject class referring to named directories
    //
    // DataDirectory need not be a proper directory tree, but forms a secondary rooted
    // directory hierarchy. 
    //
    // dirs are directories added from constructors or other external calls and are not 
    //   validated at the time of addition.
    // directories may be data directories, game directories, or version directories.
    // version directories contain exactly one subdirectory named "json"
    // game directories contain one or more version directories
    // data directories contain one or more game directories; if Game is nonempty, 
    //   data directories contain one or more game directories named Game
    // directories may be manually placed in dataDirs, gameDirs, or versionDirs; this
    //   will override any auto-detection but could result in an error-prone DataDirectory.
    //   Therefore, we can secure this as long as the lists are private and no accessible methods 
    //   add a directory to any list other than dirs without validation. 
    // LoadSubdirs() checks each directory in dirs to ensure it is already in dataDirs,
    //   gameDirs, or versionDirs or that it is exactly one of a data directory, game directory,
    //   or version directory and adds it to the appropriate directory list. It then checks
    //   each subdirectory of each directory listed in each directory list to automatically
    //   determine if the subdirectory should be added to one of the directory lists.
    // 
    public class DataDirectory {
        string Game { get; set; }
        List<DirectoryInfo> dirs { get; set; }
        List<DirectoryInfo> dataDirs { get; set; }
        List<DirectoryInfo> gameDirs { get; set; }
        Dictionary< string, List<DirectoryInfo> > versionDirs { get; set; }
        enum dirType {
            data, version, game
        }
        DataDirectory() {
            dirs = new List<DirectoryInfo>();
            gameDirs = new List<DirectoryInfo>();
            versionDirs = new Dictionary< string, List<DirectoryInfo> >();
        }
        public DataDirectory( DirectoryInfo directory ) : this() {
            dirs.Add(directory);
        }
        public DataDirectory( string pathName ) : this() {
            if ( ! Directory.Exists( pathName ) ) {
                throw new DirectoryNotFoundException($"Unable to access directory {pathName}");
            } else {
                dirs.Add( new DirectoryInfo(pathName) );
            }
        }
        public DataDirectory( string gameName, string directory) : this ( directory ) {
            Game = gameName;
        }
        dirType? GetDirectoryType(DirectoryInfo directory) {
            string pathName = directory.FullName;
            dirType? returnType = null;
            foreach ( DirectoryInfo dir in dirs ) {
                if ( pathName == dir.FullName ) {
                    returnType = dirType.data;
                    break;
                }
            }
            foreach ( DirectoryInfo gameDir in gameDirs ) {
                if ( pathName == gameDir.FullName ) {
                    if ( returnType == dirType.data ) {
                        ThrowBadDataDir();
                    }
                    returnType = dirType.game;
                    break;
                }
            }
            foreach ( List<DirectoryInfo> versionDirList in versionDirs.Values ) {
                foreach ( DirectoryInfo versionDir in versionDirList ) {
                    if ( pathName == versionDir.FullName ) {
                        if ( returnType == dirType.data || returnType == dirType.game ) {
                            ThrowBadDataDir();
                        }
                        returnType = dirType.version;
                        return returnType;
                    }
                }
            }
            if ( returnType != null ) {
                return returnType;
            }
            if ( IsVersionDirectory(directory) ) {
                returnType = dirType.version;
            }
            if ( IsGameDirectory(directory) ) {
                if ( returnType == null ) {
                    returnType = dirType.game;
                } else {
                    return null;
                }
            }
            if ( IsDataDirectory(directory) ) {
                if ( returnType == null ) {
                    returnType = dirType.data;
                } else {
                    return null;
                }
            }
            return returnType;
        }
        public override string ToString() {
            String returnString = null;
            if ( ! String.IsNullOrEmpty(Game) ) returnString += Game + ":\n";
            foreach ( DirectoryInfo dir in dirs ) {
                returnString += dir.FullName + "\n";
            }
            return returnString;
        }
        void LoadSubdirs() {
            if ( dirs.Count() == 0 ) {
                throw new Exception("No data directories defined.");
            }
            foreach ( DirectoryInfo dir in dirs ) {
                switch ( GetDirectoryType(dir) ) {
                    case ( dirType.data ):
                        if ( ! IsIncluded(dir, dataDirs) ) dataDirs.Add( dir );
                        break;
                    case ( dirType.game ):
                        if ( ! IsIncluded(dir, gameDirs) ) gameDirs.Add( dir );
                        break;
                    case ( dirType.version ):
                        if ( ! IsIncluded(dir,versionDirs) ) {
                            string versionName = dir.Name;
                            if ( versionDirs.ContainsKey(versionName) ) {
                                versionDirs[versionName].Add(dir);
                            } else {
                                versionDirs[versionName] = new List<DirectoryInfo>{ dir };
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
            foreach ( DirectoryInfo dir in dataDirs ) {
                foreach ( DirectoryInfo subdir in dir.GetDirectories() ) {

                }
            }
        }
        bool IsIncluded( DirectoryInfo directory, List<DirectoryInfo> dirList ) {
            foreach ( DirectoryInfo dir in dirList ) {
                if ( dir.FullName == directory.FullName ) return true;
            }
            return false;
        }
        bool IsIncluded( DirectoryInfo directory, Dictionary< string, List<DirectoryInfo> > versionList ) {
            foreach ( string key in versionList.Keys ) {
                if ( IsIncluded( directory, versionList[key] ) ) return true;
            }
            return false;
        }
        bool IsDataDirectory(DirectoryInfo directory) {
            if ( IsIncluded( directory, dataDirs ) ) return true;
            dirType? subdirType = null;
            foreach ( DirectoryInfo subdir in directory.GetDirectories() ) {
                if ( IsGameDirectory(subdir) ) {
                    if ( subdirType == dirType.version ) {
                        return false;
                    } else { 
                        subdirType = dirType.game;
                    }
                } else if ( IsVersionDirectory(subdir) ) {
                    if ( subdirType == dirType.game ) {
                        return false;
                    } else {
                        subdirType = dirType.version;
                    }
                }
            }
            if ( subdirType == null ) {
                return false;
            } else {
                return true;
            }
        }
        bool IsGameDirectory(DirectoryInfo directory) {
            foreach ( DirectoryInfo subdir in directory.GetDirectories() ) {
                if ( IsVersionDirectory(subdir) ) {
                    return true;
                }
            }
            return false;
        }
        bool IsVersionDirectory(DirectoryInfo directory) {
            DirectoryInfo[] jsonDirs = directory.GetDirectories("json");
            if ( jsonDirs.Length == 1 ) { 
                return true;
            } else {
                return false;
            }
        }
        void ThrowBadDataDir() {
            throw new Exception($"Unable to define structure of data directory {this.ToString()}.");
        }
        public Version GetVersion( string versionName ) {
            Version version = new Version();
            version.Name = versionName;
            if ( ! versionDirs.ContainsKey(versionName) ) {
                LoadSubdirs();
            }
            version.Assets = GetAssets(versionName);
            return version;
        }
        public List<Version> GetVersions() {
            List<Version> returnList = new List<Version>();
            if ( versionDirs.Count() == 0 ) {
                LoadSubdirs();
            }
            foreach ( string versionName in versionDirs.Keys ) {
                returnList.Add( GetVersion( versionName ) );
            }
            return returnList;
        }
        AssetBundle GetAssets( string versionName ) {
            AssetBundle assets = new AssetBundle();
            foreach ( DirectoryInfo versionDir in versionDirs[versionName] ) {
                assets.LoadFromDirectory(versionDir.FullName);
            }
            return assets;
        }
    }
    public class AssetBundle
    {
        private static readonly string gameVersion = "6.2.0";
        public static readonly string assetDir = "/Users/chjones/Downloads/APK/Marvel Future Fight/assets/" + gameVersion + "/";
        private static readonly string completeFile = $"{assetDir}assets.json";
        public Dictionary<string, AssetObject> AssetFiles { get; set; }
        public Dictionary<string, string> manifest { get; set; }
        private Dictionary<string, string> scripts { get; set; }
        public int Count
        {
            get
            {
                return AssetFiles.Count;
            }
        }
        public int Depth
        {
            get
            {
                int depth = 1;
                foreach (AssetObject asset in AssetFiles.Values)
                {
                    int newDepth = asset.Depth + 1;
                    if (newDepth > depth)
                    {
                        depth = newDepth;
                    }
                }
                return depth;
            }
        }
        public AssetBundle()
        {
            AssetFiles = new Dictionary<string, AssetObject>();
            manifest = new Dictionary<string, string>();
        }
        public void ToJsonFile()
        {
            using (StreamWriter file = new StreamWriter(completeFile))
            {
                file.WriteLine("{");
                int assetCounter = 0;
                List<string> keys = AssetFiles.Keys.ToList();
                keys.Sort();
                foreach (string key in keys)
                {
                    file.Write("\t\"" + key + "\" : ");
                    AssetFiles[key].ToJsonFile(file, 2);
                    if (assetCounter != AssetFiles.Keys.Count - 1)
                    {
                        file.WriteLine(", ");
                    }
                    assetCounter++;
                }
                file.WriteLine("}");
            }
            return;
        }
        // should ensure LoadFromDirectory() can be run for multiple directories in the same bundle?
        public void LoadFromDirectory(string directory)
        {
            string[] manifestFiles = Directory.GetFiles(directory, "*-AssetBundle.json");
            if (manifestFiles.Length != 1)
            {
                throw new Exception($"Found {manifestFiles.Length} manifest files in directory {directory} when there must be exactly one.");
            }
            AssetObject manifestAsset = new AssetObject().LoadFromFile(manifestFiles[0]);
            foreach (AssetObject entry in manifestAsset.Properties["m_Container"].Properties["Array"].Array)
            {
                string pathID = entry.Properties["data"].Properties["second"].Properties["asset"].Properties["m_PathID"].Value;
                if (pathID.StartsWith('-'))
                {
                    UInt64 newID = System.Convert.ToUInt64(pathID.Substring(1));
                    newID = UInt64.MaxValue - newID + 1;
                    pathID = newID.ToString();
                }
                manifest.Add(pathID, entry.Properties["data"].Properties["first"].Value);
            }
            string[] jsonFiles = Directory.GetFiles(directory);
            foreach (string jsonFile in jsonFiles)
            {
                // try {
                AssetObject asset = new AssetObject().LoadFromFile(jsonFile);
                if (asset != null) AssetFiles.Add(asset.AssetName, asset);
                // } catch (JsonException) {
                //     continue;
                // }
            }
        }
        public void Load()
        {
            LoadFromDirectory(assetDir + "json/");
            return;
        }
    }
    public class AssetObject
    {
        public string FileName { get; set; }
        public string AssetName { get; set; }
        public Dictionary<string, AssetObject> Properties { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        [JsonIgnore]
        public AssetObject Parent { get; set; }
        public string AssetType { get; set; }
        public JsonValueKind Type { get; set; }
        public List<AssetObject> Array { get; set; }
        [JsonIgnore]
        private bool loaded { get; set; }
        public string ScriptClass { get; set; }
        [JsonIgnore]
        public int Depth
        {
            get
            {
                int depth = 1;
                foreach (AssetObject asset in Properties.Values)
                {
                    int newDepth = asset.Depth + 1;
                    if (newDepth > depth) depth = newDepth;
                }
                foreach (AssetObject asset in Array)
                {
                    int newDepth = asset.Depth + 1;
                    if (newDepth > depth) depth = newDepth;
                }
                return depth;
            }
        }
        public AssetObject()
        {
            this.Properties = new Dictionary<string, AssetObject>();
            this.Array = new List<AssetObject>();
            this.loaded = false;
        }
        public void ToJsonFile(StreamWriter file, int indentTabs)
        {
            switch (this.Type)
            {
                case JsonValueKind.Object:
                    file.WriteLine("{");
                    int counter = 0;
                    string tabstring = "";
                    for (int tabs = 0; tabs < indentTabs; tabs++)
                    {
                        tabstring += "\t";
                    }
                    List<string> keys = Properties.Keys.ToList();
                    foreach (string key in keys)
                    {
                        file.Write(tabstring);
                        file.Write("\"" + this.Properties[key].Name + "\" : ");
                        this.Properties[key].ToJsonFile(file, indentTabs + 1);
                        if (counter != this.Properties.Keys.Count - 1)
                        {
                            file.WriteLine(",");
                        }
                        counter++;
                    }
                    if (this.Name == "Base" && this.AssetType == "MonoBehaviour")
                    {
                        file.WriteLine(",\n\t\t\"Script Class\": \"" + this.ScriptClass + "\"");
                    }
                    else
                    {
                        file.WriteLine("");
                    }
                    string closingtabs = tabstring.Substring(0, tabstring.Length - 1);
                    file.Write(closingtabs);
                    file.Write("}");
                    return;
                case JsonValueKind.Array:
                    file.WriteLine(" [");
                    tabstring = "";
                    for (int tabs = 0; tabs < indentTabs; tabs++)
                    {
                        tabstring += "\t";
                    }
                    for (counter = 0; counter < this.Array.Count; counter++)
                    {
                        file.Write(tabstring);
                        this.Array[counter].ToJsonFile(file, indentTabs + 1);
                        if (counter != this.Array.Count - 1)
                        {
                            file.WriteLine(", ");
                        }
                        else
                        {
                            file.WriteLine("");
                        }
                    }
                    closingtabs = tabstring.Substring(0, tabstring.Length - 1);
                    file.Write(closingtabs + "]");
                    break;
                case JsonValueKind.Undefined:
                    throw new Exception($"Unable to identify appropriate JSON conversion for item {this.Name} in asset {this.AssetName}.");
                default:
                    file.Write("\"" + this.Value + "\"");
                    break;
            }
        }
        public bool IsLoaded()
        {
            return this.loaded;
        }
        public AssetObject LoadFromFile(string filename)
        {
            string jsonText = File.ReadAllText(filename);
            JsonDocument json = ParseJson(jsonText);
            JsonElement jsonRoot = json.RootElement;
            JsonValueKind jsonType = jsonRoot.ValueKind;
            if (jsonType != JsonValueKind.Object)
            {
                throw new JsonException($"{filename} is not a valid asset file");
            }
            AssetObject assetObject = new AssetObject();
            assetObject.FileName = filename;
            int propertyCount = 0;
            foreach (JsonProperty jsonProperty in jsonRoot.EnumerateObject())
            {
                propertyCount++;
                assetObject.Name = jsonProperty.Name.Split(' ', 3)[2];
                assetObject.AssetType = jsonProperty.Name.Split(' ', 3)[1];
                assetObject.Value = jsonProperty.Value.ToString();
                assetObject.Type = jsonProperty.Value.ValueKind;
            }
            if (propertyCount == 0)
            {
                throw new JsonException($"{filename} does not contain any JSON members beyond root.");
            }
            else if (propertyCount != 1)
            {
                throw new JsonException($"{filename} contains more than one top level member.");
            }
            else if (assetObject.Type != JsonValueKind.Object)
            {
                throw new JsonException($"{filename} root element is type {assetObject.Type.ToString()} rather than JSON object.");
            }
            string pathID = Regex.Replace(filename, "^.*-([0-9]*)-[^-]*$", "$1");
            if (assetObject.AssetType == "AssetBundle")
            {
                JsonDocument assetBundle = ParseJson(assetObject.Value);
                foreach (JsonProperty property in assetBundle.RootElement.EnumerateObject())
                {
                    if (property.Name == "m_AssetBundleName")
                    {
                        assetObject.Name = property.Value.ToString();
                    }
                }
                assetBundle.Dispose();
                assetObject.AssetName = assetObject.Name;
            }
            else if (assetObject.AssetType == "MonoScript")
            {
                json.Dispose();
                return null;
            }
            else
            {
                try
                {
                    // assetObject.AssetName = Program.Assets.manifest[pathID];
                }
                catch
                {
                    json.Dispose();
                    throw new Exception($"File {filename} (type {assetObject.AssetType}) did not match any key in the manifest.");
                }
            }
            assetObject.ParseValue();
            json.Dispose();
            if (assetObject.AssetType == "MonoBehaviour")
            {
                try
                {
                    string scriptPathID = assetObject.Properties["m_Script"].Properties["m_PathID"].Value;
                    if (scriptPathID.StartsWith('-'))
                    {
                        UInt64 newID = System.Convert.ToUInt64(scriptPathID.Substring(1));
                        newID = UInt64.MaxValue - newID + 1;
                        scriptPathID = newID.ToString();
                    }
                    string scriptFile = Directory.GetFiles(AssetBundle.assetDir + "json/", $"*-{scriptPathID}-*")[0];
                    json = ParseJson(File.ReadAllText(scriptFile));
                    assetObject.ScriptClass = json.RootElement.GetProperty("0 MonoScript Base").GetProperty("1 string m_ClassName").ToString();
                    json.Dispose();
                }
                catch (Exception)
                {

                }
            }
            return assetObject;
        }
        public void Load(string newValue)
        {
            if (this.IsLoaded())
            {
                throw new Exception($"Attempted to load asset {this.AssetName} which is already loaded.");
            }
            if (String.IsNullOrEmpty(newValue))
            {
                if (String.IsNullOrEmpty(this.Value))
                {
                    throw new Exception($"Attempted to load null asset");
                }
            }
        }
        public string CSVtoJson(string csv)
        {
            if (String.IsNullOrWhiteSpace(csv)) return null;
            string[] lines = csv.Split(new char[] { '\r', '\n' });
            int firstLine;
            string[] headers = null;
            for (firstLine = 0; firstLine < lines.Length; firstLine++)
            {
                if (!String.IsNullOrWhiteSpace(lines[firstLine]))
                {
                    headers = lines[firstLine].Split('\t');
                    for (int cellNum = 0; cellNum < headers.Length; cellNum++)
                    {
                        string cellText = headers[cellNum];
                        string escapechars = "([\\\"\\\\])";
                        Regex regex = new Regex(escapechars);
                        cellText = regex.Replace(cellText, "\\$1");
                        headers[cellNum] = cellText;
                    }
                    break;
                }
            }
            if (headers == null) { return null; }
            string jsonArray = "[ ";
            for (int i = firstLine + 1; i < lines.Length; i++)
            {
                if (String.IsNullOrWhiteSpace(lines[i])) continue;
                string[] line = lines[i].Split('\t');
                if (line.Length != headers.Length)
                {
                    throw new Exception($"CSV poorly formed.");
                }
                string lineString = "{";
                for (int j = 0; j < headers.Length; j++)
                {
                    string cellText = line[j];
                    string escapechars = "([\\\"\\\\])";
                    Regex regex = new Regex(escapechars);
                    cellText = regex.Replace(cellText, "\\$1");
                    lineString += $"\"0 string {headers[j]}\": \"{cellText}\"";
                    if (j != headers.Length - 1)
                    {
                        lineString += ", ";
                    }
                }
                lineString += "},";
                jsonArray += lineString;
            }
            jsonArray = jsonArray.TrimEnd(new char[] { ',', ' ', '\t' });
            jsonArray += " ]";
            return jsonArray;
        }
        public void PrintTree()
        {
            switch (this.Type)
            {
                case JsonValueKind.Object:
                    if (!String.IsNullOrEmpty(this.Name))
                    {
                        Console.WriteLine(this.Name + ":");
                    }
                    foreach (AssetObject subAsset in this.Properties.Values)
                    {
                        subAsset.PrintTree();
                    }
                    return;
                case JsonValueKind.Array:
                    if (!String.IsNullOrEmpty(this.Name))
                    {
                        Console.WriteLine(this.Name + ":");
                    }
                    foreach (AssetObject subAsset in this.Array)
                    {
                        subAsset.PrintTree();
                    }
                    return;
                default:
                    if (!String.IsNullOrEmpty(this.Name))
                    {
                        Console.Write(this.Name + " ");
                    }
                    Console.Write($"(Type {this.Type.ToString()}):");
                    Console.WriteLine(this.Value);
                    return;
            }
        }
        private static JsonDocument ParseJson(string json)
        {
            var options = new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            };
            try
            {
                JsonDocument jsonDocument = JsonDocument.Parse(json, options);
                return jsonDocument;
            }
            catch (JsonException e)
            {
                System.IO.File.WriteAllText("/Users/chjones/Downloads/APK/not-json.json", json);
                throw new JsonException(e.Message);
            }
        }
        public void ParseValue()
        {
            if (String.IsNullOrEmpty(this.Value))
            {
                if (this.Type != JsonValueKind.String)
                {
                    throw new JsonException($"Asset object {this.Name} (type {this.Type}) in file {this.FileName} has no associated value.");
                }
            }
            if (this.Properties.Count != 0)
            {
                throw new Exception($"Asset object {this.Name} in file {this.FileName} already has properties loaded.");
            }
            if (this.Array.Count != 0)
            {
                throw new Exception($"WARNING: Asset object {this.Name} in file {this.FileName} already has an array loaded.");
            }
            switch (this.Type)
            {
                case JsonValueKind.String:
                    if (this.AssetType == "TextAsset" && this.Name == "m_Script")
                    {
                        if (!this.Value.Contains('\t'))
                        {
                            this.Value = System.Text.Encoding.Unicode.GetString(Convert.FromBase64String(this.Value));
                        }
                        if (this.AssetName.EndsWith(".csv", true, null) && this.Value.Contains('\t'))
                        {
                            string csvArray = CSVtoJson(this.Value);
                            this.Value = csvArray;
                            this.Type = JsonValueKind.Array;
                        }
                        else if (this.AssetName.EndsWith(".txt", true, null))
                        {
                            JsonDocument jsonDocument = ParseJson(this.Value);
                            this.Type = jsonDocument.RootElement.ValueKind;
                            jsonDocument.Dispose();
                        }
                        this.ParseValue();
                    }
                    return;
                case JsonValueKind.Object:
                    JsonDocument json;
                    json = ParseJson(this.Value);
                    JsonElement jsonValue = json.RootElement;
                    if (jsonValue.ValueKind != JsonValueKind.Object)
                    {
                        try
                        {
                            throw new JsonException($"Value of object {this.Name} in file {this.FileName} parsed as type {jsonValue.ValueKind.ToString()} rather than JSON object.");
                        }
                        catch (JsonException)
                        {
                            json.Dispose();
                        }
                    }
                    foreach (JsonProperty jsonProperty in jsonValue.EnumerateObject())
                    {
                        AssetObject newObject = new AssetObject();
                        newObject.Parent = this;
                        newObject.FileName = this.FileName;
                        newObject.AssetName = this.AssetName;
                        newObject.AssetType = this.AssetType;
                        if (jsonProperty.Name.Split(' ', 3).Length == 3)
                        {
                            newObject.Name = jsonProperty.Name.Split(' ', 3)[2];
                        }
                        else
                        {
                            newObject.Name = jsonProperty.Name;
                        }
                        newObject.Type = jsonProperty.Value.ValueKind;
                        newObject.Value = jsonProperty.Value.ToString();
                        newObject.ParseValue();
                        // Use item[] instead of Add() to allow duplicate keys,
                        // with later ones overwriting previous, something that
                        // occurs sometimes in the level.txt TextAssets
                        this.Properties[newObject.Name] = newObject;
                    }
                    json.Dispose();
                    this.Value = null;
                    return;
                case JsonValueKind.Array:
                    JsonDocument jsonArrayDocument;
                    jsonArrayDocument = ParseJson(this.Value);
                    JsonElement jsonArrayValue = jsonArrayDocument.RootElement;
                    if (jsonArrayValue.ValueKind != JsonValueKind.Array)
                    {
                        try
                        {
                            throw new Exception();
                        }
                        catch (Exception)
                        {
                            jsonArrayDocument.Dispose();
                            throw new JsonException($"Value of object {this.Name} in file {this.FileName} parsed as type {jsonArrayValue.ValueKind.ToString()} rather than JSON array.");
                        }
                    }
                    int arrayCounter = 0;
                    foreach (JsonElement jsonElement in jsonArrayValue.EnumerateArray())
                    {
                        AssetObject newObject = new AssetObject();
                        newObject.Parent = this;
                        newObject.FileName = this.FileName;
                        newObject.AssetName = this.AssetName;
                        newObject.Type = jsonElement.ValueKind;
                        newObject.Value = jsonElement.ToString();
                        newObject.ParseValue();
                        this.Array.Insert(arrayCounter, newObject);
                        arrayCounter++;
                    }
                    jsonArrayDocument.Dispose();
                    this.Value = null;
                    return;
                default:
                    return;
            }
        }
    }
}
