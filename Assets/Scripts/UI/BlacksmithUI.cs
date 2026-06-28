using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class BlacksmithUI : MonoBehaviour
{
    public static BlacksmithUI Instance { get; private set; }

    [Header("UI State")]
    private bool _isOpen = false;
    public bool IsOpen => _isOpen;

    // Giao diện Panels
    private GameObject _mainPanel;
    private TextMeshProUGUI _titleText;
    private GameObject _recipeListContainer; // Cột trái: Danh sách nút công thức
    private GameObject _recipeDetailContainer; // Cột phải: Chi tiết công thức đang chọn

    // Các phần tử hiển thị chi tiết (Cột phải)
    private TextMeshProUGUI _detailTitleText;
    private TextMeshProUGUI _detailDescText;
    private TextMeshProUGUI _ingredientsText; // Grid hiển thị nguyên liệu trực quan
    private Button _btnAction;
    private TextMeshProUGUI _btnActionText;

    // Thanh Tiến Trình
    private GameObject _progressBarContainer;
    private Image _progressBarFill;
    private TextMeshProUGUI _progressText;

    // Các nút chọn Tab chính
    private Button _btnTabRefine;
    private Button _btnTabWeapon;
    private Button _btnTabArmor;
    private Button _btnClose;

    // Prefabs vũ khí được tải tự động trong Editor
    private GameObject _daggerPrefab;
    private GameObject _swordPrefab;
    private GameObject _bowPrefab;

    // Trạng thái dữ liệu
    private int _currentTab = 0; // 0: Luyện Kim, 1: Rèn Vũ Khí, 2: Rèn Giáp Trụ
    private int _selectedRecipeIndex = 0;
    private bool _isWorking = false;

    private PlayerStats _stats;
    private PlayerMovement _playerMovement;
    private Animator _playerAnimator;

    // Danh sách các nút công thức ở cột trái để quản lý highlight active
    private List<Button> _recipeButtons = new List<Button>();

    // Cấu trúc dữ liệu công thức chế tạo
    private struct Recipe
    {
        public string name;
        public string desc;
        public string resultText;
        public int copperCost;
        public int tinCost;
        public int ingotCost;
        public int shellCost;
        public int stoneCost;
        public System.Action onCraftSuccess;
    }

    private List<Recipe> _refineRecipes = new List<Recipe>();
    private List<Recipe> _weaponRecipes = new List<Recipe>();
    private List<Recipe> _armorRecipes = new List<Recipe>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        gameObject.SetActive(false);
    }

    void Start()
    {
        SetupReferences();
        LoadWeaponPrefabs();
        InitializeRecipes();
        SetupUI();
    }

    private void SetupReferences()
    {
        PlayerController pc = FindAnyObjectByType<PlayerController>();
        if (pc != null)
        {
            _stats = pc.stats;
            _playerMovement = pc.GetComponent<PlayerMovement>();
            _playerAnimator = pc.GetComponentInChildren<Animator>();
        }
        else
        {
            PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
            if (pm != null)
            {
                _playerMovement = pm;
                _playerAnimator = pm.GetComponentInChildren<Animator>();
                PlayerController controller = pm.GetComponent<PlayerController>();
                if (controller != null) _stats = controller.stats;
            }
        }
    }

    private void LoadWeaponPrefabs()
    {
#if UNITY_EDITOR
        _daggerPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Low Poly Medieval Weapons (Melee + Ranged)/Prefabs/Dagger.prefab");
        _swordPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_DLNK/Wideblade Sword/[PREFABS]/Wideblade_Sword_Bronze.prefab");
        _bowPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Low Poly Medieval Weapons (Melee + Ranged)/Prefabs/Bow.prefab");
#endif
    }

    private void InitializeRecipes()
    {
        // 1. Tab Luyện Kim
        _refineRecipes.Add(new Recipe
        {
            name = "Luyện Thỏi Đồng Thau",
            desc = "Luyện quặng Đồng và Thiếc thô thành hợp kim Đồng Thau Đông Sơn cứng cáp.",
            resultText = "+1 Thỏi Đồng Thau",
            copperCost = 5,
            tinCost = 2,
            ingotCost = 0,
            shellCost = 0,
            stoneCost = 0,
            onCraftSuccess = () =>
            {
                _stats.copperCount -= 5;
                _stats.tinCount -= 2;
                _stats.bronzeIngot += 1;
                DamagePopup.Create(_playerAnimator.transform.position + Vector3.up * 1.8f, "+1 Thỏi Đồng Thau!", new Color(1f, 0.6f, 0f));
            }
        });

        // 2. Tab Rèn Vũ Khí
        _weaponRecipes.Add(new Recipe
        {
            name = "Dao Găm Đồng (T1)",
            desc = "Dao găm nhỏ gọn, tốc đánh nhanh. Thích hợp cho tân thủ.",
            resultText = "Trang bị Dao Găm Đồng",
            copperCost = 0,
            tinCost = 0,
            ingotCost = 2,
            shellCost = 0,
            stoneCost = 0,
            onCraftSuccess = () =>
            {
                _stats.bronzeIngot -= 2;
                if (_playerMovement != null && _daggerPrefab != null)
                {
                    _playerMovement.EquipWeapon(_daggerPrefab, new Vector3(0.05f, 0.05f, 0.05f), new Vector3(0f, 90f, 0f), new Vector3(1.2f, 1.2f, 1.2f));
                }
                DamagePopup.Create(_playerAnimator.transform.position + Vector3.up * 1.8f, "Đã trang bị Dao Găm!", Color.yellow);
            }
        });

        _weaponRecipes.Add(new Recipe
        {
            name = "Đại Đao Cổ Loa (T2)",
            desc = "Đại đao đúc bằng đồng thau nguyên khối. Tăng tầm đánh và sát thương.",
            resultText = "Trang bị Đại Đao Cổ Loa",
            copperCost = 0,
            tinCost = 0,
            ingotCost = 5,
            shellCost = 0,
            stoneCost = 0,
            onCraftSuccess = () =>
            {
                _stats.bronzeIngot -= 5;
                if (_playerMovement != null && _swordPrefab != null)
                {
                    _playerMovement.EquipWeapon(_swordPrefab, new Vector3(0.07f, 0.15f, 0.05f), new Vector3(0f, -90f, 0f), Vector3.one);
                }
                DamagePopup.Create(_playerAnimator.transform.position + Vector3.up * 1.8f, "Đã trang bị Đại Đao!", Color.yellow);
            }
        });

        _weaponRecipes.Add(new Recipe
        {
            name = "Nỏ Thần Loa Thành (T3)",
            desc = "Nỏ thần cơ quan chế tạo theo bản vẽ cổ xưa. Có thể triệu hồi thần tiễn bắn xa.",
            resultText = "Trang bị Nỏ Thần Loa Thành",
            copperCost = 0,
            tinCost = 0,
            ingotCost = 10,
            shellCost = 2,
            stoneCost = 0,
            onCraftSuccess = () =>
            {
                _stats.bronzeIngot -= 10;
                _stats.turtleShell -= 2;
                if (_playerMovement != null && _bowPrefab != null)
                {
                    _playerMovement.EquipWeapon(_bowPrefab, new Vector3(0.1f, 0.1f, 0f), new Vector3(90f, 0f, 0f), new Vector3(1.5f, 1.5f, 1.5f));
                }
                DamagePopup.Create(_playerAnimator.transform.position + Vector3.up * 1.8f, "Đã trang bị Nỏ Thần!", Color.yellow);
            }
        });

        // 3. Tab Rèn Giáp Trụ (Nâng Cấp Tier)
        _armorRecipes.Add(new Recipe
        {
            name = "Giáp Đồng Đông Sơn (T2)",
            desc = "Nâng cấp lên Giáp Đồng. Tăng vĩnh viễn HP tối đa và sức chống chịu.",
            resultText = "Nâng cấp Giáp Tier 2",
            copperCost = 0,
            tinCost = 0,
            ingotCost = 5,
            shellCost = 0,
            stoneCost = 0,
            onCraftSuccess = () => PerformArmorUpgrade(2, 5, 0, 0)
        });

        _armorRecipes.Add(new Recipe
        {
            name = "Giáp Mai Linh Quy (T3)",
            desc = "Giáp kết hợp từ thỏi đồng thau và mai rùa thần. Tăng mạnh HP và Phòng thủ.",
            resultText = "Nâng cấp Giáp Tier 3",
            copperCost = 0,
            tinCost = 0,
            ingotCost = 10,
            shellCost = 2,
            stoneCost = 0,
            onCraftSuccess = () => PerformArmorUpgrade(3, 10, 2, 0)
        });

        _armorRecipes.Add(new Recipe
        {
            name = "Đế Giáp Thần Vương (T4)",
            desc = "Giáp tối thượng được chúc phúc bởi thần khí và Đá Linh Lực cổ xưa.",
            resultText = "Nâng cấp Giáp Tier 4",
            copperCost = 0,
            tinCost = 0,
            ingotCost = 20,
            shellCost = 5,
            stoneCost = 1,
            onCraftSuccess = () => PerformArmorUpgrade(4, 20, 5, 1)
        });
    }

    private void PerformArmorUpgrade(int targetTier, int ingotCost, int shellCost, int stoneCost)
    {
        _stats.bronzeIngot -= ingotCost;
        _stats.turtleShell -= shellCost;
        _stats.spiritualStone -= stoneCost;

        UpgradeSystem us = UpgradeSystem.Instance;
        if (us != null)
        {
            us.currentTier = targetTier;
            _stats.ApplyTierBonuses(targetTier);

            if (us.skinManager != null)
            {
                us.skinManager.currentTier = targetTier;
                us.skinManager.ApplySkinByTier(targetTier);
            }

            ParticleManager.Instance?.PlayUpgradeEffect(_playerAnimator.transform, targetTier);
            ParticleManager.Instance?.PlayConsecrationEffect(_playerAnimator.transform);

            if (GameManager.Instance != null && targetTier > GameManager.Instance.currentBossTier)
            {
                GameManager.Instance.AdvanceTier();
            }

            DamagePopup.Create(_playerAnimator.transform.position + Vector3.up * 2f, $"Đã rèn: Giáp Tier {targetTier}!", Color.yellow);
        }
    }

    public void ToggleUI()
    {
        if (_isWorking) return;

        _isOpen = !_isOpen;
        gameObject.SetActive(_isOpen);

        if (_isOpen)
        {
            SetupReferences();
            SwitchTab(0); // Mặc định mở tab Luyện Kim
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void CloseUI()
    {
        if (_isWorking) return;
        _isOpen = false;
        gameObject.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Thiết lập giao diện bằng Code (Đẹp mắt, gọn gàng, chống tràn cho mọi tỷ lệ màn hình)
    private void SetupUI()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        // Bảng điều khiển trung tâm (Kích thước nhỏ gọn 540x330 để hiển thị hoàn hảo)
        _mainPanel = new GameObject("MainPanel", typeof(RectTransform));
        _mainPanel.transform.SetParent(transform, false);

        RectTransform mainRect = _mainPanel.GetComponent<RectTransform>();
        mainRect.anchorMin = new Vector2(0.5f, 0.5f);
        mainRect.anchorMax = new Vector2(0.5f, 0.5f);
        mainRect.pivot = new Vector2(0.5f, 0.5f);
        mainRect.sizeDelta = new Vector2(540f, 330f);

        Image bg = _mainPanel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.1f, 0.96f);

        Outline outline = _mainPanel.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.84f, 0f, 0.8f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        // 1. Tiêu đề
        GameObject titleObj = new GameObject("Title", typeof(RectTransform));
        titleObj.transform.SetParent(_mainPanel.transform, false);
        _titleText = titleObj.AddComponent<TextMeshProUGUI>();
        _titleText.text = "⚒️ LÒ RÈN CỔ LOA THÀNH ⚒️";
        _titleText.fontSize = 18f;
        _titleText.fontStyle = FontStyles.Bold;
        _titleText.color = new Color(1f, 0.84f, 0f);
        _titleText.alignment = TextAlignmentOptions.Center;

        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -15f);
        titleRect.sizeDelta = new Vector2(0f, 25f);

        // 2. Các Tab chuyển đổi (3 Tab bằng phẳng)
        GameObject tabsContainer = new GameObject("TabsContainer", typeof(RectTransform));
        tabsContainer.transform.SetParent(_mainPanel.transform, false);
        RectTransform tabsRect = tabsContainer.GetComponent<RectTransform>();
        tabsRect.anchorMin = new Vector2(0f, 1f);
        tabsRect.anchorMax = new Vector2(1f, 1f);
        tabsRect.pivot = new Vector2(0.5f, 1f);
        tabsRect.anchoredPosition = new Vector2(0f, -45f);
        tabsRect.sizeDelta = new Vector2(-30f, 30f);

        _btnTabRefine = CreateTabButton(tabsContainer, "TabRefine", "LUYỆN KIM", 0f, 0.32f);
        _btnTabWeapon = CreateTabButton(tabsContainer, "TabWeapon", "RÈN VŨ KHÍ", 0.34f, 0.66f);
        _btnTabArmor = CreateTabButton(tabsContainer, "TabArmor", "RÈN GIÁP TRỤ", 0.68f, 1f);

        _btnTabRefine.onClick.AddListener(() => SwitchTab(0));
        _btnTabWeapon.onClick.AddListener(() => SwitchTab(1));
        _btnTabArmor.onClick.AddListener(() => SwitchTab(2));

        // 3. Khung chứa 2 cột chính
        GameObject bodyContainer = new GameObject("BodyContainer", typeof(RectTransform));
        bodyContainer.transform.SetParent(_mainPanel.transform, false);
        RectTransform bodyRect = bodyContainer.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.pivot = new Vector2(0.5f, 0.5f);
        bodyRect.anchoredPosition = new Vector2(0f, -25f);
        bodyRect.sizeDelta = new Vector2(-30f, -110f);

        // CỘT TRÁI: Danh sách các công thức
        _recipeListContainer = new GameObject("LeftColumn", typeof(RectTransform));
        _recipeListContainer.transform.SetParent(bodyContainer.transform, false);
        RectTransform leftRect = _recipeListContainer.GetComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0f, 0f);
        leftRect.anchorMax = new Vector2(0.35f, 1f);
        leftRect.pivot = new Vector2(0f, 0.5f);
        leftRect.anchoredPosition = Vector2.zero;
        leftRect.sizeDelta = Vector2.zero;

        VerticalLayoutGroup leftLayout = _recipeListContainer.AddComponent<VerticalLayoutGroup>();
        leftLayout.spacing = 10f; // Tăng khoảng cách để nút rèn thoáng hơn, đẹp mắt
        leftLayout.padding = new RectOffset(4, 4, 8, 8); // Thêm lề đệm xung quanh danh sách
        leftLayout.childControlHeight = false; // QUAN TRỌNG: Tắt để Unity không ép chiều cao nút bằng 0
        leftLayout.childForceExpandHeight = false;

        // CỘT PHẢI: Chi tiết công thức chế tạo
        _recipeDetailContainer = new GameObject("RightColumn", typeof(RectTransform));
        _recipeDetailContainer.transform.SetParent(bodyContainer.transform, false);
        RectTransform rightRect = _recipeDetailContainer.GetComponent<RectTransform>();
        rightRect.anchorMin = new Vector2(0.38f, 0f);
        rightRect.anchorMax = new Vector2(1f, 1f);
        rightRect.pivot = new Vector2(1f, 0.5f);
        rightRect.anchoredPosition = Vector2.zero;
        rightRect.sizeDelta = Vector2.zero;

        // Nội dung tiêu đề chi tiết (Cột phải)
        GameObject dTitleObj = new GameObject("DetailTitle", typeof(RectTransform));
        dTitleObj.transform.SetParent(_recipeDetailContainer.transform, false);
        _detailTitleText = dTitleObj.AddComponent<TextMeshProUGUI>();
        _detailTitleText.fontSize = 15f;
        _detailTitleText.fontStyle = FontStyles.Bold;
        _detailTitleText.color = new Color(1f, 0.84f, 0f);

        RectTransform dtRect = dTitleObj.GetComponent<RectTransform>();
        dtRect.anchorMin = new Vector2(0f, 1f);
        dtRect.anchorMax = new Vector2(1f, 1f);
        dtRect.pivot = new Vector2(0.5f, 1f);
        dtRect.anchoredPosition = new Vector2(0f, 0f);
        dtRect.sizeDelta = new Vector2(0f, 22f);

        // Mô tả chi tiết vật phẩm chế tạo
        GameObject dDescObj = new GameObject("DetailDesc", typeof(RectTransform));
        dDescObj.transform.SetParent(_recipeDetailContainer.transform, false);
        _detailDescText = dDescObj.AddComponent<TextMeshProUGUI>();
        _detailDescText.fontSize = 11.5f;
        _detailDescText.color = new Color(0.8f, 0.8f, 0.85f);
        _detailDescText.lineSpacing = 3f;

        RectTransform ddRect = dDescObj.GetComponent<RectTransform>();
        ddRect.anchorMin = new Vector2(0f, 1f);
        ddRect.anchorMax = new Vector2(1f, 1f);
        ddRect.pivot = new Vector2(0.5f, 1f);
        ddRect.anchoredPosition = new Vector2(0f, -25f);
        ddRect.sizeDelta = new Vector2(0f, 40f);

        // Grid Nguyên liệu yêu cầu trực quan
        GameObject ingObj = new GameObject("IngredientsGrid", typeof(RectTransform));
        ingObj.transform.SetParent(_recipeDetailContainer.transform, false);
        _ingredientsText = ingObj.AddComponent<TextMeshProUGUI>();
        _ingredientsText.fontSize = 12f;
        _ingredientsText.lineSpacing = 5f;

        RectTransform ingRect = ingObj.GetComponent<RectTransform>();
        ingRect.anchorMin = new Vector2(0f, 0f);
        ingRect.anchorMax = new Vector2(1f, 1f);
        ingRect.pivot = new Vector2(0.5f, 0.5f);
        ingRect.anchoredPosition = new Vector2(0f, -25f);
        ingRect.sizeDelta = new Vector2(0f, -100f);

        // 4. Thanh tiến trình nung đúc (đặt phía dưới cùng)
        _progressBarContainer = new GameObject("ProgressBar", typeof(RectTransform));
        _progressBarContainer.transform.SetParent(_mainPanel.transform, false);
        RectTransform pbRect = _progressBarContainer.GetComponent<RectTransform>();
        pbRect.anchorMin = new Vector2(0f, 0f);
        pbRect.anchorMax = new Vector2(1f, 0f);
        pbRect.pivot = new Vector2(0.5f, 0f);
        pbRect.anchoredPosition = new Vector2(0f, 65f);
        pbRect.sizeDelta = new Vector2(-30f, 15f);

        Image pbBg = _progressBarContainer.AddComponent<Image>();
        pbBg.color = new Color(0.04f, 0.04f, 0.06f, 0.8f);

        GameObject pbFillObj = new GameObject("Fill", typeof(RectTransform));
        pbFillObj.transform.SetParent(_progressBarContainer.transform, false);
        _progressBarFill = pbFillObj.AddComponent<Image>();
        _progressBarFill.color = new Color(1f, 0.55f, 0f, 0.9f);
        _progressBarFill.type = Image.Type.Filled;
        _progressBarFill.fillMethod = Image.FillMethod.Horizontal;

        RectTransform fillRect = pbFillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = new Vector2(-2f, -2f);

        GameObject pbTxtObj = new GameObject("ProgressText", typeof(RectTransform));
        pbTxtObj.transform.SetParent(_progressBarContainer.transform, false);
        _progressText = pbTxtObj.AddComponent<TextMeshProUGUI>();
        _progressText.text = "Đang rèn...";
        _progressText.fontSize = 9.5f;
        _progressText.fontStyle = FontStyles.Bold;
        _progressText.color = Color.white;
        _progressText.alignment = TextAlignmentOptions.Center;

        RectTransform ptRect = pbTxtObj.GetComponent<RectTransform>();
        ptRect.anchorMin = Vector2.zero;
        ptRect.anchorMax = Vector2.one;
        ptRect.sizeDelta = Vector2.zero;

        _progressBarContainer.SetActive(false);

        // 5. Nút Thao Tác Chế Tạo (Nằm dưới cùng của panel)
        GameObject btnActionObj = new GameObject("BtnAction", typeof(RectTransform));
        btnActionObj.transform.SetParent(_mainPanel.transform, false);
        RectTransform actRect = btnActionObj.GetComponent<RectTransform>();
        actRect.anchorMin = new Vector2(0.5f, 0f);
        actRect.anchorMax = new Vector2(0.5f, 0f);
        actRect.pivot = new Vector2(0.5f, 0f);
        actRect.anchoredPosition = new Vector2(0f, 20f);
        actRect.sizeDelta = new Vector2(240f, 35f);

        Image actBg = btnActionObj.AddComponent<Image>();
        actBg.color = new Color(0.2f, 0.22f, 0.28f, 0.9f);
        Outline actOutline = btnActionObj.AddComponent<Outline>();
        actOutline.effectColor = new Color(1f, 0.84f, 0f, 0.5f);

        GameObject btnTxtObj = new GameObject("Text", typeof(RectTransform));
        btnTxtObj.transform.SetParent(btnActionObj.transform, false);
        _btnActionText = btnTxtObj.AddComponent<TextMeshProUGUI>();
        _btnActionText.text = "BẮT ĐẦU CHẾ TẠO";
        _btnActionText.fontSize = 12f;
        _btnActionText.fontStyle = FontStyles.Bold;
        _btnActionText.color = Color.white;
        _btnActionText.alignment = TextAlignmentOptions.Center;

        RectTransform btRect = btnTxtObj.GetComponent<RectTransform>();
        btRect.anchorMin = Vector2.zero;
        btRect.anchorMax = Vector2.one;
        btRect.sizeDelta = Vector2.zero;

        _btnAction = btnActionObj.AddComponent<Button>();
        ColorBlock cb = _btnAction.colors;
        cb.normalColor = new Color(0.2f, 0.22f, 0.28f, 0.9f);
        cb.highlightedColor = new Color(0.3f, 0.33f, 0.42f, 0.95f);
        cb.pressedColor = new Color(0.12f, 0.13f, 0.18f, 1f);
        _btnAction.colors = cb;
        _btnAction.onClick.AddListener(OnActionButtonClicked);

        // 6. Nút Đóng [X] góc trên bên phải
        GameObject btnCloseObj = new GameObject("BtnClose", typeof(RectTransform));
        btnCloseObj.transform.SetParent(_mainPanel.transform, false);
        RectTransform closeRect = btnCloseObj.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-12f, -12f);
        closeRect.sizeDelta = new Vector2(25f, 25f);

        Image closeBg = btnCloseObj.AddComponent<Image>();
        closeBg.color = new Color(0.6f, 0.15f, 0.15f, 0.8f);

        GameObject closeTxtObj = new GameObject("Text", typeof(RectTransform));
        closeTxtObj.transform.SetParent(btnCloseObj.transform, false);
        TextMeshProUGUI cTxt = closeTxtObj.AddComponent<TextMeshProUGUI>();
        cTxt.text = "X";
        cTxt.fontSize = 11f;
        cTxt.fontStyle = FontStyles.Bold;
        cTxt.color = Color.white;
        cTxt.alignment = TextAlignmentOptions.Center;

        RectTransform ctRect = closeTxtObj.GetComponent<RectTransform>();
        ctRect.anchorMin = Vector2.zero;
        ctRect.anchorMax = Vector2.one;
        ctRect.sizeDelta = Vector2.zero;

        _btnClose = btnCloseObj.AddComponent<Button>();
        ColorBlock ccb = _btnClose.colors;
        ccb.normalColor = new Color(0.6f, 0.15f, 0.15f, 0.8f);
        ccb.highlightedColor = new Color(0.8f, 0.2f, 0.2f, 0.95f);
        _btnClose.colors = ccb;
        _btnClose.onClick.AddListener(CloseUI);
    }

    private Button CreateTabButton(GameObject parent, string name, string label, float anchorMinX, float anchorMaxX)
    {
        GameObject tabObj = new GameObject(name, typeof(RectTransform));
        tabObj.transform.SetParent(parent.transform, false);

        RectTransform rect = tabObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(anchorMinX, 0f);
        rect.anchorMax = new Vector2(anchorMaxX, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        Image img = tabObj.AddComponent<Image>();
        img.color = new Color(0.14f, 0.15f, 0.18f, 0.85f);

        Outline outline = tabObj.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.84f, 0f, 0.3f);

        GameObject txtObj = new GameObject("Text", typeof(RectTransform));
        txtObj.transform.SetParent(tabObj.transform, false);
        TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
        txt.text = label;
        txt.fontSize = 11.5f;
        txt.fontStyle = FontStyles.Bold;
        txt.color = new Color(0.8f, 0.8f, 0.8f);
        txt.alignment = TextAlignmentOptions.Center;

        RectTransform tRect = txtObj.GetComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero;
        tRect.anchorMax = Vector2.one;
        tRect.sizeDelta = Vector2.zero;

        Button button = tabObj.AddComponent<Button>();
        ColorBlock cb = button.colors;
        cb.normalColor = new Color(0.14f, 0.15f, 0.18f, 0.85f);
        cb.highlightedColor = new Color(0.22f, 0.24f, 0.29f, 0.95f);
        button.colors = cb;

        return button;
    }

    private void SwitchTab(int tabIndex)
    {
        if (_isWorking) return;

        _currentTab = tabIndex;
        _selectedRecipeIndex = 0;

        // Cập nhật màu nút Tab để biểu thị trạng thái kích hoạt
        _btnTabRefine.image.color = _currentTab == 0 ? new Color(0.24f, 0.26f, 0.32f, 1f) : new Color(0.12f, 0.13f, 0.16f, 0.85f);
        _btnTabWeapon.image.color = _currentTab == 1 ? new Color(0.24f, 0.26f, 0.32f, 1f) : new Color(0.12f, 0.13f, 0.16f, 0.85f);
        _btnTabArmor.image.color = _currentTab == 2 ? new Color(0.24f, 0.26f, 0.32f, 1f) : new Color(0.12f, 0.13f, 0.16f, 0.85f);

        // Tạo danh sách nút ở cột trái tương ứng với Tab
        RebuildRecipeList();
        UpdateDisplay();
    }

    private void RebuildRecipeList()
    {
        // Xóa các nút cũ
        foreach (var btn in _recipeButtons)
        {
            if (btn != null) Destroy(btn.gameObject);
        }
        _recipeButtons.Clear();

        List<Recipe> activeList = GetActiveRecipes();

        for (int i = 0; i < activeList.Count; i++)
        {
            int index = i;
            GameObject btnObj = new GameObject($"RecipeBtn_{i}", typeof(RectTransform));
            btnObj.transform.SetParent(_recipeListContainer.transform, false);

            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0f, 38f); // Chiều cao tăng lên 38px tạo cảm giác cao cấp

            Image img = btnObj.AddComponent<Image>();
            // Chọn màu xám xanh Slate sang trọng cho nền nút chưa chọn, màu Đồng Thau cho nút được chọn
            img.color = (index == _selectedRecipeIndex) ? new Color(0.35f, 0.28f, 0.15f, 0.95f) : new Color(0.18f, 0.20f, 0.25f, 0.85f);

            Outline outl = btnObj.AddComponent<Outline>();
            outl.effectColor = (index == _selectedRecipeIndex) ? new Color(1f, 0.84f, 0f, 0.9f) : new Color(1f, 1f, 1f, 0.15f);
            outl.effectDistance = new Vector2(1f, -1f);

            GameObject txtObj = new GameObject("Text", typeof(RectTransform));
            txtObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
            txt.text = activeList[i].name;
            txt.fontSize = 12f; // Font size lớn hơn dễ đọc
            txt.fontStyle = FontStyles.Bold;
            txt.color = Color.white;
            txt.alignment = TextAlignmentOptions.MidlineLeft; // Căn lề trái tạo sự cân đối
            txt.margin = new Vector4(12f, 0f, 0f, 0f); // Thụt đầu dòng 12px

            RectTransform tRect = txtObj.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = Vector2.zero;

            Button button = btnObj.AddComponent<Button>();
            button.onClick.AddListener(() => SelectRecipe(index));
            _recipeButtons.Add(button);
        }
    }

    private List<Recipe> GetActiveRecipes()
    {
        if (_currentTab == 0) return _refineRecipes;
        if (_currentTab == 1) return _weaponRecipes;
        return _armorRecipes;
    }

    private void SelectRecipe(int index)
    {
        if (_isWorking) return;

        _selectedRecipeIndex = index;
        
        // Cập nhật lại highlight các nút
        for (int i = 0; i < _recipeButtons.Count; i++)
        {
            if (_recipeButtons[i] != null)
            {
                _recipeButtons[i].image.color = (i == _selectedRecipeIndex) ? new Color(0.35f, 0.28f, 0.15f, 0.95f) : new Color(0.18f, 0.20f, 0.25f, 0.85f);
                Outline outl = _recipeButtons[i].GetComponent<Outline>();
                if (outl != null)
                {
                    outl.effectColor = (i == _selectedRecipeIndex) ? new Color(1f, 0.84f, 0f, 0.9f) : new Color(1f, 1f, 1f, 0.15f);
                }
            }
        }

        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (_stats == null) return;

        List<Recipe> recipes = GetActiveRecipes();
        if (_selectedRecipeIndex >= recipes.Count) return;

        Recipe recipe = recipes[_selectedRecipeIndex];

        // 1. Cột phải: Hiển thị Tiêu đề và Mô tả
        _detailTitleText.text = recipe.name;
        _detailDescText.text = recipe.desc;

        // 2. Cột phải: Hiển thị các nguyên liệu yêu cầu dưới dạng thẻ trực quan (Loại bỏ các emoji bị lỗi [ ])
        string ingredientsStatus = "<b>NGUYÊN LIỆU YÊU CẦU:</b>\n\n";

        if (recipe.copperCost > 0)
        {
            string color = (_stats.copperCount >= recipe.copperCost) ? "#00FF7F" : "#FF6347";
            ingredientsStatus += $"- Quặng Đồng: <color={color}><b>{_stats.copperCount} / {recipe.copperCost}</b></color>\n";
        }
        if (recipe.tinCost > 0)
        {
            string color = (_stats.tinCount >= recipe.tinCost) ? "#00FF7F" : "#FF6347";
            ingredientsStatus += $"- Quặng Thiếc: <color={color}><b>{_stats.tinCount} / {recipe.tinCost}</b></color>\n";
        }
        if (recipe.ingotCost > 0)
        {
            string color = (_stats.bronzeIngot >= recipe.ingotCost) ? "#00FF7F" : "#FF6347";
            ingredientsStatus += $"- Thỏi Đồng Thau: <color={color}><b>{_stats.bronzeIngot} / {recipe.ingotCost}</b></color>\n";
        }
        if (recipe.shellCost > 0)
        {
            string color = (_stats.turtleShell >= recipe.shellCost) ? "#00FF7F" : "#FF6347";
            ingredientsStatus += $"- Mai Linh Quy: <color={color}><b>{_stats.turtleShell} / {recipe.shellCost}</b></color>\n";
        }
        if (recipe.stoneCost > 0)
        {
            string color = (_stats.spiritualStone >= recipe.stoneCost) ? "#00FF7F" : "#FF6347";
            ingredientsStatus += $"- Đá Linh Khí: <color={color}><b>{_stats.spiritualStone} / {recipe.stoneCost}</b></color>\n";
        }

        // Nếu công thức hoàn toàn miễn phí
        if (recipe.copperCost == 0 && recipe.tinCost == 0 && recipe.ingotCost == 0 && recipe.shellCost == 0 && recipe.stoneCost == 0)
        {
            ingredientsStatus += "<color=#00FF7F>Mô hình miễn phí hoặc đã đạt giới hạn.</color>";
        }

        _ingredientsText.text = ingredientsStatus;

        // 3. Cập nhật nút Chế Tạo chính
        if (_currentTab == 2 && (_stats.currentTier >= (_selectedRecipeIndex + 2)))
        {
            // Nếu đã sở hữu Cấp giáp này rồi
            _btnAction.interactable = false;
            _btnActionText.text = "ĐÃ NÂNG CẤP";
            _btnAction.image.color = new Color(0.15f, 0.15f, 0.18f, 0.5f);
        }
        else
        {
            _btnAction.interactable = true;
            _btnActionText.text = _currentTab == 0 ? "TIẾN HÀNH LUYỆN KIM" : "TIẾN HÀNH RÈN ĐÚC";
            _btnAction.image.color = new Color(0.2f, 0.22f, 0.28f, 0.9f);
        }
    }

    private void OnActionButtonClicked()
    {
        if (_isWorking || _stats == null) return;

        List<Recipe> recipes = GetActiveRecipes();
        if (_selectedRecipeIndex >= recipes.Count) return;

        Recipe recipe = recipes[_selectedRecipeIndex];

        // Kiểm tra xem người chơi có đủ tài nguyên hay không
        if (_stats.copperCount < recipe.copperCost ||
            _stats.tinCount < recipe.tinCost ||
            _stats.bronzeIngot < recipe.ingotCost ||
            _stats.turtleShell < recipe.shellCost ||
            _stats.spiritualStone < recipe.stoneCost)
        {
            DamagePopup.Create(_playerAnimator.transform.position + Vector3.up * 1.8f, "Không đủ nguyên liệu!", Color.red);
            AudioManager.Instance?.PlayPickupSound();
            return;
        }

        // Bắt đầu đếm thời gian rèn
        float craftTime = _currentTab == 0 ? 3f : 4f;
        string taskMsg = _currentTab == 0 ? "Đang luyện..." : "Đang rèn...";
        StartCoroutine(CraftingSequence(taskMsg, craftTime, recipe.onCraftSuccess));
    }

    private IEnumerator CraftingSequence(string taskName, float duration, System.Action onComplete)
    {
        _isWorking = true;
        _btnAction.interactable = false;
        _btnClose.interactable = false;
        _progressBarContainer.SetActive(true);
        _progressText.text = taskName;

        float elapsed = 0f;
        float animationTimer = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            animationTimer += Time.deltaTime;

            if (animationTimer >= 0.8f)
            {
                animationTimer = 0f;
                if (_playerAnimator != null)
                {
                    _playerAnimator.SetTrigger("Attack");
                }
                AudioManager.Instance?.PlayPickupSound();
                ParticleManager.Instance?.PlayHealEffect(_playerAnimator.transform);
            }

            _progressBarFill.fillAmount = elapsed / duration;
            yield return null;
        }

        _progressBarFill.fillAmount = 1f;
        yield return new WaitForSeconds(0.15f);

        onComplete?.Invoke();

        _progressBarContainer.SetActive(false);
        _btnAction.interactable = true;
        _btnClose.interactable = true;
        _isWorking = false;

        UpdateDisplay();
        InventoryUI.Instance?.UpdateInventoryDisplay();
    }

    private void Update()
    {
        if (_isOpen)
        {
            // Cưỡng ép mở khóa chuột và hiện chuột mỗi khung hình để đè lên các script Camera/Player khác
            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
            }
            if (!Cursor.visible)
            {
                Cursor.visible = true;
            }
        }
    }
}
