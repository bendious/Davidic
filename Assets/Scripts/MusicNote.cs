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

	public MusicNote(MusicNote noteOrig, float indexOffset)
	{
		m_chordIndices = Enumerable.Select(noteOrig.m_chordIndices, index => index + indexOffset).ToArray();
		LengthSixtyFourths = noteOrig.LengthSixtyFourths;
		VolumePct = noteOrig.VolumePct;
		m_chord = noteOrig.m_chord;
	}

	public List<uint> MidiKeys(uint rootKey, uint[] scaleSemitones)
	{
		return m_chordIndices.ToList().ConvertAll(index => ChordIndexToMidiKey(index, rootKey, scaleSemitones));
	}

	public uint KeyCount
	{
		get { return (uint)m_chordIndices.Length; }
	}

	public uint LengthSixtyFourths { get; }
	public float VolumePct { get; }

	public List<MidiEvent> ToMidiEvents(uint rootNote, uint[] scaleSemitones, uint startSixtyFourths, uint samplesPerSixtyFourth)
	{
		List<MidiEvent> events = new List<MidiEvent>();

		uint startSample = startSixtyFourths * samplesPerSixtyFourth;
		foreach (float index in m_chordIndices)
		{
			uint keyCur = ChordIndexToMidiKey(index, rootNote, scaleSemitones);

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
				parameter1 = (byte)ChordIndexToMidiKey(index, rootNote, scaleSemitones),
				channel = 0,
			};
			events.Add(eventOff);
		}

		return events;
	}


	private uint ChordIndexToMidiKey(float index, uint rootNote, uint[] scaleSemitones)
	{
		float chordSizeF = (float)m_chord.Length;
		Assert.IsTrue(chordSizeF > 0.0f);
		float indexMod = Utility.Modulo(index, chordSizeF);
		Assert.IsTrue(indexMod < chordSizeF);
		int octaveOffset = (int)(index / chordSizeF) + (index < 0.0f ? -1 : 0);
		float indexFractAbs = Mathf.Abs(Utility.Fract(index));
		float tonePreOctave = (indexFractAbs <= 0.333f || indexFractAbs >= 0.667f) ? m_chord[(uint)Mathf.Round(indexMod)] : (m_chord[(int)Mathf.Floor(indexMod)] + m_chord[(int)Math.Ceiling(indexMod)]) * 0.5f; // TODO: better way of picking off-chord notes?
		int totalOffset = MusicUtility.TonesToSemitones(tonePreOctave, scaleSemitones) + octaveOffset * (int)MusicUtility.semitonesPerOctave;
		return (uint)((int)rootNote + totalOffset);
	}
}
