using NUnit.Framework;
using SengokuWarSim.Sim;

namespace SengokuWarSim.Tests
{
    public class BattleSimTests
    {
        const float Dt = BattleSim.DefaultTickSeconds;

        static void Run(BattleSim sim, float seconds, params BattleAI[] ais)
        {
            int ticks = (int)(seconds / Dt);
            for (int i = 0; i < ticks && sim.Outcome == BattleOutcome.Ongoing; i++)
            {
                sim.Tick(Dt);
                foreach (var ai in ais) ai.Tick(Dt);
            }
        }

        [Test]
        public void Vec2_AngleRoundTrip()
        {
            var north = Vec2.FromAngle(0f);
            Assert.AreEqual(0f, north.X, 1e-5f);
            Assert.AreEqual(1f, north.Z, 1e-5f);
            var east = Vec2.FromAngle((float)System.Math.PI / 2f);
            Assert.AreEqual(1f, east.X, 1e-5f);
            Assert.AreEqual((float)System.Math.PI / 2f, east.ToAngle(), 1e-5f);
        }

        [Test]
        public void UnitStats_AllKindsDefined()
        {
            foreach (UnitKind kind in System.Enum.GetValues(typeof(UnitKind)))
            {
                var stats = UnitStats.Get(kind);
                Assert.AreEqual(kind, stats.Kind);
                Assert.Greater(stats.SquadSize, 0);
                Assert.Greater(stats.HitChance, 0f);
            }
        }

        [Test]
        public void Squad_MovesTowardTargetAndStops()
        {
            var sim = new BattleSim(new FlatTerrain(60f), 1);
            var s = sim.AddSquad(Faction.Kamimura, UnitKind.Hoe, new Vec2(0f, 0f), Vec2.North);
            Assert.IsTrue(sim.IssueMove(s.Id, new Vec2(0f, 10f)));
            Assert.AreEqual(SquadState.Moving, s.State);
            Run(sim, 10f);
            Assert.AreEqual(SquadState.Idle, s.State);
            Assert.AreEqual(10f, s.Position.Z, 0.6f);
        }

        [Test]
        public void Paddy_SlowsMovement()
        {
            var field = new BattleSim(new FlatTerrain(60f, TerrainKind.Field), 1);
            var paddy = new BattleSim(new FlatTerrain(60f, TerrainKind.Paddy), 1);
            var a = field.AddSquad(Faction.Kamimura, UnitKind.Hoe, Vec2.Zero, Vec2.North);
            var b = paddy.AddSquad(Faction.Kamimura, UnitKind.Hoe, Vec2.Zero, Vec2.North);
            field.IssueMove(a.Id, new Vec2(0f, 40f));
            paddy.IssueMove(b.Id, new Vec2(0f, 40f));
            Run(field, 3f);
            Run(paddy, 3f);
            Assert.Greater(a.Position.Z, b.Position.Z * 1.5f);
        }

        [Test]
        public void Melee_InflictsCasualtiesAndEndsBattle()
        {
            var sim = new BattleSim(new FlatTerrain(60f), 3);
            var a = sim.AddSquad(Faction.Kamimura, UnitKind.BambooSpear, new Vec2(0f, -3f), Vec2.North);
            var b = sim.AddSquad(Faction.Shimomura, UnitKind.Hoe, new Vec2(0f, 3f), Vec2.South);
            sim.IssueAttack(a.Id, b.Id);
            sim.IssueAttack(b.Id, a.Id);
            Run(sim, 120f);
            Assert.AreNotEqual(BattleOutcome.Ongoing, sim.Outcome);
            Assert.IsTrue(a.Soldiers < a.MaxSoldiers || b.Soldiers < b.MaxSoldiers);
        }

        [Test]
        public void IdleSquad_FightsBackWhenAttacked()
        {
            var sim = new BattleSim(new FlatTerrain(60f), 5);
            var a = sim.AddSquad(Faction.Kamimura, UnitKind.Hoe, new Vec2(0f, -1f), Vec2.North);
            var b = sim.AddSquad(Faction.Shimomura, UnitKind.BambooSpear, new Vec2(0f, 1f), Vec2.South);
            sim.IssueAttack(a.Id, b.Id);
            Run(sim, 2f);
            Assert.AreEqual(SquadState.Engaging, b.State);
            Assert.AreEqual(a.Id, b.TargetSquadId);
        }

        [Test]
        public void HeavyLosses_CauseRout()
        {
            var sim = new BattleSim(new FlatTerrain(60f), 9);
            var victim = sim.AddSquad(Faction.Shimomura, UnitKind.StoneSling, new Vec2(0f, 2f), Vec2.South);
            bool routed = false;
            sim.SquadRouted += s => { if (s == victim) routed = true; };
            // 三方向から槍で囲む
            for (int i = 0; i < 3; i++)
            {
                var atk = sim.AddSquad(Faction.Kamimura, UnitKind.BambooSpear, new Vec2(-2f + i * 2f, -1f), Vec2.North);
                sim.IssueAttack(atk.Id, victim.Id);
            }
            Run(sim, 60f);
            Assert.IsTrue(routed || !victim.IsAlive, "victim should rout or be destroyed");
        }

