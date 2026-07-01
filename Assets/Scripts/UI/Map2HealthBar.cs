using UnityEngine;
using UnityEngine.UI;

public class Map2HealthBar : MonoBehaviour
{
    public Vector3 offset = new Vector3(0f, 2.4f, 0f);
    public Vector2 size = new Vector2(1.6f, 0.18f);

    private EnemyController enemy;
    private BossController boss;
    private PlayerCombat playerCombat;
    private Image fillImage;
    private Canvas canvas;
    private float displayedFill = 1f;

    private void Awake()
    {
        enemy = GetComponent<EnemyController>();
        boss = GetComponent<BossController>();
        playerCombat = GetComponent<PlayerCombat>();
    }

    private void Start()
    {
        CreateBar();
    }

    private void LateUpdate()
    {
        if (canvas == null || fillImage == null) return;

        Camera cam = Camera.main;
        if (cam != null)
        {
            canvas.transform.position = transform.position + offset;
            canvas.transform.rotation = Quaternion.LookRotation(canvas.transform.position - cam.transform.position);
        }

        float targetFill = Mathf.Clamp01(GetHealthPercent());
        displayedFill = Mathf.MoveTowards(displayedFill, targetFill, Time.deltaTime * 1.6f);
        fillImage.fillAmount = displayedFill;
    }

    private float GetHealthPercent()
    {
        if (enemy != null && enemy.data != null)
        {
            float maxHealth = Mathf.Max(1f, enemy.data.GetScaledHealth(enemy.currentTier));
            return enemy.currentHealth / maxHealth;
        }

        if (boss != null)
        {
            float maxHealth = Mathf.Max(1f, boss.maxHealth);
            return boss.currentHealth / maxHealth;
        }

        if (playerCombat != null && playerCombat.stats != null)
        {
            float maxHealth = Mathf.Max(1f, playerCombat.stats.EffectiveMaxHealth);
            return playerCombat.currentHealth / maxHealth;
        }

        return 1f;
    }

    private void CreateBar()
    {
        GameObject canvasObj = new GameObject("HealthBarCanvas", typeof(RectTransform));
        canvasObj.transform.SetParent(transform, false);
        canvasObj.transform.localPosition = offset;

        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = size;

        GameObject backgroundObj = new GameObject("Background", typeof(RectTransform));
        backgroundObj.transform.SetParent(canvasObj.transform, false);
        Image backgroundImage = backgroundObj.AddComponent<Image>();
        backgroundImage.color = new Color(0f, 0f, 0f, 0.75f);

        RectTransform backgroundRect = backgroundObj.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        GameObject fillObj = new GameObject("Fill", typeof(RectTransform));
        fillObj.transform.SetParent(backgroundObj.transform, false);
        fillImage = fillObj.AddComponent<Image>();
        fillImage.color = playerCombat != null ? new Color(0.1f, 0.8f, 0.22f, 1f) : new Color(0.8f, 0.08f, 0.08f, 1f);
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = 0;

        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(0.03f, 0.03f);
        fillRect.offsetMax = new Vector2(-0.03f, -0.03f);
    }
}
