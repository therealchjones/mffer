using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Snappy.Sharp;
//using UnityEditor;
//using UnityEngine;
//using UnityEngine.Networking;

/* method to load data from asset vs csv
	TableUtility__Load_<object>_(0,true,MethodInfo_10988* Method$TableUtility.Load<IntAbilityGroupDataDictionary>()): (call 0x01fe1277 = 33428087)
	EBP= MethodInfo_10988*
	EAX = [EBP + 0x18] = [ 0x18th byte after address of start of MethodInfo_10988 ] = [ address of start of _union_261045 ]
		= [ address of start of void* methodDefinition OR address of start of Il2CppRGCTXData* rgctx_data]
		= void* methodDefinition OR Il2CppRGCTXData* rgctx_data
	ESI = [EAX + 0x4] (should be of type System_RuntimeTypeHandle_o* for CALL 0x0274c389 = 41206665 )
		= [ 0x4th byte after address of start of void methodDefinition or Il2CppRGCTXData rgctx_data ]
		= [ 0x4th byte after address of start of void methodDefinition or void * rgctxDataDummy, MethodInfo* method, Il2CppType * type, or Il2CppClass * klass ]
		= [ 0x4th byte after address of (nope)
class ReverseEngineeredProgramFlow {
	// Process to open CSVs from program start:
	// scenetitle$$start
	// SceneTitle__CheckServer
	// PacketTransfer__SetServerData
	// http://mheroesgb.gcdn.netmarble.com/mheroesgb/

	// - Get server data (server_info.txt)- sets url for server_info.txt & downloads, (hopefully?) puts data into something accessible by SceneTitle__SetServerDataOK (such as the not found local server file); will proceed as though PacketTransfer__SetServerData downloaded server_info.txt is sent to that

	// PacketTransfer_SetServerDataOK
	// SceneTitle__SetServerDataOK
	// ServerInfo__ParseVersionFile
	// ServerInfo__ParseVersionFileCDN (                - loads server response text
                // - ServerInfo__set_data
                //     - including, presumably, SslUrl)
	// (Back up to SceneTitle__SetServerDataOK)—>SceneTitle__ShowUpdateInfoView
	// (Callback)SceneTitle.<ShowUpdateInfoView>m_0();
	// Showtermsofservice
	// packettransfer__checkaccount
	// packettransfer$$Checkaccountok
	// sceneTitle$$oncheckaccountok
	// scenetitle__showtermsofservice (again)
	// SceneTitle$$SignIn
	// Plugins__signin
	// - Get access token
	// 	Searching for gameCode/accessToken:
	// ConfigurationImpl.loadXml
	// setGameCode
	// gameCode
	// GMC2Network.getConstants
	// Configuration.copy
	// gameToken
	// ChannelNetwork.INSTANCE.getChannel(… gameCode, … gameToken,…)
	// LogIm[pl: I_GameCode netmarbles?
	// SessionImpl: JSONObject3
	// NetMarlbeLog
	// AuthDataManager.KEY_GAME_TOKEN
	// Service$$GetGameCode: not obviously called by anything, returns only mherosgb (without e)
	// curl -H "gameCode: mherosgb" -v 'Https://apis.netmarble.com/mobileauth/v2/players/01A703E413294D38825A102A6E5E943E/deviceKeys/9C088F867BAB426987D6DA877B782F44/accessToken'
	// - gameToken:
	//     - Java doSignIn result JSON object accessToken ->SessionNetwork.signIn(gatewayUrl, playerId, DeviceKey, GameCode, AndroidId, CountryCode, str, callback)
	//     - https://apis.netmarble.com/, playerID, devicekey, Gamecode mherosgb?, androidID random uuid, US,
	//     - Https://apis.netmarble.com/mobileauth/v2/players/01A703E413294D38825A102A6E5E943E/deviceKeys/9C088F86-7BAB-4269-87D6-DA877B782F44/accessToken
	// Back up the chain to SceneTitle$$OnSignIn
	// SceneTitle\$\$NextStepByNetmarbleSignIn->
	// SceneTitle__NextStepByConnect->
	// SceneTitle$$CheckCertificationWithCondition->
	// SceneTitle\$\$PreLogin->
	// 	ServerDetail__get_WebServerSSL: returns websvr_ssl (https://mherosgb.netmarble.com/NM/)
	// ServerInfo__get_SslURL:  calls get_WebServerSSL
	// URL: concat surl, “PreLogin”: https://mherosgb.netmarble.com/NM/PreLogin
	// PacketTransfer__PreLogin: gathers form data information, calls WWWUtil_PostSSL
	// Form data:
	// - cID: PluginsNetmarbleS$$get_PlayerId; set_PlayerId (general: random UUID with -s removed, all upper case)
	// - dID: GetDeviceId2; mine is in ff_openudid.xml, general:
	//     - str=android.os.SystemProperties.ro.serialno or 0
	//     - str2=same
	//     - string2=UnityPlayer.currentActivity.getContentResolver() android_id or 0
	//     - str3=0,0,string2
	//     - str4=str,str2,string2 if all 0, randomUUID()
	//     - deviceName = Devices.getDeviceName() = e.g., “HTC One”
	//     - md5(str3+deviceName)”-“md5(str4+deviceName)
	//     - Simple valid = md5(“0,0,0HTC One”)”-“md5(randomUUID+”HTC One”)
	// - gameToken: (from above apis.netmarble.com URL)
	// - platform: android
	// - ver: 6.2.0
	// - lang: en(?)
	// - country: US(?)
	// - ds: CommonUtil$$IsDaylightSavingTime 1(?)
	// - client_ip: get_ipAddress 127.0.0.1(?)
	// - srvPush: get_allowGame(get_PushNotification) 1(?)
	// - de: get_deviceModel “HTC One” (?)
	// - pan: Panho$$isEnableLimit(0,1,0) 0(?)
	// - pan2: Panho$$isEnable(0,1,0) 0(?)
	// - timeZone: -08:00(?)
	// WWWUtil__PostSSL:  processes & submits form data; on success passes result to PacketTransfer__PreLoginOK
	// Form processing:
	// base url=Https://mheroesgb.netmarble.com/NM/PreLogin?cKey=fRealtimeSinceStartup(sec)

	// PacketTransfer__PreLogin: WWWUtil__PostSSL
	// - Get text key
	// —>PreLoginOK:
	// 	PacketTransfer__PreLoginOK: sets:
	// textKey: WWWResult->Json->key=tek
	// packetKey: concat(WWWresult—>JSON->key=sessID->last 8 characters if length > 19, WWWresult->Json->key=cID->last 8 characters)
	// CryptUtil__set_textKey(,tek,):
	//		pk = CryptUtil__get_packetKey
	//		textKey_ = CryptUtil__AESEncrypt(,tek,pk,)-->
	//		textKey_ = CryptUtil__XOREncode(,textKey_,)
	// - Get asset
	// - Decrypt asset
	// - Format csv vs load into dbtable

	// Maybe: - SceneTitle__Login_c__Iterator1__MoveNext (maybe from SceneTitle::Login)
    // - DBTable__LoadDB
    //     - Various DBTable__get_*Table
    //         - TableUtility__Load_*_
    //             - TableUtility__GetAssetPath (by type)
    //                 - TableUtility__GetPathWithoutExtension + “.asset”
    //                     - Which seems to dynamically determine loader to call, <type>$$LoadCSV

	/*****************************************************
	// Eventual goal: parse supplied text assets to CSV //
	*****************************************************/
