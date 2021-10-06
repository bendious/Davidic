using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using System.IO;
#endif
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Assertions;
using CSharpSynth.Effects;
using CSharpSynth.Sequencer;
using CSharpSynth.Synthesis;
using CSharpSynth.Midi;

[RequireComponent(typeof(AudioSource))]
public class PlayMusic : MonoBehaviour
{
#if UNITY_EDITOR
	private static void OSMD_update(uint bpm, uint[] keys, uint[] lengths, int key_count)
	{
		// copy HTML/JS header from template file
		const string output_filename = "debugOutput.html";
		File.Copy("debugInput.html", output_filename, true);
		StreamWriter outputFile = new StreamWriter(output_filename, true);

		// add array retrieval helper
		outputFile.WriteLine("\t\tvar inputArrayUint = function(array, index) {");
		outputFile.WriteLine("\t\t\treturn array[index];");
		outputFile.WriteLine("\t\t};");

		// add "params"
		outputFile.WriteLine("\t\tvar bpm = " + bpm + ";");
		outputFile.WriteLine("\t\tvar keys = [" + string.Join(", ", keys) + "];");
		outputFile.WriteLine("\t\tvar lengths = [" + string.Join(", ", lengths) + "];");
		outputFile.WriteLine("\t\tvar key_count = " + key_count + ";");

		// copy bridge code
		StreamReader inputFile = new StreamReader("Assets/Plugins/OSMD_bridge/osmd_bridge.jslib");
		const uint lineSkipCount = 6U; // TODO: dynamically determine number of skipped lines?
		for (uint i = 0U; i < lineSkipCount; ++i)
		{
			inputFile.ReadLine();
		}
		while (true)
		{
			string inLine = inputFile.ReadLine();
			if (inLine == null || inLine == "});" || inLine == "\t},") // TODO: skip a certain number of lines at the end rather than hardcoded values?
			{
				break;
			}
			outputFile.WriteLine(inLine);
		}
		inputFile.Close();

		// add HTML footer
		outputFile.WriteLine("\t\t</script>");
		outputFile.WriteLine("\t</body>");
		outputFile.WriteLine("</html>");
		outputFile.Close();
	}
#else
	// see Plugins/OSMD_bridge/osmd_bridge.jslib
	[DllImport("__Internal")]
	private static extern void OSMD_update(uint bpm, uint[] keys, uint[] lengths, int key_count);
#endif

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
		uint bpm = uint.Parse(m_tempoField.text);

		musicSequencer = new MusicSequencer(musicStreamSynthesizer, isScale, uint.Parse(m_keyMinField.text), uint.Parse(m_keyMaxField.text), (uint)m_rootNoteDropdown.value, (uint)m_scaleDropdown.value, (uint)m_instrumentDropdown.value, bpm);

		uint length_samples = musicSequencer.lengthSamples;

		AudioSource audio_source = GetComponent<AudioSource>();
		audio_source.volume = float.Parse(m_volumeField.text);
		audio_source.clip = AudioClip.Create("Generated Clip", (int)length_samples, (int)channels, (int)m_samplesPerSecond, false, on_audio_read, on_audio_set_position);
		audio_source.Play();

		uint[] keySequence = musicSequencer.keySequence;
		uint[] lengthSequence = musicSequencer.lengthSequence;
		Assert.AreEqual(keySequence.Length, lengthSequence.Length);
		OSMD_update(bpm, keySequence, lengthSequence, lengthSequence.Length);
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
