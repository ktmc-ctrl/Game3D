using SengokuWarSim.Sim;
using UnityEngine;

namespace SengokuWarSim
{
    /// <summary>IMGUI で描く戦況表示。両軍の兵力、部隊ラベル、選択中部隊、操作説明、勝敗。</summary>
    public sealed class BattleHUD : MonoBehaviour
    {
        BattleBootstrap _boot;
        PlayerCommander _commander;
        Texture2D _white;
        GUIStyle _label;
        GUIStyle _small;
        GUIStyle _big;
        GUIStyle _box;

        void Awake()
        {
            _boot = GetComponent<BattleBootstrap>();
            _commander = GetComponent<PlayerCommander>();
            _white = new Texture2D(1, 1);
            _white.SetPixel(0, 0, Color.white);
            _white.Apply();
        }

        void EnsureStyles()
        {
            if (_label != null) return;
            _label = new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = Color.white }, alignment = TextAnchor.MiddleCenter };
            _small = new GUIStyle(GUI.skin.label) { fontSize = 12, normal = { textColor = Color.white } };
            _big = new GUIStyle(GUI.skin.label) { fontSize = 40, fontStyle = FontStyle.Bold, normal = { textColor = Color.white }, alignment = TextAnchor.MiddleCenter };
            _box = new GUIStyle(GUI.skin.box) { normal = { textColor = Color.white }, alignment = TextAnchor.UpperLeft, fontSize = 13, wordWrap = true };
        }

        void OnGUI()
        {
            if (_boot == null || _boot.Sim == null) return;
            EnsureStyles();
            DrawSquadLabels();
            DrawTopBar();
            DrawSelection();
            DrawHelp();
            DrawDragBox();
            DrawOutcome();
        }

        static string FactionName(Faction f) => f == Faction.Kamimura ? "上ノ村" : "下ノ村";

        void DrawTopBar()
        {
            var sim = _boot.Sim;
            float w = Screen.width;
            Fill(new Rect(0, 0, w, 44), new Color(0f, 0f, 0f, 0.55f));

            DrawStrength(new Rect(16, 8, w * 0.35f, 28), Faction.Kamimura, true);
            DrawStrength(new Rect(w - 16 - w * 0.35f, 8, w * 0.35f, 28), Faction.Shimomura, false);

            int minutes = (int)(sim.Time / 60f);
            int seconds = (int)(sim.Time % 60f);
            string state = _boot.Paused ? "一時停止" : $"x{_boot.Speed:0.#}";
            GUI.Label(new Rect(w * 0.5f - 100, 8, 200, 28), $"{minutes:00}:{seconds:00}   {state}", _label);
        }

        void DrawStrength(Rect r, Faction f, bool leftAligned)
        {
            var sim = _boot.Sim;
            int alive = sim.CountSoldiers(f);
            int max = sim.CountMaxSoldiers(f);
            float ratio = max > 0 ? alive / (float)max : 0f;
            Fill(r, new Color(0.15f, 0.15f, 0.15f, 0.9f));
            var fill = new Rect(r.x, r.y, r.width * ratio, r.height);
            if (!leftAligned) fill.x = r.xMax - fill.width;
            Fill(fill, Palette.FactionColor(f));
            string you = f == _boot.PlayerFaction ? " (あなた)" : "";
            GUI.Label(r, $"{FactionName(f)}{you}  {alive}/{max}", _label);
        }

        void DrawSquadLabels()
        {
            var cam = Camera.main;
            if (cam == null) return;
            foreach (var view in _boot.Views)
            {
                var s = view.Squad;
                if (!s.IsAlive) continue;
                Vector3 sp = cam.WorldToScreenPoint(view.WorldCenter + Vector3.up * 2.4f);
                if (sp.z <= 0f) continue;
                float x = sp.x;
                float y = Screen.height - sp.y;
                float scale = Mathf.Clamp(60f / Mathf.Max(sp.z, 1f), 0.6f, 1.2f);
                float w = 96f * scale;
                float h = 34f * scale;
                var rect = new Rect(x - w * 0.5f, y - h, w, h);

                Color bg = view.Selected ? new Color(1f, 0.85f, 0.2f, 0.85f) : new Color(0f, 0f, 0f, 0.55f);
                Fill(rect, bg);
                Fill(new Rect(rect.x, rect.y, 4f, rect.height), Palette.FactionColor(s.Faction));

                string status = s.IsRouting ? " 敗走!" : s.State == SquadState.Engaging ? " 交戦" : "";
                GUI.Label(new Rect(rect.x + 6, rect.y, rect.width - 6, rect.height * 0.55f),
                    $"{s.NameJa} {s.Soldiers}/{s.MaxSoldiers}{status}", _small);

                // 士気バー
                var mr = new Rect(rect.x + 8, rect.yMax - 7f * scale, rect.width - 16, 4f * scale);
                Fill(mr, new Color(0.2f, 0.2f, 0.2f, 0.9f));
                Color mc = s.Morale < BattleSim.RoutThreshold ? Color.red : s.Morale < BattleSim.RallyThreshold ? new Color(1f, 0.6f, 0.1f) : new Color(0.3f, 0.9f, 0.3f);
                Fill(new Rect(mr.x, mr.y, mr.width * (s.Morale / 100f), mr.height), mc);
            }
        }

        void DrawSelection()
        {
            if (_commander == null || _commander.Selected.Count == 0) return;
            float h = 24f + _commander.Selected.Count * 20f;
            var rect = new Rect(12, Screen.height - h - 12, 300, h);
            Fill(rect, new Color(0f, 0f, 0f, 0.6f));
            GUI.Label(new Rect(rect.x + 8, rect.y + 2, rect.width, 20), "選択中の部隊", _small);
            for (int i = 0; i < _commander.Selected.Count; i++)
            {
                var s = _commander.Selected[i].Squad;
                string terrain = TerrainName(_boot.Map.KindAt(s.Position));
                GUI.Label(new Rect(rect.x + 8, rect.y + 22 + i * 20, rect.width, 20),
                    $"{s.NameJa}  {s.Soldiers}/{s.MaxSoldiers}  士気 {s.Morale:0}  {StateName(s.State)}  [{terrain}]", _small);
            }
        }

        void DrawHelp()
        {
            const string text =
                "左クリック/ドラッグ: 選択  Shift: 追加\n" +
                "右クリック: 移動 / 敵をクリックで攻撃\n" +
                "WASD: 移動  Q/E・中ドラッグ: 回転  ホイール: ズーム\n" +
                "Space: 一時停止  1/2/3: 速度  H: 停止  Ctrl+A: 全選択\n" +
                "水田: 遅い・不利  丘: 守備有利・射程+  林: 矢を防ぐ";
            var rect = new Rect(Screen.width - 372, Screen.height - 112, 360, 100);
            GUI.Box(rect, text, _box);
        }

        void DrawDragBox()
        {
            if (_commander == null || !_commander.IsDragging) return;
            Rect r = _commander.DragRect;
            var gui = new Rect(r.x, Screen.height - r.yMax, r.width, r.height);
            Fill(gui, new Color(1f, 0.9f, 0.3f, 0.15f));
            Outline(gui, new Color(1f, 0.9f, 0.3f, 0.9f));
        }

        void DrawOutcome()
        {
            var outcome = _boot.Sim.Outcome;
            if (outcome == BattleOutcome.Ongoing) return;

            bool playerWon = (outcome == BattleOutcome.KamimuraWins && _boot.PlayerFaction == Faction.Kamimura) ||
                             (outcome == BattleOutcome.ShimomuraWins && _boot.PlayerFaction == Faction.Shimomura);
            string title = outcome == BattleOutcome.Draw ? "引き分け" : playerWon ? "勝利 ― 村を守った" : "敗北 ― 村は焼かれた";

            Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.45f));
            var panel = new Rect(Screen.width * 0.5f - 220, Screen.height * 0.5f - 90, 440, 180);
            Fill(panel, new Color(0.08f, 0.08f, 0.1f, 0.95f));
            GUI.Label(new Rect(panel.x, panel.y + 16, panel.width, 60), title, _big);
            int k = _boot.Sim.CountSoldiers(Faction.Kamimura);
            int s = _boot.Sim.CountSoldiers(Faction.Shimomura);
            GUI.Label(new Rect(panel.x, panel.y + 80, panel.width, 24), $"上ノ村 残 {k}   下ノ村 残 {s}", _label);
            if (GUI.Button(new Rect(panel.x + 60, panel.y + 120, 150, 36), "同じ戦場で再戦 (R)")) _boot.Restart(false);
            if (GUI.Button(new Rect(panel.x + 230, panel.y + 120, 150, 36), "新しい戦場")) _boot.Restart(true);
        }

        static string StateName(SquadState s)
        {
            switch (s)
            {
                case SquadState.Idle: return "待機";
                case SquadState.Moving: return "移動";
                case SquadState.Engaging: return "交戦";
                case SquadState.Routing: return "敗走";
                default: return "壊滅";
            }
        }

        static string TerrainName(TerrainKind k)
        {
            switch (k)
            {
                case TerrainKind.Paddy: return "水田";
                case TerrainKind.Hill: return "丘";
                case TerrainKind.Forest: return "林";
                case TerrainKind.Road: return "街道";
                default: return "畑";
            }
        }

        void Fill(Rect r, Color c)
        {
            var prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, _white);
            GUI.color = prev;
        }

        void Outline(Rect r, Color c)
        {
            Fill(new Rect(r.x, r.y, r.width, 1), c);
            Fill(new Rect(r.x, r.yMax - 1, r.width, 1), c);
            Fill(new Rect(r.x, r.y, 1, r.height), c);
            Fill(new Rect(r.xMax - 1, r.y, 1, r.height), c);
        }
    }
}
