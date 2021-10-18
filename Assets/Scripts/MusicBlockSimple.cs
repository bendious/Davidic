using CSharpSynth.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;


public class MusicBlockSimple : MusicBlock
{
	private readonly MusicBlock[] m_blocks;


	public MusicBlockSimple(MusicBlock[] blocks)
	{
		Assert.AreNotEqual(blocks.Length, 0);
		m_blocks = blocks;
	}

	public override uint SixtyFourthsTotal()
	{
		return ListFromBlocks(block => new List<uint> { block.SixtyFourthsTotal() }).Aggregate((a, b) => a + b);
	}

	public override List<NoteTimePair> GetNotes(uint timeOffset)
	{
		uint timeItr = timeOffset;
		return ListFromBlocks(block => {
			List<NoteTimePair> list = block.GetNotes(timeItr);
			timeItr += block.SixtyFourthsTotal();
			return list;
		});
	}

	public override List<MidiEvent> ToMidiEvents(uint startSixtyFourths, uint rootKey, MusicScale scale, uint samplesPerSixtyFourth)
	{
		uint sixtyFourthsItr = startSixtyFourths;
		return ListFromBlocks(block => {
			List<MidiEvent> newEvents = block.ToMidiEvents(sixtyFourthsItr, rootKey, scale, samplesPerSixtyFourth);
			sixtyFourthsItr += block.SixtyFourthsTotal();
			return newEvents;
		});
	}

	public override MusicBlock SplitNotes()
	{
		return new MusicBlockSimple(m_blocks.Select(block => block.SplitNotes()).ToArray());
	}

	public override MusicBlock MergeNotes()
	{
		if (UnityEngine.Random.value < 0.5f/*TODO?*/)
		{
			return new MusicBlockSimple(m_blocks.Select(block => block.MergeNotes()).ToArray());
		}

		List<MusicBlock> manualBlocks = new List<MusicBlock>();
		for (int i = 0, n = m_blocks.Length; i < n; ++i)
		{
			uint sixtyFourthsMerged = 0U;
			int j, m;
			for (j = i, m = UnityEngine.Random.Range(i + 1, Math.Min(i + 3, n)); j < m && sixtyFourthsMerged < MusicUtility.sixtyFourthsPerMeasure && m_blocks[i].SixtyFourthsTotal() == m_blocks[j].SixtyFourthsTotal(); ++j) // TODO: restrict merge counts to powers of two? allow merging different length notes/blocks?
			{
				sixtyFourthsMerged += m_blocks[j].SixtyFourthsTotal();
			}
			MusicNote noteMerged = new MusicNote(m_blocks[i].GetNotes(0U).First().m_note, new float[] { 0.0f }, false) {
				LengthSixtyFourths = sixtyFourthsMerged,
			}; // TODO: better way of merging blocks?
			manualBlocks.Add(noteMerged);
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
