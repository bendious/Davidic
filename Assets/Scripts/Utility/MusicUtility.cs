using UnityEngine;
using UnityEngine.Assertions;


public static class MusicUtility
{
	public const float tonesPerOctave = 7.0f; // NOTE that this excludes the first note of the next octave even though that is often included, e.g. in scales
	public const uint semitonesPerOctave = 12U;
	public const uint secondsPerMinute = 60U;
	public const uint sixtyFourthsPerMeasure = 64U;
	public const uint sixtyFourthsPerBeat = 16U; // TODO: functionize based on time signature

	public const uint midiMiddleAKey = 57U;
	public const uint midiMiddleCKey = 60U;


	public static readonly MusicScale majorScale = new MusicScale(new uint[] {
		0, 2, 4, 5, 7, 9, 11
	}, 0, "major");
	public static readonly MusicScale naturalMinorScale = new MusicScale(new uint[] {
		0, 2, 3, 5, 7, 8, 10
	}, 0, "minor");
	public static readonly MusicScale harmonicMinorScale = new MusicScale(new uint[] {
		0, 2, 3, 5, 7, 8, 11
	}, 0, "minor");
	//public static readonly MusicScale melodicMinorScale = new MusicScale(new uint[] {
	//	0, 2, 3, 5, 7, 9, 11 ascending
	//	0, 2, 3, 5, 7, 8, 10 descending
	//}, 0, "minor");
	public static readonly MusicScale dorianMode = new MusicScale(new uint[] {
		0, 2, 3, 5, 7, 9, 10
	}, 0, "dorian");
	// Ionian mode is the same as the major scale
	public static readonly MusicScale phrygianMode = new MusicScale(new uint[] {
		0, 1, 3, 5, 7, 8, 10
	}, 0, "phrygian");
	public static readonly MusicScale lydianMode = new MusicScale(new uint[] {
		0, 2, 4, 6, 7, 9, 11
	}, 0, "lydian");
	public static readonly MusicScale mixolydianMode = new MusicScale(new uint[] {
		0, 2, 4, 5, 7, 9, 10
	}, 0, "mixolydian");
	// Aeolian mode is the same as natural minor scale
	public static readonly MusicScale locrianMode = new MusicScale(new uint[] {
		0, 1, 3, 5, 6, 8, 10
	}, 0, "locrian");
	public static readonly MusicScale[] scales = {
		majorScale,
		naturalMinorScale,
		harmonicMinorScale,
		//melodicMinorScale,
		dorianMode,
		phrygianMode,
		lydianMode,
		mixolydianMode,
		locrianMode,
	};


	public static readonly float[] chordI = { 0.0f, 2.0f, 4.0f };
	public static readonly float[] chordI7 = { 0.0f, 2.0f, 4.0f, 7.0f };
	public static readonly float[] chordII = { 1.0f, 3.0f, 5.0f };
	public static readonly float[] chordII7 = { 1.0f, 3.0f, 5.0f, 8.0f };
	public static readonly float[] chordIII = { 2.0f, 4.0f, 6.0f };
	public static readonly float[] chordIII7 = { 2.0f, 4.0f, 6.0f, 9.0f };
	public static readonly float[] chordIV = { 3.0f, 5.0f, 7.0f };
	public static readonly float[] chordIV7 = { 3.0f, 5.0f, 7.0f, 10.0f };
	public static readonly float[] chordV = { 4.0f, 6.0f, 8.0f };
	public static readonly float[] chordV7 = { 4.0f, 6.0f, 8.0f, 11.0f };
	public static readonly float[] chordVI = { 5.0f, 7.0f, 9.0f };
	public static readonly float[] chordVI7 = { 5.0f, 7.0f, 9.0f, 12.0f };
	public static readonly float[] chordVII = { 6.0f, 8.0f, 10.0f };
	public static readonly float[] chordVII7 = { 6.0f, 8.0f, 10.0f, 13.0f };

