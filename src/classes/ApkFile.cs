using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Community.Archives.Apk;
using Community.Archives.Core;

namespace Mffer {
	public class ApkFile : GameObject {
		AndroidManifest Manifest { get; set; }
		FileInfo File { get; set; }
		public ApkFile( string fileName ) {
			if ( String.IsNullOrEmpty( fileName ) ) throw new ArgumentNullException( nameof( fileName ) );
			File = new FileInfo( fileName );
			if ( !File.Exists ) throw new ArgumentException( $"Unable to access file '{fileName}'" );
			if ( IsBaseApk() ) {
				Manifest = new AndroidManifest( File.FullName );
			} else {
				Manifest = null;
			}
		}
		public string GetVersion() {
			if ( Manifest is null ) return null;
			else {
				return Manifest.GetValue( "Version" );
			}
		}
		public string GetPackageName() {
			if ( Manifest is null ) return null;
			else {
				return Manifest.GetValue( "Package" );
			}
		}
		public async IAsyncEnumerable<string> GetFileNamesAsync() {
			using ( FileStream file = new( File.FullName, FileMode.Open ) ) {
				ApkPackageReader reader = new();
				await foreach ( ArchiveEntry entry in reader.GetFileEntriesAsync( file, "" ) ) {
					yield return entry.Name;
				}
			}
		}
		public async Task ListFiles() {
			Console.WriteLine( $"Files included in {File}:" );
			await foreach ( string fileName in GetFileNamesAsync() ) {
				Console.WriteLine( "- " + fileName );
			}
		}
		bool IsBaseApk() {
			Task<bool> resourceCheck = HasResourceFile();
			resourceCheck.Wait();
			if ( resourceCheck.IsCompletedSuccessfully ) {
				return resourceCheck.Result;
			} else {
				return false;
			}
		}
		async Task<bool> HasResourceFile() {
			using ( FileStream file = new( File.FullName, FileMode.Open ) ) {
				ApkPackageReader reader = new();
				await foreach ( ArchiveEntry entry in reader.GetFileEntriesAsync( file, "^resources.arsc$" ) ) {
					return true;
				}
			}
			return false;
		}
	}
}
