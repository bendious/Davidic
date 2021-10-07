using System;
using System.Collections.Generic;


public class MusicBlock
{
	private MusicNote[] m_notes;


	public MusicBlock(MusicNote[] notes)
	{
		m_notes = notes;
	}

	public uint[] getKeys(uint rootKey, uint[] scaleSemitones)
	{
		List<uint> keyList = new List<uint>();
		foreach (MusicNote note in m_notes)
		{
			keyList.AddRange(note.midiKeys(rootKey, scaleSemitones));
		}
		return keyList.ToArray();
	}

	public uint[] getLengths()
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
}
