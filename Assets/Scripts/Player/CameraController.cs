using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform playerTransform;
    public Vector3 offset = new Vector3(0f, 4f, -6f);

    [Header("Follow Settings")]
    public float followSpeed = 8f;
    public float rotationSpeed = 5f;

    [Header("Orbit Settings")]
    public float orbitSpeed = 3f;
    public float minVerticalAngle = -30f;
    public float maxVerticalAngle = 60f;
    private float currentYaw = 0f;
    private float currentPitch = 20f;
    private float targetYaw = 0f;
    private float targetPitch = 20f;

    [Header("Collision")]
    public LayerMask collisionMask = ~0;
    public float collisionRadius = 0.3f;
    public float clipOffset = 0.3f;

    [Header("Look At")]
    public Vector3 lookAtOffset = new Vector3(0f, 1.5f, 0f);

    private Vector3 currentVelocity;
    private Vector3 currentPosition;

    void Start()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }

        if (playerTransform != null)
        {
            currentPosition = playerTransform.position + offset;
            transform.position = currentPosition;
            transform.LookAt(playerTransform.position + lookAtOffset);
        }

        targetYaw = currentYaw;
        targetPitch = currentPitch;
    }

    void LateUpdate()
    {
        if (playerTransform == null) return;

        // Tự động khóa chuột lại khi click chuột vào màn hình chơi (giúp tránh việc chuột bay ra ngoài màn hình editor)
        if (Input.GetMouseButtonDown(0))
        {
            bool isOverUI = UnityEngine.EventSystems.EventSystem.current != null &&
                            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

            if (!isOverUI)
            {
                bool isInventoryOpen = InventoryUI.Instance != null && InventoryUI.Instance.IsOpen;
                bool isBlacksmithOpen = BlacksmithUI.Instance != null && BlacksmithUI.Instance.IsOpen;
                bool isPauseOpen = UIManager.Instance != null && UIManager.Instance.pausePanel != null && UIManager.Instance.pausePanel.activeSelf;
                bool isStatsOpen = UIManager.Instance != null && UIManager.Instance.characterStatsPanel != null && UIManager.Instance.characterStatsPanel.activeSelf;
                
                if (!isInventoryOpen && !isBlacksmithOpen && !isPauseOpen && !isStatsOpen)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }

        // 1. Nhận đầu vào chuột để xoay camera (Chỉ xoay khi con trỏ chuột bị khóa trong game)
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            targetYaw += Input.GetAxis("Mouse X") * orbitSpeed;
            targetPitch -= Input.GetAxis("Mouse Y") * orbitSpeed;
            // Giới hạn góc ngước lên/cúi xuống để tránh camera lộn ngược đầu
            targetPitch = Mathf.Clamp(targetPitch, minVerticalAngle, maxVerticalAngle);
        }

        // Nội suy mượt mà để khử rung giật của chuột, tạo độ êm ái khi quay camera
        currentYaw = Mathf.Lerp(currentYaw, targetYaw, 15f * Time.deltaTime);
        currentPitch = Mathf.Lerp(currentPitch, targetPitch, 15f * Time.deltaTime);

        // 2. Tính toán Rotation của camera dựa trên góc quay Yaw và Pitch đã được làm mượt
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);

        // 3. Tính toán vị trí mong muốn của camera (sau lưng nhân vật theo hướng xoay)
        float distance = Mathf.Abs(offset.z);
        Vector3 targetDirection = rotation * Vector3.forward;
        Vector3 targetPosition = playerTransform.position + lookAtOffset - targetDirection * distance;

        // 4. Kiểm tra va chạm (Collision) để camera không bị đi xuyên tường hoặc chui xuống đất
        RaycastHit hit;
        Vector3 origin = playerTransform.position + lookAtOffset;
        Vector3 castDirection = (targetPosition - origin).normalized;
        
        if (Physics.SphereCast(origin, collisionRadius, castDirection, out hit, distance, collisionMask))
        {
            // Bỏ qua va chạm với chính người chơi
            if (hit.collider.gameObject != playerTransform.gameObject)
            {
                // Đẩy camera lại gần nhân vật để không đi xuyên qua vật cản
                targetPosition = origin + castDirection * (hit.distance - clipOffset);
            }
        }

        // 5. Di chuyển camera mượt mà tới vị trí đích
        currentPosition = Vector3.SmoothDamp(currentPosition, targetPosition, ref currentVelocity, 1f / followSpeed);
        transform.position = currentPosition;

        // 6. Camera luôn hướng về điểm ngắm (lookAtOffset) trên người nhân vật
        Vector3 lookTarget = playerTransform.position + lookAtOffset;
        transform.LookAt(lookTarget);
    }
}
