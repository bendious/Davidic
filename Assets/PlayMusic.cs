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
	private static extern void OSMD_update();

	// Start is called before the first frame update
	void Start()
	{
	}

	// Update is called once per frame
	void Update()
	{
	}

	public uint m_samples_per_second = 44100U;
	public bool m_stereo = true;
	public uint maxPolyphony = 40U;

	public UnityEngine.UI.InputField m_pitch_min_field;
	public UnityEngine.UI.InputField m_pitch_max_field;
	public UnityEngine.UI.Dropdown m_rootNoteDropdown;
	public UnityEngine.UI.Dropdown m_scaleDropdown;
	public UnityEngine.UI.InputField m_volume_field;

	private StreamSynthesizer musicStreamSynthesizer;
	private MusicSequencer musicSequencer;

	public string bankFilePath = "GM Bank/gm";

	public void playMusic(bool isScale)
	{
		uint channels = (m_stereo ? 2U : 1U);

		musicStreamSynthesizer = new StreamSynthesizer((int)m_samples_per_second, (int)channels, 4096/*TODO?*/ / (int)channels, (int)maxPolyphony);
		musicStreamSynthesizer.LoadBank(bankFilePath);
		musicSequencer = new MusicSequencer(musicStreamSynthesizer, isScale, (uint)m_rootNoteDropdown.value, (uint)m_scaleDropdown.value);

		uint length_samples = musicSequencer.lengthSamples;

		AudioSource audio_source = GetComponent<AudioSource>();
		audio_source.volume = float.Parse(m_volume_field.text);
		audio_source.clip = AudioClip.Create("Generated Clip", (int)length_samples, (int)channels, (int)m_samples_per_second, false, on_audio_read, on_audio_set_position);
		audio_source.Play();

		OSMD_update();
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
