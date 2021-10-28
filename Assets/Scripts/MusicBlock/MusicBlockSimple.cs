using CSharpSynth.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;


public class MusicBlockSimple : MusicBlock
{
	private readonly MusicBlock[] m_blocks;


	public MusicBlockSimple(MusicBlock[] blocks)
	{
		Assert.AreNotEqual(blocks.Length, 0);
		m_blocks = blocks;
	}

	public override uint SixtyFourthsTotal() => ListFromBlocks(block => new List<uint> { block.SixtyFourthsTotal() }).Aggregate((a, b) => a + b);

	public override MusicNote AsNote(uint lengthSixtyFourths) => m_blocks.First().AsNote(lengthSixtyFourths);

	public override List<ValueTuple<MusicNote, uint>> NotesOrdered(uint timeOffset)
	{
		uint timeItr = timeOffset;
		return ListFromBlocks(block => {
			List<ValueTuple<MusicNote, uint>> list = block.NotesOrdered(timeItr);
			timeItr += block.SixtyFourthsTotal();
			return list;
		});
	}

	public override List<uint> GetChannels() => ListFromBlocks(block => block.GetChannels()).Distinct().ToList();

	public override List<MidiEvent> ToMidiEvents(uint startSixtyFourths, uint rootKey, MusicScale scale, uint samplesPerSixtyFourth)
	{
		uint sixtyFourthsItr = startSixtyFourths;
		return ListFromBlocks(block => {
			List<MidiEvent> newEvents = block.ToMidiEvents(sixtyFourthsItr, rootKey, scale, samplesPerSixtyFourth);
			sixtyFourthsItr += block.SixtyFourthsTotal();
			return newEvents;
		});
	}

	public override MusicBlock SplitNotes(float[] noteLengthWeights) => new MusicBlockSimple(m_blocks.Select(block => block.SplitNotes(noteLengthWeights)).ToArray());

	public override MusicBlock MergeNotes(float[] noteLengthWeights)
	{
		Assert.AreEqual(noteLengthWeights.Length, MusicUtility.tonesPerOctave);

		List<MusicBlock> manualBlocks = new List<MusicBlock>();
		for (int i = 0, n = m_blocks.Length; i < n; ++i)
		{
			MusicBlock block_cur = m_blocks[i];
			if (UnityEngine.Random.value < 0.5f/*TODO?*/)
			{
				manualBlocks.Add(block_cur);
				continue;
			}
			uint sixtyFourthsMerged = 0U;
			int j, m;
			int doublingCountMax = Mathf.RoundToInt(Mathf.Log(MusicUtility.sixtyFourthsPerMeasure / block_cur.SixtyFourthsTotal(), 2.0f));
			int mergeCountMax = doublingCountMax <= 0 ? 1 : 1 << Utility.RandomWeighted(Enumerable.Range(1, doublingCountMax).ToArray(), new ArraySegment<float>(noteLengthWeights, noteLengthWeights.Length - doublingCountMax, doublingCountMax).ToArray()); // TODO: don't assume all notes will be the same length?
			float[] firstChord = block_cur.AsNote(uint.MaxValue).m_chord;
			for (j = i, m = Math.Min(i + mergeCountMax, n); j < m && sixtyFourthsMerged < MusicUtility.sixtyFourthsPerMeasure && firstChord == m_blocks[j].AsNote(uint.MaxValue).m_chord; ++j)
			{
				sixtyFourthsMerged += m_blocks[j].SixtyFourthsTotal();
			}
			manualBlocks.Add(block_cur.AsNote(sixtyFourthsMerged));
			i = j - 1;
		}
		return new MusicBlockSimple(manualBlocks.ToArray());
	}


	private List<T> ListFromBlocks<T>(Func<MusicBlock, List<T>> blockFunc)
	{
		List<T> list = new List<T>();
		foreach (MusicBlock block in m_blocks)
		{
			list.AddRange(blockFunc(block));
		}
		return list;
	}
}
