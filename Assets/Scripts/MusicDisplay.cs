using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
using UnityEngine;
using UnityEngine.Assertions;


public static class MusicDisplay
{
#if UNITY_EDITOR
	private const string m_debugOutputFile = "debugOutput.html";
#endif


	public static void Start()
	{
#if UNITY_EDITOR
		// copy HTML/JS header from template file
		File.Copy("debugInput.html", m_debugOutputFile, true);
		StreamWriter outputFile = new StreamWriter(m_debugOutputFile, true);

		// add array/string retrieval helpers
		outputFile.WriteLine("\t\tvar Pointer_stringify = function(str) {");
		outputFile.WriteLine("\t\t\treturn str;");
		outputFile.WriteLine("\t\t};");
		outputFile.Close();
#endif
	}

	public static void Update(string elementId, string title, string[] instrumentNames, MusicScale scale, uint rootKey, uint bpm, MusicNote[] notes, uint[] times)
	{
		string[] instrumentNamesSafe = (instrumentNames is null) ? new string[] { "" } : instrumentNames;
		string xmlStr = ToXml(title, instrumentNamesSafe, scale, rootKey, bpm, notes, times,
#if UNITY_EDITOR
			"\\n" // NOTE the escaped newlines since the string will be passed to Javascript manually
#else
			"\n"
#endif
			);

		UpdateInternal(elementId, bpm == 0 ? "compacttight" : "compact", xmlStr);
	}

	public static void Finish()
	{
#if UNITY_EDITOR
		StreamWriter outputFile = new StreamWriter(m_debugOutputFile, true);

		// add HTML footer
		outputFile.WriteLine("\t\t</script>");
		outputFile.WriteLine("\t</body>");
		outputFile.WriteLine("</html>");
		outputFile.Close();
#endif
	}

	public static string Export(string filepath, string title, string[] instrumentNames, MusicScale scale, uint rootKey, uint bpm, MusicNote[] notes, uint[] times)
	{
		string[] instrumentNamesSafe = (instrumentNames is null) ? new string[] { "" } : instrumentNames;
		string xmlStr = ToXml(title, instrumentNamesSafe, scale, rootKey, bpm, notes, times, "\n");

		if (!string.IsNullOrEmpty(filepath))
		{
			File.WriteAllText(filepath, xmlStr);
		}
		return xmlStr;
	}


	static readonly Dictionary<uint, ValueTuple<string, string>> NoteTypesByLength = new Dictionary<uint, ValueTuple<string, string>>();

	private class MusicNoteInfo
	{
		public uint m_time;
		public uint m_length;
		public HashSet<uint> m_keys;
		public List<string> m_ties;
	}

