using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class CreateStatsPanelTool
{
    [MenuItem("Tools/PRU213/Auto Create Character Stats Panel")]
    public static void CreateStatsPanel()
    {
        // 1. Tìm Canvas trong scene
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[Tools] Không tìm thấy Canvas nào trong Scene active. Hãy mở GameScene!");
            return;
        }

        // 2. Tìm UIManager trên Canvas
        UIManager uiManager = canvas.GetComponent<UIManager>();
        if (uiManager == null)
        {
            uiManager = canvas.gameObject.AddComponent<UIManager>();
        }

        Undo.RegisterCompleteObjectUndo(canvas.gameObject, "Create Premium Stats Panel");

        // 3. Tạo GameObject Viền ngoài (Gold Border)
        Transform existingBorder = canvas.transform.Find("CharacterStatsPanel_Border");
        GameObject borderObj;
        if (existingBorder != null)
        {
            borderObj = existingBorder.gameObject;
        }
        else
        {
            borderObj = new GameObject("CharacterStatsPanel_Border", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            borderObj.transform.SetParent(canvas.transform, false);
        }

        RectTransform borderRect = borderObj.GetComponent<RectTransform>();
        borderRect.anchorMin = new Vector2(0.5f, 0.5f);
        borderRect.anchorMax = new Vector2(0.5f, 0.5f);
        borderRect.pivot = new Vector2(0.5f, 0.5f);
        borderRect.anchoredPosition = new Vector2(-280f, 40f); // Đặt lệch trái, tránh đè lên rương đồ
        borderRect.sizeDelta = new Vector2(324f, 424f); // Lớn hơn panel trong 4px để làm viền

        // Màu viền vàng đồng cổ kính (Antique Gold)
        Image borderImg = borderObj.GetComponent<Image>();
        borderImg.color = new Color(0.85f, 0.65f, 0.35f, 1f); 

        // 4. Tạo GameObject Bảng gỗ trong (Stats Panel) làm con của Viền
        Transform existingPanel = borderObj.transform.Find("CharacterStatsPanel");
        GameObject panelObj;
        if (existingPanel != null)
        {
            panelObj = existingPanel.gameObject;
        }
        else
        {
            panelObj = new GameObject("CharacterStatsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelObj.transform.SetParent(borderObj.transform, false);
        }

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.offsetMin = new Vector2(3f, 3f); // Thụt vào 3px để lộ viền vàng
        panelRect.offsetMax = new Vector2(-3f, -3f);

        // Màu nền gỗ sồi sẫm sang trọng
        Image panelImg = panelObj.GetComponent<Image>();
        panelImg.color = new Color(0.12f, 0.08f, 0.06f, 0.98f); 

        // 5. Tạo Tiêu đề "TRẠNG THÁI"
        Transform titleTrans = panelObj.transform.Find("TitleText");
        GameObject titleObj;
        if (titleTrans != null)
        {
            titleObj = titleTrans.gameObject;
        }
        else
        {
            titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(panelObj.transform, false);
        }

        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -20f);
        titleRect.sizeDelta = new Vector2(0f, 40f);

        TextMeshProUGUI titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
        titleTmp.text = "<b>TRẠNG THÁI</b>";
        titleTmp.fontSize = 24;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = new Color(0.95f, 0.75f, 0.45f, 1f); // Màu chữ vàng đồng sáng

        // 6. Tạo đường vạch chia ngăn (Divider Line) bằng vàng
        Transform divTrans = panelObj.transform.Find("DividerLine");
        GameObject divObj;
        if (divTrans != null)
        {
            divObj = divTrans.gameObject;
        }
        else
        {
            divObj = new GameObject("DividerLine", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            divObj.transform.SetParent(panelObj.transform, false);
        }

        RectTransform divRect = divObj.GetComponent<RectTransform>();
        divRect.anchorMin = new Vector2(0.1f, 1f);
        divRect.anchorMax = new Vector2(0.9f, 1f);
        divRect.pivot = new Vector2(0.5f, 1f);
        divRect.anchoredPosition = new Vector2(0f, -65f);
        divRect.sizeDelta = new Vector2(0f, 2f); // Độ mỏng 2px

        Image divImg = divObj.GetComponent<Image>();
        divImg.color = new Color(0.85f, 0.65f, 0.35f, 0.6f); // Vàng mờ quý phái

        // 7. Tạo các Text hiển thị chỉ số (Căn lề trái đẹp mắt)
        string[] textNames = { "LevelText", "DamageText", "DefenseText", "HPText", "StaminaText" };
        string[] defaultTexts = { 
            "<b>Cấp độ:</b> <color=#FFD700>Tier 1</color>", 
            "<b>Sát thương:</b> <color=#FFD700>35</color>", 
            "<b>Phòng thủ:</b> <color=#FFD700>5</color>", 
            "<b>Sinh mệnh:</b> <color=#FFD700>100 / 100</color>", 
            "<b>Thể lực:</b> <color=#FFD700>100 / 100</color>" 
        };
        TextMeshProUGUI[] tmps = new TextMeshProUGUI[textNames.Length];

        float startY = -110f; // Điểm bắt đầu dưới thanh Divider
        float spacingY = -48f; // Spacing rộng vừa phải

        for (int i = 0; i < textNames.Length; i++)
        {
            Transform txtTrans = panelObj.transform.Find(textNames[i]);
            GameObject txtObj;
            if (txtTrans != null)
            {
                txtObj = txtTrans.gameObject;
            }
            else
            {
                txtObj = new GameObject(textNames[i], typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                txtObj.transform.SetParent(panelObj.transform, false);
            }

            RectTransform txtRect = txtObj.GetComponent<RectTransform>();
            txtRect.anchorMin = new Vector2(0f, 1f);
            txtRect.anchorMax = new Vector2(1f, 1f);
            txtRect.pivot = new Vector2(0.5f, 1f);
            txtRect.anchoredPosition = new Vector2(25f, startY + (i * spacingY)); // Thụt lề vào 25px nhìn rất gọn
            txtRect.sizeDelta = new Vector2(-50f, 35f);

            TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
            tmp.text = defaultTexts[i];
            tmp.fontSize = 20;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.color = new Color(0.9f, 0.9f, 0.9f, 1f); // Màu chữ trắng đục sang trọng
            tmp.enableWordWrapping = false; // Tắt ngắt dòng

            tmps[i] = tmp;
        }

        // 8. Tạo Nút Đóng (Close Button - Chữ X sắc nét không bị lỗi font)
        Transform btnTrans = panelObj.transform.Find("Close_Button");
        GameObject btnObj;
        if (btnTrans != null)
        {
            btnObj = btnTrans.gameObject;
        }
        else
        {
            btnObj = new GameObject("Close_Button", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(panelObj.transform, false);
            
            GameObject btnTextObj = new GameObject("Text (TMP)", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            btnTextObj.transform.SetParent(btnObj.transform, false);
            
            RectTransform btnTextRect = btnTextObj.GetComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.offsetMin = Vector2.zero;
            btnTextRect.offsetMax = Vector2.zero;

            TextMeshProUGUI btnTmp = btnTextObj.GetComponent<TextMeshProUGUI>();
            btnTmp.text = "<b>X</b>";
            btnTmp.fontSize = 16;
            btnTmp.alignment = TextAlignmentOptions.Center;
            btnTmp.color = Color.white;
            btnTmp.enableWordWrapping = false; // Ngăn chặn tự xuống dòng chữ X
        }

        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(1f, 1f);
        btnRect.anchorMax = new Vector2(1f, 1f);
        btnRect.pivot = new Vector2(1f, 1f);
        btnRect.anchoredPosition = new Vector2(-12f, -12f);
        btnRect.sizeDelta = new Vector2(28f, 28f); // Kích thước vuông vắn nhỏ gọn

        Image btnImg = btnObj.GetComponent<Image>();
        btnImg.color = new Color(0.55f, 0.15f, 0.15f, 1f); // Đỏ thẫm hoàng gia

        // Thêm viền vàng nhỏ cho nút X
        Outline btnOutline = btnObj.GetComponent<Outline>();
        if (btnOutline == null) btnOutline = btnObj.AddComponent<Outline>();
        btnOutline.effectColor = new Color(0.85f, 0.65f, 0.35f, 0.8f);
        btnOutline.effectDistance = new Vector2(1.5f, 1.5f);

        // Gán sự kiện click cho nút Đóng
        Button button = btnObj.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        // (Sự kiện thực tế sẽ được UIManager tự động gắn khi Run-time ở Start() để tránh lỗi mất liên kết)

        // 9. Gán các liên kết vào UIManager tự động
        uiManager.characterStatsPanel = borderObj; // Dùng Object Border ngoài làm Panel bật tắt để đi kèm viền vàng
        uiManager.statsLevelText = tmps[0];
        uiManager.statsDamageText = tmps[1];
        uiManager.statsDefenseText = tmps[2];
        uiManager.statsHpText = tmps[3];
        uiManager.statsStaminaText = tmps[4];

        // 10. Gán Nút Button cho Avatar_Mask tự động
        Avatar_Mask_Binding(canvas, uiManager);

        // Mặc định ẩn bảng
        borderObj.SetActive(false);

        EditorUtility.SetDirty(uiManager);
        EditorUtility.SetDirty(canvas.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);

        Debug.Log("<color=green>[Tools]</color> Đã tự động dựng và thiết kế bảng CharacterStatsPanel phong cách gỗ vàng cổ kính thành công!");
    }

    private static void Avatar_Mask_Binding(Canvas canvas, UIManager uiManager)
    {
        Transform avatarTrans = canvas.transform.Find("HUDPanel/Avatar_Mask");
        if (avatarTrans != null)
        {
            Button avatarBtn = avatarTrans.GetComponent<Button>();
            if (avatarBtn == null)
            {
                avatarBtn = avatarTrans.gameObject.AddComponent<Button>();
            }
            avatarBtn.onClick.RemoveAllListeners();
        }
    }
}
