namespace SengokuWarSim.Sim
{
    /// <summary>両軍の初期配置。</summary>
    public static class ScenarioBuilder
    {
        /// <summary>前列 (左から右)。</summary>
        public static readonly UnitKind[] FrontLine =
        {
            UnitKind.BambooSpear, UnitKind.Hoe, UnitKind.Headman, UnitKind.Hoe, UnitKind.BambooSpear,
        };

        /// <summary>後列 (左から右)。</summary>
        public static readonly UnitKind[] BackLine =
        {
            UnitKind.HuntingBow, UnitKind.StoneSling, UnitKind.HuntingBow,
        };

        public const float ColumnSpacing = 7f;
        public const float LineDepth = 7f;

        public static BattleSim BuildSkirmish(ITerrainQuery terrain, int seed)
        {
            var sim = new BattleSim(terrain, seed);
            float z = terrain.HalfSize * 0.55f;
            PlaceArmy(sim, Faction.Kamimura, -z, Vec2.North);
            PlaceArmy(sim, Faction.Shimomura, z, Vec2.South);
            return sim;
        }

        public static void PlaceArmy(BattleSim sim, Faction faction, float lineZ, Vec2 facing)
        {
            PlaceLine(sim, faction, FrontLine, lineZ, facing);
            // 後列は前列の後ろ (自陣側)
            PlaceLine(sim, faction, BackLine, lineZ - facing.Z * LineDepth, facing);
        }

        static void PlaceLine(BattleSim sim, Faction faction, UnitKind[] kinds, float z, Vec2 facing)
        {
            float start = -(kinds.Length - 1) * 0.5f * ColumnSpacing;
            for (int i = 0; i < kinds.Length; i++)
                sim.AddSquad(faction, kinds[i], new Vec2(start + i * ColumnSpacing, z), facing);
        }
    }
}
