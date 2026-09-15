using System;
using System.Collections.Generic;

namespace SengokuWarSim.Sim
{
    /// <summary>
    /// 戦闘シミュレーション本体。固定刻み・シード付き乱数で決定論的に進む。
    /// UnityEngine に依存しないので EditMode テストやバッチ検証でそのまま回せる。
    /// </summary>
    public sealed class BattleSim
    {
        public const float DefaultTickSeconds = 0.05f;
        public const float MeleeRange = 2.5f;
        public const float RoutThreshold = 20f;
        public const float RallyThreshold = 45f;
        public const float MinSeparation = 2.2f;
        public const float NeighbourRadius = 15f;
        public const float TimeLimitSeconds = 900f;

        readonly List<Squad> _squads = new List<Squad>();
        readonly Random _rng;
        int _nextId = 1;

        public ITerrainQuery Terrain { get; }
        public IReadOnlyList<Squad> Squads => _squads;
        public float Time { get; private set; }
        public BattleOutcome Outcome { get; private set; } = BattleOutcome.Ongoing;

        /// <summary>(被害部隊, 死者数)。</summary>
        public event Action<Squad, int> CasualtiesInflicted;
        /// <summary>(攻撃側, 目標)。射撃演出のトリガー。</summary>
        public event Action<Squad, Squad> VolleyFired;
        public event Action<Squad> SquadRouted;
        public event Action<Squad> SquadRallied;
        public event Action<Squad> SquadDestroyed;
        public event Action<BattleOutcome> BattleEnded;

        public BattleSim(ITerrainQuery terrain, int seed)
        {
            Terrain = terrain ?? throw new ArgumentNullException(nameof(terrain));
            _rng = new Random(seed);
        }

        // ------------------------------------------------------------ setup

        public Squad AddSquad(Faction faction, UnitKind kind, Vec2 position, Vec2 facing)
        {
            var stats = UnitStats.Get(kind);
            var squad = new Squad
            {
                Id = _nextId++,
                Faction = faction,
                Kind = kind,
                Stats = stats,
                Position = position.Clamped(Terrain.HalfSize - 1f),
                Facing = facing.SqrLength > 0f ? facing.Normalized : Vec2.North,
                Soldiers = stats.SquadSize,
                MaxSoldiers = stats.SquadSize,
                Morale = stats.BaseMorale,
                MoveTarget = position,
            };
            _squads.Add(squad);
            return squad;
        }

        public Squad GetSquad(int id)
        {
            for (int i = 0; i < _squads.Count; i++)
                if (_squads[i].Id == id) return _squads[i];
            return null;
        }

        // ------------------------------------------------------------ commands

        public bool IssueMove(int squadId, Vec2 target)
        {
            var s = GetSquad(squadId);
            if (s == null || !s.IsEffective) return false;
            s.State = SquadState.Moving;
            s.MoveTarget = target.Clamped(Terrain.HalfSize - 1f);
            s.TargetSquadId = -1;
            return true;
        }

        public bool IssueAttack(int squadId, int targetId)
        {
            var s = GetSquad(squadId);
            var t = GetSquad(targetId);
            if (s == null || t == null || !s.IsEffective || !t.IsAlive || s.Faction == t.Faction) return false;
            s.State = SquadState.Engaging;
            s.TargetSquadId = targetId;
            return true;
        }

        public bool IssueHalt(int squadId)
        {
            var s = GetSquad(squadId);
            if (s == null || !s.IsEffective) return false;
            s.State = SquadState.Idle;
            s.TargetSquadId = -1;
            return true;
        }

        // ------------------------------------------------------------ queries

        public int CountSoldiers(Faction faction)
        {
            int n = 0;
            foreach (var s in _squads)
                if (s.Faction == faction && s.IsAlive) n += s.Soldiers;
            return n;
        }

        public int CountMaxSoldiers(Faction faction)
        {
            int n = 0;
            foreach (var s in _squads)
                if (s.Faction == faction) n += s.MaxSoldiers;
            return n;
        }

        public bool IsBroken(Faction faction)
        {
            foreach (var s in _squads)
                if (s.Faction == faction && s.IsEffective) return false;
            return true;
        }

