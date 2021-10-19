using CSharpSynth.Synthesis;
using System.Collections.Generic;
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
	public Dropdown m_rootNoteDropdown;
	public Dropdown m_scaleDropdown;
	public ScrollRect m_instrumentScrollview;
	public Toggle m_chordRegenToggle;
	public Toggle m_rhythmRegenToggle;
	public InputField[] m_noteLengthFields;
	public InputField m_harmonyCountField;
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
		int chosenInstrumentIdx = candidateIndices[Random.Range(0, candidateIndices.Count)];
		string instrumentName = m_musicStreamSynthesizer.SoundBank.getInstrument(chosenInstrumentIdx, false/*?*/).Name;

		uint bpm = uint.Parse(m_tempoField.text);
		m_musicSequencer = new MusicSequencer(m_musicStreamSynthesizer, m_musicSequencer, isScale, (uint)m_rootNoteDropdown.value, (uint)m_scaleDropdown.value, (uint)chosenInstrumentIdx, bpm, m_chordRegenToggle.isOn, m_rhythmRegenToggle.isOn, m_noteLengthFields.Select(field => float.Parse(field.text)).ToArray(), uint.Parse(m_harmonyCountField.text));

		// update display
		MusicDisplay.Start();
		m_musicSequencer.Display("osmd-chords", "osmd-main", instrumentName, bpm);
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

	private void OnAudioSetPosition(int new_position)
	{
		m_musicSequencer.SetTime(new System.TimeSpan(new_position));
	}
}
