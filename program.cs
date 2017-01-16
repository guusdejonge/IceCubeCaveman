using System;
using System.IO;
using System.Linq;
using Cloo;

namespace Caveman {

class Program
{
	// properties
	public static ComputeContext context;
	public static ComputeCommandQueue queue;
	public static ComputeProgram program;
	public static ComputeKernel kernel;
	static ComputeBuffer<int> buffer;
	static int [] data;
	// initialiseer OpenCL
	static void InitCL()
	{
		// kies platform 0 (op sommige machines moet dit 1 of 2 zijn)
		var platform = ComputePlatform.Platforms[CLPlatform];
		Console.Write( "initializing OpenCL... " + platform.Name + " (" + platform.Profile + ").\n" );
		Console.Write( platform.Devices.First().Name + " (" + platform.Devices.First().Type + ")\n");
		Console.Write( (platform.Devices.First().GlobalMemorySize / 1024 / 1024) );
		Console.WriteLine( " MiB global memory / " + (platform.Devices.First().LocalMemorySize / 1024) + " KiB local memory");
		// maak een compute context
		context = new ComputeContext( ComputeDeviceTypes.Gpu, new ComputeContextPropertyList( platform ), null, IntPtr.Zero );
		// laad opencl programma
		var streamReader = new StreamReader( "../../program.cl" );
		string clSource = streamReader.ReadToEnd();
		streamReader.Close();
		// compileer opencl source code
		program = new ComputeProgram( context, clSource );
		try
		{
			program.Build( null, null, null, IntPtr.Zero );
		}
		catch
		{
			// fout in OpenCL code; check console window voor details.
			Console.Write( "error in kernel code:\n" );
			Console.Write( program.GetBuildLog( context.Devices[0] ) + "\n" );
		}
		// maak een commandorij
		queue = new ComputeCommandQueue( context, context.Devices[0], 0 );
		// lokaliseer de gewenste kernel in het programma
		kernel = program.CreateKernel( "device_function" );
		// alloceer data in RAM
		data = new int[2];
		data[0] = 999;		// OpenCL code gaat deze waarde testen
		data[1] = 0;		// OpenCL code gaat dit vervangen door 111
		// alloceer data op de GPU
		var flags = ComputeMemoryFlags.WriteOnly | ComputeMemoryFlags.UseHostPointer;
		buffer = new ComputeBuffer<int>( context, flags, data );
	}
	// main functie
    static void Main(string[] args)
    {
		// bepaal de parameters voor de applicatie
		string mapFile = "";		
		if (args.Length > 0)
		{
			// command line parameters: we verwachten er drie
			if (args.Length != 4)
			{
				// verkeerd aantal parameters, nog maar een keer uitleggen dan
				Console.Write( "\nCaveman Solver / Concurrency 2016-2017\nParameters:\n" );
				Console.Write( "> map: kies een map uit folder maps (bijv: polar1.in)\n" );
				Console.Write( "> processor: C (CPU) of G (GPU)\n" );
				Console.Write( "> output: S (alleen aantal stappen) of O (volledige oplossing)\n");
				Console.Write( "> platform: te gebruiken OpenCL platform (0, 1 of 2).\n\n" );
				Console.Write( "Voorbeeld:\n\ntemplate.exe polar1.in G 0\n\n" );
			}
			else
			{
				// interpreteer parameters
				mapFile = "../../maps/" + args[0];
				GPU = (args[1] == "G" ? true : false);
				Output = (args[2] == "O" ? true : false);
				CLPlatform = Convert.ToInt32( args[3] );
			}
		}
		else
		{ 
			// default parameters
			GPU = true;
			CLPlatform = 0;
			Output = true;
			mapFile = "../../maps/Polar1.in";
		}

		// los de puzzel op
		if (mapFile != "")
		{
			// initialiseer OpenCL
			InitCL();

			

			// start de solver
			Caveman c = new Caveman( mapFile );
			c.BFS();
		}

		// wacht op een toets
		Console.ReadLine();
    }

    public static bool GPU;
    public static bool Output;
	public static int CLPlatform;
}

} // namespace Caveman