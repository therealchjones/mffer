using System;
using System.IO;
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
	/// "/Users/chjones/Development/Marvel Future Fight/device-files/MFF-device-6.9.0/data/data/com.netmarble.mherosgb/shared_prefs/com.netmarble.mherosgb.v2.playerprefs.xml"
	public class PreferenceFile : PreferenceObject {
		/// <summary>
		/// Gets or sets the filename from which preferences are obtained
		/// </summary>
		public string Name { get; set; }
		/// <summary>
		/// Initializes a new <see cref="PreferenceFile"/> instance
		/// </summary>
		public PreferenceFile() : base() {
			Name = null;
		}
		/// <summary>
		/// Initializes a new <see cref="PreferenceFile"/> instance containing the preferences from
		/// <paramref name="fileName"/>
		/// </summary>
		/// <param name="fileName">Name of file from which to load preferences</param>
		public PreferenceFile( string fileName ) : this() {
			LoadFromFile( fileName );
		}
		/// <summary>
		/// Loads preferences from the given file into the
		/// <see cref="PreferenceFile"/> object
		/// </summary>
		/// <param name="fileName">path name of the file to read</param>
		public void LoadFromFile( string fileName ) {
			if ( !File.Exists( fileName ) ) {
				throw new ArgumentException( $"XML document '{fileName}' is not accessible." );
			}
			FileInfo file = new FileInfo( fileName );
			Name = file.Name;
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.Load( file.FullName );
			LoadXml( xmlDocument );
		}
	}
}
