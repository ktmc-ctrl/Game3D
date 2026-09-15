using System.Collections.Generic;

namespace SengokuWarSim.Sim
{
    /// <summary>部隊種ごとの固定パラメータ。バランス調整はここで行う。</summary>
    public sealed class UnitStats
    {
        public UnitKind Kind { get; }
        public string NameJa { get; }
        public string NameEn { get; }
        /// <summary>初期人数。</summary>
        public int SquadSize { get; }
        /// <summary>移動速度 (m/s)。</summary>
        public float MoveSpeed { get; }
        /// <summary>攻撃射程 (m)。近接は 2m 前後。</summary>
        public float Range { get; }
        /// <summary>一斉攻撃の間隔 (秒)。</summary>
        public float AttackInterval { get; }
        /// <summary>兵一人あたり、一斉攻撃ごとに敵一人を倒す確率。</summary>
        public float HitChance { get; }
        /// <summary>射撃部隊が白兵戦に巻き込まれたときの命中率。</summary>
        public float MeleeHitChance { get; }
        /// <summary>被弾軽減率 (0..1)。</summary>
        public float Armor { get; }
        /// <summary>初期士気 (0..100)。</summary>
        public float BaseMorale { get; }
        public bool IsRanged { get; }
        /// <summary>村長組のみ: この半径内の味方の士気回復を助ける。</summary>
        public float RallyRadius { get; }

        UnitStats(UnitKind kind, string nameJa, string nameEn, int size, float speed, float range,
            float interval, float hit, float meleeHit, float armor, float morale, bool ranged, float rally)
        {
            Kind = kind;
            NameJa = nameJa;
            NameEn = nameEn;
            SquadSize = size;
            MoveSpeed = speed;
            Range = range;
            AttackInterval = interval;
            HitChance = hit;
            MeleeHitChance = meleeHit;
            Armor = armor;
            BaseMorale = morale;
            IsRanged = ranged;
            RallyRadius = rally;
        }

        static readonly Dictionary<UnitKind, UnitStats> Table = new Dictionary<UnitKind, UnitStats>
        {
            { UnitKind.BambooSpear, new UnitStats(UnitKind.BambooSpear, "竹槍衆", "Bamboo Spears", 24, 2.2f, 2.2f, 1.2f, 0.10f, 0.10f, 0.05f, 60f, false, 0f) },
            { UnitKind.Hoe,         new UnitStats(UnitKind.Hoe,         "鍬衆",   "Hoe Gang",      20, 2.4f, 1.8f, 1.0f, 0.13f, 0.13f, 0.00f, 55f, false, 0f) },
            { UnitKind.HuntingBow,  new UnitStats(UnitKind.HuntingBow,  "狩弓衆", "Hunting Bows",  16, 2.3f, 18f,  2.0f, 0.07f, 0.03f, 0.00f, 45f, true,  0f) },
            { UnitKind.StoneSling,  new UnitStats(UnitKind.StoneSling,  "投石衆", "Stone Slingers",18, 2.6f, 12f,  1.5f, 0.05f, 0.03f, 0.00f, 45f, true,  0f) },
            { UnitKind.Headman,     new UnitStats(UnitKind.Headman,     "村長組", "Headman's Men",  8, 2.0f, 2.0f, 1.2f, 0.12f, 0.12f, 0.15f, 80f, false, 12f) },
        };

        public static UnitStats Get(UnitKind kind) => Table[kind];
        public static IEnumerable<UnitStats> All => Table.Values;
    }
}
