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
	/// the <see cref="Game"/>. Data from each <see cref="VersionDirectory"/> are
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
		/// <param name="pathName">The full path name of a <see
		/// cref="VersionDirectory"/> or the parent of multiple version
		/// directories.</param>
		public DataSource( string pathName ) : this() {
			Add( pathName );
		}
		/// <summary>
		/// Adds a directory to the <see cref="DataSource"/>
		/// </summary>
		/// <remarks>The <paramref name="pathName"/> is validated and examined
		/// for appropriate directories to import. The given <paramref
		/// name="pathName"/> may be a <see cref="VersionDirectory"/> or a parent
		/// of one or more <see cref="VersionDirectory"/>s.</remarks>.
		/// <param name="pathName">The full path name of a version directory or
		/// parent of version directories</param>
		public void Add( string pathName ) {
			if ( String.IsNullOrEmpty( pathName ) ) {
				throw new ArgumentNullException( "pathName" );
			}
			if ( !Directory.Exists( pathName ) ) {
				throw new ArgumentException( "Directory does not exist", "pathName" );
			}
			DirectoryInfo directory = new DirectoryInfo( pathName );
			// We check in the following order:
			// - the directory is a DeviceDirectory
			// - the directory is a VersionDirectory
			// - one or more subdirectories of the directory is a DeviceDirectory or a VersionDirectory
			// - if none of the above, error
			List<VersionDirectory> directories = new();
			if ( DeviceDirectory.IsDeviceDirectory( directory ) )
				directories.Add( new DeviceDirectory( directory ) );
			else if ( VersionDirectory.IsVersionDirectory( directory ) ) directories.Add( new VersionDirectory( directory ) );
			else {
				foreach ( DirectoryInfo subdir in directory.EnumerateDirectories() ) {
					if ( DeviceDirectory.IsDeviceDirectory( subdir ) ) directories.Add( new DeviceDirectory( subdir ) );
					else if ( VersionDirectory.IsVersionDirectory( subdir ) ) directories.Add( new VersionDirectory( subdir ) );
				}
			}
			if ( directories.Count == 0 ) throw new ArgumentException( "Directory is neither a version directory nor a parent of a version directories", "pathName" );
			foreach ( VersionDirectory versionDirectory in directories ) {
				string version = versionDirectory.VersionName;
				if ( VersionData.ContainsKey( version ) ) {
					throw new FileLoadException( $"Unable to load version directory '{versionDirectory.FullName}': already loaded assets for version {version}" );
				}
				DataBundle dataBundle = new DataBundle( versionDirectory );
				VersionData.Add( version, dataBundle );
			}
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
