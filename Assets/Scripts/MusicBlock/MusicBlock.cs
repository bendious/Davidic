using CSharpSynth.Midi;
using System.Collections.Generic;
using System;
using System.Linq;


public abstract class MusicBlock
{
	public abstract uint SixtyFourthsTotal();

	public abstract MusicNote AsNote(uint lengthSixtyFourths);
	public abstract List<ValueTuple<MusicNote, uint>> NotesOrdered(uint timeOffset);
	public abstract List<uint> GetChannels();

	public abstract List<MidiEvent> ToMidiEvents(uint startSixtyFourths, uint rootKey, MusicScale scale, uint samplesPerSixtyFourth);

	public abstract MusicBlock SplitNotes(float[] noteLengthWeights);
	public abstract MusicBlock MergeNotes(float[] noteLengthWeights);

	public void Display(uint rootKey, MusicScale scale, string elementId, string[] instrumentNames, uint bpm)
	{
		List<ValueTuple<MusicNote, uint>> noteTimeSequence = NotesOrdered(0U);
		MusicNote[] notes = noteTimeSequence.Select(pair => pair.Item1).ToArray();
		uint[] times = noteTimeSequence.Select(pair => pair.Item2).ToArray();

		MusicDisplay.Update(elementId, "", instrumentNames, scale, rootKey, bpm, notes, times);
	}

	public string Export(string filepath, uint rootKey, MusicScale scale, string[] instrumentNames, uint bpm)
	{
		List<ValueTuple<MusicNote, uint>> noteTimeSequence = NotesOrdered(0U);
		MusicNote[] notes = noteTimeSequence.Select(pair => pair.Item1).ToArray();
		uint[] times = noteTimeSequence.Select(pair => pair.Item2).ToArray();

		return MusicDisplay.Export(filepath, "", instrumentNames, scale, rootKey, bpm, notes, times);
	}
}
