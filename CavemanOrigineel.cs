using System;
using System.Collections.Generic;
using System.Diagnostics;
using Cloo;

namespace Caveman
{

class CavemanOrigineel
{
    int b, h, tt;     // Breedte, Hoogte, typen Toverschotsen
    int start, end;   // Gegeven startconfiguratie, gevonden eind
    char[,] veld;     // Kaart van het eiland
    char m, tc;       // Uitvoermodus (Length Path Animation), switch color

    Dictionary<int, int> pi;   // Backpointers in BFS

	static Stopwatch timer = new Stopwatch();
	
    public CavemanOrigineel( string file )
    {
		// Lees de invoer in volgens de specificatie
		// Eerste regel heeft b, h, tt, m
		string[] lines = System.IO.File.ReadAllLines( file );
		string[] line = lines[0].Split( ' ' );
		// string[] line = { "8", "5", "0", "P" }; // Console.ReadLine().Split(' ');
		b = Int32.Parse(line[0]);
		h = Int32.Parse(line[1]);
		tt = Int32.Parse(line[2]);
		m = 'P'; // line[3][0];
		Console.Write( "map: " + file + " (" + b + " x " + h + ", type " + m + ")\n" );
		// Deze implementatie kan zowel blauwe als gele schotsen aan met 5e parameter.
		if (line.Length > 4 && line[4] == "Y") tc = 'Y'; else tc = 'B';

		// Nu komt de kaart; let op, Y is een X met Caveman
		veld = new char[b,h];
		for (int j = 0; j < h; j++)
		{
			string l = lines[j + 1];
			for (int i = 0; i < b; i++) if (l[i] == 'Y')
			{
				start = code(i, j, 1, 0);  // 1 = staand, 0 = tover open
				veld[i, j] = 'X';
			}
			else veld[i, j] = l[i];
		}
    }

    public void BFS()
    {
        pi = new Dictionary<int, int>();   // Backpointers voor later gebruik
        Queue<int> Q = new Queue<int>();   // Queue alleen lokaal nodig
        int u;                             // Te exploreren element

		timer.Reset();
		timer.Start();

        end = 0;                           // Nog geen eindpositie gevonden
        pi.Add(start, 0);                  // Start heeft pi=0
        Q.Enqueue(start);                  // Begin BFS
        while (Q.Count > 0 && end == 0)
        {
            u = Q.Dequeue();
            foreach (int v in Adj(u))
            {
                if (! pi.ContainsKey(v))
                {
                    pi.Add(v, u); Q.Enqueue(v);
                    // Zorg voor terminatie bij succes
                    if (good(v)) end = v; 
                }
            }
        }

		// verstreken tijd
		Console.Write( "tijd: " + timer.ElapsedMilliseconds + "ms\n" );

		Geefpad();
    }

    private List<int> Adj(int p)
    {
        // Kies tussen de Adj functie voor Yellow of Blue switches 
        if (tc == 'Y')
            return YAdj(p);
        else return BAdj(p);
    }

    // Yellow switch: werkt als je ligt of staat
    // Blue switch: werkt alleen bij staan.
    // Bij gemengde types moet je aan de functie sw mee geven of je ligt of staat

    // Hieronder volgt de Y-Adj voor een eiland met Yellow

