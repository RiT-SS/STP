using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RageFollowCheck
{
	static string folder, report, userPath, userBefore;
	static int phase, stage, frame = -1, errors, captured;
	static bool oldEnabled, measuring, oldRage;
	static EnterPlayModeOptions oldOptions;
	static double timeout, resume;
	static float started, previousY, maxStep, minY, maxY, nextCapture;
	static Vector3 pausedPosition;
	static float pausedFill;
	static GameObject floor;
	static PlayerControl player;
	static SkillControl skills;
	static Rigidbody2D body;
	static RigidbodyConstraints2D constraints;
	static readonly string[] scenes = {"GameScene_AC", "GameScene_WW", "GameScene_MA"};

	static void IsolateHistory()
	{
		folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../RageFollowReview")); Directory.CreateDirectory(folder);
		var flags = BindingFlags.Static | BindingFlags.NonPublic;
		typeof(GameRecords).GetField("filePath", flags).SetValue(null, Path.Combine(folder, "test-records.json"));
		typeof(GameRecords).GetField("history", flags).SetValue(null, null);
		typeof(GameRecords).GetField("current", flags).SetValue(null, null);
	}
	public static void Regression()
	{
		IsolateHistory(); DashLootCheck.Run();
	}
	public static void Run()
	{
		IsolateHistory(); report = Path.Combine(folder, "test.txt"); File.WriteAllText(report, "Terrain-following rage and 3 second duration\n");
		userPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../../SaveData/game-records.json"));
		userBefore = File.Exists(userPath) ? File.ReadAllText(userPath) : null;
		oldEnabled = EditorSettings.enterPlayModeOptionsEnabled; oldOptions = EditorSettings.enterPlayModeOptions;
		EditorSettings.enterPlayModeOptionsEnabled = true; EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
		EventControl.ResetRun(); 角色选择界面.当前选择的角色序号 = 2;
		EditorSceneManager.OpenScene("Assets/Scenes/GameScene_AC.unity");
		Application.logMessageReceived += OnLog; EditorApplication.update += Tick;
		timeout = EditorApplication.timeSinceStartup + 180; EditorApplication.isPlaying = true;
	}
	static void OnLog(string message, string stack, LogType type)
	{
		if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) {errors++; File.AppendAllText(report, message + "\n" + stack + "\n");}
	}
	static void Require(bool ok, string message) {if (!ok) throw new Exception(message);}
	static void Pass(string message) {File.AppendAllText(report, "PASS " + message + "\n");}
	static bool Ready(string name) {return SceneManager.GetActiveScene().name == name && EventControl.isplay && PlayerControl.Instance != null;}
	static float Target() {return floor.GetComponent<BoxCollider2D>().bounds.max.y + player.box.size.y / 2f - player.box.offset.y + 0.05f;}
	static void Tick()
	{
		try
		{
			if (EditorApplication.timeSinceStartup > timeout) throw new Exception("Timed out: " + phase + "/" + stage);
			if (!EditorApplication.isPlaying || frame == Time.frameCount) return;
			frame = Time.frameCount;
			if (measuring && (PlayerControl.isRage || oldRage) && !EventControl.isPaused)
			{
				float step = Mathf.Abs(player.transform.position.y - previousY);
				maxStep = Mathf.Max(maxStep, step);
				Require(step <= 8f * Time.deltaTime + 0.03f, "Vertical jump while following or ending rage: " + step + "; dt=" + Time.deltaTime);
			}
			if (player != null) {previousY = player.transform.position.y; oldRage = PlayerControl.isRage;}
			if (phase == 0)
			{
				if (!Ready("GameScene_AC")) return;
				player = PlayerControl.Instance; skills = SkillControl.Instance; body = player.GetComponent<Rigidbody2D>(); constraints = body.constraints;
				Require(skills.ltTime == 3f && skills.cdTime == 40f, "Duration or cooldown config incorrect");
				GroundControl.Instance.enabled = false;
				foreach (Collider2D collider in GroundControl.Instance.GetComponentsInChildren<Collider2D>()) collider.enabled = false;
				floor = new GameObject("test follow platform"); floor.tag = "Ground"; floor.transform.SetParent(GroundControl.Instance.transform);
				floor.transform.position = new Vector3(PlayerControl.player_x + 3f, -0.65f, 0f);
				floor.AddComponent<BoxCollider2D>().size = new Vector2(20f, 0.3f);
				body.position = new Vector2(PlayerControl.player_x, 1f); body.velocity = Vector2.zero;
				Physics2D.SyncTransforms(); float before = player.transform.position.y;
				skills.ltTime = 30f; skills.Rage(); Require(PlayerControl.isRage && player.transform.position.y == before, "Starting rage changed height immediately");
				previousY = before; oldRage = true; measuring = true; started = Time.time; phase = 1; return;
			}
			if (phase == 1)
			{
				if (Time.time - started < 0.6f) return;
				Require(Mathf.Abs(player.transform.position.y - Target()) < 0.03f, "Did not descend to initial platform");
				floor.transform.position += Vector3.up * 0.8f; Physics2D.SyncTransforms();
				Vector3 before = player.transform.position; Require(!player.EndRage() && Vector3.Distance(before, player.transform.position) < 0.00001f, "Unaligned release snapped upward");
				started = Time.time; phase = 2; return;
			}
			if (phase == 2)
			{
				if (Time.time - started < 0.6f) return;
				Require(PlayerControl.isRage && Mathf.Abs(player.transform.position.y - Target()) < 0.03f, "Did not follow raised platform");
				floor.transform.position -= Vector3.up * 1.2f; Physics2D.SyncTransforms();
				Vector3 before = player.transform.position; Require(!player.EndRage() && Vector3.Distance(before, player.transform.position) < 0.00001f, "Unaligned release did not wait for descent");
				started = Time.time; phase = 3; return;
			}
			if (phase == 3)
			{
				if (Time.time - started < 0.65f) return;
				Require(PlayerControl.isRage && Mathf.Abs(player.transform.position.y - Target()) < 0.03f, "Did not follow lowered platform");
				player.Damage(); Require(PlayerControl.Hp == 3, "Following rage lost invulnerability");
				UnityEngine.Object.FindObjectOfType<pause>().pauseBtn(); pausedPosition = player.transform.position; pausedFill = SkillControl.lt.fillAmount;
				resume = EditorApplication.timeSinceStartup + 0.25; phase = 4; return;
			}
			if (phase == 4)
			{
				if (EditorApplication.timeSinceStartup < resume) return;
				Require(Vector3.Distance(pausedPosition, player.transform.position) < 0.00001f && SkillControl.lt.fillAmount == pausedFill, "Paused follow or timer continued");
				UnityEngine.Object.FindObjectOfType<pause>().returnBtn();
				Vector3 before = player.transform.position; SkillControl.lt.fillAmount = 0f;
				Require(player.EndRage() && Vector3.Distance(before, player.transform.position) < 0.00001f, "Ending aligned rage changed position");
				Require(body.constraints == constraints && body.gravityScale > 0f, "Release did not restore physics");
				started = Time.time; phase = 5; return;
			}
			if (phase == 5)
			{
				if (Time.time - started < 0.5f) return;
				Require(PlayerControl.Hp == 3 && PlayerControl.isGround && GroundControl.Speed == 2f && !PlayerControl.isRage, "Landing or speed restore failed");
				skills.Rage(); Require(!PlayerControl.isRage, "Cooldown was bypassed");
				Pass("controlled up/down platforms, no snap at start or release, invulnerability, pause, gravity/constraints restored, cooldown; maximum frame movement=" + maxStep.ToString("F4"));
				measuring = false; EventControl.ResetRun(); UnityEngine.Random.InitState(91); phase = 6; SceneManager.LoadScene(scenes[stage]); return;
			}
			if (phase == 6)
			{
				if (!Ready(scenes[stage])) return;
				player = PlayerControl.Instance; skills = SkillControl.Instance; body = player.GetComponent<Rigidbody2D>();
				Require(skills.ltTime == 3f && skills.cdTime == 40f, "Native scene did not use 3 seconds");
				typeof(GroundControl).GetField("piece_need", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(GroundControl.Instance, 100000);
				Time.captureFramerate = 60; skills.Rage(); Require(PlayerControl.isRage, "Natural rage failed to start");
				minY = maxY = previousY = player.transform.position.y; maxStep = 0f; measuring = oldRage = true; captured = 0;
				started = nextCapture = Time.time; phase = 7; return;
			}
			if (phase == 7)
			{
				Require(PlayerControl.Instance != null && PlayerControl.Hp > 0 && !player.deathPanel.activeSelf, "Died during following rage or release");
				minY = Mathf.Min(minY, player.transform.position.y); maxY = Mathf.Max(maxY, player.transform.position.y);
				if (stage == 1 && Time.time >= nextCapture) {Capture("frame-" + captured.ToString("D3")); captured++; nextCapture += 1f / 24f;}
				if (PlayerControl.isRage)
				{
					Require(PlayerControl.Hp == 3 && Time.time - started < 7f, "Rage damage protection or safe release failed"); return;
				}
				float duration = Time.time - started; Require(duration >= 2.95f && duration < 7f, "Wrong duration");
				Require(maxY - minY > 0.1f, "Height stayed fixed in natural terrain");
				Pass(scenes[stage] + ": base=3s; actual duration=" + duration.ToString("F2") + "; vertical range=" + (maxY - minY).ToString("F3") + "; max frame movement=" + maxStep.ToString("F4") + "; released without teleport");
				Capture("end-" + stage); PlayerControl.isShield = true; skills.shieldTime.fillAmount = 0f; started = Time.time; phase = 8; return;
			}
			if (phase == 8)
			{
				Require(PlayerControl.Instance != null && PlayerControl.Hp > 0 && body.gravityScale > 0f && !player.deathPanel.activeSelf, "Post-rage fall failed");
				if (stage == 1 && Time.time >= nextCapture) {Capture("frame-" + captured.ToString("D3")); captured++; nextCapture += 1f / 24f;}
				if (Time.time - started < 0.6f) return;
				Pass(scenes[stage] + ": continued normally after landing"); measuring = false;
				if (++stage < scenes.Length) {EventControl.ResetRun(); UnityEngine.Random.InitState(91 + stage); phase = 6; SceneManager.LoadScene(scenes[stage]); return;}
				Require(userBefore == (File.Exists(userPath) ? File.ReadAllText(userPath) : null), "Test changed actual history"); Finish();
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
