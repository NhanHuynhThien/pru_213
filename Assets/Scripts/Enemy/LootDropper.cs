using UnityEngine;
using System.Collections.Generic;

public class LootDropper : MonoBehaviour
{
    [System.Serializable]
    public class DropItemConfig
    {
        public string itemName;
        [Tooltip("Kéo Prefab hoặc Model 3D từ Assets vào đây để làm vật phẩm rơi ra")]
        public GameObject itemPrefab;
        [Tooltip("Tỉ lệ rơi vật phẩm (từ 0.0 đến 1.0, ví dụ: 0.8 = 80%)")]
        [Range(0f, 1f)] public float dropChance = 0.5f;
        [Tooltip("Tỉ lệ phóng to/thu nhỏ vật phẩm (ví dụ: 0.15, 0.15, 0.15 để thu nhỏ đá khổng lồ thành cục nhỏ vừa tay)")]
        public Vector3 customScale = new Vector3(0.2f, 0.2f, 0.2f);
    }

    [Header("Loot Configuration")]
    [Tooltip("Danh sách các vật phẩm quái vật này có thể làm rơi khi chết")]
    public List<DropItemConfig> possibleDrops = new();

    private void Start()
    {
        // Đăng ký sự kiện khi quái thường chết
        EnemyController enemy = GetComponent<EnemyController>();
        if (enemy != null)
        {
            enemy.OnEnemyDeath += HandleEnemyDeath;
        }

        // Đăng ký sự kiện khi Boss chết
        BossController boss = GetComponent<BossController>();
        if (boss != null)
        {
            boss.OnBossDeath += HandleBossDeath;
        }
    }

    private void HandleEnemyDeath(EnemyController enemy)
    {
        TriggerDrop();
    }

    private void HandleBossDeath(BossController boss)
    {
        TriggerDrop();
    }

    private void TriggerDrop()
    {
        if (possibleDrops.Count == 0) return;

        // Lọc các vật phẩm rơi hợp lệ
        List<DropItemConfig> validDrops = new List<DropItemConfig>();
        foreach (var drop in possibleDrops)
        {
            if (drop.itemPrefab != null)
            {
                validDrops.Add(drop);
            }
        }

        if (validDrops.Count == 0) return;

        // Chọn ngẫu nhiên duy nhất 1 vật phẩm trong danh sách
        DropItemConfig selectedDrop = validDrops[Random.Range(0, validDrops.Count)];

        // Kiểm tra tỉ lệ rơi của vật phẩm được chọn
        if (Random.value <= selectedDrop.dropChance)
        {
            // Sinh vật phẩm tại vị trí quái chết, nhấc lên một chút
            Vector3 spawnPos = transform.position + Vector3.up * 0.5f;
            GameObject spawnedItem = Instantiate(selectedDrop.itemPrefab, spawnPos, Quaternion.identity);

            // Thu nhỏ kích thước lại cho vừa vặn theo cấu hình của người dùng
            spawnedItem.transform.localScale = selectedDrop.customScale;

            // Tự động thêm component LootItem nếu chưa có để người chơi nhặt được
            LootItem lootScript = spawnedItem.GetComponent<LootItem>();
            if (lootScript == null)
            {
                lootScript = spawnedItem.AddComponent<LootItem>();
            }
            lootScript.startImmediately = false; // Tắt nhặt ngay lập tức để thực hiện văng giả lập

            // Gán tên vật phẩm
            lootScript.itemName = string.IsNullOrEmpty(selectedDrop.itemName) ? selectedDrop.itemPrefab.name : selectedDrop.itemName;

            Debug.Log($"<color=gold>[LootDropper]</color> Rơi ngẫu nhiên 1 nguyên liệu: <b>{lootScript.itemName}</b> tại {spawnPos}");
        }
    }
}
