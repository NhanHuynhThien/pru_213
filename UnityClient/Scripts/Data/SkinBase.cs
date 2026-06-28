using UnityEngine;

[CreateAssetMenu(fileName = "New Skin", menuName = "LoaThanh/Skin Data")]
public class SkinBase : ScriptableObject
{
    [Header("Thông tin nhận dạng")]
    public int ID;
    public string SkinName;
    public int Tier;

    [Header("Mô hình hiển thị")]
    public GameObject ModelPrefab;

    [Header("Chỉ số cộng thêm (Stats)")]
    public int BonusHealth;
    public float BonusSpeed;
    public float BonusDamage;
    public float BonusDefense;

    [Header("Tier Info")]
    public string tierDescription;
    public string tierLore;

    [Header("Visual Effects")]
    public GameObject equipEffect;
    public Color armorColor = Color.white;
}
