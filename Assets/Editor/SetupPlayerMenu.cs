using UnityEngine;
using UnityEditor;
using IdyllicFantasyNature;

public class SetupPlayerMenu
{
    [MenuItem("Tools/Setup Player Controller")]
    public static void SetupPlayer()
    {
        // 1. Create Player Capsule
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.tag = "Player"; // Add Tag for portals and camera follow
        player.transform.position = new Vector3(0, 5f, 0); // Spawns slightly above ground at center
        player.transform.localScale = new Vector3(1, 1, 1);
        player.layer = LayerMask.NameToLayer("Default");

        // 2. Add CharacterController
        // GameObject.CreatePrimitive(PrimitiveType.Capsule) already adds a CapsuleCollider.
        // We can keep it or destroy it. CharacterController has its own collider.
        Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
        
        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.5f;
        cc.center = new Vector3(0, 0, 0);

        // 3. Add PlayerMovement
        PlayerMovement pm = player.AddComponent<PlayerMovement>();
        // Using reflection or serialized object to set private fields if needed, 
        // but default inspector values might be fine. We'll set them via SerializedObject to be safe.
        SerializedObject soPm = new SerializedObject(pm);
        soPm.FindProperty("_movementSpeed").floatValue = 8f;
        soPm.FindProperty("_runMultiplier").floatValue = 2f;
        soPm.FindProperty("_gravity").floatValue = -15f;
        soPm.FindProperty("_jumpHeight").floatValue = 2f;
        soPm.ApplyModifiedProperties();

        // 4. Setup Camera
        // Find existing Main Camera
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            mainCam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
        }
        
        // Remove old camera scripts if any (e.g. SimpleCamera)
        var oldScripts = mainCam.GetComponents<MonoBehaviour>();
        foreach (var script in oldScripts)
        {
            if (script.GetType() != typeof(CameraMovement) && script.GetType().Name != "AudioListener")
            {
                Object.DestroyImmediate(script);
            }
        }

        // Parent camera to player
        mainCam.transform.SetParent(player.transform);
        mainCam.transform.localPosition = new Vector3(0, 0.8f, 0); // Eye level
        mainCam.transform.localRotation = Quaternion.identity;

        // 5. Add CameraMovement script
        CameraMovement cm = mainCam.gameObject.GetComponent<CameraMovement>();
        if (cm == null) cm = mainCam.gameObject.AddComponent<CameraMovement>();
        
        SerializedObject soCm = new SerializedObject(cm);
        soCm.FindProperty("_mouseSensity").floatValue = 3f;
        soCm.FindProperty("_controller").objectReferenceValue = player.transform;
        soCm.ApplyModifiedProperties();

        // Select the player
        Selection.activeGameObject = player;
        EditorGUIUtility.PingObject(player);

        Debug.Log("Player Controller setup successfully!");
    }
}
