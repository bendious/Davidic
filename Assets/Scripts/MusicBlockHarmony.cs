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
		foreach (NoteTimePair noteTime in melody.GetNotes(0U))
		{
			// TODO: split/merge melody notes
			MusicNote note = noteTime.m_note;
			int chordSize = (int)note.ChordCount;
			List<float> offsets = new List<float>();
			for (uint offsetIdx = 0, offsetCount = harmoniesMax; offsetIdx < offsetCount; ++offsetIdx)
			{
				offsets.Add(UnityEngine.Random.Range(-chordSize, chordSize)); // NOTE that MusicNote() handles preventing duplicates
			}
			harmonyNotes.Add(new MusicNote(note, offsets.ToArray()));
		}
		m_children = new MusicBlock[] { melody, new MusicBlockSimple(harmonyNotes.ToArray()) };
	}

	public override uint SixtyFourthsTotal()
	{
		return CombineFromChildren(block => block.SixtyFourthsTotal(), Math.Max, null);
	}

	public override List<NoteTimePair> GetNotes(uint timeOffset)
	{
		return CombineFromChildren(block => block.GetNotes(timeOffset), (a, b) => Enumerable.Concat(a, b).ToList(), list => list.Sort((a, b) => a.m_time.CompareTo(b.m_time)));
	}

	public override List<MidiEvent> ToMidiEvents(uint startSixtyFourths, uint rootKey, MusicScale scale, uint samplesPerSixtyFourth)
	{
		return CombineFromChildren(block => block.ToMidiEvents(startSixtyFourths, rootKey, scale, samplesPerSixtyFourth), (a, b) => Enumerable.Concat(a, b).ToList(), list => list.Sort((a, b) => a.deltaTime.CompareTo(b.deltaTime)));
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
}
