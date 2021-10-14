using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;


public class MusicRhythm
{
	private readonly uint[] m_lengthsSixtyFourths;
	private readonly float[] m_chordIndices;


	public static MusicRhythm Random(ChordProgression chords, float[] noteLengthWeights)
	{
		/*const*/ int type_shift_max = (int)Math.Log(MusicUtility.sixtyFourthsPerMeasure, 2.0);
		const int randomMeasuresMin = 1; // NOTE that random lengths are kept to even measure counts
		const int randomMeasuresMax = 2;
		int sixtyFourthsLeft = (int)MusicUtility.sixtyFourthsPerMeasure * UnityEngine.Random.Range(randomMeasuresMin, randomMeasuresMax + 1);

		// TODO: bring back chord_type? encourage runs of same-type notes?
		List<uint> lengths = new List<uint>();
		List<float> indices = new List<float>();
		int chordSizeMax = chords.m_progression.Max(progression => progression.Length); // TODO: use specific chords for different points in the progression?
		while (sixtyFourthsLeft > 0) {
			int[] allowedShifts = Enumerable.Range(0, Math.Min((int)Math.Log(sixtyFourthsLeft, 2.0f) + 1, noteLengthWeights.Length)).ToArray();
			uint lengthNew = (uint)(1 << Utility.RandomWeighted(allowedShifts, noteLengthWeights)); // 1, 2, 4, 8, 16, 32, or 64, limited by remaining length // TODO: favor placing longer notes at the end rather than the beginning?
			lengths.Add(lengthNew);
			indices.Add(UnityEngine.Random.Range(0, chordSizeMax)); // TODO: allow chord octave wrapping here as well as in harmonies?
			sixtyFourthsLeft -= (int)lengthNew;
			Assert.IsTrue(sixtyFourthsLeft >= 0);
		}

		return new MusicRhythm(lengths.ToArray(), indices.ToArray());
	}


	public MusicRhythm(uint[] lengthsSixtyFourths, float[] chordIndices)
	{
		m_lengthsSixtyFourths = lengthsSixtyFourths;
		m_chordIndices = chordIndices;
		Assert.AreEqual(lengthsSixtyFourths.Length, chordIndices.Length);
	}

	public List<MusicNote> Sequence(ChordProgression progression)
	{
		// TODO: more sophisticated compositions of rhythm & chords
		List<MusicNote> notes = new List<MusicNote>();
		foreach (float[] chord in progression.m_progression)
		{
			for (int i = 0, n = m_chordIndices.Length; i < n; ++i)
			{
				notes.Add(new MusicNote(new float[] { m_chordIndices[i] }, m_lengthsSixtyFourths[i], UnityEngine.Random.Range(0.5f, 1.0f), chord)); // TODO: coherent volume? pass whole progression and an index to each note?
			}
		}
		return notes;
	}

	public void Display(uint[] scaleSemitones, string elementId)
	{
		int noteCount = m_lengthsSixtyFourths.Length;
		uint[] times = Enumerable.Range(0, noteCount).Select(i => (uint)i).ToArray();
		uint[] keys = m_chordIndices.Select(index => MusicUtility.midiMiddleCKey + (uint)MusicUtility.TonesToSemitones(index, scaleSemitones)).ToArray();

		Assert.AreEqual(noteCount, times.Length);
		Assert.AreEqual(noteCount, keys.Length);

		MusicDisplay.Update(elementId, "Rhythm:", noteCount, times, keys, m_lengthsSixtyFourths, 0U); // NOTE that the bpm of 0 tells the update to use chord progression special formatting
	}
}
