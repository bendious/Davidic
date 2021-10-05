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

	public void toMidiEvents(uint rootNote, uint[] scaleSemitones, ref List<uint> keys, ref List<uint> lengths, uint timeStart, uint timeInc, ref List<MidiEvent> events)
	{
		foreach (float index in m_chordIndices)
		{
			uint keyCur = chordIndexToMidiKey(index, rootNote, scaleSemitones);

			keys.Add(keyCur);
			lengths.Add(0); // in order to more easily format chords for display w/ MusicXML's <chord/> convention of attaching to the previous note, we put the length in only the first chord note; see below

			MidiEvent eventOn = new MidiEvent();
			eventOn.deltaTime = timeStart;
			eventOn.midiChannelEvent = MidiHelper.MidiChannelEvent.Note_On;
			eventOn.parameter1 = (byte)keyCur;
			eventOn.parameter2 = (byte)(m_volumePct * 100); // velocity
			eventOn.channel = 0;
			events.Add(eventOn);
		}
		lengths[lengths.Count - m_chordIndices.Length] = m_lengthSixtyFourths;

		foreach (float index in m_chordIndices)
		{
			MidiEvent eventOff = new MidiEvent();
			eventOff.deltaTime = timeStart + timeInc;
			eventOff.midiChannelEvent = MidiHelper.MidiChannelEvent.Note_Off;
			eventOff.parameter1 = (byte)chordIndexToMidiKey(index, rootNote, scaleSemitones);
			eventOff.parameter2 = (byte)UnityEngine.Random.Range(75U, 125U); // velocity
			eventOff.channel = 0;
			events.Add(eventOff);
		}
	}


	private float modulo(float x, float m)
	{
		float r = x % m;
		return (r < 0) ? r + m : r;
	}

	private float fract(float x)
	{
		return x - (float)Math.Truncate(x);
	}

	const uint semitonesPerOctave = 12U;

	private int tonesToSemitones(float tonesFromRoot, uint[] scaleSemitones)
	{
		// NOTE that due to note averaging for off-chord notes, tonesFromRoot can have unexpected fractional values, so we "round" to the nearest half tone to turn them into standard naturals/flats/sharps
		float scaleLength = scaleSemitones.Length;
		float tonesMod = modulo(tonesFromRoot, scaleLength);
		Debug.Assert(tonesMod >= 0.0f && tonesMod < scaleLength);
		int octaveOffset = (int)(tonesFromRoot / scaleLength) + (tonesFromRoot < 0.0f ? -1 : 0);
		float fractAbs = Mathf.Abs(fract(tonesFromRoot));
		int halftoneOffset = (fractAbs <= 0.333f || fractAbs >= 0.667f) ? 0 : (tonesFromRoot < 0.0f ? -1 : 1);

		return (int)(scaleSemitones[(uint)tonesMod] + octaveOffset * (int)semitonesPerOctave + halftoneOffset);
	}

	private uint chordIndexToMidiKey(float index, uint rootNote, uint[] scaleSemitones)
	{
		float chordSizeF = (float)m_chord.Length;
		Debug.Assert(chordSizeF > 0.0f);
		float indexMod = modulo(index, chordSizeF);
		Debug.Assert(indexMod < chordSizeF);
		int octaveOffset = (int)(index / chordSizeF) + (index < 0.0f ? -1 : 0);
		float indexFractAbs = Mathf.Abs(fract(index));
		float tonePreOctave = (indexFractAbs <= 0.333f || indexFractAbs >= 0.667f) ? m_chord[(uint)Mathf.Round(indexMod)] : (m_chord[(int)Mathf.Floor(indexMod)] + m_chord[(int)Math.Ceiling(indexMod)]) * 0.5f; // TODO: better way of picking off-chord notes?
		int totalOffset = tonesToSemitones(tonePreOctave, scaleSemitones) + octaveOffset * (int)semitonesPerOctave;
		return (uint)((int)rootNote + totalOffset);
	}
}
