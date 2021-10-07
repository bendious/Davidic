using CSharpSynth.Midi;
using System.Collections.Generic;


public abstract class MusicBlock
{
	public abstract uint SixtyFourthsTotal();

	public abstract List<uint> GetKeys(uint rootKey, uint[] scaleSemitones);
	public abstract List<uint> GetLengths();

	public abstract List<MidiEvent> ToMidiEvents(uint startSixtyFourths, uint rootKey, uint[] scaleSemitones, uint samplesPerSixtyFourth);
}