// ISO8Set__LoadCSV() -->
// CSVLoader__Load(,,text/data/ISO8_SET.csv,) -->
// AssetBundleLoader__Load_TextAsset_(,text/data/ISO8-SET.csv,) -->
// AssetBundleLoader__LoadAsset(, text/data/ISO8_SET.csv,typeof(TextAsset),) -->
// AssetBundleMgr__LoadAsset(,text/data/ISO_SET.csv,type,) (or UnityEngine_Resources__Load if not found)-->
// AssetBundleMrg__LoadAsset(,text,text/data/ISO8_SET.csv,type,false,) -->
// AssetBundleMgr_AssetBundleData__LoadAsset(assetBundle, text/data/ISO8_SET.csv,type,) -->
// UnityEngine_AssetBundle__LoadAsset(assetBundle, text/data/ISO8_SET.csv,type,), returning the TextAsset back
// up the chain to CSVLoader__Load:
// str = UnityEngine_TextAsset__get_text(textAsset,);
// CSVLoader__LoadFromString(textAsset,readvalue,str,0)-->
// CryptUtil__AESDecryptText(,str,) [0x010a1fc9]-->
// 		key = CryptUtil__get_textKey(,)
// 			textKey_ = CryptUtil_TypeInfo->CryptUtil_c-->CryptUtil_StaticFields-->textKey_
//			packetKey = CryptUtil__get_packetKey(,) -->
//				PluginsCommonForAndroid-->get_packetKey -->
//				(decompiled) getPacketKey
//			textKey = CryptUtil__XORDecode(,textKey_,)
//			textKey = CryptUtil__AESDecrypt(,textKey,packetKey,) (or, if packetKey is null, just textKey_)
// 		CryptUtil__AESDecrypt(,str,key,) [0x010a055b] ->

