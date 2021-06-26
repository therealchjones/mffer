using System;
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
		private class GameObjectJsonConverterInner<T> : JsonConverter<T> where T : GameObject {
			/// <summary>
			/// Creates a GameObject from JSON formatted text
			/// </summary>
			/// <remarks>This method is not implemented.</remarks>
			/// <param name="reader"></param>
			/// <param name="typeToConvert"></param>
			/// <param name="options"></param>
			/// <returns></returns>
			public override T Read( ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options ) {
				throw new NotImplementedException();
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
				foreach ( PropertyInfo property in type.GetProperties() ) {
					writer.WritePropertyName( property.Name );
					if ( property.GetIndexParameters().Length == 0 ) {
						JsonSerializer.Serialize( writer, property.GetValue( value ), property.PropertyType, options );
					} else {
						throw new NotImplementedException();
					}
				}
				writer.WriteEndObject();
			}
		}
	}
}
