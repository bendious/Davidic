using CSharpSynth.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;


public class MusicBlockHarmony : MusicBlock
{
	private readonly MusicBlock[] m_children;


	public MusicBlockHarmony(MusicBlock melody, uint harmoniesMax, uint harmonyChannel, float[] noteLengthWeights)
	{
		bool sameChannel = melody.GetChannels().Contains(harmonyChannel);
		Assert.IsTrue(harmoniesMax > 0U || !sameChannel);
		List<MusicNote> harmonyNotes = new List<MusicNote>();
		List<ValueTuple<MusicNote, uint>> melodyNotes = (UnityEngine.Random.value < 0.333f ? melody.MergeNotes(noteLengthWeights) : (UnityEngine.Random.value < 0.5f ? melody.SplitNotes(noteLengthWeights) : melody)).NotesOrdered(0U); // TODO: more strategic splitting/merging (both w/i same block)?
		uint endTimePrev = 0U;
		foreach (ValueTuple<MusicNote, uint> noteTime in melodyNotes)
		{
			if (noteTime.Item2 < endTimePrev) // TODO: combine separate-block chords/overlaps?
			{
				continue;
			}
			MusicNote note = noteTime.Item1;
			int chordSize = (int)note.ChordCount;
			List<float> offsets = new List<float>();
			for (uint offsetIdx = 0, offsetCount = harmoniesMax + (sameChannel ? 0U : 1U); offsetIdx < offsetCount; ++offsetIdx)
			{
				offsets.Add(UnityEngine.Random.Range(1, chordSize) * (UnityEngine.Random.value < 0.5f ? -1 : 1)); // NOTE that MusicNote() handles preventing duplicates, but we still avoid offsets of 0 to prevent creating empty notes
			}
			harmonyNotes.Add(new MusicNote(note, offsets.ToArray(), sameChannel, harmonyChannel));
			endTimePrev = noteTime.Item2 + noteTime.Item1.SixtyFourthsTotal();
		}
		m_children = new MusicBlock[] { melody, new MusicBlockSimple(harmonyNotes.ToArray()) };
	}

	public override uint SixtyFourthsTotal() => CombineFromChildren(block => block.SixtyFourthsTotal(), Math.Max, null);

	public override MusicNote AsNote(uint lengthSixtyFourths) => m_children.First().AsNote(lengthSixtyFourths);

	public override List<ValueTuple<MusicNote, uint>> NotesOrdered(uint timeOffset) => CombineFromChildren(block => block.NotesOrdered(timeOffset), (a, b) => Enumerable.Concat(a, b).ToList(), list => list.Sort((a, b) => NoteSortCompare(a.Item2, b.Item2, a.Item1.LengthSixtyFourths, b.Item1.LengthSixtyFourths)));

	public override List<uint> GetChannels() => CombineFromChildren(block => block.GetChannels(), (a, b) => a.Union(b).ToList(), null);

	public override List<MidiEvent> ToMidiEvents(uint startSixtyFourths, uint rootKey, MusicScale scale, uint samplesPerSixtyFourth) => CombineFromChildren(block => block.ToMidiEvents(startSixtyFourths, rootKey, scale, samplesPerSixtyFourth), (a, b) => Enumerable.Concat(a, b).ToList(), list => list.Sort((a, b) => a.deltaTime.CompareTo(b.deltaTime)));

	public override MusicBlock SplitNotes(float[] noteLengthWeights) => new MusicBlockHarmony(m_children.Select(block => block.SplitNotes(noteLengthWeights)).ToArray());

	public override MusicBlock MergeNotes(float[] noteLengthWeights) => new MusicBlockHarmony(m_children.Select(block => block.MergeNotes(noteLengthWeights)).ToArray());


	private MusicBlockHarmony(MusicBlock[] children) => m_children = children;

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
