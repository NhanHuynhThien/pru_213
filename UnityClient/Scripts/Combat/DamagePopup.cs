using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour, IPooledObject
{
    [Header("Popup Settings")]
    public float lifetime = 1f;
    public float moveSpeed = 2f;
    public float fadeSpeed = 2f;
    public float scaleMultiplier = 1f;
    public bool isCritical = false;

    private TextMeshProUGUI textMesh;
    private Color originalColor;
    private Vector3 velocity;
    private float spawnTime;
    private bool isActive = false;
    private RectTransform rectTransform;
    private Canvas parentCanvas;

    public static DamagePopup Create(Vector3 position, int damage, bool critical = false)
    {
        GameObject popupObj;

        if (ObjectPool.Instance != null)
        {
            popupObj = ObjectPool.Instance.Spawn("DamagePopup", position, Quaternion.identity);
            if (popupObj != null)
            {
                DamagePopup popup = popupObj.GetComponent<DamagePopup>();
                if (popup != null)
                {
                    popup.Setup(damage, critical);
                }
                return popup;
            }
        }

        popupObj = new GameObject("DamagePopup", typeof(RectTransform));
        DamagePopup popup = popupObj.AddComponent<DamagePopup>();
        popup.Setup(damage, critical);
        return popup;
    }

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            rectTransform = gameObject.AddComponent<RectTransform>();
        }
    }

    void Setup(int damage, bool critical)
    {
        if (textMesh == null)
        {
            textMesh = GetComponent<TextMeshProUGUI>();
            if (textMesh == null)
            {
                textMesh = gameObject.AddComponent<TextMeshProUGUI>();
            }
        }

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            parentCanvas = canvas;
            transform.SetParent(canvas.transform, false);
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = WorldToCanvasPosition(canvas, transform.position);
            }
        }

        textMesh.text = damage.ToString();
        textMesh.fontSize = critical ? 28f : 20f;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.fontStyle = critical ? TMPro.FontStyles.Bold : TMPro.FontStyles.Normal;
        textMesh.color = critical ? new Color(1f, 0.3f, 0.1f) : Color.white;
        textMesh.outlineWidth = 0.2f;
        textMesh.outlineColor = critical ? new Color(0.5f, 0f, 0f) : Color.black;
        originalColor = textMesh.color;

        isCritical = critical;
        lifetime = critical ? 1.5f : 1f;
        spawnTime = Time.time;
        velocity = new Vector3(Random.Range(-0.5f, 0.5f), 1f, 0f) * moveSpeed;
        scaleMultiplier = critical ? 1.3f : 1f;

        transform.localScale = Vector3.one * scaleMultiplier;
        isActive = true;
    }

    static Vector2 WorldToCanvasPosition(Canvas canvas, Vector3 worldPos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            Camera.main.WorldToScreenPoint(worldPos),
            canvas.worldCamera,
            out Vector2 localPoint
        );
        return localPoint;
    }

    void Update()
    {
        if (!isActive) return;

        float elapsed = Time.time - spawnTime;
        float t = elapsed / lifetime;

        Vector3 pos = transform.position;
        pos += velocity * Time.deltaTime;
        velocity.y -= 2f * Time.deltaTime;
        transform.position = pos;

        if (parentCanvas != null && rectTransform != null)
        {
            rectTransform.anchoredPosition = WorldToCanvasPosition(parentCanvas, pos);
        }

        if (t > 0.7f)
        {
            float fade = 1f - ((t - 0.7f) / 0.3f);
            if (textMesh != null)
                textMesh.color = new Color(originalColor.r, originalColor.g, originalColor.b, fade);
        }

        if (elapsed > lifetime)
        {
            Despawn();
        }
    }

    void Despawn()
    {
        isActive = false;
        if (ObjectPool.Instance != null)
        {
            ObjectPool.Instance.Despawn("DamagePopup", gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void OnSpawn()
    {
        isActive = false;
        if (textMesh != null) textMesh.color = Color.white;
        transform.localScale = Vector3.one;
    }

    public void OnDespawn()
    {
        isActive = false;
    }
}
