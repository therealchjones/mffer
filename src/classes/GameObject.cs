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
	/// cref="GameObject"/> can be easily represented in JSON format; a <see
	/// cref="GameObject"/> is analagous to a JSON document (though more
	/// restrictive). <see cref="GameObject"/>s form the base from which other
	/// game data such as <see cref="Asset"/>s are derived. This class
	/// contains the basic structure and simple methods for manipulation of the
	/// objects that can be further extended as needed.
	/// </remarks>
	/// <seealso cref="Asset"/>
	/// <seealso cref="PreferenceObject"/>
	/// <seealso href="https://json.org/"/>
	public class GameObject : DynamicObject {
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
		GameObject GetObject( int index ) {
			if ( IsArray() ) return Value[index];
			throw new InvalidOperationException( $"Unable to get an object at index {index}: value is not an array." );
		}
		GameObject GetObject( string key = null ) {
			if ( key is null ) return this;
			if ( IsDictionary() ) return Value[key];
			throw new InvalidOperationException( $"Unable to get an object with key {key}: value is not a dictionary." );
		}
		/// <summary>
		///	Obtains a value from a nested <see cref="GameObject"/>
		/// </summary>
		/// <remarks>
		/// When it is possible to definitively select a <see cref="String"/> or
		/// null value that is represented by the <see cref="GameObject.Value"/>
		/// property or its descendants, optionally with a single level of
		/// branching where a branch can be chosen with the <see
		/// paramref="key"/> parameter, <see cref="GetValue"/> will return the value.
		/// </remarks>
		/// <param name="key">The optional name of the value for which to
		/// search</param>
		/// <returns>The value associated with this <see cref="GameObject"/> and
		/// (optionally) <paramref name="key"/></returns>
		/// <throws><see cref="KeyNotFoundException"/> if no single value can be definitively chosen</throws>
		public string GetValue( string key = null ) {
			if ( key is null ) {
				if ( IsString() || _value is null ) return Value;
				else if ( IsArray() ) {
					List<GameObject> array = Value as List<GameObject>;
					if ( array.Count == 0 ) return null;
					else if ( array.Count == 1 ) return array[0].GetValue();
					else throw new KeyNotFoundException( "Unable to get unique value: List has multiple items." );
				} else if ( IsDictionary() ) {
					Dictionary<string, GameObject> dictionary = Value as Dictionary<string, GameObject>;
					if ( dictionary.Count == 0 ) return null;
					else if ( dictionary.Count == 1 ) return dictionary.First().Value.GetValue();
					else throw new KeyNotFoundException( "Unable to get unique value: Dictionary has multiple items." );
				} else {
					throw new InvalidOperationException( "Unable to determine type of object represented by value." );
				}
			} else {
				if ( IsString() || _value is null ) {
					throw new KeyNotFoundException( $"No identifier '{key}' was found." );
				} else if ( IsArray() ) {
					List<GameObject> array = Value as List<GameObject>;
					if ( array.Count == 0 ) throw new KeyNotFoundException( $"No identifier '{key}' was found." );
					else if ( array.Count == 1 ) return array[0].GetValue( key );
					else throw new KeyNotFoundException( "Unable to get unique value: List has multiple items." );
				} else if ( IsDictionary() ) {
					Dictionary<string, GameObject> dictionary = Value as Dictionary<string, GameObject>;
					if ( dictionary.Count == 0 ) throw new KeyNotFoundException( $"No identifier '{key}' was found." );
					else {
						if ( dictionary.ContainsKey( key ) ) return dictionary[key].GetValue();
						else if ( dictionary.Count == 1 ) return dictionary.First().Value.GetValue( key );
						else return dictionary[key].GetValue(); // which will throw a KeyNotFoundException
					}
				} else {
					throw new InvalidOperationException( "Unable to determine type of object represented by value." );
				}
			}
		}
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
		bool IsString() {
			if ( _value is string ) return true;
			else return false;
		}
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
		/// Writes the <see cref="GameObject"/> in JSON format to a <see
		/// cref="Stream"/>
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
		/// <seealso href="https://json.org"/>
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
