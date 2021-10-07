using System;
using System.Linq;
using System.Collections.Generic;
using CSharpSynth.Midi;


public class MusicBlockRepeat : MusicBlock
{
	private readonly MusicBlock[] m_children;
	private readonly uint[] m_schedule;


	public MusicBlockRepeat(MusicBlock[] children, uint[] schedule)
	{
		m_children = children;
		m_schedule = schedule;
	}

	public override uint sixtyFourthsTotal()
	{
		return (uint)Enumerable.Sum(Array.ConvertAll(CombineViaSchedule(typeof(Func<uint>), "sixtyFourthsTotal", (object x) => new uint[] { (uint)x }, passthroughVargs), (uint unsigned) => (int)unsigned));
	}

	public override uint[] getKeys(uint rootKey, uint[] scaleSemitones)
	{
		return CombineViaSchedule(typeof(Func<uint, uint[], uint[]>), "getKeys", objectUintListCast, passthroughVargs, rootKey, scaleSemitones);
	}

	public override uint[] getLengths()
	{
		return CombineViaSchedule(typeof(Func<uint[]>), "getLengths", objectUintListCast, passthroughVargs);
	}

	public override MidiEvent[] toMidiEvents(uint startSixtyFourths, uint rootKey, uint[] scaleSemitones, uint samplesPerSixtyFourth)
	{
		return CombineViaSchedule(typeof(Func<uint, uint, uint[], uint, MidiEvent[]>), "toMidiEvents", (object x) => (MidiEvent[])x, (object[] vargs, MusicBlock block) => { vargs[0] = (uint)vargs[0] + block.sixtyFourthsTotal(); return vargs; }, startSixtyFourths, rootKey, scaleSemitones, samplesPerSixtyFourth);
	}


	private readonly Func<object[], MusicBlock, object[]> passthroughVargs = (object[] vargs, MusicBlock block) => vargs;
	private readonly Func<object, uint[]> objectUintListCast = (object x) => (uint[])x;

	private T[] CombineViaSchedule<T>(Type funcType, string funcName, Func<object, T[]> outputToListFunc, Func<object[], MusicBlock, object[]> vargsIncFunc, params object[] vargs)
	{
		List<T> list = new List<T>(); // TODO: less array/list interconversion?
		foreach (uint index in m_schedule)
		{
			// TODO: efficiency?
			MusicBlock childBlock = m_children[index];
			Delegate retargetedFunc = Delegate.CreateDelegate(funcType, childBlock, funcName);
			list.AddRange(outputToListFunc(retargetedFunc.DynamicInvoke(vargs)));
			vargs = vargsIncFunc(vargs, childBlock);
		}
		return list.ToArray();
	}
}
