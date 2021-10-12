using CSharpSynth.Midi;
using CSharpSynth.Synthesis;
using System;
using System.Collections.Generic;


public class MusicSequencer : CSharpSynth.Sequencer.MidiSequencer
{
	private int m_sampleTime = 0;
	private readonly uint m_samplesPerSecond = 44100; // TODO: combine w/ PlayMusic::m_samplesPerSecond
	private readonly uint m_samplesPerSixtyFourth;

	// TODO: convert to MidiFile?
	private readonly MusicBlock m_musicBlock;
	public readonly float[][] m_chordProgression;
	private readonly List<MidiEvent> m_events;
	public readonly uint m_rootKey;
	public readonly uint[] m_scaleSemitones;

	private int m_eventIndex;


	//--Public Methods
	public MusicSequencer(StreamSynthesizer synth, bool isScale, uint rootKeyIndex, uint scaleIndex, uint instrumentIndex, uint bpm)
		: base(synth)
	{
		const uint sixtyfourthsPerMeasure = 64U;
		const uint sixtyFourthsPerBeat = 16U; // TODO

		m_events = new List<MidiEvent>();

		synth.NoteOffAll(true); // prevent orphaned notes playing forever

		m_samplesPerSixtyFourth = m_samplesPerSecond * MusicUtility.secondsPerMinute / bpm / sixtyFourthsPerBeat;
		uint measureCount = (isScale ? 1U : (uint)UnityEngine.Random.Range(1, 5)/*TODO*/);
		uint sixtyfourthsTotal = sixtyfourthsPerMeasure * measureCount;

		m_rootKey = (uint)(MusicUtility.midiMiddleAKey + MusicUtility.ScaleOffset(MusicUtility.naturalMinorScaleSemitones, (int)rootKeyIndex)); // NOTE using A-minor since it contains only the natural notes // TODO: support scales starting on sharps/flats?
		m_scaleSemitones = MusicUtility.scales[scaleIndex];
		m_chordProgression = isScale ? new float[][] { MusicUtility.chordI, MusicUtility.chordII, MusicUtility.chordIII, MusicUtility.chordIV, MusicUtility.chordV, MusicUtility.chordVI, MusicUtility.chordVII, new float[] { 7.0f, 9.0f, 11.0f } } : MusicUtility.chordProgressions[UnityEngine.Random.Range(0, MusicUtility.chordProgressions.Length)]; // TODO: intelligent / user-determined choice?

		// switch to the requested instrument
		MidiEvent eventSetInstrument = new MidiEvent
		{
			deltaTime = 0,
			midiChannelEvent = MidiHelper.MidiChannelEvent.Program_Change,
			parameter1 = (byte)instrumentIndex,
			channel = 0, // TODO
		};
		m_events.Add(eventSetInstrument);

		// create notes
		uint sixtyFourthsItr = 0U;
		int chordProgIdx = 0;
		int chordIdx = 0;
		List<MusicNote> notesTemp = new List<MusicNote>();
		while (sixtyFourthsItr < sixtyfourthsTotal)
		{
			float[] chord = m_chordProgression[chordProgIdx]; // TODO: pass whole progression and an index to each note?
			chordIdx = isScale ? chordIdx : UnityEngine.Random.Range(0, chord.Length); // TODO: allow chord octave wrapping here as well as in harmonies?
			uint sixtyFourthsCur = isScale ? sixtyFourthsPerBeat / 2U : (uint)(1 << UnityEngine.Random.Range(0, (int)Math.Min(6U, sixtyfourthsTotal - sixtyFourthsItr))); // TODO: better capping at max measure end

			MusicNote noteNew = new MusicNote(new float[] { chordIdx }, sixtyFourthsCur, UnityEngine.Random.Range(0.5f, 1.0f), chord); // TODO: coherent volume
			notesTemp.Add(noteNew);

			sixtyFourthsItr += noteNew.LengthSixtyFourths;
			chordProgIdx = Utility.Modulo(chordProgIdx + 1, m_chordProgression.Length); // TODO: don't increment every note?
		}

		// organize notes into block(s)
		m_musicBlock = new MusicBlockSimple(notesTemp.ToArray());
		if (UnityEngine.Random.value > 0.5f)
		{
			m_musicBlock = new MusicBlockHarmony(m_musicBlock);
		}
		if (!isScale)
		{
			m_musicBlock = new MusicBlockRepeat(new MusicBlock[] { m_musicBlock }, new uint[] { 0, 0 }); // TODO: more sophisticated arrangements
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

	public uint LengthSamples
	{
		get { return m_musicBlock.SixtyFourthsTotal() * m_samplesPerSixtyFourth; }
	}

	public List<MusicBlock.NoteTimePair> NoteTimeSequence
	{
		get { return m_musicBlock.GetNotes(0U); }
	}
}
