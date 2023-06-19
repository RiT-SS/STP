using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class HistoryLayoutDump
{
	public static void Run()
	{
		string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../history-layout.txt"));
		File.WriteAllText(output, "Existing UI assets and layout\n");
		foreach (string name in new[] {"StartMenu", "CharacterSelection", "GameScene_AC"})
		{
			var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity");
			File.AppendAllText(output, "SCENE " + name + "; camera ortho=" + Camera.main.orthographicSize + "\n");
			foreach (var root in scene.GetRootGameObjects()) foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
			{
				if (name == "GameScene_AC" && !child.name.Contains("death") && child.GetComponentInParent<PlayerControl>() == null && child.GetComponentInParent<GroundControl>() == null) continue;
				RectTransform rect = child as RectTransform;
				var sprite = child.GetComponent<SpriteRenderer>(); var image = child.GetComponent<Image>(); var text = child.GetComponent<TextMeshProUGUI>();
				File.AppendAllText(output, child.name + "; pos=" + child.localPosition + "; scale=" + child.localScale + (rect != null ? "; size=" + rect.sizeDelta : "") + (sprite != null ? "; sprite=" + sprite.sprite : "") + (image != null ? "; image=" + image.sprite : "") + (text != null ? "; text=" + text.text + "; font=" + AssetDatabase.GetAssetPath(text.font) + "; size=" + text.fontSize : "") + "\n");
			}
		}
		EditorApplication.Exit(0);
	}
}
