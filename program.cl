int Testt()
{
return 76;
}

__kernel void device_function( 
	//__global int* a,
	__global int* data )
{
	/*
	int threadID = get_global_id( 0 );
	if (threadID == 0)
	{
		// eerste thread zet a[1] op 111 als a[0] == 999; C# code verifieert dit
		if (a[0] == 999) a[1] = 111;
	}
	*/

	//Testt(__global int* data);

	data[0]= Testt();
	
}

