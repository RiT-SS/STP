using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BarrierCollisionCheck
{
	static string folder, report, userPath, userBefore;
	static bool before, oldEnabled;
	static EnterPlayModeOptions oldOptions;
	static int phase, frame = -1, test, errors;
	static double timeout;
	static float started, startY, maxFoot;
	static bool jumpedTwice;
	static PlayerControl player;
	static Rigidbody2D body;
	static GameObject fixture;
	static BoxCollider2D beam, fence, floor;
	static readonly BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
	public static void Before() { before = true; Run(); }
	public static void Run()
	{
		folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../BarrierCollisionReview")); Directory.CreateDirectory(folder);
		report = Path.Combine(folder, before ? "before.txt" : "after.txt"); File.WriteAllText(report, "Actual player / MA_ground_3 collision check\n");
		userPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../../SaveData/game-records.json"));
		userBefore = File.Exists(userPath) ? File.ReadAllText(userPath) : null;
		typeof(GameRecords).GetField("filePath", flags).SetValue(null, Path.Combine(folder, "test-records.json"));
		typeof(GameRecords).GetField("history", flags).SetValue(null, null);
		typeof(GameRecords).GetField("current", flags).SetValue(null, null);
		oldEnabled = EditorSettings.enterPlayModeOptionsEnabled; oldOptions = EditorSettings.enterPlayModeOptions;
		EditorSettings.enterPlayModeOptionsEnabled = true; EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
		Application.logMessageReceived += OnLog; EditorApplication.update += Tick;
		EventControl.ResetRun(); 角色选择界面.当前选择的角色序号 = 0;
		EditorSceneManager.OpenScene("Assets/Scenes/GameScene_MA.unity");
		Time.captureFramerate = 50; timeout = EditorApplication.timeSinceStartup + 180; EditorApplication.isPlaying = true;
	}
	static void OnLog(string message, string stack, LogType type)
	{
		if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) {errors++; File.AppendAllText(report, message + "\n" + stack + "\n");}
	}
	static void Require(bool ok, string message) {if (!ok) throw new Exception(message);}
	static void Pass(string message) {File.AppendAllText(report, "PASS " + message + "\n");}
	static void Setup()
	{
		player = PlayerControl.Instance; body = player.GetComponent<Rigidbody2D>();
		GroundControl.Instance.enabled = false;
		foreach (var c in GroundControl.Instance.GetComponentsInChildren<Collider2D>()) c.enabled = false;
		foreach (var r in GroundControl.Instance.GetComponentsInChildren<Renderer>()) r.enabled = false;
		fixture = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/MA_ground_3.prefab"));
		fixture.transform.position = Vector3.zero;
		string suffix = test % 2 == 0 ? "" : " (1)";
		beam = fixture.transform.Find("MA_barrier_1" + suffix).GetComponent<BoxCollider2D>();
		fence = fixture.transform.Find("MA_enemy_3" + suffix).GetComponent<BoxCollider2D>();
		floor = fixture.GetComponent<BoxCollider2D>();
		foreach (var c in fixture.GetComponentsInChildren<Collider2D>()) c.enabled = c == beam || c == fence || c == floor;
		foreach (var r in fixture.GetComponentsInChildren<Renderer>()) r.enabled = r.gameObject == beam.gameObject || r.gameObject == fence.gameObject || r.gameObject == floor.gameObject;
		foreach (var c in fixture.GetComponentsInChildren<MonoBehaviour>()) c.enabled = false;
		Physics2D.SyncTransforms();
		bool mirrored = test % 2 == 1;
		fixture.transform.position += Vector3.left * (mirrored ? fence.bounds.max.x : fence.bounds.min.x);
		Physics2D.SyncTransforms();
		float half = player.box.size.x / 2f;
		float x = mirrored ? half + 0.012f : -half - 0.012f;
		PlayerControl.player_x = x;
		body.position = new Vector2(x, floor.bounds.max.y + player.box.size.y / 2f - player.box.offset.y + 0.015f);
		body.velocity = Vector2.zero;
		Physics2D.SyncTransforms(); started = Time.time; phase = 1;
		File.AppendAllText(report, "role=" + test / 2 + "; mirror=" + mirrored + "; beam=" + beam.bounds + "; fence=" + fence.bounds + "\n");
	}
	static void Tick()
	{
		try
		{
			if (EditorApplication.timeSinceStartup > timeout) throw new Exception("Timed out: " + phase + "/" + test);
			if (!EditorApplication.isPlaying || frame == Time.frameCount) return; frame = Time.frameCount;
			if (phase == 0)
			{
				if (!EventControl.isplay || PlayerControl.Instance == null) return;
				Setup(); return;
			}
			if (phase == 1)
			{
				if (Time.time - started < 0.2f) return;
				Require(PlayerControl.isGround, "Player did not settle on the floor");
				startY = body.position.y; maxFoot = player.box.bounds.min.y;
				Capture((before ? "before" : "after") + "-start-" + test);
				player.Jump(); started = Time.time; jumpedTwice = false; phase = 2; return;
			}
			if (phase == 2)
			{
				fixture.transform.position += Vector3.right * ((test % 2 == 0 ? -2f : 2f) * Time.deltaTime);
				Physics2D.SyncTransforms(); maxFoot = Mathf.Max(maxFoot, player.box.bounds.min.y);
				if (!jumpedTwice && Time.time - started >= 0.24f) {player.Jump(); jumpedTwice = true;}
				if (Time.time - started < 0.8f) return;
				Capture((before ? "before" : "after") + "-jump-" + test);
				float clearance = maxFoot - beam.bounds.max.y;
				File.AppendAllText(report, "max feet clearance above beam=" + clearance.ToString("F4") + "; player=" + body.position + "; hp=" + PlayerControl.Hp + "\n");
				Require(before ? clearance < -0.5f : clearance > 0.15f, before ? "Original snag did not reproduce" : "Player could not jump out past the seam");
				Pass((before ? "reproduced underside snag" : "escaped seam with normal double jump") + "; role=" + test / 2 + "; mirrored=" + (test % 2 == 1));
				if (before) {Finish(); return;}
				// Drop onto the stone top: its supporting surface must still work.
				body.position = new Vector2(beam.bounds.center.x, beam.bounds.max.y + 1f); body.velocity = Vector2.zero;
				PlayerControl.player_x = body.position.x;
				started = Time.time; phase = 3; return;
			}
			if (phase == 3)
			{
				if (Time.time - started < 0.65f) return;
				Require(PlayerControl.isGround && Mathf.Abs(player.box.bounds.min.y - beam.bounds.max.y) < 0.04f, "Stone top no longer supports player");
				Require(PlayerControl.Hp == 3, "Jumping out or standing on the stone caused unexpected damage");
				Pass("stone remains solid from above");
				beam.enabled = false;
				body.position = new Vector2(fence.bounds.center.x, fence.bounds.max.y + 1f); body.velocity = Vector2.zero;
				PlayerControl.player_x = body.position.x;
				started = Time.time; phase = 4; return;
			}
			if (phase == 4)
			{
				if (Time.time - started < 0.6f) return;
				Require(PlayerControl.Hp == 2, "Fence spikes no longer deal damage"); Pass("fence still damages from above");
				if (++test >= 6) {Finish(); return;}
				EventControl.ResetRun(); 角色选择界面.当前选择的角色序号 = test / 2; phase = 0;
				UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene_MA");
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
		if (userBefore != (File.Exists(userPath) ? File.ReadAllText(userPath) : null)) {errors++; File.AppendAllText(report, "Actual game records changed\n");}
		Time.captureFramerate = 0; Time.timeScale = 1f;
		EditorSettings.enterPlayModeOptionsEnabled = oldEnabled; EditorSettings.enterPlayModeOptions = oldOptions;
		EditorApplication.update -= Tick; Application.logMessageReceived -= OnLog;
		File.AppendAllText(report, "FINISHED errors=" + errors + "\n"); EditorApplication.Exit(errors == 0 ? 0 : 1);
	}
}
