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

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void DownloadFile(string gameObjectName, string methodName, string filename, byte[] byteArray, int byteArraySize);
#endif

    // Browser plugin should be called in OnPointerDown.
    public void OnPointerDown(PointerEventData eventData)
	{
#if UNITY_WEBGL && !UNITY_EDITOR
		var bytes = Encoding.UTF8.GetBytes(m_musicPlayer.Export(null));
		DownloadFile(gameObject.name, "OnFileDownload", "DavidicOutput.xml", bytes, bytes.Length);
#else
		var path = StandaloneFileBrowser.SaveFilePanel("Export to XML", "", "DavidicOutput", "xml");
		if (!string.IsNullOrEmpty(path))
		{
			m_musicPlayer.ExportXML(path);
		}
#endif
	}

	// Called from browser
	public void OnFileDownload()
	{
	}
}
