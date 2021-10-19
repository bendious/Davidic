using CSharpSynth.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;


public class MusicNote : MusicBlock
{
	private readonly float[] m_chordIndices;
	private readonly float[] m_chord;


	public MusicNote(float[] chordIndices, uint lengthSixtyFourths, float volumePct, float[] chord)
	{
		// TODO: check for / remove chord/index duplicates?
		Assert.AreNotEqual(chordIndices.Length, 0);
		Assert.AreNotEqual(chord.Length, 0);
		m_chordIndices = chordIndices;
		LengthSixtyFourths = lengthSixtyFourths;
		VolumePct = volumePct;
		m_chord = chord;
	}

	public MusicNote(MusicNote noteOrig, float[] indexOffsets, bool checkOrigDuplicates)
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
	}

	public override uint SixtyFourthsTotal()
	{
		return LengthSixtyFourths;
	}

	public override List<NoteTimePair> GetNotes(uint timeOffset)
	{
		return new List<NoteTimePair> { new NoteTimePair { m_note = this, m_time = timeOffset } };
	}

	public override List<MidiEvent> ToMidiEvents(uint startSixtyFourths, uint rootKey, MusicScale scale, uint samplesPerSixtyFourth, uint channelIdx)
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
				channel = (byte)channelIdx,
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
				channel = (byte)channelIdx,
			};
			events.Add(eventOff);
		}

		return events;
	}

	public override MusicBlock SplitNotes()
	{
		if (LengthSixtyFourths < MusicUtility.sixtyFourthsPerBeat || UnityEngine.Random.value < 0.5f) // TODO: better conditions?
		{
			return this;
		}
		int splitCount = 2 << UnityEngine.Random.Range(0, 4); // TODO: take note length weights into account?
		uint splitLength = LengthSixtyFourths / (uint)splitCount;
		Assert.AreNotEqual(splitLength, 0U);
		MusicNote splitNote = new MusicNote(m_chordIndices, splitLength, VolumePct, m_chord);
		List<MusicNote> splitNotes = Enumerable.Repeat(splitNote, splitCount).ToList();
		for (int i = 0, n = splitNotes.Count; i < n; ++i)
		{
			splitNotes[i] = new MusicNote(splitNotes[i], new float[] { UnityEngine.Random.Range(0, m_chordIndices.Length) }, false);
		}
		return new MusicBlockSimple(splitNotes.ToArray());
	}

	public override MusicBlock MergeNotes()
	{
		return this;
	}

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

	public List<uint> MidiKeys(uint rootKey, MusicScale scale)
	{
		return m_chordIndices.ToList().ConvertAll(index => ChordIndexToMidiKey(index, rootKey, scale));
	}

	public uint KeyCount
	{
		get { return (uint)m_chordIndices.Length; }
	}

	public uint ChordCount
	{
		get { return (uint)m_chord.Length; }
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

		return (indexFractAbs <= 0.333f || indexFractAbs >= 0.667f) ? m_chord[(uint)Mathf.Round(indexMod)] : (m_chord[(int)Mathf.Floor(indexMod)] + m_chord[Utility.Modulo((int)Math.Ceiling(indexMod), chordSizeI)]) * 0.5f; // TODO: better way of picking off-chord notes?
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
