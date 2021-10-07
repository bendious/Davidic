using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using CSharpSynth.Midi;

public class MusicNote
{
	private readonly float[] m_chordIndices;
	private readonly uint m_lengthSixtyFourths;
	private readonly float m_volumePct;
	private readonly float[] m_chord;


	public MusicNote(float[] chordIndices, uint lengthSixtyFourths, float volumePct, float[] chord)
	{
		// TODO: check for / remove chord/index duplicates?
		m_chordIndices = chordIndices;
		m_lengthSixtyFourths = lengthSixtyFourths;
		m_volumePct = volumePct;
		m_chord = chord;
	}

	public uint[] midiKeys(uint rootKey, uint[] scaleSemitones)
	{
		return Array.ConvertAll(m_chordIndices, (float index) => chordIndexToMidiKey(index, rootKey, scaleSemitones));
	}

	public uint keyCount
	{
		get { return (uint)m_chordIndices.Length; }
	}

	public uint length
	{
		get { return m_lengthSixtyFourths; }
	}

	public float volume
	{
		get { return m_volumePct; }
	}

	public List<MidiEvent> toMidiEvents(uint rootNote, uint[] scaleSemitones, uint startSixtyFourths, uint samplesPerSixtyFourth)
	{
		List<MidiEvent> events = new List<MidiEvent>();

		uint startSample = startSixtyFourths * samplesPerSixtyFourth;
		foreach (float index in m_chordIndices)
		{
			uint keyCur = chordIndexToMidiKey(index, rootNote, scaleSemitones);

			MidiEvent eventOn = new MidiEvent
			{
				deltaTime = startSample,
				midiChannelEvent = MidiHelper.MidiChannelEvent.Note_On,
				parameter1 = (byte)keyCur,
				parameter2 = (byte)(m_volumePct * 100), // velocity
				channel = 0,
			};
			events.Add(eventOn);
		}

		uint endSample = (startSixtyFourths + m_lengthSixtyFourths) * samplesPerSixtyFourth;
		foreach (float index in m_chordIndices)
		{
			MidiEvent eventOff = new MidiEvent
			{
				deltaTime = endSample,
				midiChannelEvent = MidiHelper.MidiChannelEvent.Note_Off,
				parameter1 = (byte)chordIndexToMidiKey(index, rootNote, scaleSemitones),
				channel = 0,
			};
			events.Add(eventOff);
		}

		return events;
	}


	private uint chordIndexToMidiKey(float index, uint rootNote, uint[] scaleSemitones)
	{
		float chordSizeF = (float)m_chord.Length;
		Assert.IsTrue(chordSizeF > 0.0f);
		float indexMod = Utility.modulo(index, chordSizeF);
		Assert.IsTrue(indexMod < chordSizeF);
		int octaveOffset = (int)(index / chordSizeF) + (index < 0.0f ? -1 : 0);
		float indexFractAbs = Mathf.Abs(Utility.fract(index));
		float tonePreOctave = (indexFractAbs <= 0.333f || indexFractAbs >= 0.667f) ? m_chord[(uint)Mathf.Round(indexMod)] : (m_chord[(int)Mathf.Floor(indexMod)] + m_chord[(int)Math.Ceiling(indexMod)]) * 0.5f; // TODO: better way of picking off-chord notes?
		int totalOffset = MusicUtility.tonesToSemitones(tonePreOctave, scaleSemitones) + octaveOffset * (int)MusicUtility.semitonesPerOctave;
		return (uint)((int)rootNote + totalOffset);
	}
}
