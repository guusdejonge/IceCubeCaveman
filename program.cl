
int xpositie(int p) { return p % 256; }
int ypositie(int p) { return (p / 256) % 256; }
int richting(int p) { return (p / 65536) % 4; }
int schakelaars(int p) { return (p / 262144); }

bool __attribute__((overloadable)) step(int x, int y, bool ver, int T, __global int* veld, int b, int h) 
	{
		// Kan Caveman op tegel x,y als hij ver?staat:ligt en T is de set toegankelijke tovertegels
		if (x < 0 || x >= b) return false;     // te ver links of rechts
		if (y < 0 || y >= h) return false;     // te hoog of laag
		if (veld[x+b*y] == 46) return false;   // mag niet in de zee
		if (veld[x+b*y] == 88) return true;    // stevige tegel, kan wel
		if (veld[x+b*y] == 90) return true;    // vuur, kan wel
		if (veld[x+b*y] == 120) return !ver;    // zwakke schots, staan mag niet liggen wel
		if (veld[x+b*y] >= 65 &&
			veld[x+b*y] <= 76) return true;    // Schakelaar, kan wel
												// Overgebleven case: toverschots, check in T
		return (T & (1 << (veld[x+b*y] - 97))) != 0;
	}

 int code(int x, int y, int richting, int schakelaars)
        {
            return (schakelaars << 18) + (richting << 16) + (y << 8) + x;
        }
 int sw(int x, int y, int t, __global int* veld, int b, int h)
        {
            // Hoe verandert de toverschotsenstand als Caveman op x,y stapt?
            if (veld[x+b*y] < 65 || veld[x+b*y] > 76)  // Dit is geen switch
                return t;
            // XOR t met de bitpositie die je afleidt uit de geraakte switch
            return t ^ (1 << (veld[x+b*y] - 65));
        }

void Yadj(int p, __local int* opties, __global int* veld, int b, int h)
{
	int i = xpositie(p); int j = ypositie(p);
    int s = richting(p); int t = schakelaars(p);

	switch (s)
            {
                case 1:  // Cave staat rechtop op i,j
                         // Kan ik naar Rechts?  Ik ga dan EW-liggen op i+1,j
                    if (step(i + 1, j, false, t, veld, b, h) && step(i + 2, j, false, t, veld, b, h))
                        //opties[0]=code(i + 1, j, 3, sw(i + 1, j, sw(i + 2, j,t, veld, b, h)));

                    // Kan ik naar Links?  Ik ga dan EW-liggen op i-2,j
                    if (step(i - 1, j, false, t, veld, b, h) && step(i - 2, j, false, t, veld, b, h))
                        //opties[1]=code(i - 2, j, 3, sw(i - 2, j, sw(i - 1, j,t, veld, b, h)));

                    // Kan ik omhoog?  Ik ga dan NZ-liggen op i,j-2
                    if (step(i, j - 2, false, t, veld, b, h) && step(i, j - 1, false, t, veld, b, h))
                        //opties[2]=code(i, j - 2, 2, sw(i, j - 2, sw(i, j - 1,t, veld, b, h)));

                    // Kan ik omlaag?  Ik ga dan NZ-liggen op i,j+1
                    if (step(i, j + 1, false, t, veld, b, h) && step(i, j + 2, false, t, veld, b, h))
                        //opties[3]=code(i, j + 1, 2, sw(i, j + 1, sw(i, j + 2,t, veld, b, h)));
                    break;

                case 2:  // Cave ligt NZ op i,j (dwz pos i,j en i,j+1)
                         // Kan ik naar rechts, NZlig op i+1,j 
                    if (step(i + 1, j, false,t, veld, b, h) && step(i + 1, j + 1, false,t, veld, b, h))
                        //opties[0]=code(i + 1, j, 2, sw(i + 1, j, sw(i + 1, j + 1,t, veld, b, h)));

                    // Kan ik naar Links, NZ liggen op i-1,j
                    if (step(i - 1, j, false,t, veld, b, h) && step(i - 1, j + 1, false,t, veld, b, h))
                        //opties[1]=code(i - 1, j, 2, sw(i - 1, j, sw(i - 1, j + 1,t, veld, b, h)));

                    // Kan ik naar boven, staan op i,j-1
                    if (step(i, j - 1, true,t, veld, b, h))
                       // opties[2]=code(i, j - 1, 1, sw(i, j - 1,t, veld, b, h));

                    // Kan ik naar beneden, staan op i, j+2
                    if (step(i, j + 2, true,t, veld, b, h))
                        //opties[3]=code(i, j + 2, 1, sw(i, j + 2,t, veld, b, h));
                    break;

                case 3:  // Cave ligt EW op i,j (dwz bezet i,j en i+1,j)
                         // Kan ik naar rechts, gaan staan op i+2,j
                    if (step(i + 2, j, true,t, veld, b, h))
                        //opties[0]=code(i + 2, j, 1, sw(i + 2, j,t, veld, b, h));

                    // Kan ik naar links, staan op i-1,j
                    if (step(i - 1, j, true,t, veld, b, h))
                       // opties[1]=code(i - 1, j, 1, sw(i - 1, j,t, veld, b, h));

                    // Kan ik naar beneden, gaan EW-liggen op i,j+1
                    if (step(i, j + 1, false,t, veld, b, h) && step(i + 1, j + 1, false,t, veld, b, h))
                        //opties[2]=code(i, j + 1, 3, sw(i, j + 1, sw(i + 1, j + 1,t, veld, b, h)));

                    // Kan ik omhoog, gaan EW-liggen op i,j-1
                    if (step(i, j - 1, false,t, veld, b, h) && step(i + 1, j - 1, false,t, veld, b, h))
                       // opties[3]=code(i, j - 1, 3, sw(i, j - 1, sw(i + 1, j - 1,t, veld, b, h)));
                    break;
                default: break;
            }
}

