using System;
using System.Collections.Generic;
using System.Diagnostics;
using Cloo;
using System.Linq;

/*

 In deze source file vind je een implementatie van de Caveman solver waarvan 
 de data layout al is aangepast aan gpgpu berekeningen. Zie ter vergelijking 
 de code in CavemanOrigineel.cs.

 - De queue is vervangen door arrays met een vaste grootte. Let op: de grootte
   van deze arrays bepaald het maximum aantal simultane taken (zie: waves).
 - De dictionary is vervangen door een bitfield. De maximale map grootte is
   256x256; per positie kan caveman op 4 manieren staan of liggen; voor elke
   positie kunnen maximaal 11 schakelaars aan of uit staan. We komen dan op
   256x256x4x(2^11) unieke states. Voor elke state geeft een enkel bit aan of
   deze state eerder is gezien. In een uint passen 32 bits. Alle unieke states
   passen nu in een uint array met een grootte van 128MB. Dit lijkt veel, maar
   zowel de CPU als de GPU hebben gigabytes aan ruimte; dit is dus geen enkel
   probleem.
   Een tweede bitveld slaat op hoe de betreffende state bereikt is. Dit kan op
   4 manieren; er zijn dus 2 bits per unieke state nodig. Dit levert een
   array van 256MB op.

 Oplossen in 'waves':

   Bij het oplossen van een BFS probleem zijn er een aantal kermerken van de
   GPU van belang:

   - Een GPU kan in een kernel geen threads aanmaken: voor elke 'invocation'
     staat het aantal threads dus vantevoren vast.
   - De GPU is effectief wanneer een groot aantal threads beschikbaar is.
   - Het is niet mogelijk om alle threads te synchroniseren, d.w.z. de
     garantie dat alle threads op de GPU een bepaald punt in de code bereikt
	 hebben kan niet gegeven worden.
   
   Een manier om binnen deze berperkingen een BFS uit te voeren is met behulp
   van wavefronts.

   Het uitvoeren van een BFS met een wavefront gaat als volgt:

   1. Stel een lijst taken op. Voor de eerste wave is dit alleen de 
      beginpositie.
   2. De CPU start de kernel, die alle input states evalueert. De kernel 
      genereert een aantal vervolgstates. Deze worden naar een tweede lijst 
	  geschreven.
   3. De kernel is nu klaar, en geeft de controle terug aan de CPU.
   4. De CPU leest uit het GPU geheugen een teller die aangeeft hoeveel nieuwe
      taken er gegenereert zijn.
   5. De CPU wisselt input- en output array om en gaat verder met stap 2.
   6. Zodra een oplossing gevonden is wordt deze loop afgebroken.

 Deze werkwijze is hieronder al geimplementeerd voor de CPU. De opdracht is om 
 dit nu op de GPU te implementeren. Dit kan het best in fases gebeuren:

   FASE 1. Implementeer method Adj op de GPU.

   - Maak de CPU data beschikbaar voor de GPU met ComputeBuffers;
   - Converteer de C# code naar OpenCL;
   - Roep de kernel aan;
   - Haal de data die veranderd is terug;
   - Verwerk deze data op de CPU met de code die ook voor de CPU implementatie 
     gebruikt wordt (na de foreach (int v in opties)).

   Werk in deze fase in kleine stappen: zorg steeds dat je werkende code hebt 
   voordat je uitbreidt. Haal steeds data terug naar de CPU om te controleren 
   dat alles klopt, dit kan er later weer uit.

   FASE 2. Implementeer de resterende code op de GPU.

   - Stuur nu ook de bitfields naar de GPU;
   - Vind een manier om de GPU de CPU te laten weten dat een oplossing is
     gevonden;
   - Haal alleen de data terug die ook echt terug moet.

   Let erop dat elk transport van data van en naar de GPU relatief veel tijd 
   kost. Het is mogelijk om dit te minimaliseren:

   1. Voor de eerste wave moet de pos_in data naar de GPU, maar de pos_uit 
      data niet (deze wordt niet gelezen maar alleen beschreven).
   2. Na elke wave moet 'N_uit' terug naar de CPU.
   3. Alleen na de laatste wave moet 'prevmove' terug naar de CPU om de 
      uiteindelijke oplossing te kunnen afdrukken.

 Test tenslotte je oplossing met de maps in de 'maps' folder. Je zult merken
 dat de grotere maps aanzienlijk sneller worden opgelost door de GPU. Voor de
 kleine maps is de overhead te groot en zal de GPU juist langzamer zijn.

*/


