using UnityEngine;

public class ElderNPC : MonoBehaviour
{
    [Header("Quest Settings")]
    public float interactionRadius = 4.0f;
    private bool _isPlayerInRange = false;
    private Transform _playerTransform;
    private SkinManager _playerSkinManager;
    private Collider _collider;

    private bool _hasReceivedGift = false;

    void Start()
    {
        Debug.Log($"[ElderNPC] Khởi chạy script trên đối tượng: {gameObject.name}. Trạng thái quà tân thủ: {_hasReceivedGift}");
        _collider = GetComponent<Collider>();
        if (_collider == null)
        {
            // Tự động thêm SphereCollider trigger nếu thiếu để tính khoảng cách
            SphereCollider sc = gameObject.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = interactionRadius;
            _collider = sc;
        }
    }

    void Update()
    {
        if (_hasReceivedGift) return;

        if (_playerTransform == null)
        {
            PlayerController pc = FindAnyObjectByType<PlayerController>();
            if (pc != null)
            {
                _playerTransform = pc.transform;
                _playerSkinManager = pc.GetComponentInChildren<SkinManager>();
            }
            else
            {
                PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
                if (pm != null)
                {
                    _playerTransform = pm.transform;
                    _playerSkinManager = pm.GetComponentInChildren<SkinManager>();
                }
            }
        }

        if (_playerTransform != null)
        {
            Vector3 centerPos = (_collider != null) ? _collider.bounds.center : transform.position;
            centerPos.y = _playerTransform.position.y;

            float distance = Vector3.Distance(centerPos, _playerTransform.position);
            
            float currentRadius = (_collider is SphereCollider) ? ((SphereCollider)_collider).radius : interactionRadius;
            _isPlayerInRange = (distance <= currentRadius);
        }
        else
        {
            _isPlayerInRange = false;
        }

        if (_isPlayerInRange && Input.GetKeyDown(KeyCode.E))
        {
            ReceiveNewbieGift();
        }
    }

    private void ReceiveNewbieGift()
    {
        if (_hasReceivedGift) return;

        if (_playerSkinManager != null)
        {
            _hasReceivedGift = true;
            _isPlayerInRange = false;

            // Nâng cấp lên Tier 1
            _playerSkinManager.currentTier = 1;
            _playerSkinManager.ApplySkinByTier(1);

            // Chạy hiệu ứng
            ParticleManager.Instance?.PlayUpgradeEffect(_playerTransform, 1);
            ParticleManager.Instance?.PlayConsecrationEffect(_playerTransform);

            // Gọi thông báo qua UIManager tập trung
            UIManager.Instance?.ShowNotification("🎉 Đã nhận: Giáp Chàm (Tier 1)!", 5f, new Color(0.2f, 1f, 0.2f));

            Debug.Log("[Trưởng Lão] Đã trao Giáp Chàm Tier 1 thành công cho Player!");
        }
        else
        {
            Debug.LogWarning("[Trưởng Lão] Không tìm thấy SkinManager trên Player!");
        }
    }

    private void OnGUI()
    {
        // 1. Hiển thị dòng chữ nhiệm vụ ở trên cùng màn hình nếu chưa nhận quà
        if (!_hasReceivedGift)
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 22;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;

            string questText = "📜 Nhiệm vụ: Chạy tới gặp Trưởng Lão để nhận Quà Tân Thủ (Giáp Chàm Tier 1)!";
            float x = Screen.width / 2 - 400;
            float y = 50;
            float w = 800;
            float h = 40;

            // Bóng đổ chữ đen
            style.normal.textColor = Color.black;
            GUI.Label(new Rect(x + 1, y + 1, w, h), questText, style);
            GUI.Label(new Rect(x - 1, y - 1, w, h), questText, style);

            // Chữ chính màu vàng Gold
            style.normal.textColor = new Color(1f, 0.84f, 0f);
            GUI.Label(new Rect(x, y, w, h), questText, style);
        }

        // 2. Hiển thị thông báo hướng dẫn nhấn phím E giống hệt Lò Rèn
        if (_isPlayerInRange && !_hasReceivedGift)
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 20;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;

            string promptText = "Nhấn [E] để trò chuyện với Trưởng Lão";
            float x = Screen.width / 2 - 200;
            float y = Screen.height / 2 + 100;
            float w = 400;
            float h = 40;

            // Bóng đổ chữ đen
            style.normal.textColor = Color.black;
            GUI.Label(new Rect(x + 1, y + 1, w, h), promptText, style);
            GUI.Label(new Rect(x - 1, y - 1, w, h), promptText, style);

            // Chữ chính màu vàng Gold
            style.normal.textColor = new Color(1f, 0.84f, 0f);
            GUI.Label(new Rect(x, y, w, h), promptText, style);
        }
    }
}