        public Squad NearestEnemy(Squad from, bool includeRouting, float maxDistance = float.MaxValue)
        {
            Squad best = null;
            float bestSqr = maxDistance * maxDistance;
            foreach (var s in _squads)
            {
                if (s.Faction == from.Faction || !s.IsAlive) continue;
                if (!includeRouting && s.IsRouting) continue;
                float d = Vec2.SqrDistance(from.Position, s.Position);
                if (d < bestSqr)
                {
                    bestSqr = d;
                    best = s;
                }
            }
            return best;
        }

        public float EffectiveRange(Squad s)
        {
            return s.Stats.Range * TerrainRules.RangeMultiplier(Terrain.KindAt(s.Position), s.Stats.IsRanged);
        }

        public static Vec2 HomeDirection(Faction faction) => faction == Faction.Kamimura ? Vec2.South : Vec2.North;

        // ------------------------------------------------------------ tick

        public void Tick(float dt)
        {
            if (Outcome != BattleOutcome.Ongoing) return;
            Time += dt;

            for (int i = 0; i < _squads.Count; i++)
            {
                var s = _squads[i];
                if (!s.IsAlive) continue;
                UpdateSquad(s, dt);
            }

            ResolveSeparation();
            CheckOutcome();
        }

        void UpdateSquad(Squad s, float dt)
        {
            switch (s.State)
            {
                case SquadState.Idle:
                    AutoEngage(s);
                    break;
                case SquadState.Moving:
                    if (MoveToward(s, s.MoveTarget, dt, 1f)) s.State = SquadState.Idle;
                    break;
                case SquadState.Engaging:
                    UpdateEngaging(s, dt);
                    break;
                case SquadState.Routing:
                    UpdateRouting(s, dt);
                    return; // 士気処理は UpdateRouting 内で完結
            }

            UpdateMorale(s, dt);
        }

        void AutoEngage(Squad s)
        {
            float range = s.Stats.IsRanged ? EffectiveRange(s) : MeleeRange + 0.5f;
            var enemy = NearestEnemy(s, includeRouting: true, maxDistance: range);
            if (enemy != null)
            {
                s.State = SquadState.Engaging;
                s.TargetSquadId = enemy.Id;
            }
        }

        void UpdateEngaging(Squad s, float dt)
        {
            var target = GetSquad(s.TargetSquadId);
            if (target == null || !target.IsAlive)
            {
                // 目標を失ったら近くの敵を探し、いなければ待機
                var next = NearestEnemy(s, includeRouting: true, maxDistance: EffectiveRange(s) * 1.5f + MeleeRange);
                if (next == null)
                {
                    s.State = SquadState.Idle;
                    s.TargetSquadId = -1;
                    return;
                }
                target = next;
                s.TargetSquadId = next.Id;
            }

            float dist = Vec2.Distance(s.Position, target.Position);
            float range = EffectiveRange(s);
            if (dist > range)
            {
                // 射撃部隊は射程の少し内側で足を止める
                float stopAt = s.Stats.IsRanged ? range * 0.9f : range;
                MoveToward(s, target.Position, dt, 1f, stopAt);
                s.AttackCooldown = Math.Max(s.AttackCooldown - dt, s.Stats.AttackInterval * 0.3f);
                return;
            }

            s.Facing = (target.Position - s.Position).Normalized;
            s.AttackCooldown -= dt;
            if (s.AttackCooldown <= 0f)
            {
                Fire(s, target, dist);
                s.AttackCooldown = s.Stats.AttackInterval;
            }
        }

