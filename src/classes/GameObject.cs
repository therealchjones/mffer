using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using MessagePack;

namespace Mffer {
	/// <summary>
	/// Represents a simple object from the <see cref="Game"/>
	/// </summary>
	/// <remarks>
	/// Each <see cref="GameObject"/> is a simple object containing data that
	/// can be formatted as a string, an array of <see cref="GameObject"/>s,
	/// a dictionary of named <see cref="GameObject"/>s, or <c>null</c>. A <see
	/// cref="GameObject"/> can be easily represented in <a href="https://json.org">JSON</a> format; a <see
	/// cref="GameObject"/> is analagous to a JSON document (though more
	/// restrictive). <see cref="GameObject"/>s form the base from which other
	/// game data such as <see cref="Asset"/>s are derived. This class
	/// contains the basic structure and simple methods for manipulation of the
	/// objects that can be further extended as needed.
	/// </remarks>
	/// <seealso cref="Asset"/>
	/// <seealso cref="PreferenceObject"/>
	public class GameObject : DynamicObject {
		/// <summary>
		/// The underlying value of the <see cref="GameObject"/>
		/// </summary>
		/// <remarks>This should be accessed via the <see cref="Value"/> property</remarks>
		private dynamic _value = null;
		/// <summary>
		/// Gets or sets the value of the object, which may be a string,
		/// array, dictionary of <see cref="GameObject"/>s indexed by strings,
		/// or null
		/// </summary>
		public virtual dynamic Value {
			get {
				if ( IsValidValue( _value ) ) return _value;
				throw new InvalidOperationException( $"The object's type is not allowed: {_value.GetType().Name}" );
			}
			set {
				if ( IsValidValue( value ) ) {
					_value = value;
				} else {
					throw new InvalidOperationException( $"Unable to give object value of type {value.GetType().Name}." );
				}
			}
		}
		/// <summary>
		/// Initializes a new <see cref="GameObject"/> instance
		/// </summary>
		public GameObject() : base() {
			Value = null;
		}
		/// <summary>
		/// Obtains the number of items represented by this <see
		/// cref="GameObject"/>
		/// </summary>
		/// <remarks>
		/// If the <see cref="GameObject"/> represents a collection of items
		/// (i.e., is equivalent to a <see cref="List{GameObject}"/> or a <see
		/// cref="Dictionary{String,GameObject}"/>), then <see cref="Count()"/>
		/// returns the number of items in the collection.
		/// </remarks>
		/// <returns>The number of items represented by this <see
		/// cref="GameObject"/></returns>
		/// <exception cref="InvalidOperationException"> if this <see
		/// cref="GameObject"/> has a simple <see cref="String"/> value or
		/// <c>null</c>, i.e., is not a collection of other <see
		/// cref="GameObject"/>s</exception>
		public int Count() {
			if ( IsArray() || IsDictionary() ) {
				return Value.Count;
			} else {
				throw new InvalidOperationException( "Unable to count; value is not an array or dictionary." );
			}
		}
		/// <summary>
		/// Obtains the names of properties that can be accessed if this <see cref="GameObject"/> is cast as <c>dynamic</c>
		/// </summary>
		/// <returns><see cref="IEnumerable{String}"/> list of <c>dynamic</c> property names</returns>
		public override IEnumerable<string> GetDynamicMemberNames() {
			if ( IsDictionary() ) return ( (Dictionary<string, GameObject>)Value ).Keys.AsEnumerable();
			else return new List<string>();
		}
		/// <summary>
		/// Integral accessor for this <see cref="GameObject"/>
		/// </summary>
		/// <param name="index"><see cref="Int32"/> index of the value to return</param>
		/// <returns>The value at <c>index</c>, iff this <see cref="GameObject"/> is an array type</returns>
		/// <exception cref="InvalidOperationException">if this <see cref="GameObject"/> is not an array type</exception>
		GameObject GetObject( int index ) {
			if ( TryGetObject( index, out GameObject item ) ) {
				return item;
			} else {
				throw new InvalidOperationException( $"Unable to get an object at index {index}." );
			}
		}
		bool TryGetObject( int index, out GameObject item ) {
			item = null;
			if ( !IsArray() ) return false;
			if ( Value.Count <= index ) return false;
			else {
				item = Value[index];
				return true;
			}
		}
		/// <summary>
		/// String accessor for this <see cref="GameObject"/>
		/// </summary>
		/// <param name="key"><see cref="String"/> key of the value to return</param>
		/// <returns>The value at <c>key</c>, iff this <see cref="GameObject"/> is a <see cref="Dictionary{K,V}"/> type</returns>
		/// <exception cref="InvalidOperationException">if this <see cref="GameObject"/> is not a <see cref="Dictionary{K,V}"/> type</exception>
		GameObject GetObject( string key ) {
			if ( TryGetObject( key, out GameObject item ) ) {
				return item;
			} else {
				throw new InvalidOperationException( $"Unable to get an object with key {key}." );
			}
		}
		bool TryGetObject( string key, out GameObject item ) {
			item = null;
			if ( !IsDictionary() ) return false;
			if ( !( (Dictionary<string, GameObject>)Value ).ContainsKey( key ) ) return false;
			else {
				item = Value[key];
				return true;
			}
		}
		bool TryGetUniqueValue( out string value ) {
			value = null;
			if ( IsString() || _value is null ) {
				value = Value;
				return true;
			}
			if ( Value.Count == 0 ) {
				return false;
			}
			if ( IsArray() && Value.Count == 1 )
				return ( (List<GameObject>)Value )[0].TryGetUniqueValue( out value );
			if ( IsDictionary() && Value.Count == 1 )
				return ( (Dictionary<string, GameObject>)Value ).First().Value.TryGetUniqueValue( out value );
			return false;
		}
		bool TryGetUniqueValue( string key, out string value ) {
			value = null;
			if ( key is null ) return TryGetUniqueValue( out value );
			if ( IsString() || _value is null ) return false;
			if ( Value.Count == 0 ) return false;
			if ( IsArray() && Value.Count == 1 )
				return Value[0].TryGetUniqueValue( key, out value );
			if ( IsDictionary() && ( (Dictionary<string, GameObject>)Value ).ContainsKey( key ) )
				return Value[key].TryGetUniqueValue( out value );
			if ( IsDictionary() && Value.Count == 1 )
				return Value.First().Value.TryGetUniqueValue( key, out value );
			return false;
		}
		bool TryGetUniqueValue( int index, out string value ) {
			value = null;
			if ( IsString() || _value is null ) return false;
			if ( Value.Count == 0 ) return false;
			if ( IsArray() && Value.Count == 1 ) {
				if ( Value[0].TryGetUniqueValue( index, out value ) ) {
					return true;
				} else if ( index == 0 )
					return Value[0].TryGetUniqueValue( out value );
			}
			if ( IsArray() && index < Value.Count )
				return Value[index].TryGetUniqueValue( out value );
			if ( IsDictionary() && Value.Count == 1 )
				return Value.First().Value.TryGetUniqueValue( index, out value );
			return false;
		}
		/// <summary>
		/// Obtains the object that is the most distant descendant of the
		/// current GameObject that can be identified uniquely
		/// </summary>
		/// <remarks>
		/// A <see cref="GameObject"/> may be diagrammed or visualized as a
		/// tree, where each child GameObject (i.e., each value of an array-type
		/// or dictionary-type GameObject) forms a "branch" and null, string, or
		/// empty arrays or dictionaries form "leaves". As it is common for
		/// array-type and dictionary-type GameObjects to have only one child as
		/// an artifact of importing, <see cref="GameObject.GetUniqueObject()"/>
		/// is useful for "squashing" those trivial objects and returning the
		/// next descendant that is a "true" branch point or "leaf". Note that
		/// this removes intervening structures such as dictionary key names
		/// when bypassing dictionaries with a single key.
		/// </remarks>
		/// <returns>The most distant <see cref="GameObject"/> that is an
		/// "unbranched" descendant of the current GameObject</returns>
		GameObject GetUniqueObject() {
			if ( Value is null ) return this;
			if ( IsString() ) return this;
			if ( Value.Count == 0 ) return this;
			if ( IsArray() && Value.Count == 1 ) return Value[0].GetUniqueObject();
			if ( IsDictionary() && Value.Count == 1 ) return Value.First().Value.GetUniqueObject();
			return this;
		}
		/// <summary>
		///	Obtains a value from a nested <see cref="GameObject"/>
		/// </summary>
		/// <remarks>
		/// When it is possible to definitively select a <see cref="String"/> or
		/// null value that is represented by the <see cref="GameObject.Value"/>
		/// property or its descendants, optionally with a single level of
		/// branching where a branch can be chosen with the <paramref
		/// name="key"/> parameter, <see cref="GetValue"/> will return the
		/// value. Note that this is different from the <see
		/// cref="GetObject(string)"/> method, which does not search nested
		/// levels of <see cref="GameObject"/>s.
		/// </remarks>
		/// <param name="key">The optional name of the value for which to
		/// search</param>
		/// <returns>The value associated with this <see cref="GameObject"/> and
		/// (optionally) <paramref name="key"/></returns>
		/// <exception cref="KeyNotFoundException"> if no single value can be
		/// definitively chosen</exception>
		public virtual string GetValue( string key = null ) {
			if ( TryGetUniqueValue( key, out string value ) ) {
				return value;
			} else {
				throw new KeyNotFoundException( $"Unable to identify a value uniquely associated with key '{key}'" );
			}
		}
		/// <summary>
		/// Reports whether a <see cref="GameObject"/> is an array type
		/// </summary>
		/// <param name="obj">the <see cref="GameObject"/> to evaluate; this <see cref="GameObject"/> if <c>null</c></param>
		/// <returns><c>true</c> if the <see cref="GameObject"/> is an array type, <c>falce</c> otherwise</returns>
		bool IsArray( dynamic obj = null ) {
			Type type;
			if ( obj is null ) {
				if ( _value is null ) return false;
				else type = _value.GetType();
			} else type = obj.GetType();
			if ( type.IsGenericType &&
				type.GetGenericTypeDefinition() == typeof( List<> ) &&
				typeof( GameObject ).IsAssignableFrom( type.GenericTypeArguments[0] ) ) return true;
			else
				return false;
		}
		/// <summary>
		/// Reports whether a <see cref="GameObject"/> is a dictionary type
		/// </summary>
		/// <param name="obj">the <see cref="GameObject"/> to evaluate; this <see cref="GameObject"/> if <c>null</c></param>
		/// <returns><c>true</c> if the <see cref="GameObject"/> is a dictiomary type, <c>falce</c> otherwise</returns>
		bool IsDictionary( dynamic obj = null ) {
			Type type;
			if ( obj is null ) {
				if ( _value is null ) return false;
				else type = _value.GetType();
			} else type = obj.GetType();
			if ( type.IsGenericType &&
				type.GetGenericTypeDefinition() == typeof( Dictionary<,> ) &&
				type.GenericTypeArguments[0] == typeof( String ) &&
				typeof( GameObject ).IsAssignableFrom( type.GenericTypeArguments[1] ) ) {
				return true;
			} else {
				return false;
			}
		}
		/// <summary>
		/// Reports whether a <see cref="GameObject"/> is a string type
		/// </summary>
		/// <returns><c>true</c> if the <see cref="GameObject"/> is an string type, <c>falce</c> otherwise</returns>
		bool IsString() {
			if ( _value is string ) return true;
			else return false;
		}
		/// <summary>
		/// Reports whether an object is a valid value for a <see
		/// cref="GameObject"/>
		/// </summary>
		/// <remarks>
		/// <see cref="GameObject"/>s must be representable as a <c>null</c>
		/// value, a <see cref="String"/>, an array of <see
		/// cref="GameObject"/>s, or a <see cref="Dictionary{K,V}"/> of <see
		/// cref="GameObject"/>s indexed by <see cref="String"/>s.
		/// </remarks>
		/// <param name="obj">the object to evaluate</param>
		/// <returns><c>true</c> if the object is null, a string, an array of
		/// <see cref="GameObject"/>s, or a string-indexed <see
		/// cref="Dictionary{K,V}"/> of <see cref="GameObject"/>s;
		/// <c>falce</c> otherwise</returns>
		bool IsValidValue( dynamic obj ) {
			if ( obj is null || obj is string || IsArray( obj ) || IsDictionary( obj ) ) return true;
			else return false;
		}
		/// <summary>
		/// Attempts to access this <see cref="GameObject"/>'s children using a
		/// string key
		/// </summary>
		/// <remarks>
		/// This method is seldom needed directly; it is used when
		/// <c>dynamic</c>ly accessing a <see cref="GameObject"/> that
		/// represents a dictionary of other <see cref="GameObject"/>s indexed
		/// by strings, such as <c>gameObject["Property1"]</c>.
		/// </remarks>
		/// <param name="binder"><see cref="GetMemberBinder"/> used by the
		/// <c>dynamic</c> call</param>
		/// <param name="result"><see cref="GameObject"/> in which to store the
		/// result if successful</param>
		/// <returns><c>true</c> if this <see cref="GameObject"/> is a
		/// dictionary type and can retrieve an item with the requested key,
		/// <c>false</c> otherwise</returns>
		public override bool TryGetMember( GetMemberBinder binder, out object result ) {
			try {
				result = GetObject( binder.Name );
				return true;
			} catch ( InvalidOperationException ) {
				try {
					result = GetValue( binder.Name );
					return true;
				} catch ( Exception e ) when ( e is InvalidOperationException || e is KeyNotFoundException ) {
					result = null;
					return false;
				}
			}
		}
		/// <summary>
		/// Attempts to access this <see cref="GameObject"/>'s children by
		/// integer index
		/// </summary>
		/// <remarks>
		/// This method is seldom needed directly; it is used when
		/// <c>dynamic</c>ly accessing a <see cref="GameObject"/> that
		/// represents a list of other <see cref="GameObject"/>s by index, such
		/// as <c>gameObject[2]</c>.
		/// </remarks>
		/// <param name="binder"><see cref="GetIndexBinder"/> associated with
		/// the <c>dynamic</c> call</param>
		/// <param name="indexes"><see cref="Array"/> of the requested
		/// indexes</param>
		/// <param name="result"><see cref="GameObject"/> in which to store the
		/// result if successful</param>
		/// <returns><c>true</c> if this <see cref="GameObject"/> is a list type
		/// and can retrieve an item at the requested index, <c>false</c>
		/// otherwise</returns>
		/// <exception cref="NotSupportedException"> if there is more than one
		/// index or it is not an integer</exception>
		public override bool TryGetIndex( GetIndexBinder binder, object[] indexes, out object result ) {
			result = null;
			if ( !IsArray() ) {
				return false;
			} else {
				if ( indexes.Length != 1 ) throw new NotSupportedException();
				if ( indexes[0] is not int ) throw new NotSupportedException();
				int index = (int)indexes[0];
				if ( ( (List<GameObject>)Value ).Count <= index ) return false;
				else {
					result = ( (List<GameObject>)Value )[index];
					return true;
				}
			}
		}
		/// <summary>
		/// Reports whether this <see cref="GameObject"/> contains a given list
		/// item or dictionary key
		/// </summary>
		/// <remarks><see cref="GameObject.Contains(string)"/> examines this
		/// <see cref="GameObject"/> and its descendents to determine if there
		/// is a unique descendent with a value that is a <see cref="List{T}"/>
		/// of GameObjects and that the list contains a GameObject with a single
		/// descendent branch and the descendent value is the string <see
		/// paramref="item"/>. Put another way, the method determines whether
		/// there is some integer <c>i</c> such that <c>this[i]</c> is or
		/// <c>this.GetValue(i)</c> returns <c>item</c>.</remarks>
		/// <param name="item"><see cref="string"/> for which to search</param>
		/// <returns><c>true</c> if the GameObject satisfies the condition
		/// above, false if the GameObject is not an array-type GameObject or
		/// does not contain such an item.</returns>
		public virtual bool Contains( string item ) {
			if ( item is null ) throw new ArgumentNullException( nameof( item ) );
			if ( IsString() || _value is null ) return false;
			if ( IsDictionary() ) {
				if ( Value.Count == 0 ) return false;
				if ( Value.ContainsKey( item ) ) return true;
				if ( Value.Count > 1 ) return false;
				return Value.First().Value.Contains( item );
			}
			if ( IsArray() ) {
				if ( Value.Count == 0 ) return false;
				foreach ( GameObject gameObject in (List<GameObject>)Value ) {
					GameObject leaf = gameObject.GetUniqueObject();
					if ( leaf.IsString() && leaf.Value == item ) return true;
					if ( leaf.IsDictionary() || leaf.IsArray() )
						if ( leaf.Contains( item ) ) return true;
				}
			}
			return false;
		}
		/// <summary>
		/// Loads associated data into the appropriate members
		/// </summary>
		/// <remarks>
		/// Derived classes should implement <see cref="LoadAll()"/> to parse
		/// data from other associated objects into appropriate class members. This would typically
		/// call an appropriate <c>Load</c> method for each of the included associated objects. (See, for instance,
		/// <see cref="AssetBundle.LoadAll()"/>.)
		/// </remarks>
		public virtual void LoadAll() {
			return;
		}
		/// <summary>
		/// Parses JSON into this <see cref="GameObject"/>'s value
		/// </summary>
		/// <param name="element">A <see cref="JsonElement"/> from a
		/// <see cref="JsonDocument"/> to load</param>
		public virtual void Load( JsonElement element ) {
			switch ( element.ValueKind ) {
				case JsonValueKind.Object:
					Value = new Dictionary<string, GameObject>();
					foreach ( JsonProperty jsonProperty in element.EnumerateObject() ) {
						GameObject newObject = new GameObject();
						newObject.Load( jsonProperty.Value );
						// Use item[] instead of Add() to allow duplicate keys,
						// with later ones overwriting previous, something that
						// occurs sometimes in the level.txt TextAssets
						( (Dictionary<string, GameObject>)Value )[jsonProperty.Name] = newObject;
					}
					return;
				case JsonValueKind.Array:
					int arrayCounter = 0;
					Value = new List<GameObject>();
					foreach ( JsonElement jsonElement in element.EnumerateArray() ) {
						GameObject newObject = new GameObject();
						newObject.Load( jsonElement );
						( (List<GameObject>)Value ).Insert( arrayCounter, newObject );
						arrayCounter++;
					}
					return;
				case JsonValueKind.Undefined:
					throw new JsonException( $"Unable to parse JSON element." );
				default:
					Value = element.ToString();
					return;
			}
		}
		/// <summary>
		/// Parses XML into this <see cref="GameObject"/>'s value
		/// </summary>
		/// <remarks>
		/// <see cref="GameObject.Load(XmlNode)"/> implicitly associates the <paramref name="node"/>
		/// data with this <see cref="GameObject"/>. Since the <see cref="GameObject"/> itself
		/// may have no "name" (but potentially be referred to by a parent <see cref="GameObject"/>),
		/// only <see cref="GameObject.Value"/> is modified by this method, and any "name" must
		/// be determined by a calling method.
		/// </remarks>
		/// <param name="node">The XML node to parse</param>
		public void Load( XmlNode node ) {
			node.Normalize();
			switch ( node.NodeType ) {
				// node types to ignore
				case XmlNodeType.Comment:
				case XmlNodeType.DocumentType:
				case XmlNodeType.Whitespace:
				case XmlNodeType.XmlDeclaration:
					Value = null;
					return;
				case XmlNodeType.Text:
					string textString = DecodeString( node.Value );
					if ( textString.Contains( '{' ) ) {
						try {
							JsonDocumentOptions jsonOptions = new JsonDocumentOptions {
								CommentHandling = JsonCommentHandling.Skip,
								AllowTrailingCommas = true
							};
							JsonDocument json = JsonDocument.Parse( textString, jsonOptions );
							Load( json.RootElement );
							json.Dispose();
						} catch ( JsonException ) {
							Value = textString;
						}
					} else {
						Value = textString;
					}
					return;
				case XmlNodeType.EntityReference:
					Value = DecodeString( node.InnerText );
					return;
				case XmlNodeType.Document:
					Load( ( (XmlDocument)node ).DocumentElement );
					return;
				case XmlNodeType.Element:
					Dictionary<string, GameObject> dictionary = new Dictionary<string, GameObject>();
					if ( ( (XmlElement)node ).HasAttributes ) {
						foreach ( XmlAttribute attribute in node.Attributes ) {
							if ( attribute.Specified ) {
								string name = attribute.Name;
								GameObject value = new GameObject();
								value.Load( attribute );
								if ( dictionary.ContainsKey( name ) ) {
									throw new FormatException( $"Multiple attributes named '{name}'." );
								}
								dictionary.Add( name, value );
							}
						}
					}
					if ( node.HasChildNodes ) {
						foreach ( XmlNode child in node.ChildNodes ) {
							string name = child.Name;
							if ( !String.IsNullOrEmpty( name ) ) {
								if ( dictionary.ContainsKey( name ) ) {
									int i = 2;
									while ( dictionary.ContainsKey( $"{name}-{i}" ) ) {
										if ( i < node.ChildNodes.Count ) {
											i++;
										} else {
											throw new FormatException( $"Too many nodes named '{name}'." );
										}
									}
									name = $"{name}-{i}";
								}
								GameObject value = new GameObject();
								value.Load( child );
								dictionary.Add( name, value );
							}
						}
					}
					if ( dictionary.Count > 0 ) {
						Value = dictionary;
					} else {
						Value = null;
					}
					return;
				case XmlNodeType.Attribute:
					Value = DecodeString( node.Value );
					return;
				// other node types are not expected to be found in MFF xml
				// files, and we should be alerted if they are
				default:
					throw new NotImplementedException( $"Unable to load XML node of type {node.GetType()}." );
			}
		}
		/// <summary>
		/// Decodes strings as used in raw preference files
		/// </summary>
		/// <remarks>
		/// <p>Preference files in Marvel Future Fight are generally XML files where
		/// data has been encoded as "web safe," often via a combination of standard
		/// URI encoding, MessagePack encoding, and base 64 encoding. Others are plain
		/// strings. <see cref="DecodeString"/> attempts to determine which encodings (if
		/// any) have been applied to a given string and to return the decoded
		/// string.</p>
		/// <p>All MessagePacked strings are binary, hence they are presented as URI-escaped base 64
		/// strings. Therefore, if a string is not valid base 64, <see cref="DecodeString"/>
		/// converts URI escapes and otherwise returns the string as is. If valid base 64,
		/// <see cref="DecodeString"/> decodes this then attempts to parse it as a MessagePack
		/// JSON formatted string. If this step fails, the base 64-decoded string is
		/// returned. No attempt is made to parse the JSON beyond returning the string.</p>
		/// </remarks>
		/// <param name="value">The string to (potentially) decode</param>
		/// <returns>The decoded string, or the original string if not encoded</returns>
		protected static string DecodeString( string value ) {
			string decodedString = value;
			if ( String.IsNullOrEmpty( decodedString ) ) {
				decodedString = "";
			} else {
				decodedString = Uri.UnescapeDataString( decodedString );
				// If the string has a space, leave it alone
				// if the string is just digits, leave it alone
				// If the string is already (consistent with) an MD5 sum, leave as is
				// and then random flags that should stay the same even though they match base64 format
				if ( !decodedString.Contains( ' ' )
					&& !decodedString.Contains( '\t' )
					&& !Regex.IsMatch( decodedString, @"^[0-9]+$" )
					&& !Regex.IsMatch( decodedString, @"^[0-9A-F]{32}$" )
					&& decodedString != "UnityGraphicsQuality"
					&& !decodedString.StartsWith( "cinematicbattlenoticeday" ) ) {
					Span<byte> bytes = new Span<byte>( new byte[decodedString.Length] );
					if ( Convert.TryFromBase64String( decodedString, bytes, out int bytesWritten ) ) {
						bytes = bytes.Slice( 0, bytesWritten );
						try {
							decodedString = MessagePackSerializer.ConvertToJson( bytes.ToArray() );
							if ( !decodedString.Contains( '{' ) ) {
								decodedString = Encoding.Unicode.GetString( bytes );
							}
						} catch ( MessagePackSerializationException ) {
							decodedString = Encoding.Unicode.GetString( bytes );
						}
					}
					// TODO: #104 Add CSV parsing (CSVtoJson) here?
				}
			}
			return decodedString;
		}
		/// <summary>
		/// Converts a CSV-formatted string to JSON format
		/// </summary>
		/// <param name="csv">The data in CSV format</param>
		/// <returns>A string containing the data in JSON format</returns>
		public static string CSVtoJson( string csv ) {
			if ( String.IsNullOrWhiteSpace( csv ) ) return null;
			string[] lines = csv.Split( new char[] { '\r', '\n' } );
			int firstLine;
			string[] headers = null;
			for ( firstLine = 0; firstLine < lines.Length; firstLine++ ) {
				if ( !String.IsNullOrWhiteSpace( lines[firstLine] ) ) {
					headers = lines[firstLine].Split( '\t' );
					for ( int cellNum = 0; cellNum < headers.Length; cellNum++ ) {
						string cellText = headers[cellNum];
						string escapechars = "([\\\"\\\\])";
						Regex regex = new Regex( escapechars );
						cellText = regex.Replace( cellText, "\\$1" );
						headers[cellNum] = cellText;
					}
					break;
				}
			}
			if ( headers == null ) { return null; }
			string jsonArray = "[ ";
			for ( int i = firstLine + 1; i < lines.Length; i++ ) {
				if ( String.IsNullOrWhiteSpace( lines[i] ) ) continue;
				string[] line = lines[i].Split( '\t' );
				if ( line.Length != headers.Length ) {
					throw new Exception( "CSV poorly formed." );
				}
				string lineString = "{";
				for ( int j = 0; j < headers.Length; j++ ) {
					string cellText = line[j];
					string escapechars = "([\\\"\\\\])";
					Regex regex = new Regex( escapechars );
					cellText = regex.Replace( cellText, "\\$1" );
					lineString += $"\"{headers[j]}\": \"{cellText}\"";
					if ( j != headers.Length - 1 ) {
						lineString += ", ";
					}
				}
				lineString += "},";
				jsonArray += lineString;
			}
			jsonArray = jsonArray.TrimEnd( new char[] { ',', ' ', '\t' } );
			jsonArray += " ]";
			return jsonArray;
		}
		/// <summary>
		/// Writes the <see cref="GameObject"/> in <a
		/// href="https://json.org">JSON</a> format to a <see cref="Stream"/>
		/// </summary>
		/// <remarks>
		/// In contrast to the default <see
		/// cref="JsonSerializer.Serialize(object?, Type,
		/// JsonSerializerOptions?)"/> method, <see
		/// cref="GameObject.ToJson(Stream, JsonSerializerOptions,
		/// JsonWriterOptions)"/> will include properties exclusive to classes
		/// derived from <see cref="GameObject"/> in both the "root" object on
		/// which the method is called as well as throughout its membership
		/// hierarchy.
		/// </remarks>
		public void ToJson( Stream stream, JsonSerializerOptions serializerOptions = default, JsonWriterOptions writerOptions = default ) {
			Utf8JsonWriter utf8Writer = new Utf8JsonWriter( stream, writerOptions );
			try {
				serializerOptions.Converters.Add( new GameObjectJsonConverter() );
			} catch ( InvalidOperationException ) {
				serializerOptions = new JsonSerializerOptions( serializerOptions );
				serializerOptions.Converters.Add( new GameObjectJsonConverter() );
			}
			JsonSerializer.Serialize( utf8Writer, this, this.GetType(), serializerOptions );
		}
		/// <summary>
		/// Obtains this <see cref="GameObject"/> formatted as a string
		/// </summary>
		/// <returns>a <see cref="String"/> representing this <see
		/// cref="GameObject"/> that is the simple string the <see
		/// cref="GameObject"/> represents if it is a simple type or null, or
		/// <see cref="Object.ToString()"/> otherwise.</returns>
		public override string ToString() {
			if ( IsString() || _value is null ) return Value;
			else return base.ToString();
		}
	}
}
