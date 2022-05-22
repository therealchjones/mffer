using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Mffer {
	public class AndroidCredentials : GameObject {
		public AndroidCredentials() : base() {
			Value = new Dictionary<string, GameObject>() {
				{ "Email", null },
				{ "Password", null},
				{ "OauthToken", null },
				{ "AndroidId", null },
			};
		}
		public AndroidCredentials( string fileName ) : this() {
			FileInfo file = new( fileName );
			if ( !file.Exists ) throw new ArgumentException( $"File '{file.FullName}' does not exist." );

			Load( file );
		}
		public void Load( FileInfo file ) {
			JsonElement json = JsonDocument.Parse( File.ReadAllText( file.FullName ) ).RootElement.GetProperty( "Value" );
			Load( json );
		}
		public void Save( string fileName ) {
			FileInfo file = new( fileName );
			JsonSerializerOptions serialOptions = new JsonSerializerOptions();
			JsonWriterOptions writeOptions = new JsonWriterOptions() { Indented = true, SkipValidation = false };
			using ( Stream stream = new FileStream( file.FullName, FileMode.Create ) ) {
				ToJson( stream, serialOptions, writeOptions );
			}
		}
	}
}
