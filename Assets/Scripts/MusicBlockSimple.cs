using CSharpSynth.Midi;
using System;
using System.Collections.Generic;
using System.Linq;


public class MusicBlockSimple : MusicBlock
{
	private readonly MusicNote[] m_notes;


	public MusicBlockSimple(MusicNote[] notes)
	{
		m_notes = notes;
	}

	public override uint SixtyFourthsTotal()
	{
		return ListFromNotes(note => new List<uint> { note.LengthSixtyFourths }).Aggregate((a, b) => a + b);
	}

	public override List<NoteTimePair> GetNotes(uint timeOffset)
	{
		List<NoteTimePair> list = new List<NoteTimePair>();
		uint timeItr = timeOffset;
		foreach (MusicNote note in m_notes)
		{
			list.Add(new NoteTimePair { m_note = note, m_time = timeItr });
			timeItr += note.LengthSixtyFourths;
		}
		return list;
	}

	public override List<MidiEvent> ToMidiEvents(uint startSixtyFourths, uint rootKey, MusicScale scale, uint samplesPerSixtyFourth)
	{
		uint sixtyFourthsItr = startSixtyFourths;
		return ListFromNotes(note => {
			List<MidiEvent> newEvents = note.ToMidiEvents(rootKey, scale, sixtyFourthsItr, samplesPerSixtyFourth);
			sixtyFourthsItr += note.LengthSixtyFourths;
			return newEvents;
		});
	}


	private List<T> ListFromNotes<T>(Func<MusicNote, List<T>> noteFunc)
	{
		List<T> list = new List<T>();
		foreach (MusicNote note in m_notes)
		{
			list.AddRange(noteFunc(note));
		}
		return list;
	}
}