// from (decompiled) classes/sources/com/seed9/unityplugins/UnityPluginCommon.java:
// AesKey = "!YJKLNGD"

// packetKey:

// CryptUtil$$Reset (but this takes it from String_TypeInfo) or PacketTransfer__PreLoginOK: (along with setting userId, sessionId, isEmailRegistered, cID, isNewAccount,apkToken,admit,textKey)
// If sessionId length >=20, take last 8 chars only; for
// (Last8 of sessionId)^2->
// CryptUtil__set_packetKey -> PluginsCommonForAndroid__set_PacketKey —>decompiled setPacketKey
// When getting packetKey, if null, concat(aesKey,aesKey) is used instead in WWWUtil__Get, but not in CryptUtil__get/set_textKey. CryptUtil$$PacketDecode tries the AESkey^2 first.
// CryptUtil__set_textKey(,string,) (from PacketTransfer$$PreLoginOK)

// Setting textKey_ in CryptUtil_TypeInfo:
// Start at CSVLoader__LoadFromString with CryptUtil_TypeInfo initializer (43ef37c, in .bss) (starting at 10a45d7):
// XOREncode/XORDecode uses xor_table in CryptUtil_TypeInfo, but since it uses it for each,
// can't I just use any table?

// For this program, reorganizing based on dependencies, so the idea is just to
// call GetCSV(Type) or something similar (maybe even an umbrella GetAllCSV or the like)
// and determine which parts need to be called to simulate a login and download or
// otherwise obtain needed data. Would also make a "force" flag to update all the date

// Where reasonable below, methods have the same name as the function's basename
// in libil2cpp.so (without having the namespace, it may combine parts from multiple
// namespaces)



//TablePath = text/data/
//TableName = TableNameAttribute/CSVTableNameAttribute
//ext = csv
// */

