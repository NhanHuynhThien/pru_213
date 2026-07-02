using UnityEngine;

public class LootItem : MonoBehaviour
{
    [Header("Item Config")]
    public string itemName = "Wideblade Sword";
    
    [Header("Weapon Config (Chỉ dùng nếu là vũ khí nhặt được)")]
    public bool isWeapon = false;
    [Tooltip("Prefab hoặc Model của vũ khí này sẽ sinh ra trên tay người chơi")]
    public GameObject weaponPrefab;
    [Tooltip("Vị trí lệch của vũ khí khi gắn vào tay phải người chơi")]
    public Vector3 equipOffset = new Vector3(-0.06f, 0.05f, 0.02f);
    [Tooltip("Góc xoay của vũ khí khi gắn vào tay phải người chơi")]
    public Vector3 equipRotation = new Vector3(80f, 0f, 0f);
    [Tooltip("Tỉ lệ scale của vũ khí khi gắn vào tay phải người chơi")]
    public Vector3 equipScale = Vector3.one;
    
    [Header("Pickup Delay")]
    [Tooltip("Thời gian chờ (giây) trước khi có thể nhặt được vật phẩm kể từ khi rơi")]
    public float pickupDelay = 1.2f; 
    [Tooltip("Nếu bật, vật phẩm có thể nhặt ngay lập tức và giữ nguyên trạng thái Trigger ban đầu (dùng cho vũ khí đặt sẵn trong cảnh)")]
    public bool startImmediately = true;
    private float spawnTime;
    
    private Collider[] colliders;
    private Rigidbody rb;
    private bool isReadyForPickup = false;

    // Các biến dùng cho giả lập văng vật lý tự động
    private Vector3 velocity;
    private float groundY;
    private bool isPopping = false;



    private void Start()
    {
        // Tự động điều chỉnh vị trí của Kiếm Đồng ở gốc tọa độ/gần người chơi sang trước mặt người chơi 6m và đặt trên mặt đất
        string nameLower = (itemName != null) ? itemName.ToLower() : "";
        string goNameLower = gameObject.name.ToLower();
        if ((nameLower.Contains("bronze") && nameLower.Contains("dagger")) || 
            (goNameLower.Contains("bronze") && (goNameLower.Contains("dagger") || goNameLower.Contains("dragger"))))
        {
            if (Time.frameCount <= 3 || Time.time < 0.3f)
            {
                Vector3 playerPos = Vector3.zero;
                Vector3 playerForward = Vector3.forward;
                bool foundPlayer = false;

                // 1. Thử tìm từ Player thực tế trong scene (chính xác nhất ở runtime)
                GameObject player = GameObject.Find("Player");
                if (player == null) player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerPos = player.transform.position;
                    playerForward = player.transform.forward;
                    foundPlayer = true;
                }

                // 2. Thử tìm từ SceneSetup (phương án dự phòng)
                if (!foundPlayer)
                {
                    SceneSetup setup = FindAnyObjectByType<SceneSetup>();
                    if (setup != null && setup.playerSpawnPoint != null)
                    {
                        playerPos = setup.playerSpawnPoint.position;
                        playerForward = setup.playerSpawnPoint.forward;
                        foundPlayer = true;
                    }
                }

                if (foundPlayer)
                {
                    float dist = Vector3.Distance(transform.position, playerPos);
                    // Nếu quá gần người chơi (dưới 8 mét) hoặc gốc tọa độ
                    if (dist < 8f || Vector3.Distance(transform.position, Vector3.zero) < 10f)
                    {
                        Vector3 spawnForward = playerForward;
                        if (spawnForward.sqrMagnitude < 0.1f) spawnForward = Vector3.forward;
                        
                        // Đặt kiếm ra trước mặt người chơi 6.0 mét
                        Vector3 targetPos = playerPos + spawnForward * 6.0f;
                        
                        // Tìm cao độ mặt đất bên dưới bằng Raycast để đặt kiếm nằm trên mặt đất chuẩn
                        RaycastHit hit;
                        if (Physics.Raycast(targetPos + Vector3.up * 2f, Vector3.down, out hit, 15f))
                        {
                            targetPos = hit.point + Vector3.up * 0.05f;
                        }
                        else
                        {
                            targetPos.y = playerPos.y - 0.9f + 0.05f; // Dự phòng bằng chân nhân vật
                        }
                        
                        transform.position = targetPos;
                        Debug.Log($"[LootItem] Đã chuyển vị trí Bronze Dagger ra trước mặt người chơi 6.0m: {targetPos}");
                    }
                }
            }
        }

