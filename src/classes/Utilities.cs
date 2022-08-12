using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Mffer {
	/// <summary>
	/// Static methods for general utilization
	/// </summary>
	public static class Utilities {
		/// <summary>
		/// Calculates a hashed version of a string
		/// </summary>
		/// <remarks>
		/// Creates a base 64 representation of the given string, creates
		/// an MD5 hash of that base 64 representation, and returns the bytestring
		/// of that hash; the result is a 32-character capitalized hexadecimal string.
		/// This is used in multiple places in Marvel Future Fight, and is included
		/// here for testing but is generally not reversible.
		/// </remarks>
		/// <param name="toHash">String to hash</param>
		/// <returns>hashed version of the string</returns>
		public static string HashString( string toHash ) {
			StringBuilder stringBuilder = new StringBuilder();
			string base64string = Convert.ToBase64String( Encoding.UTF8.GetBytes( toHash ) );
			using ( MD5 md5hash = MD5.Create() ) {
				byte[] hashBytes = md5hash.ComputeHash( Encoding.UTF8.GetBytes( base64string ) );
				for ( int i = 0; i < hashBytes.Length; i++ ) {
					stringBuilder.Append( hashBytes[i].ToString( "X2" ) );
				}
			}
			return stringBuilder.ToString();
		}
		/// <summary>
		/// Creates a new temporary directory
		/// </summary>
		/// <remarks>Attempts to create a directory within the system or user's
		/// temporary directory with an unguessable directory name. This is not
		/// expected to be cryptographically secure and could theoretically
		/// impose a race condition between determining the diirectory name and
		/// creating the directory.
		/// </remarks>
		/// <returns><see cref="DirectoryInfo"/> representing the new temporary
		/// directory</returns>
		internal static DirectoryInfo CreateTempDirectory() {
			string mainTempDir = Path.GetTempPath();
			string tmpDirName, tmpDirPath;
			do {
				tmpDirName = Path.GetRandomFileName();
				tmpDirPath = Path.Join( mainTempDir, tmpDirName );
			} while ( Directory.Exists( tmpDirName ) || File.Exists( tmpDirName ) );
			return Directory.CreateDirectory( tmpDirPath );
		}
		internal static void RemoveTempDirectory( DirectoryInfo tmpDir ) {
			string mainTempDir = Path.GetTempPath();
			if ( !tmpDir.FullName.StartsWith( mainTempDir ) ) {
				throw new ApplicationException( "Can only remove temporary directories" );
			}
			foreach ( FileInfo file in tmpDir.EnumerateFiles() ) {
				file.Delete();
			}
			foreach ( DirectoryInfo dir in tmpDir.EnumerateDirectories() ) {
				RemoveTempDirectory( dir );
			}
			tmpDir.Delete();
		}
	}
}
