using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace MFFDataApp
{
    // still much in AssetBundle and AssetObject class referring to named directories
    //
    // DataDirectory need not be a proper filesystem directory tree, but rather contains
    // a list of filesystem directories (dirs)
    //
    // dirs are directories added from constructors or other external calls
    // dirs may be version directories or parents of version directories.
    // version directories have names that include a substring starting with a digit and
    // have a subdirectory named "assets"
    // When adding a directory to the versionDirs list, the version name is taken to be the longest
    // substring of the directory name that starts with a digit.
    public class DataDirectory {
        List<DirectoryInfo> dirs { get; set; }
        Dictionary< string, List<DirectoryInfo> > versionDirs { get; set; }
        DataDirectory() {
            dirs = new List<DirectoryInfo>();
            versionDirs = new Dictionary< string, List<DirectoryInfo> >();
        }
        public DataDirectory( string pathName ) : this() {
            Add( pathName );
        }
        public void Add( string pathName ) {
            if ( ! Directory.Exists( pathName ) ) {
                throw new DirectoryNotFoundException($"Unable to access directory {pathName}");
            } else {
                DirectoryInfo dir = new DirectoryInfo(pathName);
                if ( ! IsIncluded( dir, dirs ) ) {
                    dirs.Add( dir );
                    if ( IsVersionDirectory(dir) ) {
                        AddVersionDirectory(dir);
                    } else {
                        var newVersionDirs = new List<DirectoryInfo>();
                        foreach ( DirectoryInfo subdir in dir.GetDirectories() ){
                            if ( IsVersionDirectory(subdir) ) {
                                newVersionDirs.Add(subdir);
                            }
                        }
                        if ( newVersionDirs.Count() == 0 ) {
                            ThrowBadDataDir();
                        } else {
                            foreach ( DirectoryInfo versionDir in newVersionDirs ) {
                                AddVersionDirectory( versionDir );
                            }
                        }
                    }
                }
            }
        }
        void AddVersionDirectory( DirectoryInfo dir ) {
            if ( IsIncluded( dir, versionDirs) ) return;
            string versionString = GetVersionName(dir.Name);
            if (String.IsNullOrEmpty(versionString)) {
                ThrowBadDataDir();
            } else {
                if ( ! versionDirs.ContainsKey(versionString)) {
                    versionDirs.Add( versionString, new List<DirectoryInfo>() );
                }
                versionDirs[versionString].Add( dir );
            }
        }
        string GetVersionName( string dirname ) {
            char[] digits = { '0', '1', '2','3','4','5','6','7','8','9'};
            int firstDigit = dirname.IndexOfAny(digits);
            if ( firstDigit == -1 ) return null;
            string versionName = dirname.Substring(firstDigit);
            return versionName;
        }
        bool IsIncluded( DirectoryInfo directory, List<DirectoryInfo> dirList ) {
            foreach ( DirectoryInfo dir in dirList ) {
                if ( dir.FullName == directory.FullName ) return true;
            }
            return false;
        }
        bool IsIncluded( DirectoryInfo directory, Dictionary< string, List<DirectoryInfo> > versionList ) {
            foreach ( List<DirectoryInfo> dirlist in versionList.Values ) {
                if ( IsIncluded( directory, dirlist ) ) return true;
            }
            return false;
        }
        bool IsVersionDirectory(DirectoryInfo directory) {
            string versionName = GetVersionName( directory.Name );
            if ( versionName == null ) {
                return false;
            }
            DirectoryInfo[] assetDirs = directory.GetDirectories("assets");
            if ( assetDirs.Length == 1 ) { 
                return true;
            } else {
                return false;
            }
        }
        void ThrowBadDataDir() {
            throw new Exception($"Unable to define structure of data directory.");
        }
        public List<string> GetVersionNames() {
            return versionDirs.Keys.ToList();
        }
        public AssetBundle GetAssets( string versionName ) {
            AssetBundle assets = new AssetBundle();
            assets.LoadFromVersionDirectory( versionDirs[versionName] );
            return assets;
        }
    }
    public class AssetBundle
    {
        public Dictionary<string, AssetObject> AssetFiles { get; set; }
        public Dictionary<string, string> manifest { get; set; }
        public AssetBundle()
        {
            AssetFiles = new Dictionary<string, AssetObject>();
            manifest = new Dictionary<string, string>();
        }
        public void WriteJson(StreamWriter file)
        {
            file.WriteLine("{");
            file.WriteLine("\t\"manifest\" : [");
            List<string> keys = manifest.Keys.ToList();
            keys.Sort();
            int counter = 0;
            foreach ( string key in keys ) {
                file.Write("\t\t\"" + key + "\" : ");
                file.Write("\"" + manifest[key] + "\"");
                counter++;
                if ( counter < keys.Count - 1 ) {
                    file.Write(",");
                }
                file.WriteLine();
            }
            file.WriteLine("\t],");
            file.WriteLine("\t\"AssetFiles\" : [");
            keys = AssetFiles.Keys.ToList();
            keys.Sort();
            counter = 0;
            foreach (string key in keys) {
                file.Write("\t\t\"" + key + "\" : ");
                AssetFiles[key].WriteJson(file);
                counter++;
                if ( counter < keys.Count - 1 ) {
                        file.Write(", ");
                }
                file.WriteLine();
            }
            file.WriteLine("\t]");
            file.WriteLine("}");
        }
        public void LoadFromVersionDirectory( List<DirectoryInfo> dirs ) {
            HashSet<string> manifestFiles = new HashSet<string>();
            HashSet<string> jsonFiles = new HashSet<string>();
            foreach ( DirectoryInfo directory in dirs ) {
                // should ensure that these AddRanges don't add duplicates
                // AddRange won't work if null, so need to check that rather than passing directly
                manifestFiles.UnionWith( Directory.GetFiles( directory.FullName + "/assets", "*-AssetBundle.json") );
                jsonFiles.UnionWith( Directory.GetFiles( directory.FullName + "/assets" ) );
            }
            if (manifestFiles.Count == 0)
            {
                throw new Exception("Found no manifest files.");
            }
            foreach ( string manifestFile in manifestFiles ) {
                AssetObject manifestAsset = new AssetObject();
                manifestAsset.LoadFromFile(manifestFile);
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
            }
            foreach (string jsonFile in jsonFiles)
            {
                AssetObject asset = new AssetObject();
                string pathID = Regex.Replace(jsonFile, "^.*-([0-9]*)-[^-]*$", "$1");
                if ( manifest.ContainsKey(pathID) ) {
                    asset.AssetName = manifest[pathID];
                }
                asset.LoadFromFile(jsonFile);
                if (asset != null) AssetFiles.Add(asset.AssetName, asset);
            }
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
        }
        public void WriteJson(StreamWriter file) {
            file.WriteLine("{");
            file.WriteLine("\t\"Name\" : \"" + Name + "\",");
            file.WriteLine("\t\"AssetName\" : \"" + AssetName + "\",");
            file.WriteLine("\t\"AssetType\" : \"" + AssetType + "\",");
            if ( ! String.IsNullOrEmpty(ScriptClass) ) {
                file.WriteLine("\t\"ScriptClass\" : \"" + ScriptClass + "\",");
            }
            switch (Type)
            {
                case JsonValueKind.Object:
                    file.WriteLine("\t\"Properties\" : [");
                    int counter = 0;
                    List<string> keys = Properties.Keys.ToList();
                    keys.Sort();
                    foreach (string key in keys)
                    {
                        file.Write("\t\t\"" + key + "\" : ");
                        Properties[key].WriteJson(file);
                        counter++;
                        if (counter < keys.Count - 1) {
                            file.Write(",");
                        }
                        file.WriteLine();
                    }
                    file.WriteLine("\t]");
                    break;
                case JsonValueKind.Array:
                    file.WriteLine("\t\"Array\" : [");
                    for (counter = 0; counter < Array.Count; counter++)
                    {
                        Array[counter].WriteJson(file);
                        if (counter < Array.Count - 1)
                        {
                            file.Write(",");
                        }
                        file.WriteLine();
                    }
                    file.WriteLine("\t]");
                    break;
                case JsonValueKind.Undefined:
                    throw new Exception($"Unable to identify appropriate JSON conversion for item {Name} in asset {AssetName}.");
                default:
                    file.WriteLine("\t\"Value\" : \"" + Value + "\"");
                    break;
            }
            file.Write("}");
        }
        public void LoadFromFile(string filename)
        {
            string jsonText = File.ReadAllText(filename);
            JsonDocument json = ParseJson(jsonText);
            JsonElement jsonRoot = json.RootElement;
            JsonValueKind jsonType = jsonRoot.ValueKind;
            if (jsonType != JsonValueKind.Object)
            {
                throw new JsonException($"{filename} is not a valid asset file");
            }
            FileName = filename;
            int propertyCount = 0;
            foreach (JsonProperty jsonProperty in jsonRoot.EnumerateObject())
            {
                propertyCount++;
                Name = jsonProperty.Name.Split(' ', 3)[2];
                AssetType = jsonProperty.Name.Split(' ', 3)[1];
                Value = jsonProperty.Value.ToString();
                Type = jsonProperty.Value.ValueKind;
            }
            if (propertyCount == 0)
            {
                throw new JsonException($"{filename} does not contain any JSON members beyond root.");
            }
            else if (propertyCount != 1)
            {
                throw new JsonException($"{filename} contains more than one top level member.");
            }
            else if (Type != JsonValueKind.Object)
            {
                throw new JsonException($"{filename} top level member is type {Type.ToString()} rather than JSON object.");
            }
            JsonDocument fileAsset = ParseJson(Value);
            switch (AssetType) {
                case "AssetBundle":
                    foreach (JsonProperty property in fileAsset.RootElement.EnumerateObject())
                    {
                        if (property.Name.Split(' ',3)[2] == "m_AssetBundleName")
                        {
                            Name = property.Value.ToString();
                        }
                    }
                    AssetName = Name;
                    break;
                case "MonoScript":
                    foreach (JsonProperty property in fileAsset.RootElement.EnumerateObject()) {
                        if (property.Name.Split(' ',3)[2] == "m_ClassName") {
                            Name = property.Value.ToString();
                        }
                    }
                    AssetName = Name;
                    break;
                case "MonoBehaviour":
                    break;
                case "TextAsset":
                default:
                    break;
            }
            fileAsset.Dispose();
            ParseValue();
            json.Dispose();
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
                // System.IO.File.WriteAllText("/Users/chjones/Downloads/APK/not-json.json", json);
                throw new JsonException(e.Message);
            }
        }
        public void ParseValue()
        {
            if (String.IsNullOrEmpty(Value))
            {
                if (Type != JsonValueKind.String)
                {
                    throw new JsonException($"Asset object {Name} (type {Type}) in file {FileName} has no associated value.");
                }
            }
            if (Properties.Count != 0)
            {
                throw new Exception($"Asset object {Name} in file {FileName} already has properties loaded.");
            }
            if (Array.Count != 0)
            {
                throw new Exception($"Asset object {Name} in file {FileName} already has an array loaded.");
            }
            switch (Type)
            {
                case JsonValueKind.String:
                    if (AssetType == "TextAsset" && Name == "m_Script")
                    {
                        if ( ! Value.Contains('\t'))
                        {
                            Value = System.Text.Encoding.Unicode.GetString(Convert.FromBase64String(Value));
                        }
                        if (AssetName.EndsWith(".csv", true, null) )
                        {
                            string csvArray = CSVtoJson(Value);
                            Value = csvArray;
                            Type = JsonValueKind.Array;
                            ParseValue();
                        }
                        else if (AssetName.EndsWith(".txt", true, null))
                        {
                            JsonDocument jsonDocument = ParseJson(Value);
                            Type = jsonDocument.RootElement.ValueKind;
                            jsonDocument.Dispose();
                        }
                        else {
                            return;
                        }
                    }
                    return;
                case JsonValueKind.Object:
                    JsonDocument json;
                    json = ParseJson(Value);
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
