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
		const string output_filename = "debugOutput.html";
		File.Copy("debugInput.html", output_filename, true);
		StreamWriter outputFile = new StreamWriter(output_filename, true);

		outputFile.WriteLine("var bpm = " + bpm + ";");
		outputFile.WriteLine("var keys = [" + string.Join(", ", keys) + "];");
		outputFile.WriteLine("var lengths = [" + string.Join(", ", lengths) + "];");
		outputFile.WriteLine("var key_count = " + key_count + ";");

		/* create OSMD instance if first call */
		outputFile.WriteLine("if (document.osmd == null) {");
		outputFile.WriteLine("	document.osmd = new opensheetmusicdisplay.OpenSheetMusicDisplay(\"osmd\", { drawingParameters: \"compacttight\" });");
		outputFile.WriteLine("}");

		outputFile.WriteLine("/* MusicXML header */");
		outputFile.WriteLine("/* TODO: use timewise rather than partwise? */");
		outputFile.WriteLine("var xml_str = '<?xml version=\"1.0\" encoding=\"UTF-8\"?>\\");
		outputFile.WriteLine("	<!DOCTYPE score-partwise PUBLIC \"-//Recordare//DTD MusicXML 2.0 Partwise//EN\"\\");
		outputFile.WriteLine("	  \"http://www.musicxml.org/dtds/partwise.dtd\">\\");
		outputFile.WriteLine("	<score-partwise version=\"2.0\">\\");
		outputFile.WriteLine("	  <part-list>\\");
		outputFile.WriteLine("		<score-part id=\"P1\">\\");
		outputFile.WriteLine("		  <midi-instrument id=\"P1I1\">\\");
		outputFile.WriteLine("			<midi-channel>0</midi-channel>\\");
		outputFile.WriteLine("			<midi-program>0</midi-program>\\");
		outputFile.WriteLine("		  </midi-instrument>\\");
		outputFile.WriteLine("		</score-part>\\");
		outputFile.WriteLine("	  </part-list>\\");
		outputFile.WriteLine("	  <part id=\"P1\">\\");
		outputFile.WriteLine("		<measure>\\");
		outputFile.WriteLine("		  <attributes>\\");
		outputFile.WriteLine("			<key>\\");
		outputFile.WriteLine("			  <fifths>0</fifths>\\");
		outputFile.WriteLine("			  <mode>major</mode>\\");
		outputFile.WriteLine("			</key>\\");
		outputFile.WriteLine("			<time>\\");
		outputFile.WriteLine("			  <beats>4</beats>\\");
		outputFile.WriteLine("			  <beat-type>4</beat-type>\\");
		outputFile.WriteLine("			</time>\\");
		outputFile.WriteLine("			<clef number=\"1\">\\");
		outputFile.WriteLine("			  <sign>G</sign>\\");
		outputFile.WriteLine("			  <line>2</line>\\");
		outputFile.WriteLine("			</clef>\\");
		outputFile.WriteLine("		  </attributes>\\");
		outputFile.WriteLine("		  <direction placement=\"above\">\\");
		outputFile.WriteLine("			<direction-type>\\");
		outputFile.WriteLine("			  <metronome>\\");
		outputFile.WriteLine("				<beat-unit>quarter</beat-unit>\\");
		outputFile.WriteLine("				<per-minute>' + bpm + '</per-minute>\\");
		outputFile.WriteLine("			  </metronome>\\");
		outputFile.WriteLine("			</direction-type>\\");
		outputFile.WriteLine("			<sound tempo=\"' + bpm + '\"/>\\");
		outputFile.WriteLine("		  </direction>';");

		outputFile.WriteLine("/* convert key array into MusicXML */");
		outputFile.WriteLine("const length_per_measure = 64; /* TODO: account for different time signatures */");
		outputFile.WriteLine("var length_total = 0;");
		outputFile.WriteLine("var length_val_prev = 0;");
		outputFile.WriteLine("var type_str = '';");
		outputFile.WriteLine("for (var i = 0; i < key_count; ++i) {");
		outputFile.WriteLine("	var key_val = keys[i]; /* see https://docs.unity3d.com/Manual/webgl-interactingwithbrowserscripting.html */");
		outputFile.WriteLine("	var length_val = lengths[i];");
		outputFile.WriteLine("	console.log(\"key \" + key_val + \", length \" + length_val); /* TEMP? */");

		outputFile.WriteLine("	/* measure bar if appropriate */");
		outputFile.WriteLine("	if (length_total > 0 && length_val > 0 && length_total % length_per_measure == 0) { /* TODO: handle notes crossing measures? */");
		outputFile.WriteLine("		xml_str += '\\");
		outputFile.WriteLine("		  </measure>\\");
		outputFile.WriteLine("		  <measure>';");
		outputFile.WriteLine("	}");

		outputFile.WriteLine("	/* note */");
		outputFile.WriteLine("	const semitones_from_c = [ 0, 2, 4, 5, 7, 9, 11 ];");
		outputFile.WriteLine("	const semitones_per_octave = 12;");
		outputFile.WriteLine("	const keys_per_octave = 7;");
		outputFile.WriteLine("	console.assert(semitones_from_c.length == keys_per_octave);");
		outputFile.WriteLine("	var note_semitones_from_c = key_val % semitones_per_octave;");
		outputFile.WriteLine("	var note_val = semitones_from_c.indexOf(note_semitones_from_c);");
		outputFile.WriteLine("	var semitone_offset = 0;");
		outputFile.WriteLine("	if (note_val == -1) {");
		outputFile.WriteLine("		semitone_offset = 1; /* TODO: pick between sharp/flat based on major/minor key */");
		outputFile.WriteLine("		note_val = semitones_from_c.indexOf(note_semitones_from_c - semitone_offset);");
		outputFile.WriteLine("	}");
		outputFile.WriteLine("	var note_letter = String.fromCharCode(((note_val + 2) % keys_per_octave) + 'A'.charCodeAt(0)); /* see https://stackoverflow.com/questions/36129721/convert-number-to-alphabet-letter */");
		outputFile.WriteLine("	var note_octave = key_val / semitones_per_octave - 1; /* NOTE offset: middle-C (MIDI key 60) in MusicXML is the start of octave 4 rather than 5 */");
		outputFile.WriteLine("	type_str = (length_val == 1 ? '64th' : length_val == 2 ? '32nd' : length_val == 4 ? '16th' : length_val == 8 ? 'eighth' : length_val == 16 ? 'quarter' : length_val == 32 ? 'half' : length_val == 64 ? 'whole' : type_str); /* note that length_val of 0 is used for subsequent chord notes, in which case we just reuse the previous type */");
		outputFile.WriteLine("	xml_str += '\\");
		outputFile.WriteLine("		  <note>\\");
		outputFile.WriteLine("			' + (length_val == 0 ? '<chord/>' : '') + '\\");
		outputFile.WriteLine("			<pitch>\\");
		outputFile.WriteLine("			  <step>' + note_letter + '</step>\\");
		outputFile.WriteLine("			  <alter>' + semitone_offset + '</alter>\\");
		outputFile.WriteLine("			  <octave>' + note_octave + '</octave>\\");
		outputFile.WriteLine("			</pitch>\\");
		outputFile.WriteLine("			<duration>' + (length_val == 0 ? length_val_prev : length_val) + '</duration>\\");
		outputFile.WriteLine("			<voice>1</voice>\\");
		outputFile.WriteLine("			<type>' + type_str + '</type>\\");
		outputFile.WriteLine("			<accidental>' + (semitone_offset > 0 ? 'sharp' : semitone_offset < 0 ? 'flat' : '')/*TODO: account for key signature*/ + '</accidental>\\");
		outputFile.WriteLine("		  </note>';");
		outputFile.WriteLine("	/* TODO: <beam>/<dot/>/<{p/mp/mf/f}/> */");

		outputFile.WriteLine("	length_total += length_val;");
		outputFile.WriteLine("	length_val_prev = length_val;");
		outputFile.WriteLine("}");

		outputFile.WriteLine("/* MusicXML footer */");
		outputFile.WriteLine("xml_str += '\\");
		outputFile.WriteLine("		  <barline location=\"right\">\\");
		outputFile.WriteLine("			<bar-style>light-heavy</bar-style>\\");
		outputFile.WriteLine("		  </barline>\\");
		outputFile.WriteLine("		</measure>\\");
		outputFile.WriteLine("	  </part>\\");
		outputFile.WriteLine("	</score-partwise>';");

		outputFile.WriteLine("/* TEMP? */");
		outputFile.WriteLine("console.log(xml_str);");

		outputFile.WriteLine("/* load and render */");
		outputFile.WriteLine("document.osmd.load(xml_str).then(function() {");
		outputFile.WriteLine("	document.osmd.render();");
		outputFile.WriteLine("});");

		outputFile.WriteLine("</script>");
		outputFile.WriteLine("</body>");
		outputFile.WriteLine("</html>");
		outputFile.Flush();
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
