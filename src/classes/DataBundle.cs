using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Mffer {
	/// <summary>
	/// Represents a collection of data associated with a single <see cref="Version"/>
	/// </summary>
	/// <remarks>
	/// A <see cref="DataBundle"/> includes all parsed (or parseable) files
	/// from a given <see cref="DeviceDirectory"/>, and therefore all associated
	/// with a given <see cref="Version"/>.
	/// </remarks>
	public class DataBundle : GameObject {
		/// <summary>
		/// Gets or sets the <see cref="DeviceDirectory"/> from which this
		/// <see cref="DataBundle"/> loads its data
		/// </summary>
		VersionDirectory BackingDirectory { get; set; }
		/// <summary>
		/// Sets or gets a dictionary of files containing <see cref="Version"/>
		/// data, indexed by filename
		/// </summary>
		/// <remarks>
		/// <see cref="DataBundle.DataFiles"/> is a link to the <see
		/// cref="DataBundle.BackingDirectory"/>'s <see
		/// cref="VersionDirectory.DataFiles"/> property for convenience.
		/// </remarks>
		[JsonIgnore] // Found in this.BackingDirectory instead
		public Dictionary<string, GameObject> DataFiles {
			get => BackingDirectory.DataFiles;
		}
		/// <summary>
		/// Initializes a new <see cref="DataBundle"/> instance
		/// </summary>
		DataBundle() : base() {

		}
		/// <summary>
		/// Initializes a new <see cref="DataBundle"/> instance based on the
		/// given <see cref="DeviceDirectory"/>
		/// </summary>
		/// <param name="backingDirectory"><see cref="VersionDirectory"/> from
		/// which this <see cref="DataBundle"/> will load its data</param>
		public DataBundle( VersionDirectory backingDirectory ) : this() {
			if ( backingDirectory is null ) {
				throw new ArgumentNullException( "backngDirectory" );
			}
			BackingDirectory = backingDirectory;
		}
		/// <summary>
		/// Loads all available data into the <see cref="DataFiles"/>
		/// </summary>
		public override void LoadAll() {
			BackingDirectory.LoadAll();
		}
	}
}
