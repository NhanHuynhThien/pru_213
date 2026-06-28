using UnityEngine;
using TMPro;
using System.Collections;

public class IntroSequence : MonoBehaviour
{
    [Header("Story Text")]
    [TextArea(3, 6)]
    public string[] storyLines = new string[]
    {
        "Nam 257TCN, Thuc Phan An Duong Vuong dat nen mong xay Loa Thanh.",
        "Nhung the luc ta ac tu long dat — U Minh Toc — troi day.",
        "Thanh xay xong lai do, vu khi ren xong lai gay.",
        "Ban la Cao Thuc, truyen nhan cua dong ho Cao, mang trong minh \"Ky Chi Nhan\".",
        "Tim thay Thai Co Thiet Bang — di vat cua Than Kim Quy.",
        "Hay buoc vao hanh trinh thanh hoa cua minh..."
    };

    [Header("UI References")]
    public TextMeshProUGUI storyText;
    public CanvasGroup fadeCanvas;
    public GameObject skipPrompt;

    [Header("Timing")]
    public float lineDuration = 4f;
    public float fadeInDuration = 0.5f;
    public float fadeOutDuration = 1f;

    private int currentLine = 0;
    private bool isSkipping = false;
    private bool isTyping = false;

    void Start()
    {
        if (storyText != null)
        {
            storyText.text = "";
        }

        if (fadeCanvas != null)
        {
            fadeCanvas.alpha = 0f;
        }

        StartCoroutine(PlayIntro());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            if (isTyping)
            {
                SkipTyping();
            }
            else
            {
                NextLine();
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SkipToGame();
        }
    }

    IEnumerator PlayIntro()
    {
        yield return new WaitForSeconds(0.5f);

        while (currentLine < storyLines.Length)
        {
            yield return StartCoroutine(ShowLine(storyLines[currentLine]));
            yield return new WaitForSeconds(lineDuration);
            yield return StartCoroutine(FadeOutLine());

            currentLine++;
        }

        yield return new WaitForSeconds(1f);
        SkipToGame();
    }

    IEnumerator ShowLine(string line)
    {
        isTyping = true;
        string displayText = "";
        int charIndex = 0;

        while (charIndex < line.Length)
        {
            displayText += line[charIndex];
            storyText.text = displayText;
            charIndex++;
            yield return new WaitForSeconds(0.03f);
        }

        isTyping = false;
    }

    void SkipTyping()
    {
        StopAllCoroutines();
        if (storyText != null && currentLine < storyLines.Length)
        {
            storyText.text = storyLines[currentLine];
        }
        isTyping = false;
    }

    void NextLine()
    {
        StopAllCoroutines();
        if (currentLine < storyLines.Length - 1)
        {
            currentLine++;
            StartCoroutine(ShowLine(storyLines[currentLine]));
        }
        else
        {
            SkipToGame();
        }
    }

    IEnumerator FadeOutLine()
    {
        if (fadeCanvas != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                fadeCanvas.alpha = 1f - (elapsed / fadeOutDuration);
                yield return null;
            }
            fadeCanvas.alpha = 0f;
        }
        else
        {
            yield return new WaitForSeconds(fadeOutDuration);
        }
    }

    void SkipToGame()
    {
        if (isSkipping) return;
        isSkipping = true;

        StopAllCoroutines();
        StartCoroutine(SkipSequence());
    }

    IEnumerator SkipSequence()
    {
        if (fadeCanvas != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                fadeCanvas.alpha = elapsed / fadeOutDuration;
                yield return null;
            }
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadGameScene();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
        }
    }
}
