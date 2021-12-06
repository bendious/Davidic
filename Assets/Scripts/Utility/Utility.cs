using System;
using System.Linq;
#if !UNITY_WEBGL || UNITY_EDITOR
using System.Threading;
#endif
using UnityEngine.Assertions;


public static class Utility
{
#if !UNITY_WEBGL || UNITY_EDITOR
	private static ThreadLocal<Random> m_randomThreadedInternal = new ThreadLocal<Random>(() => new Random()); // TODO: guard against identical seeds if multiple threads start up simultaneously?
	private static Random m_randomThreaded { get => m_randomThreadedInternal.Value; }
#else
	private const Random m_randomThreaded = null;
#endif


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

	public static float Fract(float x) => x - (float)Math.Truncate(x);

	public static int EnumNumTypes<T>()
	{
		return Enum.GetValues(typeof(T)).Length;
	}

#if !UNITY_WEBGL || UNITY_EDITOR
	public static bool IsMainThread { get => !Thread.CurrentThread.IsBackground; } // TODO: more exact identification w/o performance hit?
#else
	public const bool IsMainThread = true;
#endif

	public static float RandomValue { get => IsMainThread ? UnityEngine.Random.value : (float)m_randomThreaded.NextDouble(); } // TODO: use NextSingle() for efficiency once Unity C# supports it

	public static int RandomRange(int minInclusive, int maxExclusive) => IsMainThread ? UnityEngine.Random.Range(minInclusive, maxExclusive) : m_randomThreaded.Next(minInclusive, maxExclusive);

	public static float RandomRange(float minInclusive, float maxInclusive) => IsMainThread ? UnityEngine.Random.Range(minInclusive, maxInclusive) : UnityEngine.Mathf.Lerp(minInclusive, maxInclusive, RandomValue);

	public static T RandomWeighted<T>(T[] values, float[] weights)
	{
		Assert.IsFalse(weights.Any(f => f < 0.0f));

		// NOTE the array slice to handle values[] w/ shorter length than weights[] by ignoring the excess weights; the opposite situation works out equivalently w/o explicit handling since weightRandom will never result in looping beyond the number of weights given
		float weightSum = new ArraySegment<float>(weights, 0, Math.Min(values.Length, weights.Length)).Sum();
		float weightRandom = RandomRange(0.0f, weightSum);

		int idxItr = 0;
		while (weightRandom >= weights[idxItr])
		{
			weightRandom -= weights[idxItr];
			++idxItr;
		}

		Assert.IsTrue(weightRandom >= 0.0f && idxItr < values.Length);
		return values[idxItr];
	}

	public static T RandomWeightedEnum<T>(float[] weights) where T : System.Enum
	{
		/*const*/ int typeCount = EnumNumTypes<T>();
		Assert.IsTrue(weights.Length <= typeCount);
		return RandomWeighted(Enumerable.Range(0, typeCount).Select(i => {
			Assert.IsTrue(Enum.IsDefined(typeof(T), i));
			return (T)Enum.ToObject(typeof(T), i);
		}).ToArray(), weights);
	}
}