        void UpdateRouting(Squad s, float dt)
        {
            var enemy = NearestEnemy(s, includeRouting: false);
            Vec2 dir = HomeDirection(s.Faction);
            float enemyDist = float.MaxValue;
            if (enemy != null)
            {
                enemyDist = Vec2.Distance(s.Position, enemy.Position);
                Vec2 away = (s.Position - enemy.Position).Normalized;
                dir = (away + dir * 1.5f).Normalized;
            }

            Vec2 goal = s.Position + dir * 5f;
            MoveToward(s, goal, dt, 1.15f);

            // 自陣の端まで逃げ切ったら戦場から離脱
            float edge = Terrain.HalfSize - 1.5f;
            if ((s.Faction == Faction.Kamimura && s.Position.Z <= -edge) ||
                (s.Faction == Faction.Shimomura && s.Position.Z >= edge))
            {
                Destroy(s);
                return;
            }

            float regen = enemyDist > 25f ? 4f : 1f;
            s.Morale = Math.Min(100f, s.Morale + regen * dt + HeadmanBonus(s) * dt);
            if (s.Morale >= RallyThreshold)
            {
                s.State = SquadState.Idle;
                s.TargetSquadId = -1;
                SquadRallied?.Invoke(s);
            }
        }

        void UpdateMorale(Squad s, float dt)
        {
            float regen = s.State == SquadState.Engaging ? 0f : 1.5f;
            regen += HeadmanBonus(s);
            s.Morale = Math.Max(0f, Math.Min(100f, s.Morale + regen * dt));
            if (s.Morale < RoutThreshold) StartRout(s);
        }

        float HeadmanBonus(Squad s)
        {
            float bonus = 0f;
            foreach (var h in _squads)
            {
                if (h == s || h.Faction != s.Faction || !h.IsEffective || h.Stats.RallyRadius <= 0f) continue;
                if (Vec2.SqrDistance(h.Position, s.Position) <= h.Stats.RallyRadius * h.Stats.RallyRadius)
                    bonus += 1.5f;
            }
            return bonus;
        }

        /// <summary>目標へ向かって一刻み進む。到達したら true。</summary>
        bool MoveToward(Squad s, Vec2 target, float dt, float speedScale, float stopDistance = 0.4f)
        {
            Vec2 delta = target - s.Position;
            float dist = delta.Length;
            if (dist <= stopDistance) return true;

            Vec2 dir = delta * (1f / dist);
            float speed = s.Stats.MoveSpeed * speedScale * TerrainRules.SpeedMultiplier(Terrain.KindAt(s.Position));
            float step = speed * dt;
            if (step >= dist - stopDistance)
            {
                s.Position = (s.Position + dir * (dist - stopDistance)).Clamped(Terrain.HalfSize - 1f);
                s.Facing = dir;
                return true;
            }

            s.Position = (s.Position + dir * step).Clamped(Terrain.HalfSize - 1f);
            s.Facing = dir;
            return false;
        }

        /// <summary>兵一人あたりの命中確率。地形・装甲・士気・側面攻撃・敗走を加味する。</summary>
        public float HitChance(Squad attacker, Squad target, float dist)
        {
            bool inMelee = dist <= MeleeRange;
            float chance = attacker.Stats.IsRanged && inMelee ? attacker.Stats.MeleeHitChance : attacker.Stats.HitChance;
            chance *= TerrainRules.AttackMultiplier(Terrain.KindAt(attacker.Position));
            chance *= TerrainRules.DefenseMultiplier(Terrain.KindAt(target.Position), attacker.Stats.IsRanged && !inMelee);
            chance *= 1f - target.Stats.Armor;
            chance *= 0.6f + 0.4f * (attacker.Morale / 100f);

            // 側面・背後からの攻撃は効きやすい
            Vec2 toAttacker = (attacker.Position - target.Position).Normalized;
            if (Vec2.Dot(target.Facing, toAttacker) < -0.3f) chance *= 1.5f;
            // 敗走中の敵は無防備
            if (target.IsRouting) chance *= 1.5f;
            return Math.Min(1f, chance);
        }

        void Fire(Squad attacker, Squad target, float dist)
        {
            bool inMelee = dist <= MeleeRange;
            float chance = HitChance(attacker, target, dist);

            int hits = 0;
            for (int i = 0; i < attacker.Soldiers; i++)
                if (_rng.NextDouble() < chance) hits++;
            hits = Math.Min(hits, target.Soldiers);

            VolleyFired?.Invoke(attacker, target);

            // 反撃: 待機中/移動中の部隊が殴られたら攻撃者に向き直る
            if (target.IsEffective && (target.State == SquadState.Idle ||
                                       (target.State == SquadState.Moving && inMelee)))
            {
                target.State = SquadState.Engaging;
                target.TargetSquadId = attacker.Id;
            }

            if (hits > 0) ApplyCasualties(target, hits, attacker);
        }

