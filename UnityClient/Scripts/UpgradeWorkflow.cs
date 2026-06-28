using UnityEngine;

/// <summary>
/// Quản lý quy trình 4 bước: Exploration -> Refining -> Assembly -> Consecration
/// Và tích hợp hệ thống Đăng nhập / Bảng điều khiển Admin Cheat kết nối C# Server.
/// </summary>
public class UpgradeWorkflow : MonoBehaviour
{
    [Header("Tham chiếu Hệ thống")]
    public SocketClient socketClient;
    public SkinManager skinManager;

    [Header("Kho tài nguyên (Step 1)")]
    public int copperCount = 0; // Tài nguyên Đồng
    public int tinCount = 0;    // Tài nguyên Thiếc (Mở rộng thêm)
    public int targetCopperForUpgrade = 10;

    [Header("Trạng thái Đăng nhập")]
    private string usernameInput = "user";
    private string passwordInput = "user123";
    private bool isLoggedIn = false;
    private string username = "";
    private string role = "user";
    private string loginErrorMessage = "";

    void Start()
    {
        // Đăng ký nhận tin nhắn từ C# Server
        if (socketClient != null)
        {
            socketClient.OnDataReceived += HandleServerData;
        }
    }

    void OnDestroy()
    {
        if (socketClient != null)
        {
            socketClient.OnDataReceived -= HandleServerData;
        }
    }

    void Update()
    {
        // Chỉ cho phép chơi sau khi đã Đăng nhập thành công
        if (!isLoggedIn) return;

        // STEP 1: EXPLORATION (Phím E - Khai thác tài nguyên)
        if (Input.GetKeyDown(KeyCode.E))
        {
            MineCopper();
        }

        // KÍCH HOẠT STEP 2 & 3 (Phím U - Gửi rèn giáp lên C# Server)
        if (Input.GetKeyDown(KeyCode.U))
        {
            UpgradeToNextTier();
        }
    }

    private void MineCopper()
    {
        copperCount += 5;
        Debug.Log($"<color=green>[Step 1]</color> Bạn tìm thấy quặng Đồng! Tổng cộng: {copperCount}");
        
        // Đồng bộ số tài nguyên vừa kiếm được về Server
        SyncResources();
    }

    private void SyncResources()
    {
        if (socketClient != null && socketClient.IsConnected)
        {
            string payload = $"{{\"copper_count\":{copperCount}, \"tin_count\":{tinCount}}}";
            socketClient.SendAction("SYNC_DATA", payload);
        }
    }

    private void UpgradeToNextTier()
    {
        if (skinManager == null) return;

        if (skinManager.currentTier >= 4)
        {
            Debug.Log("Bạn đã đạt cấp độ Thần Vương tối thượng!");
            return;
        }

        if (copperCount >= targetCopperForUpgrade)
        {
            Debug.Log("<color=orange>[Step 2 & 3]</color> Gửi nguyên liệu sang Lò Rèn Cổ Loa (C# Server)...");
            
            // Trừ nguyên liệu cục bộ và đồng bộ lên Server trước
            copperCount -= targetCopperForUpgrade;
            SyncResources();

            // Gửi lệnh yêu cầu Server chạy tiến trình Luyện kim
            string payload = $"{{\"target_tier\": {skinManager.currentTier + 1}}}";
            socketClient.SendAction("START_UPGRADE_PROCESS", payload);
        }
        else
        {
            Debug.Log($"<color=red>Thiếu nguyên liệu!</color> Cần {targetCopperForUpgrade} Đồng để nâng cấp.");
        }
    }

    private void SendAdminCheat(int targetTier, int targetCopper)
    {
        if (socketClient != null && socketClient.IsConnected)
        {
            string payload = $"{{\"tier\":{targetTier}, \"copper\":{targetCopper}}}";
            socketClient.SendAction("ADMIN_CHEAT", payload);
            Debug.Log($"<color=yellow>[Admin]</color> Đã gửi lệnh Cheat lên Server: Tier={targetTier}, Copper={targetCopper}");
        }
    }

