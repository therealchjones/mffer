using System;
using System.Collections.Generic;
using AssetsTools;
using AssetsTools.Dynamic;

namespace Mffer {
	/// <summary>
	///  evaluate Unity files
	/// </summary>
	/// <remarks>
	/// Reverse engineering the data used by Marvel Future Fight frequently
	/// requires making assumptions about the data's structure. The <see
	/// cref="AssetFileTest"/> class includes methods and supporting members
	/// designed to test those assumptions.
	/// </remarks>
	static public class AssetFileTest {
		/// <summary>
		/// Evaluates the structure of an asset bundle file
		/// </summary>
		/// <remarks>
		/// <see cref="Test(string)"/> checks several assumptions made about an
		/// instance of an <see cref="AssetsTools.AssetBundleFile"/> and its
		/// contained structures, and throws an <see cref="System.Exception"/>
		/// if an assumption is not true in that instance.
		/// </remarks>
		/// <exception cref="NotImplementedException">Thrown if an assumption is
		/// false for this <see cref="AssetBundleFile"/>, for assumptions known
		/// to be incorrect in the general sense, but expected to be true for
		/// files processed by this program.</exception>
		/// <exception cref="Exception">Thrown when an assumption about the
		/// structure of <see cref="AssetBundleFile"/>s is found to be
		/// incorrect.</exception>
		/// <param name="fileName">The name of the asset bundle file to check</param>
		static public void Test( string fileName ) {
			List<string> KnownTypes = new List<string> { "TextAsset", "MonoScript", "MonoBehaviour", "AssetBundle" };
			Dictionary<string, int> TypeClasses = new Dictionary<string, int>();
			Dictionary<string, TypeTree> TypeTrees = new Dictionary<string, TypeTree>();
			Dictionary<string, List<int>> TypeIDs = new Dictionary<string, List<int>>();
			foreach ( string type in KnownTypes ) {
				TypeClasses.Add( type, 0 );
				TypeTrees.Add( type, null );
				TypeIDs.Add( type, new List<int>() );
			}
			List<DynamicAsset> assets = new List<DynamicAsset>();
			AssetsFile assetsFile = AssetBundleFile.LoadFromFile( fileName ).Files[0].ToAssetsFile();
			foreach ( AssetsFile.ObjectType obj in assetsFile.Objects ) {
				DynamicAsset asset = null;
				if ( assetsFile.Types[obj.TypeID].ClassID == (int)ClassIDType.MonoBehaviour ) {
					Func<UnityBinaryReader, DynamicAsset> deserialize = DynamicAsset.GenDeserializer( assetsFile.Types[obj.TypeID].TypeTree.Nodes );
					asset = deserialize( new UnityBinaryReader( obj.Data ) );
				} else {
					asset = obj.ToDynamicAsset();
				}
				if ( !KnownTypes.Contains( asset.TypeName ) ) {
					throw new NotImplementedException( $"Unable to load asset of type '{asset.TypeName}'" );
				}
				if ( !TypeIDs[asset.TypeName].Contains( obj.TypeID ) ) {
					TypeIDs[asset.TypeName].Add( obj.TypeID );
				}
				if ( TypeTrees[asset.TypeName] is null ) {
					TypeTrees[asset.TypeName] = assetsFile.Types[obj.TypeID].TypeTree;
				}
				if ( asset.TypeName == "TextAsset" && assetsFile.Types[obj.TypeID].ClassID != (int)ClassIDType.TextAsset ) {
					throw new Exception( $"TextAsset recognized as {(ClassIDType)assetsFile.Types[obj.TypeID].ClassID}" );
				}
				if ( asset.TypeName == "MonoScript" && assetsFile.Types[obj.TypeID].ClassID != (int)ClassIDType.MonoScript ) {
					throw new Exception( $"MonoScript recognized as {(ClassIDType)assetsFile.Types[obj.TypeID].ClassID}" );
				}
				if ( asset.TypeName == "AssetBundle" && assetsFile.Types[obj.TypeID].ClassID != (int)ClassIDType.AssetBundle ) {
					throw new Exception( $"AssetBundle recognized as {(ClassIDType)assetsFile.Types[obj.TypeID].ClassID}" );
				}
				if ( asset.TypeName == "MonoBehaviour" && assetsFile.Types[obj.TypeID].ClassID != (int)ClassIDType.MonoBehaviour ) {
					throw new Exception( $"MonoBehaviour recognized as {(ClassIDType)assetsFile.Types[obj.TypeID].ClassID}" );
				}
				if ( TypeClasses[asset.TypeName] == 0 ) {
					TypeClasses[asset.TypeName] = assetsFile.Types[obj.TypeID].ClassID;
				} else {
					if ( TypeClasses[asset.TypeName] != assetsFile.Types[obj.TypeID].ClassID ) {
						throw new Exception( $"Multiple classes use type name {asset.TypeName}" );
					}
				}
				assets.Add( asset );
			}
			foreach ( KeyValuePair<string, List<int>> entry in TypeIDs ) {
				if ( entry.Key != "MonoBehaviour" && entry.Value.Count > 1 ) {
					throw new Exception( $"More than one type of {entry.Key} found" );
				}
			}
			foreach ( KeyValuePair<string, TypeTree> entry in TypeTrees ) {
				if ( entry.Key != "MonoBehaviour" ) {
					// These are the (standard) structures of the uniform
					// asset types that we test. The program does not in
					// fact rely upon the exact node numbers, but they
					// appear to hold and are easy tests to check
					if ( entry.Value is null ) {
						if ( TypeIDs[entry.Key].Count > 0 ) {
							throw new Exception( $"No type tree description found for type {entry.Key}" );
						} else {
							continue;
						}
					}
					TypeTree tree = entry.Value;
					Assert( tree.Nodes[0].Name == "Base" );
					Assert( tree.Nodes[0].Type == entry.Key );
					Assert( tree.Nodes[1].Level == 1 );
					Assert( tree.Nodes[1].Name == "m_Name" );
					Assert( tree.Nodes[1].Type == "string" );
					switch ( entry.Key ) {
						case "TextAsset":
							Assert( tree.Nodes[5].Level == 1 );
							Assert( tree.Nodes[5].Name == "m_Script" );
							Assert( tree.Nodes[5].Type == "string" );
							break;
						case "MonoScript":
							Assert( tree.Nodes[23].Level == 1 );
							Assert( tree.Nodes[23].Name == "m_ClassName" );
							Assert( tree.Nodes[23].Type == "string" );
							break;
						case "AssetBundle":
							Assert( tree.Nodes[11].Level == 1 );
							Assert( tree.Nodes[11].Name == "m_Container" );
							Assert( tree.Nodes[11].Type == "map" );
							Assert( tree.Nodes[15].Level == 4 );
							Assert( tree.Nodes[15].Name == "first" );
							Assert( tree.Nodes[15].Type == "string" );
							Assert( tree.Nodes[19].Level == 4 );
							Assert( tree.Nodes[19].Name == "second" );
							Assert( tree.Nodes[19].Type == "AssetInfo" );
							Assert( tree.Nodes[24].Level == 6 );
							Assert( tree.Nodes[24].Name == "m_PathID" );
							Assert( tree.Nodes[24].Type == "SInt64" );
							Assert( tree.Nodes[32].Level == 1 );
							Assert( tree.Nodes[32].Name == "m_AssetBundleName" );
							Assert( tree.Nodes[32].Type == "string" );
							break;
					}
				}
			}
		}
		private static void Assert( bool assertion, string message = null ) {
			if ( !assertion ) {
				if ( !String.IsNullOrEmpty( message ) ) {
					throw new Exception( message );
				}
				throw new Exception( "Assumption is not true" );
			}
		}
	}
}
