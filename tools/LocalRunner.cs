// Unity 外で EditMode テストを NUnitLite で走らせるための入口。Unity プロジェクトには含めない。
namespace SengokuWarSim.Tests
{
    public static class LocalRunner
    {
        public static int Main(string[] args) => new NUnitLite.AutoRun(typeof(BattleSimTests).Assembly).Execute(args);
    }
}
