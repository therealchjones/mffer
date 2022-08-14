using System.Text;
using System.Text.Json;

namespace Mffer {
	/// <summary>
	/// Represents a file to download from Netmarble
	/// </summary>
	public class DownloadFile : GameObject {
		/// <summary>
		/// The "check_hash" value from the JSON description of the file
		/// </summary>
		int checkHashType;
		/// <summary>
		/// The "download_location" value from the JSON description of the file
		/// </summary>
		int downloadLocation = 1;
		/// <summary>
		/// The name of the final file (after download and decompression)
		/// </summary>
		string name;
		/// <summary>
		/// The expected size of the file
		/// </summary>
		ulong size;
		/// <summary>
		/// A hash of the file
		/// </summary>
		string hash;
		/// <summary>
		/// The "service" value from the JSON description of the file
		/// </summary>
		int serviceType;
		/// <summary>
		/// The "simple_hash" value from the JSON description of the file
		/// </summary>
		string simpleHash;
		/// <summary>
		/// The expected size of the compressed version of the file to download
		/// </summary>
		ulong zipSize;
		/// <summary>
		/// Whether this file is meant for the "bundle_each" directory
		/// </summary>
		bool bundleEach;
		/// <summary>
		/// The relative path destination of the downloaded compressed file
		/// </summary>
		internal string LocalFile { get; set; }
		/// <summary>
		/// The remote address of the compressed file to download
		/// </summary>
		internal string RemoteUrl {
			get {
				StringBuilder sb = new StringBuilder( "http://mheroesgb.gcdn.netmarble.com/mheroesgb/DIST/Android/v" );
				sb.Append( NetworkData.GetDownloadVersion() );
				if ( bundleEach ) sb.Append( "/BundleEach/" );
				else sb.Append( "/Bundle/" );
				sb.Append( hash );
				sb.Append( ".zip" );
				return sb.ToString();
			}
		}
		/// <summary>
		/// Creates a new instance of a <see cref="DownloadFile"/>
		/// </summary>
		DownloadFile() : base() {

		}
		/// <summary>
		/// Creates an instance of the <see cref="DownloadFile"/> class and loads data from the given <see cref="JsonElement"/>
		/// </summary>
		/// <param name="item"><see cref="JsonElement"/> including data about a <see cref="DownloadFile"/></param>
		public DownloadFile( JsonElement item ) : this() {
			Load( item );
		}
		/// <summary>
		/// Parses JSON into this <see cref="DownloadFile"/>'s members
		/// </summary>
		/// <param name="item">JSON object including data about a <see cref="DownloadFile"/></param>
		public override void Load( JsonElement item ) {
			checkHashType = item.GetProperty( "check_hash" ).GetInt32();
			downloadLocation = item.GetProperty( "download_location" ).GetInt32();
			name = item.GetProperty( "file" ).GetString();
			size = item.GetProperty( "file_size" ).GetUInt64();
			hash = item.GetProperty( "hash" ).GetString();
			serviceType = item.GetProperty( "service" ).GetInt32();
			simpleHash = item.GetProperty( "simple_hash" ).GetString();
			zipSize = item.GetProperty( "zip_size" ).GetUInt64();
			bundleEach = false;

			StringBuilder fileName = new();
			if ( bundleEach ) fileName.Append( "bundle_each/" );
			else fileName.Append( "bundle/" );
			fileName.Append( hash );
			fileName.Append( ".zip" );
			LocalFile = fileName.ToString();
		}
	}
}
