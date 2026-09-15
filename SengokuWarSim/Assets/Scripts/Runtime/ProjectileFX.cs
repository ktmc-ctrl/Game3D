using UnityEngine;

namespace SengokuWarSim
{
    /// <summary>矢・石つぶての簡易演出。放物線で飛んで消える。</summary>
    public sealed class ProjectileFX : MonoBehaviour
    {
        Vector3 _from;
        Vector3 _to;
        float _duration;
        float _arc;
        float _t;

        public static void Spawn(Vector3 from, Vector3 to, bool isArrow, Transform parent)
        {
            GameObject go;
            if (isArrow)
            {
                go = PrimitiveFactory.CreatePart("Arrow", PrimitiveType.Cylinder,
                    PrimitiveFactory.GetMaterial("Wood", Palette.Wood), parent, from, new Vector3(0.03f, 0.35f, 0.03f));
            }
            else
            {
                go = PrimitiveFactory.CreatePart("Stone", PrimitiveType.Sphere,
                    PrimitiveFactory.GetMaterial("Stone", Palette.Stone), parent, from, new Vector3(0.14f, 0.14f, 0.14f));
            }
            go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var fx = go.AddComponent<ProjectileFX>();
            fx._from = from;
            fx._to = to;
            float dist = Vector3.Distance(from, to);
            fx._duration = Mathf.Clamp(dist / (isArrow ? 28f : 18f), 0.3f, 1.4f);
            fx._arc = Mathf.Clamp(dist * (isArrow ? 0.18f : 0.28f), 0.5f, 6f);
        }

        void Update()
        {
            _t += Time.deltaTime;
            float u = Mathf.Clamp01(_t / _duration);
            Vector3 pos = Vector3.Lerp(_from, _to, u);
            pos.y += Mathf.Sin(u * Mathf.PI) * _arc;
            Vector3 velocity = pos - transform.position;
            if (velocity.sqrMagnitude > 1e-6f)
                transform.rotation = Quaternion.LookRotation(velocity) * Quaternion.Euler(90f, 0f, 0f);
            transform.position = pos;
            if (u >= 1f) Destroy(gameObject);
        }
    }
}
