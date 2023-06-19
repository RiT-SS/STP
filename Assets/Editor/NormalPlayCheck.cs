using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class NormalPlayCheck
{
	static double timeout;
	static int phase, errors;
	static string folder;
	static NormalPlayCheck()
	{
		if (SessionState.GetBool("normal-play-check", false)) EditorApplication.delayCall += Resume;
	}
	public static void Run()
	{
		SessionState.SetBool("normal-play-check", true);
		SessionState.SetBool("normal-play-old-enabled", EditorSettings.enterPlayModeOptionsEnabled);
		SessionState.SetInt("normal-play-old-options", (int)EditorSettings.enterPlayModeOptions);
		EditorSettings.enterPlayModeOptionsEnabled = false;
		EditorSceneManager.OpenScene("Assets/Scenes/GameScene_WW.unity");
		EditorApplication.isPlaying = true;
	}
	static void Resume()
	{
		folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
		File.WriteAllText(Path.Combine(folder, "normal-play-test.txt"), "Normal domain reload and graphics test\n");
		Application.logMessageReceived += OnLog;
		timeout = EditorApplication.timeSinceStartup + 40;
		EditorApplication.update += Tick;
	}
	static void OnLog(string message, string stack, LogType type)
	{
		if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
		{
			errors++;
			File.AppendAllText(Path.Combine(folder, "normal-play-test.txt"), message + "\n" + stack + "\n");
		}
	}
	static void Tick()
	{
		try
		{
			if (EditorApplication.timeSinceStartup > timeout) throw new Exception("Normal play timed out");
			if (!EditorApplication.isPlaying) return;
			if (phase == 0 && Time.time > 5f)
			{
				if (PlayerControl.Instance == null || SkillControl.Instance == null || !EventControl.isplay) throw new Exception("Normal domain reload startup failed");
				PlayerControl.Instance.GetComponent<Rigidbody2D>().simulated = false;
				PlayerControl.Instance.transform.position = new Vector3(PlayerControl.player_x, 2.5f, 0f);
				PlayerControl.isShield = true;
				phase++;
			}
			if (phase == 1 && Time.time > 6f)
			{
				PlayerControl.Hp = 1; PlayerControl.isShield = false;
				SkillControl.isEffect = true;
				PlayerControl.Instance.Damage();
				if (PlayerControl.Hp != 0 || SkillControl.isEffect) throw new Exception("Revival did not trigger");
				phase++;
			}
			if (phase == 2 && PlayerControl.Hp > 0)
			{
				if (PlayerControl.Hp != 1 || !PlayerControl.isShield || !PlayerControl.Instance.box_1.enabled) throw new Exception("Revival state mismatch: HP=" + PlayerControl.Hp + "; shield=" + PlayerControl.isShield + "; collider=" + PlayerControl.Instance.box_1.enabled + "; shield timer=" + SkillControl.Instance.shieldTime.fillAmount);
				File.AppendAllText(Path.Combine(folder, "normal-play-test.txt"), "PASS: real revival animation restores HP, shield and collider\n");
				PlayerControl.Hp = 3;
				phase++;
			}
			if (phase == 3 && Time.time > 16f)
			{
				Capture("visual-fix-gameplay.png");
				PlayerControl.Hp = 1; PlayerControl.isShield = false; SkillControl.isEffect = false;
				EventControl.Route = 219.05f;
				PlayerControl.Instance.Damage();
				phase++;
			}
			if (phase == 4 && Time.time > 16.2f)
			{
				var score = PlayerControl.Instance.deathPanel.transform.Find("score").GetComponent<TMPro.TextMeshProUGUI>();
				var route = EventControl.Instance.transform.Find("board_1/route").GetComponent<TMPro.TextMeshProUGUI>();
				if (score.text != "219.05m" || route.text != score.text || !score.font.HasCharacters("0123456789.m")) throw new Exception("Normal Play score/font mismatch");
				score.ForceMeshUpdate();
				for (int i = 0; i < score.textInfo.characterCount; i++) if (!score.textInfo.characterInfo[i].isVisible) throw new Exception("Invisible score glyph");
				Capture("visual-fix-normal-score.png");
				File.AppendAllText(Path.Combine(folder, "normal-play-test.txt"), "PASS: ordinary Play domain reload, 16 seconds rendered gameplay, score = HUD = 219.05m, numeric glyphs visible; errors=" + errors + "\n");
				Finish();
			}
		}
		catch (Exception e) { errors++; File.AppendAllText(Path.Combine(folder, "normal-play-test.txt"), e + "\n"); Debug.LogError(e); Finish(); }
	}
	static void Capture(string name)
	{
		Camera camera = Camera.main;
		var target = RenderTexture.GetTemporary(1400, 560, 24);
		var active = RenderTexture.active; var previous = camera.targetTexture;
		try
		{
			camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
			var image = new Texture2D(1400, 560, TextureFormat.RGB24, false);
			image.ReadPixels(new Rect(0, 0, 1400, 560), 0, 0); image.Apply();
			File.WriteAllBytes(Path.Combine(folder, name), image.EncodeToPNG());
			UnityEngine.Object.Destroy(image);
		}
		finally { camera.targetTexture = previous; RenderTexture.active = active; RenderTexture.ReleaseTemporary(target); }
	}
	static void Finish()
	{
		SessionState.SetBool("normal-play-check", false);
		EditorSettings.enterPlayModeOptionsEnabled = SessionState.GetBool("normal-play-old-enabled", false);
		EditorSettings.enterPlayModeOptions = (EnterPlayModeOptions)SessionState.GetInt("normal-play-old-options", 3);
		EditorApplication.update -= Tick;
		EditorApplication.Exit(errors == 0 ? 0 : 1);
	}
}
