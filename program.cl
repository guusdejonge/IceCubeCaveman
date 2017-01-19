int Testt()
{
return 76;
}

void Adj(int p, int tc, __local int* LP)
    {
       LP[0] = 3;
    }


__kernel void device_function( 
	__global int* data,
	int tc )
	{
		//data[0]= Testt();

		__local int LP [4];
		LP[0] = 1;
		//Console.WriteLine("hoi");

		Adj(0,0,LP);

		if (LP[0] == 3)
		{
			data[0]= 76;
		}
	}

