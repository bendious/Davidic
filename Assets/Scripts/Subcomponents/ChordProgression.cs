using System.Linq;
using UnityEngine.Assertions;


public class ChordProgression
{
	public readonly float[][] m_progression;


	public ChordProgression(float[][] progression) => m_progression = progression;

	public void Display(MusicScale scale, string elementId)
	{
		const uint timeInc = MusicUtility.sixtyFourthsPerBeat;
		uint chordItr = 0U;
		uint[] times = m_progression.SelectMany(chord => Enumerable.Repeat(timeInc * chordItr++, chord.Length)).ToArray();
		uint[] keys = m_progression.SelectMany(chord => chord.Select(note => MusicUtility.midiMiddleCKey + (uint)MusicUtility.TonesToSemitones(note, scale))).ToArray();

		int noteCount = keys.Length;
		MusicDisplay.Update(elementId, "Chord\\nProgression:", null, scale, times, keys, Enumerable.Repeat(timeInc, noteCount).ToArray(), Enumerable.Repeat(0U, noteCount).ToArray(), 0U); // NOTE that the bpm of 0 tells the update to use chord progression special formatting
	}
}