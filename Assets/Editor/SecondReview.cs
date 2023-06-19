using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SecondReview
{
	class Sample
	{
		public PieceControl piece;
		public float born, distance, scale, bornDistance;
		public Vector3 position;
		public bool zero;
	}
	static Dictionary<int, Sample> samples = new Dictionary<int, Sample>();
	static int phase, errors, captures, born, zero, vanished, near;
	static int[] bins = new int[4];
	static float start, nextCapture;
	static double timeout;
	static bool oldEnabled;
	static EnterPlayModeOptions oldOptions;
	static string folder, output;
	static PlayerControl player;
	static GameObject deathPanel, testPlatform, testShell;
	static Rigidbody2D body;
	static StringBuilder csv = new StringBuilder("time,id,x,y,distance,scale\n");

	public static void Run()
	{
		folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../SecondReview"));
		Directory.CreateDirectory(folder);
		output = Path.Combine(folder, "review.txt");
		File.WriteAllText(output, "Second review " + Application.unityVersion + "\n");
		oldEnabled = EditorSettings.enterPlayModeOptionsEnabled;
		oldOptions = EditorSettings.enterPlayModeOptions;
		EditorSettings.enterPlayModeOptionsEnabled = true;
		EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
		EditorSceneManager.OpenScene("Assets/Scenes/GameScene_AC.unity");
		角色选择界面.当前选择的角色序号 = 1;
		PlayerControl.Hp = 3;
		EventControl.Route = 0f;
		Application.logMessageReceived += OnLog;
		EditorApplication.update += Tick;
		timeout = EditorApplication.timeSinceStartup + 80;
		Time.captureFramerate = 60;
		EditorApplication.isPlaying = true;
	}
	static void OnLog(string message, string stack, LogType type)
	{
		if (type == LogType.Error || type == LogType.Exception)
		{
			errors++;
			File.AppendAllText(output, "RUNTIME " + message + "\n" + stack + "\n");
		}
	}
	static void Tick()
	{
		try
		{
			if (EditorApplication.timeSinceStartup > timeout) throw new Exception("Review timeout phase " + phase);
			if (!EditorApplication.isPlaying) return;
			if (phase == 0)
			{
				if (PlayerControl.Instance == null || !EventControl.isplay) return;
				player = PlayerControl.Instance;
				deathPanel = player.deathPanel;
				body = player.GetComponent<Rigidbody2D>();
				body.simulated = false;
				player.transform.position = new Vector3(PlayerControl.player_x, 3f, 0f);
				PlayerControl.isShield = true;
				start = Time.time;
				nextCapture = start + 2f;
				var hole = GameObject.Find("darkhole").GetComponent<SpriteRenderer>();
				var ground = GameObject.Find("GroundControl").transform.GetChild(0).GetComponent<SpriteRenderer>();
				File.AppendAllText(output, "Sorting source ground=" + ground.sortingOrder + "; hole=" + hole.sortingOrder + "; hole position=" + hole.transform.position + "\n");
				phase++; return;
			}
			if (phase == 1)
			{
				TrackFragments();
				if (Time.time >= nextCapture && captures < 16)
				{
					Capture(); captures++; nextCapture += 0.15f;
				}
				if (Time.time - start < 10f) return;
				File.WriteAllText(Path.Combine(folder, "fragments.csv"), csv.ToString());
				File.AppendAllText(output, "Fragments observed=" + born + "; vanished=" + vanished + "; reached zero scale=" + zero + "; last sample within .35 of center=" + near + "; distance at zero [<.3,<.6,<1,>=1]=" + string.Join(",", bins) + "\n");
				UnityEngine.Object.FindObjectOfType<GroundControl>().enabled = false;
				PlayerControl.isShield = false;
				// 测试平台只在内存中存在，不保存场景。
				player.transform.position = new Vector3(PlayerControl.player_x, 3f, 0f);
				body.simulated = true;
				Physics2D.SyncTransforms();
				testPlatform = new GameObject("review platform");
				testPlatform.tag = "Ground";
				testPlatform.transform.position = new Vector3(player.transform.position.x, player.box_1.bounds.min.y - 0.15f, 0f);
				testPlatform.AddComponent<BoxCollider2D>().size = new Vector2(2f, 0.2f);
				start = Time.time; phase++; return;
			}
			if (phase == 2)
			{
				if (Time.time - start < 0.5f) return;
				File.AppendAllText(output, "Jump baseline grounded=" + PlayerControl.isGround + "\n");
				EventControl.isplay = true;
				player.Jump();
				start = Time.time; phase++; return;
			}
			if (phase == 3)
			{
				if (Time.time - start < 0.15f) return;
				player.Jump();
				start = Time.time; phase++; return;
			}
			if (phase == 4)
			{
				if (Time.time - start < 0.15f) return;
				float before = body.velocity.y;
				player.Jump();
				File.AppendAllText(output, "Third jump airborne: velocity before=" + before + "; after=" + body.velocity.y + "; blocked=" + Mathf.Approximately(before, body.velocity.y) + "\n");
				body.simulated = false;
				PlayerControl.Hp = 3;
				player.transform.position = new Vector3(PlayerControl.player_x, 3f, 0f);
				PlayerControl.isShield = false;
				testShell = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/shell.prefab"));
				testShell.transform.position = player.transform.position + new Vector3(player.box.offset.x - 0.05f, player.box.offset.y, 0f);
				File.AppendAllText(output, "Centered shell test: player box=" + player.box.size + "; shell at=" + testShell.transform.position + "\n");
				start = Time.time; phase++; return;
			}
			if (phase == 5)
			{
				if (Time.time - start < 0.15f) return;
				File.AppendAllText(output, "Centered shell overlapping player: HP=" + PlayerControl.Hp + "; impact animation=" + testShell.GetComponent<Animator>().enabled + "\n");
				UnityEngine.Object.Destroy(testShell);
				PlayerControl.Hp = 3;
				player.transform.position = new Vector3(-3.3f, 3f, 0f);
				start = Time.time; phase++; return;
			}
			if (phase == 6)
			{
				if (Time.time - start < 0.1f) return;
				File.AppendAllText(output, "Skipped black-hole approach band: player exists=" + (PlayerControl.Instance != null) + "; HP=" + PlayerControl.Hp + "; death panel=" + deathPanel.activeSelf + "\n");
				Finish();
			}
		}
		catch (Exception e) { File.AppendAllText(output, e + "\n"); Finish(); }
	}
	static void TrackFragments()
	{
		Vector3 center = new Vector3(-3.42f, 0.06f, 0f);
		foreach (var piece in UnityEngine.Object.FindObjectsOfType<PieceControl>())
		{
			int id = piece.GetInstanceID();
			if (!samples.TryGetValue(id, out Sample s))
			{
				s = new Sample {piece = piece, born = Time.time}; samples[id] = s; born++;
				s.bornDistance = Vector2.Distance(center, piece.transform.position);
				if (born <= 3) File.AppendAllText(output, "Fragment sorting=" + piece.GetComponent<Renderer>().sortingOrder + "; initial scale=" + piece.transform.localScale + "; born distance=" + s.bornDistance + "\n");
			}
			s.position = piece.transform.position;
			s.distance = Vector2.Distance(center, s.position);
			s.scale = piece.transform.localScale.x;
			if (s.scale <= 0.001f && !s.zero)
			{
				s.zero = true; zero++;
				bins[s.distance < .3f ? 0 : s.distance < .6f ? 1 : s.distance < 1f ? 2 : 3]++;
			}
			if (born < 80) csv.AppendLine(Time.time + "," + id + "," + s.position.x + "," + s.position.y + "," + s.distance + "," + s.scale);
		}
		var remove = new List<int>();
		foreach (var entry in samples)
			if (entry.Value.piece == null)
			{
				vanished++;
				if (entry.Value.distance <= 0.35f) near++;
				remove.Add(entry.Key);
			}
		foreach (int key in remove) samples.Remove(key);
	}
	static void Capture()
	{
		Camera camera = Camera.main;
		var target = RenderTexture.GetTemporary(1400, 560, 24);
		var active = RenderTexture.active;
		var oldTarget = camera.targetTexture;
		try
		{
			camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
			var texture = new Texture2D(1400, 560, TextureFormat.RGB24, false);
			texture.ReadPixels(new Rect(0, 0, 1400, 560), 0, 0); texture.Apply();
			File.WriteAllBytes(Path.Combine(folder, "frame-" + captures.ToString("D2") + ".png"), texture.EncodeToPNG());
			UnityEngine.Object.Destroy(texture);
		}
		finally {camera.targetTexture = oldTarget; RenderTexture.active = active; RenderTexture.ReleaseTemporary(target);}
	}
	static void Finish()
	{
		Time.captureFramerate = 0;
		Time.timeScale = 1f;
		EditorApplication.update -= Tick;
		Application.logMessageReceived -= OnLog;
		EditorSettings.enterPlayModeOptionsEnabled = oldEnabled;
		EditorSettings.enterPlayModeOptions = oldOptions;
		File.AppendAllText(output, "Review finished; runtime errors=" + errors + "\n");
		EditorApplication.Exit(0);
	}
}
