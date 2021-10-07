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
		return CombineViaSchedule((MusicBlock block) => new List<uint> { block.SixtyFourthsTotal() }).Aggregate((uint a, uint b) => a + b);
	}

	public override List<uint> GetKeys(uint rootKey, uint[] scaleSemitones)
	{
		return CombineViaSchedule((MusicBlock block) => new List<uint>(block.GetKeys(rootKey, scaleSemitones)));
	}

	public override List<uint> GetLengths()
	{
		return CombineViaSchedule((MusicBlock block) => new List<uint>(block.GetLengths()));
	}

	public override List<MidiEvent> ToMidiEvents(uint startSixtyFourths, uint rootKey, uint[] scaleSemitones, uint samplesPerSixtyFourth)
	{
		uint sixtyFourthsItr = startSixtyFourths;
		return CombineViaSchedule((MusicBlock block) => {
			List<MidiEvent> list = new List<MidiEvent>(block.ToMidiEvents(sixtyFourthsItr, rootKey, scaleSemitones, samplesPerSixtyFourth));
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
