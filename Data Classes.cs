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
        public DataDirectory( string pathName ) {
            dirs = new List<DirectoryInfo>();
            versionDirs = new Dictionary< string, List<DirectoryInfo> >();
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
            return dirname.Substring(firstDigit);
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
        public Dictionary<string, AssetFile> AssetFiles { get; set; }
        public AssetBundle()
        {
            AssetFiles = new Dictionary<string, AssetFile>();
        }
        public void WriteJson(StreamWriter file, int tabs = 0)
        {
            for ( int i = 0; i < tabs; i++ ) {
                file.Write("\t");
            }
            file.WriteLine("\"Assets\" : {");
            List<string> Keys = AssetFiles.Keys.ToList<string>();
            Keys.Sort();
            int counter = 0;
            foreach ( string key in Keys ) {          
                AssetFiles[key].WriteJson(file,tabs+1);
                if ( counter < Keys.Count() - 1 ) {
                    file.Write(",");
                }
                file.WriteLine();
                counter++;
            }
            for ( int i = 0; i < tabs; i++ ) {
                file.Write("\t");
            }
            file.Write("}");          
        }
        public void LoadFromVersionDirectory( List<DirectoryInfo> dirs ) {
            Dictionary<string, string> manifest = new Dictionary<string, string>();
            HashSet<string> manifestFiles = new HashSet<string>();
            HashSet<string> scriptFiles = new HashSet<string>();
            HashSet<string> behaviorFiles = new HashSet<string>();
            HashSet<string> textFiles = new HashSet<string>();
            HashSet<string> jsonFiles = new HashSet<string>();
            HashSet<string> checkFiles = new HashSet<string>();
            foreach ( DirectoryInfo directory in dirs ) {
                string assetDir = directory.FullName + "/assets";
                manifestFiles.UnionWith( Directory.GetFiles( assetDir, "*-AssetBundle.json") );
                scriptFiles.UnionWith( Directory.GetFiles( assetDir, "*-MonoScript.json" ) );
                behaviorFiles.UnionWith( Directory.GetFiles( assetDir, "*-MonoBehaviour.json") );
                textFiles.UnionWith( Directory.GetFiles( assetDir, "*-TextAsset.json") );
                jsonFiles.UnionWith( Directory.GetFiles( assetDir ) );
            }
            checkFiles.UnionWith( manifestFiles );
            checkFiles.UnionWith( scriptFiles );
            checkFiles.UnionWith( behaviorFiles );
            checkFiles.UnionWith( textFiles );
            foreach ( string file in jsonFiles ) {
                if ( ! checkFiles.Contains(file) ) {
                    throw new Exception($"Unable to determine type of file {file}");
                }
            }
            if (manifestFiles.Count == 0)
            {
                throw new Exception("Found no manifest files.");
            }
            // should extend the manifest to include the asset bundle ID rather than just
            // the pathID, in case there are overlaps
            foreach ( string manifestFile in manifestFiles ) {
                AssetFile manifestAsset = new AssetFile();
                manifestAsset.LoadFromFile(manifestFile);
                foreach (AssetObject entry in manifestAsset.Properties["m_Container"].Properties["Array"].Array)
                {
                    string pathID = entry.Properties["data"].Properties["second"].Properties["asset"].Properties["m_PathID"].String;
                    if (pathID.StartsWith('-'))
                    {
                        UInt64 newID = System.Convert.ToUInt64(pathID.Substring(1));
                        newID = UInt64.MaxValue - newID + 1;
                        pathID = newID.ToString();
                    }
                    manifest.Add(pathID, entry.Properties["data"].Properties["first"].String);
                }
            }
            foreach ( string scriptFile in scriptFiles ) {
                AssetFile scriptAsset = new AssetFile();
                scriptAsset.LoadFromFile( scriptFile );
                string pathID = Regex.Replace(scriptFile, "^.*-([0-9]*)-[^-]*$", "$1");
                string scriptClass = scriptAsset.Name;
                manifest.Add(pathID, scriptClass);
            }
            foreach ( string behaviorFile in behaviorFiles ) {
                AssetFile behaviorAsset = new AssetFile();
                behaviorAsset.LoadFromFile( behaviorFile );
                string scriptID = behaviorAsset.Properties["m_Script"].Properties["m_PathID"].String;
                if (scriptID.StartsWith('-')) {
                    UInt64 newID = System.Convert.ToUInt64(scriptID.Substring(1));
                    newID = UInt64.MaxValue - newID + 1;
                    scriptID = newID.ToString();
                }
                if ( ! manifest.ContainsKey(scriptID) ) {
                    throw new Exception($"Script pathID {scriptID} (from file {behaviorFile}) not found in manifest");
                }
                behaviorAsset.AssetName=manifest[scriptID];
                behaviorAsset.Name=behaviorAsset.AssetName;
                if (behaviorAsset.AssetName == null) {
                        throw new Exception($"Asset file {behaviorFile} has no asset name.");
                }               
                if ( AssetFiles.ContainsKey("asset.AssetName") ) {
                    throw new Exception($"Attempted to add asset {behaviorAsset.AssetName} (from file {behaviorFile}) which already exists.");
                }
                AssetFiles.Add(behaviorAsset.AssetName, behaviorAsset);
            }
            foreach (string jsonFile in textFiles ) {
                AssetFile asset = new AssetFile();
                string pathID = Regex.Replace(jsonFile, "^.*-([0-9]*)-[^-]*$", "$1");
                if ( manifest.ContainsKey(pathID) ) {
                    asset.AssetName = manifest[pathID];
                } else {
                    throw new Exception($"pathID {pathID} (file {jsonFile}) not in manifest");
                }
                if (asset.AssetName == null) {
                        throw new Exception($"Asset file {jsonFile} has no asset name.");
                }
                asset.LoadFromFile(jsonFile);
                if ( AssetFiles.ContainsKey("asset.AssetName") ) {
                    throw new Exception($"Attempted to add asset {asset.AssetName} (from file {jsonFile}) which already exists.");
                }
                AssetFiles.Add(asset.AssetName, asset);
            }
        }
    }
    public class AssetFile : AssetObject {
        public string AssetName { get; set; }
        public string AssetType { get; set; }
        public void LoadFromFile(string filename) {
            string jsonText = File.ReadAllText(filename);
            JsonDocument json = GetJson(jsonText);
            JsonElement jsonRoot = json.RootElement;
            JsonValueKind jsonType = jsonRoot.ValueKind;
            JsonElement Value = new JsonElement();
            if (jsonType != JsonValueKind.Object) {
                throw new JsonException($"{filename} is not a valid asset file");
            }
            int propertyCount = 0;
            foreach (JsonProperty jsonProperty in jsonRoot.EnumerateObject()) {
                string[] nameArray = jsonProperty.Name.Split(' ', 3);
                if ( nameArray.Length != 3 ) {
                    throw new JsonException($"{filename} is not a valid asset file");
                }
                Name = jsonProperty.Name.Split(' ', 3)[2]; // Should be "Base"?
                AssetType = jsonProperty.Name.Split(' ', 3)[1];
                Value = jsonProperty.Value.Clone();
                Type = jsonProperty.Value.ValueKind;
                propertyCount++;
            }
            json.Dispose();
            if (propertyCount == 0) {
                throw new JsonException($"{filename} does not contain any JSON members beyond root.");
            } else if (propertyCount != 1) {
                throw new JsonException($"{filename} contains more than one top level member.");
            } else if (Type != JsonValueKind.Object) {
                throw new JsonException($"{filename} top level member is type {Type.ToString()} rather than JSON object.");
            } else if (Name != "Base") {
                throw new JsonException($"{filename} top level member is not Base");
            }
            switch (AssetType) {
                case "AssetBundle":
                    foreach (JsonProperty property in Value.EnumerateObject()) {
                        string[] nameArray = property.Name.Split(' ', 3);
                        if ( nameArray.Length != 3 ) {
                            throw new JsonException($"{filename} is not a valid asset file");
                        }
                        if (nameArray[2] == "m_AssetBundleName") {
                            Name = property.Value.GetString();
                            AssetName = Name;
                        }
                        AssetObject newAsset = new AssetObject();
                        newAsset.Name = nameArray[2];
                        newAsset.ParseJson(property.Value);
                        if ( Properties.ContainsKey(newAsset.Name) ) {
                            throw new JsonException($"Asset {Name} already contains a property named {newAsset.Name}");
                        }
                        Properties.Add(newAsset.Name, newAsset);
                    }
                    break;
                case "MonoScript":
                    foreach (JsonProperty property in Value.EnumerateObject()) {
                        string[] nameArray = property.Name.Split(' ', 3);
                        if ( nameArray.Length != 3 ) {
                            throw new JsonException($"{filename} is not a valid asset file");
                        }
                        if ( nameArray[2] == "m_ClassName" ) {
                            Name = property.Value.GetString();
                        }
                    }
                    AssetName = Name;
                    return;
                case "MonoBehaviour":
                    foreach (JsonProperty property in Value.EnumerateObject()) {
                        string[] nameArray = property.Name.Split(' ', 3);
                        if ( nameArray.Length != 3 ) {
                            throw new JsonException($"{filename} is not a valid asset file");
                        }
                        AssetObject newAsset = new AssetObject();
                        newAsset.Name = nameArray[2];
                        newAsset.ParseJson(property.Value);
                        if ( Properties.ContainsKey(newAsset.Name) ) {
                            throw new JsonException($"Asset already contains a property named {newAsset.Name}");
                        }
                        Properties.Add(newAsset.Name, newAsset);
                    }
                    break;                    
                case "TextAsset":
                    foreach (JsonProperty property in Value.EnumerateObject() ) {
                        AssetObject newAsset = new AssetObject();
                        // this repeated way of checking the name should be a method
                        string[] nameArray = property.Name.Split(' ',3);
                        if ( nameArray.Length != 3 ) {
                            throw new JsonException($"{filename} is not a valid asset file");
                        }
                        newAsset.Name = nameArray[2];
                        JsonElement value = property.Value;
                        if ( newAsset.Name == "m_Script" ) {
                            string valueString = property.Value.GetString();
                            if ( ! valueString.Contains('\t') ) { // this is the way it's checked in the program
                                valueString = System.Text.Encoding.Unicode.GetString(Convert.FromBase64String(valueString));
                            }
                            if (AssetName.EndsWith(".csv", true, null) ) {
                                valueString = CSVtoJson(valueString);
                            }
                            JsonDocument jsonDocument = GetJson(valueString);
                            value = jsonDocument.RootElement.Clone();
                            jsonDocument.Dispose();
                        }
                        newAsset.ParseJson(value);
                        if ( Properties.ContainsKey(newAsset.Name) ) {
                            throw new JsonException($"Asset already contains a property named {newAsset.Name}");
                        }
                        Properties.Add(newAsset.Name, newAsset);
                    }
                    break;
            }
        }
        string CSVtoJson(string csv)
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
        public override void WriteJson(StreamWriter file, int tabs = 0) {
            for ( int i = 0; i < tabs; i++ ) {
                file.Write("\t");
            }
            file.WriteLine($"\"{AssetName}\" : " + "{");
            for ( int i = 0; i < tabs+1; i++ ) {
                file.Write("\t");
            }
            file.Write($"\"AssetType\" : \"{AssetType}\"");
            if ( Properties.Count > 0 ) {
                file.WriteLine(",");
                base.WriteJson( file, tabs+1 );
            }
            file.WriteLine();
            for ( int i = 0; i < tabs; i++ ) {
                file.Write("\t");
            }
            file.Write("}");
        }
    }
    public class AssetObject {
        public string Name { get; set; }
        public JsonValueKind Type { get; set; }
        public Dictionary<string, AssetObject> Properties { get; set; }
        public string String { get; set; }
        public List<AssetObject> Array { get; set; }
        public AssetObject() {
            Type = new JsonValueKind();
            Properties = new Dictionary<string, AssetObject>();
            Array = new List<AssetObject>();
            Name="";
            String="";
        }
        protected JsonDocument GetJson(string json)
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
        public void ParseJson( JsonElement Value ) {
            if (Properties.Count != 0)
            {
                throw new Exception($"Asset object {Name} already has properties loaded.");
            }
            if (Array.Count != 0)
            {
                throw new Exception($"Asset object {Name} already has an array loaded.");
            }
            if ( String.Length != 0 ) {
                throw new Exception($"Asset object {Name} already has a string loaded.");
            }
            Type = Value.ValueKind;
            switch (Type) {
                case JsonValueKind.Object:
                    foreach (JsonProperty jsonProperty in Value.EnumerateObject())
                    {
                        AssetObject newObject = new AssetObject();
                        string[] nameArray = jsonProperty.Name.Split(' ', 3);
                        if (nameArray.Length == 3) {
                            newObject.Name = nameArray[2];
                        } else {
                            newObject.Name = jsonProperty.Name;
                        }
                        newObject.Type = jsonProperty.Value.ValueKind;
                        newObject.ParseJson( jsonProperty.Value );
                        // Use item[] instead of Add() to allow duplicate keys,
                        // with later ones overwriting previous, something that
                        // occurs sometimes in the level.txt TextAssets
                        Properties[newObject.Name] = newObject;
                    }
                    return;
                case JsonValueKind.Array:
                    int arrayCounter = 0;
                    foreach (JsonElement jsonElement in Value.EnumerateArray())
                    {
                        AssetObject newObject = new AssetObject();
                        newObject.Type = jsonElement.ValueKind;
                        newObject.ParseJson(jsonElement);
                        Array.Insert(arrayCounter, newObject);
                        arrayCounter++;
                    }
                    return;
                case JsonValueKind.Undefined:
                    throw new Exception($"Unable to parse asset object {Name}");
                default:
                    String = Value.ToString();
                    Type = JsonValueKind.String;
                    return;
            }
        }
        public virtual void WriteJson(StreamWriter file, int tabs = 0) {
            int counter=0;
            switch (Type) {
                case JsonValueKind.Object:
                    List<string> keys = Properties.Keys.ToList();
                    keys.Sort();
                    foreach (string key in keys) {
                        AssetObject value = Properties[key];
                        for ( int i = 0; i < tabs; i++ ) {
                            file.Write("\t");
                        }         
                        file.Write("\"" + key + "\" : ");
                        if ( value.Type == JsonValueKind.Object ) {
                            file.WriteLine("{");
                            value.WriteJson(file, tabs+1);
                            file.WriteLine();
                            for ( int i = 0; i < tabs; i++ ) {
                                file.Write("\t");
                            }
                            file.Write("}");                             
                        } else if ( value.Type == JsonValueKind.Array ) {
                            file.WriteLine("[");
                            value.WriteJson(file, tabs+1);
                            file.WriteLine();
                            for ( int i = 0; i < tabs; i++ ) {
                                file.Write("\t");
                            }
                            file.Write("]");   
                        } else {
                            value.WriteJson(file);
                        }
                        if (counter < keys.Count - 1) {
                            file.WriteLine(",");
                        }
                        counter++;
                    }
                    break;
                case JsonValueKind.Array:
                    // Should all be of the same type; do I need to check the Type of each item?
                    foreach (AssetObject item in Array) {
                        for ( int i = 0; i < tabs; i++ ) {
                            file.Write("\t");
                        }         
                        if ( item.Type == JsonValueKind.Object ) {
                            file.WriteLine("{");
                            item.WriteJson(file, tabs+1);
                            file.WriteLine();
                            for ( int i = 0; i < tabs; i++ ) {
                                file.Write("\t");
                            }
                            file.Write("}");                             
                        } else if ( item.Type == JsonValueKind.Array ) {
                            file.WriteLine("[");
                            item.WriteJson(file, tabs+1);
                            file.WriteLine();
                            for ( int i = 0; i < tabs; i++ ) {
                                file.Write("\t");
                            }
                            file.Write("]");   
                        } else {
                            item.WriteJson(file);
                        }
                        if (counter < Array.Count - 1) {
                            file.WriteLine(",");
                        }
                        counter++;
                    }
                    break;
                case JsonValueKind.Undefined:
                    throw new Exception($"Unable to identify appropriate JSON conversion for asset object {Name}.");
                default:
                    file.Write($"\"{String}\"");
                    break;
            }
        }
    }
}
