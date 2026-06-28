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

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
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
        HideAllPanels();

        switch (state)
        {
            case GameManager.GameState.Menu:
                if (menuPanel != null) menuPanel.SetActive(true);
                break;
            case GameManager.GameState.Playing:
                if (HUDPanel != null) HUDPanel.SetActive(true);
                break;
            case GameManager.GameState.Paused:
                if (pausePanel != null) pausePanel.SetActive(true);
                break;
            case GameManager.GameState.GameOver:
                if (gameOverPanel != null) gameOverPanel.SetActive(true);
                break;
            case GameManager.GameState.Victory:
                if (victoryPanel != null) victoryPanel.SetActive(true);
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

        if (timeText != null && GameManager.Instance != null)
            timeText.text = GameManager.Instance.FormatTime(GameManager.Instance.gameTime);
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
}