/*
Most are simple base64-encoded strings
the base-64 strings when decoded have a ^@ (null) before every character after every character, presumably due to
the use of 16-bit characters? Should effectively strip when possible or otherwise work around
Many are within the device/data/media/0/Android/data/com.netmarble.mherosgb/files/bundle/text asset bundle
(These have already been decoded to output/)
Others appear to be identified by TableName/TableNameAttribute/CSVTableNameAttribute but
I don't yet know where they're stored
TableNameAttribute and CSVTableNameAttribute appear to be set by their respective ctors called by various
anonymous functions with names/"filenames" strings from .rodata

The simple *.csv textassets can be exported and (when necessary), base64 decoded with only command line base64 -D -i filename
Without rigorous testing, the TableNames appear to be MonoBehavior/MonoScript pairs in bundle/text, though I'm not sure yet about decoding;
need to better eval, e.g., loading IntAbilityGroupDataDictionary from text/data/action_ability.asset

Appears UABE can extract these to JSON files
Perhaps these are the ones that are too large for CSVs?


Some are:
TableNameAttribute:
ACTION_AUTO_ABILITY
ALLIANCE_EMBLEM_BG
ALLIANCE_EMBLEM_BORDER
ALLIANCE_EMBLEM_SYMBOL
APPLY_OTHER_BY_TARGET
ARENA_PARTICIPATION_RANK_REWARD
HERO_SKILL
MOB_SKILL
GAME_CONFIG
TOURNAMENT_EVENT_BATTLE_CONFIG
SHADOWLAND_BATTLE_CONFIG
SUPER_COOP_BATTLE_CONFIG
PVP_BALANCE
DOMINATION_BATTLE_CONFIG
DANGER_ROOM_CONFIG
ACHIEVEMENTS
ALLIANCE_ACHIEVEMENTS
INTRUSION_TEAMUP
INTRUSION_BOSS
LOADING_TIP
LOADING_TIP_LIST
WORLD_BOSS_REWARD
WORLD_BOSS_HAVE_BONUS
URU_PREMIUM
URU_COMPOSE
TOURNAMENT_EVENT_REWARD
SUPER_COOP_QUEST
SUMMON
SUBTYPE_GROUP_ID
COUNTRY_LATLON
ERR_PROCESS
DOMINATION_MOB
STORY_CAMPAIGN_TRAIT
STORY_CAMPAIGN_LEVEL
STORY_CAMPAIGN_HERO
STAGE_FIRST_CLEAR_REWARD
StageEnterBundleList
TEAM_LEAGUE_RANK_REWARD
TEAM_LEAGUE
TEAM_LEAGUE_CONDITION
SPECIAL_GEAR_ICON
... and lots more

CSVTableNameAttribute:
ACTION_ABILITY
ALLIANCE_EMBLEM
ARENA_RANK_REWARD
HERO_SKILL
MOB_SKILL
RANDOM_OPTION
ADD_ABILITY_LIST

TableUtility__GetCSVPaths:
text/data/ + TableName + .csv
TableUtility__GetAssetPath:
text/data/ + TableName + .asset

*/

/* Other interesting things:
	GlobalConstants___ctor
	Maybe everything in DBTable->Fields
*/

