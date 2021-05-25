using System;
using System.Collections.Generic;
using System.IO;

namespace Mffer {
	/// <summary>
	/// Represents a filesystem directory copied from an Android device
	/// containing a Marvel Future Fight installation
	/// </summary>
	/// <remarks>
	/// <para><see cref="DeviceDirectory"/>s are objects built from
	/// filesystem directory trees that are subsets of the filesystem from
	/// an Android device upon which Marvel Future Fight has been installed.
	/// Although the <see cref="RootDirectory"/> matches the <c>/</c>
	/// directory from the Android filesystem, not all subdirectories need
	/// be included (and most overlapping mounts are not); typically a
	/// <see cref="DeviceDirectory"/> includes only those subtrees associated
	/// with the Marvel Future Fight installation.</para>
	/// <para>The filesystem directory upon which a
	/// <see cref="DeviceDirectory"/> is based is typically created by the
	/// <c>autoextract</c> program and has a name similar to
	/// <c>mff-device-files-7.0.1-170126-20210423</c>, ending in a
	/// version name.</para>
	/// </remarks>
	public class DeviceDirectory : GameObject {
		/// <summary>
		/// The files within the <see cref="RootDirectory"/> that will be loaded
		/// </summary>
		static readonly string[] FilePaths = {
			"data/Media/0/Android/data/com.netmarble.mherosgb/files/bundle/text",
			"data/Media/0/Android/data/com.netmarble.mherosgb/files/bundle/localization_en",
			"data/data/com.netmarble.mherosgb/shared_prefs/com.netmarble.mherosgb.v2.playerprefs.xml"
			};
		/// <summary>
		/// The filesystem directory in which this <see cref="DeviceDirectory"/> is rooted
		/// </summary>
		DirectoryInfo RootDirectory { get; set; }
		/// <summary>
		/// Gets or sets the individual files containing assets, indexed by file name
		/// </summary>
		/// <remarks>
		/// <see cref="AssetFiles"/> provides access to the files listed in
		/// <see cref="FilePaths"/>.
		/// </remarks>
		public Dictionary<string, GameObject> AssetFiles { get; }
		/// <summary>
		/// Gets the <see cref="Version"/> name for the <see cref="DeviceDirectory"/>
		/// </summary>
		public string VersionName {
			get {
				string version = GetVersionName( RootDirectory.Name );
				if ( version is null ) {
					throw new InvalidDataException( $"Directory '{RootDirectory.FullName}' does not have a valid version name." );
				}
				return version;
			}
		}
		/// <summary>
		/// Gets the name of the <see cref="DeviceDirectory"/>
		/// </summary>
		public string Name { get => RootDirectory.Name; }
		/// <summary>
		/// Gets the full pathname of the <see cref="DeviceDirectory"/>
		/// </summary>
		public string FullName { get => RootDirectory.FullName; }
		/// <summary>
		/// Initializes a new <see cref="DeviceDirectory"/> instance
		/// </summary>
		DeviceDirectory() : base() {
			AssetFiles = new Dictionary<string, GameObject>();
		}
		/// <summary>
		/// Creates an instance of the <see cref="DeviceDirectory"/> class
		/// from the given <paramref name="directory"/>
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
				FileInfo file = new FileInfo( directory + "/" + filePath );
				GameObject assetFile = null;
				if ( !file.Exists ) {
					throw new FileNotFoundException( $"Unable to access file '{file.FullName}'" );
				}
				if ( file.Name.EndsWith( ".xml", true, null ) ) {
					assetFile = new PreferenceFile( file );
				} else {
					assetFile = new AssetFile( file );
				}
				if ( AssetFiles.ContainsKey( file.Name ) ) {
					throw new FileLoadException( $"Unable to load '{file.FullName}': another file named '{file.Name}' is already loaded." );
				}
				AssetFiles.Add( file.Name, assetFile );
			}
		}
		/// <summary>
		/// Loads all available data into the <see cref="AssetFiles"/>
		/// </summary>
		public void LoadAllAssets() {
			foreach ( AssetFile assetFile in AssetFiles.Values ) {
				assetFile.LoadAll();
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
			if ( directory is null ) {
				throw new ArgumentNullException( "directory" );
			}
			if ( directory.Name is null ) {
				throw new ArgumentException( "Directory has no name", "directory" );
			}
			if ( !directory.Exists ) {
				throw new ArgumentException( "Directory does not exist", "directory" );
			}
			string version = GetVersionName( directory.Name );
			if ( version is null ) return false;
			foreach ( string filePath in FilePaths ) {
				if ( !File.Exists( directory.FullName + "/" + filePath ) ) {
					return false;
				}
			}
			return true;
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
		static string GetVersionName( string fullString ) {
			char[] digits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
			int firstDigit = fullString.IndexOfAny( digits );
			if ( firstDigit == -1 ) return null;
			return fullString.Substring( firstDigit );
		}
	}
}
