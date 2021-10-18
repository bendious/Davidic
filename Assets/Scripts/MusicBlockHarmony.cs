using CSharpSynth.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;


public class MusicBlockHarmony : MusicBlock
{
	private readonly MusicBlock[] m_children;


	public MusicBlockHarmony(MusicBlock melody, uint harmoniesMax)
	{
		Assert.IsTrue(harmoniesMax > 0U);
		List<MusicNote> harmonyNotes = new List<MusicNote>();
		List<NoteTimePair> melodyNotes = (UnityEngine.Random.value < 0.333f ? melody.MergeNotes() : (UnityEngine.Random.value < 0.5f ? melody.SplitNotes() : melody)).GetNotes(0U); // TODO: more strategic splitting/merging (both w/i same block)?
		foreach (NoteTimePair noteTime in melodyNotes)
		{
			MusicNote note = noteTime.m_note;
			int chordSize = (int)note.ChordCount;
			List<float> offsets = new List<float>();
			for (uint offsetIdx = 0, offsetCount = harmoniesMax; offsetIdx < offsetCount; ++offsetIdx)
			{
				offsets.Add(UnityEngine.Random.Range(1, chordSize) * (UnityEngine.Random.value < 0.5f ? -1 : 1)); // NOTE that MusicNote() handles preventing duplicates, but we still avoid offsets of 0 to prevent creating empty notes
			}
			harmonyNotes.Add(new MusicNote(note, offsets.ToArray(), true));
		}
		m_children = new MusicBlock[] { melody, new MusicBlockSimple(harmonyNotes.ToArray()) };
	}

	public override uint SixtyFourthsTotal()
	{
		return CombineFromChildren(block => block.SixtyFourthsTotal(), Math.Max, null);
	}

	public override List<NoteTimePair> GetNotes(uint timeOffset)
	{
		return CombineFromChildren(block => block.GetNotes(timeOffset), (a, b) => Enumerable.Concat(a, b).ToList(), list => list.Sort((a, b) => NoteSortCompare(a.m_time, b.m_time, a.m_note.LengthSixtyFourths, b.m_note.LengthSixtyFourths)));
	}

	public override List<MidiEvent> ToMidiEvents(uint startSixtyFourths, uint rootKey, MusicScale scale, uint samplesPerSixtyFourth)
	{
		return CombineFromChildren(block => block.ToMidiEvents(startSixtyFourths, rootKey, scale, samplesPerSixtyFourth), (a, b) => Enumerable.Concat(a, b).ToList(), list => list.Sort((a, b) => a.deltaTime.CompareTo(b.deltaTime)));
	}

	public override MusicBlock SplitNotes()
	{
		return new MusicBlockHarmony(m_children.Select(block => block.SplitNotes()).ToArray());
	}

	public override MusicBlock MergeNotes()
	{
		return new MusicBlockHarmony(m_children.Select(block => block.MergeNotes()).ToArray());
	}


	private MusicBlockHarmony(MusicBlock[] children)
	{
		m_children = children;
	}

	private T CombineFromChildren<T>(Func<MusicBlock, T> blockFunc, Func<T, T, T> combineFunc, Action<T> finalFunc)
	{
		T outVal = default;
		foreach (MusicBlock childBlock in m_children)
		{
			T newVal = blockFunc(childBlock);
			outVal = (outVal == null) ? newVal : combineFunc(outVal, newVal);
		}
		finalFunc?.Invoke(outVal);
		return outVal;
	}

	private int NoteSortCompare(uint timeA, uint timeB, uint lengthA, uint lengthB)
	{
		int timeCompare = timeA.CompareTo(timeB);
		if (timeCompare != 0)
		{
			return timeCompare;
		}
		return lengthB.CompareTo(lengthA); // NOTE the reversed order since we want LONGER notes sorted earlier
	}
}
