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
		/// <summary>
		/// Removes an existing temporary directory and all contents
		/// </summary>
		/// <remarks>For safety's sake, first ensures the given directory is
		/// within the system or user temporary directory; this is ideally used
		/// to remove those previous created by <see
		/// cref="CreateTempDirectory"/>.
		/// </remarks>
		/// <param name="tmpDir"></param>
		/// <exception cref="ApplicationException"></exception>
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
		/// <summary>
		/// Gets a <see cref="Version"/> name from a directory name
		/// </summary>
		/// <remarks>
		/// Version directories should have a name that ends in the name of the
		/// <see cref="Version"/>. A <see cref="Version"/> name should be a
		/// string that starts with a digit. Given the name of a version
		/// directory, this returns the <see cref="Version"/> name, or null
		/// if the name doesn't contain one.
		/// </remarks>
		/// <param name="fullString">The name of a directory</param>
		/// <returns>The name of the <see cref="Version"/></returns>
		public static string GetVersionName( string fullString ) {
			char[] digits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
			int firstDigit = fullString.IndexOfAny( digits );
			if ( firstDigit == -1 ) return null;
			return fullString.Substring( firstDigit );
		}
	}

}
