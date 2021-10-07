using CSharpSynth.Midi;


public abstract class MusicBlock
{
	public abstract uint sixtyFourthsTotal();

	public abstract uint[] getKeys(uint rootKey, uint[] scaleSemitones);
	public abstract uint[] getLengths();

	public abstract MidiEvent[] toMidiEvents(uint startSixtyFourths, uint rootKey, uint[] scaleSemitones, uint samplesPerSixtyFourth);
}
