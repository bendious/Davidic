using System.Collections.Generic;
using CSharpSynth.Midi;


public class MusicBlockSimple : MusicBlock
{
	private readonly MusicNote[] m_notes;


	public MusicBlockSimple(MusicNote[] notes)
	{
		m_notes = notes;
	}

	public override uint sixtyFourthsTotal()
	{
		uint sixtyFourthsSum = 0U;
		foreach (MusicNote note in m_notes)
		{
			sixtyFourthsSum += note.length;
		}
		return sixtyFourthsSum;
	}

	public override uint[] getKeys(uint rootKey, uint[] scaleSemitones)
	{
		List<uint> keyList = new List<uint>();
		foreach (MusicNote note in m_notes)
		{
			keyList.AddRange(note.midiKeys(rootKey, scaleSemitones));
		}
		return keyList.ToArray();
	}

	public override uint[] getLengths()
	{
		List<uint> lengthList = new List<uint>();
		foreach (MusicNote note in m_notes)
		{
			lengthList.Add(note.length);
			for (uint i = 1U, n = note.keyCount; i < n; ++i)
			{
				lengthList.Add(0U); // in order to more easily format chords for display w/ MusicXML's <chord/> convention of attaching to the previous note (see osmd_bridge.jslib), we put the length in only the first chord note
			}
		}
		return lengthList.ToArray();
	}

	public override MidiEvent[] toMidiEvents(uint startSixtyFourths, uint rootKey, uint[] scaleSemitones, uint samplesPerSixtyFourth)
	{
		List<MidiEvent> eventList = new List<MidiEvent>();
		uint sixtyFourthsItr = startSixtyFourths;
		foreach (MusicNote note in m_notes)
		{
			eventList.AddRange(note.toMidiEvents(rootKey, scaleSemitones, sixtyFourthsItr, samplesPerSixtyFourth));
			sixtyFourthsItr += note.length;
		}
		return eventList.ToArray();
	}
