namespace SengokuWarSim.Sim
{
    /// <summary>対戦する二つの村。</summary>
    public enum Faction
    {
        /// <summary>上ノ村。既定でプレイヤー側。南 (−Z) 側から攻め上がる。</summary>
        Kamimura = 0,
        /// <summary>下ノ村。既定で AI 側。北 (+Z) 側に陣取る。</summary>
        Shimomura = 1,
    }

    /// <summary>戦国期の村人で編成した部隊種。</summary>
    public enum UnitKind
    {
        /// <summary>竹槍衆。数が多く前線を支える。</summary>
        BambooSpear = 0,
        /// <summary>鍬衆。農具で殴る近接部隊。手数は多いが脆い。</summary>
        Hoe = 1,
        /// <summary>狩弓衆。猟師の弓。射程は長いが接近されると弱い。</summary>
        HuntingBow = 2,
        /// <summary>投石衆。石つぶて。中距離、士気が低い。</summary>
        StoneSling = 3,
        /// <summary>村長組。少数精鋭。近くの味方の士気を支える。</summary>
        Headman = 4,
    }

    public enum SquadState
    {
        Idle = 0,
        Moving = 1,
        Engaging = 2,
        /// <summary>敗走中。指示を受け付けず、敵から離れて自陣へ逃げる。</summary>
        Routing = 3,
        Destroyed = 4,
    }

    public enum TerrainKind
    {
        /// <summary>畑・草地。基準値。</summary>
        Field = 0,
        /// <summary>水田。移動が大幅に遅くなり、攻撃精度も落ちる。</summary>
        Paddy = 1,
        /// <summary>丘。守備側に有利、射撃部隊の射程が伸びる。</summary>
        Hill = 2,
        /// <summary>林。移動がやや遅く、遠距離攻撃から身を隠せる。</summary>
        Forest = 3,
        /// <summary>街道。移動が速い。</summary>
        Road = 4,
    }

    public enum BattleOutcome
    {
        Ongoing = 0,
        KamimuraWins = 1,
        ShimomuraWins = 2,
        Draw = 3,
    }
}
