using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ApplyRunFix
{
	public static void Run()
	{
		string report = Path.GetFullPath(Path.Combine(Application.dataPath, "../../run-fix-apply.txt"));
		try
		{
			var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Font/ScoreDigits SDF.asset");
			font.ReadFontAssetDefinition();
			if (!font.HasCharacters("0123456789.m场景一二三"))
			{
				font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
				if (!font.TryAddCharacters("0123456789.m场景一二三", out string missing)) throw new Exception("Missing loot characters: " + missing);
				font.atlasPopulationMode = AtlasPopulationMode.Static;
			}
			EditorUtility.SetDirty(font); EditorUtility.SetDirty(font.atlasTexture); EditorUtility.SetDirty(font.material);
			AssetDatabase.SaveAssets();
			foreach (string name in new[] { "GameScene_AC", "GameScene_WW", "GameScene_MA" })
			{
				var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity");
				font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Font/ScoreDigits SDF.asset");
				font.ReadFontAssetDefinition();
				var player = UnityEngine.Object.FindObjectOfType<PlayerControl>();
				var ground = UnityEngine.Object.FindObjectOfType<GroundControl>();
				Transform panel = player.deathPanel.transform;
				var existing = panel.Find("lootSummary");
				if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
				RectTransform summary = MakeRect("lootSummary", panel, new Vector2(0.0614f, -0.48f), new Vector2(1.85f, 0.42f));
				for (int i = 0; i < 3; i++)
				{
					RectTransform column = MakeRect("stage" + i, summary, new Vector2((i - 1) * 0.55f, 0f), new Vector2(0.6f, 0.42f));
					var title = MakeRect("title", column, new Vector2(0f, 0.13f), new Vector2(0.6f, 0.18f)).gameObject.AddComponent<TextMeshProUGUI>();
					SetText(title, font, "场景" + "一二三"[i], 0.12f, new Color(0.85f, 0.85f, 0.85f));
					var icon = MakeRect("icon", column, new Vector2(-0.14f, -0.09f), new Vector2(0.18f, 0.18f)).gameObject.AddComponent<Image>();
					icon.sprite = ground.pieces[i]; icon.preserveAspect = true; icon.raycastTarget = false;
					var count = MakeRect("count", column, new Vector2(0.1f, -0.09f), new Vector2(0.3f, 0.22f)).gameObject.AddComponent<TextMeshProUGUI>();
					SetText(count, font, "0", 0.16f, new Color(0.95f, 0.88f, 0.45f));
				}
				EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
				File.AppendAllText(report, "PASS " + name + ": three scene loot columns\n");
			}
			AssetDatabase.SaveAssets(); EditorApplication.Exit(0);
		}
		catch (Exception e) { File.AppendAllText(report, e + "\n"); Debug.LogException(e); EditorApplication.Exit(1); }
	}
	static RectTransform MakeRect(string name, Transform parent, Vector2 position, Vector2 size)
	{
		var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
		rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
		rect.anchoredPosition = position; rect.sizeDelta = size;
		return rect;
	}
	static void SetText(TextMeshProUGUI text, TMP_FontAsset font, string value, float size, Color color)
	{
		text.font = font; text.fontSharedMaterial = font.material; text.text = value;
		text.fontSize = size; text.enableWordWrapping = false; text.alignment = TextAlignmentOptions.Center;
		text.color = color; text.raycastTarget = false;
		text.enableAutoSizing = true; text.fontSizeMin = size * 0.65f; text.fontSizeMax = size;
	}
}
