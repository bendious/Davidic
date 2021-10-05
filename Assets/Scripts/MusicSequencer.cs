using System;
using System.Collections.Generic;
using CSharpSynth.Midi;
using CSharpSynth.Synthesis;
using UnityEngine;


public class MusicSequencer : CSharpSynth.Sequencer.MidiSequencer
{
	private int m_sampleTime = 0;
	private uint m_samplesPerSecond = 44100;
	private uint m_timeTotal;

	// TODO: convert to MidiFile?
	private List<MusicNote> m_notes;
	private List<uint> m_keys;
	private List<uint> m_lengths; // measured in sixty-fourth notes
	private List<MidiEvent> m_events;
	private int m_eventIndex;

	const uint semitonesPerOctave = 12U;

	private static uint[] m_majorScaleSemitones = {
		0, 2, 4, 5, 7, 9, 11
	};
	private static uint[] m_naturalMinorScaleSemitones = {
		0, 2, 3, 5, 7, 8, 10
	};
	private static uint[] m_harmonicMinorScaleSemitones = {
		0, 2, 3, 5, 7, 8, 11
	};
	//private static uint[] m_melodicMinorScaleSemitones = {
	//	0, 2, 3, 5, 7, 9, 11 ascending
	//	0, 2, 3, 5, 7, 8, 10 descending
	//};
	private static uint[] m_dorianModeSemitones = {
		0, 2, 3, 5, 7, 9, 10
	};
	// Ionian mode is the same as the major scale
	private static uint[] m_phrygianModeSemitones = {
		0, 1, 3, 5, 7, 8, 10
	};
	private static uint[] m_lydianModeSemitones = {
		0, 2, 4, 6, 7, 9, 11
	};
	private static uint[] m_mixolydianModeSemitones = {
		0, 2, 4, 5, 7, 9, 10
	};
	// Aeolian mode is the same as natural minor scale
	private static uint[] m_locrianModeSemitones = {
		0, 1, 3, 5, 6, 8, 10
	};
	private static uint[][] m_scales = {
		m_majorScaleSemitones,
		m_naturalMinorScaleSemitones,
		m_harmonicMinorScaleSemitones,
		//m_melodicMinorScaleSemitones,
		m_dorianModeSemitones,
		m_phrygianModeSemitones,
		m_lydianModeSemitones,
		m_mixolydianModeSemitones,
		m_locrianModeSemitones
	};

	private int mod(int x, int m)
	{
		int r = x % m;
		return (r < 0) ? r + m : r;
	}

	private int scaleOffset(uint[] scaleSemitones, int noteIndex)
	{
		int scaleLength = scaleSemitones.Length;
		int octaveOffset = noteIndex / scaleLength - (noteIndex < 0 ? 1 : 0);
		return octaveOffset * (int)semitonesPerOctave + (int)scaleSemitones[mod(noteIndex, scaleLength)];
	}


	//--Public Methods
	public MusicSequencer(StreamSynthesizer synth, bool isScale, uint keyMin, uint keyMax, uint rootKeyIndex, uint scaleIndex, uint instrumentIndex, uint bpm)
		: base(synth)
	{
		uint timeItr = 0;
		uint measureCount = (isScale ? 1U : (uint)UnityEngine.Random.Range(1, 5)/*TODO*/);
		const uint beatsPerMeasure = 4U; // TODO
		const uint secondsPerMinute = 60U;
		const uint sixtyFourthsPerBeat = 16U; // TODO
		uint beatsTotal = beatsPerMeasure * measureCount;
		uint lengthItr = 0U;
		uint samplesPerBeat = m_samplesPerSecond * secondsPerMinute / bpm;
		m_timeTotal = samplesPerBeat * beatsTotal;
		m_notes = new List<MusicNote>();
		m_keys = new List<uint>();
		m_lengths = new List<uint>();
		m_events = new List<MidiEvent>();
		const uint middleAIndex = 57U;
		byte keyRoot = (byte)(middleAIndex + scaleOffset(m_naturalMinorScaleSemitones, (int)rootKeyIndex)); // NOTE using A-minor since it contains only the natural notes // TODO: support scales starting on sharps/flats?
		int keyMinRooted = (int)keyMin - keyRoot;
		int keyMaxRooted = (int)keyMax - keyRoot;
		uint[] scaleSemitones = m_scales[scaleIndex];
		int chordIdx = -1;
		uint samplesPerSixtyFourth = m_samplesPerSecond / bpm / sixtyFourthsPerBeat * secondsPerMinute;

		// switch to the requested instrument
		MidiEvent eventSetInstrument = new MidiEvent();
		eventSetInstrument.deltaTime = timeItr;
		eventSetInstrument.midiChannelEvent = MidiHelper.MidiChannelEvent.Program_Change;
		eventSetInstrument.parameter1 = (byte)instrumentIndex;
		eventSetInstrument.channel = 0; // TODO
		m_events.Add(eventSetInstrument);

		while (timeItr < m_timeTotal)
		{
			chordIdx = (isScale ? chordIdx + 1 : (int)UnityEngine.Random.Range(keyMinRooted, keyMaxRooted));
			uint sixtyFourthsCur = isScale ? sixtyFourthsPerBeat / 2U : (uint)(1 << (int)UnityEngine.Random.Range(0, Math.Min(6, beatsTotal * sixtyFourthsPerBeat - lengthItr))); // TODO: better capping at max measure end
			uint timeInc = samplesPerSixtyFourth * sixtyFourthsCur;
			if (timeItr + timeInc > m_timeTotal)
			{
				break;

			}

			MusicNote noteNew = new MusicNote(new float[] { 0.0f, 1.0f }, sixtyFourthsCur, UnityEngine.Random.Range(0.5f, 1.0f), new float[] { chordIdx, chordIdx + 2.0f }); // TODO: actual chords, coherent volume
			noteNew.toMidiEvents(keyRoot, scaleSemitones, ref m_keys, ref m_lengths, timeItr, timeInc, ref m_events);
			m_notes.Add(noteNew);

			timeItr += timeInc;
			lengthItr += noteNew.length;
		}
	}

	public override bool isPlaying
	{
		get { return true; }
	}

	public override CSharpSynth.Sequencer.MidiSequencerEvent Process(int frame)
	{
		CSharpSynth.Sequencer.MidiSequencerEvent seqEvt = new CSharpSynth.Sequencer.MidiSequencerEvent();

		// stop or loop
		if (m_sampleTime >= (int)m_timeTotal)
		{
			m_sampleTime = 0;
			return null;
		}
		while (m_eventIndex < m_events.Count && m_events[m_eventIndex].deltaTime < (m_sampleTime + frame))
		{
			seqEvt.Events.Add(m_events[m_eventIndex]);
			++m_eventIndex;
		}
		return seqEvt;
	}

	public override void IncrementSampleCounter(int amount)
	{
		m_sampleTime = m_sampleTime + amount;
		base.IncrementSampleCounter(amount);
	}

	public uint lengthSamples
	{
		get { return m_timeTotal; }
	}

	public uint[] keySequence
	{
		get { return m_keys.ToArray(); }
	}

	public uint[] lengthSequence
	{
		get { return m_lengths.ToArray(); }
	}
}
