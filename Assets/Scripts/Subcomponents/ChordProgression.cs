using System.Linq;
using UnityEngine.Assertions;


public class ChordProgression
{
	public readonly float[][] m_progression;


	public ChordProgression(float[][] progression) => m_progression = progression;

	public void Display(string elementId)
	{
		const uint timeInc = MusicUtility.sixtyFourthsPerBeat;
		MusicNote[] notes = m_progression.Select(chord => new MusicNote(Enumerable.Range(0, chord.Length).Select(i => (float)i).ToArray(), timeInc, 1.0f, chord, 0U)).ToArray();
		uint chordItr = 0U;
		uint[] times = m_progression.Select(chord => timeInc * chordItr++).ToArray();

		MusicDisplay.Update(elementId, "Chord Progression:", null, MusicUtility.majorScale, MusicUtility.midiMiddleCKey, 0U, notes, times); // NOTE that the bpm of 0 tells the update to use chord progression special formatting
	}
}
