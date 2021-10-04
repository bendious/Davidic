using System;
using System.Collections.Generic;
using CSharpSynth.Midi;
using CSharpSynth.Synthesis;
using UnityEngine;

namespace CSharpSynth.Sequencer
{
	public class MusicSequencer : MidiSequencer
	{
		private int sampleTime = 0;
		private uint m_samplesPerSecond = 44100;
		private uint m_timeTotal;

		// TODO: convert to MidiFile?
		private List<uint> m_keys;
		private List<uint> m_lengths; // measured in sixty-fourth notes
		private List<MidiEvent> m_events;
		private int eventIndex;

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
		public MusicSequencer(StreamSynthesizer synth, bool isScale, uint noteMin, uint noteMax, uint rootNoteIndex, uint scaleIndex, uint instrumentIndex, uint bpm)
			: base(synth)
		{
			uint timeItr = 0;
			uint measureCount = (isScale ? 1U : (uint)UnityEngine.Random.Range(1, 5)/*TODO*/);
			const uint beatsPerMeasure = 4U; // TODO
			const uint secondsPerMinute = 60U;
			const uint sixtyFourthsPerBeat = 16U; // TODO
			uint beatsTotal = beatsPerMeasure * measureCount;
			uint lengthItr = 0U;
			m_timeTotal = (uint)(m_samplesPerSecond * beatsTotal / bpm * secondsPerMinute);
			m_keys = new List<uint>();
			m_lengths = new List<uint>();
			m_events = new List<MidiEvent>();
			const uint middleAIndex = 57U;
			byte note_root = (byte)(middleAIndex + scaleOffset(m_naturalMinorScaleSemitones, (int)rootNoteIndex)); // NOTE using A-minor since it contains only the natural notes // TODO: support scales starting on sharps/flats?
			int noteMinRooted = (int)noteMin - note_root;
			int noteMaxRooted = (int)noteMax - note_root;
			uint[] scaleSemitones = m_scales[scaleIndex];
			int note_idx = -1;

			// switch to the requested instrument
			MidiEvent eventSetInstrument = new MidiEvent();
			eventSetInstrument.deltaTime = timeItr;
			eventSetInstrument.midiChannelEvent = MidiHelper.MidiChannelEvent.Program_Change;
			eventSetInstrument.parameter1 = (byte)instrumentIndex;
			eventSetInstrument.channel = 1;//?
			m_events.Add(eventSetInstrument);

			while (timeItr < m_timeTotal)
			{
				// TODO: other types of events?

				note_idx = (isScale ? note_idx + 1 : (int)UnityEngine.Random.Range(noteMinRooted, noteMaxRooted));
				byte note_cur = (byte)(note_root + scaleOffset(scaleSemitones, note_idx));
				uint length_cur = isScale ? sixtyFourthsPerBeat / 2U : (uint)(1 << (int)UnityEngine.Random.Range(0, Math.Min(6, beatsTotal * sixtyFourthsPerBeat - lengthItr))); // TODO: better capping at max measure end
				uint timeInc = m_samplesPerSecond * length_cur / bpm / sixtyFourthsPerBeat * secondsPerMinute;
				if (timeItr + timeInc > m_timeTotal)
				{
					break;
				}

				m_keys.Add((uint)note_cur);
				m_lengths.Add(length_cur);
				MidiEvent eventOn = new MidiEvent();
				eventOn.deltaTime = timeItr;
				eventOn.midiChannelEvent = MidiHelper.MidiChannelEvent.Note_On;
				eventOn.parameter1 = note_cur; // note
				eventOn.parameter2 = (byte)UnityEngine.Random.Range(75U, 125U); // velocity
				eventOn.channel = 1;//?
				m_events.Add(eventOn);
				timeItr += timeInc;

				MidiEvent eventOff = new MidiEvent();
				eventOff.deltaTime = timeItr;
				eventOff.midiChannelEvent = MidiHelper.MidiChannelEvent.Note_Off;
				eventOff.parameter1 = note_cur; // note
				eventOff.parameter2 = (byte)UnityEngine.Random.Range(75U, 125U); // velocity
				eventOff.channel = 1;//?
				m_events.Add(eventOff);
				timeInc = 0U;//TODO?
				timeItr += timeInc;
				lengthItr += length_cur;
			}
		}

		public override bool isPlaying
		{
			get { return true; }
		}

		public override MidiSequencerEvent Process(int frame)
		{
			MidiSequencerEvent seqEvt = new MidiSequencerEvent();

			//stop or loop
			if (sampleTime >= (int)m_timeTotal)//_MidiFile.Tracks[0].TotalTime)
			{
				sampleTime = 0;
				//if (looping == true)
				//{
				//	//Clear the current programs for the channels.
				//	Array.Clear(currentPrograms, 0, currentPrograms.Length);
				//	//Clear vol, pan, and tune
				//	ResetControllers();
				//	//set bpm
				//	_MidiFile.BeatsPerMinute = 120;
				//	//Let the synth know that the sequencer is ready.
				//	eventIndex = 0;
				//}
				//else
				//{
				//	playing = false;
				//	synth.NoteOffAll(true);
				return null;
				//}
			}
			while (eventIndex < /*_MidiFile.Tracks[0].EventCount*/m_events.Count && /*_MidiFile.Tracks[0].MidiEvents*/m_events[eventIndex].deltaTime < (sampleTime + frame))
			{
				seqEvt.Events.Add(/*_MidiFile.Tracks[0].MidiEvents*/m_events[eventIndex]);
				eventIndex++;
			}
			return seqEvt;
		}

		public override void IncrementSampleCounter(int amount)
		{
			sampleTime = sampleTime + amount;
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
}