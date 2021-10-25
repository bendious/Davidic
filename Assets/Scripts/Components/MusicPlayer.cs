using CSharpSynth.Synthesis;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;


[RequireComponent(typeof(AudioSource))]
public class MusicPlayer : MonoBehaviour
{
	public uint m_samplesPerSecond = 44100U;
	public bool m_stereo = true;
	public uint m_maxPolyphony = 40U;

	public InputField m_tempoField;
	public Toggle m_scaleRegenToggle;
	public Dropdown m_rootNoteDropdown;
	public Dropdown m_scaleDropdown;
	public ScrollRect m_instrumentScrollview;
	public Toggle m_chordRegenToggle;
	public InputField m_chordField;
	public Toggle m_rhythmRegenToggle;
	public InputField m_rhythmField;
	public InputField[] m_noteLengthFields;
	public InputField m_harmonyCountField;
	public InputField m_instrumentCountField;
	public InputField m_volumeField;

	private StreamSynthesizer m_musicStreamSynthesizer;
	private MusicSequencer m_musicSequencer;

	public string m_bankFilePath = "GM Bank/gm";

	public void Start()
	{
		// create synthesizer
		// TODO: recreate if relevant params change?
		int channels = (m_stereo ? 2 : 1);
		const int bytesPerBuffer = 4096; // TODO?
		m_musicStreamSynthesizer = new StreamSynthesizer((int)m_samplesPerSecond, channels, bytesPerBuffer / channels, (int)m_maxPolyphony);
		m_musicStreamSynthesizer.LoadBank(m_bankFilePath);

		// enumerate instruments in UI
		List<CSharpSynth.Banks.Instrument> instruments = m_musicStreamSynthesizer.SoundBank.getInstruments(false);
		Assert.AreEqual(m_instrumentScrollview.content.childCount, 1);
		GameObject placeholderObj = m_instrumentScrollview.content.GetChild(0).gameObject;
		Assert.IsFalse(placeholderObj.activeSelf);
		Vector2 nextPos = placeholderObj.GetComponent<RectTransform>().anchoredPosition;
		float startY = nextPos.y;
		float lineHeight = Mathf.Abs(placeholderObj.GetComponent<RectTransform>().rect.y);
		for (int i = 0, n = instruments.Count; i < n; ++i)
		{
			if (instruments[i] == null)
			{
				continue;
			}
			string instrumentName = instruments[i].Name;

			GameObject toggleObj = Instantiate(placeholderObj, m_instrumentScrollview.content.transform);
			toggleObj.name = instrumentName;
			toggleObj.GetComponentInChildren<Text>().text = instrumentName;
			toggleObj.GetComponent<RectTransform>().anchoredPosition = nextPos;
			toggleObj.SetActive(true);

			nextPos.y -= lineHeight;
		}
		m_instrumentScrollview.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Abs(nextPos.y + startY));
	}

	public void Generate(bool isScale)
	{
		// choose instrument
		List<int> candidateIndices = new List<int>();
		int numInactive = 0;
		for (int i = 0, n = m_instrumentScrollview.content.childCount; i < n; ++i)
		{
			GameObject childObj = m_instrumentScrollview.content.GetChild(i).gameObject;
			if (!childObj.activeSelf) {
				++numInactive;
			} else if (childObj.GetComponent<Toggle>().isOn)
			{
				candidateIndices.Add(i - numInactive);
			}
		}
		if (candidateIndices.Count <= 0)
		{
			return;
		}
		List<uint> instrumentList = new List<uint>();
		for (uint i = 0U, n = uint.Parse(m_instrumentCountField.text); i < n; ++i)
		{
			instrumentList.Add((uint)candidateIndices[UnityEngine.Random.Range(0, candidateIndices.Count)]);
		}
		uint[] instrumentIndices = instrumentList.Distinct().ToArray();
		string[] instrumentNames = instrumentIndices.Select(index => m_musicStreamSynthesizer.SoundBank.getInstrument((int)index, false/*?*/).Name).ToArray();

		// parse input
		uint bpm = uint.Parse(m_tempoField.text);
		float[][] chordList = m_chordField.text.Length == 0 ? new float[][] {} : m_chordField.text.Split(new char[] { ';' }).Select(str => str.Split(new char[] { ',' }).Select(str => float.Parse(str)).ToArray()).ToArray();
		uint[] rhythmLengths = m_rhythmField.text.Length == 0 ? new uint[] {} : m_rhythmField.text.Split(new char[] { ';' }).Select(str => uint.Parse(str.Split(new char[] { ',' })[0])).ToArray();
		float[] rhythmChords = m_rhythmField.text.Length == 0 ? new float[] {} : m_rhythmField.text.Split(new char[] { ';' }).Select(str => float.Parse(str.Split(new char[] { ',' })[1])).ToArray();

		// create sequencer
		m_musicSequencer = new MusicSequencer(m_musicStreamSynthesizer, isScale, (uint)m_rootNoteDropdown.value, (uint)m_scaleDropdown.value, instrumentIndices, bpm, m_scaleRegenToggle.isOn, m_chordRegenToggle.isOn, m_rhythmRegenToggle.isOn, m_noteLengthFields.Select(field => float.Parse(field.text)).ToArray(), uint.Parse(m_harmonyCountField.text), new ChordProgression(chordList.Select(list => list.ToArray()).ToArray()), new MusicRhythm(rhythmLengths, rhythmChords));

		// update display
		m_rootNoteDropdown.value = (int)m_musicSequencer.m_rootKeyIndex;
		m_rootNoteDropdown.RefreshShownValue();
		m_scaleDropdown.value = (int)m_musicSequencer.m_scaleIndex;
		m_scaleDropdown.RefreshShownValue();
		m_chordField.text = m_musicSequencer.m_chordProgression.m_progression.Aggregate("", (str, chord) => str + (str == "" ? "" : ";") + chord.Aggregate("", (str, idx) => str + (str == "" ? "" : ",") + idx));
		m_rhythmField.text = m_musicSequencer.m_rhythm.m_lengthsSixtyFourths.Zip(m_musicSequencer.m_rhythm.m_chordIndices, (a, b) => a + "," + b).Aggregate((a, b) => a + ";" + b);
		MusicDisplay.Start();
		m_musicSequencer.Display("osmd-chords", "osmd-rhythm", "osmd-main", instrumentNames, bpm);
		MusicDisplay.Finish();
	}

	public void Play()
	{
		if (m_musicSequencer == null)
		{
			return;
		}
		AudioSource audio_source = GetComponent<AudioSource>();
		audio_source.volume = float.Parse(m_volumeField.text);
		audio_source.clip = AudioClip.Create("Generated Clip", (int)m_musicSequencer.LengthSamples, m_stereo ? 2 : 1, (int)m_samplesPerSecond, false, OnAudioRead, OnAudioSetPosition);
		audio_source.Play();
	}


	private void OnAudioRead(float[] data)
	{
		m_musicStreamSynthesizer.GetNext(data);
		// NOTE that we don't increment m_musicSequencer since m_musicStreamSynthesizer takes care of that
	}

	private void OnAudioSetPosition(int new_position) => m_musicSequencer.SetTime(new System.TimeSpan(new_position));
}
