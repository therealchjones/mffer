using System;
using System.Collections.Generic;

namespace CommandLine {
	/// <summary>
	/// Represents a model for describing a command line
	/// </summary>
	/// <remarks>
	/// The <see cref="Usage"/> class allows developers to describe
	/// what command line arguments should be expected, the application
	/// to evaluate command line arguments, and users to discover appropriate
	/// arguments to pass. It provides methods for getting and setting
	/// expected options and parameters, as well as for providing that
	/// information to users and applications.
	/// </remarks>
	public class Usage {
		/// <summary>
		/// Gets or sets the list of valid command line options
		/// </summary>
		List<string> Options { get; set; }
		/// <summary>
		/// Adds a valid <paramref name="option"/>
		/// </summary>
		/// <remarks>
		/// <see cref="AddOption"/> allows <paramref name="option"/> to be used
		/// as a command line option. If <paramref name="option"/> has one or
		/// more leading '-'s, they are removed before adding, and the proper
		/// number is required on the command line.
		/// </remarks>
		/// <param name="option">The string to add as an option</param>
		public void AddOption( string option ) {

		}
		/// <summary>
		/// Writes a standardized usage message, including options and
		/// parameters, to a string
		/// </summary>
		public override string ToString() {
			return "Usage: mffer [-h] --datadir data_directory --savedir output_directory";
		}
	}
	/// <summary>
	/// Provides command line argument access and (optional) error checking.
	/// </summary>
	/// <remarks>
	/// The <see cref="Parser"/> class evaluates the command line to
	/// categorize arguments as the <see cref="Program"/> name,
	/// <see cref="Options"/> (typically a string with a leading dash and a
	/// possible associated option-argument), and <see cref="Operands"/>,
	/// arguments that come after all options. Parsing is performed based upon
	/// POSIX syntax guidelines
	/// (<see href="https://pubs.opengroup.org/onlinepubs/9699919799/basedefs/V1_chap12.html#tag_12_02"/>)
	/// and (optionally) upon command syntax from a <see cref="Usage"/>
	/// instance.
	/// </remarks>
	class Parser {
		/// <summary>
		/// Gets or sets the name of the program given on the command line.
		/// </summary>
		/// <remarks>
		/// <see cref="Program"/> is used to refer to the program using the
		/// same terminology as the calling user. In other command-line
		/// savvy languages, this is often referred to as argv[0] or $0 or
		/// similarly. It is equivalent to
		/// <see cref="Environment.GetCommandLineArgs()"/>[0].
		/// </remarks>
		string Program { get; set; }
		/// <summary>
		/// Gets or sets the list of all command-line arguments in order
		/// </summary>
		/// <remarks>
		/// The list of <see cref="Arguments"/> is equivalent to that
		/// returned by the <see cref="Environment.GetCommandLineArgs()"/>
		/// method.
		/// </remarks>
		List<string> Arguments { get; set; }
		/// <summary>
		/// Gets or sets a dictionary of options set on the command line
		/// and any associated option arguments
		/// </summary>
		/// <remarks>
		/// When the <see cref="Parser.Parse()"/> method
		/// is run, <see cref="Arguments"/> from the command line are
		/// evaluated and those determined to be options are loaded as
		/// keys of the <see cref="Options"/> property. If an option is
		/// associated with an option argument, it is loaded as the value
		/// associated with the option's key.
		/// </remarks>
		Dictionary<string, string> Options { get; set; }
		/// <summary>
		/// Gets or sets the list of command-line operands
		/// </summary>
		/// <remarks>
		/// Command line arguments that follow the list of
		/// <see cref="Options"/> are stored, in order, in the
		/// <see cref="Operands"/> list.
		/// </remarks>
		List<string> Operands { get; set; }
		/// <summary>
		/// Gets or sets the <see cref="Usage"/> instance to be used for
		/// parsing assistance and validation.
		/// </summary>
		/// <remarks>If <see cref="CommandUsage"/> is null, the command line
		/// arguments will be parsed only according to assumptions made from
		/// the POSIX syntax guidelines.
		/// </remarks>
		/// <seealso href="https://pubs.opengroup.org/onlinepubs/9699919799/basedefs/V1_chap12.html#tag_12_02"/>
		Usage CommandUsage { get; set; }
		/// <summary>
		/// Initializes a new instance of the
		/// <see cref="Parser"/> class and parses the command line
		/// </summary>
		/// <remarks>
		/// <para>Two forms of the <see cref="Parser"/> constructor are
		/// available. The simplest form, in which no <paramref name="usage"/>
		/// parameter is passed to the constructor, performs parsing of the
		/// command line arguments into <see cref="Options"/> (and
		/// option-arguments) and <see cref="Operands"/>, but does no
		/// validation to determine whether those arguments are expected or
		/// properly formatted.
		/// </para>
		/// <para><see cref="Parser.Parser(Usage)"/> loads the
		/// <see cref="Options"/> and <see cref="Operands"/> only if they are
		/// expected and described in <paramref name="usage"/>, and throws an
		/// exception otherwise.</para>
		/// <para>(Actually, doesn't do this yet, but that's the plan.)</para>
		/// </remarks>
		public Parser( Usage usage = null ) {
			Arguments = new List<string>();
			Arguments.AddRange( Environment.GetCommandLineArgs() );
			Program = Arguments[0];
			Options = new Dictionary<string, string>();
			Operands = new List<string>();
			CommandUsage = usage;
			Parse();
		}
		/// <summary>
		/// Evaluates the command line <see cref="Arguments"/> to load
		/// <see cref="Options"/> and <see cref="Operands"/>
		/// </summary>
		void Parse() {
			for ( int i = 1; i < Arguments.Count; i++ ) {
				string option = null;
				switch ( Arguments[i] ) {
					case "--":
						if ( i != Arguments.Count - 1 ) {
							Operands.AddRange( Arguments.GetRange( i + 1, Arguments.Count - i - 1 ) );
						}
						i = Arguments.Count;
						break;
					case "-h":
					case "-?":
					case "--help":
					case "/h":
					case "/H":
					case "/?":
						break;
					case "--datadir":
					case "--savedir":
					case "--outputdir":
						option = Arguments[i].Substring( 2 );
						if ( i == Arguments.Count - 1 ) {
							throw new ArgumentException( $"'{option}' requires an argument" );
						} else if ( Options.ContainsKey( option ) ) {
							throw new ArgumentException( $"'{option}' cannot be specified more than once" );
						} else {
							i++;
							Options.Add( option, Arguments[i] );
						}
						break;
					default:
						option = Arguments[i];
						throw new ArgumentException( $"Unknown argument: '{option}'" );
				}
			}
		}
		/// <summary>
		/// Returns the argument associated with a given option
		/// </summary>
		/// <param name="option">The option</param>
		/// <returns>The <paramref name="option"/>'s argument</returns>
		/// <exception name="ApplicationException">Thrown if
		/// <paramref name="option"/> is not defined</exception>
		public string GetOption( string option ) {
			if ( Options.ContainsKey( option ) ) {
				string optionArgument = Options[option];
				return optionArgument;
			} else {
				throw new ApplicationException( $"Option '{option}' not defined" );
			}
		}
	}
}
