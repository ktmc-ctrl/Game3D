using System.Collections.Generic;
using SengokuWarSim.Sim;
using UnityEngine;

namespace SengokuWarSim
{
    /// <summary>
    /// 村人一体分のメッシュをプリミティブの合成で作る。
    /// 一体 = 1 GameObject / 1 MeshRenderer (マテリアルごとにサブメッシュ) に抑えて描画負荷を下げる。
    /// Blender で作ったモデルに差し替える場合は Build() の戻りをプレハブに置き換えればよい。
    /// </summary>
    public static class FigureFactory
    {
        const int MatSkin = 0;
        const int MatKimono = 1;
        const int MatSash = 2;
        const int MatStraw = 3;
        const int MatWood = 4;
        const int MatIron = 5;
        const int MatBamboo = 6;
        const int MatStone = 7;
        const int MatCount = 8;

        struct Part
        {
            public Mesh Mesh;
            public Matrix4x4 Matrix;
            public int Material;
        }

        static readonly Dictionary<(UnitKind, Faction), Mesh> MeshCache = new Dictionary<(UnitKind, Faction), Mesh>();

        public static void ClearCache() => MeshCache.Clear();

        public static Material[] MaterialsFor(Faction faction)
        {
            var mats = new Material[MatCount];
            mats[MatSkin] = PrimitiveFactory.GetMaterial("Skin", Palette.Skin);
            mats[MatKimono] = PrimitiveFactory.GetMaterial("Kimono", Palette.Kimono);
            mats[MatSash] = PrimitiveFactory.GetMaterial("Sash_" + faction, Palette.FactionColor(faction));
            mats[MatStraw] = PrimitiveFactory.GetMaterial("Straw", Palette.Straw);
            mats[MatWood] = PrimitiveFactory.GetMaterial("Wood", Palette.Wood);
            mats[MatIron] = PrimitiveFactory.GetMaterial("Iron", Palette.Iron, 0.6f);
            mats[MatBamboo] = PrimitiveFactory.GetMaterial("Bamboo", Palette.Bamboo);
            mats[MatStone] = PrimitiveFactory.GetMaterial("Stone", Palette.Stone);
            return mats;
        }

        /// <summary>足元原点、+Z 前向きの村人。</summary>
        public static GameObject Build(UnitKind kind, Faction faction, Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = GetMesh(kind, faction);
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = MaterialsFor(faction);
            return go;
        }

        public static Mesh GetMesh(UnitKind kind, Faction faction)
        {
            if (MeshCache.TryGetValue((kind, faction), out var cached) && cached != null) return cached;
            var mesh = BuildMesh(kind);
            mesh.name = $"Villager_{kind}";
            MeshCache[(kind, faction)] = mesh;
            return mesh;
        }

