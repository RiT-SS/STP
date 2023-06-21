using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildPlayableRelease
{
	public static void Run()
	{
		string[] args = Environment.GetCommandLineArgs();
		int index = Array.IndexOf(args, "-releasePath");
		if (index < 0 || index + 1 >= args.Length) throw new Exception("Missing -releasePath");
		string output = Path.GetFullPath(args[index + 1]);
		if (Directory.Exists(output)) throw new Exception("Output directory already exists: " + output);
		var options = new BuildPlayerOptions();
		options.scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
		if (options.scenes.Length != 5 || !options.scenes[0].EndsWith("StartMenu.unity")) throw new Exception("Unexpected scene list");
		options.locationPathName = Path.Combine(output, "时空之门.exe");
		options.target = BuildTarget.StandaloneWindows64;
		options.options = BuildOptions.None;
		BuildReport report = BuildPipeline.BuildPlayer(options);
		Debug.Log("RELEASE_BUILD " + report.summary.result + " | errors=" + report.summary.totalErrors + " | warnings=" + report.summary.totalWarnings + " | bytes=" + report.summary.totalSize);
		if (report.summary.result != BuildResult.Succeeded) throw new Exception("Release build failed");
		File.Copy(Path.Combine(Application.dataPath, "../游玩说明.txt"), Path.Combine(output, "游玩说明.txt"), true);
		string save = Path.Combine(output, "SaveData");
		if (Directory.Exists(save) || Directory.GetFiles(output, "game-records*", SearchOption.AllDirectories).Length != 0) throw new Exception("Release contains game records");
	}
}
