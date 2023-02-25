using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Mffer {
	/// <summary>
	/// Static class containing methods to encrypt and decrypt dext
	/// </summary>
	/// <remarks>Adapted from the MFF class of the same name</remarks>
	public static class CryptUtil {
		/// <summary>
		/// Encrypt the given byte array using the AES algorithm and the given
		/// key and an initialization vector that is the same as the key
		/// </summary>
		/// <param name="decrypted">the byte array to encrypt</param>
		/// <param name="key">a string of the AES key to use</param>
		/// <returns>a byte array of the encrypted message</returns>
		public static string AesEncrypt( string decrypted, string key ) {
			ASCIIEncoding asciiEncoding = new ASCIIEncoding();
			byte[] rijKey = asciiEncoding.GetBytes( key );
			UnicodeEncoding unicodeEncoding = new UnicodeEncoding();
			byte[] decryptedBytes = unicodeEncoding.GetBytes( decrypted );
			byte[] encryptedBytes = AesEncrypt( decryptedBytes, rijKey );
			return Convert.ToBase64String( encryptedBytes );
		}
		/// <summary>
		/// Encrypt the given byte array using the AES algorithm and the given key and initialization vector
		/// </summary>
		/// <param name="decrypted">the byte array to encrypt</param>
		/// <param name="key">a string of the AES key to use</param>
		/// <param name="isKeyIvSame"><c>true</c> the the initialization vector should be the same as the key; if <c>false</c>, a "blank" 16-byte array will be used as the IV</param>
		/// <returns>a byte array of the encrypted message</returns>
		public static byte[] AesEncrypt( byte[] decrypted, string key, bool isKeyIvSame = true ) {
			byte[] keyBytes = ( new ASCIIEncoding() ).GetBytes( key );
			return AesEncrypt( decrypted, keyBytes, isKeyIvSame );
		}
		/// <summary>
		/// Encrypt the given byte array using the AES algorithm and the given key and initialization vector
		/// </summary>
		/// <param name="decrypted">the byte array to encrypt</param>
		/// <param name="key">a byte array of the AES key to use</param>
		/// <param name="isKeyIvSame"><c>true</c> the the initialization vector should be the same as the key; if <c>false</c>, a "blank" 16-byte array will be used as the IV</param>
		/// <returns>a byte array of the encrypted message</returns>
		public static byte[] AesEncrypt( byte[] decrypted, byte[] key, bool isKeyIvSame = true ) {
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
		/// <summary>
		/// Decrypt the encrypted byte array using an AES algorithm with the given key and initialization vector
		/// </summary>
		/// <param name="text">the encrypted byte array</param>
		/// <param name="key">the AES key</param>
		/// <param name="iv">the AES initialization vector</param>
		/// <returns>a string of the decrypted message</returns>
		public static string Decrypt( byte[] text, byte[] key, byte[] iv ) {
			using ( Aes rijAlg = Aes.Create() ) {
				rijAlg.KeySize = key.Length << 3;
				rijAlg.BlockSize = 128;
				rijAlg.Mode = CipherMode.CBC;
				rijAlg.Padding = PaddingMode.PKCS7;
				rijAlg.Key = key;
				rijAlg.IV = iv;
				ICryptoTransform decryptor = rijAlg.CreateDecryptor( key, iv );
				using ( MemoryStream msDecrypt = new MemoryStream( text ) ) {
					using ( CryptoStream csDecrypt = new CryptoStream( msDecrypt, decryptor, CryptoStreamMode.Read ) ) {
						using ( StreamReader srDecrypt = new StreamReader( csDecrypt ) ) {
							return srDecrypt.ReadToEnd();
						}
					}
				}
			}
		}
		/// <summary>
		/// Decrypt the encrypted byte array using an AES algorithm with the given key and initialization vector
		/// </summary>
		/// <param name="text">the encrypted byte array</param>
		/// <param name="key">the AES key</param>
		/// <param name="iv">the AES initialization vector</param>
		/// <returns>a byte array of the decrypted message</returns>
		static public byte[] DecryptBytes( byte[] text, byte[] key, byte[] iv ) {
			using ( Aes rijAlg = Aes.Create() ) {
				rijAlg.KeySize = key.Length << 3;
				rijAlg.BlockSize = 128;
				rijAlg.Mode = CipherMode.CBC;
				rijAlg.Padding = PaddingMode.PKCS7;
				rijAlg.Key = key;
				rijAlg.IV = iv;
				int chunkSize = 1000;
				ICryptoTransform decryptor = rijAlg.CreateDecryptor( key, iv );
				using ( MemoryStream msDecrypt = new MemoryStream( text ) ) {
					using ( CryptoStream csDecrypt = new CryptoStream( msDecrypt, decryptor, CryptoStreamMode.Read ) ) {
						using ( BinaryReader srDecrypt = new BinaryReader( csDecrypt ) ) {
							byte[] newBytes;
							List<byte> decryptedBytes = new List<byte>();
							while ( true ) {
								newBytes = srDecrypt.ReadBytes( chunkSize );
								if ( newBytes.Length > 0 ) {
									decryptedBytes.AddRange( newBytes );
								} else {
									break;
								}
							}
							return decryptedBytes.ToArray();
						}
					}
				}
			}
		}
		/// <summary>
		/// Calculate the MD5 hash of a string
		/// </summary>
		/// <param name="input">the string to evaluate</param>
		/// <returns>the MD5 hash of <paramref name="input"/> as a string</returns>
		public static string GetMD5( string input ) {
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
	}
}