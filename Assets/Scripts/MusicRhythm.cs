using System.Linq;
using UnityEngine.Assertions;


public class MusicRhythm
{
	private readonly uint[] m_lengthsSixtyFourths;
	private readonly float[] m_chordIndices;


	public MusicRhythm(uint[] lengthsSixtyFourths, float[] chordIndices)
	{
		m_lengthsSixtyFourths = lengthsSixtyFourths;
		m_chordIndices = chordIndices;
		Assert.AreEqual(lengthsSixtyFourths.Length, chordIndices.Length);
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
