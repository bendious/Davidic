using CSharpSynth.Synthesis;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;


public class MusicPlayer
{
	public uint m_samplesPerSecond = 44100U;
	public bool m_stereo = true;
	public uint m_maxPolyphony = 40U;

	public uint m_tempo = 60U;
	public bool m_scaleReuse = false;
	public uint m_rootNoteIndex = MusicUtility.midiMiddleCKey;
	public uint m_scaleIndex = 0U;
	public bool[] m_instrumentToggles;
	public bool m_chordReuse = false;
	public ChordProgression m_chords;
	public bool m_rhythmReuse = false;
	public MusicRhythm m_rhythm;
	public float[] m_noteLengthWeights = { 0.25f, 0.5f, 1.0f, 1.0f, 0.2f, 0.025f, 0.01f };
	public uint m_harmonyCount;
	public uint m_instrumentCount;
	public float m_volume = 0.5f;

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

	public int InstrumentCount => m_musicStreamSynthesizer == null ? 0 : m_musicStreamSynthesizer.SoundBank.getInstruments(false).Count(instrument => instrument != null); // NOTE that we can't just use SoundBank.InstrumentCount since that includes null instruments...

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
			instrumentList.Add((uint)candidateIndices[Utility.RandomRange(0, candidateIndices.Count)]);
		}
		m_instrumentIndices = instrumentList.Distinct().ToArray();

		// regen any random elements
		if (!m_scaleReuse)
		{
			m_rootNoteIndex = (uint)Utility.RandomRange(0, (int)MusicUtility.tonesPerOctave);
			m_scaleIndex = (uint)Utility.RandomRange(0, MusicUtility.scales.Length);
		}
		if (!m_chordReuse || m_chords.m_progression.Length <= 0)
		{
			m_chords = isScale ? new ChordProgression(new float[][] { MusicUtility.chordI, MusicUtility.chordII, MusicUtility.chordIII, MusicUtility.chordIV, MusicUtility.chordV, MusicUtility.chordVI, MusicUtility.chordVII, MusicUtility.chordI.Select(index => index + MusicUtility.tonesPerOctave).ToArray() }) : MusicUtility.chordProgressions[Utility.RandomRange(0, MusicUtility.chordProgressions.Length)];
		}
		if (!m_rhythmReuse || m_rhythm.m_lengthsSixtyFourths.Length <= 0)
		{
			m_rhythm = isScale ? new MusicRhythm(new uint[] { MusicUtility.sixtyFourthsPerBeat / 2U }, new float[] { 0.0f }) : MusicRhythm.Random(m_chords, m_noteLengthWeights);
		}

		// create sequencer
		m_musicSequencer = new MusicSequencer(m_musicStreamSynthesizer, isScale, m_rootNoteIndex, m_scaleIndex, m_instrumentIndices, m_tempo, m_noteLengthWeights, m_harmonyCount, m_chords, m_rhythm);
	}

	public float LengthSeconds => m_musicSequencer == null ? 0.0f : m_musicSequencer.LengthSamples / (float)m_samplesPerSecond;

	public void Display(string elementIdChords, string elementIdRhythm, string elementIdMain)
	{
		if (m_musicSequencer != null)
		{
			string[] instrumentNames = m_instrumentIndices.Select(index => m_musicStreamSynthesizer.SoundBank.getInstrument((int)index, false).Name).ToArray();
			m_musicSequencer.Display(elementIdChords, elementIdRhythm, elementIdMain, instrumentNames, m_tempo);
		}
	}

	public IEnumerator Play(AudioSource[] sources, bool stream)
	{
		Assert.IsTrue(sources.Length > 1);

		// set up manual streaming since Unity can't automate it on web builds
		// see https://johnleonardfrench.com/ultimate-guide-to-playscheduled-in-unity/
		// NOTE that if ever support is added, most all of this should be replaced w/ AudioClip.Create()'s stream flag
		const double perClipSecondsMax = 1.0;
		const double gapSeconds = 0.25;

		if (m_musicSequencer == null)
		{
			yield break;
		}
		Assert.IsFalse(sources.Any(source => source == null));

		// inter-loop data
		int perClipSamplesMax = stream ? (int)(perClipSecondsMax * m_samplesPerSecond) : int.MaxValue;
		int samplesTotal = (int)m_musicSequencer.LengthSamples;

		// iterators
		int sampleItr = 0;
		int sourceIdxItr = 0;
		int clipCount = 0;
		AudioClip.PCMSetPositionCallback positionCallback = OnAudioSetPosition;
		double timeItr = AudioSettings.dspTime + gapSeconds; // NOTE that we schedule the first clip a little in advance to keep our subsequent times from being off due to the scheduler not having enough lead time

		// loop through audio in chunks
		while (sampleItr < samplesTotal)
		{
			// create clip on next source
			AudioSource source = sources[sourceIdxItr];
			int samplesCur = System.Math.Min(perClipSamplesMax, samplesTotal - sampleItr);
			source.volume = m_volume;
			source.clip = AudioClip.Create("Generated Clip " + clipCount, samplesCur, m_stereo ? 2 : 1, (int)m_samplesPerSecond, false, OnAudioRead, positionCallback);

			// schedule and wait until partway done
			source.PlayScheduled(timeItr);
			yield return new WaitForSeconds((float)(timeItr + gapSeconds - AudioSettings.dspTime)); // we wait until the clip has just started, to give ourselves plenty of lead time w/o putting too much work into a single frame

			// increment
			sampleItr += samplesCur;
			sourceIdxItr = (sourceIdxItr + 1) % sources.Length;
			++clipCount;
			positionCallback = IgnoreAudioSetPosition;
			timeItr += (double)samplesCur / m_samplesPerSecond;
		}
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

	private void OnAudioSetPosition(int newPosition) => m_musicSequencer.SetTime(System.TimeSpan.FromSeconds(newPosition / (double)m_samplesPerSecond));

	private void IgnoreAudioSetPosition(int newPosition) { } // necessary to avoid the sequencer getting reset every time the next chunk is manually "streamed"
}