    // Xử lý dữ liệu nhận từ C# Server
    private void HandleServerData(string jsonData)
    {
        try
        {
            // Phân tích hành động chung từ phản hồi
            ServerActionResponse actionRes = JsonUtility.FromJson<ServerActionResponse>(jsonData);
            if (actionRes == null || string.IsNullOrEmpty(actionRes.action)) return;

            string action = actionRes.action;

            if (action == "LOGIN_SUCCESS")
            {
                LoginSuccessData successData = JsonUtility.FromJson<LoginSuccessData>(jsonData);
                PlayerStateData state = successData.payload;

                username = state.username;
                role = state.role;
                copperCount = state.copper_count;
                tinCount = state.tin_count;
                isLoggedIn = true;
                loginErrorMessage = "";

                if (skinManager != null)
                {
                    skinManager.currentTier = state.current_tier;
                    skinManager.ApplySkinByTier(state.current_tier);
                }

                Debug.Log($"<color=green>[Auth Success]</color> Chào mừng {username} ({role}) tham gia thế giới Loa Thành Kỳ Khí!");
            }
            else if (action == "LOGIN_FAIL")
            {
                LoginFailData failData = JsonUtility.FromJson<LoginFailData>(jsonData);
                loginErrorMessage = failData.payload.message;
                isLoggedIn = false;
                Debug.LogWarning($"[Auth Fail] {loginErrorMessage}");
            }
            else if (action == "SYNC_PLAYER_DATA")
            {
                SyncPlayerDataData syncData = JsonUtility.FromJson<SyncPlayerDataData>(jsonData);
                PlayerStateData state = syncData.payload;

                username = state.username;
                role = state.role;
                copperCount = state.copper_count;
                tinCount = state.tin_count;

                if (skinManager != null)
                {
                    skinManager.currentTier = state.current_tier;
                    skinManager.ApplySkinByTier(state.current_tier);
                }

                Debug.Log($"<color=cyan>[Sync State]</color> Đồng bộ hoàn tất: Tier={state.current_tier}, Copper={state.copper_count}");
            }
        }
        catch (System.Exception ex)
        {
            // Bỏ qua các packet không khớp lớp này (ví dụ REQUIRE_CONSECRATION do SkinManager xử lý riêng)
        }
    }

    // Giao diện người chơi đơn giản & đẹp mắt
    private void OnGUI()
    {
        // Điều chỉnh kích thước chữ cơ bản
        GUI.skin.label.fontSize = 13;
        GUI.skin.button.fontSize = 13;
        GUI.skin.textField.fontSize = 13;

        // Vẽ trạng thái mạng ở góc trên bên phải
        string netStatus = "Chưa kết nối";
        Color netColor = Color.red;
        if (socketClient != null && socketClient.IsConnected)
        {
            netStatus = "Đã kết nối";
            netColor = Color.green;
        }

        GUI.color = Color.white;
        GUILayout.BeginArea(new Rect(Screen.width - 220, 10, 210, 45));
        GUILayout.BeginHorizontal("box");
        GUILayout.Label("Máy chủ: ");
        GUI.color = netColor;
        GUILayout.Label(netStatus, GUILayout.Width(100));
        GUI.color = Color.white;
        GUILayout.EndHorizontal();
        GUILayout.EndArea();

        if (!isLoggedIn)
        {
            DrawLoginUI();
        }
        else
        {
            DrawGameHUD();
            if (role.ToLower() == "admin")
            {
                DrawAdminCheatPanel();
            }
        }
    }

