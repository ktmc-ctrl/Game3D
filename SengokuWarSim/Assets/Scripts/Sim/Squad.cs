namespace SengokuWarSim.Sim
{
    /// <summary>一つの部隊の可変状態。BattleSim が所有し、ビューは読むだけ。</summary>
    public sealed class Squad
    {
        public int Id { get; internal set; }
        public Faction Faction { get; internal set; }
        public UnitKind Kind { get; internal set; }
        public UnitStats Stats { get; internal set; }

        public Vec2 Position { get; internal set; }
        public Vec2 Facing { get; internal set; } = Vec2.North;

        public int Soldiers { get; internal set; }
        public int MaxSoldiers { get; internal set; }
        /// <summary>0..100。20 を下回ると敗走、45 まで回復すると立て直す。</summary>
        public float Morale { get; internal set; }
        public SquadState State { get; internal set; } = SquadState.Idle;

        public Vec2 MoveTarget { get; internal set; }
        public int TargetSquadId { get; internal set; } = -1;
        public float AttackCooldown { get; internal set; }
        public int Kills { get; internal set; }

        public bool IsAlive => State != SquadState.Destroyed;
        public bool IsRouting => State == SquadState.Routing;
        /// <summary>戦闘に寄与できるか (生存かつ敗走中でない)。</summary>
        public bool IsEffective => IsAlive && !IsRouting;
        public float StrengthRatio => MaxSoldiers > 0 ? Soldiers / (float)MaxSoldiers : 0f;
        public string NameJa => Stats.NameJa;

        public override string ToString() =>
            $"#{Id} {Faction} {Stats.NameEn} {Soldiers}/{MaxSoldiers} morale={Morale:0} {State} @{Position}";
    }
}
