using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RecoveryCheck
{
    [MenuItem("Recovery/Open Start Menu")]
    public static void OpenMenu()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/StartMenu.unity");
    }
    static string output;
    static double deadline;
    static int stage;
    static int runtimeErrors;
    static bool oldOptionsEnabled;
    static EnterPlayModeOptions oldOptions;
    static bool gameplayChecked;
    static int totalErrors;
    static readonly string[] scenes = { "StartMenu", "GameScene_AC", "GameScene_MA", "GameScene_WW", "CharacterSelection" };
    public static void Run()
    {
        output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Unity-scene-test.txt"));
        File.WriteAllText(output, "Unity recovery scene test " + Application.unityVersion + "\n");
        Application.logMessageReceived += OnLog;
        oldOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
        oldOptions = EditorSettings.enterPlayModeOptions;
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        try
        {
            foreach (string name in scenes)
            {
                Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity");
                int missing = 0, objects = 0;
                foreach (GameObject root in scene.GetRootGameObjects())
                    foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                    {
                        objects++;
                        if (name == "StartMenu" && child.name == "Image" && GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) > 0)
                        {
                            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
                            EditorSceneManager.MarkSceneDirty(scene);
                        }
                        missing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject);
                    }
                if (scene.isDirty) EditorSceneManager.SaveScene(scene);
                File.AppendAllText(output, name + ": opened; objects=" + objects + "; missingScripts=" + missing + "\n");
            }
            stage = 0;
            totalErrors = 0;
            BeginScene();
            EditorApplication.update += Tick;
        }
        catch (Exception e) { File.AppendAllText(output, "TEST FAILED: " + e + "\n"); RestoreOptions(); EditorApplication.Exit(1); }
    }
    static void OnLog(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        {
            runtimeErrors++;
            File.AppendAllText(output, "[" + type + "] " + message + "\n" + stack + "\n");
        }
    }
    static void BeginScene()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/" + scenes[stage] + ".unity");
        runtimeErrors = 0;
        gameplayChecked = false;
        Time.timeScale = 1f;
        PlayerControl.Hp = 3;
        EventControl.Route = stage == 2 ? 123.45f : 0f;
        EditorApplication.isPlaying = true;
        deadline = EditorApplication.timeSinceStartup + 12;
    }
    static void Tick()
    {
        if (EditorApplication.isPlaying && !gameplayChecked && EditorApplication.timeSinceStartup >= deadline - 10 && stage >= 1 && stage <= 3)
        {
            gameplayChecked = true;
            try { ValidateGameplay(); }
            catch (Exception e) { Debug.LogError("Regression failed: " + e); }
        }
        if (EditorApplication.timeSinceStartup < deadline || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (EditorApplication.isPlaying)
        {
            File.AppendAllText(output, scenes[stage] + ": play mode completed; errors=" + runtimeErrors + "\n");
            totalErrors += runtimeErrors;
            EditorApplication.isPlaying = false;
            deadline = EditorApplication.timeSinceStartup + 2;
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        stage++;
        if (stage < scenes.Length) BeginScene();
        else { File.AppendAllText(output, "ALL CHECKS FINISHED; total errors=" + totalErrors + "\n"); EditorApplication.update -= Tick; RestoreOptions(); EditorApplication.Exit(totalErrors == 0 ? 0 : 1); }
    }
    static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
    static void ValidateGameplay()
    {
        var controller = UnityEngine.Object.FindObjectOfType<pause>();
        Require(controller != null, "Pause controller missing");
        if (stage == 2)
        {
            var label = GameObject.Find("Canvas").transform.Find("board_1").Find("route").GetComponent<TMPro.TextMeshProUGUI>();
            Require(label.text == "123.45m", "Transition HUD did not preserve route: " + label.text);
            File.AppendAllText(output, "PASS: transition HUD preserves 123.45m during intro\n");
        }
        EventControl.isplay = false;
        controller.pauseBtn();
        Require(controller.menu.activeSelf && !pause.menuKeys && Time.timeScale == 0f, "Pause state mismatch");
        controller.returnBtn();
        Require(!controller.menu.activeSelf && pause.menuKeys && Time.timeScale == 1f && !EventControl.isplay, "Intro resumed prematurely");
        EventControl.isplay = true;
        controller.pauseBtn();
        controller.pauseBtn();
        controller.returnBtn();
        Require(EventControl.isplay && pause.menuKeys && Time.timeScale == 1f, "Normal resume failed");
        PlayerControl.Hp = 0;
        controller.pauseBtn();
        Require(!controller.menu.activeSelf && Time.timeScale == 1f, "Pause opened after death");
        PlayerControl.Hp = 3;
        EventControl.isplay = false;
        File.AppendAllText(output, "PASS: pause/resume, repeated pause, intro state, death guard: " + scenes[stage] + "\n");
    }
    static void RestoreOptions() { EditorSettings.enterPlayModeOptionsEnabled = oldOptionsEnabled; EditorSettings.enterPlayModeOptions = oldOptions; }
}
