using UnityEngine;
using UnityEditor;

public static class PhysicsLogicUtility
{
    public static void ApplyPhysicalLogic(GameObject instance, bool isTree = false)
    {
        if (instance == null) return;

        // 1. Set Layer to "Ground" (index 8)
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer != -1)
        {
            SetLayerRecursive(instance, groundLayer);
        }
        else
        {
            // Fallback: set to default if Ground doesn't exist
            SetLayerRecursive(instance, 0);
        }

        // 2. Snap to Terrain or GroundPlane height
        Vector3 pos = instance.transform.position;
        float y = 0f;
        
        Terrain terrain = Terrain.activeTerrain;
        if (terrain != null)
        {
            y = terrain.SampleHeight(pos) + terrain.transform.position.y;
        }
        else
        {
            GameObject groundPlane = GameObject.Find("GroundPlane");
            if (groundPlane != null)
            {
                y = groundPlane.transform.position.y;
            }
        }
        pos.y = y;
        instance.transform.position = pos;

        // 3. Ensure Collider exists
        Collider existingCollider = instance.GetComponentInChildren<Collider>();
        if (existingCollider == null)
        {
            if (isTree)
            {
                // Add a CapsuleCollider for the tree trunk
                CapsuleCollider capsule = instance.AddComponent<CapsuleCollider>();
                capsule.center = new Vector3(0f, 4f, 0f);
                capsule.radius = 0.5f;
                capsule.height = 8f;
                capsule.direction = 1; // Y-axis
            }
            else
            {
                // Add a BoxCollider based on the combined bounds of the mesh renderers
                Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    Bounds combinedBounds = new Bounds(Vector3.zero, Vector3.zero);
                    bool hasBounds = false;
                    
                    // Temporarily reset rotation and position to get un-rotated bounds
                    Vector3 origPos = instance.transform.position;
                    Quaternion origRot = instance.transform.rotation;
                    Vector3 origScale = instance.transform.localScale;
                    
                    instance.transform.position = Vector3.zero;
                    instance.transform.rotation = Quaternion.identity;
                    instance.transform.localScale = Vector3.one;
                    
                    foreach (Renderer r in renderers)
                    {
                        if (r is MeshRenderer || r is SkinnedMeshRenderer)
                        {
                            if (!hasBounds)
                            {
                                combinedBounds = r.bounds;
                                hasBounds = true;
                            }
                            else
                            {
                                combinedBounds.Encapsulate(r.bounds);
                            }
                        }
                    }
                    
                    // Restore original transform
                    instance.transform.position = origPos;
                    instance.transform.rotation = origRot;
                    instance.transform.localScale = origScale;
                    
                    if (hasBounds)
                    {
                        BoxCollider box = instance.AddComponent<BoxCollider>();
                        box.center = combinedBounds.center;
                        box.size = combinedBounds.size;
                        
                        Vector3 size = box.size;
                        if (size.x < 0.1f) size.x = 0.5f;
                        if (size.y < 0.1f) size.y = 1f;
                        if (size.z < 0.1f) size.z = 0.5f;
                        box.size = size;
                    }
                    else
                    {
                        instance.AddComponent<BoxCollider>();
                    }
                }
                else
                {
                    instance.AddComponent<BoxCollider>();
                }
            }
        }
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
