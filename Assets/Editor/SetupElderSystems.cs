using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class SetupElderSystems : MonoBehaviour
{
    [MenuItem("Tools/PRU213/Thiết lập Hệ thống Trưởng Lão")]
    public static void Setup()
    {
        // 1. TÌM ĐỐI TƯỢNG TRƯỞNG LÃO PHÙ HỢP NHẤT
        // Đối tượng nhân vật Trưởng Lão 3D cụ thể (tripo_convert_3003225f-8fd3-45aa-a4cc-6afd20f5eae9)
        GameObject elderObj = GameObject.Find("tripo_convert_3003225f-8fd3-45aa-a4cc-6afd20f5eae9");
        if (elderObj == null)
        {
            // Thử tìm theo đầu GUID để phòng ngừa các thay đổi nhỏ
            var allGo = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var go in allGo)
            {
                if (go.name.StartsWith("tripo_convert_3003225f-8fd3-45aa"))
                {
                    elderObj = go;
                    break;
                }
            }
        }

        if (elderObj == null)
        {
            // Thử tìm theo từ khóa tên
            var allGo = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var go in allGo)
            {
                if (go.name.ToLower().Contains("elder") || 
                    go.name.ToLower().Contains("trưởng lão") || 
                    go.name.Contains("truong lao"))
                {
                    elderObj = go;
                    break;
                }
            }
        }

        if (elderObj == null)
        {
            Debug.LogError("[Trưởng Lão] Không tìm thấy bất kỳ đối tượng Trưởng Lão nào trong Hierarchy!");
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy đối tượng Trưởng Lão hoặc mô hình tripo_convert tương ứng trong Scene!", "OK");
            return;
        }

        // 2. DỌN DẸP SẠCH SẼ TẤT CẢ DUPLICATE SCRIPT trên các đối tượng khác để tránh xung đột
        var allElders = GameObject.FindObjectsByType<ElderNPC>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var elder in allElders)
        {
            if (elder.gameObject != elderObj)
            {
                Debug.LogWarning($"[Trưởng Lão] Phát hiện và xóa script trùng lặp trên: {elder.gameObject.name}");
                DestroyImmediate(elder);
            }
        }

        // 3. THIẾT LẬP CHO ĐỐI TƯỢNG ĐÃ CHỌN
        ElderNPC elderScript = elderObj.GetComponent<ElderNPC>();
        if (elderScript == null)
        {
            elderScript = elderObj.AddComponent<ElderNPC>();
        }
        elderScript.interactionRadius = 4.0f;

        // Đảm bảo đối tượng có SphereCollider để làm mốc bắt va chạm vật lý Trigger
        SphereCollider collider = elderObj.GetComponent<SphereCollider>();
        if (collider == null)
        {
            collider = elderObj.AddComponent<SphereCollider>();
        }
        collider.isTrigger = true;
        collider.radius = 4.0f;

        Debug.Log($"<color=green>[Trưởng Lão]</color> Đã thiết lập thành công script tương tác trên: {elderObj.name}");

        // Đánh dấu Scene đã thay đổi để Unity cho phép lưu
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        // Lựa chọn đối tượng vừa thêm để người chơi dễ kiểm tra
        Selection.activeGameObject = elderObj;

        Debug.Log("<color=green>[Trưởng Lão]</color> Đã tích hợp thành công giao diện ElderNPC vào GameObject!");
        EditorUtility.DisplayDialog("Thành công", "Đã thiết lập thành công Vùng tương tác Trưởng Lão (ElderNPC) trong Scene!", "Tuyệt vời");
    }
}
