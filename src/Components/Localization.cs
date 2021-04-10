using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Mffer {
	/// <summary>
	/// Represents a string localiization dictionary
	/// </summary>
	/// <remarks>
	/// The <see cref="Localization"/> class is a derivative of
	/// <see cref="Component"/> that provides access to the
	/// <see cref="Version"/>'s string localization dictionary. This includes
	/// methods to build the dictionary from the appropriate
	/// <see cref="AssetFile"/>, translate encoded strings into localized
	/// strings, and output the full dictionary as a JSON object.
	/// </remarks>
	public class Localization : Component {
		/// <summary>
		/// Gets or sets the dictionary object
		/// </summary>
		Dictionary<string, string> LocalDictionary { get; set; }
		/// <summary>
		/// Gets or sets the name of the localization language
		/// </summary>
		string Language { get; set; }
		/// <summary>
		/// Initializes a new instance of the <see cref="Localization"/>
		/// <see cref="Component"/>-derived class.
		/// </summary>
		public Localization() : base() {
			Name = "Localization";
			LocalDictionary = new Dictionary<string, string>();
			Language = "en";
			AddBackingAsset( $"localization/localization_{Language}.csv||LocalizationTable_{Language}" );
		}
		/// <summary>
		/// Determines whether the <see cref="Localization"/> has been
		/// loaded.
		/// </summary>
		/// <returns><c>true</c> if the <see cref="Localization"/> already
		/// contains loaded data, <c>false</c> otherwise.</returns>
		/// <seealso cref="Component.IsLoaded()"/>
		public override bool IsLoaded() {
			return LocalDictionary.Count != 0;
		}
		/// <summary>
		/// Loads data into this <see cref="Localization"/>
		/// </summary>
		/// <seealso cref="Component.Load()"/>
		public override void Load() {
			base.Load();
			if ( IsLoaded() ) return;
			AssetObject DictionaryAsset = (AssetObject)BackingAssets.First().Value;
			// the localization dictionary was a CSV in 6.2.0, but is in an asset in
			// 6.7.0; will have to manage differently
			if ( BackingAssets.First().Key.EndsWith( ".csv", StringComparison.InvariantCultureIgnoreCase ) ) {
				foreach ( AssetObject entry in DictionaryAsset.Properties["m_Script"].Array ) {
					LocalDictionary[entry.Properties["KEY"].String] = entry.Properties["TEXT"].String;
				}
			} else {
				Dictionary<string, string> keys = new Dictionary<string, string>();
				Dictionary<string, string> values = new Dictionary<string, string>();
				foreach ( int keyNum in Enumerable.Range( 0, DictionaryAsset.Properties["keyTable"].Properties["keys"].Properties["Array"].Array.Count() ) ) {
					keys.Add( DictionaryAsset.Properties["keyTable"].Properties["keys"].Properties["Array"].Array[keyNum].Properties["data"].String,
						DictionaryAsset.Properties["keyTable"].Properties["values"].Properties["Array"].Array[keyNum].Properties["data"].String );
				}
				foreach ( int keyNum in Enumerable.Range( 0, DictionaryAsset.Properties["valueTable"].Properties["keys"].Properties["Array"].Array.Count() ) ) {
					values.Add( DictionaryAsset.Properties["valueTable"].Properties["keys"].Properties["Array"].Array[keyNum].Properties["data"].String,
						DictionaryAsset.Properties["valueTable"].Properties["values"].Properties["Array"].Array[keyNum].Properties["data"].String );
				}
				if ( new HashSet<string>( keys.Values ).Count() == values.Count() ) {
					LocalDictionary = Enumerable.Range( 0, keys.Count() ).ToDictionary(
						i => keys.Keys.ToList()[i],
						i => values[keys.Values.ToList()[i]] );
				} else {
					throw new Exception( "Unable to build localization dictionary; invalid entries" );
				}
			}
		}
		/// <summary>
		/// Decodes a string using the <see cref="Localization"/> dictionary
		/// </summary>
		/// <param name="input">An encoded string to be decoded</param>
		/// <returns>The decoded and localized string</returns>
		public string GetString( string input ) {
			if ( BackingAssets.First().Key.EndsWith( ".csv",
				StringComparison.InvariantCultureIgnoreCase ) ) {
				return LocalDictionary[input];
			} else {
				return LocalDictionary[MakeHash( input )];
			}
		}
		/// <summary>
		/// Creates a reproducible numeric hash from a string
		/// </summary>
		/// <remarks>
		/// Recent versions of Marvel Future Fight use a dictionary with hashed
		/// strings as keys rather than a flat CSV file for the localization
		/// asset. <see cref="Localization.MakeHash(string)"/> calculates that
		/// hash given the non-localized <paramref name="input"/> string.
		/// </remarks>
		/// <param name="input">The string to be hashed</param>
		/// <returns>The hashed string</returns>
		string MakeHash( string input ) {
			int result = 0;
			char[] textBytes = input.ToCharArray();
			int i = 0;
			int length = textBytes.Length;
			int thisCharIndex = 0;
			if ( i < length - 1 ) {
				int nextCharIndex = 1;
				do {
					byte thisChar = Convert.ToByte( textBytes[thisCharIndex] );
					byte nextChar = Convert.ToByte( textBytes[nextCharIndex] );
					int subresult = ( ( ( result << 5 ) - result ) + Convert.ToInt32( thisChar ) );
					result = ( subresult << 5 ) - subresult + Convert.ToInt32( nextChar );
					i = i + 2;
					thisCharIndex = i;
					nextCharIndex = i + 1;
				}
				while ( i < length - 1 );
			}
			if ( i < length ) {
				result = ( ( result << 5 ) - result ) + Convert.ToInt32( textBytes[thisCharIndex] );
			}
			return result.ToString();
		}
		/// <summary>
		/// Outputs data from this <see cref="Localization"/> in JSON format
		/// </summary>
		/// <param name="file"><see cref="System.IO.StreamWriter"/> stream to
		/// which to write</param>
		/// <param name="tabs">Baseline number of tab characters to insert
		/// before each line of output</param>
		/// <seealso cref="Game.Version.WriteJson(StreamWriter, int)"/>
		public override void WriteJson( StreamWriter file, int tabs = 0 ) {

		}
	}
}
