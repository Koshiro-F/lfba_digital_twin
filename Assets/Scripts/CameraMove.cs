using UnityEngine;

public class CameraMove : MonoBehaviour
{
    [Header("移動設定")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float fastMoveSpeed = 30f;

    [Header("高さ設定")]
    [SerializeField] private float fixedHeight = 1.65f; // 常に保つY座標

    [Header("回転設定")]
    [SerializeField] private float mouseSensitivity = 2f;

    private float pitch = 0f; // 上下の回転角度
    private float yaw = 0f;   // 左右の回転角度

    void Start()
    {
        // 初期回転を取得
        pitch = transform.eulerAngles.x;
        yaw = transform.eulerAngles.y;

        // Y座標を固定高さに初期化
        Vector3 pos = transform.position;
        pos.y = fixedHeight;
        transform.position = pos;

        Cursor.lockState = CursorLockMode.None;
    }

    void Update()
    {
        HandleMovement();
        HandleRotation();
    }

    /// <summary>
    /// WASD / 矢印キーでカメラを移動（Y座標は常に fixedHeight に固定）
    /// Shiftキーで高速移動
    /// </summary>
    private void HandleMovement()
    {
        float speed = Input.GetKey(KeyCode.LeftShift) ? fastMoveSpeed : moveSpeed;

        float horizontal = Input.GetAxis("Horizontal"); // A/D or ←/→
        float vertical   = Input.GetAxis("Vertical");   // W/S or ↑/↓

        // 前後移動はカメラのforward方向を使うが、Y成分を除いて水平面のみに制限
        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = transform.right;
        right.y = 0f;
        right.Normalize();

        Vector3 direction = right * horizontal + forward * vertical;

        Vector3 newPos = transform.position + direction * speed * Time.deltaTime;
        newPos.y = fixedHeight; // Y軸を常に固定
        transform.position = newPos;
    }

    /// <summary>
    /// 右クリックを押している間、マウス操作でカメラを回転
    /// </summary>
    private void HandleRotation()
    {
        if (!Input.GetMouseButton(1)) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw   += mouseX;
        pitch -= mouseY;
        pitch  = Mathf.Clamp(pitch, -89f, 89f); // 真上・真下で反転しないよう制限

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }
}