        spawnTime = Time.time;

        // Tự động thêm BoxCollider làm Trigger nhặt đồ với kích thước chuẩn trong thế giới thực là 1.5m
        // (Khử hoàn toàn việc Scale của GameObject quá nhỏ như 0.08 làm cho Trigger thu bé lại)
        BoxCollider pickupTrigger = gameObject.AddComponent<BoxCollider>();
        Vector3 worldScale = transform.lossyScale;
        float targetWorldSize = 1.5f;
        float localX = worldScale.x > 0.001f ? (targetWorldSize / worldScale.x) : targetWorldSize;
        float localY = worldScale.y > 0.001f ? (targetWorldSize / worldScale.y) : targetWorldSize;
        float localZ = worldScale.z > 0.001f ? (targetWorldSize / worldScale.z) : targetWorldSize;
        pickupTrigger.size = new Vector3(localX, localY, localZ);
        pickupTrigger.center = Vector3.zero;
        pickupTrigger.isTrigger = true;

        colliders = GetComponentsInChildren<Collider>();

        rb = GetComponentInChildren<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; // Khóa Rigidbody thực tế để tránh xung đột vật lý
        }

        // LUÔN LUÔN để các Collider là Trigger từ đầu để tránh lỗi vật lý đẩy người chơi/quái bay lên trời
        SetCollidersTrigger(true);

        if (startImmediately)
        {
            isReadyForPickup = true;
            isPopping = false;
        }
        else
        {
            isReadyForPickup = false;
            isPopping = true;
            
            // Tạm thời tắt các collider của chính mình để tránh tia Raycast tự đâm trúng bản thân
            foreach (var c in colliders) if (c != null) c.enabled = false;

            // Tìm cao độ mặt đất bên dưới bằng Raycast để biết điểm dừng khi rơi
            groundY = transform.position.y - 0.5f; // Mức dự phòng
            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up * 2f, Vector3.down, out hit, 25f))
            {
                // Chỉ lấy mặt đất (Terrain hoặc các vật cản môi trường tĩnh), bỏ qua người chơi và quái vật
                string colName = hit.collider.name.ToLower();
                if (!hit.collider.CompareTag("Player") && 
                    !colName.Contains("player") && 
                    !colName.Contains("wolf") && 
                    !colName.Contains("linh thú") && 
                    !colName.Contains("boss") && 
                    !colName.Contains("enemy") && 
                    !colName.Contains("uminh"))
                {
                    groundY = hit.point.y;
                }
            }

            // Bật lại các collider sau khi đã lấy xong vị trí mặt đất
            foreach (var c in colliders) if (c != null) c.enabled = true;

            // Thiết lập vận tốc ban đầu để bay vòng lên (giả làm văng)
            velocity = new Vector3(
                Random.Range(-1.5f, 1.5f),
                Random.Range(4f, 5.5f),
                Random.Range(-1.5f, 1.5f)
            );
        }
    }

    private void SetCollidersTrigger(bool isTrigger)
    {
        if (colliders == null) return;
        foreach (var c in colliders)
        {
            if (c != null)
            {
                if (c is MeshCollider mc)
                {
                    mc.convex = true;
                }
                c.isTrigger = isTrigger;
            }
        }
    }

    public bool CanPickup()
    {
        return isReadyForPickup;
    }

    private void Update()
    {
        if (isPopping)
        {
            // Trọng lực giả lập tác động lên vận tốc Y
            velocity.y -= 9.81f * Time.deltaTime;
            
            // Cập nhật vị trí bằng tay
            transform.position += velocity * Time.deltaTime;
            
            // Nếu rơi xuống dưới mặt đất hoặc quá thời gian chờ
            if (transform.position.y <= groundY || Time.time - spawnTime >= pickupDelay)
            {
                isPopping = false;
                isReadyForPickup = true;
                SetCollidersTrigger(true);
                
                // Đặt quặng đứng ở độ cao mặt đất và nhấc lên một tí
                transform.position = new Vector3(transform.position.x, groundY + 0.3f, transform.position.z);
                
                // Trả góc quay thẳng đứng lại để xoay tròn đều đẹp
                transform.rotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);
            }
        }
        else if (isReadyForPickup)
        {
            // Hiệu ứng xoay tròn quanh Y và nhấp nhô nhẹ nhàng
            transform.Rotate(Vector3.up * 50f * Time.deltaTime);

            float bounce = Mathf.Sin(Time.time * 3f) * 0.1f;
            transform.Translate(Vector3.up * bounce * Time.deltaTime, Space.World);
        }
    }
}
