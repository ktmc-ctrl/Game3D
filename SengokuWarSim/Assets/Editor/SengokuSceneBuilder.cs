using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SengokuWarSim.Editor
{
    /// <summary>メニューから戦場シーンを組み立てて保存する。すべて Undo 可能。</summary>
    public static class SengokuSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/Battle.unity";

        [MenuItem("Sengoku/Build Battle Scene", false, 1)]
        public static void BuildAndSave()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            PopulateScene();
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterInBuildSettings();
            Debug.Log($"[Sengoku] Battle scene saved to {ScenePath}");
        }

        [MenuItem("Sengoku/Add Bootstrap To Current Scene", false, 2)]
        public static void AddToCurrentScene()
        {
            PopulateScene();
        }

        [MenuItem("Sengoku/Build Battle Scene And Play", false, 20)]
        public static void BuildAndPlay()
        {
            BuildAndSave();
            EditorApplication.EnterPlaymode();
        }

        /// <summary>現在のシーンにブートストラップ一式を追加する。既にあれば何もしない。</summary>
        public static GameObject PopulateScene()
        {
            var existing = Object.FindObjectOfType<BattleBootstrap>();
            if (existing != null) return existing.gameObject;

            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Build Sengoku battle scene");

            var go = new GameObject("BattleBootstrap");
            Undo.RegisterCreatedObjectUndo(go, "Create BattleBootstrap");
            Undo.AddComponent<BattleBootstrap>(go);
            Undo.AddComponent<PlayerCommander>(go);
            Undo.AddComponent<BattleHUD>(go);

            var cam = Camera.main;
            if (cam != null && cam.GetComponent<RTSCamera>() == null)
            {
                Undo.AddComponent<RTSCamera>(cam.gameObject);
                Undo.RecordObject(cam.transform, "Place camera");
                cam.transform.position = new Vector3(0f, 40f, -75f);
                cam.transform.rotation = Quaternion.Euler(50f, 0f, 0f);
            }

            var light = Object.FindObjectOfType<Light>();
            if (light != null)
            {
                Undo.RecordObject(light.transform, "Orient sun");
                light.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
            }

            Undo.CollapseUndoOperations(group);
            return go;
        }

        static void RegisterInBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes;
            foreach (var s in scenes) if (s.path == ScenePath) return;
            var list = new EditorBuildSettingsScene[scenes.Length + 1];
            scenes.CopyTo(list, 0);
            list[scenes.Length] = new EditorBuildSettingsScene(ScenePath, true);
            EditorBuildSettings.scenes = list;
        }
    }
}
