using UnityEngine;

public class BlacksmithForge : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactionRadius = 6.0f; // Bán kính tương tác thực tế thoải mái (6.0 mét)
    private bool _isPlayerInRange = false;
    private Transform _playerTransform;
    private Collider _collider;

    void Start()
    {
        // Lấy Collider của chính đối tượng để làm mốc tính khoảng cách từ tâm vòng tròn
        _collider = GetComponent<Collider>();
    }

    void Update()
    {
        // Tìm người chơi chính xác (Ưu tiên theo PlayerController để lấy đúng đối tượng con đang di chuyển thay vì đối tượng Cha ở gốc 0,0,0)
        if (_playerTransform == null)
        {
            PlayerController pc = FindAnyObjectByType<PlayerController>();
            if (pc != null)
            {
                _playerTransform = pc.transform;
            }
            else
            {
                // Dự phòng 1: Tìm theo Tag "Player" và lấy đối tượng con chứa PlayerController nếu có
                GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
                if (playerGo != null)
                {
                    PlayerController childPc = playerGo.GetComponentInChildren<PlayerController>();
                    if (childPc != null) _playerTransform = childPc.transform;
                    else _playerTransform = playerGo.transform;
                }
                else
                {
                    // Dự phòng 2: Tìm theo Component PlayerMovement
                    PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
                    if (pm != null) _playerTransform = pm.transform;
                }
            }
        }

        if (_playerTransform != null)
        {
            // Lấy vị trí tâm thực tế của Collider (vòng tròn tương tác) thay vì vị trí Pivot bị lệch
            Vector3 centerPos = (_collider != null) ? _collider.bounds.center : transform.position;
            
            // Giữ tính toán khoảng cách trên mặt phẳng ngang (bỏ qua độ cao Y)
            centerPos.y = _playerTransform.position.y;

            float distance = Vector3.Distance(centerPos, _playerTransform.position);
            bool wasInRange = _isPlayerInRange;
            
            // Lấy bán kính thực tế từ SphereCollider nếu có, ngược lại dùng interactionRadius
            float currentRadius = (_collider is SphereCollider) ? ((SphereCollider)_collider).radius : interactionRadius;
            _isPlayerInRange = (distance <= currentRadius);

            // Nếu người chơi đi xa khỏi lò rèn, tự động đóng giao diện
            if (wasInRange && !_isPlayerInRange)
            {
                if (BlacksmithUI.Instance != null && BlacksmithUI.Instance.IsOpen)
                {
                    BlacksmithUI.Instance.CloseUI();
                }
            }
        }
        else
        {
            _isPlayerInRange = false;
        }

        // Nhấn E để mở/đóng giao diện khi ở trong vùng tương tác
        if (_isPlayerInRange && Input.GetKeyDown(KeyCode.E))
        {
            BlacksmithUI ui = BlacksmithUI.Instance;
            if (ui == null)
            {
                ui = FindAnyObjectByType<BlacksmithUI>(FindObjectsInactive.Include);
                if (ui != null)
                {
                    // Đảm bảo UI khởi tạo nếu nó chưa chạy Awake
                    ui.gameObject.SetActive(true);
                }
            }

            if (ui != null)
            {
                ui.ToggleUI();
            }
            else
            {
                Debug.LogWarning("[Lò Rèn] Không tìm thấy BlacksmithUI trong Scene!");
            }
        }
    }

    // Vẽ dòng chữ hướng dẫn "Nhấn [E] để sử dụng" trên màn hình khi người chơi ở gần
    private void OnGUI()
    {
        // Hiển thị độc lập không phụ thuộc vào UI Instance có bị null hay không (dễ debug)
        if (_isPlayerInRange)
        {
            // Nếu bảng UI đang mở thì ẩn chữ đi cho đỡ vướng
            if (BlacksmithUI.Instance != null && BlacksmithUI.Instance.IsOpen)
                return;

            GUIStyle style = new GUIStyle();
            style.fontSize = 20;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;

            // Bóng đổ chữ đen
            style.normal.textColor = Color.black;
            GUI.Label(new Rect(Screen.width / 2 - 199, Screen.height / 2 + 101, 400, 40), "Nhấn [E] để sử dụng Lò Rèn", style);
            GUI.Label(new Rect(Screen.width / 2 - 201, Screen.height / 2 + 99, 400, 40), "Nhấn [E] để sử dụng Lò Rèn", style);
            
            // Chữ chính màu vàng Gold
            style.normal.textColor = new Color(1f, 0.84f, 0f);
            GUI.Label(new Rect(Screen.width / 2 - 200, Screen.height / 2 + 100, 400, 40), "Nhấn [E] để sử dụng Lò Rèn", style);
        }
    }
}
