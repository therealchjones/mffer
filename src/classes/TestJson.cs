using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Mffer {
	/// <summary>
	/// Class used for testing Json output
	/// </summary>
	public static class TestJson {
		/// <summary>
		/// Creates an example object and writes the Json serialization of it
		/// </summary>
		/// <remarks>
		/// Creates a "Game" object of this form:
		/// <code>
		/// Game game = {
		/// 	string Name = "test game";
		/// 	List&lt;Version&gt; Versions = { {
		/// 		string Name = "test-version";
		/// 		Dictionary&lt;string,Component&gt; Components = {
		/// 			{ "Localization",
		/// 				Localization localization = {
		/// 					Dictionary&lt;string,string&gt; LocalDictionary = {
		/// 						{ "foo", "bar" },
		/// 						{ "baz", "bum" }
		/// 					};
		/// 					GameObject Value =
		/// 						List&lt;GameObject&gt; Value = {
		/// 							Asset asset = {
		/// 								string Value = "deep!";
		/// 								long PathID = 3;
		/// 							};
		/// 						};
		/// 					};
		/// 				};
		/// 			};
		/// 		};
		/// 	} };
		/// };
		/// </code>
		/// but the LocalDictionary and PathID properties are not printed; of note,
		/// the Components property is identified as <c>Dictionary&lt;string,Component&gt;</c>, so
		/// perhaps the Localization is being interpreted as a Component rather than
		/// as a Localization, and then similarly the Asset is being interpreted as a
		/// GameObject rather than an Asset. This is true whether we use the
		/// <c>JsonSerializer.Serialize(game)</c> or the
		/// <c>JsonSerializer.Serialze(game,game.GetType())</c> form. If we make a custom
		/// converter, it is called for items declared as the specific associated type (GameObject) but not derivatives
		/// (Component), so it works for the Asset but not the Localization (when declared as a
		/// converter for GameObject).
		/// </remarks>
		/// <seealso href="https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-polymorphism"/>
		public static void Test() {
			Game game = new Game( "test game" );
			Version version = new Version( "test-version" );
			Localization localization = new Localization();
			localization.LocalDictionary = new Dictionary<string, string>();
			localization.LocalDictionary.Add( "foo", "bar" );
			localization.LocalDictionary.Add( "baz", "bum" );
			string soDeep = "deep!";
			Asset asset = new Asset( 3 );
			asset.Value = soDeep;
			List<GameObject> assetList = new List<GameObject> { asset };
			localization.Value = assetList;
			version.Components["Localization"] = localization;
			game.Versions.Add( version );

			JsonSerializerOptions serialOptions = new JsonSerializerOptions {
				WriteIndented = true
			};
			JsonWriterOptions writeOptions = new JsonWriterOptions() { Indented = true, SkipValidation = true };

			Console.WriteLine( "JsonSerializer.Serialize( game ):" );
			Console.WriteLine( JsonSerializer.Serialize( game, serialOptions ) );

			Console.WriteLine( "JsonSerializer.Serialize( game, game.GetType() ):" );
			Console.WriteLine( JsonSerializer.Serialize( game, game.GetType(), serialOptions ) );

			serialOptions = new JsonSerializerOptions( serialOptions );
			serialOptions.Converters.Add( new GameObjectJsonConverter() );
			Console.WriteLine( "JsonSerializer.Serialize( game ) with custom converter:" );
			Console.WriteLine( JsonSerializer.Serialize( game, serialOptions ) );

			using ( Stream console = Console.OpenStandardOutput() ) {
				Console.WriteLine( "game.ToJson(): " );
				game.ToJson( console, serialOptions, writeOptions );

				Console.WriteLine( "version.ToJson():" );
				version.ToJson( console, serialOptions, writeOptions );
			}
			// InvalidOperationException:
			// serialOptions.Converters.Add( new GameObjectJsonConverter() );
		}
	}
}
