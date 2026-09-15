using System.Collections.Generic;
using SengokuWarSim.Sim;
using UnityEngine;

namespace SengokuWarSim
{
    /// <summary>
    /// 戦場の入口。シーンに置くだけで地形・村・両軍・カメラ・HUD をすべて実行時に組み立てる。
    /// 外部アセットに依存しないので、空のシーンにこのコンポーネント一つで動く。
    /// </summary>
    public sealed class BattleBootstrap : MonoBehaviour
    {
        [Header("Scenario")]
        [Tooltip("地形と戦闘の乱数シード。同じ値なら同じ戦場・同じ結果になる。")]
        public int Seed = 7;
        [Tooltip("戦場の一辺 (m)。偶数。")]
        public int MapSize = 120;
        public Faction PlayerFaction = Faction.Kamimura;

        [Header("Simulation")]
        [Range(0f, 4f)] public float Speed = 1f;
        public bool Paused;
        public bool EnemyAI = true;
        [Tooltip("プレイヤー側も AI に任せる (観戦モード)。")]
        public bool PlayerAI;

        public BattleSim Sim { get; private set; }
        public TerrainMap Map { get; private set; }
        public ProceduralTerrainView TerrainView { get; private set; }
        public IReadOnlyList<SquadView> Views => _views;
        public Faction EnemyFaction => PlayerFaction == Faction.Kamimura ? Faction.Shimomura : Faction.Kamimura;
        public int Restarts { get; private set; }

        readonly List<SquadView> _views = new List<SquadView>();
        readonly Dictionary<int, SquadView> _viewById = new Dictionary<int, SquadView>();
        readonly List<BattleAI> _ais = new List<BattleAI>();
        Transform _world;
        Transform _fxRoot;
        float _accumulator;

        public event System.Action Rebuilt;

        void Awake()
        {
            EnsureSceneServices();
            Build();
        }

        void EnsureSceneServices()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            cam.farClipPlane = 600f;
            cam.backgroundColor = new Color(0.72f, 0.80f, 0.90f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            if (cam.GetComponent<RTSCamera>() == null) cam.gameObject.AddComponent<RTSCamera>();

            if (FindObjectOfType<Light>() == null)
            {
                var lightGo = new GameObject("Sun");
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.color = new Color(1f, 0.96f, 0.88f);
                light.intensity = 1.1f;
                light.shadows = LightShadows.Soft;
                lightGo.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
            }
            RenderSettings.ambientLight = new Color(0.45f, 0.48f, 0.52f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 120f;
            RenderSettings.fogEndDistance = 400f;
            RenderSettings.fogColor = new Color(0.72f, 0.80f, 0.90f);

            if (GetComponent<PlayerCommander>() == null) gameObject.AddComponent<PlayerCommander>();
            if (GetComponent<BattleHUD>() == null) gameObject.AddComponent<BattleHUD>();
        }

        public void Build()
        {
            if (_world != null) Destroy(_world.gameObject);
            _views.Clear();
            _viewById.Clear();
            _ais.Clear();
            _accumulator = 0f;

            if (MapSize < 40) MapSize = 40;
            if ((MapSize & 1) == 1) MapSize++;

            _world = new GameObject("World").transform;
            _fxRoot = new GameObject("FX").transform;
            _fxRoot.SetParent(_world, false);
            var corpses = new GameObject("Fallen").transform;
            corpses.SetParent(_world, false);
            var squadsRoot = new GameObject("Squads").transform;
            squadsRoot.SetParent(_world, false);

            Map = TerrainMap.Generate(MapSize, Seed);
            TerrainView = ProceduralTerrainView.Build(Map, _world);
            VillageBuilder.Build(Map, TerrainView, _world, Seed);

            Sim = ScenarioBuilder.BuildSkirmish(Map, Seed);
            foreach (var squad in Sim.Squads)
            {
                var view = SquadView.Create(squad, TerrainView, squadsRoot, corpses);
                _views.Add(view);
                _viewById[squad.Id] = view;
            }

            if (EnemyAI) _ais.Add(new BattleAI(Sim, EnemyFaction));
            if (PlayerAI) _ais.Add(new BattleAI(Sim, PlayerFaction));

            Sim.VolleyFired += OnVolleyFired;

            var cam = Camera.main != null ? Camera.main.GetComponent<RTSCamera>() : null;
            if (cam != null)
            {
                cam.MapHalfSize = Map.HalfSize;
                float homeZ = BattleSim.HomeDirection(PlayerFaction).Z * Map.HalfSize * 0.55f;
                cam.SnapTo(new Vector3(0f, 0f, homeZ), PlayerFaction == Faction.Kamimura ? 0f : 180f);
            }

            Rebuilt?.Invoke();
        }

        public void Restart(bool newSeed)
        {
            if (newSeed) Seed++;
            Restarts++;
            Build();
        }

        void Update()
        {
            if (Sim == null) return;

            if (!Paused && Sim.Outcome == BattleOutcome.Ongoing)
            {
                _accumulator += Time.deltaTime * Speed;
                int guard = 0;
                while (_accumulator >= BattleSim.DefaultTickSeconds && guard++ < 40)
                {
                    Sim.Tick(BattleSim.DefaultTickSeconds);
                    foreach (var ai in _ais) ai.Tick(BattleSim.DefaultTickSeconds);
                    _accumulator -= BattleSim.DefaultTickSeconds;
                }
            }

            float dt = Time.deltaTime;
            foreach (var view in _views) view.Sync(dt);
        }

        public SquadView GetView(int squadId) => _viewById.TryGetValue(squadId, out var v) ? v : null;

        void OnVolleyFired(Squad attacker, Squad target)
        {
            if (!attacker.Stats.IsRanged) return;
            if (!_viewById.TryGetValue(attacker.Id, out var from) || !_viewById.TryGetValue(target.Id, out var to)) return;
            if (Vector3.Distance(from.transform.position, to.transform.position) < BattleSim.MeleeRange + 0.5f) return;

            bool arrow = attacker.Kind == UnitKind.HuntingBow;
            int count = Mathf.Clamp(attacker.Soldiers / 4, 2, 6);
            for (int i = 0; i < count; i++)
            {
                Vector3 offset = new Vector3(Random.Range(-2.5f, 2.5f), 1.3f, Random.Range(-1.5f, 1.5f));
                Vector3 landing = new Vector3(Random.Range(-2.5f, 2.5f), 0.2f, Random.Range(-2f, 2f));
                ProjectileFX.Spawn(from.WorldCenter + offset, to.WorldCenter + landing, arrow, _fxRoot);
            }
        }
    }
}