    private void DrawLoginUI()
    {
        float width = 360f;
        float height = 300f;
        float x = (Screen.width - width) / 2f;
        float y = (Screen.height - height) / 2f;

        GUI.Box(new Rect(x, y, width, height), "⚔️ ĐĂNG NHẬP CỔ LOA WORKSHOP ⚔️");

        float contentX = x + 20f;
        float contentWidth = width - 40f;

        GUI.Label(new Rect(contentX, y + 35f, contentWidth, 20f), "Vui lòng đăng nhập để đồng bộ tiến trình");

        // Nhập tài khoản
        GUI.Label(new Rect(contentX, y + 65f, contentWidth, 20f), "Tên đăng nhập:");
        usernameInput = GUI.TextField(new Rect(contentX, y + 85f, contentWidth, 25f), usernameInput);

        // Nhập mật khẩu
        GUI.Label(new Rect(contentX, y + 115f, contentWidth, 20f), "Mật khẩu:");
        passwordInput = GUI.PasswordField(new Rect(contentX, y + 135f, contentWidth, 25f), passwordInput, '*');

        // Nút đăng nhập
        if (GUI.Button(new Rect(contentX, y + 175f, contentWidth, 35f), "ĐĂNG NHẬP"))
        {
            if (socketClient == null)
            {
                loginErrorMessage = "Lỗi: Không tìm thấy SocketClient!";
            }
            else if (!socketClient.IsConnected)
            {
                loginErrorMessage = "Lỗi: Chưa kết nối Server (Hãy chạy Server trước)!";
            }
            else
            {
                loginErrorMessage = "Đang xác thực...";
                string payloadJson = $"{{\"username\":\"{usernameInput.Trim()}\", \"password\":\"{passwordInput.Trim()}\"}}";
                socketClient.SendAction("LOGIN", payloadJson);
            }
        }

        // Hiển thị trạng thái lỗi
        if (!string.IsNullOrEmpty(loginErrorMessage))
        {
            GUI.color = loginErrorMessage.Contains("thành công") || loginErrorMessage.Contains("xác thực") ? Color.cyan : Color.red;
            GUI.Label(new Rect(contentX, y + 215f, contentWidth, 20f), loginErrorMessage);
            GUI.color = Color.white;
        }

        // Bảng gợi ý tài khoản test
        float hintY = y + height + 10f;
        GUI.Box(new Rect(x, hintY, width, 65f), "💡 Gợi ý tài khoản test (Hardcoded)");
        GUI.Label(new Rect(x + 10f, hintY + 20f, width - 20f, 20f), "- User : user / user123 (Chơi bình thường)");
        GUI.Label(new Rect(x + 10f, hintY + 40f, width - 20f, 20f), "- Admin: admin / admin123 (Bảng cheat tối cao)");
    }

    private void DrawGameHUD()
    {
        float width = 280f;
        float height = 270f;
        float x = 20f;
        float y = 20f;

        GUI.Box(new Rect(x, y, width, height), "⚔️ TIẾN TRÌNH CỔ LOA ⚔️");

        float contentX = x + 15f;
        float contentWidth = width - 30f;

        // Thông tin người chơi
        GUI.Label(new Rect(contentX, y + 30f, contentWidth, 20f), $"Xin chào: <b>{username}</b> ({role.ToUpper()})");

        // Thông tin Skin / Tier
        string skinName = "Chưa xác định";
        if (skinManager != null && skinManager.availableSkins != null)
        {
            var skin = skinManager.availableSkins.Find(s => s.Tier == skinManager.currentTier);
            if (skin != null) skinName = skin.SkinName;
        }
        GUI.Label(new Rect(contentX, y + 55f, contentWidth, 20f), $"Trạng thái: <b>Tier {skinManager.currentTier} - {skinName}</b>");
        GUI.Label(new Rect(contentX, y + 80f, contentWidth, 20f), $"Đồng (Copper): <b>{copperCount} quặng</b>");

        // Các phím tắt
        GUI.Label(new Rect(contentX, y + 110f, contentWidth, 20f), "Phím [E]: Đi thám hiểm (Kiếm đồng)");
        GUI.Label(new Rect(contentX, y + 130f, contentWidth, 20f), $"Phím [U]: Gửi Lò rèn (Cần {targetCopperForUpgrade} đồng)");

        // Nút tương tác
        if (GUI.Button(new Rect(contentX, y + 160f, contentWidth, 30f), "⛏️ ĐI THÁM HIỂM (+5 Đồng)"))
        {
            MineCopper();
        }

        if (GUI.Button(new Rect(contentX, y + 195f, contentWidth, 30f), "🔥 LÒ RÈN CỔ LOA (NÂNG CẤP)"))
        {
            UpgradeToNextTier();
        }

        // Nút đăng xuất
        GUI.color = Color.gray;
        if (GUI.Button(new Rect(contentX, y + 235f, contentWidth, 25f), "Đăng xuất"))
        {
            isLoggedIn = false;
            username = "";
            role = "user";
            loginErrorMessage = "";
        }
        GUI.color = Color.white;
    }

