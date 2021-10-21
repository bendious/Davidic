using System;
using UnityEngine.Assertions;


public class MusicScale
{
	public readonly uint[] m_semitones; // the number of semitones from the root note to each subsequent tone; e.g. [0,2,4,5,7,9,11] for major scales
	public readonly int m_fifths; // the number of sharps/flats in the scale (negative for flats, positive for sharps), i.e the scale's position on the Circle of Fifths; see https://www.w3.org/2021/06/musicxml40/musicxml-reference/elements/fifths/
	public readonly string m_mode; // "major"/"minor"/etc; see https://www.w3.org/2021/06/musicxml40/musicxml-reference/elements/mode/


	public MusicScale(uint[] semitones, int fifths, string mode)
	{
		Assert.AreEqual(semitones.Length, MusicUtility.tonesPerOctave);
		Assert.AreEqual(semitones[0], 0U);
		m_semitones = semitones;
		Assert.IsTrue(Math.Abs(fifths) < MusicUtility.tonesPerOctave);
		m_fifths = fifths;
		m_mode = mode;
	}
}