class NetworkData {
	/// <summary>
	/// Base URL for obtaining further network information
	/// </summary>
	/// The url returned by PatchSystem.GetBaseUrl() in the binary; could change with different versions
	const string PatchBaseUrl = "http://mheroesgb.gcdn.netmarble.com/mheroesgb/";
	/// <summary>
	///
	/// </summary>
	/// Similarly, added on by PatchSystem.CreateUrl()
	const string PatchUrl = PatchBaseUrl + "DIST/Android/";
	/// <summary>
	///
	/// </summary>
	/// From plugins (com/seed9/common/Common.java:getBundeVersion())
	const string BundleVersion = "7.5.1";
	/// <summary>
	///
	/// </summary>
	/// From ServerInfo.GetFileName()
	const string ServerFileName = "server_info.txt";
	/// <summary>
	///
	/// </summary>
	/// Similarly, added by PacketTransfer.SetServerData() (via ServerInfo.GetRemoteFilePath())
	const string ServerUrlPath = PatchUrl + "v" + BundleVersion + "/" + ServerFileName + "?p=";
	HttpClient Www = new HttpClient();
	string accessToken;
	string accessTokenUrl = "https://apis.netmarble.com/mobileauth/v2/";
	string AesKey = "!YJKLNGD";
	string cID;
	string deviceID;
	string deviceKey;
	string deviceModel;
	string gameCode = "mherosgb";
	string getVersionUrl = "https://mherosgb.netmarble.com/NM/GetVersion";
	string IP = "216.143.116.194";
	string packetKey = "OGEIJVOJGTGJJJMF";
	string preLoginUrl = "https://mherosgb.netmarble.com/NM/PreLogin";
	string sessionID;
	string textKey; // demangled text key (maybe the same as tek), for use decrypting TextAssets
	string textKey_; // stored text key, mangled from tek, which must be demangled before use
	string tek = "3&?f(7F>"; // initial key downloaded from PreLogin() ("tek" field in result JSON)
	/// <summary>
	/// Obtains the Netmarble server data
	/// </summary>
	/// Re-implementation of PacketTransfer.SetServerData() and response
	/// handlers up through ServerInfo.ParseVersionFileCDN() to obtain
	/// information about the Netmarble patch and game servers
	public string GetServerData() {
		string serverDataUrl = ServerUrlPath + ( new Random() ).Next( 0, 0x7fffffff ).ToString();
		JsonDocumentOptions jsonOptions = new JsonDocumentOptions {
			CommentHandling = JsonCommentHandling.Skip,
			AllowTrailingCommas = true
		};
		JsonDocument response = JsonDocument.Parse( Www.GetStringAsync( serverDataUrl ).Result, jsonOptions );
		return response.RootElement.ToString();
	}
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
		*/
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
	string AesEncrypt( string decrypted, string key ) {
		// Debug.Log( "AES encrypting " + decrypted + " with key " + key );
		ASCIIEncoding asciiEncoding = new ASCIIEncoding();
		byte[] rijKey = asciiEncoding.GetBytes( key );
		UnicodeEncoding unicodeEncoding = new UnicodeEncoding();
		byte[] decryptedBytes = unicodeEncoding.GetBytes( decrypted );
		byte[] encryptedBytes;
		using ( RijndaelManaged rijAlg = new RijndaelManaged() ) {
			rijAlg.KeySize = rijKey.Length << 3;
			rijAlg.BlockSize = 128;
			rijAlg.Mode = CipherMode.CBC;
			rijAlg.Padding = PaddingMode.PKCS7;
			rijAlg.Key = rijKey;
			rijAlg.IV = new byte[16];
			// Create a decryptor to perform the stream transform.
			ICryptoTransform encryptor = rijAlg.CreateEncryptor( rijAlg.Key, rijAlg.IV );
			// Create the streams used for decryption.
			using ( MemoryStream msEncrypt = new MemoryStream() ) {
				using ( CryptoStream csEncrypt = new CryptoStream( msEncrypt, encryptor, CryptoStreamMode.Write ) ) {
					csEncrypt.Write( decryptedBytes, 0, decryptedBytes.Length );
					csEncrypt.FlushFinalBlock();
					encryptedBytes = msEncrypt.ToArray();
				}
			}
		}
		return Convert.ToBase64String( encryptedBytes );
	}
	byte[] CoreAesDecrypt( byte[] text, byte[] key, byte[] iv ) {
		using ( RijndaelManaged rijAlg = new RijndaelManaged() ) {
			rijAlg.KeySize = key.Length << 3;
			rijAlg.BlockSize = 128;
			rijAlg.Mode = CipherMode.CBC;
			rijAlg.Padding = (PaddingMode)2;
			rijAlg.Key = key;
			rijAlg.IV = iv;
			ICryptoTransform decryptor = rijAlg.CreateDecryptor( key, iv );
			// Create the streams used for decryption.
			using ( MemoryStream msDecrypt = new MemoryStream( text ) ) {
				using ( CryptoStream csDecrypt = new CryptoStream( msDecrypt, decryptor, CryptoStreamMode.Write ) ) {
					csDecrypt.Write( text, 0, text.Length );
					csDecrypt.FlushFinalBlock();
					return msDecrypt.ToArray();
				}
			}
		}

	}
	string getAccessToken() {
		if ( String.IsNullOrEmpty( accessToken ) ) {
			string url = String.Concat( accessTokenUrl, "players/", getCID(), "/deviceKeys/", getDeviceKey(), "/accessToken" );
			HttpRequestMessage request = new HttpRequestMessage( HttpMethod.Get, url );
			request.Headers.Add( "GameCode", gameCode );
			HttpResponseMessage response = Www.Send( request );
			JsonDocumentOptions options = new JsonDocumentOptions {
				CommentHandling = JsonCommentHandling.Skip,
				AllowTrailingCommas = true
			};
			JsonDocument signInResponse = JsonDocument.Parse( response.Content.ReadAsStringAsync().Result, options );
			accessToken = signInResponse.RootElement.GetProperty( "resultData" ).GetProperty( "accessToken" ).GetString();
			Www.Dispose();
		}
		return accessToken;
	}
	string getAccessTokenUrl() {
		// get server info, parse for access token URL
		return "foo";
	}
	string getAesKey() {
		return String.Concat( AesKey, AesKey );
	}
	string getCID() {
		if ( String.IsNullOrEmpty( cID ) ) {
			cID = Guid.NewGuid().ToString( "N" ).ToUpper();
		}
		return cID;
	}
	string getDeviceID() {
		if ( String.IsNullOrEmpty( deviceID ) ) {
			string part1 = "0,0,0" + deviceModel;
			string part2 = getDeviceKey() + deviceModel;
			deviceID = getMD5( part1 ) + "-" + getMD5( part2 );
		}
		return deviceID;
	}
	string getDeviceKey() {
		if ( String.IsNullOrEmpty( deviceKey ) ) {
			deviceKey = Guid.NewGuid().ToString( "N" ).ToUpper();
		}
		return deviceKey;
	}
	string getDeviceModel() {
		if ( String.IsNullOrEmpty( deviceModel ) ) {
			deviceModel = "HTC One";
		}
		return deviceModel;
	}
	string getIP() {
		return IP;
	}
	string getMD5( string input ) {
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
	string getPacketKey() {
		if ( String.IsNullOrEmpty( packetKey ) ) {
			LoadConstants();
		}
		// Debug.Log( "Got packet key: " + packetKey );
		return packetKey;
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
	void LoadConstants() {
		preLogin();
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
		} */
	public void preLogin() {
		double uptime = 37;


		// Debug.Log( "Posting preLogin to " + preLoginUrl );
		HttpRequestMessage request = new HttpRequestMessage( HttpMethod.Post, preLoginUrl );
		Dictionary<string, string> formData = new Dictionary<string, string>();

		formData.Add( "cID", getCID() );
		formData.Add( "dID", getDeviceID() );
		formData.Add( "gameToken", getAccessToken() );
		formData.Add( "platform", "android" );
		formData.Add( "ver", BundleVersion );
		formData.Add( "lang", "en" );
		formData.Add( "country", "US" );
		formData.Add( "ds", "1" );
		formData.Add( "client_ip", getIP() );
		formData.Add( "srvPush", "1" );
		formData.Add( "de", getDeviceModel() );
		formData.Add( "pan", "0" );
		formData.Add( "pan2", "1" );
		formData.Add( "openId", "0" );
		formData.Add( "cKey", uptime + "" );

		FormUrlEncodedContent form = new FormUrlEncodedContent( formData );
		request.Content = form;
		HttpResponseMessage response = Www.Send( request );

		/*
				foreach ( KeyValuePair<string, string> kvp in request.GetResponseHeaders() ) {
					// Debug.Log( kvp.Key + ": " + kvp.Value );
				}
		*/
		// Debug.Log( "Downloaded a response of " + request.downloadedBytes + " bytes." );
		// Debug.Log( "Response Text: " );
		byte[] responseBytes = response.Content.ReadAsByteArrayAsync().Result;
		// Debug.Log( request.downloadHandler.text );

		byte[] decryptedBytes = ResponseDecrypt( responseBytes );
		// Debug.Log( "Decrypted bytes: " + decryptedBytes.Length );
		// Debug.Log( "Decrypted text:" );
		// Debug.Log( System.Text.Encoding.UTF8.GetString( decryptedBytes ) );

		byte[] decompressedBytes = ResponseDecompress( decryptedBytes );
		// Debug.Log( "Decompressed bytes: " + decompressedBytes.Length );
		string decompressedText = System.Text.Encoding.UTF8.GetString( decompressedBytes );
		// Debug.Log( "Decompressed text:" );
		// Debug.Log( decompressedText );

		JsonDocument result = JsonDocument.Parse( decompressedText );

		cID = result.RootElement.GetProperty( "desc" ).GetProperty( "cID" ).GetString();
		// Debug.Log( "cID: " + cID );
		sessionID = result.RootElement.GetProperty( "desc" ).GetProperty( "sessID" ).GetString();
		// Debug.Log( "sessionID: " + sessionID );
		if ( sessionID.Length > 25 ) {
			sessionID = sessionID.Substring( sessionID.Length - 8 );
			// Debug.Log( "(truncated to " + sessionID + ")" );
		}
		setPacketKey( sessionID + cID.Substring( cID.Length - 8 ) );
		// Debug.Log( "packetKey set to " + getPacketKey() );
		setTextKey( result.RootElement.GetProperty( "desc" ).GetProperty( "tek" ).GetString() );
		// Debug.Log( "result.desc.tek: " + result.desc.tek );
		// Debug.Log( "textKey_ set to " + textKey_ );
		// Debug.Log( "textKey set to " + getTextKey() );
	}
	byte[] ResponseDecompress( byte[] compressedBytes ) {
		SnappyDecompressor snappyDecompressor = new SnappyDecompressor();
		return snappyDecompressor.Decompress( compressedBytes, 0, compressedBytes.Length );
	}
	byte[] ResponseDecrypt( byte[] encryptedBytes ) {
		byte[] key = Encoding.UTF8.GetBytes( getAesKey() );
		return CoreAesDecrypt( encryptedBytes, key, key );
	}
	void setPacketKey( string protoPacketKey ) {
		if ( String.IsNullOrEmpty( protoPacketKey ) ) {
			if ( String.IsNullOrEmpty( packetKey ) ) {
				LoadConstants();
			}
			protoPacketKey = packetKey;
		}
		packetKey = protoPacketKey;
	}
	void setTextKey( string text ) {
		// Debug.Log( "Setting text key from " + text );
		//CryptUtil__set_textKey(,tek,) [0x0109fd62]:
		// pk = CryptUtil__get_packetKey
		string pk = getPacketKey();
		// Debug.Log( "packetKey: " + packetKey );
		// textKey_ = CryptUtil__AESEncrypt(,tek,pk,)[0x0109fe6f]:
		//			btSrc = CryptUtil__ConvertStringToByteArray(,tek,)
		System.Text.UnicodeEncoding unicodeEncoding = new System.Text.UnicodeEncoding( false, true, false );
		byte[] textBytes = unicodeEncoding.GetBytes( text );
		// Debug.Log( "tek.Length: " + text.Length );
		// Debug.Log( "textBytes.Length: " + textBytes.Length );
		//			btSrc = CryptUtil__AESEncrypt(,btSrc,pk,false,)[0x010a20fd]
		System.Text.ASCIIEncoding asciiEncoding = new System.Text.ASCIIEncoding();
		byte[] keyBytes = asciiEncoding.GetBytes( pk );
		// Debug.Log( "keyBytes.Length: " + keyBytes.Length );
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
		textKey_ = Convert.ToBase64String( textBytes );
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
	void showChanges() {
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
