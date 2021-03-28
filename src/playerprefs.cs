using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Xml;
using MessagePack;


namespace Mffer {
	class PlayerPrefs {

		void GetPrefs() {
			using ( StreamReader file = new StreamReader( "/Users/chjones/Development/Marvel Future Fight/device-files/MFF-device-6.9.0/data/data/com.netmarble.mherosgb/shared_prefs/com.netmarble.mherosgb.v2.playerprefs.xml" ) ) {
				XmlDocument xmlDocument = new XmlDocument();
				xmlDocument.Load( file );
				XmlElement root = xmlDocument.DocumentElement;
				foreach ( XmlElement node in root.ChildNodes ) {
					string name = Uri.UnescapeDataString( node.GetAttribute( "name" ) );
					string value = Uri.UnescapeDataString( node.InnerText );
					try {
						name = Encoding.Unicode.GetString( Convert.FromBase64String( name ) );
					} catch ( System.FormatException ) {

					}
					Console.WriteLine( name );

					if ( name == "FREE_GIFT_TABLE" ) {
						try {
							byte[] bytes = Convert.FromBase64String( value );
							value = MessagePackSerializer.ConvertToJson( bytes );
						} catch ( System.FormatException ) {

						}
						JsonDocument json = JsonDocument.Parse( value );
						Console.WriteLine( $"{name}: {value}" );
					}
				}
			}
		}
	}
}
