using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public static class ApplyHistory
{
	static readonly Color gold = new Color(0.94f, 0.8f, 0.48f);
	static readonly Color white = new Color(0.93f, 0.94f, 0.95f);
	static readonly Color muted = new Color(0.63f, 0.69f, 0.76f);
	static string report;
	static TMP_FontAsset font;
	static string[] piecePaths;

	public static void Run()
	{
		report = Path.GetFullPath(Path.Combine(Application.dataPath, "../../HistoryReview/apply.txt"));
		Directory.CreateDirectory(Path.GetDirectoryName(report)); File.WriteAllText(report, "History, names, loot icons and 3 second rage\n");
		try
		{
			string characters = "0123456789.m/: -游戏记录游玩时间排名角色距离战利品暂无完成一局后将在这里显示返回共条滚动查看开始选择退出机器人伯特盗贼燕子探险家爱丽丝";
			MakeFont("GameUI SDF", "锐字太空奇遇像素", characters);
			MakeFont("CharacterNames SDF", "zaozigongfangyifengti", "机器人 伯特盗贼 燕子探险家 爱丽丝");
			AssetDatabase.SaveAssets();
			foreach (string name in new[] { "GameScene_AC", "GameScene_WW", "GameScene_MA" })
			{
				var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity");
				var ground = UnityEngine.Object.FindObjectOfType<GroundControl>();
				piecePaths = Array.ConvertAll(ground.pieces, sprite => AssetDatabase.GetAssetPath(sprite));
				UnityEngine.Object.FindObjectOfType<SkillControl>().ltTime = 3f;
				Transform death = UnityEngine.Object.FindObjectOfType<PlayerControl>().deathPanel.transform;
				var score = death.Find("score").GetComponent<TextMeshProUGUI>();
				score.rectTransform.sizeDelta = new Vector2(1.9f, 0.5086f);
				score.enableAutoSizing = true; score.fontSizeMin = 0.15f; score.fontSizeMax = 0.4f;
				Transform summary = death.Find("lootSummary");
				for (int i = 0; i < 3; i++)
				{
					Transform column = summary.Find("stage" + i);
					if (column.Find("title") != null) UnityEngine.Object.DestroyImmediate(column.Find("title").gameObject);
					RectTransform icon = (RectTransform)column.Find("icon");
					icon.anchoredPosition = new Vector2(-0.16f, 0.01f); icon.sizeDelta = new Vector2(0.38f, 0.38f);
					RectTransform count = (RectTransform)column.Find("count");
					count.anchoredPosition = new Vector2(0.15f, 0.01f); count.sizeDelta = new Vector2(0.25f, 0.27f);
					var label = count.GetComponent<TextMeshProUGUI>(); label.fontSize = label.fontSizeMax = 0.18f; label.fontSizeMin = 0.1f;
				}
				EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
				File.AppendAllText(report, "PASS " + name + ": icons 0.18 -> 0.38; stage labels removed; rage=3s\n");
			}
			var selection = EditorSceneManager.OpenScene("Assets/Scenes/CharacterSelection.unity");
			var nameLabel = GameObject.Find("CharacterName").GetComponent<TextMeshProUGUI>();
			nameLabel.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Font/CharacterNames SDF.asset");
			nameLabel.font.ReadFontAssetDefinition(); nameLabel.fontSharedMaterial = nameLabel.font.material;
			nameLabel.enableWordWrapping = false; nameLabel.enableAutoSizing = true;
			nameLabel.fontSizeMin = 0.3f; nameLabel.fontSizeMax = nameLabel.fontSize = 0.62f;
			nameLabel.text = GameRecords.GetCharacterName(0);
			EditorSceneManager.MarkSceneDirty(selection); EditorSceneManager.SaveScene(selection);
			MakeMenu(); AssetDatabase.SaveAssets(); EditorApplication.Exit(0);
		}
		catch (Exception e) {File.AppendAllText(report, e + "\n"); Debug.LogException(e); EditorApplication.Exit(1);}
	}

	static void MakeMenu()
	{
		var scene = EditorSceneManager.OpenScene("Assets/Scenes/StartMenu.unity");
		font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Font/GameUI SDF.asset"); font.ReadFontAssetDefinition();
		var old = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "GameRecordsCanvas"); if (old != null) UnityEngine.Object.DestroyImmediate(old);
		var oldButton = GameObject.Find("RecordButton"); if (oldButton != null) UnityEngine.Object.DestroyImmediate(oldButton);
		Transform canvas = GameObject.Find("Canvas").transform;
		GameRecordPanel control = GameObject.Find("ChangeScene").GetComponent<GameRecordPanel>();
		if (control == null) control = GameObject.Find("ChangeScene").AddComponent<GameRecordPanel>();
		var root = new GameObject("GameRecordsCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
		root.layer = 5;
		Canvas overlay = root.GetComponent<Canvas>(); overlay.renderMode = RenderMode.ScreenSpaceOverlay; overlay.sortingOrder = 100;
		CanvasScaler scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1400f, 560f); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
		var background = Image("background", root.transform, Vector2.zero, Vector2.zero, Color.white);
		Stretch(background.rectTransform);
		background.sprite = canvas.Find("Image").GetComponent<Image>().sprite;
		var shade = Image("shade", root.transform, Vector2.zero, Vector2.zero, new Color(0.025f, 0.035f, 0.06f, 0.91f)); Stretch(shade.rectTransform);
		var frame = Image("frame", root.transform, Vector2.zero, new Vector2(1280f, 512f), new Color(0.08f, 0.105f, 0.14f, 0.98f));
		Image("topLine", frame.transform, new Vector2(0f, 254f), new Vector2(1280f, 3f), gold).raycastTarget = false;
		Image("divider", frame.transform, new Vector2(0f, 165f), new Vector2(1208f, 1.5f), new Color(gold.r, gold.g, gold.b, 0.38f)).raycastTarget = false;
		Text("title", frame.transform, new Vector2(-440f, 208f), new Vector2(330f, 60f), "游戏记录", 38f, gold, TextAlignmentOptions.Left);
		Button back = Button("back", frame.transform, new Vector2(533f, 208f), new Vector2(140f, 44f), "返回");
		UnityEventTools.AddPersistentListener(back.onClick, control.Close);
		Text("rankHeading", frame.transform, new Vector2(-564f, 135f), new Vector2(70f, 30f), "排名", 20f, muted);
		Text("timeHeading", frame.transform, new Vector2(-420f, 135f), new Vector2(240f, 30f), "游玩时间", 20f, muted);
		Text("roleHeading", frame.transform, new Vector2(-188f, 135f), new Vector2(240f, 30f), "角色", 20f, muted);
		Text("distanceHeading", frame.transform, new Vector2(32f, 135f), new Vector2(170f, 30f), "距离", 20f, muted);
		Text("lootHeading", frame.transform, new Vector2(375f, 135f), new Vector2(410f, 30f), "战利品", 20f, muted);
		RectTransform viewport = Rect("viewport", frame.transform, new Vector2(0f, -47f), new Vector2(1208f, 334f));
		viewport.gameObject.AddComponent<Image>().color = Color.clear;
		viewport.gameObject.AddComponent<RectMask2D>();
		RectTransform content = Rect("content", viewport, Vector2.zero, new Vector2(1208f, GameRecords.MaxRecords * 78f));
		content.anchorMin = content.anchorMax = new Vector2(0.5f, 1f); content.pivot = new Vector2(0.5f, 1f); content.anchoredPosition = Vector2.zero;
		ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.content = content; scroll.viewport = viewport; scroll.horizontal = false;
		scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 35f;
		var barImage = Image("scrollbar", frame.transform, new Vector2(615f, -47f), new Vector2(8f, 334f), new Color(0f, 0f, 0f, 0.35f));
		var handle = Image("handle", barImage.transform, Vector2.zero, Vector2.zero, new Color(gold.r, gold.g, gold.b, 0.65f)); Stretch(handle.rectTransform);
		Scrollbar bar = barImage.gameObject.AddComponent<Scrollbar>(); bar.targetGraphic = handle; bar.handleRect = handle.rectTransform; bar.direction = Scrollbar.Direction.BottomToTop;
		scroll.verticalScrollbar = bar;
		control.rows = new GameRecordRow[GameRecords.MaxRecords];
		Sprite[] pieces = Array.ConvertAll(piecePaths, path => AssetDatabase.LoadAssetAtPath<Sprite>(path));
		for (int i = 0; i < control.rows.Length; i++)
		{
			var row = Image("record" + i, content, new Vector2(0f, -i * 78f), new Vector2(1208f, 72f), i % 2 == 0 ? new Color(0.15f, 0.19f, 0.24f, 0.8f) : new Color(0.11f, 0.145f, 0.19f, 0.8f));
			row.rectTransform.anchorMin = row.rectTransform.anchorMax = new Vector2(0.5f, 1f); row.rectTransform.pivot = new Vector2(0.5f, 1f); row.rectTransform.anchoredPosition = new Vector2(0f, -i * 78f);
			GameRecordRow entry = row.gameObject.AddComponent<GameRecordRow>(); control.rows[i] = entry;
			entry.rank = Text("rank", row.transform, new Vector2(-564f, 0f), new Vector2(70f, 65f), "01", 26f, gold);
			entry.playedAt = Text("playedAt", row.transform, new Vector2(-420f, 0f), new Vector2(240f, 65f), "2026/10/17\n00:00:00", 23f, white);
			entry.character = Text("character", row.transform, new Vector2(-188f, 0f), new Vector2(245f, 65f), "探险家 爱丽丝", 27f, white);
			entry.distance = Text("distance", row.transform, new Vector2(32f, 0f), new Vector2(170f, 65f), "0.00m", 29f, gold);
			entry.loot = new TextMeshProUGUI[3];
			for (int j = 0; j < 3; j++)
			{
				float x = 235f + j * 140f;
				Image icon = Image("icon" + j, row.transform, new Vector2(x - 30f, 0f), new Vector2(56f, 56f), Color.white);
				icon.sprite = pieces[j]; icon.preserveAspect = true; icon.raycastTarget = false;
				entry.loot[j] = Text("loot" + j, row.transform, new Vector2(x + 32f, 0f), new Vector2(64f, 64f), "0", 27f, white);
			}
			row.gameObject.SetActive(false);
		}
		var empty = Text("empty", viewport, Vector2.zero, new Vector2(1000f, 180f), "暂无游戏记录\n完成一局后将在这里显示", 27f, muted);
		empty.enableWordWrapping = false;
		control.total = Text("total", frame.transform, new Vector2(550f, -235f), new Vector2(110f, 30f), "0 / " + GameRecords.MaxRecords, 18f, muted);
		control.panel = root; control.empty = empty.gameObject; control.content = content; control.scroll = scroll;
		root.SetActive(false);
		GameObject recordButton = UnityEngine.Object.Instantiate(canvas.Find("SelectButton").gameObject, canvas);
		recordButton.name = "RecordButton";
		Button record = recordButton.GetComponent<Button>();
		if (record.onClick.GetPersistentEventCount() > 0) UnityEventTools.RemovePersistentListener(record.onClick, 0);
		UnityEventTools.AddPersistentListener(record.onClick, control.Open);
		string[] buttons = { "StartButton", "SelectButton", "RecordButton", "exitButton" };
		string[] titles = { "开始游戏", "选择角色", "游戏记录", "退出游戏" };
		for (int i = 0; i < buttons.Length; i++)
		{
			RectTransform button = (RectTransform)canvas.Find(buttons[i]);
			button.localPosition = new Vector3(0f, 12f - i * 54f, button.localPosition.z);
			button.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 166f); button.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 40f);
			var label = button.GetComponentInChildren<TextMeshProUGUI>();
			label.font = font; label.fontSharedMaterial = font.material; label.text = titles[i]; label.enableWordWrapping = false;
			label.fontSize = label.fontSizeMax = 27.5f; label.fontSizeMin = 20f; label.enableAutoSizing = true;
		}
		EditorUtility.SetDirty(control); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
		File.AppendAllText(report, "PASS StartMenu: four buttons; records panel with " + GameRecords.MaxRecords + " reusable rows and scrolling\n");
	}

	static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
	{
		var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.gameObject.layer = 5;
		rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
		rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
	}
	static void Stretch(RectTransform rect) {rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;}
	static Image Image(string name, Transform parent, Vector2 position, Vector2 size, Color color)
	{
		var image = Rect(name, parent, position, size).gameObject.AddComponent<Image>(); image.color = color; return image;
	}
	static TextMeshProUGUI Text(string name, Transform parent, Vector2 position, Vector2 size, string value, float fontSize, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center)
	{
		var text = Rect(name, parent, position, size).gameObject.AddComponent<TextMeshProUGUI>(); text.font = font; text.fontSharedMaterial = font.material;
		text.text = value; text.fontSize = text.fontSizeMax = fontSize; text.fontSizeMin = fontSize * 0.65f; text.enableAutoSizing = true;
		text.alignment = align; text.color = color; text.raycastTarget = false; text.enableWordWrapping = false; return text;
	}
	static Button Button(string name, Transform parent, Vector2 position, Vector2 size, string title)
	{
		Image image = Image(name, parent, position, size, new Color(0.22f, 0.25f, 0.28f));
		Button button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
		var label = Text("label", button.transform, Vector2.zero, size, title, 24f, gold); label.margin = new Vector4(12f, 0f, 12f, 0f); return button;
	}
	static TMP_FontAsset MakeFont(string name, string source, string characters)
	{
		string path = "Assets/Font/" + name + ".asset";
		var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
		if (existing != null) {existing.ReadFontAssetDefinition(); if (!existing.HasCharacters(characters)) throw new Exception("Existing font incomplete: " + name); return existing;}
		Font typeface = AssetDatabase.LoadAssetAtPath<Font>("Assets/Font/" + source + ".ttf");
		int atlas = name == "CharacterNames SDF" ? 1024 : 2048;
		var result = TMP_FontAsset.CreateFontAsset(typeface, 90, 9, GlyphRenderMode.SDFAA, atlas, atlas, AtlasPopulationMode.Dynamic, false);
		result.name = name; result.hashCode = TMP_TextUtilities.GetSimpleHashCode(name);
		if (!result.TryAddCharacters(characters, out string missing)) throw new Exception("Font missing: " + source + ": " + missing);
		result.atlasPopulationMode = AtlasPopulationMode.Static; AssetDatabase.CreateAsset(result, path);
		AssetDatabase.AddObjectToAsset(result.atlasTexture, result); AssetDatabase.AddObjectToAsset(result.material, result);
		EditorUtility.SetDirty(result); EditorUtility.SetDirty(result.atlasTexture); EditorUtility.SetDirty(result.material); return result;
	}
}
