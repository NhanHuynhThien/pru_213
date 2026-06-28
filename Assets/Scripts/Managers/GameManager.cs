using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Menu, Loading, Playing, Paused, GameOver, Victory }
    public GameState CurrentState { get; private set; } = GameState.Menu;

    public static event System.Action<GameState> OnGameStateChanged;
    public event System.Action<GameState> OnGameStateChangedInstance;

    [Header("Scene Names")]
    public string mainMenuScene = "MainMenu";
    public string gameScene = "GameScene";
    public string loadingScene = "Loading";

    [Header("Game Settings")]
    public float gameSpeed = 1f;
    public bool isPaused = false;
    public float gameTime = 0f;
    public int currentBossTier = 1;

    [Header("Singleton")]
    public bool dontDestroyOnLoad = true;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }

    void Update()
    {
        if (CurrentState == GameState.Playing && !isPaused)
        {
            gameTime += Time.deltaTime * gameSpeed;
        }

        if (Input.GetKeyDown(KeyCode.Escape) && CurrentState == GameState.Playing)
        {
            TogglePause();
        }
    }

    public void ChangeState(GameState newState)
    {
        GameState prevState = CurrentState;
        CurrentState = newState;
        isPaused = (newState == GameState.Paused);
        OnGameStateChanged?.Invoke(newState);
        OnGameStateChangedInstance?.Invoke(newState);
        Debug.Log($"[GameManager] State: {prevState} -> {newState}");
    }

    public void LoadGameScene()
    {
        ChangeState(GameState.Loading);
        SceneManager.LoadScene(loadingScene);
        SceneManager.LoadSceneAsync(gameScene, LoadSceneMode.Additive).completed += (op) =>
        {
            Scene loading = SceneManager.GetSceneByName(loadingScene);
            if (loading.isLoaded) SceneManager.UnloadSceneAsync(loading);
            ChangeState(GameState.Playing);
        };
    }

    public void LoadMainMenu()
    {
        ChangeState(GameState.Menu);
        SceneManager.LoadScene(mainMenuScene);
    }

    public void RestartGame()
    {
        gameTime = 0f;
        currentBossTier = 1;
        LoadGameScene();
    }

    public void TogglePause()
    {
        if (CurrentState == GameState.Playing)
        {
            ChangeState(GameState.Paused);
            Time.timeScale = 0f;
        }
        else if (CurrentState == GameState.Paused)
        {
            ChangeState(GameState.Playing);
            Time.timeScale = 1f;
        }
    }

    public void GameOver()
    {
        ChangeState(GameState.GameOver);
        Time.timeScale = 0f;
    }

    public void Victory()
    {
        ChangeState(GameState.Victory);
        Time.timeScale = 0f;
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void AdvanceTier()
    {
        if (currentBossTier < 4)
        {
            currentBossTier++;
            Debug.Log($"[GameManager] Advancing to Tier {currentBossTier}");
        }
    }

    public string FormatTime(float seconds)
    {
        int mins = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return $"{mins:D2}:{secs:D2}";
    }
}
