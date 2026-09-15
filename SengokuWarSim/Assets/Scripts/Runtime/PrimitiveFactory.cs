using System.Collections.Generic;
using UnityEngine;

namespace SengokuWarSim
{
    /// <summary>
    /// 実行時に生成するマテリアルとプリミティブメッシュのキャッシュ。
    /// 外部アセットなしでシーンを組み立てるための土台。
    /// </summary>
    public static class PrimitiveFactory
    {
        static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        static readonly Dictionary<PrimitiveType, Mesh> PrimitiveMeshes = new Dictionary<PrimitiveType, Mesh>();
        static Mesh _cone;
        static Mesh _pyramid;

        public static void ClearCache()
        {
            Materials.Clear();
            PrimitiveMeshes.Clear();
            _cone = null;
            _pyramid = null;
        }

        public static Material GetMaterial(string key, Color color, float smoothness = 0.1f, bool instancing = true)
        {
            if (Materials.TryGetValue(key, out var cached) && cached != null) return cached;

            var shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Legacy Shaders/Diffuse");
            var mat = new Material(shader) { name = key, color = color };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            mat.enableInstancing = instancing;
            Materials[key] = mat;
            return mat;
        }

        public static Mesh GetPrimitiveMesh(PrimitiveType type)
        {
            if (PrimitiveMeshes.TryGetValue(type, out var mesh) && mesh != null) return mesh;
            var temp = GameObject.CreatePrimitive(type);
            mesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(temp);
            PrimitiveMeshes[type] = mesh;
            return mesh;
        }

        /// <summary>底面が y=0、頂点が y=1、半径 1 の円錐。笠や屋根に使う。</summary>
        public static Mesh GetCone(int segments = 12)
        {
            if (_cone != null) return _cone;
            _cone = BuildCone(segments, "Cone");
            return _cone;
        }

        public static Mesh GetPyramid()
        {
            if (_pyramid != null) return _pyramid;
            _pyramid = BuildCone(4, "Pyramid");
            return _pyramid;
        }

        static Mesh BuildCone(int segments, string name)
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();

            // 側面: 各三角形を独立させてフラットシェーディングにする
            for (int i = 0; i < segments; i++)
            {
                float a0 = i / (float)segments * Mathf.PI * 2f;
                float a1 = (i + 1) / (float)segments * Mathf.PI * 2f;
                var p0 = new Vector3(Mathf.Sin(a0), 0f, Mathf.Cos(a0));
                var p1 = new Vector3(Mathf.Sin(a1), 0f, Mathf.Cos(a1));
                int b = verts.Count;
                verts.Add(Vector3.up);
                verts.Add(p1);
                verts.Add(p0);
                tris.Add(b);
                tris.Add(b + 1);
                tris.Add(b + 2);
            }

            // 底面
            int center = verts.Count;
            verts.Add(Vector3.zero);
            int ringStart = verts.Count;
            for (int i = 0; i < segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                verts.Add(new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)));
            }
            for (int i = 0; i < segments; i++)
            {
                tris.Add(center);
                tris.Add(ringStart + i);
                tris.Add(ringStart + (i + 1) % segments);
            }

            var mesh = new Mesh { name = name };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>コライダーなしのプリミティブを生成する。</summary>
        public static GameObject CreatePart(string name, PrimitiveType type, Material material, Transform parent,
            Vector3 localPosition, Vector3 localScale, Quaternion? localRotation = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = localRotation ?? Quaternion.identity;
            go.transform.localScale = localScale;
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = GetPrimitiveMesh(type);
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = material;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return go;
        }

        public static GameObject CreateMeshPart(string name, Mesh mesh, Material material, Transform parent,
            Vector3 localPosition, Vector3 localScale, Quaternion? localRotation = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = localRotation ?? Quaternion.identity;
            go.transform.localScale = localScale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }
    }

    /// <summary>ゲーム全体で使う色。</summary>
    public static class Palette
    {
        public static readonly Color KamimuraIndigo = new Color(0.16f, 0.25f, 0.55f);   // 藍
        public static readonly Color ShimomuraMadder = new Color(0.72f, 0.18f, 0.16f);  // 茜
        public static readonly Color Skin = new Color(0.86f, 0.68f, 0.52f);
        public static readonly Color Kimono = new Color(0.45f, 0.40f, 0.33f);
        public static readonly Color KimonoDark = new Color(0.30f, 0.27f, 0.24f);
        public static readonly Color Straw = new Color(0.80f, 0.70f, 0.40f);
        public static readonly Color Wood = new Color(0.48f, 0.33f, 0.18f);
        public static readonly Color Bamboo = new Color(0.55f, 0.65f, 0.30f);
        public static readonly Color Iron = new Color(0.35f, 0.36f, 0.38f);
        public static readonly Color Stone = new Color(0.55f, 0.55f, 0.52f);

        public static readonly Color Field = new Color(0.52f, 0.62f, 0.30f);
        public static readonly Color Paddy = new Color(0.30f, 0.52f, 0.45f);
        public static readonly Color Hill = new Color(0.60f, 0.58f, 0.36f);
        public static readonly Color Forest = new Color(0.22f, 0.40f, 0.20f);
        public static readonly Color Road = new Color(0.68f, 0.60f, 0.45f);
        public static readonly Color Canopy = new Color(0.18f, 0.42f, 0.18f);
        public static readonly Color Wall = new Color(0.85f, 0.80f, 0.68f);
        public static readonly Color Thatch = new Color(0.55f, 0.45f, 0.25f);
        public static readonly Color Selection = new Color(1f, 0.9f, 0.3f);

        public static Color FactionColor(SengokuWarSim.Sim.Faction f) =>
            f == SengokuWarSim.Sim.Faction.Kamimura ? KamimuraIndigo : ShimomuraMadder;
    }
}