void Badj(int p, __local int* opties, __global int* veld, int b, int h)
{
			int i = xpositie(p); int j = ypositie(p);
            int s = richting(p); int t = schakelaars(p);

            switch (s)
            {
                // We zitten hier met blauwe switches, die doen niets als je er
                // op gaat liggen.  Dus alleen in de staande nieuwe posities
                // wordt sw meegenomen.
                case 1:  // Cave staat rechtop op i,j
                         // Kan ik naar Rechts?  Ik ga dan EW-liggen op i+1,j
                    if (step(i + 1, j, false, t, veld, b, h) && step(i + 2, j, false, t, veld, b, h))
                        opties[0]=code(i + 1, j, 3, t);

                    // Kan ik naar Links?  Ik ga dan EW-liggen op i-2,j
                    if (step(i - 1, j, false, t, veld, b, h) && step(i - 2, j, false, t, veld, b, h))
                        opties[0]=code(i - 2, j, 3, t);

                    // Kan ik omhoog?  Ik ga dan NZ-liggen op i,j-2
                    if (step(i, j - 2, false, t, veld, b, h) && step(i, j - 1, false, t, veld, b, h))
                        opties[0]=code(i, j - 2, 2, t);

                    // Kan ik omlaag?  Ik ga dan NZ-liggen op i,j+1
                    if (step(i, j + 1, false, t, veld, b, h) && step(i, j + 2, false, t, veld, b, h))
                        opties[0]=code(i, j + 1, 2, t);
                    break;

                case 2:  // Cave ligt NZ op i,j (dwz pos i,j en i,j+1)
                         // Kan ik naar rechts, NZlig op i+1,j 
                    if (step(i + 1, j, false, t, veld, b, h) && step(i + 1, j + 1, false, t, veld, b, h))
                        opties[0]=code(i + 1, j, 2, t);

                    // Kan ik naar Links, NZ liggen op i-1,j
                    if (step(i - 1, j, false, t, veld, b, h) && step(i - 1, j + 1, false, t, veld, b, h))
                        opties[0]=code(i - 1, j, 2, t);

                    // Kan ik naar boven, staan op i,j-1
                    if (step(i, j - 1, true, t, veld, b, h))
                        opties[0]=code(i, j - 1, 1, sw(i, j - 1, t, veld, b, h));

                    // Kan ik naar beneden, staan op i, j+2
                    if (step(i, j + 2, true, t, veld, b, h))
                        opties[0]=code(i, j + 2, 1, sw(i, j + 2, t, veld, b, h));
                    break;

                case 3:  // Cave ligt EW op i,j (dwz bezet i,j en i+1,j)
                         // Kan ik naar rechts, gaan staan op i+2,j
                    if (step(i + 2, j, true, t, veld, b, h))
                        opties[0]=code(i + 2, j, 1, sw(i + 2, j, t, veld, b, h));

                    // Kan ik naar links, staan op i-1,j
                    if (step(i - 1, j, true, t, veld, b, h))
                        opties[0]=code(i - 1, j, 1, sw(i - 1, j, t, veld, b, h));

                    // Kan ik naar beneden, gaan EW-liggen op i,j+1
                    if (step(i, j + 1, false, t, veld, b, h) && step(i + 1, j + 1, false, t, veld, b, h))
                        opties[0]=code(i, j + 1, 3, t);

                    // Kan ik omhoog, gaan EW-liggen op i,j-1
                    if (step(i, j - 1, false, t, veld, b, h) && step(i + 1, j - 1, false, t, veld, b, h))
                        opties[0]=code(i, j - 1, 3, t);
                    break;
                default: break;
            }
}

void Adj(int tc, int p, __local int* opties, __global int* veld, int b, int h)
    {
       if(tc==1)
	   {
			Yadj(p, opties, veld, b, h);
	   }
	   else
	   {
			Badj(p, opties, veld, b, h);
	   }
    }



__kernel void device_function( 
	__global int* pos_in, __global int* pa,
	int tc, int N_in, __global int* pos_uit, __global int* veld,
	int b, int h)
	{
		int id = get_global_id(0);
		int positie = pos_in[id];
	}

