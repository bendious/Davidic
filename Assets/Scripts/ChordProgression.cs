using System.Linq;
using UnityEngine.Assertions;


public class ChordProgression
{
	public readonly float[][] m_progression;


	public ChordProgression(float[][] progression)
	{
		m_progression = progression;
	}

	public void Display(uint[] scaleSemitones, string elementId)
	{
		uint timeItr = 0U;
		uint[] times = m_progression.SelectMany(chord => Enumerable.Repeat(timeItr++, chord.Length)).ToArray();
		uint[] keys = m_progression.SelectMany(chord => chord.Select(note => MusicUtility.midiMiddleCKey + (uint)MusicUtility.TonesToSemitones(note, scaleSemitones))).ToArray();

		int noteCount = keys.Length;
		Assert.AreEqual(times.Length, noteCount);

		MusicDisplay.Update(elementId, "Chord\\nProgression:", noteCount, times, keys, Enumerable.Repeat(MusicUtility.sixtyFourthsPerBeat, noteCount).ToArray(), 0U); // NOTE that the bpm of 0 tells the update to use chord progression special formatting
	}
}