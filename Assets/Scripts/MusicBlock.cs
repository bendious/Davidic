using CSharpSynth.Midi;
using System.Collections.Generic;


public abstract class MusicBlock
{
	public struct NoteTimePair
	{
		public MusicNote m_note;
		public uint m_time;
	}

	public abstract uint SixtyFourthsTotal();

	public abstract List<NoteTimePair> GetNotes(uint timeOffset);

	public abstract List<MidiEvent> ToMidiEvents(uint startSixtyFourths, uint rootKey, uint[] scaleSemitones, uint samplesPerSixtyFourth);
}
