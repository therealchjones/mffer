using System;
using System.Collections;
using System.Collections.Generic;
using AssetsTools;
using AssetsTools.Dynamic;

namespace Mffer {
	/// <summary>
	/// Extensions to convert other objects to <see cref="GameObject"/>s
	/// </summary>
	/// <remarks>
	/// To standardize access to data in a wide variety of object types, most
	/// <see cref="Game"/> data is converted to <see cref="GameObject"/>
	/// objects. The <see cref="ToGameObjectExtensions"/> class includes the
	/// polymorphic extension method <see
	/// cref="ToGameObjectExtensions.ToGameObject(object,TypeTree.Node[])"/> and
	/// supporting members.
	/// </remarks>
	public static class ToGameObjectExtensions {
		/// <summary>
		/// Convert an <see cref="Object"/> to a <see cref="GameObject"/>
		/// </summary>
		/// <remarks>
		/// <para>Where possible, <see
		/// cref="ToGameObject(object,TypeTree.Node[])"/> creates a new <see
		/// cref="GameObject"/> containing the data from a given <see
		/// cref="Object"/>, thus limiting the need for other methods to deal
		/// with arbitrary object formats. (If <paramref name="value"/> is
		/// already a <see cref="GameObject"/>, it is returned
		/// unchanged.)</para>
		/// <para>Objects from the <see cref="AssetsTools.Dynamic"/> namespace
		/// (<see cref="DynamicAsset"/>s, <see cref="DynamicAssetArray"/>s, and
		/// <see cref="DynamicAssetDictionary{TKey,TValue}"/>s) may contain
		/// arbitrary hierarchical structures defined by arrays of <see
		/// cref="TypeTree.Node"/>s. <see
		/// cref="ToGameObject(object,TypeTree.Node[])"/> requires this
		/// parameter to be included for these structures.</para>
		/// </remarks>
		/// <param name="value">The <see cref="Object"/> to convert</param>
		/// <param name="nodes">The hierarchical type array describing the
		/// structure of the object. This is required for <see
		/// cref="AssetsTools.Dynamic"/> objects.</param>
		/// <returns><see cref="GameObject"/> containing the data from <paramref
		/// name="value"/></returns>
		public static GameObject ToGameObject( this object value, TypeTree.Node[] nodes = null ) {
			switch ( value ) {
				case GameObject g:
					return g;
				case DynamicAssetDictionary<object, object>:
				case DynamicAssetArray:
				case DynamicAsset:
					if ( nodes is null ) {
						throw new ArgumentNullException( "nodes" );
					}
					break;
			}
			GameObject gameObject = new GameObject();
			switch ( value ) {
				case string s: // strings can be more generally classified as arrays of chars, so this type comes first
					gameObject.Value = s;
					break;
				case DynamicAsset asset:
					List<TypeTree.Node[]> memberNodes = GetChildNodes( nodes );
					if ( memberNodes.Count == 0 ) {
						throw new NotImplementedException( "I don't know what's going on here." );
					}
					SortedDictionary<string, GameObject> assetDictionary = new SortedDictionary<string, GameObject>();
					foreach ( TypeTree.Node[] nodeTree in memberNodes ) {
						string nodeKey = nodeTree[0].Name;
						object nodeValue = null;
						if ( !( (DynamicAsset)asset ).TryGetMember( new AssetsToolsMemberBinder( nodeKey, false ), out nodeValue ) ) {
							throw new Exception( "Unable to load into GameObject" );
						}
						assetDictionary.Add( nodeKey, nodeValue.ToGameObject( nodeTree ) );
					}
					return assetDictionary.ToGameObject();
				case Dictionary<string, GameObject> gameObjectDictionary:
					gameObject.Value = gameObjectDictionary;
					break;
				case List<GameObject> gameObjectList:
					gameObject.Value = gameObjectList;
					break;
				case DynamicAssetArray assetArray:
					List<GameObject> assetList = new List<GameObject>();
					int i = 0;
					while ( true ) {
						try {
							assetList.Add( assetArray[i].ToGameObject( GetElementNodes( nodes ) ) );
						} catch ( IndexOutOfRangeException ) {
							break;
						}
					}
					gameObject.Value = assetList;
					break;
				case IDictionary objectDictionary:
					foreach ( var key in objectDictionary.Keys ) {
						if ( key is string || key is IFormattable ) {
							break;
						}
						throw new InvalidCastException( $"Unable to convert {key.GetType().Name} to string." );
					}
					Dictionary<string, GameObject> newDictionary = new Dictionary<string, GameObject>();
					foreach ( var key in objectDictionary.Keys ) {
						TypeTree.Node[] valueNodes = null;
						if ( nodes is not null ) {
							valueNodes = GetChildNodes( GetElementNodes( nodes ) )[0];
						}
						newDictionary.Add( key.ToString(), objectDictionary[key].ToGameObject( valueNodes ) );
					}
					gameObject.Value = newDictionary;
					break;
				case IEnumerable objectList: // Note that (generic) IEnumerable<object> won't match int[]
					List<GameObject> newList = new List<GameObject>();
					foreach ( object obj in objectList ) {
						newList.Add( obj.ToGameObject( GetElementNodes( nodes ) ) );
					}
					gameObject.Value = newList;
					break;
				case IFormattable likeString:
					gameObject.Value = likeString.ToString();
					break;
				default:
					throw new NotImplementedException( $"Unable to convert {value.GetType().Name} to GameObject" );
			}
			return gameObject;
		}
		/// <summary>
		/// Obtains the type description for elements of an array
		/// </summary>
		/// <remarks>
		/// <see cref="TypeTree.Node"/> arrays contain information about the
		/// structure of an object from <see cref="AssetsTools.Dynamic"/>. For
		/// an enumerable collection of such objects described by <paramref
		/// name="nodes"/>, <see cref="GetElementNodes(TypeTree.Node[])"/>
		/// parses the array and returns an array describing the element type
		/// instead.
		/// </remarks>
		/// <param name="nodes"><see cref="TypeTree.Node"/> array describing a
		/// collection of elements</param>
		/// <returns><see cref="TypeTree.Node"/> array describing an element of
		/// the collection</returns>
		private static TypeTree.Node[] GetElementNodes( TypeTree.Node[] nodes ) {
			if ( nodes is null ) return null;
			TypeTree.Node node = nodes[0];
			int nodeLevel = node.Level;
			if ( !nodes[1].IsArray
				|| nodes.Length < 4
				|| nodes[1].Name != "Array"
				|| nodes[2].Level != nodeLevel + 2
				|| nodes[3].Level != nodeLevel + 2 ) {
				throw new TypeLoadException( "Invalid node tree for enumerable type" );
			}
			TypeTree.Node[] elementNodes = new TypeTree.Node[nodes.Length - 3];
			elementNodes[0] = nodes[3]; // element type root node
			for ( int i = 4; i < nodes.Length; i++ ) {
				if ( nodes[i].Level < nodeLevel + 3 ) {
					throw new TypeLoadException( "Node tree not valid" );
				}
				elementNodes[i - 3] = nodes[i];
			}
			return elementNodes;
		}
		/// <summary>
		/// Obtains the type description for each child of a node
		/// </summary>
		/// <remarks>
		/// <see cref="TypeTree.Node"/> arrays are hierarchical structures that contain information about the
		/// structure of an object from <see cref="AssetsTools.Dynamic"/>. Member objects are
		/// in turn described by the children of the "root" node describing the first object.
		/// <see cref="GetElementNodes(TypeTree.Node[])"/>
		/// parses the array and returns a list of node arrays describing the member types.
		/// </remarks>
		/// <param name="nodes"><see cref="TypeTree.Node"/> array describing a
		/// object</param>
		/// <returns><see cref="List{T}"/> of <see cref="TypeTree.Node"/> arrays describing the members of the object</returns>
		private static List<TypeTree.Node[]> GetChildNodes( TypeTree.Node[] nodes ) {
			if ( nodes is null || nodes.Length == 0 ) {
				throw new ArgumentNullException( "nodes" );
			}
			TypeTree.Node node = nodes[0];
			List<TypeTree.Node[]> children = new List<TypeTree.Node[]>();
			if ( nodes.Length == 1 ) return children;
			int nodeLevel = node.Level;
			for ( int i = 1; i < nodes.Length; i++ ) {
				if ( nodes[i].Level == nodeLevel + 1 ) {
					List<TypeTree.Node> childTree = new List<TypeTree.Node>();
					childTree.Add( nodes[i] );
					while ( i + 1 < nodes.Length && nodes[i + 1].Level > nodeLevel + 1 ) {
						i++;
						childTree.Add( nodes[i] );
					}
					children.Add( childTree.ToArray() );
				} else {
					throw new TypeLoadException( "Invalid node tree" );
				}
			}
			return children;
		}
	}
}