namespace Caveman
{

    class Caveman
    {
        int b, h, tt;       // Breedte, Hoogte, typen Toverschotsen
        int start;          // Gegeven startconfiguratie
        char[,] veld;       // Kaart van het eiland
        int tc;         // Uitvoermodus (Length Path Animation), switch color

        static Stopwatch timer = new Stopwatch();

        const int NMAX = 16384 * 4;
        const int SCHAKELAARS = 12;             // nodig voor dyn1.in; array gezien wordt 32Mb

        int[] pos_in = new int[NMAX];           // array met posities die nog geevalueerd moeten worden (32Kb)
        int[] pos_uit = new int[NMAX];          // array met posities voor de volgende ronde (32Kb)
        int N_in;                               // aantal te evalueren posities
        int N_uit;                              // aantal nieuwe posities
        uint[] gezien = new uint[8192 * (1 << SCHAKELAARS)]; // bit field
        uint[] prevmove = new uint[2 * 8192 * (1 << SCHAKELAARS)]; // bit field

        public Caveman(string file)
        {
            // Lees de invoer in volgens de specificatie
            // Eerste regel heeft b, h, tt, m
            string[] lines = System.IO.File.ReadAllLines(file);
            string[] line = lines[0].Split(' ');
            // string[] line = { "8", "5", "0", "P" }; // Console.ReadLine().Split(' ');
            b = Int32.Parse(line[0]);
            h = Int32.Parse(line[1]);
            tt = Int32.Parse(line[2]);
            // Deze implementatie kan zowel blauwe als gele schotsen aan met 5e parameter.
            if (line.Length > 4 && line[4] == "Y") tc = 1; else tc = 2;                         //Y wordt 1, B wordt 2
            Console.Write("map: " + file + " (" + b + " x " + h + ", type " + tc + ")\n");

            // Nu komt de kaart; let op, Y is een X met Caveman
            veld = new char[b, h];
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

        private bool IsGezien(int c)
        {
            int idx = c >> 5;
            uint mask = 1U << (c & 31);
            return (gezien[idx] & mask) != 0;
        }

        private void Gezien(int c)
        {
            int idx = c >> 5;
            uint mask = 1U << (c & 31);
            gezien[idx] |= mask;
        }

        private void SetMove(int c, int m)
        {
            int idx = c >> 4;
            uint mask = ((uint)m) << (2 * (c & 15));
            prevmove[idx] |= mask;
        }

        private int GetMove(int c)
        {
            int idx = c >> 4;
            uint mask = 3U << (2 * (c & 15));
            return (int)((prevmove[idx] & mask) >> (2 * (c & 15)));
        }

        public void BFS()
        {
            // initialiseer wavefront search
            N_in = 1; // alleen de beginpositie
            pos_in[0] = start;
            Gezien(start);
            Console.WriteLine(pos_in[1]);
            int stappen = 0;
            int meesteOpties = 0;
            int oplossing = -1;

            timer.Reset();
            timer.Start();
            

            int[] pa = new int[N_in * 4];

            int[] N_uitArray = new int[1];


            var flagwrite = ComputeMemoryFlags.WriteOnly | ComputeMemoryFlags.UseHostPointer;

            ComputeBuffer<int> pos_inBuffer = new ComputeBuffer<int>(Program.context, flagwrite, pos_in);
            ComputeBuffer<int> paBuffer = new ComputeBuffer<int>(Program.context, flagwrite, pa);
            N_uitArray[0] = 0;
            ComputeBuffer<int> N_uitBuffer = new ComputeBuffer<int>(Program.context, flagwrite, N_uitArray);

            while (true)
            {
                // verwerk alle posities die nog geevalueerd moeten worden (in pos_in, aantal is N_in)
                if (Program.GPU)
                {
                    Program.kernel.SetMemoryArgument(0, pos_inBuffer);                      // stel de parameter in
                    Program.kernel.SetMemoryArgument(1, paBuffer);                          // stel de parameter in
                    Program.kernel.SetValueArgument<int>(2, (int)tc);                       // stel de parameter in
                    Program.kernel.SetValueArgument<int>(3, (int)N_in);                     // stel de parameter in
                    
                    Program.kernel.SetMemoryArgument(4, N_uitBuffer);                             // stel de parameter in

                    long[] workSize = { 512, 512 };                                         // totaal aantal taken
                    long[] localSize = { 32, 4 };								            // threads per workgroup
                    Program.queue.Execute(Program.kernel, null, workSize, null, null);      // voer de kernel uit
                    Program.queue.ReadFromBuffer(pos_inBuffer, ref pos_in, true, null);     // haal de data terug
                    Program.queue.ReadFromBuffer(N_uitBuffer, ref N_uitArray, true, null);		// haal de data terug
                    Console.WriteLine(N_uitArray[0]);
                    if  (N_uitArray[0] == 30000)
                    {
                        Console.WriteLine("ja");
                    }
                    /*
                    if (data[0] == 76)
                    {
                        Console.WriteLine("ja");
                    }
                    else
                    {
                        Console.WriteLine("nee");
                    }*/
                }
                else
                {
                    // cpu versie
                    N_uit = 0;
                    for (int i = 0; i < N_in; i++)
                    {
                        int p = pos_in[i];
                        List<int> opties = Adj(p);
                        foreach (int v in opties) if (!IsGezien(v))
                            {
                                pos_uit[N_uit] = v;
                                // maak het bitpatroon voor de stack
                                int bits = 0;
                                if (xpositie(v) > xpositie(p)) bits = 0; // naar rechts gegaan
                                if (xpositie(v) < xpositie(p)) bits = 1; // naar links gegaan
                                if (ypositie(v) < ypositie(p)) bits = 2; // naar boven gegaan
                                if (ypositie(v) > ypositie(p)) bits = 3; // naar beneden gegaan
                                                                         // sla het bitpatroon op
                                SetMove(v, bits);
                                // markeer deze state als 'gezien'
                                Gezien(v);
                                // check, misschien zijn we er al
                                if (good(v)) oplossing = v;
                                N_uit++;
                            }
                    }
                    if (oplossing > -1) break;  // correct pad gevonden; klaar
                    if (N_uit == 0) break;      // geen oplossing
                                                // kopieer pos_uit naar pos_in voor de volgende ronde
                    if (N_uit > meesteOpties) meesteOpties = N_uit;
                    N_in = N_uit;
                    for (int i = 0; i < N_uit; i++) pos_in[i] = pos_uit[i];
                }
                stappen++;
            }

            // verstreken tijd
            Console.Write("tijd: " + timer.ElapsedMilliseconds + "ms (" + (Program.GPU ? "GPU" : "CPU") + "), " + (stappen + 1) + " stappen:\n");

            // decodeer oplossing
            string[] code = { "R", "L", "U", "D" };
            string result = "";
            for (int i = 0; i <= stappen; i++)
            {
                if (richting(oplossing) == 1) oplossing = this.code(xpositie(oplossing), ypositie(oplossing), 1, sw(xpositie(oplossing), ypositie(oplossing), schakelaars(oplossing)));
                int bits = GetMove(oplossing);
                if (richting(oplossing) == 1) oplossing = this.code(xpositie(oplossing), ypositie(oplossing), 1, sw(xpositie(oplossing), ypositie(oplossing), schakelaars(oplossing)));
                result = code[bits] + result;
                foreach (int p in Adj(oplossing))
                {
                    if (xpositie(oplossing) > xpositie(p) && bits == 0) { oplossing = p; break; }
                    if (xpositie(oplossing) < xpositie(p) && bits == 1) { oplossing = p; break; }
                    if (ypositie(oplossing) < ypositie(p) && bits == 2) { oplossing = p; break; }
                    if (ypositie(oplossing) > ypositie(p) && bits == 3) { oplossing = p; break; }
                }
            }
            if (Program.Output) Console.Write(result);

            // rapporteer potential parallelism
            Console.Write("\nmaximum aantal taken: " + meesteOpties + ".\n");
        }

        private List<int> Adj(int p)
        {
            // Kies tussen de Adj fschakelaarsie voor Yellow of Blue switches 
            if (tc == 1) return YAdj(p); else return BAdj(p);
        }

        // Yellow switch: werkt als je ligt of staat
        // Blue switch: werkt alleen bij staan.
        // Bij gemengde types moet je aan de fschakelaarsie sw mee geven of je ligt of staat

        // Hieronder volgt de Y-Adj voor een eiland met Yellow

        private List<int> YAdj(int p)
        {
            // Wat zijn de opvolgers van state p??
            List<int> pa = new List<int>();
            // Decodeer de state
            int i = xpositie(p); int j = ypositie(p);
            int s = richting(p); int t = schakelaars(p);
            switch (s)
            {
                case 1:  // Cave staat rechtop op i,j
                         // Kan ik naar Rechts?  Ik ga dan EW-liggen op i+1,j
                    if (step(i + 1, j, false, t) && step(i + 2, j, false, t))
                        pa.Add(code(i + 1, j, 3, sw(i + 1, j, sw(i + 2, j, t))));

                    // Kan ik naar Links?  Ik ga dan EW-liggen op i-2,j
                    if (step(i - 1, j, false, t) && step(i - 2, j, false, t))
                        pa.Add(code(i - 2, j, 3, sw(i - 2, j, sw(i - 1, j, t))));

                    // Kan ik omhoog?  Ik ga dan NZ-liggen op i,j-2
                    if (step(i, j - 2, false, t) && step(i, j - 1, false, t))
                        pa.Add(code(i, j - 2, 2, sw(i, j - 2, sw(i, j - 1, t))));

                    // Kan ik omlaag?  Ik ga dan NZ-liggen op i,j+1
                    if (step(i, j + 1, false, t) && step(i, j + 2, false, t))
                        pa.Add(code(i, j + 1, 2, sw(i, j + 1, sw(i, j + 2, t))));
                    break;

                case 2:  // Cave ligt NZ op i,j (dwz pos i,j en i,j+1)
                         // Kan ik naar rechts, NZlig op i+1,j 
                    if (step(i + 1, j, false, t) && step(i + 1, j + 1, false, t))
                        pa.Add(code(i + 1, j, 2, sw(i + 1, j, sw(i + 1, j + 1, t))));

                    // Kan ik naar Links, NZ liggen op i-1,j
                    if (step(i - 1, j, false, t) && step(i - 1, j + 1, false, t))
                        pa.Add(code(i - 1, j, 2, sw(i - 1, j, sw(i - 1, j + 1, t))));

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
            int i = xpositie(p); int j = ypositie(p);
            int s = richting(p); int t = schakelaars(p);
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
            return t ^ (1 << ((int)veld[x, y] - (int)'A'));
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
            return (veld[xpositie(v), ypositie(v)] == 'Z' && richting(v) == 1);
        }

        // Coderen van een state naar een int.
        // x gaat in bit 0..7, y in 8..15, richting in 16..17, schakelaars in 18.. .
        private int code(int x, int y, int richting, int schakelaars)
        {
            return (schakelaars << 18) + (richting << 16) + (y << 8) + x;
        }
        private int xpositie(int p) { return p % 256; }
        private int ypositie(int p) { return (p / 256) % 256; }
        private int richting(int p) { return (p / 65536) % 4; }
        private int schakelaars(int p) { return (p / 262144); }
    }

} // namespace Caveman
