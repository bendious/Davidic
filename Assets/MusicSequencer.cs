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
		private List<MidiEvent> m_events;
		private int eventIndex;

		const uint semitones_per_octave = 12U;

		private static uint[] Audio_major_scale_semitones = {
			0, 2, 4, 5, 7, 9, 11
		};
		private static uint[] Audio_natural_minor_scale_semitones = {
			0, 2, 3, 5, 7, 8, 10
		};
		private static uint[] Audio_harmonic_minor_scale_semitones = {
			0, 2, 3, 5, 7, 8, 11
		};
		//private static uint[] Audio_melodic_minor_scale_semitones = {
		//	0, 2, 3, 5, 7, 9, 11 ascending
		//	0, 2, 3, 5, 7, 8, 10 descending
		//};
		private static uint[] Audio_dorian_mode_semitones = {
			0, 2, 3, 5, 7, 9, 10
		};
		// Ionian mode is the same as the major scale
		private static uint[] Audio_phrygian_mode_semitones = {
			0, 1, 3, 5, 7, 8, 10
		};
		private static uint[] Audio_lydian_mode_semitones = {
			0, 2, 4, 6, 7, 9, 11
		};
		private static uint[] Audio_mixolydian_mode_semitones = {
			0, 2, 4, 5, 7, 9, 10
		};
		// Aeolian mode is the same as natural minor scale
		private static uint[] Audio_locrian_mode_semitones = {
			0, 1, 3, 5, 6, 8, 10
		};
		private static uint[][] Audio_scales = {
			Audio_major_scale_semitones,
			Audio_natural_minor_scale_semitones,
			Audio_harmonic_minor_scale_semitones,
			//Audio_melodic_minor_scale_semitones,
			Audio_dorian_mode_semitones,
			Audio_phrygian_mode_semitones,
			Audio_lydian_mode_semitones,
			Audio_mixolydian_mode_semitones,
			Audio_locrian_mode_semitones
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
			return octaveOffset * (int)semitones_per_octave + (int)scaleSemitones[mod(noteIndex, scaleLength)];
		}

		//--Public Methods
		public MusicSequencer(StreamSynthesizer synth, bool isScale, uint noteMin, uint noteMax, uint rootNoteIndex, uint scaleIndex, uint instrumentIndex)
			: base(synth)
		{
			uint timeItr = 0;
			m_timeTotal = (uint)(m_samplesPerSecond * (isScale ? 4.0f : UnityEngine.Random.Range(1.0f, 10.0f)/*TODO*/));
			m_events = new List<MidiEvent>();
			const uint middleAIndex = 58U;
			byte note_root = (byte)(middleAIndex + rootNoteIndex);
			int noteMinRooted = (int)noteMin - note_root;
			int noteMaxRooted = (int)noteMax - note_root;
			uint[] scaleSemitones = Audio_scales[scaleIndex];
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
				uint timeInc = isScale ? m_timeTotal / 8 : (uint)UnityEngine.Random.Range(m_samplesPerSecond / 10, Math.Min(m_samplesPerSecond, m_timeTotal - timeItr));
				if (timeItr + timeInc > m_timeTotal)
				{
					break;
				}

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
	}
}