using SengokuWarSim.Sim;
using UnityEngine;

namespace SengokuWarSim
{
    /// <summary>両陣営の村 (茅葺きの家) と林の木を配置する。</summary>
    public static class VillageBuilder
    {
        public static void Build(TerrainMap map, ProceduralTerrainView terrain, Transform parent, int seed)
        {
            var rng = new System.Random(seed + 101);
            var root = new GameObject("Villages").transform;
            root.SetParent(parent, false);

            float half = map.HalfSize;
            BuildVillage(root, terrain, rng, -half * 0.86f, "Kamimura");
            BuildVillage(root, terrain, rng, half * 0.86f, "Shimomura");
            BuildForest(map, terrain, parent, rng);
        }

        static void BuildVillage(Transform root, ProceduralTerrainView terrain, System.Random rng, float z, string name)
        {
            var village = new GameObject(name).transform;
            village.SetParent(root, false);
            var wall = PrimitiveFactory.GetMaterial("Wall", Palette.Wall);
            var thatch = PrimitiveFactory.GetMaterial("Thatch", Palette.Thatch);
            var wood = PrimitiveFactory.GetMaterial("Wood", Palette.Wood);

            float[] xs = { -22f, -12f, 8f, 18f, -4f, 28f };
            for (int i = 0; i < xs.Length; i++)
            {
                float x = xs[i] + ((float)rng.NextDouble() - 0.5f) * 3f;
                float zz = z + ((float)rng.NextDouble() - 0.5f) * 8f;
                var pos = terrain.ToWorld(new Vec2(x, zz));
                float w = 5f + (float)rng.NextDouble() * 3f;
                float d = 4f + (float)rng.NextDouble() * 2f;
                var house = new GameObject($"House_{i}").transform;
                house.SetParent(village, false);
                house.position = pos;
                house.rotation = Quaternion.Euler(0f, ((float)rng.NextDouble() - 0.5f) * 30f, 0f);
                PrimitiveFactory.CreatePart("Walls", PrimitiveType.Cube, wall, house, new Vector3(0f, 1.2f, 0f), new Vector3(w, 2.4f, d));
                PrimitiveFactory.CreateMeshPart("Roof", PrimitiveFactory.GetPyramid(), thatch, house,
                    new Vector3(0f, 2.4f, 0f), new Vector3(w * 0.85f, 2.6f, d * 0.85f), Quaternion.Euler(0f, 45f, 0f));
                // 軒下の柱
                PrimitiveFactory.CreatePart("Post", PrimitiveType.Cylinder, wood, house, new Vector3(w * 0.5f + 0.3f, 1.1f, 0f), new Vector3(0.2f, 1.1f, 0.2f));
            }

            // 村の入り口の木戸
            PrimitiveFactory.CreatePart("Gate", PrimitiveType.Cube, wood, village, terrain.ToWorld(new Vec2(0f, z)) + Vector3.up * 1.5f, new Vector3(4f, 0.3f, 0.3f));
        }

        static void BuildForest(TerrainMap map, ProceduralTerrainView terrain, Transform parent, System.Random rng)
        {
            var forest = new GameObject("Forest").transform;
            forest.SetParent(parent, false);
            var trunk = PrimitiveFactory.GetMaterial("Wood", Palette.Wood);
            var canopy = PrimitiveFactory.GetMaterial("Canopy", Palette.Canopy);
            int placed = 0;
            for (int cz = 0; cz < map.Size && placed < 220; cz += 2)
            for (int cx = 0; cx < map.Size && placed < 220; cx += 2)
            {
                if (map.KindAtCell(cx, cz) != TerrainKind.Forest) continue;
                if (rng.NextDouble() > 0.6) continue;
                var p = new Vec2(cx - map.HalfSize + (float)rng.NextDouble(), cz - map.HalfSize + (float)rng.NextDouble());
                var pos = terrain.ToWorld(p);
                float h = 3f + (float)rng.NextDouble() * 2.5f;
                var tree = new GameObject("Tree").transform;
                tree.SetParent(forest, false);
                tree.position = pos;
                PrimitiveFactory.CreatePart("Trunk", PrimitiveType.Cylinder, trunk, tree, new Vector3(0f, h * 0.4f, 0f), new Vector3(0.4f, h * 0.4f, 0.4f));
                PrimitiveFactory.CreatePart("Canopy", PrimitiveType.Sphere, canopy, tree, new Vector3(0f, h * 0.9f, 0f), new Vector3(2.6f, 2.2f, 2.6f) * (h / 4f));
                placed++;
            }
        }
    }
}
