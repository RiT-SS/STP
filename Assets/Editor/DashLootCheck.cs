using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DashLootCheck
{
	static readonly string[] scenes = { "GameScene_AC", "GameScene_WW", "GameScene_MA", "GameScene_AC" };
	static readonly int[] pickups = { 3, 2, 4, 1 };
	static readonly int[] sceneIndex = { 0, 1, 2, 0 };
	static readonly int[] expected = new int[3];
	static string folder, report;
	static int stage, phase, errors, frame, captures;
	static bool oldEnabled, measured;
	static EnterPlayModeOptions oldOptions;
	static double timeout, resumeAt;
	static float started, spacing, nextCapture;
	static Vector3 held;
	static PlayerControl player;
	static SkillControl skills;
	static GroundControl ground;
	static pause menu;
	static Rigidbody2D body;
	static GameObject lethal, obstacle, narrow, floor, swallowed;
	static Dictionary<Collider2D, bool> colliders = new Dictionary<Collider2D, bool>();
	static List<GameObject> fragments;

	public static void Run()
	{
		folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../DashLootReview"));
		Directory.CreateDirectory(folder); report = Path.Combine(folder, "test.txt");
		File.WriteAllText(report, "Dash, obstacle and scene loot regression\n");
		oldEnabled = EditorSettings.enterPlayModeOptionsEnabled; oldOptions = EditorSettings.enterPlayModeOptions;
		EditorSettings.enterPlayModeOptionsEnabled = true; EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
		Application.logMessageReceived += OnLog; EditorApplication.update += Tick;
		EventControl.ResetRun();
		角色选择界面.当前选择的角色序号 = 2;
		EditorSceneManager.OpenScene("Assets/Scenes/" + scenes[0] + ".unity");
		Time.captureFramerate = 60; EditorApplication.isPlaying = true;
		timeout = EditorApplication.timeSinceStartup + 180;
	}
	static void OnLog(string message, string stack, LogType type)
	{
		if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
		{
			errors++; File.AppendAllText(report, message + "\n" + stack + "\n");
		}
	}
	static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
	static void Pass(string message) { File.AppendAllText(report, "PASS " + scenes[Mathf.Min(stage, 3)] + ": " + message + "\n"); }
	static void Tick()
	{
		try
		{
			if (EditorApplication.timeSinceStartup > timeout) throw new Exception("Timed out at " + stage + "/" + phase);
			if (!EditorApplication.isPlaying) return;
			if (phase == 0)
			{
				if (SceneManager.GetActiveScene().name != scenes[stage] || !EventControl.isplay || PlayerControl.Instance == null) return;
				player = PlayerControl.Instance; skills = SkillControl.Instance; ground = GroundControl.Instance; menu = UnityEngine.Object.FindObjectOfType<pause>();
				Require(SkillControl.skill == 2 && !PlayerControl.isRage, "Scene skill reset failed");
				for (int i = 0; i < 3; i++) Require(EventControl.lootCount[i] == expected[i], "Loot was lost or reset by scene transition");
				Require(EventControl.piece_count == 0, "Per-scene portal count did not reset");
				body = player.GetComponent<Rigidbody2D>();
				body.position = new Vector2(PlayerControl.player_x, 1.5f); body.velocity = Vector2.zero;
				ground.enabled = false;
				colliders.Clear();
				foreach (Collider2D collider in ground.GetComponentsInChildren<Collider2D>()) {colliders[collider] = collider.enabled; collider.enabled = false;}
				skills.ltTime = 0.7f; skills.Rage();
				Require(PlayerControl.isRage && body.gravityScale == 0f, "Rage did not lock gravity");
				held = player.transform.position;
				player.Damage(); Require(PlayerControl.Hp == 3, "Rage took normal damage");
				lethal = MakeBox("test death area", null, held, new Vector2(1f, 1f), "Die");
				obstacle = MakeBox("test rage obstacle", null, held, new Vector2(0.5f, 0.5f), "hinder");
				obstacle.AddComponent<SpriteRenderer>().sprite = ground.transform.GetChild(0).GetComponent<SpriteRenderer>().sprite;
				started = Time.time; phase++; return;
			}
			if (phase == 1)
			{
				if (Time.time - started < 0.3f) return;
				Require(PlayerControl.isRage && PlayerControl.Hp == 3 && Vector3.Distance(player.transform.position, held) < 0.02f, "Rage fell or died over gap");
				Require(obstacle == null && UnityEngine.Object.FindObjectsOfType<PieceControl>().Length > 0, "Rage obstacle did not fracture");
				UnityEngine.Object.Destroy(lethal);
				menu.pauseBtn(); resumeAt = EditorApplication.timeSinceStartup + 0.3; phase++; return;
			}
			if (phase == 2)
			{
				if (EditorApplication.timeSinceStartup < resumeAt) return;
				Require(Vector3.Distance(player.transform.position, held) < 0.02f && PlayerControl.isRage, "Paused rage moved");
				menu.returnBtn(); phase++; return;
			}
			if (phase == 3)
			{
				if (Time.time - started < 1.1f) return;
				Require(PlayerControl.isRage && PlayerControl.Hp == 3 && SkillControl.lt.fillAmount <= 0.01f, "Rage ended over empty gap");
				narrow = MakeBox("narrow landing", ground.transform, new Vector3(PlayerControl.player_x, -0.8f, 0f), new Vector2(0.25f, 0.3f), "Ground");
				started = Time.time; phase++; return;
			}
			if (phase == 4)
			{
				if (Time.time - started < 0.2f) return;
				Require(PlayerControl.isRage, "Rage ended on insufficient ledge");
				UnityEngine.Object.Destroy(narrow);
				floor = MakeBox("safe landing", ground.transform, new Vector3(PlayerControl.player_x + 2f, -0.8f, 0f), new Vector2(8f, 0.5f), "Ground");
				started = Time.time; phase++; return;
			}
			if (phase == 5)
			{
				if (Time.time - started < 1.2f) return;
				Require(!PlayerControl.isRage && PlayerControl.Hp == 3 && body.gravityScale > 0f && PlayerControl.isGround, "Rage did not restore physics and land safely");
				Require(GroundControl.Speed == 2f && BgControl.Speed == 0.2f, "Rage speed was not restored");
				skills.Rage(); Require(!PlayerControl.isRage, "Rage ignored cooldown");
				Pass("real physics gap/death area protection, obstacle shatter, pause, safe landing extension and cooldown");
				for (int i = 0; i < pickups[stage]; i++)
				{
					var item = MakeBox("piece pickup " + i, null, player.box.bounds.center, new Vector2(0.25f, 0.25f), "Untagged");
					item.GetComponent<BoxCollider2D>().isTrigger = true;
					item.AddComponent<ItemControl>();
				}
				started = Time.time; phase++; return;
			}
			if (phase == 6)
			{
				if (Time.time - started < 0.2f) return;
				expected[sceneIndex[stage]] += pickups[stage];
				for (int i = 0; i < 3; i++) Require(EventControl.lootCount[i] == expected[i], "Actual item trigger counted loot incorrectly");
				Require(EventControl.piece_count == pickups[stage], "Current stage portal count is wrong");
				Pass("actual pickups counted once; cumulative loot=" + string.Join(",", expected));
				body.simulated = false; floor.transform.SetParent(null); UnityEngine.Object.Destroy(floor);
				foreach (var pair in colliders) if (pair.Key != null) pair.Key.enabled = pair.Value;
				ground.enabled = true;
				var parent = ground.transform.GetChild(0);
				swallowed = MakeBox("test black hole barrier", parent, new Vector3(-2.8f, -0.7f, 0f), new Vector2(0.3f, 0.6f), "hinder");
				swallowed.AddComponent<SpriteRenderer>().sprite = parent.GetComponent<SpriteRenderer>().sprite;
				started = Time.time; phase++; return;
			}
			if (phase == 7)
			{
				if (Time.time - started < 0.15f) return;
				Require(swallowed == null, "Barrier near hole did not shatter");
				Pass("black hole barrier follows shared fracture path");
				ground.enabled = false;
				CreateFragments(); started = Time.time; measured = false; captures = 0; nextCapture = 0f; phase++; return;
			}
			if (phase == 8)
			{
				float age = Time.time - started;
				if (stage == 1 && age >= nextCapture && age <= 1.2f)
				{
					Capture("compact-" + captures.ToString("D3")); captures++; nextCapture += 1f / 30f;
				}
				if (age >= 0.2f && !measured)
				{
					float ratio = Spacing() / spacing;
					Require(ratio > 1.05f && ratio < 1.5f, "Spread is not compact: " + ratio);
					Pass("compact fragment spacing ratio=" + ratio.ToString("F2")); measured = true;
				}
				if (age < 1.3f) return;
				foreach (var piece in fragments) Require(piece == null, "Fragments were not collected");
				if (stage < 3)
				{
					stage++; phase = 0;
					SceneManager.LoadScene(scenes[stage]); return;
				}
				EventControl.Route = 261.92f; PlayerControl.Hp = 1; PlayerControl.isShield = false; SkillControl.isEffect = false;
				player.Damage(); frame = Time.frameCount; phase++; return;
			}
			if (phase == 9)
			{
				if (Time.frameCount <= frame + 1) return;
				Require(player.deathPanel.activeSelf, "Death panel missing");
				var summary = player.deathPanel.transform.Find("lootSummary");
				for (int i = 0; i < 3; i++) Require(summary.Find("stage" + i + "/count").GetComponent<TextMeshProUGUI>().text == expected[i].ToString(), "Final loot label mismatch");
				Require(player.deathPanel.transform.Find("score").GetComponent<TextMeshProUGUI>().text == "261.92m", "Score changed");
				Capture("loot-summary");
				Pass("final distance and per-scene loot labels across four native scene loads");
				menu.restart(); phase++; return;
			}
			if (phase == 10)
			{
				if (SceneManager.GetActiveScene().name != "GameScene_AC" || PlayerControl.Instance == player) return;
				foreach (int count in EventControl.lootCount) Require(count == 0, "Restart retained previous loot");
				Require(EventControl.Route == 0f && PlayerControl.Hp == 3, "Restart retained run state");
				Pass("restart clears all scene loot and distance"); Finish();
			}
		}
		catch (Exception e) { errors++; File.AppendAllText(report, e + "\n"); Debug.LogError(e); Finish(); }
	}
	static GameObject MakeBox(string name, Transform parent, Vector3 position, Vector2 size, string tag)
	{
		var obj = new GameObject(name); obj.transform.SetParent(parent); obj.transform.position = position;
		obj.tag = tag; obj.AddComponent<BoxCollider2D>().size = size; return obj;
	}
	static void CreateFragments()
	{
		var source = UnityEngine.Object.Instantiate(ground.PreCrash);
		var original = ground.transform.GetChild(0).GetComponent<SpriteRenderer>().sprite;
		Rect rect = original.textureRect;
		var sprite = Sprite.Create(original.texture, new Rect(rect.x, rect.y, Mathf.Min(100f, rect.width), rect.height), Vector2.one * 0.5f, original.pixelsPerUnit);
		source.AddComponent<GroundSprite>().SetSprite(sprite); source.GetComponent<SpriteRenderer>().sprite = sprite;
		source.transform.position = new Vector3(-2.6f, -0.7f, 0f); source.AddComponent<PolygonCollider2D>();
		var explode = source.AddComponent<Explodable>(); explode.extraPoints = 10; explode.allowRuntimeFragmentation = true;
		explode.shatterType = Explodable.ShatterType.Voronoi; explode.orderInLayer = -5; explode.explode();
		fragments = new List<GameObject>(explode.fragments); Require(fragments.Count > 3, "No fragments"); spacing = Spacing();
	}
	static float Spacing()
	{
		float sum = 0f; int count = 0;
		for (int i = 0; i < fragments.Count; i++) for (int j = i + 1; j < fragments.Count; j++) {sum += Vector3.Distance(fragments[i].transform.position, fragments[j].transform.position); count++;}
		return sum / count;
	}
	static void Capture(string name)
	{
		var camera = Camera.main; var target = RenderTexture.GetTemporary(1400, 560, 24);
		var active = RenderTexture.active; var old = camera.targetTexture;
		try
		{
			camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
			var image = new Texture2D(1400, 560, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, 1400, 560), 0, 0); image.Apply();
			File.WriteAllBytes(Path.Combine(folder, name + ".png"), image.EncodeToPNG()); UnityEngine.Object.Destroy(image);
		}
		finally {camera.targetTexture = old; RenderTexture.active = active; RenderTexture.ReleaseTemporary(target);}
	}
	static void Finish()
	{
		Time.captureFramerate = 0; Time.timeScale = 1f;
		EditorSettings.enterPlayModeOptionsEnabled = oldEnabled; EditorSettings.enterPlayModeOptions = oldOptions;
		EditorApplication.update -= Tick; Application.logMessageReceived -= OnLog;
		File.AppendAllText(report, "FINISHED errors=" + errors + "\n"); EditorApplication.Exit(errors == 0 ? 0 : 1);
	}
}
