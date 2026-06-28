using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [Header("Scene Names")]
    public string mainMenuScene = "MainMenu";
    public string gameScene = "GameScene";
    public string loadingScene = "Loading";

    [Header("Loading Screen")]
    public bool showLoadingScreen = true;
    public UnityEngine.UI.Text loadingText;
    public UnityEngine.UI.Image loadingBar;

    private AsyncOperation currentOperation;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LoadScene(string sceneName)
    {
        if (showLoadingScreen && !string.IsNullOrEmpty(loadingScene))
        {
            StartCoroutine(LoadSceneWithLoading(sceneName));
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    System.Collections.IEnumerator LoadSceneWithLoading(string sceneName)
    {
        if (!string.IsNullOrEmpty(loadingScene))
        {
            currentOperation = SceneManager.LoadSceneAsync(loadingScene);
            yield return new WaitUntil(() => currentOperation.isDone);
        }

        if (loadingBar != null) loadingBar.fillAmount = 0f;
        if (loadingText != null) loadingText.text = "Dang tai...";

        AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName);
        loadOp.allowSceneActivation = false;

        float progress = 0f;
        while (!loadOp.isDone)
        {
            progress = Mathf.Clamp01(loadOp.progress / 0.9f);
            if (loadingBar != null) loadingBar.fillAmount = progress;
            if (loadingText != null) loadingText.text = $"Dang tai... {Mathf.RoundToInt(progress * 100)}%";

            if (progress >= 1f)
            {
                loadOp.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    public void LoadMainMenu()
    {
        LoadScene(mainMenuScene);
    }

    public void LoadGame()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadGameScene();
        }
        else
        {
            LoadScene(gameScene);
        }
    }
}
