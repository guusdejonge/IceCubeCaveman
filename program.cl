

int Testt()
{
return 76;
}

int xpositie(int p) { return p % 256; }
int ypositie(int p) { return (p / 256) % 256; }
int richting(int p) { return (p / 65536) % 4; }
int schakelaars(int p) { return (p / 262144); }

void Yadj(int p, __local int* opties)
{
	int i = xpositie(p); int j = ypositie(p);
    int s = richting(p); int t = schakelaars(p);


}

void Badj(int p, __local int* opties)
{
}

void Adj(int tc, int p, __local int* opties)
    {
       if(tc==1)
	   {
			Yadj(p, opties);
	   }
	   else
	   {
			Badj(p, opties);
	   }
    }


__kernel void device_function( 
	__global int* pos_in, __global int* pa,
	int tc, int N_in, __global volatile int* N_uit)
	{
		//data[0]= Testt();

		for(int i = 0; i < N_in; i++)
		{
			__local int opties [4];
			int p = pos_in[i];
			Adj(tc, p, opties);
		}

		//__local uint *counter = 3;
		//uint old_val = atomic_inc( counter );	//make atomic increment on it
		//counter = counter + 1;

		//N_uit[0] = counter;

		if (N_uit[0] < N_in)
		{
			int oud = atomic_inc( &N_uit[0]);
		}
		



		//Adj(tc, N_in, int* pos_in, int* pa);

		//__local int LP [4];
		//LP[0] = 1;
		//Console.WriteLine("hoi");

		//Adj(0,0,LP);

		//if (LP[0] == 3)
		//{
		//	data[0]= 76;
		//}
	}

