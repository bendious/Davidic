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
		uint[] timeSequence = noteTimeSequence.SelectMany(pair => Enumerable.Repeat(pair.Item2, (int)pair.Item1.KeyCount)).ToArray();
		uint[] keySequence = noteTimeSequence.SelectMany(pair => pair.Item1.MidiKeys(rootKey, scale)).ToArray();
		uint[] lengthSequence = noteTimeSequence.SelectMany(pair => Enumerable.Repeat(pair.Item1.LengthSixtyFourths, (int)pair.Item1.KeyCount)).ToArray();
		uint[] channelSequence = noteTimeSequence.SelectMany(pair => Enumerable.Repeat(pair.Item1.m_channel, (int)pair.Item1.KeyCount)).ToArray();

		MusicDisplay.Update(elementId, "", instrumentNames, scale, timeSequence, keySequence, lengthSequence, channelSequence, bpm);
	}
}
