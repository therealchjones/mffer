using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using Community.Archives.Apk;
using Community.Archives.Core;

namespace Mffer {
	/// <summary>
	/// Represents the data within an <see cref="ApkFile"/>'s manifest
	/// </summary>
	internal class AndroidManifest : GameObject {
		/// <summary>
		/// Interface allowing access to the <see cref="AndroidManifest"/>'s data
		/// </summary>
		IArchiveReader.ArchiveMetaData data;
		/// <summary>
		/// Dictionary cataloging the fields of the <see cref="AndroidManifest"/>
		/// </summary>
		public Dictionary<string, string> Fields { get; set; }
		/// <summary>
		/// XML representation of the <see cref="AndroidManifest"/>
		/// </summary>
		public XDocument Contents;
		/// <summary>
		/// Creates a new instance of an <see cref="AndroidManifest"/> from the given file
		/// </summary>
		/// <param name="fileName">the name of a manifest file from within an APK</param>
		/// <exception cref="ArgumentNullException">if the given filename is null or the empty string</exception>
		/// <exception cref="FileNotFoundException">if the file cannot be found or accessed</exception>
		/// <exception cref="ApplicationException">if the file cannot be read and parsed successfully</exception>
		public AndroidManifest( string fileName ) {
			if ( String.IsNullOrEmpty( fileName ) ) throw new ArgumentNullException( nameof( fileName ) );
			if ( !System.IO.File.Exists( fileName ) ) throw new FileNotFoundException( $"File '{fileName}' not found." );
			using ( FileStream stream = new FileStream( fileName, FileMode.Open ) ) {
				ApkPackageReader reader = new ApkPackageReader();
				Task<IArchiveReader.ArchiveMetaData> dataTask = reader.GetMetaDataAsync( stream );
				dataTask.Wait();
				if ( dataTask.IsCompletedSuccessfully ) {
					data = dataTask.Result;
				} else {
					throw new ApplicationException( $"Unable to read manifest of file '{fileName}'" );
				}
				stream.Position = 0;
				Task<XDocument> contentsTask = GetXmlFile( stream, "AndroidManifest.xml", reader );
				contentsTask.Wait();
				if ( contentsTask.IsCompletedSuccessfully ) {
					Contents = contentsTask.Result.Document;
				} else {
					throw new ApplicationException( $"Unable to get full contents of manifest of file '{fileName}'" );
				}
			}
			Fields = new();
			LoadFields();
		}
		/// <summary>
		/// Loads the readable fields from the manifest file into the <see cref="Fields"/> dictionary
		/// </summary>
		void LoadFields() {
			Fields["Package"] = data.Package;
			Fields["Version"] = data.Version;
			Fields["Architecture"] = data.Architecture;
			Fields["Description"] = data.Description;
			foreach ( string key in data.AllFields.Keys ) {
				Fields[key] = data.AllFields[key];
			}
		}
		/// <summary>
		/// Reads the requested file from a <see cref="Stream"/> using the given <see cref="ApkPackageReader"/>
		/// </summary>
		/// <param name="stream">The <see cref="Stream"/> containing the data from an APK file</param>
		/// <param name="fileName">The name of the file within the APK file to be read</param>
		/// <param name="reader">The <see cref="ApkPackageReader"/> used to access the stream</param>
		/// <returns>A <see cref="Task"/> representing the job of obtaining an <see cref="XDocument"/> version of the file</returns>
		/// <exception cref="ApplicationException">if unable to access the file</exception>
		async Task<XDocument> GetXmlFile( Stream stream, string fileName, ApkPackageReader reader ) {
			await foreach ( ArchiveEntry entry in reader.GetFileEntriesAsync( stream, $"^{fileName}$" ) ) {
				XDocument document = null;
				using ( Stream fileStream = entry.Content ) {
					AndroidBinaryXmlReader xmlReader = new();
					document = await xmlReader.ReadAsync( fileStream );
				}
				return document;
			}
			throw new ApplicationException( $"Unable to access archive file '{fileName}'" );
		}
		/// <summary>
		/// Override of <see cref="GameObject.GetValue(string)"/> that returns the value of the given field from this <see cref="AndroidManifest"/>
		/// </summary>
		/// <param name="key">The name of the field to search</param>
		/// <returns>The value of the requested field if it exists, or the same as <see cref="GameObject.GetValue(string)"/> otherwise</returns>
		public override string GetValue( string key = null ) {
			if ( !String.IsNullOrEmpty( key ) ) {
				if ( Fields.ContainsKey( key ) ) return Fields[key];
			}
			return base.GetValue( key );
		}
	}
}
