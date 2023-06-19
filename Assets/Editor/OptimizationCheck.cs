using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class OptimizationCheck
{
	static readonly string[] scenes = { "GameScene_AC", "GameScene_MA", "GameScene_WW" };
	static int stage, phase, errors, lastFrame;
	static double next, timeout;
	static float started, route;
	static string output;
	static bool oldEnabled;
	static EnterPlayModeOptions oldOptions;
	static PlayerControl player;
	static pause menu;
	static BgControl bg;
	static int bgCount, textureCount;
	static System.Collections.Generic.HashSet<int> textures = new System.Collections.Generic.HashSet<int>();
	static Vector3 playerPosition, groundPosition;
	static Transform firstGround;
	static GameObject support1, support2;
	static Rigidbody2D body;
	static bool stopping;

	public static void Run()
	{
		output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../codefix-test.txt"));
		File.WriteAllText(output, "Code fix regression " + Application.unityVersion + "\n");
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
		角色选择界面.当前选择的角色序号 = stage;
		PlayerControl.Hp = 3;
		PlayerControl.isShield = PlayerControl.isRage = PlayerControl.isMagnet = true;
		SkillControl.isEffect = false;
		EventControl.Route = stage == 1 ? 123.45f : 0f;
		EditorApplication.isPlaying = true;
		phase = 0;
		stopping = false;
		next = EditorApplication.timeSinceStartup + 2;
		timeout = EditorApplication.timeSinceStartup + 65;
	}

	static void OnLog(string message, string stack, LogType type)
	{
		if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
		{
			errors++;
			File.AppendAllText(output, type + ": " + message + "\n" + stack + "\n");
		}
	}
	static void Require(bool ok, string message)
	{
		if (!ok) throw new Exception(message);
	}
	static void Pass(string message) { File.AppendAllText(output, "PASS " + scenes[stage] + ": " + message + "\n"); }
	static void Later(double delay) { next = EditorApplication.timeSinceStartup + delay; lastFrame = Time.frameCount; }
	static void Tick()
	{
		try
		{
			if (stopping)
			{
				if (EditorApplication.isPlayingOrWillChangePlaymode) return;
				stage++;
				if (stage < scenes.Length) Begin();
				else Finish();
				return;
			}
			if (EditorApplication.timeSinceStartup > timeout) throw new Exception("Timed out at phase " + phase);
			if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < next || Time.frameCount == lastFrame) return;
			switch (phase)
			{
			case 0:
				player = PlayerControl.Instance;
				menu = UnityEngine.Object.FindObjectOfType<pause>();
				bg = UnityEngine.Object.FindObjectOfType<BgControl>();
				Require(player != null && menu != null && bg != null, "Scene controllers missing");
				Require(!PlayerControl.isShield && !PlayerControl.isRage && !PlayerControl.isMagnet && SkillControl.isEffect, "Skill state did not reset on scene entry");
				Require(SkillControl.skill == stage, "Selected character and skill disagree");
				if (stage == 1) Require(GameObject.Find("Canvas").transform.Find("board_1/route").GetComponent<TMPro.TextMeshProUGUI>().text == "123.45m", "Route changed during intro");
				Pass("skill reset and selected character");
				body = player.GetComponent<Rigidbody2D>();
				body.simulated = false;
				player.transform.position = new Vector3(PlayerControl.player_x, 3f, 0f);
				EventControl.isplay = false;
				menu.pauseBtn();
				playerPosition = player.transform.position;
				Later(0.5); phase++; break;
			case 1:
				Require(EventControl.isPaused && !EventControl.isplay && Time.timeScale == 0f, "Intro pause overwritten on later frame");
				Require(player.transform.position == playerPosition, "Player moved during intro pause");
				menu.returnBtn();
				Require(!EventControl.isplay, "Intro resumed as playing immediately");
				Later(0.1); phase++; break;
			case 2:
				if (!EventControl.isplay) { Later(0.1); break; }
				menu.pauseBtn();
				firstGround = UnityEngine.Object.FindObjectOfType<GroundControl>().transform.GetChild(0);
				groundPosition = firstGround.position;
				playerPosition = player.transform.position;
				route = EventControl.Route;
				Later(0.5); phase++; break;
			case 3:
				Require(EventControl.isPaused && !EventControl.isplay && Time.timeScale == 0f, "Gameplay pause overwritten on later frame");
				Require(firstGround.position == groundPosition && player.transform.position == playerPosition && EventControl.Route == route, "World moved while paused");
				menu.returnBtn();
				Require(EventControl.isplay, "Gameplay did not resume");
				Pass("pause across multiple frames during intro and gameplay");
				TestAudio();
				PlayerControl.Hp = 3; PlayerControl.isShield = false;
				player.Damage(); player.Damage();
				Require(PlayerControl.Hp == 2, "Blink did not block repeated damage");
				menu.pauseBtn();
				Later(0.9); phase++; break;
			case 4:
				menu.returnBtn(); player.Damage();
				Require(PlayerControl.Hp == 2, "Invulnerability expired during pause");
				started = Time.time;
				Later(0.1); phase++; break;
			case 5:
				if (Time.time - started < 0.9f) { Later(0.1); break; }
				player.Damage();
				Require(PlayerControl.Hp == 1 && player.myRender.enabled, "Damage did not resume after blink");
				Pass("damage invulnerability, scaled pause timer and expiry");
				PlayerControl.Hp = 3; PlayerControl.isShield = true;
				// 用真实物理验证同时站在两个地面上，离开其中一个仍然接地。
				player.transform.position = new Vector3(PlayerControl.player_x, 3f, 0f);
				body.simulated = true; body.velocity = Vector2.zero;
				Physics2D.SyncTransforms();
				float bottom = player.box_1.bounds.min.y;
				support1 = MakeSupport(player.transform.position.x - 0.15f, bottom - 0.13f);
				support2 = MakeSupport(player.transform.position.x + 0.15f, bottom - 0.13f);
				Later(0.4); phase++; break;
			case 6:
				Require(PlayerControl.isGround, "Player did not land on test supports");
				UnityEngine.Object.Destroy(support1);
				Later(0.2); phase++; break;
			case 7:
				Require(PlayerControl.isGround, "Leaving one of two supports cleared grounded state");
				UnityEngine.Object.Destroy(support2);
				Later(0.3); phase++; break;
			case 8:
				Require(!PlayerControl.isGround, "Leaving all supports did not clear grounded state");
				Pass("multiple simultaneous ground contacts");
				body.simulated = false;
				player.transform.position = new Vector3(PlayerControl.player_x, 3f, 0f);
				bgCount = bg.transform.childCount;
				foreach (Transform child in bg.transform) child.position = new Vector3(-20f, child.position.y, child.position.z);
				textureCount = Resources.FindObjectsOfTypeAll<Texture2D>().Length;
				textures.Clear();
				foreach (Texture2D texture in Resources.FindObjectsOfTypeAll<Texture2D>()) textures.Add(texture.GetInstanceID());
				started = Time.time;
				Later(0.1); phase++; break;
			case 9:
				Require(bg.transform.childCount == bgCount, "Backgrounds grew after wrapping");
				foreach (Transform item in UnityEngine.Object.FindObjectsOfType<Transform>())
					Require(!float.IsNaN(item.position.x) && !float.IsNaN(item.position.y), "NaN position: " + item.name);
				if (Time.time - started < 18f) { Later(0.2); break; }
				int now = Resources.FindObjectsOfTypeAll<Texture2D>().Length;
				int generated = 0;
				foreach (Texture2D texture in Resources.FindObjectsOfTypeAll<Texture2D>())
					if (!textures.Contains(texture.GetInstanceID()))
					{
						string path = AssetDatabase.GetAssetPath(texture);
						File.AppendAllText(output, "Texture added: '" + texture.name + "' " + texture.width + "x" + texture.height + " path=" + path + " persistent=" + EditorUtility.IsPersistent(texture) + "\n");
						if (!EditorUtility.IsPersistent(texture) && string.IsNullOrEmpty(texture.name)) generated++;
					}
				Require(generated == 0, "Terrain created runtime texture copies: " + generated);
				Require(UnityEngine.Object.FindObjectsOfType<FragmentLife>().Length < 250, "Unbounded live fragment count");
				Pass("18 seconds terrain slicing, fixed background count, finite positions; textures " + textureCount + " -> " + now);
				EditorApplication.isPlaying = false;
				stopping = true; break;
			}
		}
		catch (Exception e) { Debug.LogError("Regression: " + e); Finish(); }
	}

	static GameObject MakeSupport(float x, float y)
	{
		var support = new GameObject("test ground");
		support.tag = "Ground";
		support.transform.position = new Vector3(x, y, 0f);
		support.AddComponent<BoxCollider2D>().size = new Vector2(0.3f, 0.2f);
		return support;
	}
	static void TestAudio()
	{
		var audio = AudioManager.Instance;
		for (int i = 0; i < audio.MaxAudioSources + 10; i++) audio.Play("金币");
		audio.Play("Boss死了");
		AudioClip death = Resources.Load<AudioClip>("Boss死了");
		AudioSource critical = null;
		foreach (AudioSource source in audio.GetComponents<AudioSource>()) if (source.clip == death) critical = source;
		Require(audio.GetComponents<AudioSource>().Length <= audio.MaxAudioSources, "Sound pool exceeded cap");
		Require(critical != null && critical.priority == 32, "Critical sound could not preempt pool");
		for (int i = 0; i < 20; i++) audio.Play("金币");
		Require(critical.clip == death, "Pickup interrupted critical sound");
		Pass("capped sound pool and critical sound priority");
	}
	static void Finish()
	{
		EditorApplication.update -= Tick;
		Application.logMessageReceived -= OnLog;
		Time.timeScale = 1f;
		EditorSettings.enterPlayModeOptionsEnabled = oldEnabled;
		EditorSettings.enterPlayModeOptions = oldOptions;
		File.AppendAllText(output, "FINISHED; errors=" + errors + "\n");
		EditorApplication.Exit(errors == 0 ? 0 : 1);
	}
}
