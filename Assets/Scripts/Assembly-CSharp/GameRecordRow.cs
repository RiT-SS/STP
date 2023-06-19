using System;
using TMPro;
using UnityEngine;

public class GameRecordRow : MonoBehaviour
{
	public TextMeshProUGUI rank;
	public TextMeshProUGUI playedAt;
	public TextMeshProUGUI character;
	public TextMeshProUGUI distance;
	public TextMeshProUGUI[] loot;

	public void Show(GameRecords.Record record, int number)
	{
		rank.text = number.ToString("00");
		rank.color = number == 1 ? new Color(1f, 0.83f, 0.38f) : new Color(0.64f, 0.7f, 0.77f);
		playedAt.text = new DateTime(record.startedAt, DateTimeKind.Utc).ToLocalTime().ToString("yyyy/MM/dd\nHH:mm:ss");
		character.text = GameRecords.GetCharacterName(record.character);
		distance.text = record.distance.ToString("F2") + "m";
		for (int i = 0; i < loot.Length; i++) loot[i].text = record.loot[i].ToString();
	}
}
