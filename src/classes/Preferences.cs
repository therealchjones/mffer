using System;
using System.IO;
using System.Text.Json.Serialization;
using System.Xml;

namespace Mffer {
	/// <summary>
	/// Represents an object from a <see cref="PreferenceFile"/>
	/// </summary>
	public class PreferenceObject : GameObject {

	}
	/// <summary>
	/// Represents the settings from an XML-based preferences file
	/// </summary>
	/// <remarks>
	/// Each <see cref="PreferenceFile"/> represents the multiple
	/// key-value pairs present in an XML file, typically from the
	/// <code>shared_prefs</code> directory on an Android device or other
	/// locations corresponding to Unity's <code>PlayerPrefs</code> class.
	/// </remarks>
	/// <seealso
	/// href="https://docs.unity3d.com/ScriptReference/PlayerPrefs.html"/>
	public class PreferenceFile : PreferenceObject {
		/// <summary>
		/// Gets or sets the file from which preferences are obtained
		/// </summary>
		[JsonIgnore]
		FileInfo File { get; set; }
		/// <summary>
		/// Gets the name of the file from which preferences are obtained
		/// </summary>
		public string Name {
			get {
				return File.Name;
			}
		}
		/// <summary>
		/// Gets the full pathname of the file from which preferences are
		/// obtained
		/// </summary>
		public string FullName {
			get {
				return File.FullName;
			}
		}
		/// <summary>
		/// Initializes a new <see cref="PreferenceFile"/> instance
		/// </summary>
		public PreferenceFile() : base() {
		}
		/// <summary>
		/// Initializes a new <see cref="PreferenceFile"/> instance containing the preferences from
		/// <paramref name="fileName"/>
		/// </summary>
		/// <param name="fileName">Name of file from which to load preferences</param>
		public PreferenceFile( string fileName ) : this( new FileInfo( fileName ) ) { }
		/// <summary>
		/// Initializes a new <see cref="PreferenceFile"/> instance containing the preferences from
		/// <paramref name="file"/>
		/// </summary>
		/// <param name="file"><see cref="FileInfo"/> from which to load preferences</param>
		public PreferenceFile( FileInfo file ) : this() {
			File = file;
		}
		/// <summary>
		/// Loads preferences from the given file
		/// </summary>
		/// <param name="file"><see cref="FileInfo"/> to read</param>
		public void Load( FileInfo file ) {
			if ( Value is not null ) {
				throw new Exception( $"Preference file '{Name}' already loaded" );
			}
			if ( !file.Exists ) {
				throw new ArgumentException( $"XML document '{file.FullName}' is not accessible." );
			}
			File = file;
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.Load( file.FullName );
			Load( xmlDocument );
		}
		/// <summary>
		/// Loads preference data from <see cref="PreferenceFile.File"/>
		/// </summary>
		public override void LoadAll() {
			if ( Value is null ) {
				Load( File );
			}
		}
	}
}
