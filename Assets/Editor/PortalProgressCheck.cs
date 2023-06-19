using System;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PortalProgressCheck
{
	static readonly string[] scenes = {"GameScene_AC", "GameScene_WW", "GameScene_MA", "GameScene_AC"};
	static readonly int[] picked = {4, 15, 2, 3};
	static readonly int[] expected = new int[3];
	static string folder, report, userPath, userBefore, testPath;
	static bool oldEnabled;
	static EnterPlayModeOptions oldOptions;
	static int stage, errors, frame = -1;
	static double timeout;
	static GameObject previous;
	static void Require(bool ok, string message) {if (!ok) throw new Exception(message);}
	static void Pass(string message) {File.AppendAllText(report, "PASS " + message + "\n");}
	public static void Run()
	{
		folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../PortalProgressCapReview")); Directory.CreateDirectory(folder);
		report = Path.Combine(folder, "test.txt"); File.WriteAllText(report, "Portal progress display and cumulative game records\n");
		userPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../../SaveData/game-records.json")); userBefore = File.Exists(userPath) ? File.ReadAllText(userPath) : null;
		testPath = Path.Combine(folder, "test-records-" + DateTime.UtcNow.Ticks + ".json");
		var flags = BindingFlags.Static | BindingFlags.NonPublic;
		typeof(GameRecords).GetField("filePath", flags).SetValue(null, testPath);
		typeof(GameRecords).GetField("history", flags).SetValue(null, null);
		typeof(GameRecords).GetField("current", flags).SetValue(null, null);
		oldEnabled = EditorSettings.enterPlayModeOptionsEnabled; oldOptions = EditorSettings.enterPlayModeOptions;
		EditorSettings.enterPlayModeOptionsEnabled = true; EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
		EventControl.ResetRun(); GameRecords.BeginRun();
		EditorSceneManager.OpenScene("Assets/Scenes/GameScene_AC.unity");
		Application.logMessageReceived += OnLog; EditorApplication.update += Tick;
		timeout = EditorApplication.timeSinceStartup + 150; EditorApplication.isPlaying = true;
	}
	static void OnLog(string message, string stack, LogType type)
	{
		if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) {errors++; File.AppendAllText(report, message + "\n" + stack + "\n");}
	}
	static void CheckLayout(TextMeshProUGUI text)
	{
		Canvas.ForceUpdateCanvases(); text.ForceMeshUpdate();
		Require(text.textInfo.lineCount == 1 && !text.isTextOverflowing && text.textInfo.characterCount == text.text.Length, "Progress text wrapped, overflowed or lost characters: " + text.text);
		Require(text.renderedWidth <= text.rectTransform.rect.width - text.margin.x - text.margin.z + 1f, "Progress text exceeded reserved width: " + text.text);
		var icon = text.transform.parent.Find("piece_icon").GetComponent<SpriteRenderer>();
		Require(text.transform.TransformPoint(text.textBounds.max).x < icon.bounds.min.x, "Progress text overlaps loot icon");
	}
	static void Tick()
	{
		try
		{
			if (EditorApplication.timeSinceStartup > timeout) throw new Exception("Timed out: " + stage);
			if (!EditorApplication.isPlaying || frame == Time.frameCount) return; frame = Time.frameCount;
			if (SceneManager.GetActiveScene().name != scenes[stage] || !EventControl.isplay || PlayerControl.Instance == null || (previous != null && PlayerControl.Instance.gameObject == previous)) return;
			var ground = GroundControl.Instance; ground.enabled = false;
			var player = PlayerControl.Instance; player.GetComponent<Rigidbody2D>().simulated = false;
			foreach (var item in ground.GetComponentsInChildren<ItemControl>()) item.enabled = false;
			var text = EventControl.piece_number; int need = 10 + stage * 2;
			Require(EventControl.scenesPassed == stage && EventControl.PiecesNeeded == need && (int)typeof(GroundControl).GetField("piece_need", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ground) == need, "HUD requirement disagrees with portal requirement");
			Require(text.text == "0/" + need, "New scene did not reset numerator or update denominator: " + text.text);
			for (int i = 0; i < picked[stage]; i++)
			{
				EventControl.Instance.CollectPiece();
				Require(EventControl.piece_count == i + 1 && text.text == Mathf.Min(i + 1, need) + "/" + need, "Display cap changed the actual collected count");
			}
			expected[ground.scene] += picked[stage];
			Require(text.text == Mathf.Min(picked[stage], need) + "/" + need, "Collection did not update HUD");
			for (int i = 0; i < 3; i++) Require(EventControl.lootCount[i] == expected[i], "Per-run total was replaced by current scene count");
			CheckLayout(text); Capture("scene-" + stage);
			Pass(scenes[stage] + ": " + text.text + "; same threshold as portal; cumulative loot=" + string.Join(",", expected));
			if (stage == 1)
			{
				string actual = text.text;
				Require(EventControl.piece_count == 15 && EventControl.lootCount[1] == 15 && actual == "12/12", "Extra loot was capped in the backend");
				foreach (string sample in new[] {"12/12", "100/102", "1000/1024"}) {text.text = sample; CheckLayout(text);}
				text.text = actual; text.ForceMeshUpdate(); Pass("one to four digit counts stay on one line, inside text area and clear of icon");
			}
			if (++stage < scenes.Length)
			{
				previous = player.gameObject; EventControl.scenesPassed++; SceneManager.LoadScene(scenes[stage]); return;
			}
			EventControl.Route = 123.45f; EventControl.FinishRun();
			var records = GameRecords.GetRecords(); Require(records.Count == 1, "Record was not saved once");
			Require(records[0].loot[0] == 7 && records[0].loot[1] == 15 && records[0].loot[2] == 2, "Saved record contains capped counts instead of run totals");
			typeof(GameRecords).GetField("history", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, null);
			Require(GameRecords.GetRecords()[0].loot[0] == 7 && GameRecords.GetRecords()[0].loot[1] == 15 && GameRecords.GetRecords()[0].loot[2] == 2, "Cumulative loot did not survive disk reload");
			EventControl.ResetRun(); Require(EventControl.scenesPassed == 0 && EventControl.PiecesNeeded == 10 && GameRecords.GetRecords()[0].loot[0] == 7, "New run reset changed saved totals");
			Pass("collected 15 in WW with requirement 12: HUD stays 12/12, backend and saved record retain all 15; whole-run loot 7/15/2 survives reload and next-run reset"); Finish();
		}
		catch (Exception e) {errors++; File.AppendAllText(report, e + "\n"); Finish();}
	}
	static void Capture(string name)
	{
		var camera = Camera.main; var target = RenderTexture.GetTemporary(1000, 400, 24); var active = RenderTexture.active; var old = camera.targetTexture;
		try
		{
			camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
			var image = new Texture2D(1000, 400, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, 1000, 400), 0, 0); image.Apply();
			File.WriteAllBytes(Path.Combine(folder, name + ".png"), image.EncodeToPNG()); UnityEngine.Object.Destroy(image);
		}
		finally {camera.targetTexture = old; RenderTexture.active = active; RenderTexture.ReleaseTemporary(target);}
	}
	static void Finish()
	{
		if (userBefore != (File.Exists(userPath) ? File.ReadAllText(userPath) : null)) {errors++; File.AppendAllText(report, "Actual history changed\n");}
		EditorSettings.enterPlayModeOptionsEnabled = oldEnabled; EditorSettings.enterPlayModeOptions = oldOptions;
		EditorApplication.update -= Tick; Application.logMessageReceived -= OnLog;
		File.AppendAllText(report, "FINISHED errors=" + errors + "\n"); EditorApplication.Exit(errors == 0 ? 0 : 1);
	}
}
