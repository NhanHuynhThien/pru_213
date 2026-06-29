using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class CreatePauseMenuTool
{
    [MenuItem("Tools/PRU213/Auto Create Pause and Settings Menu")]
    public static void CreatePauseMenu()
    {
        // 1. Tìm Canvas trong active scene
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

        Undo.RegisterCompleteObjectUndo(canvas.gameObject, "Create Premium Pause & Settings Menu");

        // 3. Tìm HUDPanel để tạo Nút Bánh Răng Cài Đặt (Gear Button)
        Transform hudPanel = canvas.transform.Find("HUDPanel");
        GameObject gearBtnObj = null;
        if (hudPanel != null)
        {
            Transform existingGear = hudPanel.Find("GearSettingsButton");
            if (existingGear != null)
            {
                gearBtnObj = existingGear.gameObject;
            }
            else
            {
                gearBtnObj = new GameObject("GearSettingsButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                gearBtnObj.transform.SetParent(hudPanel, false);
            }

            // Tạo/cập nhật chữ "MENU" cho nút
            Transform txtTrans = gearBtnObj.transform.Find("Text (TMP)");
            GameObject txtObj;
            if (txtTrans != null)
            {
                txtObj = txtTrans.gameObject;
            }
            else
            {
                txtObj = new GameObject("Text (TMP)", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                txtObj.transform.SetParent(gearBtnObj.transform, false);
            }

            RectTransform txtRect = txtObj.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
            tmp.text = "<b>MENU</b>";
            tmp.fontSize = 14;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.95f, 0.75f, 0.45f, 1f); // Màu chữ vàng đồng sáng
            tmp.enableWordWrapping = false;

            // Thiết lập vị trí Nút Menu ở góc trên bên phải HUDPanel
            RectTransform gearRect = gearBtnObj.GetComponent<RectTransform>();
            gearRect.anchorMin = new Vector2(1f, 1f);
            gearRect.anchorMax = new Vector2(1f, 1f);
            gearRect.pivot = new Vector2(1f, 1f);
            gearRect.anchoredPosition = new Vector2(-20f, -20f);
            gearRect.sizeDelta = new Vector2(80f, 32f); // Hình chữ nhật bo góc sang trọng

            Image gearImg = gearBtnObj.GetComponent<Image>();
            gearImg.color = new Color(0.12f, 0.08f, 0.06f, 0.95f); // Nền gỗ sồi tối

            // Thêm viền vàng nhẹ cho nút
            Outline outline = gearBtnObj.GetComponent<Outline>();
            if (outline == null) outline = gearBtnObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.85f, 0.65f, 0.35f, 0.8f);
            outline.effectDistance = new Vector2(1.5f, 1.5f);
        }

        // 4. Tìm/Tạo đối tượng gốc PausePanel
        Transform pausePanelTrans = canvas.transform.Find("PausePanel");
        GameObject pausePanelObj;
        if (pausePanelTrans != null)
        {
            pausePanelObj = pausePanelTrans.gameObject;
        }
        else
        {
            pausePanelObj = new GameObject("PausePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            pausePanelObj.transform.SetParent(canvas.transform, false);
        }

        RectTransform pausePanelRect = pausePanelObj.GetComponent<RectTransform>();
        pausePanelRect.anchorMin = Vector2.zero;
        pausePanelRect.anchorMax = Vector2.one;
        pausePanelRect.offsetMin = Vector2.zero;
        pausePanelRect.offsetMax = Vector2.zero;

        // Phủ nền mờ tối cho toàn màn hình khi pause game
        Image pauseBgImg = pausePanelObj.GetComponent<Image>();
        if (pauseBgImg == null)
        {
            pauseBgImg = pausePanelObj.AddComponent<Image>();
        }
        pauseBgImg.color = new Color(0f, 0f, 0f, 0.75f);

        // 5. Tạo GameObject Viền ngoài (Gold Border) cho bảng gỗ chính
        Transform existingBorder = pausePanelObj.transform.Find("PauseMenu_Border");
        GameObject borderObj;
        if (existingBorder != null)
        {
            borderObj = existingBorder.gameObject;
        }
        else
        {
            borderObj = new GameObject("PauseMenu_Border", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            borderObj.transform.SetParent(pausePanelObj.transform, false);
        }

        RectTransform borderRect = borderObj.GetComponent<RectTransform>();
        borderRect.anchorMin = new Vector2(0.5f, 0.5f);
        borderRect.anchorMax = new Vector2(0.5f, 0.5f);
        borderRect.pivot = new Vector2(0.5f, 0.5f);
        borderRect.anchoredPosition = Vector2.zero;
        borderRect.sizeDelta = new Vector2(404f, 484f); // Rộng 404, cao 484

        Image borderImg = borderObj.GetComponent<Image>();
        borderImg.color = new Color(0.85f, 0.65f, 0.35f, 1f); // Viền vàng cổ

        // 6. Tạo GameObject Bảng Gỗ trong (Menu Content)
        Transform existingBg = borderObj.transform.Find("PauseMenu_Bg");
        GameObject menuBgObj;
        if (existingBg != null)
        {
            menuBgObj = existingBg.gameObject;
        }
        else
        {
            menuBgObj = new GameObject("PauseMenu_Bg", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            menuBgObj.transform.SetParent(borderObj.transform, false);
        }

        RectTransform menuBgRect = menuBgObj.GetComponent<RectTransform>();
        menuBgRect.anchorMin = Vector2.zero;
        menuBgRect.anchorMax = Vector2.one;
        menuBgRect.pivot = new Vector2(0.5f, 0.5f);
        menuBgRect.offsetMin = new Vector2(3f, 3f);
        menuBgRect.offsetMax = new Vector2(-3f, -3f);

        Image menuBgImg = menuBgObj.GetComponent<Image>();
        menuBgImg.color = new Color(0.12f, 0.08f, 0.06f, 0.98f); // Gỗ sồi tối đậm chất hoàng cung

        // 7. Tiêu đề "TẠM DỪNG"
        Transform titleTrans = menuBgObj.transform.Find("TitleText");
        GameObject titleObj;
        if (titleTrans != null)
        {
            titleObj = titleTrans.gameObject;
        }
        else
        {
            titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(menuBgObj.transform, false);
        }

        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -20f);
        titleRect.sizeDelta = new Vector2(0f, 40f);

        TextMeshProUGUI titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
        titleTmp.text = "<b>TẠM DỪNG</b>";
        titleTmp.fontSize = 26;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = new Color(0.95f, 0.75f, 0.45f, 1f);

        // 7b. Tạo Nút Đóng (Close Button - Chữ X sắc nét) cho Pause Menu
        Transform closeBtnTrans = menuBgObj.transform.Find("Close_Button");
        GameObject closeBtnObj;
        if (closeBtnTrans != null)
        {
            closeBtnObj = closeBtnTrans.gameObject;
        }
        else
        {
            closeBtnObj = new GameObject("Close_Button", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            closeBtnObj.transform.SetParent(menuBgObj.transform, false);

            GameObject closeTextObj = new GameObject("Text (TMP)", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            closeTextObj.transform.SetParent(closeBtnObj.transform, false);

            RectTransform closeTextRect = closeTextObj.GetComponent<RectTransform>();
            closeTextRect.anchorMin = Vector2.zero;
            closeTextRect.anchorMax = Vector2.one;
            closeTextRect.offsetMin = Vector2.zero;
            closeTextRect.offsetMax = Vector2.zero;

            TextMeshProUGUI closeTmp = closeTextObj.GetComponent<TextMeshProUGUI>();
            closeTmp.text = "<b>X</b>";
            closeTmp.fontSize = 16;
            closeTmp.alignment = TextAlignmentOptions.Center;
            closeTmp.color = Color.white;
            closeTmp.enableWordWrapping = false;
        }

        RectTransform closeRect = closeBtnObj.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-12f, -12f);
        closeRect.sizeDelta = new Vector2(28f, 28f);

        Image closeImg = closeBtnObj.GetComponent<Image>();
        closeImg.color = new Color(0.55f, 0.15f, 0.15f, 1f); // Đỏ thẫm

        Outline closeOutline = closeBtnObj.GetComponent<Outline>();
        if (closeOutline == null) closeOutline = closeBtnObj.AddComponent<Outline>();
        closeOutline.effectColor = new Color(0.85f, 0.65f, 0.35f, 0.8f);
        closeOutline.effectDistance = new Vector2(1.5f, 1.5f);

        // 8. Tạo 2 nút bấm chuyển Tab (CÀI ĐẶT & PHÍM TẮT)
        GameObject tabSettingsBtnObj = CreateButtonHelper(menuBgObj.transform, "Button_TabSettings", "CÀI ĐẶT", new Vector2(-90f, -85f), new Vector2(120f, 30f));
        GameObject tabControlsBtnObj = CreateButtonHelper(menuBgObj.transform, "Button_TabControls", "PHÍM TẮT", new Vector2(90f, -85f), new Vector2(120f, 30f));

        // 9. Tạo 2 Panel nội dung (TabContent_Settings & TabContent_Controls)
        Transform existingContentSettings = menuBgObj.transform.Find("TabContent_Settings");
        GameObject contentSettingsObj;
        if (existingContentSettings != null)
        {
            contentSettingsObj = existingContentSettings.gameObject;
        }
        else
        {
            contentSettingsObj = new GameObject("TabContent_Settings", typeof(RectTransform));
            contentSettingsObj.transform.SetParent(menuBgObj.transform, false);
        }

        RectTransform cSetRect = contentSettingsObj.GetComponent<RectTransform>();
        cSetRect.anchorMin = new Vector2(0f, 0f);
        cSetRect.anchorMax = new Vector2(1f, 1f);
        cSetRect.offsetMin = new Vector2(10f, 10f);
        cSetRect.offsetMax = new Vector2(-10f, -120f); // Dành phần dưới cho các nút Tiếp tục/Thoát

        // Tạo sliders cho nhạc và sfx
        GameObject bgmText = CreateTextHelper(contentSettingsObj.transform, "BGMText", "<b>Nhạc nền (BGM)</b>", new Vector2(25f, -25f), 16);
        GameObject bgmSliderObj = CreateSliderHelper(contentSettingsObj.transform, "Slider_BGM", new Vector2(0f, -50f));

        GameObject sfxText = CreateTextHelper(contentSettingsObj.transform, "SFXText", "<b>Âm thanh (SFX)</b>", new Vector2(25f, -95f), 16);
        GameObject sfxSliderObj = CreateSliderHelper(contentSettingsObj.transform, "Slider_SFX", new Vector2(0f, -120f));

        // Dọn dẹp các nút cũ trực tiếp dưới menuBgObj (nếu có) để tránh bị lặp nút
        string[] oldBtnNames = { "Button_Resume", "Button_MainMenu", "Button_Quit" };
        foreach (var oldName in oldBtnNames)
        {
            Transform oldBtn = menuBgObj.transform.Find(oldName);
            if (oldBtn != null)
            {
                Object.DestroyImmediate(oldBtn.gameObject);
            }
        }

        // Các nút bấm điều khiển game chính (Tiếp tục, Menu, Thoát) - Đặt làm con của Tab Cài Đặt (contentSettingsObj)
        GameObject resumeBtnObj = CreateButtonHelper(contentSettingsObj.transform, "Button_Resume", "TIẾP TỤC", new Vector2(0f, -180f), new Vector2(240f, 35f));
        GameObject mainMenuBtnObj = CreateButtonHelper(contentSettingsObj.transform, "Button_MainMenu", "ĐĂNG XUẤT", new Vector2(0f, -235f), new Vector2(240f, 35f));
        GameObject quitBtnObj = CreateButtonHelper(contentSettingsObj.transform, "Button_Quit", "THOÁT GAME", new Vector2(0f, -290f), new Vector2(240f, 35f));

        // Thiết lập màu sắc nút Đăng xuất / Thoát
        mainMenuBtnObj.GetComponent<Image>().color = new Color(0.5f, 0.15f, 0.15f, 1f);
        quitBtnObj.GetComponent<Image>().color = new Color(0.4f, 0.1f, 0.1f, 1f);

        // Tạo TabContent_Controls
        Transform existingContentControls = menuBgObj.transform.Find("TabContent_Controls");
        GameObject contentControlsObj;
        if (existingContentControls != null)
        {
            contentControlsObj = existingContentControls.gameObject;
        }
        else
        {
            contentControlsObj = new GameObject("TabContent_Controls", typeof(RectTransform));
            contentControlsObj.transform.SetParent(menuBgObj.transform, false);
        }

        RectTransform cCtrlRect = contentControlsObj.GetComponent<RectTransform>();
        cCtrlRect.anchorMin = new Vector2(0f, 0f);
        cCtrlRect.anchorMax = new Vector2(1f, 1f);
        cCtrlRect.offsetMin = new Vector2(10f, 10f);
        cCtrlRect.offsetMax = new Vector2(-10f, -120f);

        // Dọn dẹp nội dung cũ trong TabContent_Controls để vẽ lại từ đầu
        foreach (Transform child in contentControlsObj.transform)
        {
            Object.DestroyImmediate(child.gameObject);
        }

        string[] keyLabels = { "W, A, S, D", "SPACE", "LEFT SHIFT", "CHUỘT TRÁI / F", "ALT TRÁI / TAB", "I / B", "C" };
        string[] keyDescs = { "Di chuyển nhân vật", "Nhảy cao", "Chạy nhanh", "Tấn công kiếm", "Hiện/Ẩn con trỏ chuột", "Mở Hành trang / Lò rèn", "Bảng trạng thái nhân vật" };

        float startY = -15f;
        float rowSpacing = -48f;
        float rowWidth = 350f;
        float rowHeight = 38f;

        for (int i = 0; i < keyLabels.Length; i++)
        {
            GameObject rowObj = new GameObject("Row_" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            rowObj.transform.SetParent(contentControlsObj.transform, false);

            RectTransform rowRect = rowObj.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.5f, 1f);
            rowRect.anchorMax = new Vector2(0.5f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.anchoredPosition = new Vector2(0f, startY + (i * rowSpacing));
            rowRect.sizeDelta = new Vector2(rowWidth, rowHeight);

            // Nền tối mờ nhẹ cho từng phím
            Image rowImg = rowObj.GetComponent<Image>();
            rowImg.color = new Color(0.06f, 0.04f, 0.03f, 0.45f);

            // Viền vàng mỏng bên dưới để ngăn cách dòng
            GameObject border = new GameObject("Border", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            border.transform.SetParent(rowObj.transform, false);
            RectTransform lineBorderRect = border.GetComponent<RectTransform>();
            lineBorderRect.anchorMin = new Vector2(0f, 0f);
            lineBorderRect.anchorMax = new Vector2(1f, 0f);
            lineBorderRect.pivot = new Vector2(0.5f, 0f);
            lineBorderRect.anchoredPosition = Vector2.zero;
            lineBorderRect.sizeDelta = new Vector2(0f, 1f);
            border.GetComponent<Image>().color = new Color(0.85f, 0.65f, 0.35f, 0.25f);

            // Cột trái: Phím bấm (Màu vàng sáng nổi bật)
            GameObject keyTextObj = new GameObject("KeyText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            keyTextObj.transform.SetParent(rowObj.transform, false);
            RectTransform keyTextRect = keyTextObj.GetComponent<RectTransform>();
            keyTextRect.anchorMin = new Vector2(0f, 0.5f);
            keyTextRect.anchorMax = new Vector2(0.5f, 0.5f);
            keyTextRect.pivot = new Vector2(0f, 0.5f);
            keyTextRect.anchoredPosition = new Vector2(15f, 0f);
            keyTextRect.sizeDelta = new Vector2(rowWidth * 0.5f - 20f, rowHeight);

            TextMeshProUGUI keyTmp = keyTextObj.GetComponent<TextMeshProUGUI>();
            keyTmp.text = "<b><color=#FFD700>" + keyLabels[i] + "</color></b>";
            keyTmp.fontSize = 14;
            keyTmp.alignment = TextAlignmentOptions.Left;
            keyTmp.enableWordWrapping = false;

            // Cột phải: Mô tả chức năng (Màu trắng bạc tinh tế)
            GameObject descTextObj = new GameObject("DescText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            descTextObj.transform.SetParent(rowObj.transform, false);
            RectTransform descTextRect = descTextObj.GetComponent<RectTransform>();
            descTextRect.anchorMin = new Vector2(0.5f, 0.5f);
            descTextRect.anchorMax = new Vector2(1f, 0.5f);
            descTextRect.pivot = new Vector2(1f, 0.5f);
            descTextRect.anchoredPosition = new Vector2(-15f, 0f);
            descTextRect.sizeDelta = new Vector2(rowWidth * 0.5f - 20f, rowHeight);

            TextMeshProUGUI descTmp = descTextObj.GetComponent<TextMeshProUGUI>();
            descTmp.text = keyDescs[i];
            descTmp.fontSize = 14;
            descTmp.alignment = TextAlignmentOptions.Right;
            descTmp.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            descTmp.enableWordWrapping = false;
        }

        // 10. Gán các liên kết vào UIManager tự động
        uiManager.pausePanel = pausePanelObj;
        uiManager.gearButton = gearBtnObj;
        uiManager.tabSettingsBtn = tabSettingsBtnObj.GetComponent<Button>();
        uiManager.tabControlsBtn = tabControlsBtnObj.GetComponent<Button>();
        uiManager.tabContentSettings = contentSettingsObj;
        uiManager.tabContentControls = contentControlsObj;
        uiManager.musicVolumeSlider = bgmSliderObj.GetComponent<Slider>();
        uiManager.sfxVolumeSlider = sfxSliderObj.GetComponent<Slider>();
        uiManager.resumeButton = resumeBtnObj.GetComponent<Button>();
        uiManager.mainMenuButton = mainMenuBtnObj.GetComponent<Button>();
        uiManager.quitButton = quitBtnObj.GetComponent<Button>();

        // Mặc định ẩn Panel khi bắt đầu
        pausePanelObj.SetActive(false);
        contentSettingsObj.SetActive(true);
        contentControlsObj.SetActive(false);

        EditorUtility.SetDirty(uiManager);
        EditorUtility.SetDirty(canvas.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);

        Debug.Log("<color=green>[Tools]</color> Đã tự động dựng và cấu hình bảng Pause Menu & Settings thành công!");
    }

    private static GameObject CreateButtonHelper(Transform parent, string name, string text, Vector2 pos, Vector2 size)
    {
        Transform trans = parent.Find(name);
        GameObject btnObj;
        if (trans != null)
        {
            btnObj = trans.gameObject;
        }
        else
        {
            btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);

            GameObject txtObj = new GameObject("Text (TMP)", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform txtRect = txtObj.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
            tmp.text = "<b>" + text + "</b>";
            tmp.fontSize = (size.y > 35f) ? 18 : 14;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.enableWordWrapping = false;
        }

        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        Image img = btnObj.GetComponent<Image>();
        img.color = new Color(0.24f, 0.18f, 0.14f, 1f); // Nền gỗ sáng cho nút

        Outline outline = btnObj.GetComponent<Outline>();
        if (outline == null) outline = btnObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.85f, 0.65f, 0.35f, 0.8f);
        outline.effectDistance = new Vector2(1f, 1f);

        return btnObj;
    }

    private static GameObject CreateTextHelper(Transform parent, string name, string text, Vector2 pos, int fontSize)
    {
        Transform trans = parent.Find(name);
        GameObject txtObj;
        if (trans != null)
        {
            txtObj = trans.gameObject;
        }
        else
        {
            txtObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(parent, false);
        }

        RectTransform rect = txtObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(-50f, 300f); // Cho phép giãn dòng tự do

        TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.color = new Color(0.9f, 0.9f, 0.9f, 1f);

        return txtObj;
    }

    private static GameObject CreateSliderHelper(Transform parent, string name, Vector2 pos)
    {
        Transform trans = parent.Find(name);
        GameObject sldObj;
        if (trans != null)
        {
            sldObj = trans.gameObject;
        }
        else
        {
            // Tạo Slider GameObject
            sldObj = new GameObject(name, typeof(RectTransform), typeof(Slider));
            sldObj.transform.SetParent(parent, false);

            // Tạo Background
            GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bgObj.transform.SetParent(sldObj.transform, false);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0.25f);
            bgRect.anchorMax = new Vector2(1f, 0.75f);
            bgRect.sizeDelta = Vector2.zero;
            bgObj.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 1f);

            // Tạo Fill Area
            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sldObj.transform, false);
            RectTransform faRect = fillArea.GetComponent<RectTransform>();
            faRect.anchorMin = new Vector2(0f, 0.25f);
            faRect.anchorMax = new Vector2(1f, 0.75f);
            faRect.sizeDelta = Vector2.zero;

            GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillObj.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.sizeDelta = Vector2.zero;
            fillObj.GetComponent<Image>().color = new Color(0.85f, 0.65f, 0.35f, 1f); // Màu thanh chạy màu vàng đồng

            // Tạo Handle Slide Area & Handle
            GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(sldObj.transform, false);
            RectTransform haRect = handleArea.GetComponent<RectTransform>();
            haRect.anchorMin = Vector2.zero;
            haRect.anchorMax = Vector2.one;
            haRect.sizeDelta = Vector2.zero;

            GameObject handleObj = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            handleObj.transform.SetParent(handleArea.transform, false);
            RectTransform handleRect = handleObj.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(15f, 0f);
            handleObj.GetComponent<Image>().color = Color.white;

            // Thiết lập các thuộc tính Slider component
            Slider slider = sldObj.GetComponent<Slider>();
            slider.fillRect = fillObj.GetComponent<RectTransform>();
            slider.handleRect = handleObj.GetComponent<RectTransform>();
            slider.targetGraphic = handleObj.GetComponent<Image>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
        }

        RectTransform rect = sldObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(250f, 20f);

        return sldObj;
    }
}
