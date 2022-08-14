using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Mffer {
	/// <summary>
	/// Represents the necessary details for Android/Google login and authorization
	/// </summary>
	public class AndroidCredentials : GameObject {
		/// <summary>
		/// Creates a new instance of <see cref="AndroidCredentials"/>
		/// </summary>
		public AndroidCredentials() : base() {
			Value = new Dictionary<string, GameObject>() {
				{ "Email", null },
				{ "Password", null},
				{ "OauthToken", null },
				{ "AndroidId", null },
			};
		}
		/// <summary>
		/// Creates a new instance of <see cref="AndroidCredentials"/> using data from the given file
		/// </summary>
		/// <param name="fileName">the path to a file containing Android/Google authentication &amp; authorization data</param>
		/// <exception cref="ArgumentException">if the file <paramref name="fileName"/> does not exist</exception>
		public AndroidCredentials( string fileName ) : this() {
			FileInfo file = new( fileName );
			if ( !file.Exists ) throw new ArgumentException( $"File '{file.FullName}' does not exist." );

			Load( file );
		}
		/// <summary>
		/// Loads authorization &amp; authentication data from the given file
		/// </summary>
		/// <param name="file">the path to a file containing Android/Google authentication &amp; authorization data</param>
		public void Load( FileInfo file ) {
			JsonElement json = JsonDocument.Parse( File.ReadAllText( file.FullName ) ).RootElement.GetProperty( "Value" );
			Load( json );
		}
		/// <summary>
		/// Saves data from this <see cref="AndroidCredentials"/> instance to the given file
		/// </summary>
		/// <param name="fileName">the path to a file in which to save Android/Google authentication &amp; authorization data</param>
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
