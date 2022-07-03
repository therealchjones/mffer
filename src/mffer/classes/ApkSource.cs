using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using GooglePlayStoreApi;

namespace Mffer {
	public class ApkSource {
		GooglePlayStoreClient PlayStore;
		public ApkSource( AndroidCredentials credentials ) {
			if ( credentials is null ) throw new ArgumentNullException( "credentials" );
			string email = credentials.GetValue( "Email" );
			string androidId = credentials.GetValue( "AndroidId" );
			string token = credentials.GetValue( "OauthToken" );
			string appName = "com.google.android.gm";

			PlayStore = new GooglePlayStoreClient( email, androidId );
			//token = PlayStore.GetGoogleToken( token ).GetAwaiter().GetResult();
			token = "aas_et/" + token;
			PlayStore.GetGoogleAuth( token ).Wait();

			GooglePlayStore.DetailsResponse response = PlayStore.AppDetail( appName ).GetAwaiter().GetResult();
			int versionCode = response.Item.Details.AppDetails.VersionCode;
			string versionString = response.Item.Details.AppDetails.VersionString;
			int offerType = response.Item.Offer[0].OfferType;
			PlayStore.Purchase( appName, offerType, versionCode );
			byte[] apkBytes = PlayStore.DownloadApk( appName ).GetAwaiter().GetResult();
			File.WriteAllBytes( $"{appName}.apk", apkBytes );
		}
		string EncryptCredentials( string email, string password ) {
			if ( String.IsNullOrEmpty( email ) || String.IsNullOrEmpty( password ) ) throw new ArgumentNullException();
			string publicKey = "AAAAgMom/1a/v0lblO2Ubrt60J2gcuXSljGFQXgcyZWveWLEwo6prwgi3iJIZdodyhKZQrNWp5nKJ3srRXcUW+F1BD3baEVGcmEgqaLZUNBjm057pKRI16kB0YppeGx5qIQ5QjKzsR8ETQbKLNWgRY0QRNVz34kMJR3P/LgHax/6rmf5AAAAAwEAAQ==";
			byte[] publicKeyBytes = Convert.FromBase64String( publicKey );
			int modulusLength = System.Net.IPAddress.NetworkToHostOrder( BitConverter.ToInt32( publicKeyBytes, 0 ) );
			byte[] modulus = new ArraySegment<byte>( publicKeyBytes, 4, modulusLength ).ToArray();
			int exponentLength = System.Net.IPAddress.NetworkToHostOrder( BitConverter.ToInt32( publicKeyBytes, 4 + modulusLength ) );
			byte[] exponent = new ArraySegment<byte>( publicKeyBytes, 4 + modulusLength + 4, exponentLength ).ToArray();
			RSACryptoServiceProvider encryptor = new RSACryptoServiceProvider();
			encryptor.ImportParameters( new RSAParameters { Exponent = exponent, Modulus = modulus } );

			byte[] header = new byte[5];
			header[0] = 0;
			byte[] sig = SHA1.Create().ComputeHash( publicKeyBytes );
			Array.Copy( sig, 0, header, 1, 4 );

			byte[] emailBytes = System.Text.Encoding.UTF8.GetBytes( email );
			byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes( password );
			byte[] rawCredentials = new byte[emailBytes.Length + 1 + passwordBytes.Length];
			Array.Copy( emailBytes, rawCredentials, emailBytes.Length );
			rawCredentials[emailBytes.Length] = 0;
			Array.Copy( passwordBytes, 0, rawCredentials, emailBytes.Length + 1, passwordBytes.Length );

			byte[] encryptedCredentials = encryptor.Encrypt( rawCredentials, RSAEncryptionPadding.OaepSHA1 );
			byte[] messageBytes = new byte[encryptedCredentials.Length + 5];
			Array.Copy( header, messageBytes, header.Length );
			Array.Copy( encryptedCredentials, 0, messageBytes, header.Length, encryptedCredentials.Length );
			string message = Convert.ToBase64String( messageBytes );
			return message.Replace( '+', '-' ).Replace( '/', '_' );
		}
		internal class PasswordCryptor {
			private static byte[] RSAEncrypt( byte[] modulus, byte[] exponent, byte[] bytes ) {
				using ( var rsa = new RSACryptoServiceProvider() ) {
					rsa.ImportParameters( new RSAParameters {
						Modulus = modulus,
						Exponent = exponent
					} );

					return rsa.Encrypt( bytes, true );
				}
			}
			private static string GetUrlSafeBase64( string str ) {
				return str
					.Replace( "+", "-" )
					.Replace( "/", "_" );
			}
			public static string EncryptPassword( string email, string password ) {
				var strPublicKey = "AAAAgMom/1a/v0lblO2Ubrt60J2gcuXSljGFQXgcyZWveWLEwo6prwgi3iJIZdodyhKZQrNWp5nKJ3srRXcUW+F1BD3baEVGcmEgqaLZUNBjm057pKRI16kB0YppeGx5qIQ5QjKzsR8ETQbKLNWgRY0QRNVz34kMJR3P/LgHax/6rmf5AAAAAwEAAQ==";
				var publicKeyBytes = Convert.FromBase64String( strPublicKey );
				var modulus = new byte[] { 0xCA, 0x26, 0xFF, 0x56, 0xBF, 0xBF, 0x49, 0x5B, 0x94, 0xED, 0x94, 0x6E, 0xBB, 0x7A, 0xD0, 0x9D, 0xA0, 0x72, 0xE5, 0xD2, 0x96, 0x31, 0x85, 0x41, 0x78, 0x1C, 0xC9, 0x95, 0xAF, 0x79, 0x62, 0xC4, 0xC2, 0x8E, 0xA9, 0xAF, 0x08, 0x22, 0xDE, 0x22, 0x48, 0x65, 0xDA, 0x1D, 0xCA, 0x12, 0x99, 0x42, 0xB3, 0x56, 0xA7, 0x99, 0xCA, 0x27, 0x7B, 0x2B, 0x45, 0x77, 0x14, 0x5B, 0xE1, 0x75, 0x04, 0x3D, 0xDB, 0x68, 0x45, 0x46, 0x72, 0x61, 0x20, 0xA9, 0xA2, 0xD9, 0x50, 0xD0, 0x63, 0x9B, 0x4E, 0x7B, 0xA4, 0xA4, 0x48, 0xD7, 0xA9, 0x01, 0xD1, 0x8A, 0x69, 0x78, 0x6C, 0x79, 0xA8, 0x84, 0x39, 0x42, 0x32, 0xB3, 0xB1, 0x1F, 0x04, 0x4D, 0x06, 0xCA, 0x2C, 0xD5, 0xA0, 0x45, 0x8D, 0x10, 0x44, 0xD5, 0x73, 0xDF, 0x89, 0x0C, 0x25, 0x1D, 0xCF, 0xFC, 0xB8, 0x07, 0x6B, 0x1F, 0xFA, 0xAE, 0x67, 0xF9 };
				var exponent = new byte[] { 0x01, 0x00, 0x01 };

				var header = Enumerable.Concat( new byte[] { 0x00 }, SHA1.Create().ComputeHash( publicKeyBytes ).Take( 4 ).ToArray() );
				var result = Enumerable.Concat( header, RSAEncrypt( modulus, exponent, Encoding.ASCII.GetBytes( $"{email}\0{password}" ) ) );

				string final = GetUrlSafeBase64( Convert.ToBase64String( result.ToArray() ) );
				return final;
			}
		}
	}
}
