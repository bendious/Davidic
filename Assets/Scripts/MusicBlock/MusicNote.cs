using CSharpSynth.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;


public class MusicNote : MusicBlock
{
	private readonly float[] m_chordIndices;
	public readonly float[] m_chord;
	public readonly uint m_channel;


	public MusicNote(float[] chordIndices, uint lengthSixtyFourths, float volumePct, float[] chord, uint channel)
	{
		// TODO: check for / remove chord/index duplicates?
		Assert.AreNotEqual(chordIndices.Length, 0);
		Assert.AreNotEqual(chord.Length, 0);
		m_chordIndices = chordIndices;
		LengthSixtyFourths = lengthSixtyFourths;
		VolumePct = volumePct;
		m_chord = chord;
		m_channel = channel;
	}

	public MusicNote(MusicNote noteOrig, float[] indexOffsets, bool checkOrigDuplicates, uint channelOverride)
	{
		List<float> indicesCombined = new List<float>();
		foreach (float index in noteOrig.m_chordIndices)
		{
			foreach (float offset in indexOffsets)
			{
				float indexNew = index + offset;
				if (!(checkOrigDuplicates && noteOrig.m_chordIndices.Contains(indexNew)))
				{
					indicesCombined.Add(indexNew);
				}
			}
		}
		m_chordIndices = indicesCombined.Distinct().ToArray();
		Assert.AreNotEqual(m_chordIndices.Length, 0);
		LengthSixtyFourths = noteOrig.LengthSixtyFourths;
		VolumePct = noteOrig.VolumePct;
		m_chord = noteOrig.m_chord;
		m_channel = (channelOverride == uint.MaxValue) ? noteOrig.m_channel : channelOverride;
	}

	public override uint SixtyFourthsTotal() => LengthSixtyFourths;

	public override MusicNote AsNote(uint lengthSixtyFourths)
	{
		if (lengthSixtyFourths == LengthSixtyFourths || lengthSixtyFourths == uint.MaxValue)
		{
			return this;
		}
		MusicNote copy = (MusicNote)MemberwiseClone();
		copy.LengthSixtyFourths = lengthSixtyFourths;
		return copy;
	}

	public override List<ValueTuple<MusicNote, uint>> NotesOrdered(uint timeOffset) => new List<ValueTuple<MusicNote, uint>> { new ValueTuple<MusicNote, uint>(this, timeOffset) };

	public override List<uint> GetChannels() => new List<uint> { m_channel };

	public override List<MidiEvent> ToMidiEvents(uint startSixtyFourths, uint rootKey, MusicScale scale, uint samplesPerSixtyFourth)
	{
		List<MidiEvent> events = new List<MidiEvent>();

		uint startSample = startSixtyFourths * samplesPerSixtyFourth;
		foreach (float index in m_chordIndices)
		{
			uint keyCur = ChordIndexToMidiKey(index, rootKey, scale);

			MidiEvent eventOn = new MidiEvent
			{
				deltaTime = startSample,
				midiChannelEvent = MidiHelper.MidiChannelEvent.Note_On,
				parameter1 = (byte)keyCur,
				parameter2 = (byte)(VolumePct * 100), // velocity
				channel = (byte)m_channel,
			};
			events.Add(eventOn);
		}

		uint endSample = (startSixtyFourths + LengthSixtyFourths) * samplesPerSixtyFourth - 1U; // -1 to ensure stopping before starting again for adjacent same-key notes // TODO: leave noticable gaps between notes? staccato/legato/etc?
		foreach (float index in m_chordIndices)
		{
			MidiEvent eventOff = new MidiEvent
			{
				deltaTime = endSample,
				midiChannelEvent = MidiHelper.MidiChannelEvent.Note_Off,
				parameter1 = (byte)ChordIndexToMidiKey(index, rootKey, scale),
				channel = (byte)m_channel,
			};
			events.Add(eventOff);
		}

		return events;
	}

	public override MusicBlock SplitNotes(float[] noteLengthWeights)
	{
		Assert.AreEqual(noteLengthWeights.Length, MusicUtility.tonesPerOctave);
		if (LengthSixtyFourths < MusicUtility.sixtyFourthsPerBeat || UnityEngine.Random.value < 0.5f) // TODO: better conditions?
		{
			return this;
		}
		int halveCountMax = Mathf.RoundToInt(Mathf.Log(LengthSixtyFourths, 2.0f));
		Assert.IsTrue(halveCountMax > 0);
		int splitCount = 1 << Utility.RandomWeighted(Enumerable.Range(1, halveCountMax).ToArray(), new ArraySegment<float>(noteLengthWeights, 0, halveCountMax).Reverse().ToArray());
		uint splitLength = LengthSixtyFourths / (uint)splitCount;
		Assert.AreNotEqual(splitLength, 0U);
		MusicNote splitNote = new MusicNote(m_chordIndices, splitLength, VolumePct, m_chord, m_channel);
		List<MusicNote> splitNotes = Enumerable.Repeat(splitNote, splitCount).ToList();
		for (int i = 0, n = splitNotes.Count; i < n; ++i)
		{
			splitNotes[i] = new MusicNote(splitNotes[i], new float[] { UnityEngine.Random.Range(0, m_chordIndices.Length) }, false, uint.MaxValue);
		}
		return new MusicBlockSimple(splitNotes.ToArray());
	}

	public override MusicBlock MergeNotes(float[] noteLengthWeights) => this;

	public bool ContainsRoot()
	{
		foreach (float index in m_chordIndices)
		{
			if (Mathf.Approximately(Utility.Modulo(ChordIndexToToneOffset(index), MusicUtility.tonesPerOctave), 0.0f))
			{
				return true;
			}
		}
		return false;
	}

	public List<uint> MidiKeys(uint rootKey, MusicScale scale) => m_chordIndices.ToList().ConvertAll(index => ChordIndexToMidiKey(index, rootKey, scale));

	public uint KeyCount
	{
		get => (uint)m_chordIndices.Length;
	}

	public uint ChordCount
	{
		get => (uint)m_chord.Length;
	}

	public uint LengthSixtyFourths { get; set; }
	public float VolumePct { get; }


	private float ChordIndexToToneOffset(float index)
	{
		float indexFractAbs = Mathf.Abs(Utility.Fract(index));
		int chordSizeI = (int)ChordCount;
		float chordSizeF = chordSizeI;
		Assert.IsTrue(chordSizeF > 0.0f);
		float indexMod = Utility.Modulo(index, chordSizeF);
		Assert.IsTrue(indexMod < chordSizeF);

		return (indexFractAbs <= 0.333f || indexFractAbs >= 0.667f) ? m_chord[(uint)Mathf.Round(indexMod)] : (m_chord[Mathf.FloorToInt(indexMod)] + m_chord[Utility.Modulo(Mathf.CeilToInt(indexMod), chordSizeI)]) * 0.5f; // TODO: better way of picking off-chord notes?
	}

	private uint ChordIndexToMidiKey(float index, uint rootNote, MusicScale scale)
	{
		float chordSizeF = (int)ChordCount;
		Assert.IsTrue(chordSizeF > 0.0f);
		int octaveOffset = (int)(index / chordSizeF) + (index < 0.0f ? -1 : 0);
		float tonePreOctave = ChordIndexToToneOffset(index);
		int totalOffset = MusicUtility.TonesToSemitones(tonePreOctave, scale) + octaveOffset * (int)MusicUtility.semitonesPerOctave;
		return (uint)((int)rootNote + totalOffset);
	}
}