	private static string ToXml(string title, string[] instrumentNames, MusicScale scale, uint rootKey, uint bpm, MusicNote[] notes, uint[] times, string newline)
	{
		Assert.IsTrue(notes.Length > 0);
		Assert.AreEqual(notes.Length, times.Length);

		// constants
		if (NoteTypesByLength.Count == 0) // TODO: move into constructor once Unity supports the newer C# API
		{
			NoteTypesByLength.Add(1, new ValueTuple<string, string>("64th", ""));
			NoteTypesByLength.Add(2, new ValueTuple<string, string>("32nd", ""));
			NoteTypesByLength.Add(3, new ValueTuple<string, string>("32nd", "<dot/>"));
			NoteTypesByLength.Add(4, new ValueTuple<string, string>("16th", ""));
			NoteTypesByLength.Add(6, new ValueTuple<string, string>("16th", "<dot/>"));
			NoteTypesByLength.Add(8, new ValueTuple<string, string>("eighth", ""));
			NoteTypesByLength.Add(12, new ValueTuple<string, string>("eighth", "<dot/>"));
			NoteTypesByLength.Add(16, new ValueTuple<string, string>("quarter", ""));
			NoteTypesByLength.Add(24, new ValueTuple<string, string>("quarter", "<dot/>"));
			NoteTypesByLength.Add(32, new ValueTuple<string, string>("half", ""));
			NoteTypesByLength.Add(48, new ValueTuple<string, string>("half", "<dot/>"));
			NoteTypesByLength.Add(64, new ValueTuple<string, string>("whole", ""));
			NoteTypesByLength.Add(96, new ValueTuple<string, string>("whole", "<dot/>"));
		}
		const uint sixtyFourthsBeamMax = 8U;

		// collect/compress info from notes
		List<List<MusicNoteInfo>> noteObjects = new List<List<MusicNoteInfo>>();
		uint timeIdx = 0U;
		foreach (MusicNote note in notes)
		{
			while (note.m_channel >= noteObjects.Count)
			{
				noteObjects.Add(new List<MusicNoteInfo>());
			}
			List<MusicNoteInfo> noteList = noteObjects[(int)note.m_channel];
			uint timeCur = times[timeIdx];
			if (noteList.Count > 0U && noteList.Last().m_time == timeCur && noteList.Last().m_length == note.LengthSixtyFourths)
			{
				noteList.Last().m_keys.UnionWith(note.MidiKeys(rootKey, scale));
			}
			else
			{
				noteList.Add(new MusicNoteInfo
				{
					m_time = timeCur,
					m_length = note.LengthSixtyFourths,
					m_keys = new HashSet<uint>(note.MidiKeys(rootKey, scale)),
					m_ties = new List<string>(),
				});
			}
			++timeIdx;
		}

		// split notes crossing a measure boundary or of uneven length
		bool isUntimed = bpm == 0;
		uint sixtyFourthsPerMeasure = isUntimed ? uint.MaxValue : MusicUtility.sixtyFourthsPerMeasure;
		foreach (List<MusicNoteInfo> noteList in noteObjects)
		{
			for (int listIdx = 0; listIdx < noteList.Count; ++listIdx)
			{
				MusicNoteInfo noteObj = noteList[listIdx];
				uint timeValNext = noteObj.m_time + noteObj.m_length;
				uint lengthPost = timeValNext % sixtyFourthsPerMeasure;

				bool withinMeasure = lengthPost <= 0 || lengthPost >= noteObj.m_length;
				if (withinMeasure && (noteObj.m_length <= 0 || NoteTypesByLength.ContainsKey(noteObj.m_length)))
				{
					// don't need to split this note
					continue;
				}

				if (withinMeasure)
				{
					// get longest standard/dotted length less than the current length
					uint lengthItr;
					for (lengthItr = noteObj.m_length; lengthItr > 0 && !NoteTypesByLength.ContainsKey(lengthItr); --lengthItr) ; // TODO: efficiency?
					lengthPost = lengthItr;
				}

				// shorten and tie existing note
				noteObj.m_length -= lengthPost;
				timeValNext -= lengthPost;
				noteObj.m_ties.Add("start");

				// add new tied note (after any harmony notes that come before it)
				int idxNew;
				for (idxNew = listIdx + 1; idxNew < noteList.Count && noteList[idxNew].m_time < timeValNext; ++idxNew) ;
				MusicNoteInfo noteNew = new MusicNoteInfo { m_time = timeValNext, m_length = lengthPost, m_keys = noteObj.m_keys, m_ties = new List<string> { "stop" } };
				noteList.Insert(idxNew, noteNew);

				// re-process the shortened note next iteration in case it is now an odd length
				--listIdx;
			}
		}

		// MusicXML header
		// TODO: use XmlWriter for cleaner generation?
		string xmlStr = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + newline + "<!DOCTYPE score-partwise PUBLIC \"-//Recordare//DTD MusicXML 2.0 Partwise//EN\"" + newline + "\t\"http://www.musicxml.org/dtds/partwise.dtd\">" + newline + "<score-partwise version=\"2.0\">" + newline + "\t<part-list>";
		for (int channelIdx = 0; channelIdx < instrumentNames.Length; ++channelIdx)
		{
			string instrumentNameStr = instrumentNames[channelIdx];
			xmlStr += newline + "\t\t<score-part id=\"" + instrumentNameStr + "\">" + newline + "\t\t\t<part-name>" + instrumentNameStr + "</part-name>" + newline + "\t\t\t<midi-instrument id=\"" + instrumentNameStr + "I1\">" + newline + "\t\t\t\t<midi-channel>" + channelIdx + "</midi-channel>" + newline + "\t\t\t\t<midi-program>0</midi-program>" + newline + "\t\t\t</midi-instrument>" + newline + "\t\t</score-part>";
		}
		xmlStr += newline + "\t</part-list>";

		string perNoteStr = isUntimed ? "\t\t\t\t<notehead>x</notehead>" + newline : "";

		for (int channelIdx = 0; channelIdx < instrumentNames.Length; ++channelIdx)
		{
			List<MusicNoteInfo> noteList = noteObjects[channelIdx];

			string instrumentNameStr = instrumentNames[channelIdx];
			xmlStr += newline + "\t<part id=\"" + instrumentNameStr + "\">"
				+ (isUntimed ? newline + "\t\t<measure>" + newline + "\t\t\t<attributes>" + newline + "\t\t\t\t<key print-object=\"no\"></key>" + newline + "\t\t\t\t<time print-object=\"no\"></time>" + newline + "\t\t\t\t<clef>" + newline + "\t\t\t\t\t<sign>percussion</sign>" + newline + "\t\t\t\t\t<staff-lines>5</staff-lines>" + newline + "\t\t\t\t</clef>" + newline + "\t\t\t</attributes>" + newline + "\t\t\t<direction placement=\"above\">" + newline + "\t\t\t\t<direction-type>" + newline + "\t\t\t\t\t<words>" + title + "</words>" + newline + "\t\t\t\t</direction-type>" + newline + "\t\t\t</direction>"
				: newline + "\t\t<measure>" + newline + "\t\t\t<attributes>" + newline + "\t\t\t\t<key>" + newline + "\t\t\t\t\t<fifths>" + scale.m_fifths + "</fifths>" + newline + "\t\t\t\t\t<mode>" + scale.m_mode + "</mode>" + newline + "\t\t\t\t</key>" + newline + "\t\t\t\t<time>" + newline + "\t\t\t\t\t<beats>4</beats>" + newline + "\t\t\t\t\t<beat-type>4</beat-type>" + newline + "\t\t\t\t</time>" + newline + "\t\t\t\t<clef number=\"1\">" + newline + "\t\t\t\t\t<sign>G</sign>" + newline + "\t\t\t\t\t<line>2</line>" + newline + "\t\t\t\t</clef>" + newline + "\t\t\t\t<divisions>16</divisions>" + newline + "\t\t\t</attributes>" + newline + "\t\t\t<direction placement=\"above\">" + newline + "\t\t\t\t<direction-type>" + newline + "\t\t\t\t\t<metronome>" + newline + "\t\t\t\t\t\t<beat-unit>quarter</beat-unit>" + newline + "\t\t\t\t\t\t<per-minute>" + bpm + "</per-minute>" + newline + "\t\t\t\t\t</metronome>" + newline + "\t\t\t\t</direction-type>" + newline + "\t\t\t\t<sound tempo=\"" + bpm + "\"/>" + newline + "\t\t\t</direction>"); // TODO: base divisions on time signature?

			// accumulators / inter-note memory
			int timeValPrev = -1;
			int lengthValPrev = -1;
			int overlapAmount = 0; // TODO: support more than two overlapping voices?

			// per-note
			for (int noteIdx = 0; noteIdx < noteList.Count; ++noteIdx)
			{
				MusicNoteInfo noteObj = noteList[noteIdx];

				int notePrevEnd = Math.Max(0, timeValPrev + lengthValPrev);
				if (noteObj.m_time < notePrevEnd)
				{
					// overlap w/ previous note(s)
					overlapAmount = (int)(notePrevEnd - noteObj.m_time);
					xmlStr += newline + "\t\t\t<backup>" + newline + "\t\t\t\t<duration>" + overlapAmount + "</duration>" + newline + "\t\t\t</backup>";
				}
				else if (noteObj.m_time > notePrevEnd)
				{
					// add rest
					// TODO: handle non-standard lengths
					ValueTuple<string, string> typeAndDotStr = NoteTypesByLength.ContainsKey(noteObj.m_length) ? NoteTypesByLength[noteObj.m_length] : new ValueTuple<string, string>("ERROR", "");
					string typeStr = typeAndDotStr.Item1;
					string dotStr = typeAndDotStr.Item2;
					xmlStr += newline + "\t\t\t<note>" + newline + "\t\t\t\t<rest/>" + newline + "\t\t\t\t<duration>" + (noteObj.m_time - notePrevEnd) + "</duration>" + newline + "\t\t\t\t<voice>1</voice>" + newline + "\t\t\t\t<type>" + typeStr + "</type>" + dotStr + newline + "\t\t\t\t</note>";
				}

				// add barline if appropriate
				bool newMeasure = overlapAmount <= 0 && noteObj.m_time > 0 && noteObj.m_time % sixtyFourthsPerMeasure == 0;
				if (newMeasure)
				{
					xmlStr += newline + "\t\t</measure>" + newline + "\t\t<measure>";
				}

				int keyIdx = 0;
				foreach (uint keyVal in noteObj.m_keys.Distinct()) // TODO: fix up HashSet usage to not require Distinct() call?
				{
					// per-note tags
					string pitchTag = isUntimed ? "unpitched" : "pitch";
					string pitchPrefix = isUntimed ? "display-" : "";
					uint noteSemitonesFromC = keyVal % MusicUtility.semitonesPerOctave;
					int noteVal = Array.IndexOf(MusicUtility.majorScale.m_semitones, noteSemitonesFromC);
					int semitoneOffset = 0;
					if (noteVal == -1)
					{
						semitoneOffset = (scale.m_fifths < 0) ? -1 : 1;
						noteVal = Array.IndexOf(MusicUtility.majorScale.m_semitones, (uint)(noteSemitonesFromC - semitoneOffset));
					}
					char noteLetter = (char)((noteVal + 2) % MusicUtility.tonesPerOctave + 'A');
					int noteOctave = Mathf.FloorToInt(keyVal / MusicUtility.semitonesPerOctave) - 1; // NOTE offset: middle-C (MIDI key 60) in MusicXML is the start of octave 4 rather than 5
					ValueTuple<string, string> typeAndDotStr = NoteTypesByLength.ContainsKey(noteObj.m_length) ? NoteTypesByLength[noteObj.m_length] : new ValueTuple<string, string>("ERROR", "");
					string typeStr = typeAndDotStr.Item1;
					string dotStr = typeAndDotStr.Item2;
					bool beamBefore = !newMeasure && noteIdx > 0 && noteObj.m_length <= sixtyFourthsBeamMax && lengthValPrev <= sixtyFourthsBeamMax;
					bool beamAfter = (noteIdx + 1 < noteList.Count && noteObj.m_length <= sixtyFourthsBeamMax && noteList[noteIdx + 1].m_length <= sixtyFourthsBeamMax); // TODO: detect measure end?
					string beamStr = (beamBefore && beamAfter) ? "continue" : (beamBefore ? "end" : (beamAfter ? "begin" : ""));
					string accidentalStr = (semitoneOffset > 0 ? "sharp" : semitoneOffset < 0 ? "flat" : "");

					// add to XML
					xmlStr += newline + "\t\t\t<note>" + newline + "\t\t\t\t" + (keyIdx > 0 ? "<chord/>" : "") +
					"<" + pitchTag + ">" + newline + "\t\t\t\t\t<" + pitchPrefix + "step>" + noteLetter + "</" + pitchPrefix + "step>" + newline + "\t\t\t\t\t<alter>" + semitoneOffset + "</alter>" + newline + "\t\t\t\t\t<" + pitchPrefix + "octave>" + noteOctave + "</" + pitchPrefix + "octave>" + newline + "\t\t\t\t</" + pitchTag + ">" + newline + "\t\t\t\t<duration>" + noteObj.m_length + "</duration>" + newline + "\t\t\t\t<voice>" + (overlapAmount > 0 ? 2 : 1) + "</voice>" + newline + "\t\t\t\t<type>" + typeStr + "</type>" + dotStr + newline
					+ (beamStr == "" ? "" : "\t\t\t\t<beam number=\"1\">" + beamStr + "</beam>" + newline)
					+ (accidentalStr == "" ? "" : "\t\t\t\t<accidental>" + accidentalStr + "</accidental>" + newline)
					+ perNoteStr;
					if (noteObj.m_ties.Count > 0)
					{
						xmlStr += "\t\t\t\t<notations>" + newline;
						foreach (string tieTypeStr in noteObj.m_ties)
						{
							xmlStr += "\t\t\t\t\t<tied type=\"" + tieTypeStr + "\"/>" + newline;
						}
						xmlStr += "\t\t\t\t</notations>" + newline;
					}
					xmlStr += "\t\t\t</note>";
					// TODO: <{p/mp/mf/f}/>?

					++keyIdx;
				}

				timeValPrev = (int)noteObj.m_time;
				lengthValPrev = (int)noteObj.m_length;
				overlapAmount -= lengthValPrev;
			}

			if (!isUntimed)
			{
				xmlStr += newline + "\t\t\t<barline location=\"right\">" + newline + "\t\t\t\t<bar-style>light-heavy</bar-style>" + newline + "\t\t\t</barline>";
			}
			xmlStr += newline + "\t\t</measure>" + newline + "\t</part>";
		}

		// footer
		xmlStr += newline + "</score-partwise>";

		return xmlStr;
	}

#if UNITY_EDITOR
	private static void UpdateInternal(string element_id, string osmd_params, string xml_str)
	{
		StreamWriter outputFile = new StreamWriter(m_debugOutputFile, true);

		// add "params"
		outputFile.WriteLine("\t\tvar element_id = '" + element_id + "';");
		outputFile.WriteLine("\t\tvar osmd_params = '" + osmd_params + "';");
		outputFile.WriteLine("\t\tvar xml_str = '" + xml_str + "';");

		// copy bridge code
		StreamReader inputFile = new StreamReader("Assets/Plugins/OSMD_bridge/osmd_bridge.jslib");
		const uint lineSkipCount = 2U; // TODO: dynamically determine number of skipped lines?
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
	private static extern void UpdateInternal(string element_id, string osmd_params, string xml_str);
#endif
}
