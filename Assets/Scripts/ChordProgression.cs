using System.Linq;
using UnityEngine.Assertions;


public class ChordProgression
{
	public readonly float[][] m_progression;


	public ChordProgression(float[][] progression)
	{
		m_progression = progression;
	}

	public void Display(MusicScale scale, string elementId)
	{
		const uint timeInc = MusicUtility.sixtyFourthsPerBeat;
		uint chordItr = 0U;
		uint[] times = m_progression.SelectMany(chord => Enumerable.Repeat(timeInc * chordItr++, chord.Length)).ToArray();
		uint[] keys = m_progression.SelectMany(chord => chord.Select(note => MusicUtility.midiMiddleCKey + (uint)MusicUtility.TonesToSemitones(note, scale))).ToArray();

		int noteCount = keys.Length;
		Assert.AreEqual(times.Length, noteCount);

		MusicDisplay.Update(elementId, "Chord\\nProgression:", "", scale.m_fifths, scale.m_mode, noteCount, times, keys, Enumerable.Repeat(timeInc, noteCount).ToArray(), 0U); // NOTE that the bpm of 0 tells the update to use chord progression special formatting
	}
}
