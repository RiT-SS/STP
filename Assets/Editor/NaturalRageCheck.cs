using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class NaturalRageCheck
{
	static string folder, report;
	static int stage, phase, errors, peak, captured;
	static bool oldEnabled;
	static EnterPlayModeOptions oldOptions;
	static readonly string[] scenes = { "GameScene_WW", "GameScene_AC", "GameScene_MA" };
	static double timeout;
	static float started, nextCapture;
	static PlayerControl player;
	public static void Run()
	{
		folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../DashLootReview"));
		Directory.CreateDirectory(folder); report = Path.Combine(folder, "natural-rage-test.txt");
		File.WriteAllText(report, "Natural terrain rage endurance\n");
		oldEnabled = EditorSettings.enterPlayModeOptionsEnabled; oldOptions = EditorSettings.enterPlayModeOptions;
		EditorSettings.enterPlayModeOptionsEnabled = true; EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
		EventControl.ResetRun(); 角色选择界面.当前选择的角色序号 = 2;
		UnityEngine.Random.InitState(2917);
		EditorSceneManager.OpenScene("Assets/Scenes/" + scenes[0] + ".unity");
		Application.logMessageReceived += OnLog; EditorApplication.update += Tick;
		timeout = EditorApplication.timeSinceStartup + 180; EditorApplication.isPlaying = true;
	}
	static void OnLog(string message, string stack, LogType type)
	{
		if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) {errors++; File.AppendAllText(report, message + "\n" + stack + "\n");}
	}
	static void Require(bool ok, string message) {if (!ok) throw new Exception(message);}
	static void Tick()
	{
		try
		{
			if (EditorApplication.timeSinceStartup > timeout) throw new Exception("Natural rage timed out: " + stage + "/" + phase);
			if (!EditorApplication.isPlaying) return;
			if (phase == 0)
			{
				if (SceneManager.GetActiveScene().name != scenes[stage] || !EventControl.isplay || PlayerControl.Instance == null) return;
				// 只抬高本次测试的过关门槛，让冲刺在同一关完整跑完，不改游戏场景。
				typeof(GroundControl).GetField("piece_need", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(GroundControl.Instance, 100000);
				player = PlayerControl.Instance; PlayerControl.Hp = 3; PlayerControl.isShield = false;
				SkillControl.Instance.Rage(); Require(PlayerControl.isRage, "Natural rage did not start");
				started = Time.time; nextCapture = Time.time + 1f; peak = 0; captured = 0; phase = 1; return;
			}
			if (phase == 1)
			{
				Require(PlayerControl.Instance != null && (PlayerControl.isRage ? PlayerControl.Hp == 3 : PlayerControl.Hp > 0) && !player.deathPanel.activeSelf, "Rage check: elapsed=" + (Time.time - started) + "; rage=" + PlayerControl.isRage + "; HP=" + PlayerControl.Hp + "; death=" + player.deathPanel.activeSelf + "; position=" + player.transform.position + "; lt=" + SkillControl.lt.fillAmount + "; ground=" + PlayerControl.isGround);
				peak = Mathf.Max(peak, UnityEngine.Object.FindObjectsOfType<PieceControl>().Length);
				if (stage == 0 && Time.time >= nextCapture && captured < 36)
				{
					Capture("natural-" + captured.ToString("D3")); captured++; nextCapture += 1f / 15f;
				}
				if (PlayerControl.isRage)
				{
					Require(Time.time - started < 16f, "Rage never found a safe landing"); return;
				}
				Require(Time.time - started >= SkillControl.Instance.ltTime - 0.1f && GroundControl.Speed == 2f, "Rage duration or ending speed wrong");
				PlayerControl.isShield = true;
				File.AppendAllText(report, "PASS " + scenes[stage] + ": full rage survived; duration=" + (Time.time - started).ToString("F2") + "; peak fragments=" + peak + "\n");
				started = Time.time; phase = 2; return;
			}
			if (phase == 2)
			{
				Require(PlayerControl.Instance != null && PlayerControl.Hp > 0 && !player.deathPanel.activeSelf, "Post-rage landing failed");
				if (Time.time - started < 0.8f) return;
				File.AppendAllText(report, "PASS " + scenes[stage] + ": normal physics continues after rage\n");
				if (stage == scenes.Length - 1)
				{
					PlayerControl.Hp = 1; PlayerControl.isShield = false; SkillControl.isEffect = false;
					player.Damage(); Canvas.ForceUpdateCanvases(); Capture("loot-summary-final");
					Finish(); return;
				}
				stage++;
				phase = 0; PlayerControl.Hp = 3; SceneManager.LoadScene(scenes[stage]);
			}
		}
		catch (Exception e) {errors++; File.AppendAllText(report, e + "\n"); if (PlayerControl.Instance != null) Capture("natural-failure"); Debug.LogError(e); Finish();}
	}
	static void Capture(string name)
	{
		Camera camera = Camera.main; var target = RenderTexture.GetTemporary(1400, 560, 24);
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
		EditorSettings.enterPlayModeOptionsEnabled = oldEnabled; EditorSettings.enterPlayModeOptions = oldOptions;
		Time.timeScale = 1f; EditorApplication.update -= Tick; Application.logMessageReceived -= OnLog;
		File.AppendAllText(report, "FINISHED errors=" + errors + "\n"); EditorApplication.Exit(errors == 0 ? 0 : 1);
	}
}
