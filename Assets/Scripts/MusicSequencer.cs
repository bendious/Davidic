using CSharpSynth.Midi;
using CSharpSynth.Synthesis;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;


public class MusicSequencer : CSharpSynth.Sequencer.MidiSequencer
{
	private readonly uint m_samplesPerSecond = 44100; // TODO: combine w/ PlayMusic::m_samplesPerSecond
	private readonly uint m_samplesPerSixtyFourth;

	private readonly uint m_rootKey;
	private readonly uint[] m_scaleSemitones;

	// TODO: convert to MidiFile?
	private readonly MusicBlock m_musicBlock;
	private readonly ChordProgression m_chordProgression;
	private readonly MusicRhythm m_rhythm;
	private readonly List<MidiEvent> m_events;

	private int m_sampleTime = 0;
	private int m_eventIndex = 0;


	//--Public Methods
	public MusicSequencer(StreamSynthesizer synth, MusicSequencer prevInstance, bool isScale, uint rootKeyIndex, uint scaleIndex, uint instrumentIndex, uint bpm, bool regenChords, bool regenRhythm, float[] noteLengthWeights, uint harmoniesMax)
		: base(synth)
	{
		// initialize
		m_events = new List<MidiEvent>();
		synth.NoteOffAll(true); // prevent orphaned notes playing forever
		m_samplesPerSixtyFourth = m_samplesPerSecond * MusicUtility.secondsPerMinute / bpm / MusicUtility.sixtyFourthsPerBeat;

		// determine constituent pieces
		m_rootKey = (uint)(MusicUtility.midiMiddleAKey + MusicUtility.ScaleOffset(MusicUtility.naturalMinorScaleSemitones, (int)rootKeyIndex)); // NOTE using A-minor since it contains only the natural notes // TODO: support scales starting on sharps/flats?
		m_scaleSemitones = MusicUtility.scales[scaleIndex];
		m_chordProgression = isScale ? new ChordProgression(new float[][] { MusicUtility.chordI, MusicUtility.chordII, MusicUtility.chordIII, MusicUtility.chordIV, MusicUtility.chordV, MusicUtility.chordVI, MusicUtility.chordVII, new float[] { 7.0f, 9.0f, 11.0f } }) : (prevInstance == null || regenChords ? MusicUtility.chordProgressions[UnityEngine.Random.Range(0, MusicUtility.chordProgressions.Length)] : prevInstance.m_chordProgression);
		m_rhythm = isScale ? new MusicRhythm(new uint[] { MusicUtility.sixtyFourthsPerBeat / 2U }, new float[] { 0.0f }) : (prevInstance == null || regenRhythm ? MusicRhythm.Random(m_chordProgression, noteLengthWeights) : prevInstance.m_rhythm);

		// switch to the requested instrument
		MidiEvent eventSetInstrument = new MidiEvent
		{
			deltaTime = 0,
			midiChannelEvent = MidiHelper.MidiChannelEvent.Program_Change,
			parameter1 = (byte)instrumentIndex,
			channel = 0, // TODO
		};
		m_events.Add(eventSetInstrument);

		// sequence into notes, organize into block(s)
		m_musicBlock = new MusicBlockSimple(m_rhythm.Sequence(m_chordProgression).ToArray());
		if (harmoniesMax > 0U)
		{
			m_musicBlock = new MusicBlockHarmony(m_musicBlock, harmoniesMax);
		}
		m_events.AddRange(m_musicBlock.ToMidiEvents(0U, m_rootKey, m_scaleSemitones, m_samplesPerSixtyFourth));
	}

	public override bool isPlaying
	{
		get { return true; }
	}

	public override CSharpSynth.Sequencer.MidiSequencerEvent Process(int frame)
	{
		CSharpSynth.Sequencer.MidiSequencerEvent seqEvt = new CSharpSynth.Sequencer.MidiSequencerEvent();

		// stop or loop
		if (m_sampleTime >= (int)LengthSamples)
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
		m_sampleTime += amount;
		base.IncrementSampleCounter(amount);
	}

	public override void SetTime(TimeSpan time)
	{
		// TODO: handle time changes beyond just reset-to-start?
		if (time.Ticks != 0)
		{
			return;
		}
		m_sampleTime = (int)time.Ticks;
		m_eventIndex = 0;
		base.SetTime(time);
	}

	public uint LengthSamples
	{
		get { return m_musicBlock.SixtyFourthsTotal() * m_samplesPerSixtyFourth; }
	}

	public void Display(string elementIdChords, string elementIdMain, uint bpm)
	{
		m_chordProgression.Display(m_scaleSemitones, elementIdChords);
		m_rhythm.Display(m_scaleSemitones, "osmd-rhythm");
		m_musicBlock.Display(m_rootKey, m_scaleSemitones, elementIdMain, bpm);
	}
}
