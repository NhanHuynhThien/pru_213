using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Quản lý hệ thống Evolution Tiers (Giáp Chàm -> Thần Vương)
/// </summary>
public class SkinManager : MonoBehaviour
{
    [Header("Tham chiếu mạng")]
    public SocketClient socketClient;
    public Transform characterRoot; // Nơi sinh ra Model 3D
    
    [Header("Dữ liệu Skin")]
    public List<SkinBase> availableSkins; // Kéo 4 ScriptableObject vào đây

    private GameObject _currentModel;
    public int currentTier = 1;

    void Start()
    {
        // Đăng ký nhận lệnh từ Python Server
        if (socketClient != null)
        {
            socketClient.OnDataReceived += HandleServerResponse;
        }

        // Khởi tạo Skin ban đầu (Tier 1: Giáp Chàm)
        ApplySkinByTier(currentTier);
    }

    private void HandleServerResponse(string jsonData)
    {
        try
        {
            // Nếu Python báo đã xong Step 2 & 3 (Luyện kim & Lắp ráp)
            if (jsonData.Contains("REQUIRE_CONSECRATION"))
            {
                Debug.Log("<color=cyan>[Step 4: Consecration]</color> Hệ thống Python báo: Luyện khí hoàn tất! Bắt đầu thanh tẩy...");
                
                // Kích hoạt thử thách cuối cùng (Ở đây ta giả lập thắng luôn sau 1 giây)
                Invoke("CompleteConsecration", 1.5f);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Lỗi xử lý phản hồi từ Server: " + e.Message);
        }
    }

    private void CompleteConsecration()
    {
        Debug.Log("<color=gold>Thanh tẩy thành công! Kỳ khí đã thức tỉnh.</color>");
        UpgradeToNextTier();
    }

    public void UpgradeToNextTier()
    {
        currentTier++;
        if (currentTier > 4) currentTier = 4;
        ApplySkinByTier(currentTier);
    }

    public void ApplySkinByTier(int tier)
    {
        SkinBase skinData = availableSkins.Find(s => s.Tier == tier);

        if (skinData != null && skinData.ModelPrefab != null)
        {
            if (_currentModel != null) Destroy(_currentModel);

            _currentModel = Instantiate(skinData.ModelPrefab, characterRoot);
            _currentModel.transform.localPosition = Vector3.zero;
            _currentModel.transform.localRotation = Quaternion.identity;

            Debug.Log($"<color=white>Đã chuyển sang:</color> <color=yellow>{skinData.SkinName}</color>");
        }
    }

    void OnDestroy()
    {
        if (socketClient != null)
            socketClient.OnDataReceived -= HandleServerResponse;
    }
}