	public static readonly ChordProgression[] chordProgressions =
	{
		// https://www.musictheoryacademy.com/understanding-music/chord-progressions/
		new ChordProgression(new float[][] { chordI, chordIV, chordV }),
		new ChordProgression(new float[][] { chordI, chordV, chordVI, chordIV }),
		new ChordProgression(new float[][] { chordI, chordIV, chordI, chordV, chordI }),
		new ChordProgression(new float[][] { chordI, chordVI, chordII, chordV }),
		new ChordProgression(new float[][] { chordI, chordIV, chordVII, chordIII, chordVI, chordII, chordV, chordI }),
		new ChordProgression(new float[][] { chordI, chordIV, chordV, chordI }),

		// https://en.wikipedia.org/wiki/List_of_chordprogressions
		// TODO: split according to type and match w/ scale used?
		// Major
		new ChordProgression(new float[][] { chordI, chordVI/*m*/, chordIV, chordV }), // 50s progression
		new ChordProgression(new float[][] { chordII/*m*/, new float[] { 0.5f, 3.0f, 6.5f }, chordI7 }), // ii-V-I w/ tritone substitution
		new ChordProgression(new float[][] { new float[] { 1.5f, 3.5f, 5.0f, 7.0f }/*m?*/, new float[] { -3.0f, -1.0f, 1.0f, 4.0f, 6.0f, 10.0f }, chordI7/*+*/ }), // viiO7/V-V-I
		new ChordProgression(new float[][] { chordII/*m*/, new float[] { 1.0f, 3.0f, 4.5f, 5.5f }, chordI }), // Backdoor progression
		new ChordProgression(new float[][] { new float[] { 0.0f, 2.0f, 4.0f, 6.0f }, new float[] { -1.0f, 1.0f, 3.0f, 5.0f }/*m*/, new float[] { 2.0f, 4.5f, 6.0f, 8.0f }, new float[] { -2.0f, 0.0f, 2.0f, 4.0f }/*m*/, new float[] { 1.0f, 3.5f, 5.0f, 7.0f }, new float[] { -3.0f, -1.0f, 1.0f, 3.0f }/*m*/, new float[] { 0.0f, 2.0f, 4.0f, 5.5f }, new float[] { 3.0f, 5.0f, 7.0f, 8.5f }, new float[] { 3.0f, 4.5f, 7.0f, 8.5f }/*m*/, new float[] { -1.5f, 1.0f, 3.0f, 4.5f }, new float[] { 2.0f, 4.0f, 6.0f, 8.0f }/*m*/, new float[] { -2.0f, 0.5f, 2.0f, 4.0f }, new float[] { 1.5f, 3.0f, 5.5f, 6.5f }/*m*/, new float[] { -2.5f, -0.5f, 1.5f, 3.5f }, new float[] { 1.0f, 3.0f, 5.0f, 7.0f }/*m*/, new float[] { -3.0f, -1.0f, 1.0f, 3.0f }, new float[] { 0.0f, 2.0f, 4.0f, 6.0f }, new float[] { -2.0f, 0.5f, 2.0f, 4.0f }, new float[] { 1.0f, 3.0f, 5.0f, 7.0f }/*m*/, new float[] { -3.0f, -1.0f, 1.0f, 3.0f } }), // Bird changes
		new ChordProgression(new float[][] { chordVI/*m*/, chordII/*m*/, chordV, chordI }), // Circle progression
		new ChordProgression(new float[][] { chordI7, new float[] { 1.5f, 5.5f, 7.5f, 11.0f }, new float[] { 4.5f, 7.0f, 8.5f, 11.5f }, new float[] { -1.0f, 3.5f, 5.0f, 8.5f }, new float[] { 2.0f, 4.5f, 6.0f, 9.0f }, new float[] { -3.0f, 1.0f, 3.0f, 6.0f }, chordI7 }), // Coltrane changes
		new ChordProgression(new float[][] { chordI7, chordV7, chordIV7, chordIV7, chordI7, chordV7, chordI7, chordV7 }), // Eight-bar blues
		new ChordProgression(new float[][] { chordII/*m*/, chordV/*+*/, chordI7 }), // ii-V-I progression
		new ChordProgression(new float[][] { chordV7, chordIII7 }), // Irregular resolution
		new ChordProgression(new float[][] { chordI, chordIV, chordII/*m*/, chordV }), // Montgomery-Ward bridge
		new ChordProgression(new float[][] { new float[] { 0.0f, 2.0f, 4.0f, 6.0f }, new float[] { -0.5f, 2.0f, 4.0f, 6.5f }/*m*/, new float[] { -1.0f, 2.0f, 4.0f, 7.0f }, new float[] { -2.0f, 2.0f, 4.0f, 7.5f }, new float[] { -2.5f, 2.0f, 4.5f, 7.5f }/*m*/, new float[] { -3.0f, 2.0f, 5.0f, 7.5f }, new float[] { -3.5f, 2.5f, 6.0f, 7.5f }, new float[] { -4.0f, 3.0f, 6.0f, 7.5f }/*m*/, new float[] { -4.5f, 3.5f, 6.0f, 7.5f }, new float[] { -5.5f, 4.0f, 6.0f, 7.5f }, new float[] { -6.0f, 4.0f, 6.0f, 8.0f }/*m*/, new float[] { -6.5f, 4.0f, 6.0f, 8.5f }, new float[] { -7.0f, 4.0f, 6.0f, 9.0f } }), // Omnibus progression
		new ChordProgression(new float[][] { chordI, chordV, chordVI, chordIII/*m*/, chordIV, chordI, chordIV, chordV }), // Pachelbel's Canon
		new ChordProgression(new float[][] { chordI, chordIV, chordI, chordV, chordI, chordIV, chordI,/*-*/chordV, chordI }), // Passamezzo moderno
		new ChordProgression(new float[][] { chordI,/*-*/chordV, chordVI/*m*/,/*-*/chordIV }), // I-V-vi-IV progression
		new ChordProgression(new float[][] { new float[] { 2.0f, 4.5f, 6.0f, 8.0f }, new float[] { -2.0f, 2.0f, 4.0f, 7.5f }, new float[] { 1.0f, 3.5f, 5.0f, 7.0f }, new float[] { -3.0f, 1.0f, 3.0f, 6.0f } }), // Ragtime progression
		new ChordProgression(new float[][] { chordI, new float[] { -2.0f, 0.0f, 2.0f, 4.0f }, chordII, new float[] { -3.0f, 1.0f, 6.0f }, chordI, new float[] { -2.0f, 0.0f, 2.0f, 4.0f }, chordII, new float[] { -3.0f, 1.0f, 6.0f }, chordI, new float[] { 0.0f, 2.0f, 4.0f, 5.5f }, new float[] { 0.0f, 3.0f, 5.0f }, new float[] { 0.0f, 3.5f, 5.0f }, chordI, new float[] { -3.0f, 1.0f, 3.0f, 6.0f }, chordI7 }), ///*V/V/V/V*/,/*-*//*V/V/V*/,/*-*//*V/V*/,/*-*/4.0f, chordI7 }), // Rhythm changes
		new ChordProgression(new float[][] { new float[] { 0.0f, 2.0f, 4.0f, 9.0f }, new float[] { -3.0f, -1.0f, 1.0f, 8.0f }, new float[] { -2.0f, 0.0f, 2.0f, 7.0f }/*m*/, new float[] { -5.0f, -2.5f, -1.0f, 6.0f }, new float[] { 0.0f, 2.0f, 4.0f, 9.0f }, new float[] { -3.0f, -1.0f, 1.0f, 8.0f }, new float[] { -2.0f, 0.0f, 2.0f, 7.0f }/*m*/,/*-*/new float[] { -5.0f, -2.5f, -1.0f, 6.0f }, new float[] { -2.0f, 0.0f, 2.0f, 5.0f }/*m*/ }), // Romanesca
		new ChordProgression(new float[][] { chordI7, chordI7, chordI7, chordI7, chordI7, chordI7, chordI7, chordI7, chordIV7, chordIV7, chordI7, chordI7, chordV7, chordIV7, chordI7, chordI7 }), // Sixteen-bar blues
		new ChordProgression(new float[][] { new float[] { -4.0f, -2.0f, 0.0f, 1.5f },/*-*/new float[] { -3.5f, -2.0f, 0.5f, 1.5f }, new float[] { -3.0f, -1.5f, 0.0f, 2.0f },/*-*/new float[] { -7.0f, -5.0f, -3.0f, -1.5f, 0.0f }, new float[] { -4.0f, -2.0f, 0.0f, 1.5f },/*-*/new float[] { -3.5f, -2.0f, 0.5f, 1.5f }, new float[] { -3.0f, -1.5f, 0.0f, 2.0f },/*-*/new float[] { -7.0f, -5.0f, -3.0f, -1.5f, 0.0f }, new float[] { -4.0f, -2.0f, 0.0f, 1.5f },/*-*/new float[] { -3.5f, -2.0f, 0.5f, 1.5f }, new float[] { -3.0f, -1.5f, 0.0f, 2.0f },/*-*/new float[] { -2.0f, 0.5f, 2.0f, 4.0f }, new float[] { -6.0f, -3.5f, -2.0f, 0.0f },/*-*/new float[] { -3.0f, -1.0f, 1.0f, 3.0f }, new float[] { -7.0f, -5.0f, -3.0f, -1.5f, 0.0f } }), // Stomp progression
		new ChordProgression(new float[][] { chordI7, chordI7, chordI7, chordI7, chordIV7, chordIV7, chordI7, chordI7, chordV7, chordIV7, chordI7, chordI7 }), // Twelve-bar blues
		new ChordProgression(new float[][] { chordI, new float[] { -2.0f, 2.0f, 4.0f, 7.0f }/*m*/, new float[] { 1.0f, 3.0f, 5.0f, 8.0f }/*m*/, new float[] { -3.0f, 1.0f, 3.0f, 6.0f } }), // Turnaround
		new ChordProgression(new float[][] { new float[] { -3.0f, 1.0f, 3.0f, 6.0f },/*-*/new float[] { 0.0f, 1.5f, 3.0f, 5.0f }, new float[] { 0.0f, 2.0f, 4.0f, 5.5f, 7.0f } }), // V-IV-I turnaround
		// Minor
		new ChordProgression(new float[][] { chordI/*m*/, chordV, chordI/*m*/, /*b*/chordVII, /*b*/chordIII, /*b*/chordVII, chordI/*m*/, chordV, chordI/*m*/, chordV, chordI/*m*/, /*b*/chordVII, /*b*/chordIII, /*b*/chordVII, chordI/*m*/,/*-*/chordV, chordI/*m*/ }), // Folia
		new ChordProgression(new float[][] { new float[] { -2.0f, 0.0f, 2.0f, 7.0f }/*m*/, new float[] { -3.0f, -1.0f, 1.0f, 6.0f }, new float[] { -2.0f, 0.0f, 2.0f, 5.0f }/*m*/, new float[] { -5.0f, 2.5f, -1.0f, 4.5f }, new float[] { 0.0f, 2.0f, 4.0f, 7.0f }, new float[] { -3.0f, -1.0f, 1.0f, 6.0f }, new float[] { -2.0f, 0.0f, 2.0f, 5.0f }/*m*/,/*-*/new float[] { -5.0f, 2.5f, -1.0f, 4.5f }, new float[] { -2.0f, 0.0f, 2.0f, 5.0f }/*m*/ }), // Passamezzo antico
		new ChordProgression(new float[][] { chordI, new float[] { 5.5f, 1.0f, 3.0f }, new float[] { 4.5f, 0.0f, 1.5f }, new float[] { 5.5f, 1.0f, 3.0f } }), // I bVII bVI bVII
		// Mixolodian
		new ChordProgression(new float[][] { new float[] { 0.0f, 4.0f, 7.0f, 9.0f, 11.0f, 14.0f }, new float[] { 0.0f, 3.0f, 7.0f, 10.0f, 12.0f, 14.0f }, new float[] { -1.5f, 3.0f, 5.5f, 8.0f, 10.0f, 12.5f }, new float[] { 0.0f, 3.0f, 7.0f, 10.0f, 12.0f, 14.0f } }), // I-IV-bVII-IV
		new ChordProgression(new float[][] { new float[] { 1.0f, 3.0f, 5.0f, 7.0f }/*m*/, new float[] { 1.5f, 4.0f, 6.0f }, chordI7 }), // bIII+ as dominant substitute
		new ChordProgression(new float[][] { new float[] { -9.0f, -5.0f, 0.0f, 5.0f }, new float[] { -12.0f, -5.0f, -1.0f, 4.0f }, new float[] { -10.0f, -6.0f, -1.0f, 4.0f }, new float[] { -13.0f, -6.0f, -2.0f, 3.0f } }), // Chromatic descending 5-6 sequence
		new ChordProgression(new float[][] { new float[] { 3.0f, 5.5f, 8.0f }, new float[] { 3.0f, 4.0f, 6.0f, 8.0f } }), // bVII-V7 cadence
		// Phrygian dominant
		new ChordProgression(new float[][] { chordIV/*m*/, chordIII, /*b*/new float[] { 1.0f, 3.0f, 5.0f }, chordI }), // Andalusian cadence
	};


