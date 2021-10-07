using System;
using System.Linq;
using System.Collections.Generic;
using CSharpSynth.Midi;


public class MusicBlockSimple : MusicBlock
{
	private readonly MusicNote[] m_notes;


	public MusicBlockSimple(MusicNote[] notes)
	{
		m_notes = notes;
	}

	public override uint sixtyFourthsTotal()
	{
		return (uint)Enumerable.Sum(Array.ConvertAll(ListFromNotes((MusicNote note) => new List<uint> { note.length }).ToArray(), (uint x) => (int)x));
	}

	public override uint[] getKeys(uint rootKey, uint[] scaleSemitones)
	{
		return ListFromNotes((MusicNote note) => new List<uint>(note.midiKeys(rootKey, scaleSemitones))).ToArray();
	}

	public override uint[] getLengths()
	{
		return ListFromNotes((MusicNote note) => {
			List<uint> lengthList = new List<uint> { note.length };
			for (uint i = 1U, n = note.keyCount; i < n; ++i)
			{
				lengthList.Add(0U); // in order to more easily format chords for display w/ MusicXML's <chord/> convention of attaching to the previous note (see osmd_bridge.jslib), we put the length in only the first chord note
			}
			return lengthList;
		}).ToArray();
	}

	public override MidiEvent[] toMidiEvents(uint startSixtyFourths, uint rootKey, uint[] scaleSemitones, uint samplesPerSixtyFourth)
	{
		uint sixtyFourthsItr = startSixtyFourths;
		return ListFromNotes((MusicNote note) => {
			List<MidiEvent> newEvents = note.toMidiEvents(rootKey, scaleSemitones, sixtyFourthsItr, samplesPerSixtyFourth);
			sixtyFourthsItr += note.length;
			return newEvents;
		}).ToArray();
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
