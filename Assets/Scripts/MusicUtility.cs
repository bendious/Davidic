using System;
using UnityEngine;


public static class MusicUtility
{
	public const uint semitonesPerOctave = 12U;
	public const uint secondsPerMinute = 60U;

	public const uint midiMiddleAKey = 57U;

	public static readonly uint[] majorScaleSemitones = {
		0, 2, 4, 5, 7, 9, 11
	};
	public static readonly uint[] naturalMinorScaleSemitones = {
		0, 2, 3, 5, 7, 8, 10
	};
	public static readonly uint[] harmonicMinorScaleSemitones = {
		0, 2, 3, 5, 7, 8, 11
	};
	//public static readonly uint[] melodicMinorScaleSemitones = {
	//	0, 2, 3, 5, 7, 9, 11 ascending
	//	0, 2, 3, 5, 7, 8, 10 descending
	//};
	public static readonly uint[] dorianModeSemitones = {
		0, 2, 3, 5, 7, 9, 10
	};
	// Ionian mode is the same as the major scale
	public static readonly uint[] phrygianModeSemitones = {
		0, 1, 3, 5, 7, 8, 10
	};
	public static readonly uint[] lydianModeSemitones = {
		0, 2, 4, 6, 7, 9, 11
	};
	public static readonly uint[] mixolydianModeSemitones = {
		0, 2, 4, 5, 7, 9, 10
	};
	// Aeolian mode is the same as natural minor scale
	public static readonly uint[] locrianModeSemitones = {
		0, 1, 3, 5, 6, 8, 10
	};
	public static readonly uint[][] scales = {
		majorScaleSemitones,
		naturalMinorScaleSemitones,
		harmonicMinorScaleSemitones,
		//melodicMinorScaleSemitones,
		dorianModeSemitones,
		phrygianModeSemitones,
		lydianModeSemitones,
		mixolydianModeSemitones,
		locrianModeSemitones
	};


	public static int scaleOffset(uint[] scaleSemitones, int noteIndex)
	{
		int scaleLength = scaleSemitones.Length;
		int octaveOffset = noteIndex / scaleLength - (noteIndex < 0 ? 1 : 0);
		return octaveOffset * (int)MusicUtility.semitonesPerOctave + (int)scaleSemitones[Utility.modulo(noteIndex, scaleLength)];
	}

	public static int tonesToSemitones(float tonesFromRoot, uint[] scaleSemitones)
	{
		// NOTE that due to note averaging for off-chord notes, tonesFromRoot can have unexpected fractional values, so we "round" to the nearest half tone to turn them into standard naturals/flats/sharps
		float scaleLength = scaleSemitones.Length;
		float tonesMod = Utility.modulo(tonesFromRoot, scaleLength);
		Debug.Assert(tonesMod >= 0.0f && tonesMod < scaleLength);
		int octaveOffset = (int)(tonesFromRoot / scaleLength) + (tonesFromRoot < 0.0f ? -1 : 0);
		float fractAbs = Mathf.Abs(Utility.fract(tonesFromRoot));
		int halftoneOffset = (fractAbs <= 0.333f || fractAbs >= 0.667f) ? 0 : (tonesFromRoot < 0.0f ? -1 : 1);

		return (int)(scaleSemitones[(uint)tonesMod] + octaveOffset * (int)semitonesPerOctave + halftoneOffset);
	}
}