    private List<int> YAdj(int p)
    {
        // Wat zijn de opvolgers van state p??
        List<int> pa = new List<int>();
        // Decodeer de state
        int i = unci(p); int j = uncj(p);
        int s = uncs(p); int t = unct(p);
        switch (s)
        {
            case 1:  // Cave staat rechtop op i,j
                // Kan ik naar Rechts?  Ik ga dan EW-liggen op i+1,j
                if (step(i+1, j, false, t) && step(i+2, j, false, t)) 
                    pa.Add(code(i+1, j, 3, sw(i+1,j,sw(i+2,j,t))));

                // Kan ik naar Links?  Ik ga dan EW-liggen op i-2,j
                if (step(i-1, j, false, t) && step(i-2, j, false, t)) 
                    pa.Add(code(i-2, j, 3, sw(i-2,j,sw(i-1,j,t))));

                // Kan ik omhoog?  Ik ga dan NZ-liggen op i,j-2
                if (step(i, j-2, false, t) && step(i, j-1, false, t)) 
                    pa.Add(code(i, j-2, 2, sw(i,j-2,sw(i,j-1,t))));

                // Kan ik omlaag?  Ik ga dan NZ-liggen op i,j+1
                if (step(i, j+1, false, t) && step(i, j+2, false, t)) 
                    pa.Add(code(i, j+1, 2, sw(i,j+1,sw(i,j+2,t))));
                break;

            case 2:  // Cave ligt NZ op i,j (dwz pos i,j en i,j+1)
                // Kan ik naar rechts, NZlig op i+1,j 
                if (step(i + 1, j, false, t) && step(i + 1, j + 1, false, t)) 
                    pa.Add(code(i + 1, j, 2, sw(i + 1, j, sw(i + 1, j + 1, t))));

                // Kan ik naar Links, NZ liggen op i-1,j
                if (step(i - 1, j, false, t) && step(i - 1, j + 1, false, t))
                    pa.Add(code(i - 1, j, 2, sw(i - 1, j, sw(i - 1, j + 1, t))));

                // Kan ik naar boven, staan op i,j-1
                if (step(i, j-1, true, t))
                    pa.Add(code(i, j-1, 1, sw(i,j-1,t)));

                // Kan ik naar beneden, staan op i, j+2
                if (step(i, j + 2, true, t))
                    pa.Add(code(i, j + 2, 1, sw(i, j + 2, t)));
                break;

            case 3:  // Cave ligt EW op i,j (dwz bezet i,j en i+1,j)
                // Kan ik naar rechts, gaan staan op i+2,j
                if (step(i + 2, j, true, t))
                    pa.Add(code(i + 2, j, 1, sw(i + 2, j, t)));

                // Kan ik naar links, staan op i-1,j
                if (step(i - 1, j, true, t))
                    pa.Add(code(i - 1, j, 1, sw(i - 1, j, t)));

                // Kan ik naar beneden, gaan EW-liggen op i,j+1
                if (step(i, j + 1, false, t) && step(i + 1, j + 1, false, t))
                    pa.Add(code(i, j + 1, 3, sw(i, j + 1, sw(i + 1, j + 1, t))));

                // Kan ik omhoog, gaan EW-liggen op i,j-1
                if (step(i, j - 1, false, t) && step(i + 1, j - 1, false, t))
                    pa.Add(code(i, j - 1, 3, sw(i, j - 1, sw(i + 1, j - 1, t))));
                break;
            default: break;
        }
        return pa;
    }

    // Hieronder volgt de B-Adj voor een eiland met Blue
    private List<int> BAdj(int p)
    {
        // Wat zijn de opvolgers van state p??
        List<int> pa = new List<int>();
        // Decodeer de state
        int i = unci(p); int j = uncj(p);
        int s = uncs(p); int t = unct(p);
        switch (s)
        {
            // We zitten hier met blauwe switches, die doen niets als je er
            // op gaat liggen.  Dus alleen in de staande nieuwe posities
            // wordt sw meegenomen.
            case 1:  // Cave staat rechtop op i,j
                // Kan ik naar Rechts?  Ik ga dan EW-liggen op i+1,j
                if (step(i + 1, j, false, t) && step(i + 2, j, false, t))
                    pa.Add(code(i + 1, j, 3, t));

                // Kan ik naar Links?  Ik ga dan EW-liggen op i-2,j
                if (step(i - 1, j, false, t) && step(i - 2, j, false, t))
                    pa.Add(code(i - 2, j, 3, t));

                // Kan ik omhoog?  Ik ga dan NZ-liggen op i,j-2
                if (step(i, j - 2, false, t) && step(i, j - 1, false, t))
                    pa.Add(code(i, j - 2, 2, t));

                // Kan ik omlaag?  Ik ga dan NZ-liggen op i,j+1
                if (step(i, j + 1, false, t) && step(i, j + 2, false, t))
                    pa.Add(code(i, j + 1, 2, t));
                break;

            case 2:  // Cave ligt NZ op i,j (dwz pos i,j en i,j+1)
                // Kan ik naar rechts, NZlig op i+1,j 
                if (step(i + 1, j, false, t) && step(i + 1, j + 1, false, t))
                    pa.Add(code(i + 1, j, 2, t));

                // Kan ik naar Links, NZ liggen op i-1,j
                if (step(i - 1, j, false, t) && step(i - 1, j + 1, false, t))
                    pa.Add(code(i - 1, j, 2, t));

                // Kan ik naar boven, staan op i,j-1
                if (step(i, j - 1, true, t))
                    pa.Add(code(i, j - 1, 1, sw(i, j - 1, t)));

                // Kan ik naar beneden, staan op i, j+2
                if (step(i, j + 2, true, t))
                    pa.Add(code(i, j + 2, 1, sw(i, j + 2, t)));
                break;

            case 3:  // Cave ligt EW op i,j (dwz bezet i,j en i+1,j)
                // Kan ik naar rechts, gaan staan op i+2,j
                if (step(i + 2, j, true, t))
                    pa.Add(code(i + 2, j, 1, sw(i + 2, j, t)));

                // Kan ik naar links, staan op i-1,j
                if (step(i - 1, j, true, t))
                    pa.Add(code(i - 1, j, 1, sw(i - 1, j, t)));

                // Kan ik naar beneden, gaan EW-liggen op i,j+1
                if (step(i, j + 1, false, t) && step(i + 1, j + 1, false, t))
                    pa.Add(code(i, j + 1, 3, t));

                // Kan ik omhoog, gaan EW-liggen op i,j-1
                if (step(i, j - 1, false, t) && step(i + 1, j - 1, false, t))
                    pa.Add(code(i, j - 1, 3, t));
                break;
            default: break;
        }
        return pa;
    }

