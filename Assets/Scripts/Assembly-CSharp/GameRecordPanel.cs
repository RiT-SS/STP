using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameRecordPanel : MonoBehaviour
{
	public GameObject panel;
	public GameObject empty;
	public RectTransform content;
	public ScrollRect scroll;
	public GameRecordRow[] rows;
	public TextMeshProUGUI total;

	public void Open()
	{
		var records = GameRecords.GetRecords();
		for (int i = 0; i < rows.Length; i++)
		{
			rows[i].gameObject.SetActive(i < records.Count);
			if (i < records.Count) rows[i].Show(records[i], i + 1);
		}
		empty.SetActive(records.Count == 0);
		total.text = records.Count + " / " + GameRecords.MaxRecords;
		content.sizeDelta = new Vector2(content.sizeDelta.x, records.Count * 78f);
		scroll.verticalScrollbar.gameObject.SetActive(content.sizeDelta.y > scroll.viewport.rect.height);
		panel.SetActive(true);
		Canvas.ForceUpdateCanvases();
		scroll.verticalNormalizedPosition = 1f;
	}

	public void Close()
	{
		panel.SetActive(false);
	}

	private void Update()
	{
		if (panel.activeSelf && Input.GetKeyDown(KeyCode.Escape)) Close();
	}
}
