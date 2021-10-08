using CSharpSynth.Midi;
using System;
using System.Collections.Generic;
using System.Linq;


public class MusicBlockRepeat : MusicBlock
{
	private readonly MusicBlock[] m_children;
	private readonly uint[] m_schedule;


	public MusicBlockRepeat(MusicBlock[] children, uint[] schedule)
	{
		m_children = children;
		m_schedule = schedule;
	}

	public override uint SixtyFourthsTotal()
	{
		return CombineViaSchedule(block => new List<uint> { block.SixtyFourthsTotal() }).Aggregate((a, b) => a + b);
	}

	public override List<NoteTimePair> GetNotes(uint timeOffset)
	{
		uint timeItr = timeOffset;
		return CombineViaSchedule(block => {
			List<NoteTimePair> notes = block.GetNotes(timeItr);
			timeItr += block.SixtyFourthsTotal();
			return notes;
		});
	}

	public override List<MidiEvent> ToMidiEvents(uint startSixtyFourths, uint rootKey, uint[] scaleSemitones, uint samplesPerSixtyFourth)
	{
		uint sixtyFourthsItr = startSixtyFourths;
		return CombineViaSchedule(block => {
			List<MidiEvent> list = block.ToMidiEvents(sixtyFourthsItr, rootKey, scaleSemitones, samplesPerSixtyFourth);
			sixtyFourthsItr += block.SixtyFourthsTotal();
			return list;
		});
	}


	private List<T> CombineViaSchedule<T>(Func<MusicBlock, List<T>> blockFunc)
	{
		List<T> list = new List<T>();
		foreach (uint index in m_schedule)
		{
			MusicBlock childBlock = m_children[index];
			list.AddRange(blockFunc(childBlock));
		}
		return list;
	}
}
