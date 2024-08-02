using SFB;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Text;
using System.Runtime.InteropServices;
#endif
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


[RequireComponent(typeof(Button))]
public class WebSaveHelper : MonoBehaviour, IPointerDownHandler
{
	public MusicPlayerUI m_musicPlayer;
	[SerializeField] private bool m_isMidi = false;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void DownloadFile(string gameObjectName, string methodName, string filename, byte[] byteArray, int byteArraySize);
#endif

    // Browser plugin should be called in OnPointerDown.
    public void OnPointerDown(PointerEventData eventData)
	{
#if UNITY_WEBGL && !UNITY_EDITOR
		byte[] bytes = m_musicPlayer.Export(null, m_isMidi);
		DownloadFile(gameObject.name, "OnFileDownload", "DavidicOutput" + (m_isMidi ? ".midi" : ".xml"), bytes, bytes.Length);
#else
		var path = StandaloneFileBrowser.SaveFilePanel(m_isMidi ? "Export to MIDI" : "Export to XML", "", "DavidicOutput", m_isMidi ? "midi" : "xml");
		if (!string.IsNullOrEmpty(path))
		{
			m_musicPlayer.Export(path, m_isMidi);
		}
#endif
	}

	// Called from browser
	public void OnFileDownload()
	{
	}
}
