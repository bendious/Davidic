using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using System.IO;
#else
using System.Runtime.InteropServices;
#endif
using UnityEngine;
using UnityEngine.Assertions;
using CSharpSynth.Synthesis;


[RequireComponent(typeof(AudioSource))]
public class MusicPlayer : MonoBehaviour
{
#if UNITY_EDITOR
	private static void OSMD_update(uint bpm, uint[] chord_times, uint[] chord_keys, int chord_count, uint[] times, uint[] keys, uint[] lengths, int note_count)
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
		outputFile.WriteLine("\t\tvar chord_times = [" + string.Join(", ", chord_times) + "];");
		outputFile.WriteLine("\t\tvar chord_keys = [" + string.Join(", ", chord_keys) + "];");
		outputFile.WriteLine("\t\tvar chord_count = " + chord_count + ";");
		outputFile.WriteLine("\t\tvar times = [" + string.Join(", ", times) + "];");
		outputFile.WriteLine("\t\tvar keys = [" + string.Join(", ", keys) + "];");
		outputFile.WriteLine("\t\tvar lengths = [" + string.Join(", ", lengths) + "];");
		outputFile.WriteLine("\t\tvar note_count = " + note_count + ";");

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
	private static extern void OSMD_update(uint bpm, uint[] chord_times, uint[] chord_keys, int chord_count, uint[] times, uint[] keys, uint[] lengths, int note_count);
#endif

	public uint m_samplesPerSecond = 44100U;
	public bool m_stereo = true;
	public uint m_maxPolyphony = 40U;

	public UnityEngine.UI.InputField m_tempoField;
	public UnityEngine.UI.Dropdown m_rootNoteDropdown;
	public UnityEngine.UI.Dropdown m_scaleDropdown;
	public UnityEngine.UI.Dropdown m_instrumentDropdown;
	public UnityEngine.UI.InputField m_volumeField;

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
		m_instrumentDropdown.ClearOptions();
		List<CSharpSynth.Banks.Instrument> instruments = m_musicStreamSynthesizer.SoundBank.getInstruments(false);
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

	public void Generate(bool isScale)
	{
		uint bpm = uint.Parse(m_tempoField.text);
		m_musicSequencer = new MusicSequencer(m_musicStreamSynthesizer, isScale, (uint)m_rootNoteDropdown.value, (uint)m_scaleDropdown.value, (uint)m_instrumentDropdown.value, bpm);

		// update display
		uint[] chordProgressionTimes = m_musicSequencer.m_chordProgression.SelectMany(chord => Enumerable.Repeat((uint)System.Array.IndexOf(m_musicSequencer.m_chordProgression, chord), chord.Length)).ToArray();
		uint[] chordProgressionKeys = m_musicSequencer.m_chordProgression.SelectMany(chord => chord.Select(note => (uint)MusicUtility.TonesToSemitones(note, m_musicSequencer.m_scaleSemitones) + 60U)).ToArray();
		List<MusicBlock.NoteTimePair> noteTimeSequence = m_musicSequencer.NoteTimeSequence;
		uint[] timeSequence = noteTimeSequence.SelectMany(pair => Enumerable.Repeat(pair.m_time, (int)pair.m_note.KeyCount)).ToArray();
		uint[] keySequence = noteTimeSequence.SelectMany(pair => pair.m_note.MidiKeys(m_musicSequencer.m_rootKey, m_musicSequencer.m_scaleSemitones)).ToArray();
		uint[] lengthSequence = noteTimeSequence.SelectMany(pair => Enumerable.Repeat(pair.m_note.LengthSixtyFourths, (int)pair.m_note.KeyCount)).ToArray();
		Assert.AreEqual(chordProgressionTimes.Length, chordProgressionKeys.Length);
		Assert.AreEqual(timeSequence.Length, keySequence.Length);
		Assert.AreEqual(keySequence.Length, lengthSequence.Length);
		OSMD_update(bpm, chordProgressionTimes, chordProgressionKeys, chordProgressionKeys.Length, timeSequence, keySequence, lengthSequence, keySequence.Length);
	}

	public void Play()
	{
		if (m_musicSequencer == null)
		{
			return;
		}
		AudioSource audio_source = GetComponent<AudioSource>();
		audio_source.volume = float.Parse(m_volumeField.text);
		audio_source.clip = AudioClip.Create("Generated Clip", (int)m_musicSequencer.LengthSamples, (m_stereo ? 2 : 1), (int)m_samplesPerSecond, false, OnAudioRead, OnAudioSetPosition);
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
