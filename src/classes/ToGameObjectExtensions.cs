using System;
using System.Collections;
using System.Collections.Generic;

namespace Mffer {
	/// <summary>
	/// Extensions to convert other objects to <see cref="GameObject"/>s
	/// </summary>
	/// <remarks>
	/// To standardize access to data in a wide variety of object types, most
	/// <see cref="Game"/> data is converted to <see cref="GameObject"/>
	/// objects. The <see cref="ToGameObjectExtensions"/> class includes the
	/// polymorphic extension method <see
	/// cref="ToGameObjectExtensions.ToGameObject(object)"/> and
	/// supporting members.
	/// </remarks>
	public static class ToGameObjectExtensions {
		/// <summary>
		/// Convert an <see cref="Object"/> to a <see cref="GameObject"/>
		/// </summary>
		/// <remarks>
		/// <para>Where possible, <see
		/// cref="ToGameObject(object)"/> creates a new <see
		/// cref="GameObject"/> containing the data from a given <see
		/// cref="Object"/>, thus limiting the need for other methods to deal
		/// with arbitrary object formats. (If <paramref name="value"/> is
		/// already a <see cref="GameObject"/>, it is returned
		/// unchanged.)</para>
		/// </remarks>
		/// <param name="value">The <see cref="Object"/> to convert</param>
		/// <returns><see cref="GameObject"/> containing the data from <paramref
		/// name="value"/></returns>
		public static GameObject ToGameObject( this object value ) {
			if ( value is GameObject g ) return g;
			GameObject gameObject = new GameObject();
			switch ( value ) {
				case string s: // strings can be more generally classified as arrays of chars, so this type comes first
					gameObject.Value = s;
					break;
				case Dictionary<string, GameObject> gameObjectDictionary:
					gameObject.Value = gameObjectDictionary;
					break;
				case List<GameObject> gameObjectList:
					gameObject.Value = gameObjectList;
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
						newDictionary.Add( key.ToString(), objectDictionary[key].ToGameObject() );
					}
					gameObject.Value = newDictionary;
					break;
				case IEnumerable objectList: // Note that (generic) IEnumerable<object> won't match int[]
					List<GameObject> newList = new List<GameObject>();
					foreach ( object obj in objectList ) {
						newList.Add( obj.ToGameObject() );
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
	}
}
