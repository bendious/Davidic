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
		return ListFromNotes((MusicNote note) => new List<uint> { note.LengthSixtyFourths }).Aggregate((uint a, uint b) => a + b);
	}

	public override List<uint> GetKeys(uint rootKey, uint[] scaleSemitones)
	{
		return ListFromNotes((MusicNote note) => note.MidiKeys(rootKey, scaleSemitones));
	}

	public override List<uint> GetLengths()
	{
		return ListFromNotes((MusicNote note) => {
			List<uint> lengthList = new List<uint> { note.LengthSixtyFourths };
			for (uint i = 1U, n = note.KeyCount; i < n; ++i)
			{
				lengthList.Add(0U); // in order to more easily format chords for display w/ MusicXML's <chord/> convention of attaching to the previous note (see osmd_bridge.jslib), we put the length in only the first chord note
			}
			return lengthList;
		});
	}

	public override List<MidiEvent> ToMidiEvents(uint startSixtyFourths, uint rootKey, uint[] scaleSemitones, uint samplesPerSixtyFourth)
	{
		uint sixtyFourthsItr = startSixtyFourths;
		return ListFromNotes((MusicNote note) => {
			List<MidiEvent> newEvents = note.ToMidiEvents(rootKey, scaleSemitones, sixtyFourthsItr, samplesPerSixtyFourth);
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
