using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Snappier;

namespace Mffer {
	/// <summary>
	/// Represents the data available online
	/// </summary>
	/// <remarks>
	/// <para>Some data are not included in the game files, but are not unique to an
	/// individual user. This class includes the properties and methods used to
	/// obtain and evaluate this information. Of note, though this
	/// <see cref="Component"/> is associated with a given <see cref="Version"/>, the
	/// data may be neither unique to nor uniquely determined by that specific
	/// version. Data is often only availble for the latest version of the game, but
	/// may still be applicable to older versions.</para>
	/// <para>Many initial values are hard coded, just as they are in the game binary.
	/// This additionally means the code may have to change from time to time to work
	/// properly. Program and function flows to identify these within the game code are
	/// included in the mffer documentation.</para>
	/// </remarks>
	public static class NetworkData {
		/// <summary>
		/// The primary URL for all patch (e.g., downloadable) data
		/// </summary>
		/// <remarks>Obtained in libil2cpp.so via PatchSystem.GetBaseUrl()</remarks>
		const string PatchBaseUrl = "http://mheroesgb.gcdn.netmarble.com/mheroesgb/";
		/// <summary>
		/// System-specific URL for all patch (e.g., downloadable) data
		/// </summary>
		/// <remarks>Obtained in libil2cpp.so via PatchSystem.CreateUrl()</remarks>
		const string PatchUrl = PatchBaseUrl + "DIST/Android/";
		/// <summary>
		/// Country code for the download URL
		/// </summary>
		/// <remarks>In mffer, constant string set to "US"</remarks>
		const string CountryCode = "US";
		/// <summary>
		/// Name of the server information file
		/// </summary>
		/// <remarks>Obtained in libil2cpp.so via ServerInfo.GetFileName()</remarks>
		const string ServerFileName = "server_info.txt";
		/// <summary>
		/// The AES encryption key used for file decryption if none has been downloaded
		/// </summary>
		/// <remarks>Obtained in libil2cpp.so via CryptUtil.get_aesKey()</remarks>
		const string AesKey = "!YJKLNGD";
		/// <summary>
		/// The game code used to download data
		/// </summary>
		/// <remarks>Obtained via a long path in Java to base/resources/res/xml/nmconfiguration.xml</remarks>
		const string GameCode = "mherosgb";
		/// <summary>
		/// Base URL used to sign in on Netmarble servers
		/// </summary>
		/// <remarks>Obtained via Java::PlatformDetails.getGateWayUrl()</remarks>
		const string GateWayUrl = "https://apis.netmarble.com";
		/// <summary>
		/// Time zone used in HTTP requests
		/// </summary>
		/// <remarks>Obtained in libil2cpp.so vi
		/// PluginsNetmarbleS.GetTimeZone(). In mffer, constant
		/// "+1:00"</remarks>
		const string TimeZone = "+1:00";
		/// <summary>
		/// <see cref="HttpClient"/> used to send requests to Netmarble
		/// </summary>
		static readonly HttpClient Www = new HttpClient();
		/// <summary>
		/// Random number generator used when needed to format HTTP requests to Netmarble
		/// </summary>
		static readonly Random Rng = new Random();
		/// <summary>
		/// <see cref="JsonDocumentOptions"/> used when parsing Netmarble responses
		/// </summary>
		static readonly JsonDocumentOptions JsonOptions = new JsonDocumentOptions {
			CommentHandling = JsonCommentHandling.Skip,
			AllowTrailingCommas = true
		};
		/// <summary>
		/// Data about Netmarble's servers and the game
		/// </summary>
		static JsonElement ServerInfo = GetServerInfo();
		/// <summary>
		/// Uptime reported in HTTP requests; saved as static property for
		/// strictly increasing value
		/// </summary>
		static float Uptime = 0;
		/// <summary>
		/// Flag to note whether a PreLogin is already underway, to avoid
		/// attempting nested requests
		/// </summary>
		static bool PreLoginInProgress = false;
		/// <summary>
		/// Encryption key used to encrypt messages and decrypt responses
		/// </summary>
		static string PacketKey = null;
		/// <summary>
		/// Encryption key used to decrypt some assets
		/// </summary>
		static string TextKey = "3&?f(7F>";
		/// <summary>
		/// Identifier for Android device
		/// </summary>
		static string CID = null;
		/// <summary>
		/// Identifier for the current login session
		/// </summary>
		static string SessionID = null;
		/// <summary>
		/// Identifier for the current user
		/// </summary>
		static string UserID = null;
		/// <summary>
		/// Type of Android device
		/// </summary>
		static string DeviceModel = null;
		/// <summary>
		/// Identifier for Android device
		/// </summary>
		static string DeviceId = null;
		/// <summary>
		/// Identifier for Android device and user
		/// </summary>
		static string AndroidId = null;
		/// <summary>
		/// Identifier for Android device, user, and application
		/// </summary>
		static string DeviceKey = null;
		/// <summary>
		/// IP address provided in HTTP requests
		/// </summary>
		static string IP = null;
		/// <summary>
		/// String used in HTTP requests
		/// </summary>
		static string AccessToken = null;
		/// <summary>
		/// The most recent version of Marvel Future Fight available
		/// </summary>
		static string LatestVersion = null;
		/// <summary>
		/// Initializes the static <see cref="NetworkData"/> class
		/// </summary>
		static NetworkData() {
		}
		/// <summary>
		/// Obtains info about the Netmarble servers
		/// </summary>
		/// <remarks>
		/// Downloads the Netmarble server data, loads it into <see
		/// cref="NetworkData.ServerInfo"/>, and returns that server info.
		/// Re-implementation of libil2cpp.so's
		/// <c>PacketTransfer.SetServerData()</c> and the following processing
		/// steps.
		/// </remarks>
		/// <returns>A <see cref="JsonElement"/> describing information about
		/// various Netmarble servers for  the latest available version of
		/// Marvel Future Fight</returns>
		static JsonElement GetServerInfo() {
			if ( ServerInfo.ValueKind == JsonValueKind.Undefined ) {
				ServerInfo = GetServerInfoForVersion( GetVersion() );
			}
			return ServerInfo;
		}
		/// <summary>
		/// A <see cref="string"/> representing the latest version of the app.
		/// </summary>
		/// <remarks>
		/// Obtained in libil2cpp.so via plugins:com/seed9/common/Common.java:getBundleVersion() and
		/// varies by version; we obtain this by estimation and successive
		/// checks with the Netmarble servers
		/// </remarks>
		static public string GetVersion() {
			if ( String.IsNullOrWhiteSpace( LatestVersion ) ) LatestVersion = GetLatestAppVersion();
			return LatestVersion;
		}
		/// <summary>
		/// Obtained in libil2cpp.so via PacketTransfer.SetServerDataOK()
		/// </summary>
		/// <param name="version"></param>
		/// <returns></returns>
		/// <exception cref="ApplicationException"></exception>
		static JsonElement GetServerInfoForVersion( string version ) {
			if ( String.IsNullOrEmpty( version ) ) throw new ApplicationException( "Requires valid version string" );
			JsonDocument doc = JsonDocument.Parse( Www.GetStringAsync( GetServerInfoUrl( version ) ).Result, JsonOptions );
			JsonElement info = doc.RootElement.Clone();
			doc.Dispose();
			return info;
		}
		/// <summary>
		/// Returns the URL from which to download Netmarble server information
		/// </summary>
		/// <remarks>
		/// Remake of `libil2cpp.so`'s `ServerInfo.GetRemoteFilePath()`
		/// </remarks>
		/// <param name="appVersion">The version of MFF. If not provided, empty,
		/// or only whitespace, uses <see cref="NetworkData.GetLatestAppVersion()"/></param>
		/// <returns>A <see cref="String"/> representation of the URL</returns>
		static string GetServerInfoUrl( string appVersion = "" ) {
			if ( String.IsNullOrWhiteSpace( appVersion ) ) appVersion = GetVersion();
			return PatchUrl + "v" + appVersion + "/" + ServerFileName + "?p=" + Rng.Next( 0, 0x7fffffff ).ToString();
		}
		/// <summary>
		/// Determines the latest available version of Marvel Future Fight
		/// </summary>
		/// <remarks>Derived from SceneTitle.CheckVersion(); here, the version
		/// is determined by estimating based on the current date then making
		/// successive calls to the Netmarble servers until getting a response
		/// suggesting the correct version.</remarks>
		/// <returns>a <see cref="String"/> identifying the latest available version</returns>
		static string GetLatestAppVersion() {
			string possVersion = GetTimeBasedAppVersion();
			string minVersion;
			string maxVersion;
			List<string> possibleVersions = new();
			while ( !possibleVersions.Contains( possVersion ) ) {
				possibleVersions.Add( possVersion );
				JsonElement serverDetail = GetServerDataForVersion( possVersion ).GetProperty( "detail" );
				minVersion = serverDetail.GetProperty( "min_ver" ).GetString();
				maxVersion = serverDetail.GetProperty( "max_ver" ).GetString();
				if ( new System.Version( minVersion ) > new System.Version( possVersion ) ) possVersion = minVersion;
				else if ( new System.Version( maxVersion ) > new System.Version( possVersion ) ) possVersion = maxVersion;
				else return possVersion;
			}
			throw new ApplicationException( "Unable to determine latest application version." );
		}
		/// <summary>
		/// Obtain an approximate current app version
		/// </summary>
		/// <remarks>Determines a naive first guess for the current version
		/// based on the current date. MFF makes 10 "minor" releases each year
		/// and increases the "major" version every April historically. This
		/// method intentionally underestimates the version.
		/// </remarks>
		/// <returns>a <see cref="String"/> representation of an estimated
		/// current app version</returns>
		static string GetTimeBasedAppVersion() {
			int days = ( DateTimeOffset.UtcNow - new DateTimeOffset( 2014, 5, 1, 0, 0, 0, TimeSpan.Zero ) ).Days;
			int major = days / 365;
			int daysSinceMajor = days - major * 365;
			int minor = Math.Max( daysSinceMajor / 31 - 2, 0 );
			int patch = 0;
			return major.ToString() + '.' + minor.ToString() + '.' + patch.ToString();
		}
		/// <summary>
		/// Returns settings for the Netmarble server in use
		/// </summary>
		/// <remarks>
		/// Re-implementation of libil2cpp.so's ServerInfo.get_data()
		/// </remarks>
		/// <returns></returns>
		static JsonElement GetServerData() {
			return GetServerDataForVersion( GetVersion() );
		}
		/// <summary>
		/// Get settings from Netmarble for the given version of Marvel Future Fight
		/// </summary>
		/// <param name="version">Version name to request</param>
		/// <returns>A server data document corresponding to the given version</returns>
		static JsonElement GetServerDataForVersion( string version ) {
			string selectServerType = GetServerInfoForVersion( version ).GetProperty( "select_server" ).GetProperty( "type" ).GetString();
			JsonElement serverList = GetServerInfoForVersion( version ).GetProperty( "server_list" );
			JsonElement selectedServer = new JsonElement();
			foreach ( JsonElement server in serverList.EnumerateArray() ) {
				if ( server.GetProperty( "type" ).GetString() == selectServerType ) {
					selectedServer = server;
					break;
				}
			}
			if ( !selectedServer.TryGetProperty( "type", out _ ) ) {
				foreach ( JsonElement server in serverList.EnumerateArray() ) {
					selectedServer = server;
					break;
				}
			}
			return selectedServer;
		}
		/// <summary>
		/// Obtains the URL of the primary Netmarble HTTP server to use
		/// </summary>
		/// <remarks>
		/// Re-implementation of libil2cpp.so's ServerInfo.get_URL()
		/// </remarks>
		/// <returns>server url</returns>
		static string GetServerUrl() {
			return GetServerData().GetProperty( "detail" ).GetProperty( "websvr" ).GetString();
		}
		/// <summary>
		/// Get a document from Netmarble servers via HTTP using the given form data
		/// </summary>
		/// <param name="url">URL from which to request the document</param>
		/// <param name="formData">data to send with the request</param>
		/// <returns>A decrypted and decompressed response in <see cref="JsonDocument"/> format</returns>
		static JsonDocument GetWww( string url, Dictionary<string, string> formData ) {
			url = url + "?cKey=" + GetUptime();
			string param = url;
			HttpRequestMessage request = GetWwwRequest( url, formData );
			byte[] responseBytes = Www.Send( request ).Content.ReadAsByteArrayAsync().Result;
			string key = GetAesKey() + GetAesKey();
			byte[] keyBytes = Encoding.UTF8.GetBytes( key );
			byte[] decryptedBytes = CryptUtil.DecryptBytes( responseBytes, keyBytes, keyBytes );
			int decompressedLength = Snappy.GetUncompressedLength( decryptedBytes );
			byte[] decompressedBytes = Snappy.DecompressToArray( decryptedBytes );
			string text = Encoding.UTF8.GetString( decompressedBytes );
			return JsonDocument.Parse( text );
		}
		/// <summary>
		/// Get a document from Netmarble using HTTP using the given parameters
		/// </summary>
		/// <param name="param">Parameters in URL (HTTP GET) format</param>
		/// <returns>A decrypted document in <see cref="JsonDocument"/> format</returns>
		static JsonDocument GetWww( string param ) {
			HttpRequestMessage request = GetWwwRequest( param );
			byte[] responseBytes = Www.Send( request ).Content.ReadAsByteArrayAsync().Result;
			string text = null;
			if ( !param.StartsWith( "http" ) ) {
				text = ResponseDecrypt( responseBytes );
			} else {
				text = Encoding.UTF8.GetString( responseBytes );
			}
			return JsonDocument.Parse( text );
		}
		/// <summary>
		/// Obtains network data via HTTP(S)
		/// </summary>
		/// <remarks>
		/// Reimplimentation and simplification of WWWUtil.Get(),
		/// WWWUtil.GetRountine(), and affiliated methods
		/// </remarks>
		static HttpRequestMessage GetWwwRequest( string parameter, Dictionary<string, string> formData = null ) {
			HttpRequestMessage request = new HttpRequestMessage();
			if ( parameter.StartsWith( "http" ) ) {
				StringBuilder urlBuilder = new StringBuilder( parameter );
				if ( parameter.Contains( '?' ) ) {
					urlBuilder.Append( '&' );
				} else {
					urlBuilder.Append( '?' );
				}
				urlBuilder.Append( "cKey=" );
				urlBuilder.Append( GetUptime() );
				request.RequestUri = new Uri( urlBuilder.ToString() );
				if ( formData != null && formData.Count > 0 ) {
					FormUrlEncodedContent form = new FormUrlEncodedContent( formData );
					request.Method = HttpMethod.Post;
					request.Content = form;
				}
			} else {
				HttpContent form = BuildRequestContent( parameter );
				request.Method = HttpMethod.Post;
				request.RequestUri = new Uri( GetServerUrl() + "FF" );
				request.Content = form;
			}
			// many other settings for data fields/properties within WWWUtil.Get(), dunno what's useful
			// (essentially all the properties defined in game's WWWData)
			//MyData.RestoreAllPreviousData();
			return request;
		}
		/// <summary>
		/// Formats an <see cref="HttpContent"/> object appropriate for an HTTP request to the Netmarble servers based on the provided parameter string
		/// </summary>
		/// <param name="param">an HTTP GET-style parameter string to be formatted into an <see cref="HttpContent"/> object</param>
		/// <returns>an <see cref="HttpContent"/> object including the information from <paramref name="param"/> and additional required data</returns>
		static HttpContent BuildRequestContent( string param ) {
			string completeParam = AddDefaultPacketParameter( param );
			string key = GetPacketKey();
			if ( String.IsNullOrEmpty( key ) )
				key = GetAesKey() + GetAesKey();
			byte[] parameterBytes = Encoding.UTF8.GetBytes( completeParam );
			byte[] encryptedParameter = CryptUtil.AesEncrypt( parameterBytes, key, true );
			byte[] header = MakeWwwHeader( SessionID, encryptedParameter.Length );
			byte[] contents = new byte[header.Length + encryptedParameter.Length];
			Array.Copy( header, contents, header.Length );
			Array.Copy( encryptedParameter, 0, contents, header.Length, encryptedParameter.Length );
			// Using the BestHTTP plugin (https://github.com/magento-hackathon/DashboardVR/blob/b4623ec42af062cf7f40d55e3ce331eba0d3b920/VR/UnityProject/Assets/Plugins/Best%20HTTP%20(Pro)/BestHTTP/Forms/Implementations/HTTPMultiPartForm.cs) to prepare the form
			ByteArrayContent binaryContent = new ByteArrayContent( contents );
			MultipartFormDataContent form = new MultipartFormDataContent();
			form.Add( binaryContent, "bin", "bin" );
			return form;
		}
		/// <summary>
		/// Adds common parameters to web requests
		/// </summary>
		/// <remarks>
		/// Re-implementation of libil2cpp.so's WWWUtil.AddDefaultPacketParameter(param)
		/// </remarks>
		/// <param name="parameter"></param>
		/// <returns></returns>
		static string AddDefaultPacketParameter( string parameter ) {
			StringBuilder parameterBuilder = new StringBuilder( parameter );
			if ( parameter.Contains( '?' ) )
				parameterBuilder.Append( '&' );
			else
				parameterBuilder.Append( '?' );
			parameterBuilder.Append( "uID=" );
			parameterBuilder.Append( GetUserId() );
			parameterBuilder.Append( "&cKey=" + GetUptime() );
			// more appears to be done if the service is Tencent, but not global
			return parameterBuilder.ToString();
		}
		/// <summary>
		/// Create the encrypted "header" for HTTP(S) messages
		/// </summary>
		/// <param name="sessionId"></param>
		/// <param name="size"></param>
		/// <returns>The encrypted "header" as a byte array</returns>
		static byte[] MakeWwwHeader( string sessionId, int size ) {
			if ( String.IsNullOrEmpty( sessionId ) ) sessionId = "0000000000000000000000";
			byte[] sizeBytes = BitConverter.GetBytes( size );
			if ( BitConverter.IsLittleEndian ) Array.Reverse( sizeBytes );
			byte[] sessionBytes = Encoding.ASCII.GetBytes( sessionId );
			byte[] headerBytes = new byte[sessionBytes.Length + 5];
			headerBytes[0] = 0xff;
			Array.Copy( sizeBytes, sizeBytes.Length - 3, headerBytes, 1, 3 );
			headerBytes[4] = (byte)( Rng.Next( 0x100 ) );
			Array.Copy( sessionBytes, 0, headerBytes, 5, sessionBytes.Length );
			return CryptUtil.AesEncrypt( headerBytes, GetAesKey() + GetAesKey() );
		}
		/// <summary>
		/// Sign in to Netmarble servers
		/// </summary>
		/// <remarks>
		/// Re-implementation of SceneTitle.SignIn() and the many methods it calls
		/// </remarks>
		static void SignIn() {
			string url = GateWayUrl + "/mobileauth/v2/players/";
			url += GetPlayerId() + "/deviceKeys/" + GetDeviceKey();
			url += "/accessToken?nmDeviceKey=" + GetAndroidId();
			url += "&countryCode=" + CountryCode;
			url += "&adId=";
			HttpRequestMessage request = new HttpRequestMessage( HttpMethod.Get, url );
			request.Headers.Add( "Accept", "application/json" );
			request.Headers.Add( "GameCode", GetGameCode() );
			HttpResponseMessage response = Www.Send( request );
			JsonDocument result = JsonDocument.Parse( response.Content.ReadAsStringAsync().Result.ToString(), JsonOptions );
			AccessToken = result.RootElement.GetProperty( "resultData" ).GetProperty( "accessToken" ).GetString();
		}
		/// <summary>
		/// Obtains the packet key used for encryption &amp; decryption
		/// </summary>
		/// <remarks>
		/// Re-implementation of libil2cpp.so's CryptUtil.get_packetKey()
		/// </remarks>
		/// <returns></returns>
		static string GetPacketKey() {
			if ( String.IsNullOrEmpty( PacketKey ) ) {
				if ( PreLoginInProgress ) {
					return null;
				} else {
					LoadConstants();
				}
			}
			return PacketKey;
		}
		/// <summary>
		/// Loads the data needed for later Netmarble download requests
		/// </summary>
		static void LoadConstants() {
			PreLogin();
		}
		/// <summary>
		/// Obtains data from Netmarble used for later downloads
		/// </summary>
		/// <remarks>
		/// Reimplementation of libil2cpp.so's SceneTitle.PreLogin() and the
		/// following steps through SceneTitle.PreLoginOK()
		/// </remarks>
		static void PreLogin() {
			PreLoginInProgress = true;
			string url = GetSslUrl() + "PreLogin";
			if ( url.Contains( '?' ) ) {
				url += '&';
			} else {
				url += '?';
			}
			url += "cKey=" + GetUptime();
			Dictionary<string, string> formData = new Dictionary<string, string>();
			formData.Add( "gameToken", GetGameToken() );
			formData.Add( "cID", GetCID() );
			formData.Add( "dID", GetDeviceId() );
			formData.Add( "platform", "android" );
			formData.Add( "ver", LatestVersion );
			formData.Add( "lang", "en" );
			formData.Add( "country", "US" );
			formData.Add( "ds", "1" );
			formData.Add( "client_ip", GetIP() );
			formData.Add( "srvPush", "1" );
			formData.Add( "de", GetDeviceModel() );
			formData.Add( "pan", "0" );
			formData.Add( "pan2", "1" );
			formData.Add( "timeZone", GetTimeZone() );
			FormUrlEncodedContent form = new FormUrlEncodedContent( formData );
			JsonElement desc = new JsonElement();
			int i = 0;
			while ( i++ < 100 ) {
				HttpRequestMessage request = new HttpRequestMessage( HttpMethod.Post, url );
				request.Content = form;
				HttpResponseMessage response = Www.Send( request );
				byte[] responseBytes = response.Content.ReadAsByteArrayAsync().Result;
				string text = ResponseDecrypt( responseBytes );
				using ( JsonDocument result = JsonDocument.Parse( text ) ) {
					if ( result != null
						&& result.RootElement.TryGetProperty( "desc", out JsonElement jsonElement ) ) {
						desc = jsonElement.Clone();
						break;
					}
				}
			}
			UserID = desc.GetProperty( "uID" ).GetUInt64().ToString();
			CID = desc.GetProperty( "cID" ).GetString();
			string cIDEnd = CID.Substring( CID.Length - 8 );
			SessionID = desc.GetProperty( "sessID" ).GetString();
			string sessionIDEnd = null;
			if ( SessionID.Length > 25 ) {
				sessionIDEnd = SessionID.Substring( SessionID.Length - 8 );
				SessionID = SessionID.Substring( 0, SessionID.Length - 8 );
			}
			if ( !String.IsNullOrEmpty( sessionIDEnd ) && !String.IsNullOrEmpty( cIDEnd ) )
				SetPacketKey( sessionIDEnd + cIDEnd );
			SetTextKey( desc.GetProperty( "tek" ).GetString() );
			PreLoginInProgress = false;
		}
		/// <summary>
		/// Get the player ID (another name for the CID)
		/// </summary>
		/// <remarks>
		/// Re-implementation of Java::SessionImpl.getPlayerID()
		/// </remarks>
		/// <returns>the player ID as a string</returns>
		static string GetPlayerId() {
			return GetCID();
		}
		/// <summary>
		/// Get the CID
		/// </summary>
		/// <returns>the CID in string format</returns>
		static string GetCID() {
			if ( String.IsNullOrEmpty( CID ) ) {
				CID = Guid.NewGuid().ToString( "N" ).ToUpper();
			}
			return CID;
		}
		/// <summary>
		/// Get the device model
		/// </summary>
		/// <returns>the device model in string format</returns>
		static string GetDeviceModel() {
			if ( String.IsNullOrEmpty( DeviceModel ) ) {
				DeviceModel = "HTC One";
			}
			return DeviceModel;
		}
		/// <summary>
		/// Get the device ID
		/// </summary>
		/// <returns>The device ID in string format</returns>
		static string GetDeviceId() {
			if ( String.IsNullOrEmpty( DeviceId ) ) {
				string part1 = "0,0," + GetAndroidId();
				string part2 = GetDeviceKey() + GetDeviceModel();
				DeviceId = CryptUtil.GetMD5( part1 ) + "-" + CryptUtil.GetMD5( part2 );
			}
			return DeviceId;
		}
		/// <summary>
		/// Get the device key
		/// </summary>
		/// <remarks>
		/// Reimplementation of Java::SessionImpl.getDeviceKey()
		/// </remarks>
		/// <returns>the device key as a string</returns>
		static string GetDeviceKey() {
			if ( String.IsNullOrEmpty( DeviceKey ) ) {
				DeviceKey = Guid.NewGuid().ToString( "N" ).ToUpper();
			}
			return DeviceKey;
		}
		/// <summary>
		/// Get the Android ID
		/// </summary>
		/// <returns>the Android ID as a string</returns>
		static string GetAndroidId() {
			if ( String.IsNullOrEmpty( AndroidId ) ) {
				long num = ( Rng.Next() << 31 | Rng.Next() );
				AndroidId = num.ToString( "X16" );
			}
			return AndroidId;
		}
		/// <summary>
		/// Get a mock IP from which the request is originating
		/// </summary>
		/// <remarks>In mffer, always returns a constant internal IP</remarks>
		/// <returns>An IP address as a string</returns>
		static string GetIP() {
			if ( String.IsNullOrEmpty( IP ) )
				IP = "10.0.2.16";
			return IP;
		}
		/// <summary>
		/// Decrypts a response from Netmarble servers
		/// </summary>
		/// <param name="encryptedBytes">the encrypted response to decrypt</param>
		/// <returns>the decrypted response as a string</returns>
		static string ResponseDecrypt( byte[] encryptedBytes ) {
			string key = GetPacketKey();
			if ( String.IsNullOrEmpty( key ) ) {
				key = GetAesKey() + GetAesKey();
			}
			byte[] keyBytes = Encoding.UTF8.GetBytes( key );
			return CryptUtil.Decrypt( encryptedBytes, keyBytes, keyBytes );
		}
		/// <summary>
		/// Get the access token, signing in if not already done so
		/// </summary>
		/// <returns>the access token as a string</returns>
		static string GetAccessToken() {
			if ( String.IsNullOrEmpty( AccessToken ) ) {
				SignIn();
			}
			return AccessToken;
		}
		/// <summary>
		/// Get the AES encryption key
		/// </summary>
		/// <returns>the AES key as a string</returns>
		static string GetAesKey() {
			return AesKey;
		}
		/// <summary>
		/// Get the game code
		/// </summary>
		/// <returns>the game code as a string</returns>
		static string GetGameCode() {
			return GameCode;
		}
		/// <summary>
		/// Obtains the game token
		/// </summary>
		/// <remarks>
		/// Re-implementation of PluginsNetmarbleSForAndroid.get_gameToken()
		/// </remarks>
		/// <returns>A string representing the game token</returns>
		static string GetGameToken() {
			return GetAccessToken();
		}
		/// <summary>
		/// Obtains the URL to use for secure Netmarble HTTP requests
		/// </summary>
		/// <remarks>
		/// Re-implementation of libil2cpp.so's ServerInfo.get_SslURL()
		/// </remarks>
		/// <returns>A string representing the secure URL</returns>
		static string GetSslUrl() {
			return GetServerData().GetProperty( "detail" ).GetProperty( "websvr_ssl" ).GetString();
		}
		/// <summary>
		/// Gets the time zone
		/// </summary>
		/// <remarks>Though the game reports the device's time zone, this method
		/// always returns "+1:00"</remarks>
		/// <returns>The time zone as a string</returns>
		static string GetTimeZone() {
			return TimeZone;
		}
		/// <summary>
		/// Simulates the uptime to report
		/// </summary>
		/// <remarks>
		/// Alternate implementation of libil2cpp.so's
		/// Time.get_realtimeSinceStartup()
		/// </remarks>
		/// <returns>a string representation of a double floating point number</returns>
		static string GetUptime() {
			Uptime = Uptime + (Single)( Rng.NextDouble() ) * 37;
			return Uptime.ToString();
		}
		/// <summary>
		/// Simulates the user ID to report
		/// </summary>
		/// <remarks>
		/// Alternate implementation of libil2cpp.so's Global.get_me().userId
		/// </remarks>
		/// <returns>a properly formatted string representation of a user ID
		/// that may or may not exist</returns>
		static string GetUserId() {
			if ( String.IsNullOrEmpty( UserID ) ) {
				if ( PreLoginInProgress ) {
					return null;
				} else {
					PreLogin();
				}
			}
			return UserID;
		}
		/// <summary>
		/// Reports the download version for available Marvel Future Fight
		/// assets
		/// </summary>
		/// <remarks>
		/// The "download version" is usually the same as the game version, but
		/// if there are bug fixes within the game that do not require changes
		/// to the data files, they may be different. The download version is
		/// used to determine the proper URL from which to download the data
		/// files, and it is part of the version string given in the names of
		/// directories created when downloading assets.
		/// </remarks>
		/// <returns>a string representation of the currently available download
		/// version</returns>
		public static string GetDownloadVersion() {
			JsonElement info = GetServerInfoForVersion( GetVersion() );
			if ( info.TryGetProperty( "download_version", out JsonElement versionJson ) && !String.IsNullOrEmpty( versionJson.GetString() ) ) {
				return versionJson.GetString();
			}
			return GetVersion();
		}
		/// <summary>
		/// Sets the <see cref="PacketKey"/> property to the given string or null
		/// </summary>
		/// <param name="protoPacketKey">the string to assign to <see cref="PacketKey"/></param>
		static void SetPacketKey( string protoPacketKey = null ) {
			PacketKey = protoPacketKey;
		}
		/// <summary>
		/// Creates a text key from the given string.
		/// </summary>
		/// <remarks>Based on MFF's CryptUtil.TextKey.set(), called by
		/// PacketTransfer.PreLoginOK(). However, MFF stores the TextKey field
		/// encrypted and XOR'ed; for simplicity, we store it in
		/// plaintext.</remarks>
		/// <param name="text">string from which the text key will be
		/// derived</param>
		static void SetTextKey( string text ) {
			TextKey = text;
		}
		/// <summary>
		/// Obtains the text key used for some asset decryption
		/// </summary>
		///
		/// <returns>the text key as a string</returns>
		static public string GetTextKey() {
			if ( String.IsNullOrEmpty( TextKey ) ) LoadConstants();
			return TextKey;
		}
		/// <summary>
		/// Downloads and parses available data for the given <see cref="Alliance"/>
		/// </summary>
		/// <param name="alliance">the <see cref="Alliance"/> to be updated with
		/// all available data</param>
		/// <returns><c>true</c> if data for the given <see cref="Alliance"/> is
		/// successfully downloaded and parsed into the <see cref="Alliance"/>
		/// instance, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentException"> if <paramref name="alliance"/>
		/// does not have a defined <see cref="Alliance.Id"/> or <see
		/// cref="Alliance.Name"/></exception>
		static public bool TryGetAllianceData( Alliance alliance ) {
			if ( alliance.Id == default ) {
				if ( String.IsNullOrEmpty( alliance.Name ) ) {
					throw new ArgumentException( "Neither alliance name nor alliance ID are given", "alliance" );
				}
				Alliance newAlliance = FindAlliance( alliance.Name );
				if ( newAlliance == null
					|| newAlliance.Id == default )
					return false;
				alliance.Id = newAlliance.Id;
			}
			string param = "ViewAllianceInfo?guID=" + alliance.Id.ToString();
			using ( JsonDocument result = GetWww( param ) ) {
				if ( result == null
					|| !result.RootElement.TryGetProperty( "err", out JsonElement error )
					|| !error.TryGetInt32( out int errorNum )
					|| errorNum != 0 ) {
					return false;
				}
				alliance.Load( result.RootElement );
			}
			return true;
		}
		/// <summary>
		/// Get an <see cref="Alliance"/> by name from the Netmarble servers
		/// </summary>
		/// <param name="allianceName">the name of the sought alliance</param>
		/// <returns>An <see cref="Alliance"/> with the given name, or null if none is found</returns>
		static Alliance FindAlliance( string allianceName ) {
			string param = "SearchAlliance?guildName="
				+ Convert.ToBase64String( Encoding.UTF8.GetBytes( allianceName ) );
			Alliance alliance = new Alliance( allianceName );
			using ( JsonDocument result = GetWww( param ) ) {
				if ( result != null
					&& result.RootElement.TryGetProperty( "err", out JsonElement error )
					&& error.TryGetInt32( out int errorNum )
					&& errorNum == 0 ) {
					alliance.Load( result.RootElement );
				} else {
					alliance = null;
				}
			}
			return alliance;
		}
		/// <summary>
		/// Obtains a list of alliances from Netmarble servers
		/// </summary>
		/// <remarks>
		/// <see cref="FindSuggestedAlliances()"/> obtains a list of 10
		/// alliances from Netmarble servers. The alliances have typically had
		/// a member login within the preceding 24 hours. Repeated calls to
		/// <see cref="FindSuggestedAlliances()"/> often have significant
		/// overlap in the alliances returned. The data provided about each
		/// alliance is incomplete; more complete data for a given
		/// <see cref="Alliance"/> obtained from
		/// <see cref="FindSuggestedAlliances()"/> can be obtained via
		/// <see cref="TryGetAllianceData(Alliance)"/>.
		/// </remarks>
		/// <returns>a <see cref="List{Alliance}"/> of known <see cref="Alliance"/>s</returns>
		static public List<Alliance> FindSuggestedAlliances() {
			const string param = "GetSuggestionAllianceList?lang=";
			List<Alliance> alliances = new List<Alliance>();
			using ( JsonDocument result = GetWww( param ) ) {
				if ( result == null
					|| !result.RootElement.TryGetProperty( "desc", out JsonElement desc )
					|| !desc.TryGetProperty( "sgs", out JsonElement sgs )
					|| sgs.ValueKind != JsonValueKind.Array )
					return alliances;
				foreach ( JsonElement allianceJson in sgs.EnumerateArray() ) {
					Alliance alliance = new Alliance( allianceJson );
					if ( !String.IsNullOrEmpty( alliance.Name )
						|| alliance.Id != default )
						alliances.Add( alliance );
				}
				return alliances;
			}
		}
		/// <summary>
		/// Obtain the downloadable content from Netmarble servers
		/// </summary>
		/// <remarks>
		/// Based on the PatchSystem methods from the game. Downloads the asset
		/// bundle files for the latest available version of the game.
		/// </remarks>
		/// <param name="destDir">directory into which downloaded files
		/// will be written</param>
		static public void DownloadAssets( string destDir ) {
			if ( String.IsNullOrEmpty( destDir ) ) throw new ArgumentNullException( "destination" );
			string version = GetDownloadVersion();
			Dictionary<string, string> formData = new Dictionary<string, string> {
				{ "platform", "0"},
				{ "ver", version }
			};
			string url = GetServerUrl() + "GetVersion";
			JsonElement ver = GetWww( url, formData ).RootElement.GetProperty( "desc" ).GetProperty( "ver" );
			List<DownloadFile> fileList = new List<DownloadFile>();
			List<DownloadFile> downloadList = new List<DownloadFile>();
			List<DownloadFile> retryList = new List<DownloadFile>();
			Directory.CreateDirectory( destDir );
			foreach ( JsonElement item in ver.EnumerateArray() ) {
				DownloadFile file = new DownloadFile( item );
				fileList.Add( file );
			}
			Console.WriteLine( $"Downloading {fileList.Count} files..." );
			DownloadFile[] newList = new DownloadFile[fileList.Count];
			fileList.CopyTo( newList );
			DirectoryInfo tmpDir = Utilities.CreateTempDirectory();
			try {
				do {
					retryList.Clear();
					foreach ( DownloadFile file in newList ) {
						if ( !TryDownloadFile(
								file.RemoteUrl,
								Path.Join( tmpDir.FullName, file.LocalFile ) )
							) {
							retryList.Add( file );
						}
					}
					if ( retryList.Count > 0 ) {
						Console.WriteLine( $"Retrying {retryList.Count} files..." );
						newList = new DownloadFile[retryList.Count];
						retryList.CopyTo( newList );
					}
				} while ( retryList.Count > 0 );
				Console.WriteLine( "Unzipping files..." );
				string assetDirName = "mff-assets-" + version;
				foreach ( DownloadFile file in fileList ) {
					if ( File.Exists( Path.Join( tmpDir.FullName, file.LocalFile ) ) ) {
						ZipFile.ExtractToDirectory( Path.Join( tmpDir.FullName, file.LocalFile ), Path.Join( tmpDir.FullName, assetDirName ), false );
					} else {
						throw new ApplicationException( $"File not found: {Path.Join( tmpDir.FullName, file.LocalFile )}" );
					}
				}
				DateTime fileDate = DateTime.MinValue;
				foreach ( string file in Directory.EnumerateFiles( Path.Join( tmpDir.FullName, assetDirName ) ) ) {
					DateTime newFileDate = File.GetLastWriteTime( file );
					if ( newFileDate > fileDate ) fileDate = newFileDate;
				}
				destDir = Path.Join( destDir, "mff-assets-" + version + "-" + fileDate.ToString( "yyyyMMdd" ) );
				while ( Directory.Exists( destDir ) ) {
					int lastDash = destDir.LastIndexOf( "-" );
					if ( lastDash + 2 <= destDir.Length ) { // the dash is more than 2 spots from the end
						destDir = destDir + "-1";
					} else {
						int suffix = Int32.Parse( destDir.Substring( lastDash + 1 ) );
						destDir = destDir.Substring( 0, lastDash + 1 ) + ( suffix + 1 ).ToString();
					}
				}
				Directory.Move( Path.Join( tmpDir.FullName, assetDirName ), destDir );
			} finally {
				Utilities.RemoveTempDirectory( tmpDir );
			}
			Console.WriteLine( "Done." );
		}
		/// <summary>
		/// Attempts to download a given file from the Netmarble servers
		/// </summary>
		/// <param name="url">URL of the file to download</param>
		/// <param name="file">Local filename of the downloaded file; will be
		/// overwritten if exists.</param>
		/// <returns><c>true</c> if the file is successfully downloaded and
		/// written to disk, <c>false</c> otherwise.</returns>
		static bool TryDownloadFile( string url, string file ) {
			HttpRequestMessage request = new HttpRequestMessage( HttpMethod.Get, url );
			request.Headers.Add( "Range", "bytes=0-" );
			FileInfo newFile = new( file );
			if ( newFile.Exists ) throw new ApplicationException( $"Unable to download; file '{newFile.FullName}' already exists." );
			if ( !newFile.Directory.Exists ) newFile.Directory.Create();
			try {
				using ( FileStream localFile = new FileStream( newFile.FullName, FileMode.Create ) ) {
					Www.Send( request ).Content.CopyTo( localFile, null, new System.Threading.CancellationToken() );
				}
			} catch ( HttpRequestException ) {
				return false;
			}
			return true;
		}
	}
}
// Old code originally determined via RE but no longer used; here for future reference
/*
class NetworkDataOld {
	string accessToken;
	string accessTokenUrl = "https://apis.netmarble.com/mobileauth/v2/";
	string cID;
	string deviceID;
	string deviceKey;
	string deviceModel;
	string gameCode = "mherosgb";
	string getVersionUrl = "https://mherosgb.netmarble.com/NM/GetVersion";
	string packetKey = "OGEIJVOJGTGJJJMF";
	string preLoginUrl = "https://mherosgb.netmarble.com/NM/PreLogin";
	string sessionID;
	string textKey; // demangled text key (maybe the same as tek), for use decrypting TextAssets
	string textKey_; // stored text key, mangled from tek, which must be demangled before use
	string tek = "3&?f(7F>"; // initial key downloaded from PreLogin() ("tek" field in result JSON)
		void CheckAssets() {
			string assetBundleDir = "Z:\\APK\\Marvel Future Fight\\device\\data\\media\\0\\Android\\data\\com.netmarble.mherosgb\\files\\";
			System.Collections.Generic.IEnumerable<string> assetBundleFiles =
				Directory.EnumerateFiles( assetBundleDir, "*", SearchOption.AllDirectories );
			AssetBundle assetBundleFile;
			StringBuilder output = new StringBuilder();
			foreach ( string fileName in assetBundleFiles ) {
				assetBundleFile = AssetBundle.LoadFromFile( fileName );
				string[] assetNames;
				try {
					assetNames = assetBundleFile.GetAllAssetNames();
				} catch {
					continue;
				}
				foreach ( string assetName in assetNames ) {
					//	UnityEngine.Object asset = assetBundleFile.LoadAsset( assetName );
						Type assetType = AssetDatabase.GetMainAssetTypeAtPath( assetName );
						output.AppendLine( assetName + "\t" +
											fileName + "\t" +
											assetType
											);
					output.AppendLine( assetName + "\t" + fileName );
				}
				assetBundleFile.Unload( true );
			}
			File.WriteAllBytes( "Z:\\APK\\Marvel Future Fight\\assets", System.Text.Encoding.UTF8.GetBytes( output.ToString() ) );
		}
	void GetCSVs() {
		var myLoadedAssetBundle = AssetBundle.LoadFromFile( testAssetFile );
		//// Debug.Log( "Loaded asset bundle" + testAssetFile );
		string[] myAssets = myLoadedAssetBundle.GetAllAssetNames();
		for ( int i = 0; i < myAssets.Length; i++ ) {
			if ( myAssets[i].EndsWith( ".csv", StringComparison.OrdinalIgnoreCase ) ) {
				byte[] myBytes;
				UnicodeEncoding encoding = new UnicodeEncoding();
				TextAsset myAsset = (TextAsset)myLoadedAssetBundle.LoadAsset( myAssets[i] );
				//// Debug.Log( "Loaded asset " + myAsset.name);
				string myText = myAsset.text;
				if ( !myText.Contains( "	" ) ) {
					myText.Replace( " ", "+" );
					int buffer = myText.Length % 4;
					if ( buffer != 0 ) {
						buffer = 4 - buffer;
						for ( int j = 0; j < buffer; j++ ) myText = myText + "=";
					}
					myBytes = Convert.FromBase64String( myText );
					myText = encoding.GetString( myBytes );
					//// Debug.Log("Decoded text: " + myText);
					File.WriteAllBytes( testOutputDirectory + myAsset.name + ".csv", myBytes );
				}
			}
		}
				// Debug.Log( "tek: " + tek);
				setTextKey(tek);
				// Debug.Log( "textKey_: " + textKey_ );
				getTextKey();
				// Debug.Log( "textKey: " + textKey );
	}

string AesDecrypt( string encrypted, string key ) {
	if ( encrypted.Contains( "	" ) ) {
		// Debug.Log( "Tab found, not decrypting." );
		return encrypted;
	}
	encrypted = encrypted.Replace( " ", "+" );
	// // Debug.Log( "Before buffering, encrypted length = " + encrypted.Length );
	int buffer = encrypted.Length % 4;
	if ( buffer != 0 ) {
		buffer = 4 - buffer;
		for ( int i = 0; i < buffer; i++ ) {
			encrypted = encrypted + "=";
		}
	}
	// Debug.Log( "After buffering, encrypted length = " + encrypted.Length );
	byte[] encryptedBytes = Convert.FromBase64String( encrypted );
	byte[] rijKey = Encoding.ASCII.GetBytes( key );
	byte[] rijIV = new byte[16];
	byte[] decryptedBytes;
	UnicodeEncoding unicode = new UnicodeEncoding( false, true, false );
	// Debug.Log( "Encrypted bytes: " + unicode.GetString( encryptedBytes ) );
	// Debug.Log( $"Decrypting. Encrypted bytes = {encryptedBytes.Length}, AES Key = { System.Text.Encoding.UTF8.GetString( rijKey )}, AES IV = { System.Text.Encoding.UTF8.GetString( rijIV )}." );
	using ( RijndaelManaged rijAlg = new RijndaelManaged() ) {
		rijAlg.KeySize = rijKey.Length << 3;
		rijAlg.BlockSize = 0x80;
		rijAlg.Mode = (CipherMode)1;
		rijAlg.Padding = (PaddingMode)1;
		rijAlg.Key = rijKey;
		rijAlg.IV = rijIV;
		// Create a decryptor to perform the stream transform.
		ICryptoTransform decryptor = rijAlg.CreateDecryptor( rijKey, rijIV );

		// Create the streams used for decryption.
		using ( MemoryStream msDecrypt = new MemoryStream() ) {
			using ( CryptoStream csDecrypt = new CryptoStream( msDecrypt, decryptor, CryptoStreamMode.Write ) ) {
				csDecrypt.Write( encryptedBytes, 0, encryptedBytes.Length );
				csDecrypt.FlushFinalBlock();
				decryptedBytes = msDecrypt.ToArray();
			}
		}
	}
	UnicodeEncoding unicodeEncoding = new UnicodeEncoding();
	return unicodeEncoding.GetString( decryptedBytes );
}

string getTextKey() {
	// CryptUtil__get_textKey
	if ( String.IsNullOrEmpty( textKey_ ) ) {
		LoadConstants();
	}
	// textKey = CryptUtil__XORDecode( textKey_ )[0x010a02f3]
	// Debug.Log( "textKey_ length: " + textKey_.Length );
	byte[] textBytes = Convert.FromBase64String( textKey_ );
	// Debug.Log( "textKey_ array length: " + textBytes.Length );
	// Debug.Log( "textKey_ byte array: " + Encoding.UTF8.GetString( textBytes ) );
	StringBuilder builder = new StringBuilder( 0x100 );
	uint[] xor_table = { 0, 0, 0, 0 };
	for ( int i = 0; i < textBytes.Length; i++ ) {
		builder.Append( (uint)( (char)( (int)textBytes[i] ^ xor_table[i % xor_table.Length] ) ) );
	}
	textKey = builder.ToString();
	// Debug.Log( "Xor'd textKey length: " + textKey.Length );
	// Debug.Log( "Xor'd textKey: " + textKey );
	string pk = getPacketKey();
	// Debug.Log( "PacketKey: " + pk );
	// textKey = CryptUtil__AESDecrypt( textKey, packetKey )[0x10a055b]
	if ( textKey.Contains( "	" ) ) {
		// Debug.Log( "Encrypted string contains tab, stopping." );
		return null;
	}
	textKey = textKey.Replace( " ", "+" );
	int bufferLength = textKey.Length % 4;
	if ( bufferLength != 0 ) {
		bufferLength = 4 - bufferLength;
		for ( int i = 0; i < bufferLength; i++ ) {
			textKey = textKey + "=";
		}
	}
	// Debug.Log( "Buffered textKey length: " + textKey.Length );
	textBytes = Convert.FromBase64String( textKey );
	// Debug.Log( "Encrypted textKey length: " + textBytes.Length );
	// CryptUtil__AESDecrypt( btSrc, pk, btDst)[0x10a28fa]
	System.Text.ASCIIEncoding asciiEncoding = new System.Text.ASCIIEncoding();
	byte[] keyBytes = asciiEncoding.GetBytes( pk );
	System.Security.Cryptography.RijndaelManaged rijAlg = new System.Security.Cryptography.RijndaelManaged();
	rijAlg.KeySize = pk.Length << 3;
	rijAlg.BlockSize = 0x80;
	rijAlg.Mode = (CipherMode)1;
	rijAlg.Padding = (PaddingMode)1;
	rijAlg.Key = keyBytes;
	rijAlg.IV = new byte[16];
	// Debug.Log( $"Key: {pk}, BlockSize: {rijAlg.BlockSize}, KeySize: {rijAlg.KeySize}" );
	ICryptoTransform decryptor = rijAlg.CreateDecryptor();
	using ( MemoryStream msDecrypt = new MemoryStream() ) {
		using ( CryptoStream csDecrypt = new CryptoStream( msDecrypt, decryptor, CryptoStreamMode.Write ) ) {
			csDecrypt.Write( textBytes, 0, textBytes.Length );
			csDecrypt.FlushFinalBlock();
			textBytes = msDecrypt.ToArray();
		}
	}

	//CryptUtil__ConvertByteArrayToString( byteArray )
	System.Text.UnicodeEncoding unicodeEncoding = new System.Text.UnicodeEncoding();
	textKey = unicodeEncoding.GetString( textBytes );
	return textKey;
}
string GetVersion() {
	//File.WriteAllBytes( responseFile, decrypted );
	//SnappyDecompressor snappyDecompressor = new SnappyDecompressor();
	//byte[] decompressed = snappyDecompressor.Decompress( decrypted, 0, decrypted.Length );
	//GetVersionResponse getVersionResponse = JsonUtility.FromJson<GetVersionResponse>( result );
	//// Debug.Log( "getVersionResponse.err = " + getVersionResponse.err );
	//File.WriteAllBytes( getVersionResponseFile, decompressed);
	//// Debug.Log( "Decompressed as string:" );
	//// Debug.Log( System.Text.Encoding.UTF8.GetString( decompressed, 0, decompressed.Length ) );

	return VersionResponseDecrypt();
}
	void LoadTestAsset() {
		var myLoadedAssetBundle = AssetBundle.LoadFromFile( testAssetFile );
		// Debug.Log( "Loaded asset bundle" + testAssetFile );
		TextAsset myAsset = (TextAsset)myLoadedAssetBundle.LoadAsset( testAssetName, typeof( TextAsset ) );
		// Debug.Log( "Loaded asset " + testAssetName );
		string myText = myAsset.text;
		// Debug.Log( "Raw asset text:" );
		// Debug.Log( myText );
		string myTextDecrypted = AesDecrypt( myText, getTextKey() );
		// Debug.Log( "Decrypted asset text:" );
		// Debug.Log( myTextDecrypted );
	}

string VersionResponseDecrypt() {
	return "To Do";
}
string XORDecode( string encoded ) {
	byte[] encodedBytes = Convert.FromBase64String( encoded );
	byte[] XORtable = { 0, 0, 0, 0 };
	StringBuilder builder = new StringBuilder( 0x100 );
	for ( int counter = 0; counter < encodedBytes.Length; counter++ ) {
		byte XORentry = XORtable[counter % XORtable.Length];
		byte encodedEntry = encodedBytes[counter];
		builder.Append( encodedEntry ^ XORentry );
	}
	return builder.ToString();
}
string XOREncode( string decoded ) {
	// Debug.Log( "XOR encoding " + decoded );
	char[] decodedBytes = decoded.ToCharArray();
	return Convert.ToBase64String( Encoding.UTF8.GetBytes( XORTransform( decodedBytes ) ) );
}
string XORTransform( char[] oldBytes ) {
	byte[] XORtable = { 0, 0, 0, 0 }; // actual details appears to be secret(?), but unnecessary
									  // (see, "System_Runtime_CompilerServices_RuntimeHelpers__InitializeArray"
									  // with "Field$<PrivateImplementationDetails>.$field-95621CF60D17BA910660C8D6362C36C91D509757")
	StringBuilder builder = new StringBuilder( oldBytes.Length );
	for ( int counter = 0; counter < oldBytes.Length; counter++ ) {
		byte XORentry = XORtable[counter % XORtable.Length];
		char oldBytesEntry = oldBytes[counter];
		builder.Append( oldBytesEntry ^ XORentry );
	}
	return builder.ToString();
}
}
*/
