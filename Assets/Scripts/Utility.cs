using System;
using System.Linq;
using UnityEngine.Assertions;


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

	public static T RandomWeighted<T>(T[] values, float[] weights)
	{
		// NOTE the array slice to handle values[] w/ shorter length than weights[] by ignoring the excess weights; the opposite situation works out equivalently w/o explicit handling since weightRandom will never result in looping beyond the number of weights given
		float weightSum = new ArraySegment<float>(weights, 0, Math.Min(values.Length, weights.Length)).Sum();
		float weightRandom = UnityEngine.Random.Range(0.0f, weightSum);

		int idxItr = 0;
		while (weightRandom >= weights[idxItr])
		{
			weightRandom -= weights[idxItr];
			++idxItr;
		}

		Assert.IsTrue(weightRandom >= 0.0f && idxItr < values.Length);
		return values[idxItr];
	}
}
