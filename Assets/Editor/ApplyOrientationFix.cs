using System;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ApplyOrientationFix
{
	public static void Run()
	{
		string report = Path.GetFullPath(Path.Combine(Application.dataPath, "../../OrientationReview/apply.txt"));
		Directory.CreateDirectory(Path.GetDirectoryName(report));
		File.WriteAllText(report, "Obstacle classification and preview animation sync\n");
		try
		{
			int count = 0;
			foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/GameObject" }))
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				GameObject root = PrefabUtility.LoadPrefabContents(path);
				int added = Classify(root);
				if (added > 0) PrefabUtility.SaveAsPrefabAsset(root, path);
				PrefabUtility.UnloadPrefabContents(root);
				count += added;
			}
			foreach (string name in new[] { "GameScene_AC", "GameScene_WW", "GameScene_MA" })
			{
				var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity");
				foreach (GameObject root in scene.GetRootGameObjects()) count += Classify(root);
				EditorSceneManager.MarkSceneDirty(scene);
				EditorSceneManager.SaveScene(scene);
			}
			File.AppendAllText(report, "Classified objects=" + count + "\n");
			string[] characters = Array.ConvertAll(UnityEngine.Object.FindObjectOfType<PlayerControl>().character, c => AssetDatabase.GetAssetPath(c));
			var selection = EditorSceneManager.OpenScene("Assets/Scenes/CharacterSelection.unity");
			Transform menu = UnityEngine.Object.FindObjectOfType<角色选择界面>().transform;
			string[] previews = { "adventureGirlRun", "NinjaGirlRun", "RobotRun" };
			string[] clips = { "adventureGitrrun", "girl2", "robotRun" };
			for (int i = 0; i < previews.Length; i++)
			{
				AnimatorController game = AssetDatabase.LoadAssetAtPath<AnimatorController>(characters[i]);
				AnimatorState run = Array.Find(game.layers[0].stateMachine.states, s => s.state.name == "run").state;
				if (run == null) throw new Exception("Missing run state " + game.name);
				AnimatorController preview = (AnimatorController)menu.Find(previews[i]).GetComponent<Animator>().runtimeAnimatorController;
				if (preview == game || preview.layers[0].stateMachine.states.Length != 1) throw new Exception("Unexpected preview controller");
				AnimatorState state = preview.layers[0].stateMachine.states[0].state;
				AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/AnimationClip/" + clips[i] + ".anim");
				if (clip == null || !(run.motion is AnimationClip)) throw new Exception("Missing running clip");
				state.motion = clip;
				state.speed = clip.length / ((AnimationClip)run.motion).length * run.speed;
				state.cycleOffset = run.cycleOffset;
				EditorUtility.SetDirty(state);
				EditorUtility.SetDirty(preview);
				File.AppendAllText(report, previews[i] + " -> original preview sprites; speed=" + state.speed + "; matches " + run.motion.name + " cycle\n");
			}
			AssetDatabase.SaveAssets();
			EditorApplication.Exit(0);
		}
		catch (Exception e) {File.AppendAllText(report, e.ToString()); EditorApplication.Exit(1);}
	}

	static int Classify(GameObject root)
	{
		int count = 0;
		foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
		{
			bool terrain = child.CompareTag("Ground");
			if (!terrain && !child.CompareTag("hinder") && !child.CompareTag("enemy")) continue;
			var type = terrain ? ObstacleControl.ObstacleType.Terrain :
				child.name.StartsWith("AC_enemy_1") || child.name.StartsWith("MA_enemy_1") ? ObstacleControl.ObstacleType.Creature : ObstacleControl.ObstacleType.Obstacle;
			ObstacleControl obstacle = child.GetComponent<ObstacleControl>();
			if (obstacle != null && obstacle.type == type) continue;
			if (obstacle == null) obstacle = child.gameObject.AddComponent<ObstacleControl>();
			obstacle.type = type;
			EditorUtility.SetDirty(obstacle);
			count++;
		}
		return count;
	}
}
