using UnityEngine;
using UnityEngine.UI;

public class SelectionControls : MonoBehaviour
{
	public void SelectOrDeselectAll(bool select)
	{
		foreach (Toggle t in GetComponentsInChildren<Toggle>()) {
			t.isOn = select;
		}
	}
}
