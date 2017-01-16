int Testt()
{
return 76;
}

__kernel void device_function( 
	__global int* data,
	int tc )
{
	data[0]= Testt();
}

