using System;
using System.Collections.Generic;
using UnityEngine;
using CSharpSynth.Midi;

public class MusicNote
{
	private float[] m_chordIndices;
	private uint m_lengthSixtyFourths;
	private float m_volumePct;
	private float[] m_chord;


	public MusicNote(float[] chordIndices, uint lengthSixtyFourths, float volumePct, float[] chord)
	{
		m_chordIndices = chordIndices;
		m_lengthSixtyFourths = lengthSixtyFourths;
		m_volumePct = volumePct;
		m_chord = chord;
	}

	public float[] chordIndices
	{
		get { return m_chordIndices; }
	}

	public uint length
	{
		get { return m_lengthSixtyFourths; }
	}

	public float volume
	{
		get { return m_volumePct; }
	}

	public void toMidiEvents(uint rootNote, uint[] scaleSemitones, ref List<uint> keys, ref List<uint> lengths, uint startSixtyFourths, uint samplesPerSixtyFourth, ref List<MidiEvent> events)
	{
		uint startSample = startSixtyFourths * samplesPerSixtyFourth;
		foreach (float index in m_chordIndices)
		{
			uint keyCur = chordIndexToMidiKey(index, rootNote, scaleSemitones);

			keys.Add(keyCur);
			lengths.Add(0); // in order to more easily format chords for display w/ MusicXML's <chord/> convention of attaching to the previous note, we put the length in only the first chord note; see below

			MidiEvent eventOn = new MidiEvent();
			eventOn.deltaTime = startSample;
			eventOn.midiChannelEvent = MidiHelper.MidiChannelEvent.Note_On;
			eventOn.parameter1 = (byte)keyCur;
			eventOn.parameter2 = (byte)(m_volumePct * 100); // velocity
			eventOn.channel = 0;
			events.Add(eventOn);
		}
		lengths[lengths.Count - m_chordIndices.Length] = m_lengthSixtyFourths;

		uint endSample = (startSixtyFourths + m_lengthSixtyFourths) * samplesPerSixtyFourth;
		foreach (float index in m_chordIndices)
		{
			MidiEvent eventOff = new MidiEvent();
			eventOff.deltaTime = endSample;
			eventOff.midiChannelEvent = MidiHelper.MidiChannelEvent.Note_Off;
			eventOff.parameter1 = (byte)chordIndexToMidiKey(index, rootNote, scaleSemitones);
			eventOff.channel = 0;
			events.Add(eventOff);
		}
	}


	private uint chordIndexToMidiKey(float index, uint rootNote, uint[] scaleSemitones)
	{
		float chordSizeF = (float)m_chord.Length;
		Debug.Assert(chordSizeF > 0.0f);
		float indexMod = Utility.modulo(index, chordSizeF);
		Debug.Assert(indexMod < chordSizeF);
		int octaveOffset = (int)(index / chordSizeF) + (index < 0.0f ? -1 : 0);
		float indexFractAbs = Mathf.Abs(Utility.fract(index));
		float tonePreOctave = (indexFractAbs <= 0.333f || indexFractAbs >= 0.667f) ? m_chord[(uint)Mathf.Round(indexMod)] : (m_chord[(int)Mathf.Floor(indexMod)] + m_chord[(int)Math.Ceiling(indexMod)]) * 0.5f; // TODO: better way of picking off-chord notes?
		int totalOffset = MusicUtility.tonesToSemitones(tonePreOctave, scaleSemitones) + octaveOffset * (int)MusicUtility.semitonesPerOctave;
		return (uint)((int)rootNote + totalOffset);
	}
}
