using CSharpSynth.Midi;
using CSharpSynth.Synthesis;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;


public class MusicSequencer : CSharpSynth.Sequencer.MidiSequencer
{
	private static readonly int[,] rootKeyToFifths = // TODO: functionize / determine algorithmically? handle signatures containing both sharps and flats?
	{
		{ 3, 5, 0, 2, 4, -1, 1 }, // major
		{ 0, 2, -3, -1, 1, -4, -2 }, // natural minor
		{ 1, 3, -2, +1-1, 2, -3, +1-2 }, // harmonic minor
		{ 1, 3, -2, 0, 2, -3, -1 }, // dorian
		{ -1, 1, -4, -2, 0, -5, -3 }, // phrygian
		{ 4, 6, 1, 3, 5, 0, 2 }, // lydian
		{ 2, 4, -1, 1, 3, -2, 0 }, // mixolydian
		{ -2, 0, -5, -3, -1, -6, -4 }, // locrian
	};

	private readonly uint m_samplesPerSecond = 44100; // TODO: combine w/ PlayMusic::m_samplesPerSecond
	private readonly uint m_samplesPerSixtyFourth;

	private readonly uint m_rootKey;
	private readonly MusicScale m_scale;

	// TODO: convert to MidiFile?
	private readonly MusicBlock m_musicBlock;
	public readonly ChordProgression m_chordProgression;
	public readonly MusicRhythm m_rhythm;
	private readonly MidiEvent[] m_events;

	private int m_sampleTime = 0;
	private int m_eventIndex = 0;


	public MusicSequencer(StreamSynthesizer synth, bool isScale, uint rootKeyIndex, uint scaleIndex, uint[] instrumentIndices, uint bpm, float[] noteLengthWeights, uint harmoniesMax, ChordProgression chords, MusicRhythm rhythm)
		: base(synth)
	{
		// initialize
		List<MidiEvent> eventList = new List<MidiEvent>();
		synth.NoteOffAll(true); // prevent orphaned notes playing forever
		m_samplesPerSixtyFourth = m_samplesPerSecond * MusicUtility.secondsPerMinute / bpm / MusicUtility.sixtyFourthsPerBeat;

		// determine constituent pieces
		m_rootKey = (uint)(MusicUtility.midiMiddleAKey + MusicUtility.ScaleOffset(MusicUtility.naturalMinorScale, (int)rootKeyIndex)); // NOTE using A-minor since it contains only the natural notes // TODO: support scales starting on sharps/flats?
		MusicScale scaleOrig = MusicUtility.scales[scaleIndex];
		m_scale = new MusicScale(scaleOrig.m_semitones, rootKeyToFifths[scaleIndex,rootKeyIndex], scaleOrig.m_mode);
		m_chordProgression = isScale ? new ChordProgression(new float[][] { MusicUtility.chordI, MusicUtility.chordII, MusicUtility.chordIII, MusicUtility.chordIV, MusicUtility.chordV, MusicUtility.chordVI, MusicUtility.chordVII, MusicUtility.chordI.Select(index => index + MusicUtility.tonesPerOctave).ToArray() }) : chords;
		m_rhythm = isScale ? new MusicRhythm(new uint[] { MusicUtility.sixtyFourthsPerBeat / 2U }, new float[] { 0.0f }) : rhythm;

		// switch to the requested instruments
		byte channelIdx = 0;
		foreach (uint instrumentIndex in instrumentIndices)
		{
			MidiEvent eventSetInstrument = new MidiEvent
			{
				deltaTime = 0,
				midiChannelEvent = MidiHelper.MidiChannelEvent.Program_Change,
				parameter1 = (byte)instrumentIndex,
				channel = channelIdx++,
			};
			eventList.Add(eventSetInstrument);
		}

		// sequence into notes
		List<MusicNote> notes = m_rhythm.Sequence(m_chordProgression, 0U);

		if (!isScale)
		{
			// ensure ending on a long root note // TODO: better outro logic?
			uint outroLengthMin = MusicUtility.sixtyFourthsPerMeasure / (uint)UnityEngine.Random.Range(1, 3);
			if (notes.Last().ContainsRoot()) // TODO: include harmonies in check but also add harmonies if adding an additional note?
			{
				notes.Last().LengthSixtyFourths = Math.Max(outroLengthMin, notes.Last().LengthSixtyFourths);
			}
			else
			{
				notes.Add(new MusicNote(new float[] { 0.0f }, outroLengthMin, UnityEngine.Random.Range(0.5f, 1.0f), MusicUtility.chordI7, 0U)); // TODO: coherent volume?
			}
		}

		// organize into block(s)
		m_musicBlock = new MusicBlockSimple(notes.ToArray());
		for (uint channelItr = (harmoniesMax > 0U) ? 0U : 1U, n = (uint)instrumentIndices.Length; channelItr < n; ++channelItr)
		{
			m_musicBlock = new MusicBlockHarmony(m_musicBlock, harmoniesMax, channelItr, noteLengthWeights);
		}
		eventList.AddRange(m_musicBlock.ToMidiEvents(0U, m_rootKey, m_scale, m_samplesPerSixtyFourth));

		m_events = eventList.ToArray();
	}

	public override bool isPlaying
	{
		get => true;
	}

	public override CSharpSynth.Sequencer.MidiSequencerEvent Process(int frame)
	{
		if (m_sampleTime >= (int)LengthSamples)
		{
			return null;
		}

		CSharpSynth.Sequencer.MidiSequencerEvent seqEvt = new CSharpSynth.Sequencer.MidiSequencerEvent();

		while (m_eventIndex < m_events.Length && m_events[m_eventIndex].deltaTime < (m_sampleTime + frame))
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
		Assert.IsTrue(time.Ticks >= 0 && m_samplesPerSecond > 0U);
		if (time.Ticks > 0) // TODO: handle non-restart set requests?
		{
			return;
		}
		m_sampleTime = 0;
		m_eventIndex = 0;
		base.SetTime(time);
	}

	public uint LengthSamples
	{
		get => m_musicBlock.SixtyFourthsTotal() * m_samplesPerSixtyFourth;
	}

	public void Display(string elementIdChords, string elementIdRhythm, string elementIdMain, string[] instrumentNames, uint bpm)
	{
		m_chordProgression.Display(elementIdChords);
		m_rhythm.Display(elementIdRhythm);
		m_musicBlock.Display(m_rootKey, m_scale, elementIdMain, instrumentNames, bpm);
	}

	public string Export(string filepath, string[] instrumentNames, uint bpm)
	{
		return m_musicBlock.Export(filepath, m_rootKey, m_scale, instrumentNames, bpm);
	}
}
