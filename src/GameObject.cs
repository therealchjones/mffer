using System;
using System.Collections.Generic;
using System.IO;
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
	/// can be formatted as a string, an array of <see cref="GameObject"/>s, or
	/// a dictionary of named <see cref="GameObject"/>s. A <see
	/// cref="GameObject"/> can be easily represented in JSON format; a <see
	/// cref="GameObject"/> is analagous to a JSON documnent (though more
	/// restrictive). <see cref="GameObject"/>s form the base from which other
	/// game data such as <see cref="AssetObject"/>s and <see
	/// cref="PreferenceObject"/>s are derived. This class contains the basic
	/// structure and simple methods for manipulation of the objects that can be
	/// further extended as needed.
	/// </remarks>
	/// <seealso cref="AssetObject"/>
	/// <seealso cref="PreferenceObject"/>
	/// <seealso href="https://json.org/"/>
	public class GameObject {
		/// <summary>
		/// Gets or sets the value of the object, which may be a string,
		/// array, or object
		/// </summary>
		public dynamic Value {
			get {
				if ( _value is null ) return null;
				if ( _value is string || _value is List<GameObject> || _value is Dictionary<string, GameObject> ) return _value;
				throw new InvalidOperationException( $"The object's type is not allowed: {_value.GetType().Name}" );
			}
			set {
				if ( value is null ) {
					_value = null;
				} else if ( value is string || value is List<GameObject> || value is Dictionary<string, GameObject> ) {
					_value = value;
				} else {
					throw new InvalidOperationException( $"Unable to give object value of type {value.GetType().Name}." );
				}
			}
		}
		private dynamic _value = null;
		/// <summary>
		/// Parses JSON into this <see cref="GameObject"/>'s value
		/// </summary>
		/// <param name="element">A <see cref="JsonElement"/> from a
		/// <see cref="JsonDocument"/> to load</param>
		public void LoadJson( JsonElement element ) {
			switch ( element.ValueKind ) {
				case JsonValueKind.Object:
					Value = new Dictionary<string, GameObject>();
					foreach ( JsonProperty jsonProperty in element.EnumerateObject() ) {
						GameObject newObject = new GameObject();
						newObject.LoadJson( jsonProperty.Value );
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
						newObject.LoadJson( jsonElement );
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
		/// <see cref="GameObject.LoadXml"/> implicitly associates the <paramref name="node"/>
		/// data with this <see cref="GameObject"/>. Since the <see cref="GameObject"/> itself
		/// may have no "name" (but potentially be referred to by a parent <see cref="GameObject"/>),
		/// only <see cref="GameObject.Value"/> is modified by this method, and any "name" must
		/// be determined by a calling method.
		/// </remarks>
		/// <param name="node">The XML node to parse</param>
		public void LoadXml( XmlNode node ) {
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
							LoadJson( json.RootElement );
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
					LoadXml( ( (XmlDocument)node ).DocumentElement );
					return;
				case XmlNodeType.Element:
					Dictionary<string, GameObject> dictionary = new Dictionary<string, GameObject>();
					if ( ( (XmlElement)node ).HasAttributes ) {
						foreach ( XmlAttribute attribute in node.Attributes ) {
							if ( attribute.Specified ) {
								string name = attribute.Name;
								GameObject value = new GameObject();
								value.LoadXml( attribute );
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
								value.LoadXml( child );
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
		string DecodeString( string value ) {
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
				}
			}
			return decodedString;
		}
		/// <summary>
		/// Writes the data from the <see cref="GameObject"/> to a
		/// <see cref="StreamWriter"/> stream
		/// </summary>
		/// <param name="file">The name of the <see cref="StreamWriter"/>
		/// stream to which to write</param>
		/// <param name="tabs">The number of tab characters to prepend to each
		/// line</param>
		/// <seealso cref="Game.Version.WriteJson(StreamWriter, int)"/>
		public virtual void WriteJson( StreamWriter file, int tabs = 0 ) {
			for ( int i = 0; i < tabs; i++ ) {
				file.Write( "\t" );
			}
			switch ( Value ) {
				case string s:
					file.Write( "\"" + JsonEncodedText.Encode( s ) + "\"" );
					return;
				case List<GameObject> array:
					file.WriteLine( "[" );
					for ( int i = 0; i < array.Count; i++ ) {
						array[i].WriteJson( file, tabs + 1 );
						if ( i < array.Count - 1 ) {
							file.Write( "," );
						}
						file.WriteLine();
					}
					for ( int i = 0; i < array.Count; i++ ) {
						file.Write( "\t" );
					}
					file.Write( "]" );
					return;
				case Dictionary<string, GameObject> dictionary:
					file.WriteLine( "{" );
					int entryCounter = 0;
					foreach ( KeyValuePair<string, GameObject> entry in dictionary ) {
						for ( int t = 0; t < tabs + 1; t++ ) {
							file.Write( "\t" );
						}
						file.WriteLine( "\"" + JsonEncodedText.Encode( entry.Key ) + "\" : " );
						entry.Value.WriteJson( file, tabs + 2 );
						if ( entryCounter < dictionary.Count - 1 ) {
							file.Write( "," );
						}
						file.WriteLine();
						entryCounter++;
					}
					for ( int t = 0; t < tabs; t++ ) {
						file.Write( "\t" );
					}
					file.Write( "}" );
					return;
				default:
					throw new FormatException( $"Unable to write object of type {Value.GetType()} in JSON format." );
			}
		}
	}
}
