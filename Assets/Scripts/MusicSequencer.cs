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


	//--Public Methods
	public MusicSequencer(StreamSynthesizer synth, bool isScale, uint keyMin, uint keyMax, uint rootKeyIndex, uint scaleIndex, uint instrumentIndex, uint bpm)
		: base(synth)
	{
		uint timeItr = 0;
		uint measureCount = (isScale ? 1U : (uint)UnityEngine.Random.Range(1, 5)/*TODO*/);
		const uint beatsPerMeasure = 4U; // TODO
		const uint sixtyFourthsPerBeat = 16U; // TODO
		uint beatsTotal = beatsPerMeasure * measureCount;
		uint lengthItr = 0U;
		uint samplesPerBeat = m_samplesPerSecond * MusicUtility.secondsPerMinute / bpm;
		m_timeTotal = samplesPerBeat * beatsTotal;
		m_notes = new List<MusicNote>();
		m_keys = new List<uint>();
		m_lengths = new List<uint>();
		m_events = new List<MidiEvent>();
		byte keyRoot = (byte)(MusicUtility.midiMiddleAKey + MusicUtility.scaleOffset(MusicUtility.naturalMinorScaleSemitones, (int)rootKeyIndex)); // NOTE using A-minor since it contains only the natural notes // TODO: support scales starting on sharps/flats?
		int keyMinRooted = (int)keyMin - keyRoot;
		int keyMaxRooted = (int)keyMax - keyRoot;
		uint[] scaleSemitones = MusicUtility.scales[scaleIndex];
		int chordIdx = -1;
		uint samplesPerSixtyFourth = m_samplesPerSecond / bpm / sixtyFourthsPerBeat * MusicUtility.secondsPerMinute;

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
