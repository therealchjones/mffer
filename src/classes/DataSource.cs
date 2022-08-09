using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Mffer {
	/// <summary>
	/// Provides a store of <see cref="Game"/> data
	/// </summary>
	/// <remarks>
	/// <para>All filesystem interaction (with the possible exception of
	/// validating command-line arguments) should be performed via this class. A
	/// <see cref="DataSource"/> is built from <see cref="DataBundle"/>
	/// objects that are each associated with a given version of
	/// the <see cref="Game"/>. Data from each <see cref="DeviceDirectory"/> are
	/// loaded into the <see cref="DataBundle"/> associated with the same
	/// version when requested by the a <see cref="Game"/> instance.</para>
	/// <para>The <see cref="DataSource"/> class includes these definitions and
	/// methods to build instances of them and access their data.</para>
	/// </remarks>
	public class DataSource : GameObject {
		/// <summary>
		/// Gets or sets the imported data, indexed by version name
		/// </summary>
		public Dictionary<string, DataBundle> VersionData { get; set; }
		/// <summary>
		/// Initializes a new <see cref="DataSource"/> instance
		/// </summary>
		DataSource() : base() {
			VersionData = new Dictionary<string, DataBundle>();
		}
		/// <summary>
		/// Initializes a new <see cref="DataSource"/> instance containing a
		/// directory
		/// </summary>
		/// <remarks>The <paramref name="pathName"/> is validated and examined
		/// for appropriate directories to import.</remarks>
		/// <seealso cref="Add(string)"/>
		/// <param name="pathName">The full path name of a device directory or
		/// parent of device directories</param>
		public DataSource( string pathName ) : this() {
			Add( pathName );
		}
		/// <summary>
		/// Adds a directory to the <see cref="DataSource"/>
		/// </summary>
		/// <remarks>The <paramref name="pathName"/> is validated and examined
		/// for appropriate directories to import. The given <paramref
		/// name="pathName"/> may be a <see cref="DeviceDirectory"/> or a parent
		/// of one or more <see cref="DeviceDirectory"/>s.</remarks>.
		/// <param name="pathName">The full path name of a device directory or
		/// parent of device directories</param>
		public void Add( string pathName ) {
			if ( String.IsNullOrEmpty( pathName ) ) {
				throw new ArgumentNullException( "pathName" );
			}
			if ( !Directory.Exists( pathName ) ) {
				throw new ArgumentException( "Directory does not exist", "pathName" );
			}
			DirectoryInfo directory = new DirectoryInfo( pathName );
			if ( !DeviceDirectory.IsDeviceDirectory( directory ) ) {
				List<DirectoryInfo> subdirs = directory.GetDirectories().ToList();
				List<DirectoryInfo> deviceDirs = new List<DirectoryInfo>();
				foreach ( DirectoryInfo subdir in subdirs ) {
					if ( DeviceDirectory.IsDeviceDirectory( subdir ) ) {
						deviceDirs.Add( subdir );
						Add( subdir.FullName );
					}
				}
				if ( deviceDirs.Count == 0 ) {
					throw new ArgumentException( "Directory is neither a device directory nor a parent of a device directory", "pathName" );
				}
				return;
			}
			DeviceDirectory deviceDirectory = new DeviceDirectory( directory );
			string version = deviceDirectory.VersionName;
			if ( VersionData.ContainsKey( version ) ) {
				throw new FileLoadException( $"Unable to load device directory '{deviceDirectory.FullName}': already loaded assets for version {version}" );
			}
			DataBundle dataBundle = new DataBundle( deviceDirectory );
			VersionData.Add( version, dataBundle );
		}
		/// <summary>
		/// Creates a list of the identified version names
		/// </summary>
		/// <returns>The list of loaded version names</returns>
		public List<string> GetVersionNames() {
			return VersionData.Keys.ToList();
		}
		/// <summary>
		/// Provides the <see cref="DataBundle"/> associated with the given version name
		/// </summary>
		/// <param name="versionName">Name of the version</param>
		/// <returns>The <see cref="DataBundle"/> for version <paramref name="versionName"/></returns>
		public DataBundle GetData( string versionName ) {
			if ( String.IsNullOrEmpty( versionName ) ) {
				throw new ArgumentNullException( "versionName", "The version name must not be empty." );
			}
			if ( !VersionData.ContainsKey( versionName ) ) {
				throw new ArgumentException( $"No data loaded for version {versionName}", "versionName" );
			}
			return VersionData[versionName];
		}
	}
}
