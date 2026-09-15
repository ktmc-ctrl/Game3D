using System.Collections.Generic;
using SengokuWarSim.Sim;
using UnityEngine;

namespace SengokuWarSim
{
    /// <summary>
    /// プレイヤー操作。左クリック/ドラッグで選択、右クリックで移動・攻撃。
    /// Space で一時停止、1/2/3 で速度、H で停止、Ctrl+A で全選択。
    /// </summary>
    public sealed class PlayerCommander : MonoBehaviour
    {
        public const float DragThresholdPixels = 6f;
        public const float FormationSpacing = 7f;

        BattleBootstrap _boot;
        readonly List<SquadView> _selected = new List<SquadView>();
        Vector3 _dragStart;
        bool _dragging;

        public IReadOnlyList<SquadView> Selected => _selected;
        public bool IsDragging => _dragging && (Input.mousePosition - _dragStart).magnitude > DragThresholdPixels;
        public Rect DragRect => ScreenRect(_dragStart, Input.mousePosition);

        void Awake()
        {
            _boot = GetComponent<BattleBootstrap>();
            if (_boot != null) _boot.Rebuilt += () => _selected.Clear();
        }

        void Update()
        {
            if (_boot == null || _boot.Sim == null) return;
            HandleHotkeys();
            if (_boot.Sim.Outcome != BattleOutcome.Ongoing) return;
            HandleSelection();
            HandleOrders();
            PruneSelection();
        }

        void HandleHotkeys()
        {
            if (Input.GetKeyDown(KeyCode.Space)) _boot.Paused = !_boot.Paused;
            if (Input.GetKeyDown(KeyCode.Alpha1)) _boot.Speed = 1f;
            if (Input.GetKeyDown(KeyCode.Alpha2)) _boot.Speed = 2f;
            if (Input.GetKeyDown(KeyCode.Alpha3)) _boot.Speed = 4f;
            if (Input.GetKeyDown(KeyCode.R) && _boot.Sim.Outcome != BattleOutcome.Ongoing) _boot.Restart(false);

            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftCommand);
            if (ctrl && Input.GetKeyDown(KeyCode.A))
            {
                ClearSelection();
                foreach (var v in _boot.Views)
                    if (v.Squad.Faction == _boot.PlayerFaction && v.Squad.IsEffective) Select(v, true);
            }
            if (Input.GetKeyDown(KeyCode.H))
                foreach (var v in _selected) _boot.Sim.IssueHalt(v.Squad.Id);
        }

        void HandleSelection()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _dragStart = Input.mousePosition;
                _dragging = true;
            }

            if (Input.GetMouseButtonUp(0) && _dragging)
            {
                _dragging = false;
                bool additive = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                if ((Input.mousePosition - _dragStart).magnitude > DragThresholdPixels)
                {
                    if (!additive) ClearSelection();
                    var rect = ScreenRect(_dragStart, Input.mousePosition);
                    var cam = Camera.main;
                    foreach (var v in _boot.Views)
                    {
                        if (v.Squad.Faction != _boot.PlayerFaction || !v.Squad.IsAlive) continue;
                        Vector3 sp = cam.WorldToScreenPoint(v.WorldCenter);
                        if (sp.z > 0f && rect.Contains(new Vector2(sp.x, sp.y))) Select(v, true);
                    }
                }
                else
                {
                    var hit = RaycastSquad(Input.mousePosition);
                    if (!additive) ClearSelection();
                    if (hit != null && hit.Squad.Faction == _boot.PlayerFaction && hit.Squad.IsAlive)
                        Select(hit, !additive || !hit.Selected);
                }
            }
        }

        void HandleOrders()
        {
            if (!Input.GetMouseButtonDown(1) || _selected.Count == 0) return;

            var target = RaycastSquad(Input.mousePosition);
            if (target != null && target.Squad.Faction != _boot.PlayerFaction && target.Squad.IsAlive)
            {
                foreach (var v in _selected) _boot.Sim.IssueAttack(v.Squad.Id, target.Squad.Id);
                return;
            }

            if (!RaycastGround(Input.mousePosition, out Vector3 ground)) return;
            IssueFormationMove(new Vec2(ground.x, ground.z));
        }

        /// <summary>複数部隊は目的地を中心に、進行方向と直交する横一列に並べる。</summary>
        void IssueFormationMove(Vec2 destination)
        {
            var movers = new List<SquadView>();
            foreach (var v in _selected) if (v.Squad.IsEffective) movers.Add(v);
            if (movers.Count == 0) return;

            Vec2 centroid = Vec2.Zero;
            foreach (var v in movers) centroid += v.Squad.Position;
            centroid *= 1f / movers.Count;

            Vec2 forward = (destination - centroid).Normalized;
            if (forward.SqrLength < 0.01f) forward = movers[0].Squad.Facing;
            Vec2 right = -forward.Perpendicular;

            // 現在の横位置順に並べて交差を減らす
            movers.Sort((a, b) => Vec2.Dot(a.Squad.Position - centroid, right).CompareTo(Vec2.Dot(b.Squad.Position - centroid, right)));
            float start = -(movers.Count - 1) * 0.5f * FormationSpacing;
            for (int i = 0; i < movers.Count; i++)
                _boot.Sim.IssueMove(movers[i].Squad.Id, destination + right * (start + i * FormationSpacing));
        }

        // ------------------------------------------------------------ helpers

        SquadView RaycastSquad(Vector3 screenPos)
        {
            var cam = Camera.main;
            if (cam == null) return null;
            var ray = cam.ScreenPointToRay(screenPos);
            SquadView best = null;
            float bestDist = float.MaxValue;
            foreach (var hit in Physics.RaycastAll(ray, 1000f))
            {
                var view = hit.collider.GetComponentInParent<SquadView>();
                if (view == null || hit.distance >= bestDist) continue;
                best = view;
                bestDist = hit.distance;
            }
            return best;
        }

        bool RaycastGround(Vector3 screenPos, out Vector3 point)
        {
            point = Vector3.zero;
            var cam = Camera.main;
            if (cam == null) return false;
            var ray = cam.ScreenPointToRay(screenPos);
            foreach (var hit in Physics.RaycastAll(ray, 1000f))
            {
                if (hit.collider.GetComponent<ProceduralTerrainView>() == null) continue;
                point = hit.point;
                return true;
            }
            // 地形に当たらなければ y=0 平面で代用
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (!plane.Raycast(ray, out float enter)) return false;
            point = ray.GetPoint(enter);
            return true;
        }

        static Rect ScreenRect(Vector3 a, Vector3 b)
        {
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        void Select(SquadView v, bool on)
        {
            if (on)
            {
                if (!_selected.Contains(v)) _selected.Add(v);
                v.SetSelected(true);
            }
            else
            {
                _selected.Remove(v);
                v.SetSelected(false);
            }
        }

        void ClearSelection()
        {
            foreach (var v in _selected) if (v != null) v.SetSelected(false);
            _selected.Clear();
        }

        void PruneSelection()
        {
            for (int i = _selected.Count - 1; i >= 0; i--)
            {
                var v = _selected[i];
                if (v == null || !v.Squad.IsAlive)
                {
                    if (v != null) v.SetSelected(false);
                    _selected.RemoveAt(i);
                }
            }
        }
    }
}
