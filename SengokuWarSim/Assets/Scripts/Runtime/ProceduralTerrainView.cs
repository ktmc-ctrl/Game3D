using System.Collections.Generic;
using SengokuWarSim.Sim;
using UnityEngine;

namespace SengokuWarSim
{
    /// <summary>TerrainMap を地形メッシュ (地形種ごとのサブメッシュ) と MeshCollider に変換する。</summary>
    public sealed class ProceduralTerrainView : MonoBehaviour
    {
        public TerrainMap Map { get; private set; }

        static readonly TerrainKind[] Kinds =
        {
            TerrainKind.Field, TerrainKind.Paddy, TerrainKind.Hill, TerrainKind.Forest, TerrainKind.Road,
        };

        public static ProceduralTerrainView Build(TerrainMap map, Transform parent)
        {
            var go = new GameObject("Terrain");
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<ProceduralTerrainView>();
            view.Map = map;
            view.BuildMesh();
            return view;
        }

        void BuildMesh()
        {
            int size = Map.Size;
            int vc = Map.VertexCount;
            float half = Map.HalfSize;

            var verts = new Vector3[vc * vc];
            var uvs = new Vector2[vc * vc];
            for (int z = 0; z < vc; z++)
            for (int x = 0; x < vc; x++)
            {
                verts[z * vc + x] = new Vector3(x - half, Map.VertexHeight(x, z), z - half);
                uvs[z * vc + x] = new Vector2(x / (float)size, z / (float)size);
            }

            var tris = new List<int>[Kinds.Length];
            for (int i = 0; i < tris.Length; i++) tris[i] = new List<int>();

            for (int cz = 0; cz < size; cz++)
            for (int cx = 0; cx < size; cx++)
            {
                int sub = System.Array.IndexOf(Kinds, Map.KindAtCell(cx, cz));
                int a = cz * vc + cx;
                int b = a + 1;
                int c = a + vc;
                int d = c + 1;
                var list = tris[sub];
                list.Add(a); list.Add(c); list.Add(b);
                list.Add(b); list.Add(c); list.Add(d);
            }

            var mesh = new Mesh { name = "BattlefieldTerrain" };
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.subMeshCount = Kinds.Length;
            for (int i = 0; i < Kinds.Length; i++) mesh.SetTriangles(tris[i], i);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = gameObject.AddComponent<MeshRenderer>();
            mr.sharedMaterials = new[]
            {
                PrimitiveFactory.GetMaterial("T_Field", Palette.Field, 0.05f, false),
                PrimitiveFactory.GetMaterial("T_Paddy", Palette.Paddy, 0.5f, false),
                PrimitiveFactory.GetMaterial("T_Hill", Palette.Hill, 0.05f, false),
                PrimitiveFactory.GetMaterial("T_Forest", Palette.Forest, 0.05f, false),
                PrimitiveFactory.GetMaterial("T_Road", Palette.Road, 0.05f, false),
            };
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            gameObject.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        public Vector3 ToWorld(Vec2 p) => new Vector3(p.X, Map.HeightAt(p), p.Z);
        public static Vec2 ToSim(Vector3 p) => new Vec2(p.x, p.z);
    }
}
