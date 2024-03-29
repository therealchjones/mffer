using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Mffer {
	/// <summary>
	/// Represents a string localiization dictionary
	/// </summary>
	/// <remarks>
	/// The <see cref="Localization"/> class is a derivative of
	/// <see cref="Component"/> that provides access to the
	/// <see cref="Version"/>'s string localization dictionary. This includes
	/// methods to build the dictionary from the appropriate
	/// <see cref="AssetBundle"/>, translate encoded strings into localized
	/// strings, and output the full dictionary as a JSON object.
	/// </remarks>
	public class Localization : Component {
		/// <summary>
		/// Gets or sets the dictionary object
		/// </summary>
		public Dictionary<string, string> LocalDictionary { get; set; }
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
			AddBackingData( $"localization/localization_{Language}.csv||localization/localization_{Language}.asset" );
		}
		/// <summary>
		/// Determines whether the <see cref="Localization"/> has been
		/// loaded.
		/// </summary>
		/// <returns><c>true</c> if the <see cref="Localization"/> already
		/// contains loaded data, <c>false</c> otherwise.</returns>
		/// <seealso cref="Component.IsLoaded()"/>
		public override bool IsLoaded() {
			if ( BackingData is not null
				&& BackingData.Count > 0
				&& BackingData.First().Value is not null )
				return true;
			return false;
		}
		/// <summary>
		/// Loads asset data for this <see cref="Localization"/>
		/// </summary>
		/// <seealso cref="Component.Load()"/>
		public override void Load() {
			if ( IsLoaded() ) return;
			base.Load();
		}
		/// <summary>
		/// Loads all asset data for this <see cref="Localization"/> into the component
		/// </summary>
		/// <exception cref="InvalidOperationException"></exception>
		/// <exception cref="Exception"></exception>
		public override void LoadAll() {
			base.Load();
			dynamic asset = BackingData.First().Value;
			if ( asset is null ) throw new InvalidOperationException( $"Unable to load localization; no asset loaded." );
			if ( BackingData.First().Key.EndsWith( ".csv", StringComparison.InvariantCultureIgnoreCase ) ) {
				if ( asset.GetValue( "m_Script" ) is String csv ) {
					using ( JsonDocument dictionary = JsonDocument.Parse( CSVtoJson( csv ) ) ) {
						foreach ( JsonElement entry in dictionary.RootElement.EnumerateArray() ) {
							LocalDictionary[entry.GetProperty( "KEY" ).GetString()] = entry.GetProperty( "TEXT" ).GetString();
						}
					}
				} else {
					throw new Exception( "Unable to parse dictionary." );
				}
			} else {
				Dictionary<string, string> keys = new Dictionary<string, string>();
				Dictionary<string, string> values = new Dictionary<string, string>();
				dynamic assetKeys = asset.keyTable.keys;
				dynamic assetValues = asset.keyTable.values;
				foreach ( int keyNum in Enumerable.Range( 0, assetKeys.Count() ) ) {
					keys.Add( assetKeys[keyNum].ToString(),
						assetValues[keyNum].ToString() );
				}
				assetKeys = asset.valueTable.keys;
				assetValues = asset.valueTable.values;
				foreach ( int keyNum in Enumerable.Range( 0, assetKeys.Count() ) ) {
					values.Add( assetKeys[keyNum].ToString(),
						assetValues[keyNum].ToString() );
				}
				if ( new HashSet<string>( keys.Values ).Count == values.Count ) {
					LocalDictionary = Enumerable.Range( 0, keys.Count ).ToDictionary(
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
			if ( LocalDictionary.ContainsKey( input ) ) return LocalDictionary[input];
			string value;
			if ( BackingData.First().Key.EndsWith( ".csv", StringComparison.InvariantCultureIgnoreCase ) )
				value = GetStringFromCsv( input );
			else
				value = GetStringFromAsset( input );
			LocalDictionary.Add( input, value );
			return value;
		}
		string GetStringFromCsv( string input ) {
			dynamic asset = BackingData.First().Value;
			if ( asset.GetValue( "m_Script" ) is String csv ) {
				using ( JsonDocument dictionary = JsonDocument.Parse( CSVtoJson( csv ) ) ) {
					foreach ( JsonElement entry in dictionary.RootElement.EnumerateArray() ) {
						if ( entry.GetProperty( "KEY" ).GetString() == input )
							return entry.GetProperty( "TEXT" ).GetString();
					}
				}
				throw new KeyNotFoundException( input );
			} else {
				throw new InvalidOperationException( "Unable to parse dictionary." );
			}
		}
		string GetStringFromAsset( string input ) {
			dynamic asset = BackingData.First().Value;
			List<GameObject> keys = asset.keyTable.keys.Value;
			int numKeys = keys.Count;
			string hash = input;
			if ( keys[1].Value != "PACKAGE_01" )
				hash = MakeHash( input );
			bool found = false;
			int valueIndex = 0;
			for ( int i = 0; i < numKeys; i++ ) {
				if ( keys[i].Value == hash ) {
					found = true;
					valueIndex = Convert.ToInt32( asset.keyTable.values[i].Value );
					break;
				}
			}
			if ( !found ) throw new KeyNotFoundException( input );
			List<GameObject> valueIndices = asset.valueTable.keys.Value;
			if ( valueIndices[valueIndex - 1].Value == valueIndex.ToString() ) {
				return asset.valueTable.values[valueIndex - 1].Value;
			}
			for ( int i = 0; i < valueIndices.Count; i++ ) {
				if ( valueIndices[i].Value == valueIndex.ToString() ) {
					return asset.valueTable.values[i].Value;
				}
			}
			throw new InvalidOperationException( "Localization dictionary has unrecognized format" );
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
	}
}
