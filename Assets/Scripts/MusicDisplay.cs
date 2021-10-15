using System.IO;
#if !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif


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
		outputFile.WriteLine("\t\tvar inputArrayUint = function(array, index) {");
		outputFile.WriteLine("\t\t\treturn array[index];");
		outputFile.WriteLine("\t\t};");
		outputFile.Close();
#endif
	}

#if UNITY_EDITOR
	public static void Update(string element_id, string title, int key_fifths, string key_mode, int note_count, uint[] times, uint[] keys, uint[] lengths, uint bpm)
	{
		StreamWriter outputFile = new StreamWriter(m_debugOutputFile, true);

		// add "params"
		outputFile.WriteLine("\t\tvar element_id = '" + element_id + "';");
		outputFile.WriteLine("\t\tvar title = '" + title + "';");
		outputFile.WriteLine("\t\tvar key_fifths = " + key_fifths + ";");
		outputFile.WriteLine("\t\tvar key_mode = '" + key_mode + "';");
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
	public static extern void Update(string element_id, string title, int key_fifths, string key_mode, int note_count, uint[] times, uint[] keys, uint[] lengths, uint bpm);
#endif

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
}
