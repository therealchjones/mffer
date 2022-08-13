using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Community.Archives.Apk;
using Community.Archives.Core;

namespace Mffer {
	/// <summary>
	/// Represents an APK file containing game code
	/// </summary>
	/// <remarks>
	/// Software is delivered from the Google Play Store in the form of APK
	/// files (or some variant thereof). The <see cref="ApkFile"/> class
	/// includes properties and methods for accessing those files.
	/// </remarks>
	public class ApkFile : GameObject {
		AndroidManifest Manifest { get; set; }
		FileInfo File { get; set; }
		/// <summary>
		/// Creates a new instance of an <see cref="ApkFile"/> from the given filename
		/// </summary>
		/// <param name="fileName">The name of an APK file</param>
		/// <exception cref="ArgumentNullException">if the filename is null or the empty string</exception>
		/// <exception cref="ArgumentException">if the filename does not represent an accessible file</exception>
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
		/// <summary>
		/// Gets the version of the game included in this <see cref="ApkFile"/>
		/// </summary>
		/// <returns>A string representation of the version of the game to which
		/// this <see cref="ApkFile"/> belongs if that information is
		/// represented in the file, <c>null</c> otherwise</returns>
		public string GetVersion() {
			if ( Manifest is null ) return null;
			else {
				return Manifest.GetValue( "Version" );
			}
		}
		/// <summary>
		/// Gets the Android/Google Play Store package name of which this <see
		/// cref="ApkFile"/> is a part
		/// </summary>
		/// <returns>A string representation of the package name, such as
		/// <c>com.netmarble.mheroesgb</c></returns>
		public string GetPackageName() {
			if ( Manifest is null ) return null;
			else {
				return Manifest.GetValue( "Package" );
			}
		}
		/// <summary>
		/// Asynchronously provides the names of the files in the <see
		/// cref="ApkFile"/>
		/// </summary>
		/// <returns>An <see cref="IAsyncEnumerable{T}"/> list of strings naming
		/// the files within the <see cref="ApkFile"/></returns>
		public async IAsyncEnumerable<string> GetFileNamesAsync() {
			using ( FileStream file = new( File.FullName, FileMode.Open ) ) {
				ApkPackageReader reader = new();
				await foreach ( ArchiveEntry entry in reader.GetFileEntriesAsync( file, "" ) ) {
					yield return entry.Name;
				}
			}
		}
		/// <summary>
		/// Lists the names of the files in the <see
		/// cref="ApkFile"/> on the console
		/// </summary>
		/// <returns>A asynchronous <see cref="Task"/> representing the job listing the files on the console</returns>
		public async Task ListFiles() {
			Console.WriteLine( $"Files included in {File}:" );
			await foreach ( string fileName in GetFileNamesAsync() ) {
				Console.WriteLine( "- " + fileName );
			}
		}
		/// <summary>
		/// Reports whether this <see cref="ApkFile"/> is the "base" file for a
		/// set of split APKs
		/// </summary>
		/// <returns><c>true</c> if the <see cref="ApkFile"/> has a resource
		/// file, <c>false</c> otherwise</returns>
		bool IsBaseApk() {
			Task<bool> resourceCheck = HasResourceFile();
			resourceCheck.Wait();
			if ( resourceCheck.IsCompletedSuccessfully ) {
				return resourceCheck.Result;
			} else {
				return false;
			}
		}
		/// <summary>
		/// Asynchronously checks for whether this <see cref="ApkFile"/>
		/// contains a resource file
		/// </summary>
		/// <returns><c>true</c> if the <see cref="ApkFile"/> contains a
		/// resource file, <c>false</c> otherwise</returns>
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
