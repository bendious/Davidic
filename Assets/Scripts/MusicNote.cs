using CSharpSynth.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;


public class MusicNote
{
	private readonly float[] m_chordIndices;
	private readonly float[] m_chord;


	public MusicNote(float[] chordIndices, uint lengthSixtyFourths, float volumePctIn, float[] chord)
	{
		// TODO: check for / remove chord/index duplicates?
		m_chordIndices = chordIndices;
		LengthSixtyFourths = lengthSixtyFourths;
		VolumePct = volumePctIn;
		m_chord = chord;
	}

	public MusicNote(MusicNote noteOrig, float[] indexOffsets)
	{
		List<float> indicesCombined = new List<float>();
		foreach (float index in noteOrig.m_chordIndices)
		{
			foreach (float offset in indexOffsets)
			{
				float indexNew = index + offset;
				if (!noteOrig.m_chordIndices.Contains(indexNew))
				{
					indicesCombined.Add(indexNew);
				}
			}
		}
		m_chordIndices = indicesCombined.Distinct().ToArray();
		LengthSixtyFourths = noteOrig.LengthSixtyFourths;
		VolumePct = noteOrig.VolumePct;
		m_chord = noteOrig.m_chord;
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

	public List<MidiEvent> ToMidiEvents(uint rootNote, MusicScale scale, uint startSixtyFourths, uint samplesPerSixtyFourth)
	{
		List<MidiEvent> events = new List<MidiEvent>();

		uint startSample = startSixtyFourths * samplesPerSixtyFourth;
		foreach (float index in m_chordIndices)
		{
			uint keyCur = ChordIndexToMidiKey(index, rootNote, scale);

			MidiEvent eventOn = new MidiEvent
			{
				deltaTime = startSample,
				midiChannelEvent = MidiHelper.MidiChannelEvent.Note_On,
				parameter1 = (byte)keyCur,
				parameter2 = (byte)(VolumePct * 100), // velocity
				channel = 0,
			};
			events.Add(eventOn);
		}

		uint endSample = (startSixtyFourths + LengthSixtyFourths) * samplesPerSixtyFourth;
		foreach (float index in m_chordIndices)
		{
			MidiEvent eventOff = new MidiEvent
			{
				deltaTime = endSample,
				midiChannelEvent = MidiHelper.MidiChannelEvent.Note_Off,
				parameter1 = (byte)ChordIndexToMidiKey(index, rootNote, scale),
				channel = 0,
			};
			events.Add(eventOff);
		}

		return events;
	}


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
