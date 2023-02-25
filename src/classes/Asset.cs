using System;
using System.Text;

namespace Mffer {
	/// <summary>
	/// Represents a single asset used by Marvel Future Fight
	/// </summary>
	/// <remarks>As Unity's Asset class is primarily for using an asset within
	/// the Unity Editor, this class is modeled on the needs for <see
	/// cref="Asset"/> use within games and in the mffer project. The class
	/// includes members used to access and manipulate data from Unity assets.
	/// </remarks>
	public class Asset : GameObject {
		/// <summary>
		/// Gets or sets the <see cref="Asset"/>'s path ID
		/// </summary>
		public long PathID { get; set; }
		/// <summary>
		/// Gets or sets the name of this <see cref="Asset"/>
		/// </summary>
		public string Name { get; set; }
		/// <summary>
		/// Initialize a new <see cref="Asset"/> instance with the given
		/// <paramref name="pathID"/>
		/// </summary>
		/// <param name="pathID">Path ID of the asset</param>
		public Asset( long pathID ) : base() {
			PathID = pathID;
		}
		/// <summary>
		/// Obtain CSV data from assets that are thinly encoded CSVs
		/// </summary>
		/// <remarks>
		/// Adapted from MFF CryptUtil_AESDecryptText()
		/// </remarks>
		/// <returns><see cref="String"/> containing the CSV (tab-delimited) data</returns>
		/// <exception cref="System.Exception"></exception>
		public string GetCsv() {
			if ( Value is null ) throw new System.Exception( "Not a CSV asset" );
			string encryptedCsv = GetValue( "m_Script" );
			int bufferLength = encryptedCsv.Length % 4;
			if ( bufferLength != 0 ) bufferLength = 4 - bufferLength;
			for ( int i = 0; i < bufferLength; i++ ) {
				encryptedCsv = string.Concat( encryptedCsv, "=" );
			}
			byte[] encryptedBytes = Convert.FromBase64String( encryptedCsv );
			byte delimiter = Encoding.Unicode.GetBytes( "\t" )[0];
			string text = "";
			foreach ( byte b in encryptedBytes ) {
				if ( b == delimiter ) {
					text = Encoding.Unicode.GetString( encryptedBytes );
					break;
				}
			}
			if ( !String.IsNullOrEmpty( text ) )
				text = CryptUtil.Decrypt( encryptedBytes, Encoding.ASCII.GetBytes( NetworkData.GetTextKey() ), new byte[16] );
			return text;
		}
	}
}
