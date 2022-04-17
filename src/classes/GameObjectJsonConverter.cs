using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mffer {
	/// <summary>
	/// Helper class to write <see cref="GameObject"/> instances to JSON
	/// </summary>
	public class GameObjectJsonConverter : JsonConverterFactory {
		/// <summary>
		/// Identifies whether <see cref="GameObjectJsonConverter"/> has the
		/// capability to handle an object of a given <see cref="Type"/>
		/// </summary>
		/// <param name="typeToConvert"><see cref="Type"/> to evaluate</param>
		/// <returns><c>true</c> if <see cref="GameObjectJsonConverter"/>
		/// supports handling <paramref name="typeToConvert"/> (i.e., if
		/// <paramref name="typeToConvert"/> derives from
		/// <see cref="GameObject"/>); <c>false</c> otherwise</returns>
		public override bool CanConvert( Type typeToConvert ) {
			return typeof( GameObject ).IsAssignableFrom( typeToConvert );
		}
		/// <summary>
		/// Creates a custom class instance that converts a specific
		/// object type to JSON format
		/// </summary>
		/// <param name="typeToConvert"><see cref="Type"/> of object to for which to create
		/// a converter class</param>
		/// <param name="options"><see cref="JsonSerializerOptions"/> to use
		/// in the serialization of the object</param>
		/// <returns></returns>
		public override JsonConverter CreateConverter( Type typeToConvert, JsonSerializerOptions options ) {
			JsonConverter converter = (JsonConverter)Activator.CreateInstance(
				typeof( GameObjectJsonConverterInner<> ).MakeGenericType( new Type[] { typeToConvert } ),
				BindingFlags.Instance | BindingFlags.Public, null, null, null );
			return converter;
		}
		class GameObjectJsonConverterInner<T> : JsonConverter<T> where T : GameObject {
			/// <summary>
			/// Creates a GameObject-derived object from JSON formatted text
			/// </summary>
			/// <remarks>
			/// Of note, this depends upon the GameObject-derived class having a parameterless
			/// constructor. It also requires that any of the class's <c>dynamic</c> properties or
			/// others of type <see cref="System.Object"/> have the same restrictions as the
			/// <see cref="GameObject.Value"/> property, namely that its value can only be
			/// <c>null</c>, a <see cref="String"/>, a GameObject-derived object, or a <see cref="List{T}"/>
			/// of GameObject-derived objects.
			/// </remarks>
			/// <param name="reader"></param>
			/// <param name="typeToConvert"></param>
			/// <param name="options"></param>
			/// <returns></returns>
			public override T Read( ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options ) {
				if ( reader.TokenType != JsonTokenType.StartObject ) throw new JsonException();
				T gameObject = Activator.CreateInstance<T>();
				if ( typeToConvert != typeof( T ) ) throw new NotSupportedException();
				Dictionary<string, PropertyInfo> properties = new Dictionary<string, PropertyInfo>();
				foreach ( PropertyInfo property in typeToConvert.GetProperties() )
					properties.Add( property.Name, property );
				while ( reader.Read() ) {
					switch ( reader.TokenType ) {
						case JsonTokenType.EndObject:
							return (T)gameObject;
						case JsonTokenType.PropertyName:
							string propertyName = reader.GetString();
							if ( properties.ContainsKey( propertyName ) ) {
								Type propertyType = properties[propertyName].PropertyType;
								if ( properties[propertyName].GetSetMethod() == null ) {
									if ( !reader.TrySkip() ) throw new JsonException();
								} else {
									if ( propertyType == typeof( Object ) ) { // then this is (probably) the dynamic Value
										Utf8JsonReader tempReader = reader; // clones the reader struct, leaving the original in position
										if ( !tempReader.Read() ) throw new JsonException();
										switch ( tempReader.TokenType ) {
											case JsonTokenType.Null:
												propertyType = typeof( String );
												break;
											case JsonTokenType.String:
												propertyType = typeof( String );
												break;
											case JsonTokenType.StartArray:
												if ( !tempReader.Read() ) throw new JsonException();
												if ( tempReader.TokenType == JsonTokenType.EndArray )
													propertyType = typeof( List<GameObject> );
												else if ( tempReader.TokenType != JsonTokenType.StartObject )
													throw new JsonException();
												else {
													Type memberType = InferGameObjectType( tempReader );
													propertyType = typeof( List<> ).MakeGenericType( new Type[] { memberType } );
												}
												break;
											case JsonTokenType.StartObject:
												propertyType = InferGameObjectType( tempReader );
												break;
											default:
												throw new JsonException();
										}
									}
									properties[propertyName].SetValue( gameObject, JsonSerializer.Deserialize( ref reader, propertyType, options ) );
								}
							} else {
								if ( !reader.TrySkip() ) throw new JsonException();
							}
							break;
						default:
							throw new JsonException();
					}
				}
				throw new JsonException();
			}
			/// <summary>
			/// Writes a JSON-formatted representation of a <see cref="GameObject"/>
			/// </summary>
			/// <param name="writer">JSON stream writer to which to write</param>
			/// <param name="value"><see cref="GameObject"/> instance to write</param>
			/// <param name="options"><see cref="JsonSerializerOptions"/> to use in writing</param>
			public override void Write( Utf8JsonWriter writer, T value, JsonSerializerOptions options ) {
				writer.WriteStartObject();
				Type type = value.GetType();
				PropertyInfo[] properties = type.GetProperties( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static );
				Array.Sort( properties, new PropertyComparer() );
				foreach ( PropertyInfo property in properties ) {
					if ( property.IsDefined( typeof( JsonIgnoreAttribute ) ) ) continue;
					writer.WritePropertyName( property.Name );
					if ( property.GetIndexParameters().Length == 0 ) {
						JsonSerializer.Serialize( writer, property.GetValue( value ), property.PropertyType, options );
					} else {
						throw new NotImplementedException();
					}
				}
				writer.WriteEndObject();
			}
			/// <summary>
			/// Determine which GameObject-derived type is represented by the JSON text
			/// </summary>
			/// <remarks>
			/// This is intended to determine the more specific derived type being parsed
			/// by examining the properties of an object in JSON format. The <see paramref="reader"/>
			/// should be positioned inside the beginning of the object (e.g., having already
			/// read the <c>{</c> starting the object). The reader is not suitable for use after this
			/// method as its position is not guaranteed. Thus, use a clone of the reader used for
			/// the real data input.
			/// </remarks>
			/// <param name="reader"><see cref="Utf8JsonReader"/> inside the beginning of the object to be examined</param>
			/// <returns><see cref="Type"/> of the object examined</returns>
			Type InferGameObjectType( Utf8JsonReader reader ) {
				Dictionary<string, JsonTokenType> jsonProperties = new Dictionary<string, JsonTokenType>();
				string currentProperty;
				JsonTokenType currentType;
				while ( reader.Read() && reader.TokenType != JsonTokenType.EndObject ) {
					if ( reader.TokenType != JsonTokenType.PropertyName ) throw new JsonException();
					currentProperty = reader.GetString();
					if ( !reader.Read() ) throw new JsonException();
					if ( reader.TokenType == JsonTokenType.Comment
						|| reader.TokenType == JsonTokenType.EndArray
						|| reader.TokenType == JsonTokenType.EndObject
						|| reader.TokenType == JsonTokenType.None
						|| reader.TokenType == JsonTokenType.PropertyName )
						throw new JsonException();
					currentType = reader.TokenType;
					jsonProperties.Add( currentProperty, currentType );
					reader.Skip();
				}
				if ( jsonProperties.Count == 0 ) return typeof( GameObject );
				if ( jsonProperties.Count == 1 && jsonProperties.ContainsKey( "Value" ) ) return typeof( GameObject );
				IEnumerable<Type> derivedClasses = Assembly.GetAssembly( typeof( GameObject ) ).GetTypes().Where(
					 type => typeof( GameObject ).IsAssignableFrom( type )
				);
				Dictionary<Type, int[]> typeMatches = new Dictionary<Type, int[]>();
				foreach ( Type type in derivedClasses ) {
					int both = 0, typeOnly = 0, jsonOnly = 0;
					List<string> typeProperties = new List<string>();
					foreach ( PropertyInfo property in type.GetProperties() ) {
						typeProperties.Add( property.Name );
						if ( jsonProperties.ContainsKey( property.Name ) ) both++;
						// would be nice to test whether the types are compatible, but
						// this is difficult because of crazy things like importing a
						// JsonValueKind.String into a DateTimeOffset
						else typeOnly++;
					}
					foreach ( string property in jsonProperties.Keys ) {
						if ( !typeProperties.Contains( property ) ) jsonOnly++;
					}
					int[] matchResults = new int[] { both, typeOnly, jsonOnly };
					if ( both > 0 ) typeMatches.Add( type, matchResults );
				}
				if ( typeMatches.Count <= 1 ) return typeof( GameObject );
				List<KeyValuePair<Type, int[]>> typeList = typeMatches.ToList();
				typeList.Sort( CompareMatches );
				return typeList.Last().Key;
			}
			private static int CompareMatches( KeyValuePair<Type, int[]> x, KeyValuePair<Type, int[]> y ) {
				int bothX = x.Value[0];
				int typeOnlyX = x.Value[1];
				int jsonOnlyX = x.Value[2];
				int bothY = y.Value[0];
				int typeOnlyY = y.Value[1];
				int jsonOnlyY = y.Value[2];
				if ( bothX > bothY ) return 1;
				if ( bothY > bothX ) return -1;
				if ( typeOnlyX < typeOnlyY ) return 1;
				if ( typeOnlyY < typeOnlyX ) return -1;
				if ( jsonOnlyX < jsonOnlyY ) return 1;
				if ( jsonOnlyY < jsonOnlyX ) return -1;
				if ( x.Key.IsAssignableFrom( y.Key ) && !y.Key.IsAssignableFrom( x.Key ) ) return 1;
				if ( y.Key.IsAssignableFrom( x.Key ) && !x.Key.IsAssignableFrom( y.Key ) ) return -1;
				return 0;
			}
		}
		class PropertyComparer : IComparer<PropertyInfo> {
			public int Compare( PropertyInfo a, PropertyInfo b ) {
				return StringComparer.InvariantCulture.Compare( a.Name, b.Name );
			}
		}
	}
}
