using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RunRulesCheck
{
	static readonly string[] scenes = {"GameScene_AC", "GameScene_WW", "GameScene_MA"};
	static readonly int[] route = {0, 1, 2, 0, 1, 2, 1, 0, 2};
	static readonly int[] loot = new int[3];
	static readonly BindingFlags instance = BindingFlags.Instance | BindingFlags.NonPublic;
	static string folder, report, userPath, userBefore;
	static bool oldEnabled, refreshed;
	static EnterPlayModeOptions oldOptions;
	static int phase, stage, frame = -1, errors;
	static float started, refreshAt, pausedShield, pausedMagnet, magnetExpired, shieldExpired;
	static double timeout, resume;
	static GroundControl ground;
	static PlayerControl player;
	static SkillControl skills;
	static pause menu;
	static GameObject previousPlayer;
	static int Need {get {return (int)typeof(GroundControl).GetField("piece_need", instance).GetValue(ground);}}
	static void Invoke(object obj, string name, params object[] args) {obj.GetType().GetMethod(name, instance).Invoke(obj, args);}
	static void Require(bool ok, string message) {if (!ok) throw new Exception(message);}
	static void Pass(string message) {File.AppendAllText(report, "PASS " + message + "\n");}
	public static void Run()
	{
		folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../RunRulesReview")); Directory.CreateDirectory(folder);
		report = Path.Combine(folder, "test.txt"); File.WriteAllText(report, "Pickups, portal progression and scene routing\n");
		userPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../../SaveData/game-records.json"));
		userBefore = File.Exists(userPath) ? File.ReadAllText(userPath) : null;
		var flags = BindingFlags.Static | BindingFlags.NonPublic;
		typeof(GameRecords).GetField("filePath", flags).SetValue(null, Path.Combine(folder, "test-records.json"));
		typeof(GameRecords).GetField("history", flags).SetValue(null, null);
		typeof(GameRecords).GetField("current", flags).SetValue(null, null);
		oldEnabled = EditorSettings.enterPlayModeOptionsEnabled; oldOptions = EditorSettings.enterPlayModeOptions;
		EditorSettings.enterPlayModeOptionsEnabled = true; EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
		Application.logMessageReceived += OnLog; EditorApplication.update += Tick;
		EventControl.ResetRun(); GameRecords.BeginRun(); 角色选择界面.当前选择的角色序号 = 2;
		EditorSceneManager.OpenScene("Assets/Scenes/GameScene_AC.unity");
		Time.captureFramerate = 60; timeout = EditorApplication.timeSinceStartup + 240; EditorApplication.isPlaying = true;
	}
	static void OnLog(string message, string stack, LogType type)
	{
		if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) {errors++; File.AppendAllText(report, message + "\n" + stack + "\n");}
	}
	static bool Ready(int scene)
	{
		return SceneManager.GetActiveScene().name == scenes[scene] && EventControl.isplay && PlayerControl.Instance != null && (previousPlayer == null || PlayerControl.Instance.gameObject != previousPlayer);
	}
	static void HoldScene()
	{
		ground = GroundControl.Instance; player = PlayerControl.Instance; skills = SkillControl.Instance; menu = UnityEngine.Object.FindObjectOfType<pause>();
		ground.enabled = false; player.GetComponent<Rigidbody2D>().simulated = false;
		foreach (var item in ground.GetComponentsInChildren<ItemControl>()) item.enabled = false;
	}
	static void Pickup(string name)
	{
		var obj = new GameObject(name); var item = obj.AddComponent<ItemControl>();
		Invoke(item, "Start"); Invoke(item, "OnTriggerEnter2D", player.box_1);
	}
	static void CheckPrefabSlots()
	{
		int slots = 0;
		foreach (var prefab in ground.GroundPrefabs)
		{
			var obj = UnityEngine.Object.Instantiate(prefab); obj.SetActive(false);
			var positions = new Dictionary<Transform, Vector3>(); var rotations = new Dictionary<Transform, Quaternion>();
			foreach (Transform child in obj.transform) {positions[child] = child.localPosition; rotations[child] = child.localRotation;}
			Invoke(ground, "SetItems", obj.transform);
			foreach (Transform child in obj.transform)
			{
				Require(positions.ContainsKey(child) && child.localPosition == positions[child] && child.localRotation == rotations[child], "Moved or added a pickup slot in " + prefab.name);
				if (child.name.Contains("piece")) Require(child.gameObject.activeSelf && child.GetComponent<SpriteRenderer>().sprite == ground.pieces[ground.scene], "Loot piece was removed or randomized");
				if (child.name.Contains("item") || child.name == "shield" || child.name == "magnet" || child.name == "med") slots++;
			}
			UnityEngine.Object.DestroyImmediate(obj);
		}
		Pass(scenes[ground.scene] + ": all " + ground.GroundPrefabs.Length + " native platform variants keep slot positions, rotations and loot; pickup slots=" + slots);
	}
	static void CheckProbability()
	{
		UnityEngine.Random.InitState(20261017); int count = 0; int[] types = new int[3];
		string[] names = {"shield", "magnet", "med"};
		for (int i = 0; i < 4000; i++)
		{
			var obj = new GameObject("probability fixture"); obj.SetActive(false);
			var item = new GameObject("item"); item.transform.SetParent(obj.transform); item.transform.localPosition = new Vector3(1.2f, 0.8f, 0f); item.AddComponent<SpriteRenderer>();
			var piece = new GameObject("piece"); piece.transform.SetParent(obj.transform); piece.AddComponent<SpriteRenderer>();
			Invoke(ground, "SetItems", obj.transform);
			Require(item.transform.localPosition == new Vector3(1.2f, 0.8f, 0f) && piece.activeSelf, "Spawn chance changed positions or loot");
			if (item.activeSelf)
			{
				int type = Array.IndexOf(names, item.name); Require(type >= 0 && item.GetComponent<SpriteRenderer>().sprite == ground.items[type], "Pickup sprite and effect disagree"); count++; types[type]++;
			}
			UnityEngine.Object.DestroyImmediate(obj);
		}
		Require(count > 880 && count < 1120, "Pickup frequency not near 25%: " + count);
		for (int i = 0; i < 3; i++) Require(types[i] > 200, "A pickup type never or rarely spawned");
		Pass("4000 independent slots: spawned=" + count + " (" + (count / 40f).ToString("F2") + "%), shield/magnet/med=" + string.Join(",", types));
	}
	static void ClearGrounds()
	{
		while (ground.transform.childCount > 0) UnityEngine.Object.DestroyImmediate(ground.transform.GetChild(0).gameObject);
	}
	static void Generate()
	{
		var obj = UnityEngine.Object.Instantiate(ground.GroundPrefabs[0], ground.transform);
		while (obj.transform.childCount > 0) UnityEngine.Object.DestroyImmediate(obj.transform.GetChild(0).gameObject);
		var render = obj.GetComponent<SpriteRenderer>(); Sprite source = render.sprite; Rect rect = source.textureRect;
		render.sprite = Sprite.Create(source.texture, new Rect(rect.x, rect.y, 100f, rect.height), new Vector2(0.5f, 0.5f), source.pixelsPerUnit);
		obj.transform.position = new Vector3(-4f, -2f, 0f); obj.GetComponent<BoxCollider2D>().enabled = false;
		Invoke(ground, "Update");
	}
	static int Portals()
	{
		int count = 0;
		foreach (Transform root in ground.transform) foreach (Transform child in root) if (child.name == "portal(Clone)") count++;
		return count;
	}
	static void Transition(int target)
	{
		previousPlayer = player.gameObject;
		if (EventControl.scenesPassed >= 2)
		{
			int step = (target - ground.scene + 3) % 3; Require(step != 0, "Test route repeats current scene");
			int seed = 0; for (; seed < 100; seed++) {UnityEngine.Random.InitState(seed); if (UnityEngine.Random.Range(1, 3) == step) break;}
			Require(seed < 100, "Could not select a random branch"); UnityEngine.Random.InitState(seed);
		}
		var effect = UnityEngine.Object.Instantiate(ground.Portal_Effect); var ani = effect.GetComponent<Animator>();
		ani.Update(0f); ani.Play(ani.GetCurrentAnimatorStateInfo(0).fullPathHash, 0, 1.1f); ani.Update(0f);
		Require(ani.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f, "Portal animation did not finish");
		GroundControl.portal_effect = effect.transform; GroundControl.new_scene = true; EventControl.isplay = false;
		Invoke(ground, "Update");
	}
	static void Tick()
	{
		try
		{
			if (EditorApplication.timeSinceStartup > timeout) throw new Exception("Timed out: " + stage + "/" + phase + "; scene=" + SceneManager.GetActiveScene().name);
			if (!EditorApplication.isPlaying || frame == Time.frameCount) return; frame = Time.frameCount;
			if (phase == 0)
			{
				if (!Ready(route[stage])) return;
				HoldScene(); Require(EventControl.scenesPassed == stage && Need == 10 + stage * 2, "Wrong completed stage count or portal requirement");
				Require(EventControl.piece_count == 0 && !GroundControl.new_scene && !PlayerControl.isShield && !PlayerControl.isMagnet, "Scene pickup count, portal or buffs were not reset");
				Require(skills.MaTime == 5f && skills.ShTime == 5f && skills.ltTime == 3f, "Wrong pickup/rage serialized durations");
				for (int i = 0; i < 3; i++) Require(EventControl.lootCount[i] == loot[i], "Scene transition lost per-run loot");
				Pass("entered #" + (stage + 1) + " " + scenes[route[stage]] + "; need=" + Need + "; completed=" + EventControl.scenesPassed);
				if (stage < 3) CheckPrefabSlots();
				if (stage == 0)
				{
					CheckProbability(); Pickup("shield"); Pickup("magnet"); Require(PlayerControl.isShield && PlayerControl.isMagnet, "Real pickup did not activate buffs");
					started = Time.time; magnetExpired = shieldExpired = -1f; phase = 1; return;
				}
				phase = 10; return;
			}
			if (phase == 1)
			{
				if (Time.time - started < 1f) return;
				menu.pauseBtn(); pausedShield = skills.shieldTime.fillAmount; pausedMagnet = skills.magnetTime.fillAmount;
				resume = EditorApplication.timeSinceStartup + 0.3; phase = 2; return;
			}
			if (phase == 2)
			{
				if (EditorApplication.timeSinceStartup < resume) return;
				Require(skills.shieldTime.fillAmount == pausedShield && skills.magnetTime.fillAmount == pausedMagnet && PlayerControl.isShield && PlayerControl.isMagnet, "Pause consumed buff duration");
				menu.returnBtn(); Pass("pause freezes both buff timers"); phase = 3; return;
			}
			if (phase == 3)
			{
				float elapsed = Time.time - started;
				if (!refreshed && elapsed >= 2.5f) {Pickup("shield"); refreshAt = Time.time; refreshed = true; Require(skills.shieldTime.fillAmount == 0f, "Shield pickup did not refresh duration");}
				if (magnetExpired < 0f && !PlayerControl.isMagnet) magnetExpired = elapsed;
				if (shieldExpired < 0f && !PlayerControl.isShield) shieldExpired = Time.time - refreshAt;
				if (elapsed < 4.75f) Require(PlayerControl.isMagnet, "Magnet expired early");
				if (refreshed && Time.time - refreshAt < 4.75f) Require(PlayerControl.isShield, "Refreshed shield expired early");
				if (elapsed < 7.7f) return;
				Require(magnetExpired > 4.85f && magnetExpired < 5.06f && shieldExpired > 4.85f && shieldExpired < 5.06f, "Buff expiration incorrect: magnet=" + magnetExpired + "; shield=" + shieldExpired);
				Require(skills.shieldTime.fillAmount == 1f && skills.magnetTime.fillAmount == 1f, "Expired UI did not finish");
				Pass("native pickup timers: magnet=" + magnetExpired.ToString("F3") + "s, refreshed shield=" + shieldExpired.ToString("F3") + "s; 5s settings, refresh and expiration work"); phase = 10; return;
			}
			if (phase == 10)
			{
				ClearGrounds();
				for (int i = 0; i < Need - 1; i++) EventControl.Instance.CollectPiece();
				loot[ground.scene] += Need - 1; Generate(); phase = 11; return;
			}
			if (phase == 11)
			{
				Require(!GroundControl.new_scene && Portals() == 0, "Portal appeared below threshold");
				EventControl.Instance.CollectPiece(); loot[ground.scene]++; Generate(); phase = 12; return;
			}
			if (phase == 12)
			{
				Require(GroundControl.new_scene && Portals() == 1 && GroundControl.portal != null, "Portal did not appear at threshold");
				Generate(); phase = 13; return;
			}
			if (phase == 13)
			{
				Require(Portals() == 1 && EventControl.scenesPassed == stage, "Duplicate portal or stage count advanced before travel");
				Pass(scenes[ground.scene] + ": no portal at " + (Need - 1) + ", exactly one portal from " + Need + "; stage count changes only on travel");
				if (stage + 1 < route.Length) {Transition(route[stage + 1]); stage++; phase = 0; return;}
				previousPlayer = player.gameObject; menu.restart(); phase = 20; return;
			}
			if (phase == 20)
			{
				if (!Ready(0)) return; HoldScene();
				Require(EventControl.scenesPassed == 0 && Need == 10 && EventControl.piece_count == 0, "Restart did not reset progression");
				for (int i = 0; i < 3; i++) Require(EventControl.lootCount[i] == 0, "Restart kept previous loot");
				Pass("restart returns to AC, requirement 10, zero completed scenes and zero loot");
				Transition(1); phase = 21; return;
			}
			if (phase == 21)
			{
				if (!Ready(1)) return; HoldScene(); Require(EventControl.scenesPassed == 1 && Need == 12, "New run did not restore fixed opening order");
				Pass("fresh run again follows AC -> WW; all six later random source/destination pairs exercised without immediate repeats"); Finish();
			}
		}
		catch (Exception e) {errors++; File.AppendAllText(report, e + "\n"); Finish();}
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