    private int sw(int x, int y, int t)
    {
        // Hoe verandert de toverschotsenstand als Caveman op x,y stapt?
        if (veld[x, y] < 'A' || veld[x, y] > 'L')  // Dit is geen switch
            return t; 
        // XOR t met de bitpositie die je afleidt uit de geraakte switch
        return t ^ (1 << ( (int)veld[x,y] - (int)'A' ));
    }

    private bool step(int x, int y, bool ver, int T)
    {
        // Kan Caveman op tegel x,y als hij ver?staat:ligt en T is de set toegankelijke tovertegels
        if (x < 0 || x >= b) return false;     // te ver links of rechts
        if (y < 0 || y >= h) return false;     // te hoog of laag
        if (veld[x, y] == '.') return false;   // mag niet in de zee
        if (veld[x, y] == 'X') return true;    // stevige tegel, kan wel
        if (veld[x, y] == 'Z') return true;    // vuur, kan wel
        if (veld[x, y] == 'x') return !ver;    // zwakke schots, staan mag niet liggen wel
        if (veld[x, y] >= 'A' &&
            veld[x, y] <= 'L') return true;    // Schakelaar, kan wel
        // Overgebleven case: toverschots, check in T
        return (T & (1 << (veld[x, y] - 'a'))) != 0;
    }

    private bool good(int v)
    {
        // Toestand v is goed als eindstand als Caveman op vuur staat, rechtop
        return (veld[unci(v), uncj(v)] == 'Z' && uncs(v) == 1);
    }


    public void Geefpad()
    {
        // We gaan vanaf de gevonden eindtoestand eind terug door pi af te lopen.
        LinkedList<int> Pad = new LinkedList<int>();
        Pad.AddFirst(end); 
        int prev; 
        pi.TryGetValue(end, out prev);
        while (prev != 0)
        {
            Pad.AddFirst(prev);
            pi.TryGetValue(prev, out prev);
        }
        // Pad heeft de lijst van alle states; stappen is 1 minder
        Console.WriteLine(Pad.Count - 1);
        // In Length modus was dit het wel
        if (m == 'L') return;

        // Voor andere uitvoermodi, geef lijst van stappen LRUD
        // Vergelijk de coordinaten met die van voorganger
        foreach (int v in Pad)
        {
            if (v == start) continue;
            int p; pi.TryGetValue(v, out p);
            char s = ' '; 
            if (unci(v) > unci(p)) s = 'R';
            if (unci(v) < unci(p)) s = 'L';
            if (uncj(v) < uncj(p)) s = 'U';
            if (uncj(v) > uncj(p)) s = 'D';
            Console.Write(s);
        }
        Console.WriteLine();
        // Voor de Path modus was dat het wel
        if (m == 'P') return;

        // Animation mode, teken de configuraties
        Console.WriteLine();
        foreach (int v in Pad)
        {
            // Decodeer de state
            int i = unci(v); int j = uncj(v);
            int s = uncs(v); int t = unct(v);
            for (int y = 0; y < h; y++)
            {
                if (y == 0)
                {
                    char c = ' ';
                    int p; pi.TryGetValue(v, out p);
                    if (unci(v) > unci(p)) c = 'R';
                    if (unci(v) < unci(p)) c = 'L';
                    if (uncj(v) < uncj(p)) c = 'U';
                    if (uncj(v) > uncj(p)) c = 'D';
                    if (v == start) c = 's';
                    Console.Write(c + ": ");
                }
                else Console.Write("   ");

                for (int x = 0; x < b; x++)
                {
                    char u = veld[x, y];
                    if (x == i && y == j) u = 'Y';
                    else if (s == 2 && x == i && y == j + 1) u = 'Y';
                    else if (s == 3 && x == i + 1 && y == j) u = 'Y';
                    else if (u >= 'a' && u <= 'l')
                        u = ((t & (1 << (u - 'a'))) != 0) ? 'O' : 'o';
                    Console.Write(u);
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }
 
        return;
    }

    // Coding and decoding a configuration in an int.
    // Position of Caveman is i,j, Direction is s, bridges T
    // Put i in bit 0..7, j in 8..15, s in 16..17, T in higher bits.
    private int code(int i, int j, int s, int T)
    {
        return 262144 * T + 65536 * s + 256 * j + i;
    }
    private int unci(int p) { return p % 256; }
    private int uncj(int p) { return (p / 256) % 256; }
    private int uncs(int p) { return (p / 65536) % 4; }
    private int unct(int p) { return (p / 262144); }
}

} // namespace Caveman
