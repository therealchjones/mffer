using Mffer;

namespace MyProgram {
	/// <summary>
	/// The example application class
	/// </summary>
	public class ExampleProgram {
		/// <summary>
		/// The entry point for the <see cref="Program"/>
		/// </summary>
		/// <remarks>
		/// Within <see cref="main(string[])"/>, commands similar to those
		/// available on the command line may be used. Additionally, the
		/// mffer library API may be used to add new components to the data
		/// extraction and reporting process.
		/// </remarks>
		/// <param name="args">The command-line arguments</param>
		/// <returns>0 if there are no errors</returns>
		/// <seealso cref="Game"/>
		/// <seealso cref="Component"/>
		public int main( string[] args ) {

			Component myComponent = new Component( "My Component" );

			return 0;
		}
	}
}
