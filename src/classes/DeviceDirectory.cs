using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace Mffer {
	/// <summary>
	/// Represents a filesystem directory copied from an Android device
	/// containing a Marvel Future Fight installation
	/// </summary>
	/// <remarks>
	/// <para><see cref="DeviceDirectory"/>s are built from filesystem directory
	/// trees that are subsets of the filesystem from an Android device upon
	/// which Marvel Future Fight has been installed. Although the <see
	/// cref="VersionDirectory.RootDirectory"/> matches the <c>/</c> directory from the Android
	/// filesystem, not all subdirectories need be included (and most
	/// overlapping mounts are not); typically a <see cref="DeviceDirectory"/>
	/// includes only those subtrees associated with the Marvel Future Fight
	/// installation.</para>
	/// <para>The filesystem directory upon which a <see
	/// cref="DeviceDirectory"/> is based is typically created by the
	/// <c>autoextract</c> program and has a name similar to
	/// <c>mff-device-files-7.0.1-170126-20210423</c>.</para>
	/// </remarks>
	public class DeviceDirectory : VersionDirectory {
		/// <summary>
		/// The files within the <see cref="VersionDirectory.RootDirectory"/> that will be loaded
		/// </summary>
		static readonly string[] FilePaths = {
			"data/Media/0/Android/data/com.netmarble.mherosgb/files/bundle/text",
			"data/Media/0/Android/data/com.netmarble.mherosgb/files/bundle/localization_en",
			"data/data/com.netmarble.mherosgb/shared_prefs/com.netmarble.mherosgb.v2.playerprefs.xml"
			};
		/// <summary>
		/// Initializes a new <see cref="DeviceDirectory"/> instance
		/// </summary>
		DeviceDirectory() : base() {
		}
		/// <summary>
		/// Creates an instance of the <see cref="DeviceDirectory"/> class from
		/// the given <paramref name="directory"/>
		/// </summary>
		/// <param name="directory">The root of a filesystem directory tree
		/// copied from an Android filesystem</param>
		public DeviceDirectory( DirectoryInfo directory ) : this() {
			if ( directory is null ) {
				throw new ArgumentNullException( "directory" );
			}
			if ( !directory.Exists ) {
				throw new DirectoryNotFoundException( $"Unable to access directory '{directory.FullName}'" );
			}
			if ( !IsDeviceDirectory( directory ) ) {
				throw new ArgumentException( $"{directory} is not a valid device directory" );
			}
			RootDirectory = directory;
			foreach ( string filePath in FilePaths ) {
				FileInfo file = new FileInfo( Path.Join( RootDirectory.FullName, filePath ) );
				if ( !file.Exists ) {
					throw new FileNotFoundException( $"Unable to access file '{file.FullName}'" );
				}
				if ( DataFiles.ContainsKey( file.Name ) ) {
					throw new FileLoadException( $"Unable to load '{file.FullName}': another file named '{file.Name}' is already loaded." );
				}
				GameObject dataFile = null;
				if ( file.Name.EndsWith( ".xml", true, null ) ) {
					dataFile = new PreferenceFile( file );
				} else {
					dataFile = assetReader.LoadAssetBundle( file.FullName );
				}
				DataFiles.Add( file.Name, dataFile );
			}
		}
		/// <summary>
		/// Checks whether a given directory is a <see cref="DeviceDirectory"/>
		/// </summary>
		/// <param name="directory"><see cref="DirectoryInfo"/> to check</param>
		/// <returns><c>true</c> if <paramref name="directory"/> exists, has a
		/// name containing a valid version string, and includes the files
		/// listed in DeviceDirectory.FilePaths.
		/// </returns>
		/// <remarks>These criteria are necessary but may not sufficient for ensuring
		/// <paramref name="directory"/> is a valid <see cref="DeviceDirectory"/>;
		/// further checks are needed before assuming any other characteristics of
		/// <see cref="DeviceDirectory"/></remarks>
		public static bool IsDeviceDirectory( DirectoryInfo directory ) {
			if ( !VersionDirectory.IsVersionDirectory( directory ) ) return false;
			foreach ( string filePath in FilePaths ) {
				if ( !File.Exists( Path.Join( directory.FullName, filePath ) ) ) {
					return false;
				}
			}
			return true;
		}
	}
}
