using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Snappy.Sharp;

namespace Mffer {
	/// <summary>
	/// Represents the static data available online
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
		// Obtained in libil2cpp.so via PatchSystem.GetBaseUrl()
		const string PatchBaseUrl = "http://mheroesgb.gcdn.netmarble.com/mheroesgb/";
		// Obtained in libil2cpp.so via PatchSystem.CreateUrl()
		const string PatchUrl = PatchBaseUrl + "DIST/Android/";
		const string CountryCode = "US";
		// Obtained in libil2cpp.so via plugins: com/seed9/common/Common.java:getBundleVersion()
		// varies by version
		const string BundleVersion = "7.7.0";
		// Obtained in libil2cpp.so via ServerInfo.GetFileName()
		const string ServerFileName = "server_info.txt";
		// Obtained in libil2cpp.so via CryptUtil.get_aesKey()
		const string AesKey = "!YJKLNGD";
		// Obtained via a long path in Java to base/resources/res/xml/nmconfiguration.xml
		const string GameCode = "mherosgb";
		// Obtained via Java::PlatformDetails.getGateWayUrl()
		const string GateWayUrl = "https://apis.netmarble.com";
		// Obtained in libil2cpp.so vi PluginsNetmarbleS.GetTimeZone()
		const string TimeZone = "+1:00";
		static readonly HttpClient Www = new HttpClient();
		static readonly Random Rng = new Random();
		static readonly JsonDocumentOptions JsonOptions = new JsonDocumentOptions {
			CommentHandling = JsonCommentHandling.Skip,
			AllowTrailingCommas = true
		};
		static JsonDocument ServerInfo = null;
		static float Uptime = 0;
		static bool PreLoginInProgress = false;
		static string PacketKey = null;
		static string CID = null;
		static string SessionID = null;
		static string UserID = null;
		public static string DeviceModel = null;
		static string DeviceId = null;
		static string AndroidId = null;
		static string DeviceKey = null;
		static string IP = null;
		static string AccessToken = null;
		/// <summary>
		/// Initializes the static <see cref="NetworkData"/> class
		/// </summary>
		static NetworkData() {
		}
		static public List<Alliance> CheckProspectiveAlliances( List<Alliance> alliances ) {
			List<Alliance> newList = new List<Alliance>();
			DateTimeOffset now = DateTimeOffset.Now;
			TimeSpan sinceLastCheck;
			foreach ( Alliance alliance in alliances ) {
				if ( alliance.LastUpdateTime != default ) {
					sinceLastCheck = now - alliance.LastUpdateTime;
				} else {
					sinceLastCheck = now - now;
				}
				UpdateAlliance( alliance );
				if ( alliance.WeeklyExperience != 0
					|| alliance.Players.Count >= alliance.MaxMembers )
					continue;
				if ( alliance.LastLoginTime == default
					|| now - alliance.LastLoginTime > sinceLastCheck )
					newList.Add( alliance );
			}
			return newList;
		}
		static void UpdateAlliance( Alliance alliance ) {
			GetAllianceMembers( alliance );
		}
		/// <summary>
		/// Obtains data about the NetMarble servers
		/// </summary>
		/// <remarks>
		/// Downloads the NetMarble server data and loads it into
		/// <see cref="NetworkData.ServerInfo"/> if necessary, and returns that
		/// server info. Re-implementation of libil2cpp.so's
		/// <c>PacketTransfer.SetServerData()</c> and the following processing
		/// steps.
		/// </remarks>
		/// <returns></returns>
		static JsonDocument GetServerInfo() {
			if ( ServerInfo == null ) {
				// Obtained in libil2cpp.so via ServerInfo.GetRemoteFilePath()
				string serverInfoUrl = PatchUrl + "v" + BundleVersion + "/" + ServerFileName + "?p=" + Rng.Next( 0, 0x7fffffff ).ToString();
				// Obtained in libil2cpp.so via PacketTransfer.SetServerDataOK()
				ServerInfo = JsonDocument.Parse(
					Www.GetStringAsync( serverInfoUrl ).Result,
					JsonOptions
				);
			}
			return ServerInfo;
		}
		/// <summary>
		/// Returns settings for the NetMarble server in use
		/// </summary>
		/// <remarks>
		/// Re-implementation of libil2cpp.so's ServerInfo.get_data()
		/// </remarks>
		/// <returns></returns>
		static JsonElement GetServerData() {
			string selectServerType = GetServerInfo().RootElement.GetProperty( "select_server" ).GetProperty( "type" ).GetString();
			JsonElement serverList = GetServerInfo().RootElement.GetProperty( "server_list" );
			JsonElement selectedServer = new JsonElement();
			bool selected = false;
			foreach ( JsonElement server in serverList.EnumerateArray() ) {
				if ( server.GetProperty( "type" ).GetString() == selectServerType ) {
					selectedServer = server;
					selected = true;
					break;
				}
			}
			if ( selected == false ) {
				foreach ( JsonElement server in serverList.EnumerateArray() ) {
					selectedServer = server;
					break;
				}
			}
			return selectedServer;
		}
		/// <summary>
		/// Obtains the URL of the primary NetMarble HTTP server to use
		/// </summary>
		/// <remarks>
		/// Re-implementation of libil2cpp.so's ServerInfo.get_URL()
		/// </remarks>
		/// <returns>server url</returns>
		static string GetServerUrl() {
			return GetServerData().GetProperty( "detail" ).GetProperty( "websvr" ).GetString();
		}
		/// <summary>
		/// Find available alliances
		/// </summary>
		/// <remarks>
		/// Rework/reimplementation of PacketTransfer.GetRecommendedAllianceList()
		/// and PacketTransfer.GetRecommendedAllianceListOk()
		/// </remarks>
		static Dictionary<Alliance, string> FindInactiveAlliances( int alliancesToCheck, int daysInactive ) {
			const string param = "GetSuggestionAllianceList?lang=";
			int pulledAlliances = 0;
			HashSet<string> checkedAllianceNames = new HashSet<string>();
			Dictionary<Alliance, string> prospectiveAlliances = new Dictionary<Alliance, string>();
			Dictionary<Alliance, string> qualifyingAlliances = new Dictionary<Alliance, string>();
			JsonElement alliances;
			JsonElement desc, gname, wExp, autoJoinYN;
			JsonElement level, shopLevel, requiredLevel;
			string encodedName, description;
			int j = 0;
			double allianceDaysInactive = 0, maxDaysInactive = 0;
			while ( pulledAlliances < alliancesToCheck ) {
				using ( JsonDocument result = GetWww( param ) ) {
					if ( result == null
						|| !result.RootElement.TryGetProperty( "desc", out desc )
						|| !desc.TryGetProperty( "sgs", out JsonElement jsonElement )
						|| jsonElement.ValueKind != JsonValueKind.Array )
						continue;
					alliances = jsonElement.Clone();
				}
				foreach ( JsonElement allianceJson in alliances.EnumerateArray() ) {
					if ( !allianceJson.TryGetProperty( "gname", out gname )
						|| gname.ValueKind != JsonValueKind.String )
						continue;
					encodedName = gname.GetString();
					pulledAlliances++;
					if ( String.IsNullOrEmpty( encodedName )
						|| checkedAllianceNames.Contains( encodedName )
						|| !allianceJson.TryGetProperty( "wExp", out wExp )
						|| wExp.ValueKind != JsonValueKind.Number
						|| !allianceJson.TryGetProperty( "autoJoinYN", out autoJoinYN )
						|| autoJoinYN.ValueKind != JsonValueKind.Number )
						continue;
					checkedAllianceNames.Add( encodedName );
					if ( wExp.GetInt32() != 0
						|| autoJoinYN.GetInt32() != 1 )
						continue;
					if ( !allianceJson.TryGetProperty( "glv", out level )
						|| level.ValueKind != JsonValueKind.Number
						|| !allianceJson.TryGetProperty( "sLv", out shopLevel )
						|| shopLevel.ValueKind != JsonValueKind.Number
						|| !allianceJson.TryGetProperty( "lvt", out requiredLevel )
						|| shopLevel.ValueKind != JsonValueKind.Number )
						continue;
					Alliance alliance = new Alliance( allianceJson );
					UpdateAlliance( alliance );
					if ( alliance.Players.Count >= alliance.MaxMembers )
						continue;
					allianceDaysInactive = alliance.GetDaysInactive();
					description = $"level {level.GetInt32()}, shop level {shopLevel.GetInt32()}, required level {requiredLevel.GetInt32()}, {allianceDaysInactive} days without activity";
					prospectiveAlliances.Add( alliance, description );
					if ( allianceDaysInactive > maxDaysInactive ) maxDaysInactive = allianceDaysInactive;
					if ( allianceDaysInactive > daysInactive ) qualifyingAlliances.Add( alliance, description );
				}
			}
			return qualifyingAlliances;
		}
		static Dictionary<Alliance, string> FindInactiveAlliances( int alliancesToCheck ) {
			return FindInactiveAlliances( alliancesToCheck, 14 );
		}
		static Dictionary<Alliance, string> FindInactiveAlliances() {
			return FindInactiveAlliances( 100000, 14 );
		}
		public static List<Alliance> FindProspectiveAlliances() {
			return FindInactiveAlliances( 100000, 0 ).Keys.ToList();
		}
		public static List<Alliance> FindProspectiveAlliances( int toSearch ) {
			return FindInactiveAlliances( toSearch, 0 ).Keys.ToList();
		}
		public static List<Alliance> FindProspectiveAlliances( List<Alliance> oldAlliances ) {
			List<Alliance> newAlliances = CheckProspectiveAlliances( oldAlliances );
			newAlliances.AddRange( FindProspectiveAlliances() );
			return newAlliances;
		}
		/// <summary>
		/// Determines whether an alliance has had no logins for two weeks
		/// </summary>
		/// <param name="alliance">Alliance to check</param>
		/// <returns>False if any of the members of the alliance have logged in
		/// within two weeks, true otherwise</returns>
		static bool CanBeCaptured( Alliance alliance ) {
			if ( alliance.GetDaysInactive() > 14 ) return true;
			else return false;
		}
		static List<Alliance> GetInactiveAlliances( List<Alliance> alliances, int daysInactive = 1 ) {
			List<Alliance> inactiveAlliances = new List<Alliance>();
			foreach ( Alliance alliance in alliances ) {
				if ( alliance.GetDaysInactive() > daysInactive )
					inactiveAlliances.Add( alliance );
			}
			return inactiveAlliances;
		}
		static Alliance FindAlliance( string allianceName ) {
			string param = "SearchAlliance?guildName="
				+ Convert.ToBase64String( Encoding.UTF8.GetBytes( allianceName ) );
			Alliance alliance = new Alliance( allianceName );
			using ( JsonDocument result = GetWww( param ) ) {
				alliance.Load( result.RootElement );
			}
			return alliance;
		}
		static public Alliance GetAlliance( string allianceName ) {
			if ( String.IsNullOrEmpty( allianceName ) ) throw new ArgumentNullException( "allianceName" );
			Alliance alliance = new Alliance( allianceName );
			GetAllianceMembers( alliance );
			return alliance;
		}
		static void GetAllianceMembers( Alliance alliance ) {
			if ( alliance.Id == default ) {
				if ( String.IsNullOrEmpty( alliance.Name ) ) {
					throw new ArgumentException( "Neither alliance name nor alliance ID are given", "alliance" );
				}
				alliance.Id = FindAlliance( alliance.Name ).Id;
			}
			string param = "ViewAllianceInfo?guID=" + alliance.Id.ToString();
			using ( JsonDocument result = GetWww( param ) ) {
				if ( result != null ) {
					alliance.Load( result.RootElement );
				}
			}
		}
		static JsonDocument GetWww( string param ) {
			HttpRequestMessage request = GetWwwRequest( param );
			byte[] responseBytes = Www.Send( request ).Content.ReadAsByteArrayAsync().Result;
			byte[] decryptedBytes = ResponseDecrypt( responseBytes );
			string text = ResponseDecompress( decryptedBytes );
			return FixJson( text );
		}
		/// <summary>
		/// Trims illegal characters from the end of a JSON string until
		/// parseable, then parses the JSON
		/// </summary>
		/// <param name="text">JSON-formatted string</param>
		/// <returns>JsonDocument object, discarding extraneous data after the
		/// last <c>}</c></returns>
		static JsonDocument FixJson( string text ) {
			int lastBrace = text.LastIndexOf( '}' );
			JsonDocument result = null;
			while ( lastBrace > -1 ) {
				text = text.Substring( 0, lastBrace + 1 );
				try {
					result = JsonDocument.Parse( text );
					break;
				} catch ( System.Text.Json.JsonException ) {
					text = text.Substring( 0, text.Length - 1 );
					lastBrace = text.LastIndexOf( '}' );
				}
			}
			if ( lastBrace > -1 ) return result;
			else {
				result.Dispose();
				return null;
			}
		}
		/// <summary>
		/// Obtains network data via HTTP(S)
		/// </summary>
		/// <remarks>
		/// Reimplimentation and simplification of WWWUtil.Get(),
		/// WWWUtil.GetRountine(), and affiliated methods
		/// </remarks>
		static HttpRequestMessage GetWwwRequest( string parameter ) {
			HttpRequestMessage request = new HttpRequestMessage();
			if ( !parameter.StartsWith( "http" ) ) {
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
		static HttpContent BuildRequestContent( string param ) {
			string completeParam = AddDefaultPacketParameter( param );
			string key = GetPacketKey();
			if ( String.IsNullOrEmpty( key ) )
				key = GetAesKey() + GetAesKey();
			byte[] parameterBytes = Encoding.UTF8.GetBytes( completeParam );
			byte[] encryptedParameter = AesEncrypt( parameterBytes, key, true );
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
			return AesEncrypt( headerBytes, GetAesKey() + GetAesKey() );
		}
		/// <summary>
		///
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
		static void LoadConstants() {
			PreLogin();
		}
		/// <summary>
		///
		/// </summary>
		/// <remarks>
		/// Reimplementation of libil2cpp.so's SceneTitle.PreLogin() and the
		/// following steps through SceneTitle.PreLoginOK()
		/// </remarks>
		/// <exception cref="NotImplementedException"></exception>
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
			formData.Add( "ver", BundleVersion );
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
				byte[] decryptedBytes = ResponseDecrypt( responseBytes );
				string text = ResponseDecompress( decryptedBytes );
				using ( JsonDocument result = FixJson( text ) ) {
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
		///
		/// </summary>
		/// <remarks>
		/// Re-implementation of Java::SessionImpl.getPlayerID()
		/// </remarks>
		/// <returns></returns>
		static string GetPlayerId() {
			return GetCID();
		}
		static string GetCID() {
			if ( String.IsNullOrEmpty( CID ) ) {
				CID = Guid.NewGuid().ToString( "N" ).ToUpper();
			}
			return CID;
		}
		static string GetDeviceModel() {
			if ( String.IsNullOrEmpty( DeviceModel ) ) {
				DeviceModel = "HTC One";
			}
			return DeviceModel;
		}
		static string GetDeviceId() {
			if ( String.IsNullOrEmpty( DeviceId ) ) {
				string part1 = "0,0," + GetAndroidId();
				string part2 = GetDeviceKey() + GetDeviceModel();
				DeviceId = GetMD5( part1 ) + "-" + GetMD5( part2 );
			}
			return DeviceId;
		}
		/// <summary>
		///
		/// </summary>
		/// <remarks>
		/// Reimplementation of Java::SessionImpl.getDeviceKey()
		/// </remarks>
		/// <returns></returns>
		static string GetDeviceKey() {
			if ( String.IsNullOrEmpty( DeviceKey ) ) {
				DeviceKey = Guid.NewGuid().ToString( "N" ).ToUpper();
			}
			return DeviceKey;
		}
		static string GetAndroidId() {
			if ( String.IsNullOrEmpty( AndroidId ) ) {
				long num = ( Rng.Next() << 31 | Rng.Next() );
				AndroidId = num.ToString( "X16" );
			}
			return AndroidId;
		}
		static string AesEncrypt( string decrypted, string key ) {
			ASCIIEncoding asciiEncoding = new ASCIIEncoding();
			byte[] rijKey = asciiEncoding.GetBytes( key );
			UnicodeEncoding unicodeEncoding = new UnicodeEncoding();
			byte[] decryptedBytes = unicodeEncoding.GetBytes( decrypted );
			byte[] encryptedBytes = AesEncrypt( decryptedBytes, rijKey );
			return Convert.ToBase64String( encryptedBytes );
		}
		static byte[] AesEncrypt( byte[] decrypted, string key, bool isKeyIvSame = true ) {
			byte[] keyBytes = ( new ASCIIEncoding() ).GetBytes( key );
			return AesEncrypt( decrypted, keyBytes, isKeyIvSame );
		}
		static byte[] AesEncrypt( byte[] decrypted, byte[] key, bool isKeyIvSame = true ) {
			byte[] encryptedBytes = null;
			using ( RijndaelManaged rijAlg = new RijndaelManaged() ) {
				rijAlg.KeySize = key.Length << 3;
				rijAlg.BlockSize = 128;
				rijAlg.Mode = CipherMode.CBC;
				rijAlg.Padding = PaddingMode.PKCS7;
				rijAlg.Key = key;
				if ( isKeyIvSame ) rijAlg.IV = key;
				else rijAlg.IV = new byte[16];
				ICryptoTransform encryptor = rijAlg.CreateEncryptor( rijAlg.Key, rijAlg.IV );
				using ( MemoryStream msEncrypt = new MemoryStream() ) {
					using ( CryptoStream csEncrypt = new CryptoStream( msEncrypt, encryptor, CryptoStreamMode.Write ) ) {
						csEncrypt.Write( decrypted, 0, decrypted.Length );
						csEncrypt.FlushFinalBlock();
						encryptedBytes = msEncrypt.ToArray();
					}
				}
			}
			return encryptedBytes;
		}
		static string GetMD5( string input ) {
			string output;
			using ( MD5 md5hash = MD5.Create() ) {
				byte[] outputBytes = md5hash.ComputeHash( Encoding.UTF8.GetBytes( input ) );
				var outputBuilder = new StringBuilder();
				for ( int i = 0; i < outputBytes.Length; i++ ) {
					outputBuilder.Append( outputBytes[i].ToString( "X2" ) );
				}
				output = outputBuilder.ToString();
			}
			return output;
		}
		static string GetIP() {
			if ( String.IsNullOrEmpty( IP ) )
				IP = "10.0.2.16";
			return IP;
		}
		static byte[] ResponseDecrypt( byte[] encryptedBytes ) {
			string key = GetPacketKey();
			if ( String.IsNullOrEmpty( key ) ) {
				key = GetAesKey() + GetAesKey();
			}
			byte[] keyBytes = Encoding.UTF8.GetBytes( key );
			return CoreDecrypt( encryptedBytes, keyBytes, keyBytes );
		}
		static byte[] CoreDecrypt( byte[] text, byte[] key, byte[] iv ) {
			using ( RijndaelManaged rijAlg = new RijndaelManaged() ) {
				rijAlg.KeySize = key.Length << 3;
				rijAlg.BlockSize = 128;
				rijAlg.Mode = CipherMode.CBC;
				rijAlg.Padding = (PaddingMode)2;
				rijAlg.Key = key;
				rijAlg.IV = iv;
				ICryptoTransform decryptor = rijAlg.CreateDecryptor( key, iv );
				using ( MemoryStream msDecrypt = new MemoryStream( text ) ) {
					using ( CryptoStream csDecrypt = new CryptoStream( msDecrypt, decryptor, CryptoStreamMode.Write ) ) {
						csDecrypt.Write( text, 0, text.Length );
						csDecrypt.FlushFinalBlock();
						return msDecrypt.ToArray();
					}
				}
			}
		}
		static string ResponseDecompress( byte[] compressedBytes ) {
			string decompressed = "";
			try {
				SnappyDecompressor snappyDecompressor = new SnappyDecompressor();
				byte[] decompressedBytes = snappyDecompressor.Decompress( compressedBytes, 0, compressedBytes.Length );
				decompressed = Encoding.UTF8.GetString( decompressedBytes );
			} catch ( IndexOutOfRangeException ) {
				decompressed = Encoding.UTF8.GetString( compressedBytes );
			}
			return decompressed;
		}
		static string GetAccessToken() {
			if ( String.IsNullOrEmpty( AccessToken ) ) {
				SignIn();
			}
			return AccessToken;
		}
		static string GetAesKey() {
			return AesKey;
		}
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
		/// Obtains the URL to use for secure NetMarble HTTP requests
		/// </summary>
		/// <remarks>
		/// Re-implementation of libil2cpp.so's ServerInfo.get_SslURL()
		/// </remarks>
		/// <returns>A string representing the secure URL</returns>
		static string GetSslUrl() {
			return GetServerData().GetProperty( "detail" ).GetProperty( "websvr_ssl" ).GetString();
		}
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
		static void SetPacketKey( string protoPacketKey = null ) {
			PacketKey = protoPacketKey;
		}
		static void SetTextKey( string text ) {
			string pk = GetPacketKey();
			System.Text.UnicodeEncoding unicodeEncoding = new System.Text.UnicodeEncoding( false, true, false );
			byte[] textBytes = unicodeEncoding.GetBytes( text );
			System.Text.ASCIIEncoding asciiEncoding = new System.Text.ASCIIEncoding();
			byte[] keyBytes = asciiEncoding.GetBytes( pk );
			System.Security.Cryptography.RijndaelManaged rijAlg = new System.Security.Cryptography.RijndaelManaged();
			rijAlg.KeySize = keyBytes.Length << 3;
			rijAlg.BlockSize = 0x80;
			rijAlg.Mode = (CipherMode)1;
			rijAlg.Padding = (PaddingMode)1;
			rijAlg.Key = keyBytes;
			rijAlg.IV = new byte[16];
			ICryptoTransform encryptor = rijAlg.CreateEncryptor( rijAlg.Key, rijAlg.IV );
			using ( MemoryStream msEncrypt = new MemoryStream() ) {
				using ( CryptoStream csEncrypt = new CryptoStream( msEncrypt, encryptor, CryptoStreamMode.Write ) ) {
					csEncrypt.Write( textBytes, 0, textBytes.Length );
					csEncrypt.FlushFinalBlock();
					textBytes = msEncrypt.ToArray();
				}
			}
			// Debug.Log( "Encrypted textBytes.Length: " + textBytes.Length );
			// textKey_ = CryptUtil__ConvertByteArrayToStringB(,btSrc,)
			string textKey_ = Convert.ToBase64String( textBytes );
			// Debug.Log( "Base64 textKey_.Length: " + textKey_.Length );
			// Debug.Log( "Base64 textKey_: " + textKey_ );
			// textKey_ = CryptUtil__XOREncode(,textKey_,) [0x0109ff70]
			StringBuilder builder = new StringBuilder( textKey_.Length );
			char[] decoded = textKey_.ToCharArray();
			// Debug.Log( "textKey as char[] length: " + decoded.Length );
			uint[] xor_table = { 0, 0, 0, 0 };
			// Debug.Log( "xor_table.Length: " + xor_table.Length );
			for ( int i = 0; i < decoded.Length; i++ ) {
				builder.Append( (uint)( (char)( (uint)decoded[i] ^ xor_table[i % xor_table.Length] ) ) );
			}
			textKey_ = builder.ToString();
			// Debug.Log( "XORed textKey_ length: " + textKey_.Length );
			// Debug.Log( "textKey_: " + textKey_ );
			System.Text.Encoding encoding = System.Text.Encoding.UTF8;
			textKey_ = Convert.ToBase64String( encoding.GetBytes( textKey_ ) );
			// Debug.Log( "Base64'd textKey_ length: " + textKey_.Length );
			// Debug.Log( "Set textKey_ to " + textKey_ );
		}
	}
}

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
	/*
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
		} */
/*
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
/*	void LoadTestAsset() {
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
