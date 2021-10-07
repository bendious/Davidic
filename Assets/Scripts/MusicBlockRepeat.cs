using System;
using System.Linq;
using System.Collections.Generic;
using CSharpSynth.Midi;


public class MusicBlockRepeat : MusicBlock
{
	private readonly MusicBlock[] m_children;
	private readonly uint[] m_schedule;


	public MusicBlockRepeat(MusicBlock[] children, uint[] schedule)
	{
		m_children = children;
		m_schedule = schedule;
	}

	public override uint sixtyFourthsTotal()
	{
		return (uint)Enumerable.Sum(Array.ConvertAll(CombineViaSchedule((MusicBlock block) => new List<uint> { block.sixtyFourthsTotal() }), (uint unsigned) => (int)unsigned));
	}

	public override uint[] getKeys(uint rootKey, uint[] scaleSemitones)
	{
		return CombineViaSchedule((MusicBlock block) => new List<uint>(block.getKeys(rootKey, scaleSemitones)));
	}

	public override uint[] getLengths()
	{
		return CombineViaSchedule((MusicBlock block) => new List<uint>(block.getLengths()));
	}

	public override MidiEvent[] toMidiEvents(uint startSixtyFourths, uint rootKey, uint[] scaleSemitones, uint samplesPerSixtyFourth)
	{
		uint sixtyFourthsItr = startSixtyFourths;
		return CombineViaSchedule((MusicBlock block) => {
			List<MidiEvent> list = new List<MidiEvent>(block.toMidiEvents(sixtyFourthsItr, rootKey, scaleSemitones, samplesPerSixtyFourth));
			sixtyFourthsItr += block.sixtyFourthsTotal();
			return list;
		});
	}


	private T[] CombineViaSchedule<T>(Func<MusicBlock, List<T>> blockFunc)
	{
		List<T> list = new List<T>(); // TODO: less array/list interconversion?
		foreach (uint index in m_schedule)
		{
			MusicBlock childBlock = m_children[index];
			list.AddRange(blockFunc(childBlock));
		}
		return list.ToArray();
	}
}
