using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public class FixAllCollidersMenu
{
    [MenuItem("Tools/Fix All Colliders (Add Missing)")]
    public static void FixAllColliders()
    {
        int added = 0;
        int skipped = 0;

        // Get all root objects in the active scene
        GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();

        foreach (GameObject root in rootObjects)
        {
            // Skip system objects (cameras, lights, player, managers)
            string name = root.name.ToLower();
            if (name.Contains("camera") || name.Contains("light") || 
                name.Contains("player") || name.Contains("manager") ||
                name.Contains("canvas") || name.Contains("eventsystem") ||
                name.Contains("spawner") || name.Contains("pool"))
            {
                skipped++;
                continue;
            }

            // Process this root and all its children
            ProcessGameObject(root, ref added, ref skipped);
        }

        Debug.Log($"[Fix All Colliders] Done! Added {added} colliders. Skipped {skipped} objects.");
    }

    private static void ProcessGameObject(GameObject go, ref int added, ref int skipped)
    {
        // Check if this object (or its immediate parent groups) is a container
        // Container groups like "Village_Area", "Forest", "Royal_Area" etc.
        // should not get colliders themselves, only their leaf children
        bool isContainer = false;
        string goName = go.name.ToLower();

        // Known container group names
        if (goName == "village_area" || goName == "forest" || goName == "royal_area" ||
            goName == "military_camp" || goName == "environment_props" || 
            goName == "outer_fortress" || goName == "camp_group" ||
            goName == "groundplane" || goName == "terrain")
        {
            isContainer = true;
        }

        if (isContainer)
        {
            // Process children only
            foreach (Transform child in go.transform)
            {
                ProcessGameObject(child.gameObject, ref added, ref skipped);
            }
            return;
        }

        // Check if this object or any of its children already has a collider
        Collider existingCollider = go.GetComponentInChildren<Collider>();
        if (existingCollider != null)
        {
            skipped++;
            // Still process children in case some don't have colliders
            return;
        }

        // Check if this object has any renderers (meaning it's a visible object)
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            skipped++;
            return;
        }

        // Determine if it's a tree (by name heuristic)
        bool isTree = goName.Contains("tree") || goName.Contains("willow") || 
                      goName.Contains("blossom") || goName.Contains("fir") ||
                      goName.Contains("broadleaf");

        if (isTree)
        {
            // Add CapsuleCollider for tree trunk
            CapsuleCollider capsule = go.AddComponent<CapsuleCollider>();
            // Calculate bounds to determine proper capsule size
            Bounds bounds = CalculateCombinedBounds(go);
            capsule.center = go.transform.InverseTransformPoint(bounds.center);
            capsule.height = bounds.size.y;
            capsule.radius = Mathf.Min(bounds.size.x, bounds.size.z) * 0.15f; // Thin trunk
            capsule.radius = Mathf.Max(capsule.radius, 0.3f); // Minimum radius
            capsule.direction = 1; // Y-axis
            added++;
        }
        else
        {
            // Add BoxCollider for buildings/structures/props
            Bounds bounds = CalculateCombinedBounds(go);
            if (bounds.size.sqrMagnitude > 0.01f)
            {
                BoxCollider box = go.AddComponent<BoxCollider>();
                box.center = go.transform.InverseTransformPoint(bounds.center);
                
                // Convert world-space size to local-space size
                Vector3 localSize = bounds.size;
                Vector3 scale = go.transform.lossyScale;
                if (scale.x != 0) localSize.x /= scale.x;
                if (scale.y != 0) localSize.y /= scale.y;
                if (scale.z != 0) localSize.z /= scale.z;
                box.size = localSize;
                
                added++;
            }
            else
            {
                skipped++;
            }
        }

        // Set layer to Ground if the Ground layer exists
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer != -1)
        {
            SetLayerRecursive(go, groundLayer);
        }
    }

    private static Bounds CalculateCombinedBounds(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.one);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        return bounds;
    }

    private static void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursive(child.gameObject, layer);
        }
    }
}
