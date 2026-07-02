using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class ResetStatsOnLoad
{
    static ResetStatsOnLoad()
    {
        ResetStats();
    }

    [MenuItem("Tools/PRU213/Reset Player Stats về Tier 0")]
    public static void ResetStats()
    {
        string assetPath = "Assets/Data/PlayerStats.asset";
        PlayerStats stats = AssetDatabase.LoadAssetAtPath<PlayerStats>(assetPath);
        if (stats != null)
        {
            stats.ApplyTierBonuses(0);
            EditorUtility.SetDirty(stats);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=green>[PRU213]</color> Tự động reset PlayerStats về Tier 0 (Mặc định) thành công!");
        }
    }
}