        void ApplyCasualties(Squad victim, int count, Squad attacker)
        {
            victim.Soldiers -= count;
            attacker.Kills += count;
            victim.Morale -= count * (100f / victim.MaxSoldiers) * 0.7f;
            CasualtiesInflicted?.Invoke(victim, count);

            if (victim.Soldiers <= 0)
            {
                Destroy(victim);
                return;
            }
            if (victim.Morale < RoutThreshold && !victim.IsRouting) StartRout(victim);
        }

        void StartRout(Squad s)
        {
            if (s.IsRouting || !s.IsAlive) return;
            s.State = SquadState.Routing;
            s.TargetSquadId = -1;
            SquadRouted?.Invoke(s);
            ShakeNeighbours(s, -8f);
        }

        void Destroy(Squad s)
        {
            if (!s.IsAlive) return;
            s.State = SquadState.Destroyed;
            s.Soldiers = 0;
            s.TargetSquadId = -1;
            SquadDestroyed?.Invoke(s);
            ShakeNeighbours(s, -10f);
            foreach (var e in _squads)
            {
                if (e.Faction == s.Faction || !e.IsEffective) continue;
                if (Vec2.SqrDistance(e.Position, s.Position) <= NeighbourRadius * NeighbourRadius)
                    e.Morale = Math.Min(100f, e.Morale + 5f);
            }
        }

        void ShakeNeighbours(Squad source, float delta)
        {
            foreach (var a in _squads)
            {
                if (a == source || a.Faction != source.Faction || !a.IsEffective) continue;
                if (Vec2.SqrDistance(a.Position, source.Position) > NeighbourRadius * NeighbourRadius) continue;
                a.Morale = Math.Max(0f, a.Morale + delta);
                if (a.Morale < RoutThreshold) StartRout(a);
            }
        }

        void ResolveSeparation()
        {
            float minSqr = MinSeparation * MinSeparation;
            for (int i = 0; i < _squads.Count; i++)
            {
                var a = _squads[i];
                if (!a.IsAlive) continue;
                for (int j = i + 1; j < _squads.Count; j++)
                {
                    var b = _squads[j];
                    if (!b.IsAlive) continue;
                    Vec2 delta = b.Position - a.Position;
                    float sqr = delta.SqrLength;
                    if (sqr >= minSqr) continue;
                    Vec2 push;
                    if (sqr < 1e-6f)
                    {
                        push = a.Facing.Perpendicular * (MinSeparation * 0.5f);
                    }
                    else
                    {
                        float d = (float)Math.Sqrt(sqr);
                        push = delta * (1f / d) * ((MinSeparation - d) * 0.5f);
                    }
                    a.Position = (a.Position - push).Clamped(Terrain.HalfSize - 1f);
                    b.Position = (b.Position + push).Clamped(Terrain.HalfSize - 1f);
                }
            }
        }

        void CheckOutcome()
        {
            // 両陣営が揃うまでは勝敗を付けない (片側だけのサンドボックス用)
            if (CountMaxSoldiers(Faction.Kamimura) == 0 || CountMaxSoldiers(Faction.Shimomura) == 0) return;

            bool kBroken = IsBroken(Faction.Kamimura);
            bool sBroken = IsBroken(Faction.Shimomura);
            BattleOutcome result = BattleOutcome.Ongoing;

            if (kBroken && sBroken) result = BattleOutcome.Draw;
            else if (sBroken) result = BattleOutcome.KamimuraWins;
            else if (kBroken) result = BattleOutcome.ShimomuraWins;
            else if (Time >= TimeLimitSeconds)
            {
                int k = CountSoldiers(Faction.Kamimura);
                int s = CountSoldiers(Faction.Shimomura);
                result = k > s ? BattleOutcome.KamimuraWins : s > k ? BattleOutcome.ShimomuraWins : BattleOutcome.Draw;
            }

            if (result == BattleOutcome.Ongoing) return;
            Outcome = result;
            BattleEnded?.Invoke(result);
        }
    }
}
