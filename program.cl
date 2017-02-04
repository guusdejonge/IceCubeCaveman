
int xpositie(int p) { return p % 256; }
int ypositie(int p) { return (p / 256) % 256; }
int richting(int p) { return (p / 65536) % 4; }
int schakelaars(int p) { return (p / 262144); }

void Yadj(int p, __local int* opties)
{
	int i = xpositie(p); int j = ypositie(p);
    int s = richting(p); int t = schakelaars(p);

	/*switch (s)
            {
                case 1:  // Cave staat rechtop op i,j
                         // Kan ik naar Rechts?  Ik ga dan EW-liggen op i+1,j
                    if (step(i + 1, j, false, t) && step(i + 2, j, false, t))
                        //pa.Add(code(i + 1, j, 3, sw(i + 1, j, sw(i + 2, j, t))));

                    // Kan ik naar Links?  Ik ga dan EW-liggen op i-2,j
                    if (step(i - 1, j, false, t) && step(i - 2, j, false, t))
                        //pa.Add(code(i - 2, j, 3, sw(i - 2, j, sw(i - 1, j, t))));

                    // Kan ik omhoog?  Ik ga dan NZ-liggen op i,j-2
                    if (step(i, j - 2, false, t) && step(i, j - 1, false, t))
                        //pa.Add(code(i, j - 2, 2, sw(i, j - 2, sw(i, j - 1, t))));

                    // Kan ik omlaag?  Ik ga dan NZ-liggen op i,j+1
                    if (step(i, j + 1, false, t) && step(i, j + 2, false, t))
                        //pa.Add(code(i, j + 1, 2, sw(i, j + 1, sw(i, j + 2, t))));
                    break;

                case 2:  // Cave ligt NZ op i,j (dwz pos i,j en i,j+1)
                         // Kan ik naar rechts, NZlig op i+1,j 
                    if (step(i + 1, j, false, t) && step(i + 1, j + 1, false, t))
                        //pa.Add(code(i + 1, j, 2, sw(i + 1, j, sw(i + 1, j + 1, t))));

                    // Kan ik naar Links, NZ liggen op i-1,j
                    if (step(i - 1, j, false, t) && step(i - 1, j + 1, false, t))
                        //pa.Add(code(i - 1, j, 2, sw(i - 1, j, sw(i - 1, j + 1, t))));

                    // Kan ik naar boven, staan op i,j-1
                    if (step(i, j - 1, true, t))
                       // pa.Add(code(i, j - 1, 1, sw(i, j - 1, t)));

                    // Kan ik naar beneden, staan op i, j+2
                    if (step(i, j + 2, true, t))
                        //pa.Add(code(i, j + 2, 1, sw(i, j + 2, t)));
                    break;

                case 3:  // Cave ligt EW op i,j (dwz bezet i,j en i+1,j)
                         // Kan ik naar rechts, gaan staan op i+2,j
                    if (step(i + 2, j, true, t))
                        //pa.Add(code(i + 2, j, 1, sw(i + 2, j, t)));

                    // Kan ik naar links, staan op i-1,j
                    if (step(i - 1, j, true, t))
                       // pa.Add(code(i - 1, j, 1, sw(i - 1, j, t)));

                    // Kan ik naar beneden, gaan EW-liggen op i,j+1
                    if (step(i, j + 1, false, t) && step(i + 1, j + 1, false, t))
                        //pa.Add(code(i, j + 1, 3, sw(i, j + 1, sw(i + 1, j + 1, t))));

                    // Kan ik omhoog, gaan EW-liggen op i,j-1
                    if (step(i, j - 1, false, t) && step(i + 1, j - 1, false, t))
                       // pa.Add(code(i, j - 1, 3, sw(i, j - 1, sw(i + 1, j - 1, t))));
                    break;
                default: break;
            }*/
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

	int test(){
		return 3;
	}

__kernel void device_function( 
	__global int* pos_in, __global int* pa,
	int tc, int N_in, __global volatile int* N_uit)
	{
		int id = get_global_id(0);
		int positie = pos_in[id];

		__local int opties [4];
		Adj(tc, positie, opties);

		//data[0]= Testt();

		/*for(int i = 0; i < N_in; i++)
		{
			__local int opties [4];
			int p = pos_in[i];
			Adj(tc, p, opties);
		}*/

		//__local uint *counter = 3;
		//uint old_val = atomic_inc( counter );	//make atomic increment on it
		//counter = counter + 1;

		//N_uit[0] = counter;

		/*if (N_uit[0] < N_in)
		{
			int oud = atomic_inc( &N_uit[0]);
		}*/
		
		//int i = get_global_id(0);

		//N_uit[i] = 3;

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

