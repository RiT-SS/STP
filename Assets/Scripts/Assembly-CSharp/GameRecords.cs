using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class GameRecords
{
	public const int MaxRecords = 20;
	[Serializable]
	public class Record
	{
		public long startedAt;
		public int character;
		public float distance;
		public int[] loot;
	}

	[Serializable]
	private class RecordList
	{
		public List<Record> records = new List<Record>();
	}

	private static readonly string[] names = { "探险家 爱丽丝", "盗贼 燕子", "机器人 伯特" };
	private static Record current;
	private static RecordList history;
	private static string filePath;

	public static bool IsRunning { get { return current != null; } }

	public static string GetCharacterName(int character)
	{
		return names[Mathf.Clamp(character, 0, names.Length - 1)];
	}

	public static void BeginRun()
	{
		if (current != null) return;
		current = new Record();
		current.startedAt = DateTime.UtcNow.Ticks;
		current.character = 角色选择界面.当前选择的角色序号;
	}

	public static void EndRun(bool finished = false)
	{
		if (current == null) return;
		Record record = current;
		current = null;
		if (!finished && EventControl.Route <= 0f) return;
		record.distance = (float)Math.Round(EventControl.Route, 2);
		record.loot = new int[3];
		Array.Copy(EventControl.lootCount, record.loot, record.loot.Length);
		GetRecords().Add(record);
		SortRecords();
		Save();
	}

	public static List<Record> GetRecords()
	{
		if (history != null) return history.records;
		history = new RecordList();
		if (string.IsNullOrEmpty(filePath))
		{
			string folder = Application.isEditor ? "../../SaveData" : "../SaveData";
			filePath = Path.GetFullPath(Path.Combine(Application.dataPath, folder, "game-records.json"));
		}
		try
		{
			if (File.Exists(filePath)) history = JsonUtility.FromJson<RecordList>(File.ReadAllText(filePath)) ?? new RecordList();
			if (history.records == null) history.records = new List<Record>();
			history.records.RemoveAll(record => record == null || record.startedAt <= 0 || record.startedAt > DateTime.MaxValue.Ticks || record.character < 0 || record.character >= names.Length || float.IsNaN(record.distance) || float.IsInfinity(record.distance) || record.distance < 0f || record.loot == null || record.loot.Length != 3);
			bool tooMany = history.records.Count > MaxRecords;
			SortRecords();
			if (tooMany) Save();
		}
		catch (Exception error)
		{
			Debug.LogWarning("游戏记录读取失败：" + error.Message);
			history = new RecordList();
		}
		return history.records;
	}

	private static void SortRecords()
	{
		history.records.Sort((a, b) =>
		{
			int distance = b.distance.CompareTo(a.distance);
			return distance != 0 ? distance : b.startedAt.CompareTo(a.startedAt);
		});
		if (history.records.Count > MaxRecords) history.records.RemoveRange(MaxRecords, history.records.Count - MaxRecords);
	}

	private static void Save()
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(filePath));
			string temporary = filePath + ".tmp";
			File.WriteAllText(temporary, JsonUtility.ToJson(history, true));
			if (File.Exists(filePath)) File.Replace(temporary, filePath, null);
			else File.Move(temporary, filePath);
		}
		catch (Exception error) { Debug.LogWarning("游戏记录保存失败：" + error.Message); }
	}
}
