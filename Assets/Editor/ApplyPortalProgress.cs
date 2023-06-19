using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ApplyPortalProgress
{
	public static void Run()
	{
		string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../PortalProgressCapReview")); Directory.CreateDirectory(folder);
		string report = Path.Combine(folder, "apply.txt"); File.WriteAllText(report, "Portal progress HUD\n");
		try
		{
			foreach (string name in new[] {"GameScene_AC", "GameScene_WW", "GameScene_MA"})
			{
				var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity");
				var text = UnityEngine.Object.FindObjectOfType<EventControl>().transform.Find("board_2/piece_number").GetComponent<TextMeshProUGUI>();
				text.font.ReadFontAssetDefinition();
				if (!text.font.HasCharacters("0123456789/")) throw new Exception("HUD font missing progress characters");
				text.text = "0/10"; text.enableWordWrapping = false; text.enableAutoSizing = true;
				text.fontSizeMin = 20f; text.fontSizeMax = 36f; text.fontSize = 36f;
				EditorUtility.SetDirty(text); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
				File.AppendAllText(report, "PASS " + name + ": collected/required, no parentheses, no wrap, 20-36pt auto sizing, icon margin retained\n");
			}
			EditorApplication.Exit(0);
		}
		catch (Exception e) {File.AppendAllText(report, e + "\n"); EditorApplication.Exit(1);}
	}
}