        [Test]
        public void Ranged_DoesNotCloseToMelee()
        {
            var sim = new BattleSim(new FlatTerrain(60f), 11);
            var bow = sim.AddSquad(Faction.Kamimura, UnitKind.HuntingBow, new Vec2(0f, -30f), Vec2.North);
            var target = sim.AddSquad(Faction.Shimomura, UnitKind.Hoe, new Vec2(0f, 0f), Vec2.South);
            sim.IssueHalt(target.Id);
            sim.IssueAttack(bow.Id, target.Id);
            Run(sim, 6f);
            float d = Vec2.Distance(bow.Position, target.Position);
            Assert.Greater(d, 10f);
            Assert.LessOrEqual(d, bow.Stats.Range + 0.5f);
        }

        sealed class SplitTerrain : ITerrainQuery
        {
            public float HalfSize => 60f;
            public TerrainKind KindAt(Vec2 p) => p.Z > 0f ? TerrainKind.Hill : TerrainKind.Field;
            public float HeightAt(Vec2 p) => 0f;
        }

        [Test]
        public void Hill_ReducesIncomingHitChance()
        {
            var sim = new BattleSim(new SplitTerrain(), 21);
            var atk = sim.AddSquad(Faction.Kamimura, UnitKind.BambooSpear, new Vec2(0f, -1f), Vec2.North);
            var onHill = sim.AddSquad(Faction.Shimomura, UnitKind.BambooSpear, new Vec2(0f, 1f), Vec2.South);
            var onField = sim.AddSquad(Faction.Shimomura, UnitKind.BambooSpear, new Vec2(0f, -3f), Vec2.North);
            float hill = sim.HitChance(atk, onHill, 2f);
            float field = sim.HitChance(atk, onField, 2f);
            Assert.Greater(field, 0f);
            Assert.AreEqual(field * 0.75f, hill, 1e-4f);
        }

        [Test]
        public void FlankAndRout_IncreaseHitChance()
        {
            var sim = new BattleSim(new FlatTerrain(60f), 21);
            var atk = sim.AddSquad(Faction.Kamimura, UnitKind.Hoe, new Vec2(0f, -1f), Vec2.North);
            var facing = sim.AddSquad(Faction.Shimomura, UnitKind.Hoe, new Vec2(0f, 1f), Vec2.South);
            var backTurned = sim.AddSquad(Faction.Shimomura, UnitKind.Hoe, new Vec2(0f, 4f), Vec2.North);
            float front = sim.HitChance(atk, facing, 2f);
            float rear = sim.HitChance(atk, backTurned, 2f);
            Assert.AreEqual(front * 1.5f, rear, 1e-4f);
        }

        [Test]
        public void SingleFaction_DoesNotEndBattle()
        {
            var sim = new BattleSim(new FlatTerrain(60f), 1);
            sim.AddSquad(Faction.Kamimura, UnitKind.Hoe, Vec2.Zero, Vec2.North);
            Run(sim, 1f);
            Assert.AreEqual(BattleOutcome.Ongoing, sim.Outcome);
        }

        [Test]
        public void Scenario_IsDeterministicForSameSeed()
        {
            string Play(int seed)
            {
                var map = TerrainMap.Generate(120, seed);
                var sim = ScenarioBuilder.BuildSkirmish(map, seed);
                Run(sim, 200f, new BattleAI(sim, Faction.Kamimura), new BattleAI(sim, Faction.Shimomura));
                var sb = new System.Text.StringBuilder();
                sb.Append(sim.Outcome).Append('|');
                foreach (var s in sim.Squads) sb.Append(s.Soldiers).Append(',').Append(s.Position).Append(';');
                return sb.ToString();
            }
            Assert.AreEqual(Play(123), Play(123));
            Assert.AreNotEqual(Play(123), Play(124));
        }

        [Test]
        public void Scenario_AIvsAI_EndsWithinTimeLimit()
        {
            var map = TerrainMap.Generate(120, 7);
            var sim = ScenarioBuilder.BuildSkirmish(map, 7);
            Assert.AreEqual(16, sim.Squads.Count);
            Run(sim, BattleSim.TimeLimitSeconds + 1f, new BattleAI(sim, Faction.Kamimura), new BattleAI(sim, Faction.Shimomura));
            Assert.AreNotEqual(BattleOutcome.Ongoing, sim.Outcome);
            Assert.Less(sim.Time, BattleSim.TimeLimitSeconds, "battle should be decided by combat, not the clock");
        }

        [Test]
        public void TerrainMap_HasEveryKindAndBoundedHeights()
        {
            var map = TerrainMap.Generate(120, 7);
            Assert.Greater(map.CountCells(TerrainKind.Paddy), 100);
            Assert.Greater(map.CountCells(TerrainKind.Hill), 20);
            Assert.Greater(map.CountCells(TerrainKind.Forest), 50);
            Assert.Greater(map.CountCells(TerrainKind.Road), 100);
            Assert.AreEqual(TerrainKind.Road, map.KindAt(new Vec2(0f, 0f)));
            for (int i = 0; i < 200; i++)
            {
                var p = new Vec2(-60f + i * 0.6f, 30f - i * 0.3f);
                float h = map.HeightAt(p);
                Assert.IsTrue(h > -1f && h < 8f, $"height out of range at {p}: {h}");
            }
            // 範囲外の問い合わせでも落ちない
            Assert.DoesNotThrow(() => map.KindAt(new Vec2(999f, -999f)));
            Assert.DoesNotThrow(() => map.HeightAt(new Vec2(999f, -999f)));
        }
    }
}
