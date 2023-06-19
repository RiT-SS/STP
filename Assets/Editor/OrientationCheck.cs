using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OrientationCheck
{
	static string folder, report;
	static int phase, role, example, errors, captured;
	static bool oldEnabled, pauseChecked;
	static EnterPlayModeOptions oldOptions;
	static double timeout, resume;
	static float started, previewStart, gameStart, peakAngle, nextCapture;
	static Animator preview, game;
	static Animator[] previews;
	static string[] controllerPaths;
	static Dictionary<string, string[]> obstacles = new Dictionary<string, string[]>();
	static List<string> names = new List<string>();
	static Dictionary<PieceControl, Quaternion> pieces = new Dictionary<PieceControl, Quaternion>();
	static GameObject source, whole;
	static Vector3 wholePosition;
	static Quaternion[] localRotations;
	static Vector3[] localPositions;
	static bool creature;
	static HashSet<Sprite> previewSprites = new HashSet<Sprite>();

	public static void Run()
	{
		folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../OrientationReview"));
		Directory.CreateDirectory(folder); report = Path.Combine(folder, "test.txt");
		File.WriteAllText(report, "Preview timing, obstacle types and fragment orientation regression\n");
		try
		{
			EditorSceneManager.OpenScene("Assets/Scenes/GameScene_AC.unity");
			controllerPaths = Array.ConvertAll(UnityEngine.Object.FindObjectOfType<PlayerControl>().character, c => AssetDatabase.GetAssetPath(c));
			foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/GameObject" }))
			{
				GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
				foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
				{
					if (!child.CompareTag("Ground") && !child.CompareTag("hinder") && !child.CompareTag("enemy")) continue;
					var marker = child.GetComponent<ObstacleControl>();
					Require(marker != null, "Missing obstacle category: " + prefab.name + "/" + child.name);
					if (child.CompareTag("Ground")) {Require(marker.type == ObstacleControl.ObstacleType.Terrain, "Ground misclassified"); continue;}
					bool living = child.name.StartsWith("AC_enemy_1") || child.name.StartsWith("MA_enemy_1");
					Require((marker.type == ObstacleControl.ObstacleType.Creature) == living, "Wrong obstacle category: " + child.name);
					string name = child.name.Split('(')[0].Trim();
					if (!obstacles.ContainsKey(name)) obstacles[name] = new[] {AssetDatabase.GetAssetPath(prefab), AnimationUtility.CalculateTransformPath(child, prefab.transform)};
				}
			}
			File.AppendAllText(report, "PASS all ground and obstacle prefab categories; unique obstacles=" + obstacles.Count + "\n");
			names.AddRange(obstacles.Keys);
			names.Add("AC_enemy_1"); names.Add("MA_enemy_1");
			oldEnabled = EditorSettings.enterPlayModeOptionsEnabled; oldOptions = EditorSettings.enterPlayModeOptions;
			EditorSettings.enterPlayModeOptionsEnabled = true; EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
			EditorSceneManager.OpenScene("Assets/Scenes/CharacterSelection.unity");
			Application.logMessageReceived += OnLog; EditorApplication.update += Tick;
			timeout = EditorApplication.timeSinceStartup + 180;
			Time.captureFramerate = 60; EditorApplication.isPlaying = true;
		}
		catch (Exception e) {File.AppendAllText(report, e.ToString()); EditorApplication.Exit(1);}
	}
	static void Require(bool ok, string message) {if (!ok) throw new Exception(message);}
	static void OnLog(string message, string stack, LogType type)
	{
		if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) {errors++; File.AppendAllText(report, message + "\n" + stack + "\n");}
	}
	static void Tick()
	{
		try
		{
			if (EditorApplication.timeSinceStartup > timeout) throw new Exception("Timed out at " + phase + "/" + example);
			if (!EditorApplication.isPlaying) return;
			if (phase == 0)
			{
				var menu = UnityEngine.Object.FindObjectOfType<角色选择界面>();
				if (menu == null || Time.timeSinceLevelLoad < 0.2f) return;
				previews = new[] {menu.transform.Find("adventureGirlRun").GetComponent<Animator>(), menu.transform.Find("NinjaGirlRun").GetComponent<Animator>(), menu.transform.Find("RobotRun").GetComponent<Animator>()};
				BeginRole(); return;
			}
			if (phase == 1)
			{
				if (Time.time - started < 0.3f) return;
				string[] originalClips = { "adventureGitrrun", "girl2", "robotRun" };
				Require(preview.GetCurrentAnimatorClipInfo(0)[0].clip == AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/AnimationClip/" + originalClips[role] + ".anim"), "Original preview art changed");
				previewStart = preview.GetCurrentAnimatorStateInfo(0).normalizedTime;
				gameStart = game.GetCurrentAnimatorStateInfo(0).normalizedTime;
				previewSprites.Clear();
				started = Time.time; phase = 2; return;
			}
			if (phase == 2)
			{
				previewSprites.Add(preview.GetComponent<SpriteRenderer>().sprite);
				if (Time.time - started < 1f) return;
				float previewRate = (preview.GetCurrentAnimatorStateInfo(0).normalizedTime - previewStart) / (Time.time - started);
				float gameRate = (game.GetCurrentAnimatorStateInfo(0).normalizedTime - gameStart) / (Time.time - started);
				Require(Mathf.Abs(previewRate - gameRate) < 0.01f, "Animation playback rate mismatch");
				for (int i = 0; i < 3; i++) Require(previews[i].speed == (i == role ? 1f : 0f), "Selection freeze or playback changed");
				Require(previewSprites.Count > 3 && !previewSprites.Contains(null), "Preview did not animate or has missing sprites");
				File.AppendAllText(report, "PASS character " + role + ": original preview sprites preserved; measured preview/game cycles per sec=" + previewRate.ToString("F4") + "/" + gameRate.ToString("F4") + "\n");
				Capture("selection-" + role);
				UnityEngine.Object.Destroy(game.gameObject); role++;
				if (role < 3) {BeginRole(); return;}
				EventControl.ResetRun(); 角色选择界面.当前选择的角色序号 = 2;
				phase = 3; SceneManager.LoadScene("GameScene_AC"); return;
			}
			if (phase == 3)
			{
				if (SceneManager.GetActiveScene().name != "GameScene_AC" || !EventControl.isplay || PlayerControl.Instance == null) return;
				GroundControl.Instance.enabled = false;
				PlayerControl.Instance.GetComponent<Rigidbody2D>().simulated = false;
				started = Time.time; phase = 4; return;
			}
			if (phase == 4)
			{
				if (Time.time - started < 1.5f) return;
				BeginObstacle(); return;
			}
			if (phase == 5)
			{
				float age = Time.time - started;
				foreach (var pair in pieces) if (pair.Key != null)
				{
					float angle = Quaternion.Angle(pair.Value, pair.Key.transform.rotation);
					peakAngle = Mathf.Max(peakAngle, angle);
					Require(angle < 0.05f, "Fragment rotated during spread or absorption: " + angle);
				}
				if (creature && whole != null)
				{
					for (int i = 0; i < whole.transform.childCount; i++)
					{
						Require(Vector3.Distance(localPositions[i], whole.transform.GetChild(i).localPosition) < 0.0001f, "Creature body parts separated");
						Require(Quaternion.Angle(localRotations[i], whole.transform.GetChild(i).localRotation) < 0.05f, "Creature body parts rotated");
					}
				}
				if (age >= 0.15f && captured == 0)
				{
					Require(source == null, "Original obstacle still active");
					if (creature) Require(whole != null && whole.GetComponentsInChildren<MeshRenderer>().Length == 0 && whole.GetComponentsInChildren<Collider2D>().Length == 0, "Creature fractured or kept damage collision");
					Capture(names[example]); captured++;
					if (creature && !pauseChecked) {pauseChecked = true; UnityEngine.Object.FindObjectOfType<pause>().pauseBtn(); wholePosition = whole.transform.position; resume = EditorApplication.timeSinceStartup + 0.2; phase = 6; return;}
				}
				if (age < 1.4f) return;
				foreach (var pair in pieces) Require(pair.Key == null, "Fragment leaked after absorption");
				Require(whole == null, "Creature visual leaked after fade");
				File.AppendAllText(report, "PASS " + names[example] + ": " + (creature ? "intact fade; no shards, collision, detached parts or leaks" : "shattered; full-lifetime maximum rotation=" + peakAngle.ToString("F4")) + "\n");
				example++;
				if (example < names.Count) {BeginObstacle(); return;}
				Finish(); return;
			}
			if (phase == 6)
			{
				if (EditorApplication.timeSinceStartup < resume) return;
				Require(whole != null && Vector3.Distance(wholePosition, whole.transform.position) < 0.0001f, "Paused creature fade moved");
				UnityEngine.Object.FindObjectOfType<pause>().returnBtn(); phase = 5;
			}
		}
		catch (Exception e) {errors++; File.AppendAllText(report, e + "\n"); Finish();}
	}
	static void BeginRole()
	{
		角色选择界面.当前选择的角色序号 = role;
		preview = previews[role];
		game = new GameObject("actual gameplay animator").AddComponent<Animator>();
		game.gameObject.AddComponent<SpriteRenderer>(); game.transform.position = new Vector3(0f, 100f, 0f);
		game.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPaths[role]); game.cullingMode = AnimatorCullingMode.AlwaysAnimate;
		game.SetBool("isIdle", false); game.SetBool("isDie", false); game.SetInteger("action", 0); game.Play("run", 0, 0f);
		started = Time.time; phase = 1;
	}
	static void BeginObstacle()
	{
		string[] asset = obstacles[names[example]];
		GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(asset[0]);
		source = UnityEngine.Object.Instantiate(prefab.transform.Find(asset[1]).gameObject);
		source.name = names[example] + " orientation test";
		source.transform.position = new Vector3(example < obstacles.Count ? -2.8f : 0f, 0.05f, 0f);
		creature = source.GetComponent<ObstacleControl>().type == ObstacleControl.ObstacleType.Creature;
		GroundControl.BreakObject(source);
		whole = GameObject.Find(source.name + " whole");
		pieces.Clear(); peakAngle = 0f; captured = 0;
		foreach (PieceControl piece in UnityEngine.Object.FindObjectsOfType<PieceControl>()) pieces[piece] = piece.transform.rotation;
		if (creature)
		{
			Require(pieces.Count == 0 && whole != null, "Biological obstacle created fragments");
			localRotations = new Quaternion[whole.transform.childCount]; localPositions = new Vector3[whole.transform.childCount];
			for (int i = 0; i < whole.transform.childCount; i++) {localRotations[i] = whole.transform.GetChild(i).localRotation; localPositions[i] = whole.transform.GetChild(i).localPosition;}
		}
		else Require(pieces.Count > 0 && whole == null, "Nonbiological obstacle did not fracture");
		started = Time.time; phase = 5;
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
