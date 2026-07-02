using UnityEngine;
using System;

public class UpgradeSystem : MonoBehaviour
{
    public static UpgradeSystem Instance { get; private set; }

    public enum UpgradeStep
    {
        None,
        Exploration,
        Refining,
        Assembly,
        Consecration
    }

    public UpgradeStep currentStep = UpgradeStep.None;
    public int currentTier = 0;

    [Header("Step Requirements")]
    public int copperForTier2 = 20;
    public int copperForTier3 = 50;
    public int copperForTier4 = 100;
    public int tinForTier2 = 5;
    public int tinForTier3 = 15;
    public int tinForTier4 = 30;

    [Header("Progress")]
    public float refiningProgress = 0f;
    public float assemblyProgress = 0f;
    public float consecrationProgress = 0f;

    public event Action<UpgradeStep> OnStepChanged;
    public event Action<int> OnTierUpgradeComplete;

    [Header("References")]
    public PlayerStats playerStats;
    public SkinManager skinManager;
    public ParticleManager particleManager;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (playerStats == null)
        {
            PlayerController pc = FindObjectOfType<PlayerController>();
            if (pc != null) playerStats = pc.stats;
        }

        if (playerStats != null)
        {
            currentTier = playerStats.currentTier;
        }
    }

    public int GetCopperRequired(int tier)
    {
        return tier switch
        {
            2 => copperForTier2,
            3 => copperForTier3,
            4 => copperForTier4,
            _ => 0
        };
    }

    public int GetTinRequired(int tier)
    {
        return tier switch
        {
            2 => tinForTier2,
            3 => tinForTier3,
            4 => tinForTier4,
            _ => 0
        };
    }

    public bool CanAffordUpgrade(int tier)
    {
        if (playerStats == null) return false;
        if (tier > 4) return false;

        int copperNeeded = GetCopperRequired(tier);
        int tinNeeded = GetTinRequired(tier);

        return playerStats.copperCount >= copperNeeded && playerStats.tinCount >= tinNeeded;
    }

    public bool TryStartUpgrade()
    {
        int nextTier = currentTier + 1;
        if (nextTier > 4) return false;

        if (!CanAffordUpgrade(nextTier))
        {
            Debug.Log($"[Upgrade] Chưa đủ tài nguyên! Cần {GetCopperRequired(nextTier)} đồng, {GetTinRequired(nextTier)} thiếc.");
            return false;
        }

        StartUpgradeProcess(nextTier);
        return true;
    }

    public void StartUpgradeProcess(int targetTier)
    {
        if (targetTier > 4) return;

        Debug.Log($"[Upgrade] Bắt đầu tiến trình nâng cấp lên Tier {targetTier}!");
        SetStep(UpgradeStep.Exploration);

        StartCoroutine(UpgradeSequence(targetTier));
    }

    System.Collections.IEnumerator UpgradeSequence(int targetTier)
    {
        yield return new WaitForSeconds(0.5f);

        SetStep(UpgradeStep.Refining);
        refiningProgress = 0f;
        Debug.Log("[Upgrade] Step 2: Refining - Đang nung chảy nguyên liệu...");

        float refiningDuration = 5f;
        float elapsed = 0f;
        while (elapsed < refiningDuration)
        {
            elapsed += Time.deltaTime;
            refiningProgress = elapsed / refiningDuration;

            UIManager.Instance?.UpdateUpgradeProgress(refiningProgress);
            UIManager.Instance?.ShowUpgradePanel(
                $"[Bước 2] Luyện Kim: {Mathf.RoundToInt(refiningProgress * 100)}%",
                refiningProgress
            );

            yield return null;
        }

        refiningProgress = 1f;
        Debug.Log("[Upgrade] Step 2 hoàn tất!");

        yield return new WaitForSeconds(0.5f);

        SetStep(UpgradeStep.Assembly);
        assemblyProgress = 0f;
        Debug.Log("[Upgrade] Step 3: Assembly - Đang lắp ráp bánh răng...");

        float assemblyDuration = 5f;
        elapsed = 0f;
        while (elapsed < assemblyDuration)
        {
            elapsed += Time.deltaTime;
            assemblyProgress = elapsed / assemblyDuration;

            UIManager.Instance?.UpdateUpgradeProgress(assemblyProgress);
            UIManager.Instance?.ShowUpgradePanel(
                $"[Bước 3] Lắp Ráp: {Mathf.RoundToInt(assemblyProgress * 100)}%",
                assemblyProgress
            );

            yield return null;
        }

        assemblyProgress = 1f;
        Debug.Log("[Upgrade] Step 3 hoàn tất!");

        yield return new WaitForSeconds(0.5f);

        SetStep(UpgradeStep.Consecration);
        consecrationProgress = 0f;
        Debug.Log("[Upgrade] Step 4: Consecration - Thực hiện nghi lễ thanh tẩy...");

        float consecrationDuration = 4f;
        elapsed = 0f;
        while (elapsed < consecrationDuration)
        {
            elapsed += Time.deltaTime;
            consecrationProgress = elapsed / consecrationDuration;

            UIManager.Instance?.UpdateUpgradeProgress(consecrationProgress);
            UIManager.Instance?.ShowUpgradePanel(
                $"[Bước 4] Thanh Tẩy: {Mathf.RoundToInt(consecrationProgress * 100)}%",
                consecrationProgress
            );

            yield return null;
        }

        consecrationProgress = 1f;
        CompleteUpgrade(targetTier);
    }

    public void CompleteUpgrade(int newTier)
    {
        if (!CanAffordUpgrade(newTier)) return;

        playerStats.copperCount -= GetCopperRequired(newTier);
        playerStats.tinCount -= GetTinRequired(newTier);

        currentTier = newTier;
        refiningProgress = 0f;
        assemblyProgress = 0f;
        consecrationProgress = 0f;

        if (playerStats != null)
        {
            playerStats.ApplyTierBonuses(newTier);
        }

        if (skinManager != null)
        {
            skinManager.currentTier = newTier;
            skinManager.ApplySkinByTier(newTier);
        }

        particleManager?.PlayUpgradeEffect(transform, newTier);
        particleManager?.PlayConsecrationEffect(transform);

        UIManager.Instance?.HideUpgradePanel();

        OnTierUpgradeComplete?.Invoke(newTier);

        SetStep(UpgradeStep.None);

        Debug.Log($"[Upgrade] NÂNG CẤP THÀNH CÔNG! Cao Thục đã đạt Tier {newTier} - {GetTierName(newTier)}!");

        if (GameManager.Instance != null && newTier > GameManager.Instance.currentBossTier)
        {
            GameManager.Instance.AdvanceTier();
        }
    }

    void SetStep(UpgradeStep step)
    {
        currentStep = step;
        OnStepChanged?.Invoke(step);
    }

    string GetTierName(int tier)
    {
        return tier switch
        {
            1 => "Giáp Chàm",
            2 => "Giáp Đồng",
            3 => "Giáp Linh Quy",
            4 => "Thần Vương",
            _ => "Unknown"
        };
    }

    public string GetCurrentStepName()
    {
        return currentStep switch
        {
            UpgradeStep.Exploration => "Khám Phá",
            UpgradeStep.Refining => "Luyện Kim",
            UpgradeStep.Assembly => "Lắp Ráp",
            UpgradeStep.Consecration => "Thanh Tẩy",
            _ => "Sẵn Sàng"
        };
    }
}
