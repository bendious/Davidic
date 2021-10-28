using CSharpSynth.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;


public class MusicBlockRepeat : MusicBlock
{
	private readonly MusicBlock[] m_children;
	private readonly uint[] m_schedule;


	public MusicBlockRepeat(MusicBlock[] children, uint[] schedule)
	{
		Assert.AreNotEqual(children.Length, 0);
		Assert.IsTrue(schedule.Length >= children.Length);
		m_children = children;
		m_schedule = schedule;
	}

	public override uint SixtyFourthsTotal() => CombineViaSchedule(block => new List<uint> { block.SixtyFourthsTotal() }).Aggregate((a, b) => a + b);

	public override MusicNote AsNote(uint lengthSixtyFourths) => m_children[m_schedule.First()].AsNote(lengthSixtyFourths);

	public override List<ValueTuple<MusicNote, uint>> NotesOrdered(uint timeOffset)
	{
		uint timeItr = timeOffset;
		return CombineViaSchedule(block => {
			List<ValueTuple<MusicNote, uint>> notes = block.NotesOrdered(timeItr);
			timeItr += block.SixtyFourthsTotal();
			return notes;
		});
	}

	public override List<uint> GetChannels() => CombineViaSchedule(block => block.GetChannels()).Distinct().ToList();

	public override List<MidiEvent> ToMidiEvents(uint startSixtyFourths, uint rootKey, MusicScale scale, uint samplesPerSixtyFourth)
	{
		uint sixtyFourthsItr = startSixtyFourths;
		return CombineViaSchedule(block => {
			List<MidiEvent> list = block.ToMidiEvents(sixtyFourthsItr, rootKey, scale, samplesPerSixtyFourth);
			sixtyFourthsItr += block.SixtyFourthsTotal();
			return list;
		});
	}

	public override MusicBlock SplitNotes(float[] noteLengthWeights) => new MusicBlockRepeat(m_children.Select(block => block.SplitNotes(noteLengthWeights)).ToArray(), m_schedule);

	public override MusicBlock MergeNotes(float[] noteLengthWeights) => new MusicBlockRepeat(m_children.Select(block => block.MergeNotes(noteLengthWeights)).ToArray(), m_schedule);


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
