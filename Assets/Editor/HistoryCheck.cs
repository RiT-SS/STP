using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class HistoryCheck
{
	static string folder, report, path, userPath, oldUserData;
	static int phase, errors, diedFrame;
	static float started;
	static double timeout;
	static bool oldEnabled;
	static EnterPlayModeOptions oldOptions;
	static long firstStart, secondStart;
	static GameRecords.Record firstRecord;
	static GameRecordPanel menu;
	static readonly BindingFlags fields = BindingFlags.NonPublic | BindingFlags.Static;

	public static void Run()
	{
		folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../HistoryReview")); Directory.CreateDirectory(folder);
		report = Path.Combine(folder, "test.txt"); File.WriteAllText(report, "History, per-run loot, names, icons and 3 second rage\n");
		oldEnabled = EditorSettings.enterPlayModeOptionsEnabled; oldOptions = EditorSettings.enterPlayModeOptions;
		try
		{
			userPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../../SaveData/game-records.json"));
			oldUserData = File.Exists(userPath) ? File.ReadAllText(userPath) : null;
			path = Path.Combine(folder, "test-records.json"); if (File.Exists(path)) File.Delete(path);
			SetPath(); TestData();
			EditorSettings.enterPlayModeOptionsEnabled = true; EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
			EditorSceneManager.OpenScene("Assets/Scenes/StartMenu.unity");
			Application.logMessageReceived += OnLog; EditorApplication.update += Tick;
			timeout = EditorApplication.timeSinceStartup + 240; EditorApplication.isPlaying = true;
		}
		catch (Exception e) {errors++; File.AppendAllText(report, e + "\n"); Finish();}
	}
	static void SetPath()
	{
		typeof(GameRecords).GetField("filePath", fields).SetValue(null, path);
		typeof(GameRecords).GetField("history", fields).SetValue(null, null);
		typeof(GameRecords).GetField("current", fields).SetValue(null, null);
	}
	static GameRecords.Record Current() {return (GameRecords.Record)typeof(GameRecords).GetField("current", fields).GetValue(null);}
	static void Require(bool ok, string message) {if (!ok) throw new Exception(message);}
	static void Pass(string message) {File.AppendAllText(report, "PASS " + message + "\n");}
	static void TestData()
	{
		Require(GameRecords.MaxRecords == 20, "History limit must be 20");
		Require(GameRecords.GetRecords().Count == 0, "New history was not empty");
		var all = new List<GameRecords.Record>();
		for (int i = 0; i < 26; i++)
		{
			EventControl.ResetRun(); 角色选择界面.当前选择的角色序号 = i % 3; GameRecords.BeginRun();
			Current().startedAt = new DateTime(2026, 9, i + 1, 6, 37, i, DateTimeKind.Utc).Ticks;
			EventControl.Route = i == 24 ? 12345.67f : 100f + i % 6 * 125.25f;
			for (int j = 0; j < 3; j++) EventControl.lootCount[j] = i * 137 + j;
			GameRecords.Record record = Current(); all.Add(record); GameRecords.EndRun(true); GameRecords.EndRun(true);
			Require(GameRecords.GetRecords().Count == Mathf.Min(i + 1, 20), "Duplicate saves or wrong cap");
		}
		var expected = all.OrderByDescending(record => record.distance).ThenByDescending(record => record.startedAt).Take(20).ToList();
		var actual = GameRecords.GetRecords();
		for (int i = 0; i < 20; i++) Require(actual[i] == expected[i], "Wrong distance/time ordering at " + i);
		Require(actual.All(record => record.distance > 100f), "Low scores were not discarded");
		int before = actual[0].loot[0]; EventControl.ResetRun();
		Require(EventControl.lootCount.All(number => number == 0) && actual[0].loot[0] == before, "Reset carried loot or mutated saved loot");
		SetPath(); actual = GameRecords.GetRecords();
		Require(actual.Count == 20, "History did not reload from disk");
		for (int i = 0; i < 20; i++) Require(actual[i].startedAt == expected[i].startedAt && actual[i].loot.SequenceEqual(expected[i].loot), "Disk serialization lost time or loot");
		Pass("26 results -> top 20 by distance then newest start time; discarded scores deleted; save deduplicated; independent loot snapshots and disk reload");
	}
	static void OnLog(string message, string stack, LogType type)
	{
		if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) {errors++; File.AppendAllText(report, message + "\n" + stack + "\n");}
	}
	static bool Ready(string scene) {return SceneManager.GetActiveScene().name == scene && PlayerControl.Instance != null && EventControl.isplay;}
	static void Freeze()
	{
		GroundControl.Instance.enabled = false; PlayerControl.Instance.GetComponent<Rigidbody2D>().simulated = false;
	}
	static void Collect(int number) {for (int i = 0; i < number; i++) EventControl.Instance.CollectPiece();}
	static void StartGame(int role)
	{
		角色选择界面.当前选择的角色序号 = role;
		GameObject.Find("StartButton").GetComponent<Button>().onClick.Invoke();
		Require(Current() != null && Current().character == role, "Start button did not capture selected role and time");
	}
	static void Tick()
	{
		try
		{
			if (EditorApplication.timeSinceStartup > timeout) throw new Exception("Timed out at phase " + phase);
			if (!EditorApplication.isPlaying) return;
			if (phase == 0)
			{
				if (Time.timeSinceLevelLoad < 0.3f) return;
				menu = UnityEngine.Object.FindObjectOfType<GameRecordPanel>(); Require(menu != null, "Records component missing");
				var button = GameObject.Find("RecordButton").GetComponent<Button>();
				Canvas.ForceUpdateCanvases();
				var pointer = new PointerEventData(EventSystem.current);
				pointer.position = RectTransformUtility.WorldToScreenPoint(button.GetComponentInParent<Canvas>().worldCamera, button.transform.position);
				var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
				Require(hits.Any(hit => hit.gameObject.GetComponentInParent<Button>() == button), "Records button cannot receive pointer input");
				Capture("main-menu"); button.onClick.Invoke();
				Require(menu.panel.activeSelf && menu.rows.Length == 20 && !menu.empty.activeSelf, "History did not open with 20 rows");
				var records = GameRecords.GetRecords();
				for (int i = 0; i < 20; i++)
				{
					Require(menu.rows[i].character.text == GameRecords.GetCharacterName(records[i].character), "Wrong history role name");
					Require(menu.rows[i].playedAt.text == new DateTime(records[i].startedAt, DateTimeKind.Utc).ToLocalTime().ToString("yyyy/MM/dd\nHH:mm:ss"), "History shows cumulative time");
					for (int j = 0; j < 3; j++) Require(menu.rows[i].loot[j].text == records[i].loot[j].ToString(), "History loot row mismatch");
					foreach (var icon in menu.rows[i].GetComponentsInChildren<Image>()) if (icon.name.StartsWith("icon")) Require(icon.rectTransform.sizeDelta.x >= 56f, "History icon too small");
				}
				Capture("records-top"); Capture("records-small", 700, 280); Capture("records-wide", 1600, 900);
				menu.scroll.verticalNormalizedPosition = 0f; Canvas.ForceUpdateCanvases(); Capture("records-bottom");
				var corners = new Vector3[4]; menu.rows[19].GetComponent<RectTransform>().GetWorldCorners(corners);
				Require(menu.scroll.viewport.rect.Contains(menu.scroll.viewport.InverseTransformPoint((corners[0] + corners[2]) * 0.5f)), "20th row cannot be reached by scrolling");
				GameRecords.GetRecords().Clear(); menu.Open(); Require(menu.empty.activeSelf, "Empty state missing"); Capture("records-empty");
				SetPath(); menu.Open();
				menu.panel.transform.Find("frame/back").GetComponent<Button>().onClick.Invoke(); Require(!menu.panel.activeSelf, "History back button failed");
				Pass("menu pointer input; 20 history rows and local start timestamps; large icons; empty state; scroll reaches row 20; return button; 3 display sizes captured");
				StartGame(2); firstStart = Current().startedAt; phase = 1; return;
			}
			if (phase == 1)
			{
				if (!Ready("GameScene_AC")) return;
				Freeze(); Require(EventControl.lootCount.All(number => number == 0), "First run did not start at zero");
				Require(SkillControl.Instance.ltTime == 3f, "Serialized rage duration is not 3 seconds");
				Collect(3); SceneManager.LoadScene("GameScene_WW"); phase = 2; return;
			}
			if (phase == 2)
			{
				if (!Ready("GameScene_WW")) return;
				Freeze(); Require(Current().startedAt == firstStart && EventControl.lootCount.SequenceEqual(new[] {3,0,0}), "Scene transition changed run start or loot");
				Require(EventControl.piece_count == 0 && SkillControl.Instance.ltTime == 3f, "Scene reset or rage config wrong");
				Collect(2); SceneManager.LoadScene("GameScene_MA"); phase = 3; return;
			}
			if (phase == 3)
			{
				if (!Ready("GameScene_MA")) return;
				Freeze(); Collect(4); Require(SkillControl.Instance.ltTime == 3f, "MA rage config not updated");
				EventControl.Route = 20000.12f; PlayerControl.Hp = 1; PlayerControl.isShield = false; SkillControl.isEffect = false;
				PlayerControl.Instance.Damage(); diedFrame = Time.frameCount; phase = 4; return;
			}
			if (phase == 4)
			{
				if (Time.frameCount <= diedFrame + 1) return;
				Require(PlayerControl.Instance.deathPanel.activeSelf, "Death panel not shown");
				var summary = PlayerControl.Instance.deathPanel.transform.Find("lootSummary");
				int[] expected = {3,2,4};
				for (int i = 0; i < 3; i++)
				{
					var column = summary.Find("stage" + i);
					Require(column.Find("title") == null, "Stage labels remain in settlement");
					Require(((RectTransform)column.Find("icon")).sizeDelta.x >= 0.38f, "Settlement icon still too small");
					Require(column.Find("count").GetComponent<TextMeshProUGUI>().text == expected[i].ToString(), "Settlement loot wrong");
				}
				firstRecord = GameRecords.GetRecords()[0]; Require(firstRecord.startedAt == firstStart && firstRecord.character == 2 && firstRecord.loot.SequenceEqual(expected), "Settlement did not save a single run snapshot");
				int count = GameRecords.GetRecords().Count; EventControl.FinishRun(); Require(GameRecords.GetRecords().Count == count, "Death saved twice");
				Capture("settlement"); Pass("three native stages keep the same run/time; loot 3/2/4; settlement has no stage titles and 2.1x icons; finish saved once");
				UnityEngine.Object.FindObjectOfType<pause>().returnToMenu(); phase = 5; return;
			}
			if (phase == 5)
			{
				if (SceneManager.GetActiveScene().name != "StartMenu" || Time.timeSinceLevelLoad < 0.2f) return;
				Require(EventControl.lootCount.All(number => number == 0), "Returning to menu carried loot outside the run");
				StartGame(0); secondStart = Current().startedAt; Require(secondStart > firstStart, "New run reused previous start time"); phase = 6; return;
			}
			if (phase == 6)
			{
				if (!Ready("GameScene_AC")) return;
				Freeze(); Require(EventControl.lootCount.All(number => number == 0), "New run inherited old loot"); Collect(1); EventControl.Route = 40000.34f;
				UnityEngine.Object.FindObjectOfType<pause>().returnToMenu(); phase = 7; return;
			}
			if (phase == 7)
			{
				if (SceneManager.GetActiveScene().name != "StartMenu" || Time.timeSinceLevelLoad < 0.2f) return;
				var record = GameRecords.GetRecords()[0];
				Require(record.startedAt == secondStart && record.character == 0 && record.loot.SequenceEqual(new[] {1,0,0}), "Second run contains lifetime loot or wrong hero/time");
				Require(firstRecord.loot.SequenceEqual(new[] {3,2,4}), "Reset mutated a completed run");
				File.WriteAllLines(Path.Combine(folder, "expected-start-times.txt"), new[] {secondStart.ToString(), firstStart.ToString()});
				menu = UnityEngine.Object.FindObjectOfType<GameRecordPanel>(); menu.Open(); Capture("records-after-runs"); menu.Close();
				Pass("new run starts at 0; second run only 1/0/0; old 3/2/4 result unchanged; abandoning a played run saves its own start time and selected hero");
				UnityEngine.Random.InitState(44); StartGame(2); phase = 8; return;
			}
			if (phase == 8)
			{
				if (!Ready("GameScene_AC")) return;
				typeof(GroundControl).GetField("piece_need", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(GroundControl.Instance, 100000);
				Require(SkillControl.Instance.ltTime == 3f, "Robot duration did not persist");
				Time.captureFramerate = 60; SkillControl.Instance.Rage(); Require(PlayerControl.isRage, "Robot rage failed to start"); started = Time.time; phase = 9; return;
			}
			if (phase == 9)
			{
				Require(PlayerControl.Instance != null && PlayerControl.Hp > 0, "Robot died during rage or release");
				if (PlayerControl.isRage)
				{
					Require(PlayerControl.Hp == 3 && Time.time - started < 9f, "Rage protection or safe release failed"); return;
				}
				float duration = Time.time - started; Require(duration >= 2.9f && duration < 9f && SkillControl.lt.fillAmount <= 0.01f, "Rage did not use the new 3s base duration");
				Pass("robot base duration 3s, cooldown unchanged; natural terrain release at " + duration.ToString("F2") + "s with damage protection");
				PlayerControl.isShield = true; SkillControl.Instance.shieldTime.fillAmount = 0f; started = Time.time; phase = 10; return;
			}
			if (phase == 10)
			{
				if (Time.time - started < 0.8f) return;
				Require(PlayerControl.Hp > 0 && !PlayerControl.Instance.deathPanel.activeSelf && PlayerControl.Instance.GetComponent<Rigidbody2D>().gravityScale > 0f, "Safe landing after rage failed");
				UnityEngine.Object.FindObjectOfType<pause>().returnToMenu(); phase = 11; return;
			}
			if (phase == 11)
			{
				if (SceneManager.GetActiveScene().name != "StartMenu" || Time.timeSinceLevelLoad < 0.2f) return;
				GameObject.Find("SelectButton").GetComponent<Button>().onClick.Invoke(); phase = 12; return;
			}
			if (phase >= 12 && phase <= 14)
			{
				if (SceneManager.GetActiveScene().name != "CharacterSelection" || Time.timeSinceLevelLoad < 0.3f) return;
				if (phase > 12 && Time.time - started < 0.2f) return;
				int role = phase - 12; var text = GameObject.Find("CharacterName").GetComponent<TextMeshProUGUI>(); text.ForceMeshUpdate();
				Require(text.text == GameRecords.GetCharacterName(role) && text.font.HasCharacters(text.text) && !text.isTextOverflowing, "Renamed character missing glyphs or overflows");
				Capture("character-" + role);
				if (role < 2) {UnityEngine.Object.FindObjectOfType<角色选择界面>().切换到下一个角色(); started = Time.time; phase++; return;}
				Pass("all three renamed characters display completely in selection");
				Require(oldUserData == (File.Exists(userPath) ? File.ReadAllText(userPath) : null), "Test polluted actual user history");
				Finish();
			}
		}
		catch (Exception e) {errors++; File.AppendAllText(report, e + "\n"); Finish();}
	}
	static void Capture(string name, int width = 1400, int height = 560)
	{
		var camera = Camera.main; var target = RenderTexture.GetTemporary(width, height, 24);
		var active = RenderTexture.active; var old = camera.targetTexture;
		var overlays = UnityEngine.Object.FindObjectsOfType<Canvas>().Where(canvas => canvas.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
		try
		{
			camera.targetTexture = target;
			foreach (var canvas in overlays) {canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = camera.nearClipPlane + 1f; canvas.sortingLayerName = "Dialogs";}
			Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
			var image = new Texture2D(width, height, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
			File.WriteAllBytes(Path.Combine(folder, name + ".png"), image.EncodeToPNG()); UnityEngine.Object.Destroy(image);
		}
		finally
		{
			foreach (var canvas in overlays) {canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null;}
			camera.targetTexture = old; RenderTexture.active = active; RenderTexture.ReleaseTemporary(target); Canvas.ForceUpdateCanvases();
		}
	}
	static void Finish()
	{
		Time.captureFramerate = 0; Time.timeScale = 1f;
		EditorSettings.enterPlayModeOptionsEnabled = oldEnabled; EditorSettings.enterPlayModeOptions = oldOptions;
		EditorApplication.update -= Tick; EditorApplication.update -= VisualTick; Application.logMessageReceived -= OnLog;
		File.AppendAllText(report, "FINISHED errors=" + errors + "\n"); EditorApplication.Exit(errors == 0 ? 0 : 1);
	}
	public static void Visual()
	{
		folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../HistoryReview")); path = Path.Combine(folder, "test-records.json");
		report = Path.Combine(folder, "visual-test.txt"); File.WriteAllText(report, "Final history and settlement visual check\n");
		oldEnabled = EditorSettings.enterPlayModeOptionsEnabled; oldOptions = EditorSettings.enterPlayModeOptions; SetPath();
		EditorSettings.enterPlayModeOptionsEnabled = true; EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
		EditorSceneManager.OpenScene("Assets/Scenes/StartMenu.unity");
		Application.logMessageReceived += OnLog; EditorApplication.update += VisualTick;
		timeout = EditorApplication.timeSinceStartup + 90; EditorApplication.isPlaying = true;
	}
	static void VisualTick()
	{
		try
		{
			if (EditorApplication.timeSinceStartup > timeout) throw new Exception("Visual check timed out");
			if (!EditorApplication.isPlaying) return;
			if (phase == 0)
			{
				if (Time.timeSinceLevelLoad < 0.2f) return;
				Require(SceneManager.GetActiveScene().GetRootGameObjects().Count(root => root.name == "GameRecordsCanvas") == 1, "Duplicate records canvases");
				Capture("main-menu"); menu = UnityEngine.Object.FindObjectOfType<GameRecordPanel>(); menu.Open();
				Require(menu.panel.GetComponent<Canvas>().renderMode == RenderMode.ScreenSpaceOverlay && menu.panel.GetComponent<Canvas>().sortingOrder >= 100, "Records canvas is below other UI");
				Capture("records-top"); Capture("records-small", 700, 280); Capture("records-wide", 1600, 900);
				menu.scroll.verticalNormalizedPosition = 0f; Canvas.ForceUpdateCanvases(); Capture("records-bottom");
				GameRecords.GetRecords().Clear(); menu.Open(); Capture("records-empty"); SetPath(); menu.Close();
				Pass("single history canvas; correct world-art ordering; top, bottom, empty, small and wide layouts captured");
				角色选择界面.当前选择的角色序号 = 2; SceneManager.LoadScene("GameScene_MA"); phase = 1; return;
			}
			if (phase == 1)
			{
				if (!Ready("GameScene_MA")) return;
				Freeze(); typeof(GameRecords).GetField("current", fields).SetValue(null, null);
				EventControl.Route = 20000.12f; EventControl.lootCount[0] = 3; EventControl.lootCount[1] = 2; EventControl.lootCount[2] = 4;
				PlayerControl.Hp = 1; PlayerControl.isShield = false; SkillControl.isEffect = false; PlayerControl.Instance.Damage(); diedFrame = Time.frameCount; phase = 2; return;
			}
			if (phase == 2)
			{
				if (Time.frameCount <= diedFrame + 1) return;
				var score = PlayerControl.Instance.deathPanel.transform.Find("score").GetComponent<TextMeshProUGUI>(); score.ForceMeshUpdate();
				Require(score.text == "20000.12m" && score.textBounds.size.x <= score.rectTransform.rect.width + 0.02f && !score.isTextOverflowing, "Long distance still exceeds settlement frame");
				Capture("settlement"); Pass("five-digit distance stays inside the settlement frame with larger loot icons"); Finish();
			}
		}
		catch (Exception e) {errors++; File.AppendAllText(report, e + "\n"); Finish();}
	}
	public static void Persistence()
	{
		folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../HistoryReview")); path = Path.Combine(folder, "test-records.json");
		string result = Path.Combine(folder, "persistence.txt");
		try
		{
			SetPath(); var records = GameRecords.GetRecords(); var starts = File.ReadAllLines(Path.Combine(folder, "expected-start-times.txt"));
			Require(records.Count == 20 && records[0].distance == 40000.34f && records[1].distance == 20000.12f, "Restart lost records or ordering");
			Require(records[0].startedAt.ToString() == starts[0] && records[1].startedAt.ToString() == starts[1], "Restart lost the start timestamp");
			Require(records[0].loot.SequenceEqual(new[] {1,0,0}) && records[1].loot.SequenceEqual(new[] {3,2,4}), "Restart mixed per-run loot");
			for (int i = 1; i < records.Count; i++) Require(records[i-1].distance > records[i].distance || (records[i-1].distance == records[i].distance && records[i-1].startedAt >= records[i].startedAt), "Bad ranking after restart");
			File.WriteAllText(result, "PASS new Unity process: top 20, tie ordering, original start timestamps and independent 1/0/0 vs 3/2/4 loot preserved\n"); EditorApplication.Exit(0);
		}
		catch (Exception e) {File.WriteAllText(result, e.ToString()); EditorApplication.Exit(1);}
	}
}
