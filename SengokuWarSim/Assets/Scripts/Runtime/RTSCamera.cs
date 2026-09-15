using UnityEngine;

namespace SengokuWarSim
{
    /// <summary>
    /// 俯瞰カメラ。WASD/矢印で平行移動、Q/E または中ボタンドラッグで回転、
    /// ホイールでズーム、画面端で自動スクロール。
    /// </summary>
    public sealed class RTSCamera : MonoBehaviour
    {
        public float PanSpeed = 30f;
        public float RotateSpeed = 90f;
        public float ZoomSpeed = 25f;
        public float MinDistance = 12f;
        public float MaxDistance = 110f;
        public float MinPitch = 25f;
        public float MaxPitch = 75f;
        public float EdgeScrollMargin = 12f;
        public bool EdgeScroll = true;
        public float MapHalfSize = 60f;

        public Vector3 Pivot = Vector3.zero;
        public float Yaw;
        public float Pitch = 52f;
        public float Distance = 55f;

        Vector3 _lastMouse;

        public void SnapTo(Vector3 pivot, float yaw)
        {
            Pivot = pivot;
            Yaw = yaw;
            Distance = 55f;
            Pitch = 52f;
            Apply();
        }

        void Start() => Apply();

        void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            Vector2 move = Vector2.zero;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) move.y += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) move.y -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) move.x += 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) move.x -= 1f;

            if (EdgeScroll && Application.isFocused)
            {
                Vector3 m = Input.mousePosition;
                if (m.x >= 0f && m.x <= Screen.width && m.y >= 0f && m.y <= Screen.height)
                {
                    if (m.x < EdgeScrollMargin) move.x -= 1f;
                    else if (m.x > Screen.width - EdgeScrollMargin) move.x += 1f;
                    if (m.y < EdgeScrollMargin) move.y -= 1f;
                    else if (m.y > Screen.height - EdgeScrollMargin) move.y += 1f;
                }
            }

            if (move.sqrMagnitude > 0f)
            {
                move.Normalize();
                Quaternion yawRot = Quaternion.Euler(0f, Yaw, 0f);
                Vector3 delta = yawRot * new Vector3(move.x, 0f, move.y) * PanSpeed * dt * (Distance / 55f);
                Pivot += delta;
            }

            if (Input.GetKey(KeyCode.Q)) Yaw -= RotateSpeed * dt;
            if (Input.GetKey(KeyCode.E)) Yaw += RotateSpeed * dt;

            if (Input.GetMouseButtonDown(2)) _lastMouse = Input.mousePosition;
            if (Input.GetMouseButton(2))
            {
                Vector3 d = Input.mousePosition - _lastMouse;
                _lastMouse = Input.mousePosition;
                Yaw += d.x * 0.25f;
                Pitch = Mathf.Clamp(Pitch - d.y * 0.15f, MinPitch, MaxPitch);
            }

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.0001f)
                Distance = Mathf.Clamp(Distance - scroll * ZoomSpeed * 4f, MinDistance, MaxDistance);

            Pivot.x = Mathf.Clamp(Pivot.x, -MapHalfSize, MapHalfSize);
            Pivot.z = Mathf.Clamp(Pivot.z, -MapHalfSize, MapHalfSize);
            Apply();
        }

        void Apply()
        {
            Quaternion rot = Quaternion.Euler(Pitch, Yaw, 0f);
            transform.position = Pivot + rot * new Vector3(0f, 0f, -Distance);
            transform.rotation = rot;
        }
    }
}
