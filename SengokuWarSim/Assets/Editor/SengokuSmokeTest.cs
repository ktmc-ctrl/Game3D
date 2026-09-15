using System;
using SengokuWarSim.Sim;
using UnityEditor;
using UnityEngine;

namespace SengokuWarSim.Editor
{
    /// <summary>
    /// バッチモード用スモークテスト:
    ///   Unity -batchmode -nographics -quit -projectPath . -executeMethod SengokuWarSim.Editor.SengokuSmokeTest.Run
    /// 1) 両軍 AI 同士でヘッドレス戦闘を回して決着が付くことを確認
    /// 2) シーンを組み立てて必須コンポーネントが揃うことを確認
    /// 失敗時は終了コード 1。
    /// </summary>
    public static class SengokuSmokeTest
    {
        public static void Run()
        {
            int code = 0;
            try
            {
                RunHeadlessBattle(seed: 7);
                RunHeadlessBattle(seed: 42);
                CheckSceneBuild();
                Debug.Log("[Sengoku] Smoke test PASSED");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError("[Sengoku] Smoke test FAILED");
                code = 1;
            }

            if (Application.isBatchMode) EditorApplication.Exit(code);
        }

        public static void RunHeadlessBattle(int seed)
        {
            var map = TerrainMap.Generate(120, seed);
            var sim = ScenarioBuilder.BuildSkirmish(map, seed);
            var a = new BattleAI(sim, Faction.Kamimura);
            var b = new BattleAI(sim, Faction.Shimomura);
            int startK = sim.CountSoldiers(Faction.Kamimura);
            int startS = sim.CountSoldiers(Faction.Shimomura);

            int ticks = 0;
            int maxTicks = (int)(BattleSim.TimeLimitSeconds / BattleSim.DefaultTickSeconds) + 10;
            while (sim.Outcome == BattleOutcome.Ongoing && ticks++ < maxTicks)
            {
                sim.Tick(BattleSim.DefaultTickSeconds);
                a.Tick(BattleSim.DefaultTickSeconds);
                b.Tick(BattleSim.DefaultTickSeconds);
            }

            if (sim.Outcome == BattleOutcome.Ongoing) throw new Exception($"seed {seed}: battle did not end within time limit");
            int endK = sim.CountSoldiers(Faction.Kamimura);
            int endS = sim.CountSoldiers(Faction.Shimomura);
            if (endK == startK && endS == startS) throw new Exception($"seed {seed}: no casualties were inflicted");
            Debug.Log($"[Sengoku] seed {seed}: {sim.Outcome} after {sim.Time:0}s  Kamimura {startK}->{endK}  Shimomura {startS}->{endS}");
        }

        static void CheckSceneBuild()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects,
                UnityEditor.SceneManagement.NewSceneMode.Single);
            var go = SengokuSceneBuilder.PopulateScene();
            if (go.GetComponent<BattleBootstrap>() == null) throw new Exception("BattleBootstrap missing");
            if (go.GetComponent<PlayerCommander>() == null) throw new Exception("PlayerCommander missing");
            if (go.GetComponent<BattleHUD>() == null) throw new Exception("BattleHUD missing");
            if (Camera.main == null || Camera.main.GetComponent<RTSCamera>() == null) throw new Exception("RTSCamera missing");
            Debug.Log($"[Sengoku] scene '{scene.name}' populated OK");
        }
    }
}
