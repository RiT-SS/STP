using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ApplyRageFollow
{
	public static void Run()
	{
		string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../RageFollowReview"));
		Directory.CreateDirectory(folder); string report = Path.Combine(folder, "apply.txt");
		File.WriteAllText(report, "Robot terrain following and 3 second rage\n");
		try
		{
			foreach (string name in new[] {"GameScene_AC", "GameScene_WW", "GameScene_MA"})
			{
				var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity");
				var skills = UnityEngine.Object.FindObjectOfType<SkillControl>(); skills.ltTime = 3f;
				EditorUtility.SetDirty(skills); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
				File.AppendAllText(report, "PASS " + name + ": rage=3s; cooldown=" + skills.cdTime + "s\n");
			}
			EditorApplication.Exit(0);
		}
		catch (Exception e) {File.AppendAllText(report, e.ToString()); EditorApplication.Exit(1);}
	}
}