        static Mesh BuildMesh(UnitKind kind)
        {
            var parts = new List<Part>();
            Mesh cube = PrimitiveFactory.GetPrimitiveMesh(PrimitiveType.Cube);
            Mesh sphere = PrimitiveFactory.GetPrimitiveMesh(PrimitiveType.Sphere);
            Mesh capsule = PrimitiveFactory.GetPrimitiveMesh(PrimitiveType.Capsule);
            Mesh cylinder = PrimitiveFactory.GetPrimitiveMesh(PrimitiveType.Cylinder);
            Mesh cone = PrimitiveFactory.GetCone();

            // 脚 (Unity の Cylinder は高さ 2 なので scale.y = 高さ/2)
            Add(parts, cylinder, new Vector3(-0.09f, 0.22f, 0f), Quaternion.identity, new Vector3(0.13f, 0.22f, 0.13f), MatKimono);
            Add(parts, cylinder, new Vector3(0.09f, 0.22f, 0f), Quaternion.identity, new Vector3(0.13f, 0.22f, 0.13f), MatKimono);
            // 胴 (着物)
            Add(parts, capsule, new Vector3(0f, 0.78f, 0f), Quaternion.identity, new Vector3(0.36f, 0.34f, 0.26f), MatKimono);
            // 帯 = 村の色
            Add(parts, cube, new Vector3(0f, 0.66f, 0f), Quaternion.identity, new Vector3(0.38f, 0.09f, 0.29f), MatSash);
            // 腕
            Add(parts, cylinder, new Vector3(-0.23f, 0.82f, 0.02f), Quaternion.Euler(0f, 0f, 12f), new Vector3(0.09f, 0.22f, 0.09f), MatSkin);
            Add(parts, cylinder, new Vector3(0.23f, 0.82f, 0.02f), Quaternion.Euler(0f, 0f, -12f), new Vector3(0.09f, 0.22f, 0.09f), MatSkin);
            // 頭
            Add(parts, sphere, new Vector3(0f, 1.22f, 0f), Quaternion.identity, new Vector3(0.24f, 0.24f, 0.24f), MatSkin);

            // 被り物
            if (kind == UnitKind.Headman)
            {
                // 陣笠 (平たい鉄笠)
                Add(parts, cone, new Vector3(0f, 1.30f, 0f), Quaternion.identity, new Vector3(0.36f, 0.12f, 0.36f), MatIron);
            }
            else
            {
                // 編み笠
                Add(parts, cone, new Vector3(0f, 1.28f, 0f), Quaternion.identity, new Vector3(0.32f, 0.20f, 0.32f), MatStraw);
            }

            // 得物
            switch (kind)
            {
                case UnitKind.BambooSpear:
                    Add(parts, cylinder, new Vector3(0.30f, 1.05f, 0.15f), Quaternion.Euler(-18f, 0f, 0f), new Vector3(0.035f, 1.15f, 0.035f), MatBamboo);
                    break;
                case UnitKind.Hoe:
                    Add(parts, cylinder, new Vector3(0.30f, 0.95f, 0.05f), Quaternion.Euler(-10f, 0f, 0f), new Vector3(0.04f, 0.6f, 0.04f), MatWood);
                    Add(parts, cube, new Vector3(0.30f, 1.53f, 0.22f), Quaternion.Euler(60f, 0f, 0f), new Vector3(0.16f, 0.04f, 0.18f), MatIron);
                    break;
                case UnitKind.HuntingBow:
                    Add(parts, cylinder, new Vector3(0.32f, 1.0f, 0.12f), Quaternion.Euler(-6f, 0f, 0f), new Vector3(0.03f, 0.75f, 0.03f), MatWood);
                    // 矢筒
                    Add(parts, cylinder, new Vector3(-0.18f, 0.9f, -0.2f), Quaternion.Euler(20f, 0f, 20f), new Vector3(0.08f, 0.3f, 0.08f), MatStraw);
                    break;
                case UnitKind.StoneSling:
                    Add(parts, cylinder, new Vector3(0.30f, 0.62f, 0.12f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.015f, 0.25f, 0.015f), MatWood);
                    Add(parts, sphere, new Vector3(0.30f, 0.55f, 0.16f), Quaternion.identity, new Vector3(0.09f, 0.09f, 0.09f), MatStone);
                    break;
                case UnitKind.Headman:
                    // 腰の刀
                    Add(parts, cube, new Vector3(-0.2f, 0.62f, -0.05f), Quaternion.Euler(0f, 20f, 75f), new Vector3(0.03f, 0.7f, 0.05f), MatIron);
                    break;
            }

            return Combine(parts);
        }

        static void Add(List<Part> parts, Mesh mesh, Vector3 pos, Quaternion rot, Vector3 scale, int material)
        {
            parts.Add(new Part { Mesh = mesh, Matrix = Matrix4x4.TRS(pos, rot, scale), Material = material });
        }

        static Mesh Combine(List<Part> parts)
        {
            var perMaterial = new List<CombineInstance>[MatCount];
            foreach (var p in parts)
            {
                if (perMaterial[p.Material] == null) perMaterial[p.Material] = new List<CombineInstance>();
                perMaterial[p.Material].Add(new CombineInstance { mesh = p.Mesh, transform = p.Matrix, subMeshIndex = 0 });
            }

            var subMeshes = new CombineInstance[MatCount];
            for (int i = 0; i < MatCount; i++)
            {
                var sub = new Mesh();
                if (perMaterial[i] != null) sub.CombineMeshes(perMaterial[i].ToArray(), true, true);
                subMeshes[i] = new CombineInstance { mesh = sub, transform = Matrix4x4.identity, subMeshIndex = 0 };
            }

            var result = new Mesh();
            result.CombineMeshes(subMeshes, false, false);
            result.RecalculateBounds();
            return result;
        }
    }
}
