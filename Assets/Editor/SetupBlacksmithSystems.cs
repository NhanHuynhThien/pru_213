using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;

public class SetupBlacksmithSystems : MonoBehaviour
{
    [MenuItem("Tools/PRU213/Thiết lập Hệ thống Lò Rèn")]
    public static void Setup()
    {
        // 1. TÌM ĐỐI TƯỢNG THỢ RÈN PHÙ HỢP NHẤT
        // Ưu tiên đối tượng nhân vật thợ rèn 3D cụ thể (tripo_convert_f674688d-7846-4522-ab2d-81ed7dadc549)
        GameObject forgeObj = GameObject.Find("tripo_convert_f674688d-7846-4522-ab2d-81ed7dadc549");
        if (forgeObj == null)
        {
            // Thử tìm theo đầu GUID để phòng ngừa các thay đổi nhỏ
            var allGo = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var go in allGo)
            {
                if (go.name.StartsWith("tripo_convert_f674688d-7846-4522"))
                {
                    forgeObj = go;
                    break;
                }
            }
        }

        if (forgeObj == null)
        {
            // Fallback cuối cùng nếu không tìm thấy nhân vật thợ rèn thì gắn tạm vào lò rèn/nhà
            forgeObj = GameObject.Find("Blacksmith");
        }

        if (forgeObj == null)
        {
            var allGo = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var go in allGo)
            {
                if (go.name.ToLower().Contains("blacksmith") || 
                    go.name.ToLower().Contains("forge") || 
                    go.name.Contains("lò rèn"))
                {
                    forgeObj = go;
                    break;
                }
            }
        }

        if (forgeObj == null)
        {
            Debug.LogError("[Lò Rèn] Không tìm thấy bất kỳ đối tượng Lò Rèn hay Thợ Rèn nào trong Hierarchy!");
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy Lò Rèn hoặc Thợ Rèn trong Scene!", "OK");
            return;
        }

        // 2. DỌN DẸP SẠCH SẼ TẤT CẢ DUPLICATE SCRIPT trên các đối tượng khác để tránh xung đột
        var allForges = GameObject.FindObjectsByType<BlacksmithForge>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var forge in allForges)
        {
            if (forge.gameObject != forgeObj)
            {
                Debug.LogWarning($"[Lò Rèn] Phát hiện và xóa script trùng lặp trên: {forge.gameObject.name}");
                DestroyImmediate(forge);
            }
        }

        // 3. THIẾT LẬP CHO ĐỐI TƯỢNG ĐÃ CHỌN
        BlacksmithForge forgeScript = forgeObj.GetComponent<BlacksmithForge>();
        if (forgeScript == null)
        {
            forgeScript = forgeObj.AddComponent<BlacksmithForge>();
        }
        forgeScript.interactionRadius = 6.0f;

        // Đảm bảo đối tượng có SphereCollider để làm mốc bắt va chạm vật lý Trigger
        SphereCollider collider = forgeObj.GetComponent<SphereCollider>();
        if (collider == null)
        {
            collider = forgeObj.AddComponent<SphereCollider>();
        }
        collider.isTrigger = true;
        collider.radius = 6.0f;

        Debug.Log($"<color=green>[Lò Rèn]</color> Đã thiết lập thành công script tương tác trên: {forgeObj.name}");

        // 2. Tìm Canvas chính của màn hình
        Canvas canvas = GameObject.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[Lò Rèn] Không tìm thấy Canvas chính trong Hierarchy!");
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy UI Canvas chính trong Scene!", "OK");
            return;
        }

        // Đảm bảo Canvas chính có component GraphicRaycaster để nhận tia click chuột
        GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
            Debug.Log("<color=green>[Lò Rèn]</color> Đã tự động thêm GraphicRaycaster vào Canvas chính.");
        }

        // Đảm bảo có EventSystem hoạt động trong Scene (Bắt buộc để Unity nhận diện click chuột vào UI)
        var eventSystem = GameObject.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include);
        if (eventSystem == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("<color=green>[Lò Rèn]</color> Đã tự động tạo mới EventSystem để xử lý click chuột.");
        }
        else if (!eventSystem.gameObject.activeSelf)
        {
            eventSystem.gameObject.SetActive(true);
            Debug.Log("<color=green>[Lò Rèn]</color> Đã kích hoạt lại EventSystem bị ẩn trong Scene.");
        }

        // 3. Thiết lập bảng UI Rèn dưới Canvas
        Transform existingPanel = canvas.transform.Find("BlacksmithUIPanel");
        GameObject panelObj;
        if (existingPanel != null)
        {
            panelObj = existingPanel.gameObject;
            Debug.Log("[Lò Rèn] Đã tìm thấy BlacksmithUIPanel cũ, tiến hành cập nhật.");
        }
        else
        {
            panelObj = new GameObject("BlacksmithUIPanel", typeof(RectTransform));
            panelObj.transform.SetParent(canvas.transform, false);
            Debug.Log("[Lò Rèn] Đã tạo mới đối tượng BlacksmithUIPanel.");
        }

        // Cấu hình RectTransform phủ toàn bộ Canvas
        RectTransform rect = panelObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        // Thêm component điều khiển giao diện BlacksmithUI
        BlacksmithUI blacksmithUI = panelObj.GetComponent<BlacksmithUI>();
        if (blacksmithUI == null)
        {
            blacksmithUI = panelObj.AddComponent<BlacksmithUI>();
        }

        // 5. Tự động sửa lỗi trùng lặp AudioListener (Tránh cảnh báo spam 999+ gây giật lag Editor)
        var listeners = GameObject.FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (listeners.Length > 1)
        {
            AudioListener primaryListener = null;
            
            // Tìm listener ưu tiên ở Main Camera chính có tag MainCamera hoặc CameraController
            foreach (var lis in listeners)
            {
                if (lis.CompareTag("MainCamera") || lis.GetComponent<CameraController>() != null)
                {
                    primaryListener = lis;
                    break;
                }
            }

            // Nếu không tìm thấy, chọn listener đầu tiên làm chính
            if (primaryListener == null && listeners.Length > 0)
            {
                primaryListener = listeners[0];
            }

            // Vô hiệu hóa các listener phụ trùng lặp khác
            foreach (var lis in listeners)
            {
                if (lis != primaryListener)
                {
                    lis.enabled = false;
                    Debug.LogWarning($"[Lò Rèn] Đã tự động vô hiệu hóa AudioListener trùng lặp trên GameObject: {lis.gameObject.name} để tránh spam cảnh báo lag.");
                }
            }
        }

        // Đánh dấu Scene đã thay đổi để Unity cho phép lưu
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        // Lựa chọn đối tượng vừa thêm để người chơi dễ kiểm tra
        Selection.activeGameObject = panelObj;

        Debug.Log("<color=green>[Lò Rèn]</color> Đã tích hợp thành công giao diện BlacksmithUI vào Canvas chính!");
        EditorUtility.DisplayDialog("Thành công", "Đã thiết lập thành công Vùng tương tác Lò Rèn và Bảng giao diện Rèn (BlacksmithUI) dưới Canvas chính!", "Tuyệt vời");
    }
}
