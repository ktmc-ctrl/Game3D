using System.Collections.Generic;
using SengokuWarSim.Sim;
using UnityEngine;

namespace SengokuWarSim
{
    /// <summary>
    /// Sim 側の Squad を 3D 表示に反映する。人数分の村人フィギュアを隊列に並べ、
    /// 死者は倒れた姿勢で戦場に残し、敗走・選択状態を見た目に出す。
    /// </summary>
    public sealed class SquadView : MonoBehaviour
    {
        public const float Spacing = 0.9f;

        public Squad Squad { get; private set; }
        public ProceduralTerrainView Terrain { get; private set; }
        public bool Selected { get; private set; }

        readonly List<Transform> _figures = new List<Transform>();
        readonly List<Vector3> _slots = new List<Vector3>();
        Transform _corpseRoot;
        GameObject _selectionRing;
        Transform _banner;
        float _bobPhase;
        float _smoothYaw;
        System.Random _rng;
        Vector3 _lastPos;
        float _visualSpeed;

        public static SquadView Create(Squad squad, ProceduralTerrainView terrain, Transform parent, Transform corpseRoot)
        {
            var go = new GameObject($"Squad_{squad.Id}_{squad.Faction}_{squad.Kind}");
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<SquadView>();
            view.Squad = squad;
            view.Terrain = terrain;
            view._corpseRoot = corpseRoot;
            view._rng = new System.Random(squad.Id * 7919);
            view.BuildFigures();
            view.BuildDecorations();
            view.SnapToSim();
            return view;
        }

        void BuildFigures()
        {
            int n = Squad.MaxSoldiers;
            int cols = Squad.Kind == UnitKind.Headman ? 4 : 6;
            int rows = Mathf.CeilToInt(n / (float)cols);
            for (int i = 0; i < n; i++)
            {
                int row = i / cols;
                int col = i % cols;
                float jitterX = ((float)_rng.NextDouble() - 0.5f) * 0.2f;
                float jitterZ = ((float)_rng.NextDouble() - 0.5f) * 0.2f;
                var slot = new Vector3((col - (cols - 1) * 0.5f) * Spacing + jitterX, 0f, -row * Spacing + jitterZ);
                _slots.Add(slot);
                var fig = FigureFactory.Build(Squad.Kind, Squad.Faction, transform, $"Villager_{i:00}");
                fig.transform.localPosition = slot;
                fig.transform.localRotation = Quaternion.Euler(0f, ((float)_rng.NextDouble() - 0.5f) * 10f, 0f);
                _figures.Add(fig.transform);
            }

            // 選択・当たり判定用のコライダー (部隊全体を覆う)
            var box = gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.9f, -(rows - 1) * Spacing * 0.5f);
            box.size = new Vector3(cols * Spacing + 0.6f, 1.9f, rows * Spacing + 0.6f);
        }

        void BuildDecorations()
        {
            var ringMat = PrimitiveFactory.GetMaterial("SelectionRing", Palette.Selection, 0.2f);
            var box = GetComponent<BoxCollider>();
            _selectionRing = PrimitiveFactory.CreatePart("SelectionRing", PrimitiveType.Cylinder, ringMat, transform,
                new Vector3(box.center.x, 0.05f, box.center.z), new Vector3(box.size.x * 1.1f, 0.02f, box.size.z * 1.1f));
            _selectionRing.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _selectionRing.SetActive(false);

            // 指物 (旗): 部隊の識別用
            var pole = PrimitiveFactory.CreatePart("BannerPole", PrimitiveType.Cylinder,
                PrimitiveFactory.GetMaterial("Wood", Palette.Wood), transform,
                new Vector3(0f, 1.6f, -0.3f), new Vector3(0.05f, 1.6f, 0.05f));
            _banner = pole.transform;
            PrimitiveFactory.CreatePart("BannerCloth", PrimitiveType.Cube,
                PrimitiveFactory.GetMaterial("Sash_" + Squad.Faction, Palette.FactionColor(Squad.Faction)), pole.transform,
                new Vector3(0f, 0.75f, 0f), new Vector3(9f, 0.28f, 0.6f));
        }

        public void SetSelected(bool selected)
        {
            Selected = selected;
            if (_selectionRing != null) _selectionRing.SetActive(selected);
        }

        public Vector3 WorldCenter => transform.position + transform.rotation * GetComponent<BoxCollider>().center;

        void SnapToSim()
        {
            transform.position = Terrain.ToWorld(Squad.Position);
            _smoothYaw = Squad.Facing.ToAngle() * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, _smoothYaw, 0f);
            _lastPos = transform.position;
        }

        /// <summary>毎フレーム呼ぶ。Sim の状態を補間しつつ反映する。</summary>
        public void Sync(float dt)
        {
            if (Squad == null) return;

            // 位置・向き
            Vector3 target = Terrain.ToWorld(Squad.Position);
            transform.position = Vector3.Lerp(transform.position, target, Mathf.Clamp01(dt * 10f));
            float yaw = Squad.Facing.ToAngle() * Mathf.Rad2Deg;
            _smoothYaw = Mathf.LerpAngle(_smoothYaw, yaw, Mathf.Clamp01(dt * 6f));
            transform.rotation = Quaternion.Euler(0f, _smoothYaw, 0f);

            float moved = (transform.position - _lastPos).magnitude;
            _lastPos = transform.position;
            _visualSpeed = Mathf.Lerp(_visualSpeed, dt > 0f ? moved / dt : 0f, 0.2f);

            // 人数の反映: 減った分は倒れて戦場に残す
            while (_figures.Count > Squad.Soldiers && _figures.Count > 0)
            {
                int idx = _rng.Next(_figures.Count);
                var fig = _figures[idx];
                _figures.RemoveAt(idx);
                _slots.RemoveAt(idx);
                DropCorpse(fig);
            }

            // 歩行アニメーション (上下動 + 前傾)。敗走中は速く。
            bool moving = _visualSpeed > 0.2f;
            float rate = Squad.IsRouting ? 14f : 9f;
            _bobPhase += dt * (moving ? rate : 2f);
            float lean = moving ? (Squad.IsRouting ? 14f : 6f) : 0f;
            for (int i = 0; i < _figures.Count; i++)
            {
                var fig = _figures[i];
                float bob = moving ? Mathf.Abs(Mathf.Sin(_bobPhase + i * 0.7f)) * 0.06f : 0f;
                var slot = _slots[i];
                fig.localPosition = new Vector3(slot.x, bob, slot.z);
                float sway = Squad.State == SquadState.Engaging ? Mathf.Sin(_bobPhase * 2f + i) * 8f : 0f;
                fig.localRotation = Quaternion.Euler(lean + sway, 0f, 0f);
            }

            if (_banner != null)
            {
                _banner.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 2f + Squad.Id) * 4f, 0f, Mathf.Cos(Time.time * 1.7f) * 3f);
                _banner.gameObject.SetActive(Squad.IsAlive);
            }
        }

        void DropCorpse(Transform fig)
        {
            if (_corpseRoot == null)
            {
                Destroy(fig.gameObject);
                return;
            }
            fig.SetParent(_corpseRoot, true);
            var pos = fig.position;
            pos.y = Terrain.Map.HeightAt(new Vec2(pos.x, pos.z)) + 0.15f;
            fig.position = pos;
            fig.rotation = Quaternion.Euler(-90f + ((float)_rng.NextDouble() - 0.5f) * 20f, (float)_rng.NextDouble() * 360f, 0f);
            fig.name = "Fallen";
            Destroy(fig.gameObject, 45f);
        }
    }
}
