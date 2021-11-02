using CSharpSynth.Synthesis;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;


public class MusicPlayer
{
	public uint m_samplesPerSecond = 44100U;
	public bool m_stereo = true;
	public uint m_maxPolyphony = 40U;

	public uint m_tempo;
	public bool m_scaleReuse;
	public uint m_rootNoteIndex;
	public uint m_scaleIndex;
	public bool[] m_instrumentToggles;
	public bool m_chordReuse;
	public ChordProgression m_chords;
	public bool m_rhythmReuse;
	public MusicRhythm m_rhythm;
	public float[] m_noteLengthWeights;
	public uint m_harmonyCount;
	public uint m_instrumentCount;
	public float m_volume;

	public string m_bankFilePath = "GM Bank/gm";


	private StreamSynthesizer m_musicStreamSynthesizer;
	private MusicSequencer m_musicSequencer;
	private uint[] m_instrumentIndices;


	public void Start()
	{
		// create synthesizer
		// TODO: recreate if relevant params change?
		int channels = m_stereo ? 2 : 1;
		const int bytesPerBuffer = 4096; // TODO?
		m_musicStreamSynthesizer = new StreamSynthesizer((int)m_samplesPerSecond, channels, bytesPerBuffer / channels, (int)m_maxPolyphony);
		m_musicStreamSynthesizer.LoadBank(m_bankFilePath);
	}

	public string[] InstrumentNames() => m_musicStreamSynthesizer.SoundBank.getInstruments(false).Where(instrument => instrument != null).Select(instrument => instrument.Name).ToArray();

	public void Generate(bool isScale)
	{
		// choose instrument
		List<int> candidateIndices = new List<int>();
		for (int i = 0, n = m_instrumentToggles.Length; i < n; ++i)
		{
			if (m_instrumentToggles[i])
			{
				candidateIndices.Add(i);
			}
		}
		if (candidateIndices.Count <= 0)
		{
			candidateIndices = Enumerable.Range(0, m_instrumentToggles.Length).ToList();
		}
		List<uint> instrumentList = new List<uint>();
		for (uint i = 0U, n = m_instrumentCount; i < n; ++i)
		{
			instrumentList.Add((uint)candidateIndices[UnityEngine.Random.Range(0, candidateIndices.Count)]);
		}
		m_instrumentIndices = instrumentList.Distinct().ToArray();

		// regen any random elements
		if (!m_scaleReuse)
		{
			m_rootNoteIndex = (uint)UnityEngine.Random.Range(0, (int)MusicUtility.tonesPerOctave);
			m_scaleIndex = (uint)UnityEngine.Random.Range(0, MusicUtility.scales.Length);
		}
		if (!m_chordReuse || m_chords.m_progression.Length <= 0)
		{
			m_chords = isScale ? new ChordProgression(new float[][] { MusicUtility.chordI, MusicUtility.chordII, MusicUtility.chordIII, MusicUtility.chordIV, MusicUtility.chordV, MusicUtility.chordVI, MusicUtility.chordVII, MusicUtility.chordI.Select(index => index + MusicUtility.tonesPerOctave).ToArray() }) : MusicUtility.chordProgressions[UnityEngine.Random.Range(0, MusicUtility.chordProgressions.Length)];
		}
		if (!m_rhythmReuse || m_rhythm.m_lengthsSixtyFourths.Length <= 0)
		{
			m_rhythm = isScale ? new MusicRhythm(new uint[] { MusicUtility.sixtyFourthsPerBeat / 2U }, new float[] { 0.0f }) : MusicRhythm.Random(m_chords, m_noteLengthWeights);
		}

		// create sequencer
		m_musicSequencer = new MusicSequencer(m_musicStreamSynthesizer, isScale, m_rootNoteIndex, m_scaleIndex, m_instrumentIndices, m_tempo, m_noteLengthWeights, m_harmonyCount, m_chords, m_rhythm);
	}

	public void Display(string elementIdChords, string elementIdRhythm, string elementIdMain)
	{
		if (m_musicSequencer != null)
		{
			string[] instrumentNames = m_instrumentIndices.Select(index => m_musicStreamSynthesizer.SoundBank.getInstrument((int)index, false).Name).ToArray();
			m_musicSequencer.Display(elementIdChords, elementIdRhythm, elementIdMain, instrumentNames, m_tempo);
		}
	}

	public void Play(AudioSource source)
	{
		Assert.IsNotNull(source);
		if (m_musicSequencer == null)
		{
			return;
		}
		source.volume = m_volume;
		source.clip = AudioClip.Create("Generated Clip", (int)m_musicSequencer.LengthSamples, m_stereo ? 2 : 1, (int)m_samplesPerSecond, false, OnAudioRead, OnAudioSetPosition); // NOTE that the streaming flag should be set if WebGL ever supports it
		source.time = 0.0f;
		OnAudioSetPosition(0); // needed to ensure WebGL resets audio clip
		source.Play();
	}

	public string Export(string filepath)
	{
		Assert.IsNotNull(m_musicSequencer);
		string[] instrumentNames = m_instrumentIndices.Select(index => m_musicStreamSynthesizer.SoundBank.getInstrument((int)index, false).Name).ToArray();
		return m_musicSequencer.Export(filepath, instrumentNames, m_tempo);
	}


	private void OnAudioRead(float[] data)
	{
		m_musicStreamSynthesizer.GetNext(data);
		// NOTE that we don't increment m_musicSequencer since m_musicStreamSynthesizer takes care of that
	}

	private void OnAudioSetPosition(int new_position) => m_musicSequencer.SetTime(System.TimeSpan.FromSeconds(new_position / (double)m_samplesPerSecond));
}
