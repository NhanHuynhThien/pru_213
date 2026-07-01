using UnityEngine;
using UnityEngine.UI;

public class Map2CombatHUD : MonoBehaviour
{
    private PlayerCombat playerCombat;
    private Image hpFill;
    private Text hpText;
    private float displayedFill = 1f;

    private void Start()
    {
        CreateHud();
    }

    private void Update()
    {
        if (playerCombat == null)
        {
            playerCombat = FindFirstObjectByType<PlayerCombat>();
        }

        if (playerCombat == null || playerCombat.stats == null || hpFill == null) return;

        int maxHp = playerCombat.stats.EffectiveMaxHealth;
        int currentHp = Mathf.Clamp(playerCombat.currentHealth, 0, maxHp);
        float targetFill = maxHp > 0 ? (float)currentHp / maxHp : 0f;
        displayedFill = Mathf.MoveTowards(displayedFill, targetFill, Time.deltaTime * 1.8f);
        hpFill.fillAmount = displayedFill;

        if (hpText != null)
        {
            hpText.text = $"HP {currentHp}/{maxHp}";
        }
    }

    private void CreateHud()
    {
        Canvas canvas = null;
        foreach (Canvas existingCanvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (existingCanvas.renderMode != RenderMode.WorldSpace)
            {
                canvas = existingCanvas;
                break;
            }
        }

        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Map2_CombatCanvas", typeof(RectTransform));
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        Transform oldPanel = canvas.transform.Find("Map2_HPPanel");
        if (oldPanel != null)
        {
            Destroy(oldPanel.gameObject);
        }

        GameObject panel = new GameObject("Map2_HPPanel", typeof(RectTransform));
        panel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(24f, -24f);
        panelRect.sizeDelta = new Vector2(260f, 36f);

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.62f);

        GameObject fillObj = new GameObject("HPFill", typeof(RectTransform));
        fillObj.transform.SetParent(panel.transform, false);
        hpFill = fillObj.AddComponent<Image>();
        hpFill.color = new Color(0.1f, 0.8f, 0.22f, 0.95f);
        hpFill.type = Image.Type.Filled;
        hpFill.fillMethod = Image.FillMethod.Horizontal;
        hpFill.fillOrigin = 0;

        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(4f, 4f);
        fillRect.offsetMax = new Vector2(-4f, -4f);

        GameObject textObj = new GameObject("HPText", typeof(RectTransform));
        textObj.transform.SetParent(panel.transform, false);
        hpText = textObj.AddComponent<Text>();
        hpText.text = "HP";
        hpText.alignment = TextAnchor.MiddleCenter;
        hpText.color = Color.white;
        hpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (hpText.font == null)
        {
            hpText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        hpText.fontSize = 16;
        hpText.fontStyle = FontStyle.Bold;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }
}
