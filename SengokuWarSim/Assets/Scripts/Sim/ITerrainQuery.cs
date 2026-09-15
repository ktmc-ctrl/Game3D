namespace SengokuWarSim.Sim
{
    /// <summary>シミュレーションが地形に問い合わせる最小インターフェース。</summary>
    public interface ITerrainQuery
    {
        /// <summary>マップは [-HalfSize, +HalfSize] の正方形。</summary>
        float HalfSize { get; }
        TerrainKind KindAt(Vec2 position);
        float HeightAt(Vec2 position);
    }

    /// <summary>テスト・ヘッドレス用の平坦な地形。</summary>
    public sealed class FlatTerrain : ITerrainQuery
    {
        public FlatTerrain(float halfSize, TerrainKind kind = TerrainKind.Field)
        {
            HalfSize = halfSize;
            Kind = kind;
        }

        public float HalfSize { get; }
        public TerrainKind Kind { get; }
        public TerrainKind KindAt(Vec2 position) => Kind;
        public float HeightAt(Vec2 position) => 0f;
    }

    public static class TerrainRules
    {
        public static float SpeedMultiplier(TerrainKind kind)
        {
            switch (kind)
            {
                case TerrainKind.Paddy: return 0.55f;
                case TerrainKind.Forest: return 0.7f;
                case TerrainKind.Hill: return 0.85f;
                case TerrainKind.Road: return 1.15f;
                default: return 1f;
            }
        }

        /// <summary>攻撃側が立っている地形による命中率補正。</summary>
        public static float AttackMultiplier(TerrainKind kind)
        {
            return kind == TerrainKind.Paddy ? 0.8f : 1f;
        }

        /// <summary>防御側が立っている地形による被弾率補正。</summary>
        public static float DefenseMultiplier(TerrainKind kind, bool attackerIsRanged)
        {
            switch (kind)
            {
                case TerrainKind.Hill: return 0.75f;
                case TerrainKind.Forest: return attackerIsRanged ? 0.6f : 1f;
                default: return 1f;
            }
        }

        public static float RangeMultiplier(TerrainKind kind, bool isRanged)
        {
            return isRanged && kind == TerrainKind.Hill ? 1.2f : 1f;
        }
    }
}
