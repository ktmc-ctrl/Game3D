// Gemma4UnityController.cs
// Editor window that sends a natural-language request to a local
// OpenAI-compatible chat endpoint (Ollama by default) and applies the
// returned JSON actions to the open scene. Only `create`, `transform` and
// `set_color` actions are accepted; every mutation is recorded with Undo.
//
// Install: copy to <project>/Assets/Editor/Gemma4UnityController.cs
// Open:    Window > Gemma > Scene Controller

using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace GemmaTools
{
    public sealed class Gemma4UnityController : EditorWindow
    {
        const string PrefKeyUrl = "Gemma4Unity.Url";
        const string PrefKeyModel = "Gemma4Unity.Model";

        const string SystemPrompt =
            "You control a Unity scene. Reply ONLY with JSON of the form " +
            "{\"actions\":[...]} and nothing else. Allowed actions:\n" +
            "{\"action\":\"create\",\"name\":\"Crate\",\"primitive\":\"cube|sphere|capsule|cylinder|plane|quad\"," +
            "\"position\":[x,y,z],\"rotation\":[x,y,z],\"scale\":[x,y,z],\"color\":[r,g,b]}\n" +
            "{\"action\":\"transform\",\"name\":\"Crate\",\"position\":[x,y,z],\"rotation\":[x,y,z],\"scale\":[x,y,z]}\n" +
            "{\"action\":\"set_color\",\"name\":\"Crate\",\"color\":[r,g,b]}\n" +
            "Colors are 0..1 floats. Omit fields you do not want to change. Units are metres.";

        string _url = "http://localhost:11434/v1/chat/completions";
        string _model = "gemma3:4b";
        string _prompt = "Create a red cube at (0,0.5,0) and a blue sphere 2 metres to its right.";
        string _log = "";
        Vector2 _logScroll;
        UnityWebRequest _inFlight;

        [MenuItem("Window/Gemma/Scene Controller")]
        public static void Open()
        {
            var window = GetWindow<Gemma4UnityController>("Gemma Scene");
            window.minSize = new Vector2(420, 360);
        }

        void OnEnable()
        {
            _url = EditorPrefs.GetString(PrefKeyUrl, _url);
            _model = EditorPrefs.GetString(PrefKeyModel, _model);
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Local OpenAI-compatible endpoint", EditorStyles.boldLabel);
            _url = EditorGUILayout.TextField("URL", _url);
            _model = EditorGUILayout.TextField("Model", _model);
            if (GUI.changed)
            {
                EditorPrefs.SetString(PrefKeyUrl, _url);
                EditorPrefs.SetString(PrefKeyModel, _model);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Request", EditorStyles.boldLabel);
            _prompt = EditorGUILayout.TextArea(_prompt, GUILayout.MinHeight(70));

            using (new EditorGUI.DisabledScope(_inFlight != null))
            {
                if (GUILayout.Button(_inFlight != null ? "Waiting for model..." : "Send"))
                {
                    Send();
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Log", EditorStyles.boldLabel);
            _logScroll = EditorGUILayout.BeginScrollView(_logScroll, GUILayout.ExpandHeight(true));
            EditorGUILayout.TextArea(_log, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        void Send()
        {
            var body = new ChatRequest
            {
                model = _model,
                messages = new[]
                {
                    new ChatMessage { role = "system", content = SystemPrompt },
                    new ChatMessage { role = "user", content = _prompt },
                },
                temperature = 0.1f,
            };
            string json = JsonUtility.ToJson(body);

            _inFlight = new UnityWebRequest(_url, "POST");
            _inFlight.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            _inFlight.downloadHandler = new DownloadHandlerBuffer();
            _inFlight.SetRequestHeader("Content-Type", "application/json");
            _inFlight.timeout = 120;
            var op = _inFlight.SendWebRequest();
            op.completed += _ => OnResponse();
            Log("> " + _prompt);
        }

        void OnResponse()
        {
            var req = _inFlight;
            _inFlight = null;
            try
            {
                if (req.result != UnityWebRequest.Result.Success)
                {
                    Log("HTTP error: " + req.error + "\n" + req.downloadHandler.text);
                    return;
                }

                var response = JsonUtility.FromJson<ChatResponse>(req.downloadHandler.text);
                if (response == null || response.choices == null || response.choices.Length == 0)
                {
                    Log("Empty response: " + req.downloadHandler.text);
                    return;
                }

                string content = StripCodeFence(response.choices[0].message.content);
                Log("< " + content);
                ApplyActions(content);
            }
            catch (Exception e)
            {
                Log("Exception: " + e.Message);
            }
            finally
            {
                req.Dispose();
                Repaint();
            }
        }

        // ---------------------------------------------------------------- actions

        public void ApplyActions(string json)
        {
            ActionList list;
            try
            {
                list = JsonUtility.FromJson<ActionList>(json);
            }
            catch (Exception e)
            {
                Log("Invalid JSON: " + e.Message);
                return;
            }

            if (list == null || list.actions == null)
            {
                Log("No actions found.");
                return;
            }

            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Gemma scene actions");
            int applied = 0;
            foreach (var a in list.actions)
            {
                string error;
                if (!ApplyOne(a, out error)) Log("  skipped: " + error);
                else applied++;
            }
            Undo.CollapseUndoOperations(group);
            Log($"Applied {applied}/{list.actions.Length} action(s).");
        }

        static readonly HashSet<string> AllowedPrimitives = new HashSet<string>
        {
            "cube", "sphere", "capsule", "cylinder", "plane", "quad"
        };

        bool ApplyOne(SceneAction a, out string error)
        {
            error = null;
            if (a == null) { error = "null action"; return false; }
            if (string.IsNullOrEmpty(a.name) || a.name.Length > 64) { error = "invalid name"; return false; }

            switch (a.action)
            {
                case "create":
                {
                    if (a.primitive == null || !AllowedPrimitives.Contains(a.primitive.ToLowerInvariant()))
                    {
                        error = "unknown primitive '" + a.primitive + "'";
                        return false;
                    }
                    var type = (PrimitiveType)Enum.Parse(typeof(PrimitiveType), Capitalize(a.primitive), true);
                    var go = GameObject.CreatePrimitive(type);
                    go.name = a.name;
                    Undo.RegisterCreatedObjectUndo(go, "Create " + a.name);
                    ApplyTransform(go, a);
                    if (IsVec3(a.color)) ApplyColor(go, a.color);
                    return true;
                }
                case "transform":
                {
                    var go = GameObject.Find(a.name);
                    if (go == null) { error = "object not found: " + a.name; return false; }
                    Undo.RecordObject(go.transform, "Transform " + a.name);
                    ApplyTransform(go, a);
                    return true;
                }
                case "set_color":
                {
                    var go = GameObject.Find(a.name);
                    if (go == null) { error = "object not found: " + a.name; return false; }
                    if (!IsVec3(a.color)) { error = "color must have 3 components"; return false; }
                    ApplyColor(go, a.color);
                    return true;
                }
                default:
                    error = "unsupported action '" + a.action + "'";
                    return false;
            }
        }

        static void ApplyTransform(GameObject go, SceneAction a)
        {
            var t = go.transform;
            if (IsVec3(a.position)) t.position = Clamp(ToVec3(a.position), 1000f);
            if (IsVec3(a.rotation)) t.rotation = Quaternion.Euler(ToVec3(a.rotation));
            if (IsVec3(a.scale))
            {
                var s = ToVec3(a.scale);
                s = new Vector3(Mathf.Clamp(s.x, 0.01f, 1000f), Mathf.Clamp(s.y, 0.01f, 1000f), Mathf.Clamp(s.z, 0.01f, 1000f));
                t.localScale = s;
            }
        }

        static void ApplyColor(GameObject go, float[] rgb)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;
            var color = new Color(Mathf.Clamp01(rgb[0]), Mathf.Clamp01(rgb[1]), Mathf.Clamp01(rgb[2]));
            var shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit") ?? renderer.sharedMaterial.shader;
            var mat = new Material(shader) { name = go.name + "_Mat", color = color };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            Undo.RecordObject(renderer, "Set color " + go.name);
            renderer.sharedMaterial = mat;
        }

        static bool IsVec3(float[] v) => v != null && v.Length == 3 &&
                                         !float.IsNaN(v[0]) && !float.IsNaN(v[1]) && !float.IsNaN(v[2]);
        static Vector3 ToVec3(float[] v) => new Vector3(v[0], v[1], v[2]);
        static Vector3 Clamp(Vector3 v, float limit) =>
            new Vector3(Mathf.Clamp(v.x, -limit, limit), Mathf.Clamp(v.y, -limit, limit), Mathf.Clamp(v.z, -limit, limit));
        static string Capitalize(string s) => char.ToUpperInvariant(s[0]) + s.Substring(1).ToLowerInvariant();

        static string StripCodeFence(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Trim();
            if (s.StartsWith("```"))
            {
                int firstNewline = s.IndexOf('\n');
                if (firstNewline >= 0) s = s.Substring(firstNewline + 1);
                int fence = s.LastIndexOf("```", StringComparison.Ordinal);
                if (fence >= 0) s = s.Substring(0, fence);
            }
            int start = s.IndexOf('{');
            int end = s.LastIndexOf('}');
            if (start >= 0 && end > start) s = s.Substring(start, end - start + 1);
            return s.Trim();
        }

        void Log(string line)
        {
            _log += line + "\n";
            if (_log.Length > 20000) _log = _log.Substring(_log.Length - 20000);
            Repaint();
        }

        // ---------------------------------------------------------------- DTOs

        [Serializable] class ChatMessage { public string role; public string content; }
        [Serializable] class ChatRequest { public string model; public ChatMessage[] messages; public float temperature; }
        [Serializable] class ChatChoice { public ChatMessage message; }
        [Serializable] class ChatResponse { public ChatChoice[] choices; }

        [Serializable]
        public class SceneAction
        {
            public string action;
            public string name;
            public string primitive;
            public float[] position;
            public float[] rotation;
            public float[] scale;
            public float[] color;
        }

        [Serializable] public class ActionList { public SceneAction[] actions; }
    }
}
