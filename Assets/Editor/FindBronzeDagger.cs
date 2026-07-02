using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public class FindBronzeDagger : MonoBehaviour
{
    [MenuItem("Tools/PRU213/Sửa vị trí Kiếm Đồng trong Scene")]
    public static void FindAndFixDagger()
    {
        int sceneCount = SceneManager.sceneCount;
        bool found = false;

        // Tìm vị trí của Thợ Rèn để di chuyển Kiếm Đồng đến đó
        GameObject blacksmith = GameObject.Find("tripo_convert_f674688d-7846-4522-ab2d-81ed7dadc549");
        if (blacksmith == null)
        {
            // Tìm dự phòng nếu tên thợ rèn bị khác đi một chút
            var allGo = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var go in allGo)
            {
                if (go.name.StartsWith("tripo_convert_f674688d-7846-4522") || go.name.ToLower().Contains("blacksmith"))
                {
                    blacksmith = go;
                    break;
                }
            }
        }

        for (int i = 0; i < sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            foreach (GameObject rootObj in scene.GetRootGameObjects())
            {
                Transform[] allChildren = rootObj.GetComponentsInChildren<Transform>(true);
                foreach (Transform t in allChildren)
                {
                    GameObject go = t.gameObject;
                    string nameLower = go.name.ToLower();
                    if (nameLower.Contains("bronze_dagger") || nameLower.Contains("bronze_dragger"))
                    {
                        found = true;
                        Vector3 currentPos = go.transform.position;

                        // Nếu Kiếm Đồng ở quá gần gốc tọa độ (0, 0, 0) - khu vực người chơi spawn
                        if (Vector3.Distance(currentPos, Vector3.zero) < 10f || Vector3.Distance(currentPos, new Vector3(0, 5f, 0)) < 10f)
                        {
                            Vector3 targetPos = new Vector3(15f, 0.5f, -15f); // vị trí mặc định an toàn
                            
                            if (blacksmith != null)
                            {
                                // Đặt ở trước mặt Thợ Rèn khoảng 2m để người chơi thấy khi đến Lò Rèn
                                targetPos = blacksmith.transform.position + blacksmith.transform.forward * 2.5f + blacksmith.transform.right * 1f;
                                targetPos.y = blacksmith.transform.position.y + 0.1f; // đặt sát mặt đất của Thợ Rèn
                            }

                            Undo.RecordObject(go.transform, "Fix Bronze Dagger Position");
                            go.transform.position = targetPos;
                            
                            // Đảm bảo lưu thay đổi trong Unity
                            EditorUtility.SetDirty(go);
                            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
                            
                            Debug.Log($"<color=green>[Sửa Kiếm Đồng]</color> Đã dịch chuyển thành công {go.name} từ vị trí cũ {currentPos} (gần spawn) sang vị trí mới ở Lò Rèn: {targetPos}");
                        }
                        else
                        {
                            Debug.Log($"[Sửa Kiếm Đồng] Tìm thấy {go.name} tại {currentPos} (Khoảng cách an toàn, không cần sửa).");
                        }
                    }
                }
            }
        }

        if (!found)
        {
            Debug.LogWarning("[Sửa Kiếm Đồng] Không tìm thấy bất kỳ đối tượng nào chứa 'bronze_dagger' hay 'bronze_dragger' trong scene.");
            EditorUtility.DisplayDialog("Thông báo", "Không tìm thấy Kiếm Đồng trong Scene!", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Thành công", "Đã quét và chỉnh sửa vị trí Kiếm Đồng trong Scene để tránh tự động nhặt khi bắt đầu!", "Tuyệt vời");
        }
    }
}
