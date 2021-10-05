/* A trivial dc-clone in C# */
using System;
using System.IO; //< Path interface
using System.Collections.Generic; //< List and Queue containers

// C uses a macro to help keep common application exit invocations clean
//   C# should do the same...
public enum EXIT_STATUS : int {
	SUCCESS = 0,
	FAILURE,
}

public class Program {
	public static int VERSION_MAJOR = 2021;
	public static int VERSION_MINOR = 278;
	public static int VERSION_REVISION = 1774023;

	private static List<decimal> TheStack = new List<decimal>();
	private static Queue<string> FileQ = new Queue<string>();

	private static void Help() {
		Console.WriteLine( "Usage: {0} [OPTION] [file ...]", 
			Path.GetFileName( 
				System.Reflection.Assembly.GetExecutingAssembly().CodeBase
			)
		);
		Console.WriteLine( "  -e, --expression=EXPR   evaluate expression" );
		Console.WriteLine( "  -f, --file=FILE         evaluate contents of file" );
		Console.WriteLine( "  -h, --help              display this help and exit" );
		Console.WriteLine( "  -V, --version           output version information and exit" );

		System.Environment.Exit( (int)EXIT_STATUS.SUCCESS );
	}

	private static void Version() {
		Console.WriteLine( "{0} V{1:D4}.{2:D3} R{3:D7}",
			Path.GetFileName(
				System.Reflection.Assembly.GetExecutingAssembly().CodeBase
			),
			VERSION_MAJOR,
			VERSION_MINOR,
			VERSION_REVISION
		);
		Console.WriteLine( "  A trivial dc clone in C#" );
		Console.WriteLine();
		Console.WriteLine( "Authored by Maurolepis Dreki as a demonstration;" );
		Console.WriteLine( "  NOT LICENSED FOR PERSONAL OR COMMERCIAL USE!" );
		Console.WriteLine( "  Please use GNU/dc instead for anything unrelated to evaluating the author's coding skills.");

		System.Environment.Exit( (int)EXIT_STATUS.SUCCESS );
	}

	private static void Eval( string line ) {
		Console.WriteLine( " CRITICAL: UNIMPLIMENTED: Eval" );
		Console.WriteLine( $"   {line}" );
	}

	private static void Read_FILE( string path ) {
		if( ! File.Exists( path ) ) {
			Console.WriteLine( "ERROR: no such file: {0}", path);
			return;
		}

		foreach( string line in File.ReadAllLines( path ) ) {
			Eval( line );
		}
	}

	private static void Read_STDIN() {
		const string prompt = ": ";
		string readLine;
		Console.Write( prompt );
		while( (readLine = Console.ReadLine()) != null ) {
			Eval( readLine );
			Console.Write( prompt );
		}
	}

  public static int Main(string[] args) {
    // Process Arguments
		for( int ix = 0; ix < args.Length; ix++ ) {
			// Check for Help invocation
			if( args[ix].StartsWith( "-h" ) || args[ix].Equals( "--help") ) {
				Help();
			// Check for Version invocation
			} else if( args[ix].StartsWith( "-V" ) || args[ix].Equals( "--version" ) ) {
				Version();
			// Check short file option
			} else if( args[ix].StartsWith( "-f" ) ) {
				try {
					FileQ.Enqueue( args[ix].Length > 2 ? args[ix].Substring( 2 ) : args[++ix] );
				} catch( IndexOutOfRangeException ) {
					Console.WriteLine( " FATAL: expected file, saw EOL" );
					System.Environment.Exit( (int)EXIT_STATUS.FAILURE );
				}
			// Check long file option
			} else if( args[ix].StartsWith( "--file" ) ) {
				try {
					// This version of the program doesn't actualy care what the delimiter is, just that it is there...
					FileQ.Enqueue( args[ix].Length > 6 ? args[ix].Substring( 7 ) : args[++ix] );
				} catch( IndexOutOfRangeException ) {
					Console.WriteLine( " FATAL: expected file, saw EOL" );
					System.Environment.Exit( (int)EXIT_STATUS.FAILURE );
				}
			// Check short eval option
			} else if( args[ix].StartsWith( "-e" ) ) {
				try {
					Eval( args[ix].Length > 2 ? args[ix].Substring( 2 ) : args[++ix] );
				} catch( IndexOutOfRangeException ) {
					Console.WriteLine( " FATAL: expected expression, saw EOL" );
					System.Environment.Exit( (int)EXIT_STATUS.FAILURE );
				}
			// Check long eval option
			} else if( args[ix].StartsWith( "--expression" ) ) {
				try {
					Eval( args[ix].Length > 12 ? args[ix].Substring( 13 ) : args[++ix] );
				} catch( IndexOutOfRangeException ) {
					Console.WriteLine( " FATAL: expected expression, saw EOL" );
					System.Environment.Exit( (int)EXIT_STATUS.FAILURE );
				}
			// Default: read arg as file
			} else {
				FileQ.Enqueue( args[ix] );
			}
		}

		// Default Mode: Read from stdin
		if( args.Length == 0 ) {
				Read_STDIN();
		}

		// Process files
		while( FileQ.Count > 0 ) {
				Read_FILE( FileQ.Dequeue() );
		}

		return (int)EXIT_STATUS.SUCCESS;
  }
}

/* CHANGE LOG:
 *   2021.10.04 1700->2000 (3hr):
 *     Wrote argument processor (main), Help, and Version.
 *   2021.10.05 0358->0415 (17min):
 *     Implimnented Read_STDIN
 *   2021.10.05 0730->0740, 1100->1130 (40min):
 *     Implimented Read_FILE
 */