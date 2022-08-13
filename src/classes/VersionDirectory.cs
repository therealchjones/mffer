using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace Mffer {
	/// <summary>
	/// Represents a filesystem directory containing data for a single version
	/// of Marvel Future Fight
	/// </summary>
	/// <remarks>
	/// <para><see cref="VersionDirectory"/>s are built from filesystem
	/// directory trees that contain files needed to analyze or explore Marvel
	/// Future Fight.</para>
	/// </remarks>
	public class VersionDirectory : GameObject {
		/// <summary>
		/// <see cref="IAssetReader"/> providing access to the asset data within
		/// this <see cref="VersionDirectory"/>
		/// </summary>
		public IAssetReader assetReader;
		/// <summary>
		/// The files beneath the <see cref="VersionDirectory"/> that will be loaded
		/// </summary>
		static readonly string[] RequiredFiles = {
			"text",
			"localization_en",
			};
		/// <summary>
		/// The filesystem directory in which this <see cref="VersionDirectory"/>
		/// is rooted
		/// </summary>
		[JsonIgnore]
		public DirectoryInfo RootDirectory { get; set; }
		/// <summary>
		/// Gets or sets the individual files containing data to evaluate,
		/// indexed by file name
		/// </summary>
		/// <remarks>
		/// <see cref="DataFiles"/> provides access to the files listed in <see
		/// cref="RequiredFiles"/>.
		/// </remarks>
		public Dictionary<string, GameObject> DataFiles { get; }
		/// <summary>
		/// Gets the full pathname of the <see cref="DeviceDirectory"/>
		/// </summary>
		[JsonIgnore]
		public string FullName { get => RootDirectory.FullName; }
		/// <summary>
		/// Gets the <see cref="Version"/> name for the <see
		/// cref="VersionDirectory"/>
		/// </summary>
		public string VersionName {
			get {
				string version = Utilities.GetVersionName( RootDirectory.Name );
				if ( version is null ) {
					throw new InvalidDataException( $"Directory '{RootDirectory.FullName}' does not have a valid version name." );
				}
				return version;
			}
		}
		/// <summary>
		/// Initializes a new <see cref="VersionDirectory"/> instance
		/// </summary>
		public VersionDirectory() : base() {
			DataFiles = new Dictionary<string, GameObject>();
			assetReader = new AssetsToolsNETReader();
		}
		/// <summary>
		/// Creates an instance of the <see cref="VersionDirectory"/> class from
		/// the given <paramref name="directory"/>
		/// </summary>
		/// <param name="directory">The root of a filesystem directory tree
		/// containing data from a single version of Marvel Future Fight</param>
		public VersionDirectory( DirectoryInfo directory ) : this() {
			if ( directory is null ) {
				throw new ArgumentNullException( "directory" );
			}
			if ( !directory.Exists ) {
				throw new DirectoryNotFoundException( $"Unable to access directory '{directory.FullName}'" );
			}
			RootDirectory = directory;
			foreach ( string filePath in RequiredFiles ) {
				foreach ( FileInfo foundFile in RootDirectory.EnumerateFiles( filePath, SearchOption.AllDirectories ) ) {
					if ( DataFiles.ContainsKey( foundFile.Name ) )
						throw new FileLoadException( $"Multiple files named {filePath} found beneath {directory}." );
					DataFiles.Add( foundFile.Name, assetReader.LoadAssetBundle( foundFile.FullName ) );
				}
			}
		}
		/// <summary>
		/// Loads all available data into the <see cref="DataFiles"/>
		/// </summary>
		public override void LoadAll() {
			foreach ( GameObject entry in DataFiles.Values ) {
				entry.LoadAll();
			}
		}
		/// <summary>
		/// Determines whether the given directory is a proper version directory
		/// </summary>
		/// <remarks>
		/// In order to be a VersionDirectory, the directory must have a version
		/// string name and contain exactly one copy of each of the <see
		/// cref="RequiredFiles"/>
		/// </remarks>
		/// <param name="directory"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException">if the parameter is
		/// null</exception>
		/// <exception cref="ArgumentException">if the directory name is null or
		/// empty, or if the directory does not exist</exception>
		public static bool IsVersionDirectory( DirectoryInfo directory ) {
			//
			if ( directory is null ) {
				throw new ArgumentNullException( "directory" );
			}
			if ( String.IsNullOrEmpty( directory.Name ) ) {
				throw new ArgumentException( "Directory has no name", "directory" );
			}
			if ( !directory.Exists ) {
				throw new ArgumentException( "Directory does not exist", "directory" );
			}
			if ( Utilities.GetVersionName( directory.Name ) is null ) return false;
			foreach ( string filePath in RequiredFiles ) {
				List<FileInfo> files = new();
				foreach ( FileInfo file in directory.EnumerateFiles( filePath, SearchOption.AllDirectories ) ) {
					files.Add( file );
				}
				if ( files.Count != 1 ) return false;
			}
			return true;
		}
	}
}