	public static int ScaleOffset(MusicScale scale, int noteIndex)
	{
		int scaleLength = scale.m_semitones.Length;
		int octaveOffset = noteIndex / scaleLength - (noteIndex < 0 ? 1 : 0);
		return octaveOffset * (int)semitonesPerOctave + (int)scale.m_semitones[Utility.Modulo(noteIndex, scaleLength)];
	}

	public static int TonesToSemitones(float tonesFromRoot, MusicScale scale)
	{
		// NOTE that due to note averaging for off-chord notes, tonesFromRoot can have unexpected fractional values, so we "round" to the nearest half tone to turn them into standard naturals/flats/sharps
		float scaleLength = scale.m_semitones.Length;
		float tonesMod = Utility.Modulo(tonesFromRoot, scaleLength);
		Assert.IsTrue(tonesMod >= 0.0f && tonesMod < scaleLength);
		int octaveOffset = (int)(tonesFromRoot / scaleLength) + (tonesFromRoot < 0.0f ? -1 : 0);
		float fractAbs = Mathf.Abs(Utility.Fract(tonesFromRoot));
		int halftoneOffset = (fractAbs <= 0.333f || fractAbs >= 0.667f) ? 0 : (tonesFromRoot < 0.0f ? -1 : 1);

		return (int)(scale.m_semitones[(uint)tonesMod] + octaveOffset * (int)semitonesPerOctave + halftoneOffset);
	}
}
