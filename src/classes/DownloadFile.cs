using System.Text;
using System.Text.Json;

namespace Mffer {
	/// <summary>
	/// Represents a file to download from Netmarble
	/// </summary>
	public class DownloadFile : GameObject {
		int checkHashType;
		int downloadLocation = 1;
		string name;
		ulong size;
		string hash;
		int serviceType;
		string simpleHash;
		ulong zipSize;
		bool bundleEach;
		internal string LocalFile { get; set; }
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
