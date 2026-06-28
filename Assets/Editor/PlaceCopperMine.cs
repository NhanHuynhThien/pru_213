using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class PlaceCopperMine : MonoBehaviour
{
    [MenuItem("Tools/PRU213/Đặt Mỏ Đồng Cổ Đại")]
    public static void PlaceMine()
    {
        // Đường dẫn tới file model 3D mỏ đồng cổ đại của bạn
        string assetPath = "Assets/Assets_3D/Assets_3D/Props/Điểm Thu Thập Tài Nguyên/nant_yr_eira_bronze_age_copper_mine_powys/scene.gltf";
        GameObject minePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

        if (minePrefab == null)
        {
            // Thử tìm định dạng khác nếu có
            assetPath = "Assets/Assets_3D/Assets_3D/Props/Điểm Thu Thập Tài Nguyên/nant_yr_eira_bronze_age_copper_mine_powys/scene.glb";
            minePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        }

        if (minePrefab == null)
        {
            Debug.LogError("[Lò Rèn] Không tìm thấy mô hình scene.gltf tại: " + assetPath);
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy file mô hình mỏ đồng 3D trong thư mục!", "OK");
            return;
        }

        // 1. Tạo instance trong scene đang mở dưới dạng Prefab/Model link
        GameObject mineInstance = (GameObject)PrefabUtility.InstantiatePrefab(minePrefab);
        mineInstance.name = "Mỏ Đồng Cổ Đại (Ancient Copper Mine)";
        
        // 2. Đặt ở tọa độ tối ưu (X = 110, Y = 1.2, Z = 110) để dìm chân mỏ sát mặt đất
        mineInstance.transform.position = new Vector3(110f, 1.2f, 110f);
        
        // 3. Nghiêng nhẹ trục Z (-8 độ) để ép góc bị lơ lửng cắm hẳn xuống đất
        mineInstance.transform.rotation = Quaternion.Euler(-180f, 0f, -8f);
        
        // 4. Làm dẹt độ cao trục Y (Y = 0.05) để ăn khớp mượt mà với địa hình phẳng
        mineInstance.transform.localScale = new Vector3(0.15f, 0.05f, 0.15f);

        // 5. TỰ ĐỘNG THÊM VA CHẠM VẬT LÝ (MeshCollider) cho tất cả lưới con
        MeshFilter[] filters = mineInstance.GetComponentsInChildren<MeshFilter>(true);
        int addedColliders = 0;
        foreach (var filter in filters)
        {
            if (filter.sharedMesh != null)
            {
                MeshCollider col = filter.gameObject.GetComponent<MeshCollider>();
                if (col == null)
                {
                    col = filter.gameObject.AddComponent<MeshCollider>();
                }
                col.sharedMesh = filter.sharedMesh;
                addedColliders++;
            }
        }

        // Đánh dấu Scene có thay đổi để có thể lưu lại
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        
        // Tự động chọn (focus) vào đối tượng vừa tạo để bạn dễ quan sát
        Selection.activeGameObject = mineInstance;
        
        Debug.Log($"<color=green>[Lò Rèn]</color> Đã tự động đặt Mỏ Đồng và tạo {addedColliders} collider va chạm vật lý!");
        EditorUtility.DisplayDialog("Thành công", $"Đã đặt Mỏ Đồng Cổ Đại thành công!\nTự động thêm {addedColliders} va chạm vật lý giúp nhân vật có thể đi bộ leo lên đồi mỏ.", "Tuyệt vời");
    }
}
