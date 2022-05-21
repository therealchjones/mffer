using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using Community.Archives.Apk;
using Community.Archives.Core;

namespace Mffer {
	internal class AndroidManifest : GameObject {
		IArchiveReader.ArchiveMetaData data;
		public Dictionary<string, string> Fields { get; set; }
		public XDocument Contents;
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
		void LoadFields() {
			Fields["Package"] = data.Package;
			Fields["Version"] = data.Version;
			Fields["Architecture"] = data.Architecture;
			Fields["Description"] = data.Description;
			foreach ( string key in data.AllFields.Keys ) {
				Fields[key] = data.AllFields[key];
			}
		}
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
		public override string GetValue( string key = null ) {
			if ( !String.IsNullOrEmpty( key ) ) {
				if ( Fields.ContainsKey( key ) ) return Fields[key];
			}
			return base.GetValue( key );
		}
	}
}
