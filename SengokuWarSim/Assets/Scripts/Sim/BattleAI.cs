using System.Collections.Generic;

namespace SengokuWarSim.Sim
{
    /// <summary>
    /// 単純な部隊 AI。近接部隊は最寄りの敵に突撃、射撃部隊は距離を保って撃ち、
    /// 村長組は前線の後ろに付いて士気を支える。
    /// </summary>
    public sealed class BattleAI
    {
        readonly BattleSim _sim;
        float _timer;

        public Faction Faction { get; }
        public float DecisionInterval { get; set; } = 1.0f;
        /// <summary>開戦から前進を始めるまでの秒数。</summary>
        public float AdvanceDelay { get; set; } = 3f;
        public float KiteDistance { get; set; } = 6f;

        public BattleAI(BattleSim sim, Faction faction)
        {
            _sim = sim;
            Faction = faction;
        }

        public void Tick(float dt)
        {
            _timer -= dt;
            if (_timer > 0f) return;
            _timer = DecisionInterval;
            if (_sim.Time < AdvanceDelay || _sim.Outcome != BattleOutcome.Ongoing) return;

            var melee = new List<Squad>();
            foreach (var s in _sim.Squads)
                if (s.Faction == Faction && s.IsEffective && !s.Stats.IsRanged && s.Kind != UnitKind.Headman)
                    melee.Add(s);

            foreach (var s in _sim.Squads)
            {
                if (s.Faction != Faction || !s.IsEffective) continue;
                var enemy = _sim.NearestEnemy(s, includeRouting: false) ?? _sim.NearestEnemy(s, includeRouting: true);
                if (enemy == null) continue;
                float dist = Vec2.Distance(s.Position, enemy.Position);

                if (s.Stats.IsRanged)
                {
                    if (dist < KiteDistance)
                    {
                        Vec2 away = (s.Position - enemy.Position).Normalized;
                        _sim.IssueMove(s.Id, s.Position + away * 10f);
                    }
                    else if (s.State != SquadState.Engaging || s.TargetSquadId != enemy.Id)
                    {
                        _sim.IssueAttack(s.Id, enemy.Id);
                    }
                }
                else if (s.Kind == UnitKind.Headman)
                {
                    if (dist < 12f)
                    {
                        if (s.State != SquadState.Engaging) _sim.IssueAttack(s.Id, enemy.Id);
                    }
                    else if (melee.Count > 0)
                    {
                        Vec2 centroid = Vec2.Zero;
                        foreach (var m in melee) centroid += m.Position;
                        centroid *= 1f / melee.Count;
                        Vec2 behind = centroid + BattleSim.HomeDirection(Faction) * 5f;
                        if (Vec2.Distance(s.Position, behind) > 3f) _sim.IssueMove(s.Id, behind);
                    }
                }
                else if (s.State != SquadState.Engaging)
                {
                    _sim.IssueAttack(s.Id, enemy.Id);
                }
            }
        }
    }
}
