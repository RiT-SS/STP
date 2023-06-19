using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RebirthVisibilityCheck
{
	static readonly string[] scenes = {"GameScene_AC", "GameScene_WW", "GameScene_MA"};
	static readonly string[] causes = {"damage", "fall", "blackhole", "trap-fatal", "trap-layer", "offscreen-fall", "high-damage"};
	static readonly BindingFlags fields = BindingFlags.NonPublic | BindingFlags.Instance;
	static string folder, report, userPath, userBefore;
	static bool before, oldEnabled, captured;
	static EnterPlayModeOptions oldOptions;
	static int stage, phase, errors, frame = -1, effectFrames, visibleFrames, fullFrames, trapOrder, trapLayer;
	static double timeout;
	static float started, restored, initialY;
	static Vector3 startPosition, previousPosition;
	static float lastProgress, maxStep, maxAlignmentError, nextCapture, previousSampleTime, previousDelta;
	static int captureCount;
	static PlayerControl player;
	static Rigidbody2D body;
	static RigidbodyConstraints2D constraints;
	static BoxCollider2D floor;
	static GameObject lethal, enemy;
	static int Scene {get {return before ? 1 : Mathf.Min(stage / 3, 2);}}
	static int Cause {get {return before ? stage : stage < 9 ? stage % 3 : stage - 6;}}
	static bool Reviving {get {return (bool)typeof(PlayerControl).GetField("reflag", fields).GetValue(player);}}
	static Animator Effect {get {return (Animator)typeof(PlayerControl).GetField("rebirthAnimator", fields).GetValue(player);}}
	static void Require(bool ok, string message) {if (!ok) throw new Exception(message);}
	public static void Before() {before = true; Run();}
	public static void Trap() {stage = 9; Run();}
	public static void TrapLayer() {stage = 10; Run();}
	public static void Offscreen() {stage = 11; Run();}
	public static void High() {stage = 12; Run();}
	public static void Run()
	{
		folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../RebirthMotionReview")); Directory.CreateDirectory(folder);
		report = Path.Combine(folder, before ? "before.txt" : stage == 9 ? "trap.txt" : stage == 10 ? "trap-layer.txt" : stage == 11 ? "offscreen.txt" : stage == 12 ? "high.txt" : "after.txt"); File.WriteAllText(report, "Revival motion, visible alignment and landing\n");
		userPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../../SaveData/game-records.json")); userBefore = File.Exists(userPath) ? File.ReadAllText(userPath) : null;
		var flags = BindingFlags.NonPublic | BindingFlags.Static;
		typeof(GameRecords).GetField("filePath", flags).SetValue(null, Path.Combine(folder, "test-records-" + DateTime.UtcNow.Ticks + ".json"));
		typeof(GameRecords).GetField("history", flags).SetValue(null, null);
		typeof(GameRecords).GetField("current", flags).SetValue(null, null);
		oldEnabled = EditorSettings.enterPlayModeOptionsEnabled; oldOptions = EditorSettings.enterPlayModeOptions;
		EditorSettings.enterPlayModeOptionsEnabled = true; EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
		EventControl.ResetRun(); 角色选择界面.当前选择的角色序号 = 0;
		EditorSceneManager.OpenScene("Assets/Scenes/" + scenes[Scene] + ".unity");
		Application.logMessageReceived += OnLog; EditorApplication.update += Tick;
		Time.captureFramerate = 60; timeout = EditorApplication.timeSinceStartup + 240; EditorApplication.isPlaying = true;
	}
	static void OnLog(string message, string stack, LogType type)
	{
		if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) {errors++; File.AppendAllText(report, message + "\n" + stack + "\n");}
	}
	static void Setup()
	{
		Time.captureFramerate = 60;
		player = PlayerControl.Instance; body = player.GetComponent<Rigidbody2D>(); constraints = body.constraints;
		GroundControl.Instance.enabled = false;
		foreach (var c in UnityEngine.Object.FindObjectsOfType<Collider2D>()) if (c.gameObject != player.gameObject) c.enabled = false;
		foreach (var item in UnityEngine.Object.FindObjectsOfType<ItemControl>()) item.enabled = false;
		var obj = new GameObject("test landing floor"); obj.tag = "Ground"; obj.transform.position = new Vector3(0f, -1.2f, 0f);
		floor = obj.AddComponent<BoxCollider2D>(); floor.size = new Vector2(20f, 0.2f);
		player.transform.position = new Vector3(PlayerControl.player_x, floor.bounds.max.y + player.box.size.y / 2f - player.box.offset.y + 0.02f, 0f);
		body.velocity = Vector2.zero; PlayerControl.Hp = Cause == 0 || Cause == 3 || Cause == 6 ? 1 : 3; PlayerControl.isShield = false; SkillControl.isEffect = true;
		if (Cause == 0 || Cause == 6)
		{
			if (Cause == 6) player.transform.position = new Vector3(PlayerControl.player_x, 2.2f, 0f);
			Physics2D.SyncTransforms(); Vector3 death = player.transform.position;
			body.velocity = new Vector2(0f, 6.5f); player.Damage();
			Require(Vector3.Distance(death, player.transform.position) < 0.00001f, "Revival teleported at start");
		}
		else if (Cause == 1 || Cause == 5)
		{
			// A real Die collision while using the short sliding collider.
			if (Cause == 5) player.transform.position = new Vector3(PlayerControl.player_x, -2.2f, 0f);
			player.Slide(); body.velocity = Vector2.down * 12f;
			lethal = new GameObject("test death area"); lethal.tag = "Die"; lethal.transform.position = player.transform.position;
			lethal.AddComponent<BoxCollider2D>().size = Cause == 5 ? new Vector2(2f, 2f) : new Vector2(0.3f, 0.3f); Physics2D.SyncTransforms();
		}
		else if (Cause == 2) player.transform.position = new Vector3(-2.24f, player.transform.position.y, 0f);
		else
		{
			foreach (var prefab in GroundControl.Instance.GroundPrefabs)
			{
				foreach (Transform child in prefab.GetComponentsInChildren<Transform>())
				{
					if (!child.name.Contains("MA_enemy_4")) continue;
					lethal = UnityEngine.Object.Instantiate(child.gameObject); break;
				}
				if (lethal != null) break;
			}
			Require(lethal != null, "Native trap prefab not found");
			var render = lethal.GetComponent<SpriteRenderer>(); trapOrder = render.sortingOrder; trapLayer = render.sortingLayerID;
			Physics2D.SyncTransforms(); lethal.transform.position += player.box.bounds.center - lethal.GetComponent<Collider2D>().bounds.center; Physics2D.SyncTransforms();
		}
		started = Time.time; phase = Cause == 4 ? 4 : 1;
	}
	static void Tick()
	{
		try
		{
			if (EditorApplication.timeSinceStartup > timeout) throw new Exception("Timed out: " + stage + "/" + phase);
			if (!EditorApplication.isPlaying || frame == Time.frameCount) return; frame = Time.frameCount;
			if (phase == 0)
			{
				if (SceneManager.GetActiveScene().name != scenes[Scene] || !EventControl.isplay || PlayerControl.Instance == null) return;
				Setup(); return;
			}
			if (phase == 1)
			{
				if (Cause == 3 && !Reviving && PlayerControl.isTrap && enemy == null)
				{
					var render = lethal.GetComponent<SpriteRenderer>(); Require(render.sortingLayerID == player.myRender.sortingLayerID && render.sortingOrder == player.myRender.sortingOrder + 1, "Held trap does not cover character");
					enemy = new GameObject("test fatal enemy"); enemy.tag = "enemy";
					enemy.transform.position = player.box.bounds.center; enemy.AddComponent<BoxCollider2D>().size = new Vector2(0.25f, 0.25f); Physics2D.SyncTransforms();
				}
				if (!Reviving) {Require(Time.time - started < 0.6f, "Death did not enter revival"); return;}
				if (!before && Cause == 3) Require(!PlayerControl.isTrap, "Fatal trap collision overwrote revival state");
				if (Cause == 3) {var render = lethal.GetComponent<SpriteRenderer>(); Require(render.sortingLayerID == trapLayer && render.sortingOrder == trapOrder, "Revival did not restore trap rendering order");}
				if (lethal != null) UnityEngine.Object.Destroy(lethal);
				if (enemy != null) UnityEngine.Object.Destroy(enemy);
				Physics2D.SyncTransforms();
				started = Time.time; initialY = body.position.y; captured = false; effectFrames = visibleFrames = fullFrames = 0; phase = 2;
				startPosition = (Vector3)typeof(PlayerControl).GetField("rebirthStart", fields).GetValue(player);
				if (Cause == 5) Require(startPosition.y < Camera.main.transform.position.y - Camera.main.orthographicSize, "Offscreen fall fixture did not start below the camera");
				if (Cause == 6) Require(startPosition.y > Camera.main.transform.position.y + Camera.main.orthographicSize, "High death fixture did not start above the camera");
				previousPosition = player.transform.position; lastProgress = 0f; maxStep = maxAlignmentError = 0f;
				previousSampleTime = Time.time; previousDelta = Time.deltaTime;
				captureCount = 0; nextCapture = started;
				Capture("start-" + stage);
			}
			if (phase == 2)
			{
				if (Reviving)
				{
					var render = Effect.GetComponent<SpriteRenderer>(); Bounds b = render.bounds; var camera = Camera.main;
					float low = camera.transform.position.y - camera.orthographicSize, high = camera.transform.position.y + camera.orthographicSize;
					effectFrames++;
					if (b.max.y >= low && b.min.y <= high) visibleFrames++;
					if (b.min.y >= low && b.max.y <= high) fullFrames++;
					if (!before)
					{
						Vector3 target = new Vector3(PlayerControl.player_x, 1f, startPosition.z), delta = target - startPosition, current = player.transform.position;
						float progress = Vector3.Dot(current - startPosition, delta) / delta.sqrMagnitude;
						Require(progress >= lastProgress - 0.001f && progress <= 1.001f && Vector3.Distance(current, startPosition + delta * progress) < 0.01f, "Revival path reversed, overshot or left its path");
						float step = Vector3.Distance(current, previousPosition); maxStep = Mathf.Max(step, maxStep);
						float sampleTime = Mathf.Max(Time.time - previousSampleTime, Time.deltaTime) + previousDelta;
						Require(step <= delta.magnitude * 1.7f / 0.9166667f * sampleTime + 0.025f, "Position jumped during revival: " + step + "; sample interval=" + sampleTime);
						Require(body.velocity.sqrMagnitude < 0.001f, "Physics velocity leaked into revival motion");
						float error = Mathf.Abs(render.bounds.center.x - player.myRender.bounds.center.x); maxAlignmentError = Mathf.Max(maxAlignmentError, error);
						Require(error < 0.06f, "Effect and character visible centers diverged: " + error);
						if (Effect.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.88f) Require(b.min.y >= low && b.max.y <= high, "Revival ended outside the camera");
						lastProgress = progress; previousPosition = current;
						previousSampleTime = Time.time; previousDelta = Time.deltaTime;
					}
					if (stage == 5 && Time.time >= nextCapture) {Capture("frame-" + captureCount.ToString("D3")); captureCount++; nextCapture += 1f / 30f;}
					if (!captured && Time.time - started > 0.45f) {Capture("effect-" + stage); captured = true;}
					Require(Time.time - started < 1.2f, "Revival animation never ended"); return;
				}
				restored = Time.time; float duration = restored - started;
				Require(PlayerControl.Hp == 1 && PlayerControl.isShield && player.box == player.box_1 && player.box_1.enabled && !player.box_2.enabled && !SkillControl.isEffect, "Revival restored incorrect health, protection, collider or cooldown");
				if (!before)
				{
					Require(duration > 0.83f && duration < 1.02f && body.constraints == constraints, "Revival timing or constraints changed");
					Require(Vector3.Distance(player.transform.position, new Vector3(PlayerControl.player_x, 1f, startPosition.z)) < 0.05f, "Revival did not arrive at target before falling");
					Require(!(bool)typeof(PlayerControl).GetField("isSlide", fields).GetValue(player) && !PlayerControl.isTrap, "Revival kept slide/trap state");
				}
				File.AppendAllText(report, scenes[Scene] + "/" + causes[Cause] + ": start y=" + initialY.ToString("F3") + ", animation=" + duration.ToString("F3") + "s, release y=" + body.position.y.ToString("F3") + ", visible frames=" + visibleFrames + "/" + effectFrames + ", fully visible=" + fullFrames + "\n");
				File.AppendAllText(report, "PASS continuous motion from " + startPosition + "; maximum frame movement=" + maxStep.ToString("F4") + "; maximum visible-center difference=" + maxAlignmentError.ToString("F4") + "\n");
				Capture("released-" + stage); phase = 3; return;
			}
			if (phase == 3)
			{
				if (stage == 5 && Time.time >= nextCapture) {Capture("frame-" + captureCount.ToString("D3")); captureCount++; nextCapture += 1f / 30f;}
				Require(PlayerControl.Hp == 1 && !player.deathPanel.activeSelf, "Died before landing");
				if (!PlayerControl.isGround || Mathf.Abs(player.box.bounds.min.y - floor.bounds.max.y) > 0.04f) {Require(Time.time - restored < 2f, "Did not land after revival"); return;}
				File.AppendAllText(report, "PASS landed after " + (Time.time - started).ToString("F3") + "s; post-animation fall=" + (Time.time - restored).ToString("F3") + "s\n");
				if (!before)
				{
					Require(visibleFrames > effectFrames / 2 && Time.time - restored < 0.9f, "Effect did not become visible in time or fall took too long");
					PlayerControl.isShield = false; player.Damage(); Require(PlayerControl.Hp == 0 && player.deathPanel.activeSelf && !Reviving, "Cooldown allowed a second resurrection");
				}
				if (++stage >= (before ? 2 : 10)) {Finish(); return;}
				EventControl.ResetRun(); phase = 0; SceneManager.LoadScene(scenes[Scene]);
			}
			if (phase == 4)
			{
				if (!PlayerControl.isTrap) {Require(Time.time - started < 0.6f, "Native trap did not capture player"); return;}
				var render = lethal.GetComponent<SpriteRenderer>();
				Require(render.sortingLayerID == player.myRender.sortingLayerID && render.sortingOrder == player.myRender.sortingOrder + 1, "Closed trap is not one order above player");
				if (Time.time - started < 0.2f) return;
				Capture("trap-held"); File.AppendAllText(report, "PASS trapped: trap order=" + render.sortingOrder + "; player order=" + player.myRender.sortingOrder + "\n"); phase = 5; return;
			}
			if (phase == 5)
			{
				if (PlayerControl.isTrap) {Require(Time.time - started < 0.8f, "Trap did not release player"); return;}
				var render = lethal.GetComponent<SpriteRenderer>();
				Require(render.sortingLayerID == trapLayer && render.sortingOrder == trapOrder && !lethal.GetComponent<BoxCollider2D>().enabled && PlayerControl.Hp == 3, "Normal release did not restore rendering/collider state");
				File.AppendAllText(report, "PASS released normally: original trap layer/order restored; health unchanged\n"); Finish();
			}
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
			File.WriteAllBytes(Path.Combine(folder, (before ? "before-" : "after-") + name + ".png"), image.EncodeToPNG()); UnityEngine.Object.Destroy(image);
		}
		finally {camera.targetTexture = old; RenderTexture.active = active; RenderTexture.ReleaseTemporary(target);}
	}
	static void Finish()
	{
		if (userBefore != (File.Exists(userPath) ? File.ReadAllText(userPath) : null)) {errors++; File.AppendAllText(report, "Actual history changed\n");}
		Time.captureFramerate = 0; Time.timeScale = 1f;
		EditorSettings.enterPlayModeOptionsEnabled = oldEnabled; EditorSettings.enterPlayModeOptions = oldOptions;
		EditorApplication.update -= Tick; Application.logMessageReceived -= OnLog;
		File.AppendAllText(report, "FINISHED errors=" + errors + "\n"); EditorApplication.Exit(errors == 0 ? 0 : 1);
	}
}
