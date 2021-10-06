/* A trivial dc-clone in C# */
using System;
using System.IO; //< Path interface
using System.Collections.Generic; //< List and Queue containers

// C uses a macro to help keep common application exit invocations clean
//   C# should do the same...
enum EXIT_STATUS : int {
	SUCCESS = 0,
	FAILURE,
}

public struct StackObj {
	// vv C# is stupid: structs are are naturaly public by default, a virtue of their nature, but I have to explicitly state it here anyway.
	public string str;
	public decimal num;
	public bool isString;
}

class Program {
	public static int VERSION_MAJOR = 2021;
	public static int VERSION_MINOR = 279;
	public static int VERSION_REVISION = 0364386;

	private static Dictionary<char, Stack<StackObj>> Memory = new Dictionary<char, Stack<StackObj>>();
	private static List<StackObj> TheStack = new List<StackObj>();
	private static Queue<string> FileQ = new Queue<string>();
	private static int Precision = 0;

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

	private static string Eval_buffer = String.Empty;

	private static bool Eval_buffer_hasString() {
		return Eval_buffer.StartsWith( "[" );
	}

	private static int Eval_buffer_stringDepth() {
		int depth = 0;

		foreach( char c in Eval_buffer ) {
			switch( c ) {
				case '[':
					depth++;
					break;
				case ']':
					depth--;
					break;
			}
		}

		return depth;
	}

	private static void Eval( string line ) {
		for( int il = 0; il < line.Length; il++ ) {
			// SECTION: String Continuation
			if( Eval_buffer_hasString() ) {	
				Eval_buffer += line[il];

				if( Eval_buffer_stringDepth() == 0 ) {
					do_push_string( Eval_buffer );
					Eval_buffer = String.Empty;
				}
			}

			// SECTION: Numberic Continuation
			else if( Eval_buffer.Length > 0 && Char.IsDigit( line[il] ) || line[il] == '.' && !Eval_buffer.Contains( '.' ) ) {
				Eval_buffer += line[il];
			} 

			// SECTION: Non-string Buffer push/exec
			else if( Eval_buffer.Length > 0 ) {
				// CASE: false-positive: subtract operator received, not negation operator
				if( Eval_buffer.Length == 1 && Eval_buffer[0] == '-' ) {
					do_subtract();
					il--;
				} else {
					do_push_number( Decimal.Parse( Eval_buffer ) );
				}

				Eval_buffer = String.Empty;
			}

			// SECTION: Operators
			else switch( line[il] ) {
				// buffer should be ""
				case '[': // Start String Op
				case '-': // Subtraction or Negation Op
					Eval_buffer += line[il];
					break;
				case '+':   do_add();                       break;
				case '*':   do_multiply();                  break;
				case '/':	  do_divide();                    break;
				case '%':	  do_modulo();                    break;
				case '~':   do_modulo_divide();             break;
				case '^':   do_power();                     break;
				case '|':   do_modulo_power();              break;
				case 'v':   do_sqrt();                      break;
				case 'p':   do_print();                     break;
				case 'P':   do_print_pop();                 break;
				case 'n':   do_print_pop_nonl();            break;
				case 'f':   do_print_stack();               break;
				case 'c':   do_clear_stack();               break;
				case 'd':   do_duplicate();                 break;
				case 'r':   do_swap();                      break;
				case 'R':   do_rotate_stack();              break;
				case 's':   do_store( line[++il] );         break;
				case 'l':   do_readback( line[++il] );      break;
				case 'S':   do_store_stack( line[++il] );   break;
				case 'L':   do_moveback( line[++il] );      break;
				case 'k':   do_setPrecision();              break;
				case 'K':   do_getPrecision();              break;
				case 'x':   do_eval();                      break;
				case 'z':   do_getStackSize();              break;

				// Unimplimented instructions
				case 'i': // Set Input Radix
				case 'o': // Set Ouput Radix
					do_discard();
					Console.WriteLine( $" WARNING: {line[il]} is not implimented by this software." );
					break;
				case 'I': // Get Input Radix
				case 'O': // Get Output Radix
					do_push_number( 10 );
					Console.WriteLine( $" WARNING: {line[il]} is not implimented by this software." );
					break;

				// Comment
				case '#':
					return;
				
				// Whitespace
				case ' ':
				case '\t':
				case '\r':
				case '\n':
				case '\f':
				case '\v':
					break;
				
				default:
					Console.WriteLine( $" ERROR: {line[il]} is not a recognized instruction." );
					break;
			}
		}
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
 *   2021.10.05 2145->0045 (3hr):
 *     NOTE: Eval is a hand-written parser, a finite state automation, so 
 *       Coding these sections will take longer than the rest of this business
 *       logic.
 */