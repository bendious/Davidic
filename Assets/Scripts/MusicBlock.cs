using CSharpSynth.Midi;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;


public abstract class MusicBlock
{
	public struct NoteTimePair
	{
		public MusicNote m_note;
		public uint m_time;
	}

	public abstract uint SixtyFourthsTotal();

	public abstract List<NoteTimePair> GetNotes(uint timeOffset);

	public abstract List<MidiEvent> ToMidiEvents(uint startSixtyFourths, uint rootKey, MusicScale scale, uint samplesPerSixtyFourth);

	public void Display(uint rootKey, MusicScale scale, string element_id, uint bpm)
	{
		List<NoteTimePair> noteTimeSequence = GetNotes(0U);
		uint[] timeSequence = noteTimeSequence.SelectMany(pair => Enumerable.Repeat(pair.m_time, (int)pair.m_note.KeyCount)).ToArray();
		uint[] keySequence = noteTimeSequence.SelectMany(pair => pair.m_note.MidiKeys(rootKey, scale)).ToArray();
		uint[] lengthSequence = noteTimeSequence.SelectMany(pair => Enumerable.Repeat(pair.m_note.LengthSixtyFourths, (int)pair.m_note.KeyCount)).ToArray();

		int noteCount = keySequence.Length;
		Assert.AreEqual(timeSequence.Length, noteCount);
		Assert.AreEqual(noteCount, lengthSequence.Length);

		MusicDisplay.Update(element_id, "", scale.m_fifths, scale.m_mode, noteCount, timeSequence, keySequence, lengthSequence, bpm);
	}
}
