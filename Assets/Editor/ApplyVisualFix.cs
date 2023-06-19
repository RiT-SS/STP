using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public static class ApplyVisualFix
{
	const int HudLayer = 1144202601;
	const int ModalLayer = 1144202602;
	public static void Run()
	{
		string report = Path.GetFullPath(Path.Combine(Application.dataPath, "../../visual-fix-apply.txt"));
		try
		{
			AddLayer("HUD", HudLayer);
			AddLayer("Dialogs", ModalLayer);
			MakeScoreFont();
			AssetDatabase.SaveAssets();
			foreach (string name in new[] { "GameScene_AC", "GameScene_WW", "GameScene_MA" })
			{
				var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity");
				TMP_FontAsset scoreFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Font/ScoreDigits SDF.asset");
				scoreFont.ReadFontAssetDefinition();
				if (!scoreFont.HasCharacters("0123456789.m")) throw new Exception("Saved score font is missing numeric characters: " + scoreFont.characterTable.Count);
				var player = UnityEngine.Object.FindObjectOfType<PlayerControl>();
				var pause = UnityEngine.Object.FindObjectOfType<pause>();
				var events = UnityEngine.Object.FindObjectOfType<EventControl>();
				SetLayer(events.transform, HudLayer);
				var canvas = events.GetComponent<Canvas>();
				canvas.overrideSorting = true;
				canvas.sortingLayerID = HudLayer;
				// 暂停、失败弹窗高于 HUD；按钮仍由各自的 GraphicRaycaster 接收。
				SetLayer(pause.transform, HudLayer);
				SetDialog(pause.menu.transform);
				Canvas deathCanvas = player.deathPanel.GetComponentInParent<Canvas>();
				SetLayer(deathCanvas.transform, ModalLayer);
				deathCanvas.overrideSorting = true;
				deathCanvas.sortingLayerID = ModalLayer;
				deathCanvas.sortingOrder = 10;
				var score = player.deathPanel.transform.Find("score").GetComponent<TextMeshProUGUI>();
				score.font = scoreFont;
				score.fontSharedMaterial = scoreFont.material;
				score.enableWordWrapping = false;
				score.enableAutoSizing = true;
				score.fontSizeMin = 0.18f;
				score.fontSizeMax = 0.4f;
				score.fontSize = 0.4f;
				score.overflowMode = TextOverflowModes.Overflow;
				score.text = "0.00m";
				EditorSceneManager.MarkSceneDirty(scene);
				EditorSceneManager.SaveScene(scene);
				File.AppendAllText(report, "PASS " + name + ": HUD and dialogs sorted; score digits repaired\n");
			}
			AssetDatabase.SaveAssets();
			EditorApplication.Exit(0);
		}
		catch (Exception e) { File.AppendAllText(report, e + "\n"); Debug.LogException(e); EditorApplication.Exit(1); }
	}
	static void AddLayer(string name, int id)
	{
		var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
		var layers = settings.FindProperty("m_SortingLayers");
		for (int i = 0; i < layers.arraySize; i++)
			if (layers.GetArrayElementAtIndex(i).FindPropertyRelative("uniqueID").intValue == id) return;
		int index = layers.arraySize;
		layers.InsertArrayElementAtIndex(index);
		var layer = layers.GetArrayElementAtIndex(index);
		layer.FindPropertyRelative("name").stringValue = name;
		layer.FindPropertyRelative("uniqueID").intValue = id;
		layer.FindPropertyRelative("locked").boolValue = false;
		settings.ApplyModifiedProperties();
	}
	static void SetLayer(Transform root, int layer)
	{
		foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true)) renderer.sortingLayerID = layer;
		foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true)) canvas.sortingLayerID = layer;
		foreach (SpriteMask mask in root.GetComponentsInChildren<SpriteMask>(true))
		{
			mask.frontSortingLayerID = layer;
			mask.backSortingLayerID = layer;
		}
	}
	static void SetDialog(Transform root)
	{
		var canvas = root.GetComponent<Canvas>();
		if (canvas == null) canvas = root.gameObject.AddComponent<Canvas>();
		canvas.overrideSorting = true;
		canvas.sortingLayerID = ModalLayer;
		canvas.sortingOrder = 10;
		if (root.GetComponent<GraphicRaycaster>() == null) root.gameObject.AddComponent<GraphicRaycaster>();
		SetLayer(root, ModalLayer);
	}
	static TMP_FontAsset MakeScoreFont()
	{
		const string path = "Assets/Font/ScoreDigits SDF.asset";
		var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
		if (existing != null)
		{
			existing.hashCode = TMP_TextUtilities.GetSimpleHashCode(existing.name);
			existing.ReadFontAssetDefinition();
			EditorUtility.SetDirty(existing);
			return existing;
		}
		Font source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Font/锐字太空奇遇像素.ttf");
		var font = TMP_FontAsset.CreateFontAsset(source, 90, 9, GlyphRenderMode.SDFAA, 512, 512, AtlasPopulationMode.Dynamic, false);
		font.name = "ScoreDigits SDF";
		font.hashCode = TMP_TextUtilities.GetSimpleHashCode(font.name);
		if (!font.TryAddCharacters("0123456789.m", out string missing)) throw new Exception("Score font missing: " + missing);
		font.atlasPopulationMode = AtlasPopulationMode.Static;
		AssetDatabase.CreateAsset(font, path);
		AssetDatabase.AddObjectToAsset(font.atlasTexture, font);
		AssetDatabase.AddObjectToAsset(font.material, font);
		EditorUtility.SetDirty(font);
		EditorUtility.SetDirty(font.atlasTexture);
		EditorUtility.SetDirty(font.material);
		return font;
	}
}
