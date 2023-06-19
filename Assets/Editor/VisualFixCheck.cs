using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class VisualFixCheck
{
	static readonly string[] scenes = { "GameScene_WW", "GameScene_AC", "GameScene_MA" };
	static string folder, report;
	static int stage, phase, errors, frame;
	static bool oldEnabled, stopping, spreadChecked, pauseChecked;
	static EnterPlayModeOptions oldOptions;
	static double timeout, resumeAt;
	static float started, nextCapture, initialSpacing;
	static PlayerControl player;
	static pause menu;
	static GameObject shell, deathPanel;
	static List<GameObject> fragments;
	static Vector3[] pausedPositions;
	static int captures;

	public static void Run()
	{
		folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../VisualFixReview"));
		Directory.CreateDirectory(folder);
		report = Path.Combine(folder, "test.txt");
		File.WriteAllText(report, "HUD, score and fragment regression " + Application.unityVersion + "\n");
		oldEnabled = EditorSettings.enterPlayModeOptionsEnabled;
		oldOptions = EditorSettings.enterPlayModeOptions;
		EditorSettings.enterPlayModeOptionsEnabled = true;
		EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
		Application.logMessageReceived += OnLog;
		EditorApplication.update += Tick;
		Begin();
	}
	static void Begin()
	{
		EditorSceneManager.OpenScene("Assets/Scenes/" + scenes[stage] + ".unity");
		角色选择界面.当前选择的角色序号 = 1;
		PlayerControl.Hp = 3;
		EventControl.Route = 0f;
		Time.captureFramerate = 60;
		phase = 0; captures = 0;
		spreadChecked = pauseChecked = stopping = false;
		timeout = EditorApplication.timeSinceStartup + 65;
		EditorApplication.isPlaying = true;
	}
	static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
	static void Pass(string message) { File.AppendAllText(report, "PASS " + scenes[stage] + ": " + message + "\n"); }
	static void OnLog(string message, string stack, LogType type)
	{
		if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
		{
			errors++;
			File.AppendAllText(report, type + ": " + message + "\n" + stack + "\n");
		}
	}
	static void Tick()
	{
		try
		{
			if (stopping)
			{
				if (EditorApplication.isPlayingOrWillChangePlaymode) return;
				stage++;
				if (stage < scenes.Length) Begin(); else Finish();
				return;
			}
			if (EditorApplication.timeSinceStartup > timeout) throw new Exception("Timed out at phase " + phase);
			if (!EditorApplication.isPlaying) return;
			if (phase == 0)
			{
				if (PlayerControl.Instance == null || !EventControl.isplay) return;
				player = PlayerControl.Instance;
				deathPanel = player.deathPanel;
				menu = UnityEngine.Object.FindObjectOfType<pause>();
				player.GetComponent<Rigidbody2D>().simulated = false;
				player.transform.position = new Vector3(PlayerControl.player_x, 3f, 0f);
				UnityEngine.Object.FindObjectOfType<GroundControl>().enabled = false;
				var ui = EventControl.Instance.GetComponent<Canvas>();
				int hud = SortingLayer.GetLayerValueFromID(ui.sortingLayerID);
				int world = SortingLayer.GetLayerValueFromID(0);
				Require(hud > world, "HUD sorting layer does not cover world");
				foreach (Renderer r in EventControl.Instance.GetComponentsInChildren<Renderer>(true)) Require(SortingLayer.GetLayerValueFromID(r.sortingLayerID) > world, "HUD renderer still uses world layer: " + r.name);
				var label = deathPanel.transform.Find("score").GetComponent<TextMeshProUGUI>();
				label.font.ReadFontAssetDefinition();
				Require(label.font.HasCharacters("0123456789.m"), "Score font is missing numeric characters: " + label.font.name + "; table=" + label.font.characterTable.Count);
				Pass("HUD and skill sprite sorting above world; score has every digit");
				PlayerControl.isShield = false; PlayerControl.Hp = 3;
				shell = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/shell.prefab"));
				shell.transform.position = player.transform.position + new Vector3(player.box.offset.x - 0.05f, player.box.offset.y, 0f);
				started = Time.time; phase++; return;
			}
			if (phase == 1)
			{
				if (Time.time - started < 0.15f) return;
				Require(PlayerControl.Hp == 2 && shell.GetComponent<Animator>().enabled, "Centered projectile failed to hit");
				UnityEngine.Object.Destroy(shell);
				Pass("projectile centered in player torso causes one hit");
				PlayerControl.Hp = 3; PlayerControl.isShield = true;
				CreateFragments();
				started = Time.time; nextCapture = 0f; phase++; return;
			}
			if (phase == 2)
			{
				float age = Time.time - started;
				if (age > 0.14f && !pauseChecked)
				{
					pauseChecked = true;
					pausedPositions = new Vector3[fragments.Count];
					for (int i = 0; i < fragments.Count; i++) pausedPositions[i] = fragments[i].transform.position;
					menu.pauseBtn();
					resumeAt = EditorApplication.timeSinceStartup + 0.3;
					phase = 3; return;
				}
				if (age > 0.29f && !spreadChecked)
				{
					spreadChecked = true;
					float spacing = Spacing();
					Require(spacing > initialSpacing * 1.4f, "Fragments did not separate before absorption");
					Pass("fragment average separation " + initialSpacing.ToString("F3") + " -> " + spacing.ToString("F3"));
				}
				if (stage == 0 && age >= nextCapture && age <= 1.65f)
				{
					Capture("fragment-" + captures.ToString("D3")); captures++; nextCapture += 1f / 30f;
				}
				if (age < 1.7f) return;
				foreach (var fragment in fragments) Require(fragment == null, "Fragment did not finish absorption");
				Pass("fragments finish absorption without leaving overlapping pieces");
				MakeWorldObstacles();
				EventControl.Route = 219.05f;
				PlayerControl.Hp = 1; PlayerControl.isShield = false; SkillControl.isEffect = false;
				player.Damage();
				frame = Time.frameCount; phase = 4; return;
			}
			if (phase == 3)
			{
				if (EditorApplication.timeSinceStartup < resumeAt) return;
				for (int i = 0; i < fragments.Count; i++) Require(fragments[i].transform.position == pausedPositions[i], "Fragment moved during pause");
				Require(CanClick(menu.menu.transform.Find("returnToGame")), "Pause continue button not raycastable");
				menu.returnBtn();
				Pass("fragment pause and menu button raycast");
				phase = 2; return;
			}
			if (phase == 4)
			{
				if (Time.frameCount <= frame + 1) return;
				Require(deathPanel.activeSelf && PlayerControl.Hp == 0 && !EventControl.isplay, "Death did not freeze run");
				var route = EventControl.Instance.transform.Find("board_1/route").GetComponent<TextMeshProUGUI>();
				var score = deathPanel.transform.Find("score").GetComponent<TextMeshProUGUI>();
				score.ForceMeshUpdate();
				Require(score.text == "219.05m" && score.text == route.text, "HUD and death score disagree");
				foreach (TMP_CharacterInfo c in score.textInfo.characterInfo) if (c.character == '9') Require(c.isVisible, "Digit 9 is invisible in score mesh");
				Require(CanClick(deathPanel.transform.Find("restart")), "Death restart button not raycastable");
				Capture(scenes[stage] + "-score-219.05");
				Pass("death HUD = 219.05m; all glyphs visible; restart button raycast");
				deathPanel.SetActive(false);
				PlayerControl.Hp = 3; EventControl.isplay = true;
				EventControl.Route = 9879.7f;
				player.transform.position = new Vector3(-3.3f, 3f, 0f);
				frame = Time.frameCount; phase++; return;
			}
			if (phase == 5)
			{
				if (Time.frameCount <= frame + 2) return;
				Require(PlayerControl.Instance == null && PlayerControl.Hp == 0 && deathPanel.activeSelf, "Inner black hole skipped death settlement");
				Require(deathPanel.transform.Find("score").GetComponent<TextMeshProUGUI>().text == "9879.70m", "Large score lost digits");
				Pass("direct inner black-hole death settles and stops dependent updates");
				EditorApplication.isPlaying = false; stopping = true;
			}
		}
		catch (Exception e) { errors++; File.AppendAllText(report, e + "\n"); Debug.LogError(e); Finish(); }
	}
	static void CreateFragments()
	{
		var ground = UnityEngine.Object.FindObjectOfType<GroundControl>();
		var source = UnityEngine.Object.Instantiate(ground.PreCrash);
		var original = ground.transform.GetChild(0).GetComponent<SpriteRenderer>().sprite;
		Rect rect = original.textureRect;
		var sprite = Sprite.Create(original.texture, new Rect(rect.x, rect.y, 100f, rect.height), Vector2.one * 0.5f, original.pixelsPerUnit);
		source.AddComponent<GroundSprite>().SetSprite(sprite);
		var renderer = source.GetComponent<SpriteRenderer>();
		renderer.sprite = sprite; renderer.sortingOrder = -5;
		source.transform.position = new Vector3(-2.5f, -0.8f, 0f);
		source.AddComponent<PolygonCollider2D>();
		var explode = source.AddComponent<Explodable>();
		explode.shatterType = Explodable.ShatterType.Voronoi;
		explode.extraPoints = 10; explode.allowRuntimeFragmentation = true; explode.orderInLayer = -5;
		explode.explode();
		fragments = new List<GameObject>(explode.fragments);
		Require(fragments.Count > 3, "No fragments generated");
		initialSpacing = Spacing();
	}
	static float Spacing()
	{
		float total = 0f; int pairs = 0;
		for (int i = 0; i < fragments.Count; i++)
			for (int j = i + 1; j < fragments.Count; j++) {total += Vector3.Distance(fragments[i].transform.position, fragments[j].transform.position); pairs++;}
		return total / pairs;
	}
	static bool CanClick(Transform button)
	{
		var pointer = new PointerEventData(EventSystem.current);
		pointer.position = RectTransformUtility.WorldToScreenPoint(Camera.main, button.position);
		var hits = new List<RaycastResult>();
		EventSystem.current.RaycastAll(pointer, hits);
		foreach (var hit in hits) if (hit.gameObject.transform == button || hit.gameObject.transform.IsChildOf(button)) return true;
		return false;
	}
	static void MakeWorldObstacles()
	{
		Sprite sprite = null;
		foreach (var render in UnityEngine.Object.FindObjectsOfType<SpriteRenderer>())
			if (render.name.Contains("pipe")) { sprite = render.sprite; break; }
		if (sprite == null) sprite = UnityEngine.Object.FindObjectOfType<GroundControl>().transform.GetChild(0).GetComponent<SpriteRenderer>().sprite;
		foreach (string name in new[] { "Skill_1", "board_1" })
		{
			var probe = new GameObject("HUD occlusion test obstacle");
			probe.transform.position = EventControl.Instance.transform.Find(name).position;
			var render = probe.AddComponent<SpriteRenderer>();
			render.sprite = sprite; render.sortingLayerID = 0; render.sortingOrder = 32760;
			probe.transform.localScale = new Vector3(1f, 2f, 1f);
		}
	}
	static void Capture(string name)
	{
		Camera camera = Camera.main;
		var target = RenderTexture.GetTemporary(1400, 560, 24);
		var active = RenderTexture.active; var old = camera.targetTexture;
		try
		{
			camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
			var image = new Texture2D(1400, 560, TextureFormat.RGB24, false);
			image.ReadPixels(new Rect(0, 0, 1400, 560), 0, 0); image.Apply();
			File.WriteAllBytes(Path.Combine(folder, name + ".png"), image.EncodeToPNG());
			UnityEngine.Object.Destroy(image);
		}
		finally { camera.targetTexture = old; RenderTexture.active = active; RenderTexture.ReleaseTemporary(target); }
	}
	static void Finish()
	{
		Time.captureFramerate = 0; Time.timeScale = 1f;
		EditorSettings.enterPlayModeOptionsEnabled = oldEnabled;
		EditorSettings.enterPlayModeOptions = oldOptions;
		EditorApplication.update -= Tick;
		Application.logMessageReceived -= OnLog;
		File.AppendAllText(report, "FINISHED errors=" + errors + "\n");
		EditorApplication.Exit(errors == 0 ? 0 : 1);
	}
}
