using System.IO;
using System.IO.Compression;
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
		string LocalFile {
			get {
				StringBuilder sb = new StringBuilder( "files/" );
				if ( bundleEach ) sb.Append( "bundle_each/" );
				else sb.Append( "bundle/" );
				sb.Append( hash );
				sb.Append( ".zip" );
				return sb.ToString();
			}
		}
		string RemoteUrl {
			get {
				StringBuilder sb = new StringBuilder( "http://mheroesgb.gcdn.netmarble.com/mheroesgb/DIST/Android/v" );
				sb.Append( NetworkData.AppVersion );
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
		}
		/// <summary>
		/// Downloads the data this <see cref="DownloadFile"/> represents and saves it to the local filesystem
		/// </summary>
		public void Download() {
			NetworkData.TryDownloadFile( RemoteUrl, LocalFile );
		}
		/// <summary>
		/// Unzips a downloaded zip archive to its final destination
		/// </summary>
		public void Unzip() {
			if ( File.Exists( LocalFile ) ) {
				ZipFile.ExtractToDirectory( LocalFile, "./files/bundle/" );
				File.Delete( LocalFile );
			}
		}
	}
}
