using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Canvas References")]
    public Canvas mainCanvas;
    public GameObject HUDPanel;
    public GameObject menuPanel;
    public GameObject pausePanel;
    public GameObject gameOverPanel;
    public GameObject victoryPanel;
    public GameObject upgradePanel;
    public GameObject inventoryPanel;

    [Header("HUD Elements")]
    public Image healthBar;
    public Image staminaBar;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI staminaText;
    public TextMeshProUGUI copperText;
    public TextMeshProUGUI tierText;
    public TextMeshProUGUI waveText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI enemyCountText;
    public Image bossHealthBar;
    public TextMeshProUGUI bossNameText;
    public GameObject bossHealthPanel;

    [Header("Upgrade Panel")]
    public TextMeshProUGUI upgradeStepText;
    public Image upgradeProgressBar;
    public TextMeshProUGUI resourceRequiredText;

    [Header("Player Reference")]
    public PlayerCombat playerCombat;
    public PlayerStats playerStats;
    public EnemySpawner enemySpawner;

    [Header("Tier Display")]
    public Image tierIcon;
    public TextMeshProUGUI tierNameText;

    [Header("Character Stats Panel")]
    public GameObject characterStatsPanel;
    public TextMeshProUGUI statsLevelText;
    public TextMeshProUGUI statsDamageText;
    public TextMeshProUGUI statsDefenseText;
    public TextMeshProUGUI statsHpText;
    public TextMeshProUGUI statsStaminaText;

    [Header("Pause & Settings Panel")]
    public GameObject gearButton;
    public Button tabSettingsBtn;
    public Button tabControlsBtn;
    public GameObject tabContentSettings;
    public GameObject tabContentControls;
    public UnityEngine.UI.Slider musicVolumeSlider;
    public UnityEngine.UI.Slider sfxVolumeSlider;
    public Button resumeButton;
    public Button mainMenuButton;
    public Button quitButton;

    void Awake()
    {
        EnsureManagersAreActive();

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void EnsureManagersAreActive()
    {
        // 1. Tìm GameManager trong các root GameObject (kể cả bị ẩn)
        GameObject gmGo = FindRootGameObject("GameManager");
        if (gmGo != null)
        {
            if (!gmGo.activeSelf)
            {
                gmGo.SetActive(true);
                Debug.Log("<color=green>[UIManager]</color> Đã tự động kích hoạt GameManager bị ẩn!");
            }
            GameManager gm = gmGo.GetComponent<GameManager>();
            if (gm == null)
            {
                gm = gmGo.AddComponent<GameManager>();
                Debug.Log("<color=green>[UIManager]</color> Tự động gắn thêm component GameManager bị thiếu!");
            }
        }

        // 2. Tìm AudioManager
        GameObject amGo = FindRootGameObject("AudioManager");
        if (amGo != null)
        {
            if (!amGo.activeSelf)
            {
                amGo.SetActive(true);
                Debug.Log("<color=green>[UIManager]</color> Đã tự động kích hoạt AudioManager bị ẩn!");
            }
            AudioManager am = amGo.GetComponent<AudioManager>();
            if (am == null)
            {
                am = amGo.AddComponent<AudioManager>();
                Debug.Log("<color=green>[UIManager]</color> Tự động gắn thêm component AudioManager bị thiếu!");
            }
        }

        // 3. Tìm ParticleManager
        GameObject pmGo = FindRootGameObject("ParticleManager");
        if (pmGo != null)
        {
            if (!pmGo.activeSelf)
            {
                pmGo.SetActive(true);
                Debug.Log("<color=green>[UIManager]</color> Đã tự động kích hoạt ParticleManager bị ẩn!");
            }
            ParticleManager pm = pmGo.GetComponent<ParticleManager>();
            if (pm == null)
            {
                pm = pmGo.AddComponent<ParticleManager>();
                Debug.Log("<color=green>[UIManager]</color> Tự động gắn thêm component ParticleManager bị thiếu!");
            }
        }
    }

    GameObject FindRootGameObject(string name)
    {
        UnityEngine.SceneManagement.Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach (GameObject rootObj in activeScene.GetRootGameObjects())
        {
            if (rootObj.name == name) return rootObj;
        }
        
        // Tìm trong toàn bộ scene kể cả đối tượng ẩn
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject go in allObjects)
        {
            if (go.name == name && go.scene.isLoaded)
            {
                return go;
            }
        }
        return null;
    }

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.OnGameStateChanged += HandleGameStateChanged;
        }

        HideAllPanels();
        if (HUDPanel != null) HUDPanel.SetActive(true);

        FindPlayerReferences();
        AutoFindUIReferences();
        EnsureBossHealthUIExists();
        BindStatsPanelEvents();
        BindPauseMenuEvents();
    }

    void BindPauseMenuEvents()
    {
        if (gearButton != null)
        {
            Button btn = gearButton.GetComponent<Button>();
            if (btn == null) btn = gearButton.AddComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => {
                Debug.Log($"[UIManager] Clicked MENU Button! GameManager.Instance != null: {GameManager.Instance != null}");
                if (GameManager.Instance != null)
                {
                    Debug.Log($"[UIManager] Current GameState before toggle: {GameManager.Instance.CurrentState}");
                    GameManager.Instance.TogglePause();
                }
            });
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(ResumeGame);
        }

        if (pausePanel != null)
        {
            Transform closeBtnTrans = pausePanel.transform.Find("PauseMenu_Border/PauseMenu_Bg/Close_Button");
            if (closeBtnTrans != null)
            {
                Button closeBtn = closeBtnTrans.GetComponent<Button>();
                if (closeBtn != null)
                {
                    closeBtn.onClick.RemoveAllListeners();
                    closeBtn.onClick.AddListener(ResumeGame);
                }
            }
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(MainMenu);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(QuitGame);
        }

        if (tabSettingsBtn != null)
        {
            tabSettingsBtn.onClick.RemoveAllListeners();
            tabSettingsBtn.onClick.AddListener(ShowSettingsTab);
        }

        if (tabControlsBtn != null)
        {
            tabControlsBtn.onClick.RemoveAllListeners();
            tabControlsBtn.onClick.AddListener(ShowControlsTab);
        }

        if (musicVolumeSlider != null && AudioManager.Instance != null)
        {
            musicVolumeSlider.value = AudioManager.Instance.musicVolume;
            musicVolumeSlider.onValueChanged.RemoveAllListeners();
            musicVolumeSlider.onValueChanged.AddListener((val) => {
                AudioManager.Instance.SetMusicVolume(val);
            });
        }

        if (sfxVolumeSlider != null && AudioManager.Instance != null)
        {
            sfxVolumeSlider.value = AudioManager.Instance.sfxVolume;
            sfxVolumeSlider.onValueChanged.RemoveAllListeners();
            sfxVolumeSlider.onValueChanged.AddListener((val) => {
                AudioManager.Instance.SetSFXVolume(val);
            });
        }
    }

    public void ShowSettingsTab()
    {
        if (tabContentSettings != null) tabContentSettings.SetActive(true);
        if (tabContentControls != null) tabContentControls.SetActive(false);
    }

    public void ShowControlsTab()
    {
        if (tabContentSettings != null) tabContentSettings.SetActive(false);
        if (tabContentControls != null) tabContentControls.SetActive(true);
    }

    void BindStatsPanelEvents()
    {
        if (characterStatsPanel != null)
        {
            Button closeBtn = characterStatsPanel.GetComponentInChildren<Button>();
            if (closeBtn != null)
            {
                closeBtn.onClick.RemoveAllListeners();
                closeBtn.onClick.AddListener(ToggleStatsPanel);
            }
        }

        if (HUDPanel != null)
        {
            Transform avatarTrans = HUDPanel.transform.Find("Avatar_Mask");
            if (avatarTrans != null)
            {
                Button avatarBtn = avatarTrans.GetComponent<Button>();
                if (avatarBtn == null) avatarBtn = avatarTrans.gameObject.AddComponent<Button>();
                avatarBtn.onClick.RemoveAllListeners();
                avatarBtn.onClick.AddListener(ToggleStatsPanel);
            }
        }
    }

    void Update()
    {
        UpdateHUD();
    }

    void FindPlayerReferences()
    {
        if (playerCombat == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerCombat = player.GetComponent<PlayerCombat>();
                PlayerController pc = player.GetComponent<PlayerController>();
                if (pc != null) playerStats = pc.stats;
            }
        }

        if (enemySpawner == null)
        {
            enemySpawner = FindObjectOfType<EnemySpawner>();
        }
    }

    void HandleGameStateChanged(GameManager.GameState state)
    {
        Debug.Log($"[UIManager] HandleGameStateChanged - Trạng thái mới: {state}. Bảng PausePanel bị null: {pausePanel == null}");
        HideAllPanels();

        switch (state)
        {
            case GameManager.GameState.Menu:
                if (menuPanel != null) menuPanel.SetActive(true);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;
            case GameManager.GameState.Playing:
                if (HUDPanel != null) HUDPanel.SetActive(true);
                // Khóa chuột khi quay lại chơi
                bool otherUIOpen = (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen) ||
                                   (BlacksmithUI.Instance != null && BlacksmithUI.Instance.IsOpen) ||
                                   (characterStatsPanel != null && characterStatsPanel.activeSelf);
                if (!otherUIOpen)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                break;
            case GameManager.GameState.Paused:
                if (pausePanel != null) pausePanel.SetActive(true);
                // Mở khóa chuột để tương tác với menu cài đặt
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;
            case GameManager.GameState.GameOver:
                if (gameOverPanel != null) gameOverPanel.SetActive(true);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;
            case GameManager.GameState.Victory:
                if (victoryPanel != null) victoryPanel.SetActive(true);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;
        }
    }

    void UpdateHUD()
    {
        if (playerCombat != null && playerStats != null)
        {
            float healthPercent = playerCombat.GetHealthPercent();
            if (healthBar != null) healthBar.fillAmount = Mathf.Lerp(healthBar.fillAmount, healthPercent, 5f * Time.deltaTime);
            if (healthText != null) healthText.text = $"{Mathf.CeilToInt(playerCombat.currentHealth)} / {playerStats.EffectiveMaxHealth}";

            if (staminaBar != null)
            {
                float staminaPercent = playerStats.stamina / playerStats.maxStamina;
                staminaBar.fillAmount = Mathf.Lerp(staminaBar.fillAmount, staminaPercent, 10f * Time.deltaTime);
            }
            if (staminaText != null) staminaText.text = $"{Mathf.CeilToInt(playerStats.stamina)} / {playerStats.maxStamina}";

            if (copperText != null) copperText.text = $"Đồng: {playerStats.copperCount}";
        }

        if (tierText != null && playerStats != null)
            tierText.text = $"Tier {playerStats.currentTier} - {playerStats.tierName}";

        if (tierNameText != null && playerStats != null)
            tierNameText.text = playerStats.tierName;

        if (enemySpawner != null)
        {
            if (waveText != null) waveText.text = $"Wave {enemySpawner.CurrentWave}";
            if (enemyCountText != null) enemyCountText.text = $"Kills: {enemySpawner.TotalKills}";
        }


        if (characterStatsPanel != null && characterStatsPanel.activeSelf)
        {
            UpdateStatsPanel();
        }
    }

    public void ToggleStatsPanel()
    {
        if (characterStatsPanel != null)
        {
            bool isActive = !characterStatsPanel.activeSelf;
            characterStatsPanel.SetActive(isActive);
            Debug.Log($"[UIManager] ToggleStatsPanel - Trạng thái bảng: {isActive}");
            
            if (isActive)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                UpdateStatsPanel();
            }
            else
            {
                // Chỉ khóa chuột nếu các UI chính khác cũng đang đóng
                bool otherUIOpen = (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen) ||
                                   (BlacksmithUI.Instance != null && BlacksmithUI.Instance.IsOpen) ||
                                   (pausePanel != null && pausePanel.activeSelf) ||
                                   (upgradePanel != null && upgradePanel.activeSelf);
                                   
                if (!otherUIOpen)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }
        else
        {
            Debug.LogWarning("[UIManager] ToggleStatsPanel - characterStatsPanel đang là null!");
        }
    }

    public void UpdateStatsPanel()
    {
        if (playerStats != null)
        {
            if (statsLevelText != null) statsLevelText.text = $"Cấp: {playerStats.currentTier} ({playerStats.tierName})";
            if (statsDamageText != null) statsDamageText.text = $"Sát thương: {playerStats.EffectiveAttackDamage}";
            if (statsDefenseText != null) statsDefenseText.text = $"Phòng thủ: {playerStats.EffectiveDefense}";
            if (statsHpText != null)
            {
                float curHp = playerCombat != null ? playerCombat.currentHealth : playerStats.currentHealth;
                statsHpText.text = $"Máu: {Mathf.CeilToInt(curHp)} / {playerStats.EffectiveMaxHealth}";
            }
            if (statsStaminaText != null) statsStaminaText.text = $"Thể lực: {Mathf.CeilToInt(playerStats.stamina)} / {playerStats.maxStamina}";
        }
    }

    public void ShowUpgradePanel(string stepInfo, float progress)
    {
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(true);
            if (upgradeStepText != null) upgradeStepText.text = stepInfo;
            if (upgradeProgressBar != null) upgradeProgressBar.fillAmount = progress;
        }
    }

    public void HideUpgradePanel()
    {
        if (upgradePanel != null) upgradePanel.SetActive(false);
    }

    public void UpdateUpgradeProgress(float progress)
    {
        if (upgradeProgressBar != null)
            upgradeProgressBar.fillAmount = Mathf.Lerp(upgradeProgressBar.fillAmount, progress, 10f * Time.deltaTime);
    }

    public void ShowBossHealth(string bossName, float healthPercent)
    {
        if (bossHealthPanel != null)
        {
            bossHealthPanel.SetActive(true);
            if (bossNameText != null) bossNameText.text = bossName;
            if (bossHealthBar != null) bossHealthBar.fillAmount = healthPercent;
        }
    }

    public void HideBossHealth()
    {
        if (bossHealthPanel != null) bossHealthPanel.SetActive(false);
    }

    public void UpdateBossHealth(float healthPercent)
    {
        if (bossHealthBar != null)
            bossHealthBar.fillAmount = Mathf.Lerp(bossHealthBar.fillAmount, healthPercent, 5f * Time.deltaTime);
    }

    void HideAllPanels()
    {
        if (HUDPanel != null) HUDPanel.SetActive(false);
        if (menuPanel != null) menuPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (upgradePanel != null) upgradePanel.SetActive(false);
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
    }

    void OnDestroy()
    {
        GameManager.OnGameStateChanged -= HandleGameStateChanged;
    }

    public void ResumeGame()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.TogglePause();
    }

    public void RestartGame()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RestartGame();
    }

    public void MainMenu()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.LoadMainMenu();
    }

    public void QuitGame()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.QuitGame();
    }

    void AutoFindUIReferences()
    {
        if (mainCanvas == null)
        {
            mainCanvas = GetComponent<Canvas>();
            if (mainCanvas == null) mainCanvas = FindAnyObjectByType<Canvas>();
        }

        if (mainCanvas != null)
        {
            if (HUDPanel == null) HUDPanel = FindChildByName(mainCanvas.gameObject, "HUDPanel");
            if (pausePanel == null) pausePanel = FindChildByName(mainCanvas.gameObject, "PausePanel");
            if (gameOverPanel == null) gameOverPanel = FindChildByName(mainCanvas.gameObject, "GameOverPanel");
            if (victoryPanel == null) victoryPanel = FindChildByName(mainCanvas.gameObject, "VictoryPanel");
            if (upgradePanel == null) upgradePanel = FindChildByName(mainCanvas.gameObject, "UpgradePanel");
            if (bossHealthPanel == null) bossHealthPanel = FindChildByName(mainCanvas.gameObject, "BossHealthPanel");

            if (HUDPanel != null)
            {
                if (healthBar == null) healthBar = FindComponentInChild<Image>(HUDPanel, "HealthBar", "Health Bar", "HPBar");
                if (staminaBar == null) staminaBar = FindComponentInChild<Image>(HUDPanel, "StaminaBar", "Stamina Bar", "EnergyBar");
                if (healthText == null) healthText = FindComponentInChild<TextMeshProUGUI>(HUDPanel, "HealthText", "Health Text", "HPText");
                if (staminaText == null) staminaText = FindComponentInChild<TextMeshProUGUI>(HUDPanel, "StaminaText", "Stamina Text", "EnergyText");
                if (copperText == null) copperText = FindComponentInChild<TextMeshProUGUI>(HUDPanel, "CopperText", "Copper Text", "GoldText", "MoneyText");
                if (tierText == null) tierText = FindComponentInChild<TextMeshProUGUI>(HUDPanel, "TierText", "Tier Text", "RankText");
                if (waveText == null) waveText = FindComponentInChild<TextMeshProUGUI>(HUDPanel, "WaveText", "Wave Text");
                if (timeText == null) timeText = FindComponentInChild<TextMeshProUGUI>(HUDPanel, "TimeText", "Time Text");
                if (enemyCountText == null) enemyCountText = FindComponentInChild<TextMeshProUGUI>(HUDPanel, "EnemyCountText", "Enemy Count Text", "KillCountText", "KillsText");
            }
        }
    }

    void EnsureBossHealthUIExists()
    {
        if (bossHealthPanel == null)
        {
            if (mainCanvas == null) mainCanvas = FindAnyObjectByType<Canvas>();
            if (mainCanvas != null)
            {
                bossHealthPanel = FindChildByName(mainCanvas.gameObject, "BossHealthPanel");
                if (bossHealthPanel == null)
                {
                    bossHealthPanel = new GameObject("BossHealthPanel", typeof(RectTransform));
                    bossHealthPanel.transform.SetParent(mainCanvas.transform, false);
                    
                    RectTransform rect = bossHealthPanel.GetComponent<RectTransform>();
                    rect.anchorMin = new Vector2(0.5f, 1f);
                    rect.anchorMax = new Vector2(0.5f, 1f);
                    rect.pivot = new Vector2(0.5f, 1f);
                    rect.anchoredPosition = new Vector2(0f, -50f);
                    rect.sizeDelta = new Vector2(400f, 30f);
                    
                    bossHealthPanel.SetActive(false);
                }
            }
        }

        if (bossHealthPanel == null) return;

        // Check if we need to create the Boss Name Text
        if (bossNameText == null)
        {
            bossNameText = bossHealthPanel.GetComponentInChildren<TextMeshProUGUI>(true);
            if (bossNameText == null)
            {
                GameObject nameObj = new GameObject("BossNameText");
                nameObj.transform.SetParent(bossHealthPanel.transform, false);
                bossNameText = nameObj.AddComponent<TextMeshProUGUI>();
                bossNameText.text = "Boss Name";
                bossNameText.fontSize = 18f;
                bossNameText.alignment = TextAlignmentOptions.Center;
                bossNameText.color = Color.white;
                
                RectTransform rect = nameObj.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, 25f);
                rect.sizeDelta = new Vector2(0f, 30f);
            }
        }

        // Check if we need to create the Boss Health Bar
        if (bossHealthBar == null)
        {
            Image[] images = bossHealthPanel.GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                if (img.gameObject.name.Contains("Bar") || img.gameObject.name.Contains("Fill"))
                {
                    bossHealthBar = img;
                    break;
                }
            }

            if (bossHealthBar == null)
            {
                // Background
                GameObject bgObj = new GameObject("BossHealthBG");
                bgObj.transform.SetParent(bossHealthPanel.transform, false);
                Image bgImage = bgObj.AddComponent<Image>();
                bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
                
                RectTransform bgRect = bgObj.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.sizeDelta = Vector2.zero;

                // Fill Image
                GameObject fillObj = new GameObject("BossHealthBar");
                fillObj.transform.SetParent(bossHealthPanel.transform, false);
                bossHealthBar = fillObj.AddComponent<Image>();
                bossHealthBar.color = new Color(0.85f, 0.15f, 0.15f, 1f);
                bossHealthBar.type = Image.Type.Filled;
                bossHealthBar.fillMethod = Image.FillMethod.Horizontal;
                bossHealthBar.fillOrigin = (int)Image.OriginHorizontal.Left;
                bossHealthBar.fillAmount = 1f;

                RectTransform fillRect = fillObj.GetComponent<RectTransform>();
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = Vector2.one;
                fillRect.sizeDelta = new Vector2(-4f, -4f);
                fillRect.anchoredPosition = Vector2.zero;
            }
        }
    }

    GameObject FindChildByName(GameObject parent, string name)
    {
        Transform[] children = parent.GetComponentsInChildren<Transform>(true);
        foreach (var child in children)
        {
            if (child.gameObject.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
            {
                return child.gameObject;
            }
        }
        return null;
    }

    T FindComponentInChild<T>(GameObject parent, params string[] possibleNames) where T : Component
    {
        T comp = null;
        foreach (var name in possibleNames)
        {
            Transform[] children = parent.GetComponentsInChildren<Transform>(true);
            foreach (var child in children)
            {
                if (child.gameObject.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                {
                    comp = child.GetComponent<T>();
                    if (comp != null) return comp;
                }
            }
        }
        comp = parent.GetComponentInChildren<T>(true);
        return comp;
    }
}
