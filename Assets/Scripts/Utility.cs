using System;


public static class Utility
{
	public static int modulo(int x, int m)
	{
		int r = x % m;
		return (r < 0) ? r + m : r;
	}

	public static float modulo(float x, float m)
	{
		float r = x % m;
		return (r < 0) ? r + m : r;
	}

	public static float fract(float x)
	{
		return x - (float)Math.Truncate(x);
	}
}
