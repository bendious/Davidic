using System;


public static class Utility
{
	public static int Modulo(int x, int m)
	{
		int r = x % m;
		return (r < 0) ? r + m : r;
	}

	public static float Modulo(float x, float m)
	{
		float r = x % m;
		return (r < 0) ? r + m : r;
	}

	public static float Fract(float x)
	{
		return x - (float)Math.Truncate(x);
	}
}
