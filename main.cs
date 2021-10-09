/* A trivial dc-clone in C# */
using System;
using System.IO; //< Path interface
using System.Collections.Generic; //< List and Queue container

public struct StackObj {
	// vv C# is stupid: structs are are naturaly public by default, a virtue of their nature, but I have to explicitly state it here anyway.
	public string str;
	public decimal num;
	public bool isString;
}

class Program {
	public static int VERSION_MAJOR = 2021;
	public static int VERSION_MINOR = 282;
	public static int VERSION_REVISION = 6169264;

	private static bool DEBUG_Eval = false;

	private static Dictionary<char, Stack<StackObj>> Memory = new Dictionary<char, Stack<StackObj>>();
	private static List<StackObj> TheStack = new List<StackObj>();
	private static Queue<string> FileQ = new Queue<string>();
	private static List<long> Primes = new List<long>();
	private static int Precision = 0;

	// C uses a macro to help keep common application exit invocations clean
	//   C# should do the same...
	public const int EXIT_SUCCESS = 0;
	public const int EXIT_FAILURE = 1;

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

		System.Environment.Exit( EXIT_SUCCESS );
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

		System.Environment.Exit( EXIT_SUCCESS );
	}

	// Interface for discering the type of object on the top of TheStack
	private static bool TheStack_hasString( int indx = 0 ) {
		return TheStack[indx].isString;
	}

	// A more complete modulus algorithm that works with any nubmer set
	private static decimal NaturalModulo( decimal num, decimal mod ) {
		return num - mod * Math.Floor( num / mod );
	}

	// Initialize Primes
	private static void InitPrimes() {
		Primes.Add( 2 );
		Primes.Add( 3 );
	}

	// Generates and returns next prime number in sequence
	private static long NextPrime() {
		// Ensure Primes is initialized
		if( Primes.Count == 0 )
			InitPrimes();

		//   vv Potential Prime
		long pp = Primes[Primes.Count - 1];
		try_next: 
			pp += Primes[0];
			foreach( long prime in Primes )
				if( pp % prime == 0 )
					goto try_next;
		
		// If you reach this point a new prime's been found
		Primes.Add( pp );
		return pp;
	}

	// Generate Primes until Primes[Primes.Length - 1] >= target
	private static void EnsurePrimesIncludes( long target ) {
		// Ensure Primes is initialized
		if( Primes.Count == 0 )
			InitPrimes();

		while( Primes[Primes.Count - 1] < target )
			NextPrime();
	}

	// Ensure Memory[Register] is Initialized
	private static void EnsureMemoryInitd( char Register ) {
		if( ! Memory.ContainsKey( Register ) )
			Memory[Register] = new Stack<StackObj>();
	}

	// SECTION: Operations
	// Wrapper for pushing objects the TheStack
	private static void do_push( StackObj so ) {
		TheStack.Insert( 0, so );
	}

	// Wrapper for getting objects from TheStack
	private static StackObj do_pop() {
		StackObj so = TheStack[0];
		TheStack.RemoveAt( 0 );
		return so;
	}

	// Wrapper for looking at objects on TheStack
	private static StackObj do_peek() {
		return TheStack[0];
	}

	// Wrapper for pushing strings to TheStack
	private static void do_push_string( string s ) {
		StackObj so = new StackObj();
		so.str = s;
		so.isString = true;

		do_push( so );
	}

	// Wrapper for pushing nubers to TheStack
	private static void do_push_number( decimal d ) {
		StackObj so = new StackObj();
		so.num = d;
		so.isString = false;

		do_push( so );
	}

	// Wrapper for discarding objects on TheStack
	private static void do_discard() {
		do_pop();
	}

	// Wrapper for popping numbers off TheStack
	private static decimal do_pop_number() {
		// Type Checking
		if( TheStack_hasString() ) {
			Console.WriteLine( " FATAL: Expected number, got string {0}", TheStack[0].str );
			System.Environment.Exit( EXIT_FAILURE );
		}
		
		return do_pop().num;
	}

	// Wrapper for popping strings off TheStack
	private static string do_pop_string() {
		// Type Checking
		if( !TheStack_hasString() ) {
			Console.WriteLine( " FATAL: Expected string, got number ({0})", TheStack[0].num );
			System.Environment.Exit( EXIT_FAILURE );
		}
		
		return do_pop().str;
	}

	private static void do_add() {
		// Employing Commutative property of addition
		do_push_number( do_pop_number() + do_pop_number() );
	}

	private static void do_subtract() {
		decimal val = do_pop_number();
		do_push_number( do_pop_number() - val);
	}

	private static void do_multiply() {
		// Employing Commutative property of multiplication
		do_push_number( do_pop_number() * do_pop_number() );
	}

	private static void do_divide() {
		decimal val = do_pop_number();
		do_push_number( do_pop_number() / val );
	}

	private static void do_modulo() {
		decimal val = do_pop_number();
		do_push_number( NaturalModulo( do_pop_number(), val ) );
	}

	// pops two values, and divides them; returns quotent and remainder respectivly
	private static void do_modulo_divide() {
		decimal[] val = new decimal[2];
		for( int indx = 0; indx < val.Length; indx++ )
			val[indx] = do_pop_number();

		// Using variables because we calculate them in reverse
		decimal quotent;
		decimal remainder;

		remainder = NaturalModulo( val[1], val[0] );
		val[1] -= remainder;

		quotent = val[1] / val[0];

		do_push_number( quotent );
		do_push_number( remainder );
	}

	// computes exponent
	//   one of few things C# does acceptably
	private static void do_power() {
		decimal exp = do_pop_number();
		do_push_number( (decimal)Math.Pow( (double)do_pop_number(), (double)exp ) );
	}

	// Modulo-Power solver using Integration
	private static decimal do_modulo_power_subroutine( decimal num, long exp, decimal mod ) {
		// SUBSECTION: Factor natural power
		Dictionary<long, long> pow_fact = new Dictionary<long, long>();
		long max_prime = (long)Math.Floor( Math.Sqrt( exp ) );

		// Ensure we have enough primes for factoring
		EnsurePrimesIncludes( max_prime );

		// FACTOR!!!
		int pindx;
		while( exp > 1 ) {
			for( pindx = 0; 
				pindx < Primes.Count
					&& Primes[pindx] <= max_prime 
					&& exp % Primes[pindx] != 0; 
				pindx++ ); //< TRANSLATION: find Prime where...
			
			if( Primes[pindx] <= max_prime ) {
				if( pow_fact.ContainsKey( Primes[pindx] ) )
					pow_fact[Primes[pindx]]++;
				else
					pow_fact[Primes[pindx]] = 1;


				exp /= Primes[pindx];
				max_prime = (long) Math.Floor( Math.Sqrt( exp ) );
			} else {
				// Reaching this block means that exp is prime itself
				if( pow_fact.ContainsKey( exp ) )
					pow_fact[exp]++;
				else
					pow_fact[exp] = 1;
				break;
			}
		}
		
		// SUBSECTION: Exponentiate
		Stack<decimal> subExp = new Stack<decimal>();
		decimal modNum = NaturalModulo( num, mod );
		foreach( var pair in pow_fact ) {
			// I'm not impressed with C#'s Math Library's inability to maintain my
			//    28 place "decimal" precision.  And since I don't have to worry 
			//    about inversion or roots, I'll just do it by hand...
			decimal subNum = modNum;
			for( long itr = 0; itr < pair.Key; itr++ )
				subNum = NaturalModulo( subNum * modNum, mod );

			if( pair.Value > 1 )
				subExp.Push( do_modulo_power_subroutine( subNum, pair.Value, mod ) );
			else
				subExp.Push( subNum );
		}

		decimal result = subExp.Pop();
		while( subExp.Count > 0 ) 
			result = NaturalModulo( result * subExp.Pop(), mod );

		return result;
	}

	// performs modulo exponentiation
	private static void do_modulo_power() {
		decimal mod = do_pop_number();
		decimal pow = do_pop_number();
		decimal num = do_pop_number();

		// Seperate natural and real parts of the exponent for factoring
		int pow_sign = (int)(pow / Math.Abs( pow ));
		decimal pow_frac = Math.Abs( pow ) - Math.Truncate( Math.Abs( pow ) );
		long pow_int = (long)Math.Truncate( Math.Abs( pow ) );

		// Calculate natural exponent
		decimal natExp = do_modulo_power_subroutine( num, pow_int, mod );

		// Calulate root 
		decimal root = (decimal)Math.Pow( (double)num, (double) pow_frac );

		// merge Calculation
		num = NaturalModulo( natExp * root, mod );

		// Do modular inverse
		if( pow_sign < 0 ) {
			// UNDONE
		}

		Console.WriteLine( " FATAL: Modulo-Powers operation is incomplete!" );
		System.Environment.Exit( EXIT_FAILURE );
	}

	private static void do_sqrt() {
		do_push_number( (decimal)Math.Sqrt( (double)do_pop_number() ) );
	}

	private static void do_print() {
		if( TheStack_hasString() )
			Console.WriteLine( do_peek().str );
		else
			Console.WriteLine( Math.Round( do_peek().num, Precision ) );
	}

	private static void do_print_pop() {
		do_print();
		do_discard();
	}

	private static void do_print_pop_nonl() {
		if( TheStack_hasString() )
			Console.Write( do_pop_string() );
		else
			Console.Write( Math.Round( do_pop_number(), Precision ) );
	}

	private static void do_print_stack() {
		for( int indx = 0; indx < TheStack.Count; indx++ ) {
			if( TheStack_hasString( indx ) )
				Console.WriteLine( TheStack[indx].str );
			else
				Console.WriteLine( Math.Round( TheStack[indx].num, Precision ) );
		}
	}

	private static void do_clear_stack() {
		TheStack.Clear();
	}

	private static void do_duplicate() {
		do_push( do_peek() );
	}

	private static void do_swap() {
		// If only C# used real pointers...
		var tmp = TheStack[1];
		TheStack[1] = TheStack[0];
		TheStack[0] = tmp;
	}

	private static void do_rotate_stack() {
		if( Math.Truncate( do_peek().num ) != do_peek().num ) {
			Console.WriteLine( " WARNING: received non-integer number of units to rotate ({0}); will truncate to integer.", do_peek().num );
		}

		int rot = (int)Math.Truncate( do_pop_number() );
		rot %= TheStack.Count;
		if( rot < 0 )
			rot += TheStack.Count;
		
		if( rot == 0 )
			return; //< do nothing

		TheStack.AddRange( TheStack.GetRange( 0, rot ) );
		TheStack.RemoveRange( 0, rot );
	}

	// Move top of TheStack to Memory[Register] {overwrite}
	private static void do_store( char Register ) {
		EnsureMemoryInitd( Register );

		// This is an overwrite; pop previous value if exists
		if( Memory[Register].Count > 0 )
			Memory[Register].Pop();

		Memory[Register].Push( do_pop() );
	}

	// Copy value at Memory[Register] into TheStack
	private static void do_readback( char Register ) {
		EnsureMemoryInitd( Register );

		if( Memory[Register].Count > 0 ) {
			do_push( Memory[Register].Peek() );
		} else {
			// Memory Stack is Empty
			Console.WriteLine( " WARNING: attempted read of empty register ({0})", Register );
			do_push_number( 0 );
		}
	}

	// Move top of TheStack to Memory[Register] {push}
	private static void do_store_stack( char Register ) {
		EnsureMemoryInitd( Register );

		Memory[Register].Push( do_pop() );
	}

	// Move top of Memory[Register] to TheStack
	private static void do_moveback( char Register ) {
		EnsureMemoryInitd( Register );

		if( Memory[Register].Count > 0 ) {
			do_push( Memory[Register].Pop() );
		} else {
			// Memory Stack is Empty
			Console.WriteLine( " WARNING: attempted read of empty register ({0})", Register );
		}
	}

	private static void do_setPrecision() {
		if( Math.Truncate( do_peek().num ) != do_peek().num )
			Console.WriteLine( " WARNING: attempted pass of real as precission ({0}); converted to natural");
		
		int integer = (int)Math.Truncate( do_pop_number() );
		Precision = integer < 0 ? 0 : integer;
	}

	private static void do_getPrecision() {
		do_push_number( Precision );
	}

	private static void do_getStackSize() {
		do_push_number( TheStack.Count );
	}

	private static void do_eval() {
		string expr = do_pop_string();
		Eval( expr.Substring( 1, expr.Length - 2 ) );
	}

	// SECTION: Eval
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
			// DEBUG Meta:
			if( DEBUG_Eval ) {
				Console.WriteLine( "     il: {0}", il );
				Console.WriteLine( "    pos: {0}v", new String( ' ', il ) );
				Console.WriteLine( "   line: {0}", line );
				Console.WriteLine( "   buff: {0}", Eval_buffer );
				Console.WriteLine();
			}

			// SECTION: String Continuation
			if( Eval_buffer_hasString() ) {	
				Eval_buffer += line[il];

				if( Eval_buffer_stringDepth() == 0 ) {
					do_push_string( Eval_buffer );
					Eval_buffer = String.Empty;
				}
			}

			// SECTION: Numberic Continuation
			else if( Char.IsDigit( line[il] ) || line[il] == '.' && !Eval_buffer.Contains( '.' ) ) {
				Eval_buffer += line[il];
			} 

			// SECTION: Non-string Buffer push/exec
			else if( Eval_buffer.Length > 0 ) {
				// CASE: false-positive: subtract operator received, not negation operator
				if( Eval_buffer.Length == 1 && Eval_buffer[0] == '-' ) {
					do_subtract();
				} else {
					do_push_number( Decimal.Parse( Eval_buffer ) );
				}

				il--;
				Eval_buffer = String.Empty;
			}

			// SUBSECTION: Operators
			else switch( line[il] ) {
				// buffer should be ""
				case '[': // Start String Op
				case '-': // Subtraction or Negation Op
					Eval_buffer += line[il];
					break;
				case '+':   do_add();                                  break;
				case '*':   do_multiply();                             break;
				case '/':	  do_divide();                               break;
				case '%':	  do_modulo();                               break;
				case '~':   do_modulo_divide();                        break;
				case '^':   do_power();                                break;
				case '|':   do_modulo_power();                         break;
				case 'v':   do_sqrt();                                 break;
				case 'p':   do_print();                                break;
				case 'P':   do_print_pop();                            break;
				case 'n':   do_print_pop_nonl();                       break;
				case 'f':   do_print_stack();                          break;
				case 'c':   do_clear_stack();                          break;
				case 'd':   do_duplicate();                            break;
				case 'r':   do_swap();                                 break;
				case 'R':   do_rotate_stack();                         break;
				case 's':   do_store( line[++il] );                    break;
				case 'l':   do_readback( line[++il] );                 break;
				case 'S':   do_store_stack( line[++il] );              break;
				case 'L':   do_moveback( line[++il] );                 break;
				case 'k':   do_setPrecision();                         break;
				case 'K':   do_getPrecision();                         break;
				case 'x':   do_eval();                                 break;
				case 'z':   do_getStackSize();                         break;
				case 'q':   System.Environment.Exit( EXIT_SUCCESS );   break;

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
					goto flush_buffer;
				
				// Whitespace
				case ' ':
				case '\t':
				case '\r':
				case '\n':
				case '\f':
				case '\v':
					break;
				
				default:
					Console.WriteLine( $" ERROR: `{line[il]}' is not a recognized instruction." );
					break;
			}
		}

		flush_buffer:
		if( Eval_buffer.Length > 0 && ! Eval_buffer_hasString() ){
			do_push_number( Decimal.Parse( Eval_buffer ) );
			Eval_buffer = String.Empty;
		}
	}

	// SECTION: Readers
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

	// SECTION: Main
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
					System.Environment.Exit( EXIT_FAILURE );
				}
			// Check long file option
			} else if( args[ix].StartsWith( "--file" ) ) {
				try {
					// This version of the program doesn't actualy care what the delimiter is, just that it is there...
					FileQ.Enqueue( args[ix].Length > 6 ? args[ix].Substring( 7 ) : args[++ix] );
				} catch( IndexOutOfRangeException ) {
					Console.WriteLine( " FATAL: expected file, saw EOL" );
					System.Environment.Exit( EXIT_FAILURE );
				}
			// Check short eval option
			} else if( args[ix].StartsWith( "-e" ) ) {
				try {
					Eval( args[ix].Length > 2 ? args[ix].Substring( 2 ) : args[++ix] );
				} catch( IndexOutOfRangeException ) {
					Console.WriteLine( " FATAL: expected expression, saw EOL" );
					System.Environment.Exit( EXIT_FAILURE );
				}
			// Check long eval option
			} else if( args[ix].StartsWith( "--expression" ) ) {
				try {
					Eval( args[ix].Length > 12 ? args[ix].Substring( 13 ) : args[++ix] );
				} catch( IndexOutOfRangeException ) {
					Console.WriteLine( " FATAL: expected expression, saw EOL" );
					System.Environment.Exit( EXIT_FAILURE );
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

		return EXIT_SUCCESS;
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
 *   2021.10.06 0810->0830 (20min):
 *     Implimented:
 *        do_push
 *        do_pop
 *        do_peek
 *        do_push_string
 *        do_push_number
 *        do_discard
 *   2021.10.07 1100->1130, 1330->1340 (40min):
 *     Replaced EXIT_STATUS enum with constants
 *       (because C# is stupid and can't impicit cast anything)
 *     Implimented:
 *       TheStack_hasString
 *       do_pop_number
 *       do_pop_string
 *       do_add
 *       do_subtract
 *       do_multiply
 *       do_divide
 *       do_modulo
 *   2021.10.08 0915->1815 (9hr):
 *     Implimented:
 *       NaturalModulo
 *       InitPrimes
 *       NextPrime
 *       EnsurePrimesIncludes
 *       EnsureMemoryInitd
 *       do_modulo_divide
 *       do_power
 *       do_modulo_power_subroutine
 *       do_sqrt
 *       do_print
 *       do_print_pop
 *       do_print_pop_nonl
 *       do_print_stack
 *       do_clear_stack
 *       do_duplicate
 *       do_swap
 *       do_rotate_stack
 *       do_store
 *       do_readback
 *       do_store_stack
 *       do_moveback
 *       do_setPrecision
 *       do_getPrecision
 *       do_getStackSize
 *       do_eval
 *     Corrected:
 *       do_subtract
 *       do_divide
 *       do_modulo
 *     Incomplete:
 *       do_modulo_power
 *   2021.10.09: Debugged Eval and Swap:
 *     Wasn't handling nubmers correctly...
 */
