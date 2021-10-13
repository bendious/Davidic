using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using System.IO;
#else
using System.Runtime.InteropServices;
#endif
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using CSharpSynth.Synthesis;


[RequireComponent(typeof(AudioSource))]
public class MusicPlayer : MonoBehaviour
{
#if UNITY_EDITOR
	private const string m_outputFilename = "debugOutput.html";
#endif

	private static void OSMD_start()
	{
#if UNITY_EDITOR
		// copy HTML/JS header from template file
		File.Copy("debugInput.html", m_outputFilename, true);
		StreamWriter outputFile = new StreamWriter(m_outputFilename, true);

		// add array/string retrieval helpers
		outputFile.WriteLine("\t\tvar Pointer_stringify = function(str) {");
		outputFile.WriteLine("\t\t\treturn str;");
		outputFile.WriteLine("\t\t};");
		outputFile.WriteLine("\t\tvar inputArrayUint = function(array, index) {");
		outputFile.WriteLine("\t\t\treturn array[index];");
		outputFile.WriteLine("\t\t};");
		outputFile.Close();
#endif
	}

#if UNITY_EDITOR
	private static void OSMD_update(string element_id, int note_count, uint[] times, uint[] keys, uint[] lengths, uint bpm)
	{
		StreamWriter outputFile = new StreamWriter(m_outputFilename, true);

		// add "params"
		outputFile.WriteLine("\t\tvar element_id = '" + element_id + "';");
		outputFile.WriteLine("\t\tvar note_count = " + note_count + ";");
		outputFile.WriteLine("\t\tvar times = [" + string.Join(", ", times) + "];");
		outputFile.WriteLine("\t\tvar keys = [" + string.Join(", ", keys) + "];");
		outputFile.WriteLine("\t\tvar lengths = [" + string.Join(", ", lengths) + "];");
		outputFile.WriteLine("\t\tvar bpm = " + bpm + ";");

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
		outputFile.Close();
	}
#else
	// see Plugins/OSMD_bridge/osmd_bridge.jslib
	[DllImport("__Internal")]
	private static extern void OSMD_update(string element_id, int note_count, uint[] times, uint[] keys, uint[] lengths, uint bpm);
#endif

	private static void OSMD_finish()
	{
#if UNITY_EDITOR
		StreamWriter outputFile = new StreamWriter(m_outputFilename, true);

		// add HTML footer
		outputFile.WriteLine("\t\t</script>");
		outputFile.WriteLine("\t</body>");
		outputFile.WriteLine("</html>");
		outputFile.Close();
#endif
	}

	public uint m_samplesPerSecond = 44100U;
	public bool m_stereo = true;
	public uint m_maxPolyphony = 40U;

	public InputField m_tempoField;
	public Dropdown m_rootNoteDropdown;
	public Dropdown m_scaleDropdown;
	public ScrollRect m_instrumentScrollview;
	public Text m_instrumentText;
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
		m_instrumentText.text = "Current: " + m_musicStreamSynthesizer.SoundBank.getInstrument(chosenInstrumentIdx, false/*?*/).Name;

		uint bpm = uint.Parse(m_tempoField.text);
		m_musicSequencer = new MusicSequencer(m_musicStreamSynthesizer, isScale, (uint)m_rootNoteDropdown.value, (uint)m_scaleDropdown.value, (uint)chosenInstrumentIdx, bpm);

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
		OSMD_start();
		OSMD_update("osmd-chords", chordProgressionKeys.Length, chordProgressionTimes, chordProgressionKeys, Enumerable.Repeat(16U, chordProgressionKeys.Length).ToArray(), 0U);
		OSMD_update("osmd-main", keySequence.Length, timeSequence, keySequence, lengthSequence, bpm);
		OSMD_finish();
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
