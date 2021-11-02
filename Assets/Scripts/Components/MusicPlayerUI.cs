using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;


[RequireComponent(typeof(AudioSource))]
public class MusicPlayerUI : MonoBehaviour
{
	public uint m_samplesPerSecond = 44100U;
	public bool m_stereo = true;
	public uint m_maxPolyphony = 40U;

	public InputField m_tempoField;
	public Toggle m_scaleReuseToggle;
	public Dropdown m_rootNoteDropdown;
	public Dropdown m_scaleDropdown;
	public ScrollRect m_instrumentScrollview;
	public Toggle m_chordReuseToggle;
	public InputField m_chordField;
	public Toggle m_rhythmReuseToggle;
	public InputField m_rhythmField;
	public InputField[] m_noteLengthFields;
	public InputField m_harmonyCountField;
	public InputField m_instrumentCountField;
	public InputField m_volumeField;

	public string m_bankFilePath = "GM Bank/gm";


	private MusicPlayer m_player;


	public void Start()
	{
		m_player = new MusicPlayer();
		m_player.Start();

		// enumerate instruments in UI
		string[] instrumentNames = m_player.InstrumentNames();
		Assert.AreEqual(m_instrumentScrollview.content.childCount, 1);
		GameObject placeholderObj = m_instrumentScrollview.content.GetChild(0).gameObject;
		Assert.IsFalse(placeholderObj.activeSelf);
		Vector2 nextPos = placeholderObj.GetComponent<RectTransform>().anchoredPosition;
		float startY = nextPos.y;
		float lineHeight = Mathf.Abs(placeholderObj.GetComponent<RectTransform>().rect.y);
		for (int i = 0, n = instrumentNames.Length; i < n; ++i)
		{
			if (instrumentNames[i] == null)
			{
				continue;
			}
			string instrumentName = instrumentNames[i];

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
		// parse input
		float[][] chordList = m_chordField.text.Length == 0 ? new float[][] { } : m_chordField.text.Split(new char[] { ';' }).Select(str => str.Split(new char[] { ',' }).Select(str => float.Parse(str)).ToArray()).ToArray();
		uint[] rhythmLengths = m_rhythmField.text.Length == 0 ? new uint[] { } : m_rhythmField.text.Split(new char[] { ';' }).Select(str => uint.Parse(str.Split(new char[] { ',' })[0])).ToArray();
		float[] rhythmChords = m_rhythmField.text.Length == 0 ? new float[] { } : m_rhythmField.text.Split(new char[] { ';' }).Select(str => float.Parse(str.Split(new char[] { ',' })[1])).ToArray();

		// pass input through
		m_player.m_samplesPerSecond = m_samplesPerSecond;
		m_player.m_stereo = m_stereo;
		m_player.m_maxPolyphony = m_maxPolyphony;
		m_player.m_tempo = uint.Parse(m_tempoField.text);
		m_player.m_scaleReuse = m_scaleReuseToggle.isOn;
		m_player.m_rootNoteIndex = (uint)m_rootNoteDropdown.value;
		m_player.m_scaleIndex = (uint)m_scaleDropdown.value;
		m_player.m_instrumentToggles = m_instrumentScrollview.content.GetComponentsInChildren<Toggle>().Select(toggle => toggle.isOn).ToArray();
		m_player.m_chordReuse = m_chordReuseToggle.isOn;
		m_player.m_chords = new ChordProgression(chordList);
		m_player.m_rhythmReuse = m_rhythmReuseToggle.isOn;
		m_player.m_rhythm = new MusicRhythm(rhythmLengths, rhythmChords);
		m_player.m_noteLengthWeights = m_noteLengthFields.Select(field => float.Parse(field.text)).ToArray();
		m_player.m_harmonyCount = uint.Parse(m_harmonyCountField.text);
		m_player.m_instrumentCount = uint.Parse(m_instrumentCountField.text);
		m_player.m_volume = float.Parse(m_volumeField.text);
		m_player.m_bankFilePath = m_bankFilePath;

		// generate
		m_player.Generate(isScale);

		// update display
		m_rootNoteDropdown.value = (int)m_player.m_rootNoteIndex;
		m_rootNoteDropdown.RefreshShownValue();
		m_scaleDropdown.value = (int)m_player.m_scaleIndex;
		m_scaleDropdown.RefreshShownValue();
		m_chordField.text = m_player.m_chords.m_progression.Aggregate("", (str, chord) => str + (str == "" ? "" : ";") + chord.Aggregate("", (str, idx) => str + (str == "" ? "" : ",") + idx));
		m_rhythmField.text = m_player.m_rhythm.m_lengthsSixtyFourths.Zip(m_player.m_rhythm.m_chordIndices, (a, b) => a + "," + b).Aggregate((a, b) => a + ";" + b);
		MusicDisplay.Start();
		m_player.Display("osmd-chords", "osmd-rhythm", "osmd-main");
		MusicDisplay.Finish();
	}

	public void Play()
	{
		m_player.Play(GetComponent<AudioSource>());
	}

	public string Export(string filepath)
	{
		return m_player.Export(filepath);
    }
}