    private void DrawAdminCheatPanel()
    {
        float width = 280f;
        float height = 240f;
        float x = Screen.width - width - 20f;
        float y = 70f;

        GUI.Box(new Rect(x, y, width, height), "⚡ BẢNG ADMIN CHEAT TỐI CAO ⚡");

        float contentX = x + 15f;
        float contentWidth = width - 30f;

        GUI.color = Color.yellow;
        GUI.Label(new Rect(contentX, y + 25f, contentWidth, 20f), "Quyền kiểm soát máy chủ:");
        GUI.color = Color.white;

        // Cheat tài nguyên
        if (GUI.Button(new Rect(contentX, y + 50f, contentWidth, 30f), "💰 CHEAT +100 ĐỒNG"))
        {
            SendAdminCheat(skinManager.currentTier, copperCount + 100);
        }

        // Chuyển Tier trực tiếp
        GUI.Label(new Rect(contentX, y + 90f, contentWidth, 20f), "Chuyển đổi cấp độ (Bỏ qua rèn):");

        float btnW = (contentWidth - 10f) / 2f;

        if (GUI.Button(new Rect(contentX, y + 115f, btnW, 25f), "Tier 1: Giáp Chàm"))
        {
            SendAdminCheat(1, copperCount);
        }

        if (GUI.Button(new Rect(contentX + btnW + 10f, y + 115f, btnW, 25f), "Tier 2: Giáp Đồng"))
        {
            SendAdminCheat(2, copperCount);
        }

        if (GUI.Button(new Rect(contentX, y + 145f, btnW, 25f), "Tier 3: Mai Rùa"))
        {
            SendAdminCheat(3, copperCount);
        }

        if (GUI.Button(new Rect(contentX + btnW + 10f, y + 145f, btnW, 25f), "Tier 4: Thần Vương"))
        {
            SendAdminCheat(4, copperCount);
        }

        // Cheat tối thượng
        GUI.color = Color.cyan;
        if (GUI.Button(new Rect(contentX, y + 185f, contentWidth, 35f), "🌟 MAX TÀI NGUYÊN & TIER 4 🌟"))
        {
            SendAdminCheat(4, 9999);
        }
        GUI.color = Color.white;
    }
}

#region Lớp cấu trúc gói tin JSON đồng bộ Unity
[System.Serializable]
public class ServerActionResponse
{
    public string action;
}

[System.Serializable]
public class PlayerStateData
{
    public string username;
    public string role;
    public int player_id;
    public int current_tier;
    public int copper_count;
    public int tin_count;
}

[System.Serializable]
public class LoginSuccessData
{
    public string action;
    public PlayerStateData payload;
}

[System.Serializable]
public class LoginFailPayload
{
    public string message;
}

[System.Serializable]
public class LoginFailData
{
    public string action;
    public LoginFailPayload payload;
}

[System.Serializable]
public class SyncPlayerDataData
{
    public string action;
    public PlayerStateData payload;
}
#endregion
