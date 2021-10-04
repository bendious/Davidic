using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using CSharpSynth.Effects;
using CSharpSynth.Sequencer;
using CSharpSynth.Synthesis;
using CSharpSynth.Midi;

[RequireComponent(typeof(AudioSource))]
public class PlayMusic : MonoBehaviour
{
	// see Plugins/OSMD_bridge/osmd_bridge.jslib
	[DllImport("__Internal")]
	private static extern void OSMD_update(int bpm, uint[] keys, int key_count);

	public uint m_samplesPerSecond = 44100U;
	public bool m_stereo = true;
	public uint maxPolyphony = 40U;

	public UnityEngine.UI.InputField m_keyMinField;
	public UnityEngine.UI.InputField m_keyMaxField;
	public UnityEngine.UI.InputField m_tempoField;
	public UnityEngine.UI.Dropdown m_rootNoteDropdown;
	public UnityEngine.UI.Dropdown m_scaleDropdown;
	public UnityEngine.UI.Dropdown m_instrumentDropdown;
	public UnityEngine.UI.InputField m_volumeField;

	private StreamSynthesizer musicStreamSynthesizer;
	private MusicSequencer musicSequencer;

	public string bankFilePath = "GM Bank/gm";

	public void Start()
	{
		// create synthesizer
		// TODO: recreate if relevant params change?
		int channels = (m_stereo ? 2 : 1);
		const int bytesPerBuffer = 4096; // TODO?
		musicStreamSynthesizer = new StreamSynthesizer((int)m_samplesPerSecond, channels, bytesPerBuffer / channels, (int)maxPolyphony);
		musicStreamSynthesizer.LoadBank(bankFilePath);

		// enumerate instruments in UI
		m_instrumentDropdown.ClearOptions();
		List<CSharpSynth.Banks.Instrument> instruments = musicStreamSynthesizer.SoundBank.getInstruments(false);
		for (int i = 0, n = instruments.Count; i < n; ++i)
		{
			if (instruments[i] == null)
			{
				continue;
			}
			m_instrumentDropdown.options.Add(new UnityEngine.UI.Dropdown.OptionData(instruments[i].Name));
		}
		m_instrumentDropdown.RefreshShownValue();
	}

	public void playMusic(bool isScale)
	{
		uint channels = (m_stereo ? 2U : 1U);

		musicSequencer = new MusicSequencer(musicStreamSynthesizer, isScale, uint.Parse(m_keyMinField.text), uint.Parse(m_keyMaxField.text), (uint)m_rootNoteDropdown.value, (uint)m_scaleDropdown.value, (uint)m_instrumentDropdown.value);

		uint length_samples = musicSequencer.lengthSamples;

		AudioSource audio_source = GetComponent<AudioSource>();
		audio_source.volume = float.Parse(m_volumeField.text);
		audio_source.clip = AudioClip.Create("Generated Clip", (int)length_samples, (int)channels, (int)m_samplesPerSecond, false, on_audio_read, on_audio_set_position);
		audio_source.Play();

		uint[] keySequence = musicSequencer.keySequence;
		OSMD_update(int.Parse(m_tempoField.text), keySequence, keySequence.Length);
	}

	void on_audio_read(float[] data)
	{
		musicStreamSynthesizer.GetNext(data);
		// NOTE that we don't increment musicSequencer since musicStreamSynthesizer takes care of that
	}

	void on_audio_set_position(int new_position)
	{
		Debug.Log("new position: " + new_position);
//TODO		musicSequencer.SetTime(new_position);
	}
}
