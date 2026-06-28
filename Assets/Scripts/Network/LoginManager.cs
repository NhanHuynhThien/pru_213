using UnityEngine;
using System;
using System.Collections.Generic;

public class LoginManager : MonoBehaviour
{
    public static LoginManager Instance { get; private set; }

    [Header("Network")]
    public NetworkManager networkManager;

    [Header("Player Data")]
    public string username = "";
    public string role = "user";
    public bool isLoggedIn = false;

    public event Action<PlayerStateData> OnLoginSuccess;
    public event Action<string> OnLoginFail;
    public event Action<PlayerStateData> OnDataSync;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (networkManager == null)
            networkManager = NetworkManager.Instance;

        if (networkManager != null)
        {
            networkManager.OnDataReceived += HandleServerData;
        }
    }

    void OnDestroy()
    {
        if (networkManager != null)
            networkManager.OnDataReceived -= HandleServerData;
    }

    public void Login(string username, string password)
    {
        if (networkManager == null || !networkManager.IsConnected)
        {
            OnLoginFail?.Invoke("Chua ket noi Server!");
            return;
        }

        string payload = $"{{\"username\":\"{username}\",\"password\":\"{password}\"}}";
        networkManager.SendAction("LOGIN", payload);
        Debug.Log($"[Login] Dang nhap: {username}");
    }

    public void RequestPlayerData()
    {
        if (networkManager == null || !networkManager.IsConnected) return;
        networkManager.SendAction("GET_PLAYER_DATA", "{}");
    }

    public void SyncResources(int copper, int tin)
    {
        if (networkManager == null || !networkManager.IsConnected) return;
        string payload = $"{{\"copper_count\":{copper}, \"tin_count\":{tin}}}";
        networkManager.SendAction("SYNC_DATA", payload);
    }

    public void StartUpgrade(int targetTier)
    {
        if (networkManager == null || !networkManager.IsConnected) return;
        string payload = $"{{\"target_tier\":{targetTier}}}";
        networkManager.SendAction("START_UPGRADE_PROCESS", payload);
    }

    void HandleServerData(string json)
    {
        try
        {
            var actionResponse = JsonUtility.FromJson<ServerActionResponse>(json);
            if (actionResponse == null || string.IsNullOrEmpty(actionResponse.action)) return;

            switch (actionResponse.action)
            {
                case "LOGIN_SUCCESS":
                    var loginData = JsonUtility.FromJson<LoginSuccessData>(json);
                    if (loginData != null && loginData.payload != null)
                    {
                        username = loginData.payload.username;
                        role = loginData.payload.role;
                        isLoggedIn = true;
                        OnLoginSuccess?.Invoke(loginData.payload);
                    }
                    break;

                case "LOGIN_FAIL":
                    var failData = JsonUtility.FromJson<LoginFailData>(json);
                    if (failData != null)
                    {
                        OnLoginFail?.Invoke(failData.payload?.message ?? "Dang nhap that bai!");
                    }
                    break;

                case "SYNC_PLAYER_DATA":
                    var syncData = JsonUtility.FromJson<SyncPlayerDataData>(json);
                    if (syncData != null && syncData.payload != null)
                    {
                        OnDataSync?.Invoke(syncData.payload);
                    }
                    break;

                case "REQUIRE_CONSECRATION":
                    Debug.Log("[Login] Server yeu cau Thanh Tay!");
                    UpgradeSystem.Instance?.CompleteUpgrade(
                        JsonUtility.FromJson<ConsecrationPayload>(json)?.payload?.next_tier ?? 2
                    );
                    break;

                case "STATUS":
                    Debug.Log("[Server] " + json);
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[Login] Loi xu ly: " + ex.Message);
        }
    }
}

[System.Serializable]
public class ServerActionResponse { public string action; }

[System.Serializable]
public class PlayerStateData
{
    public string username;
    public string role;
    public int player_id;
    public int current_tier;
    public int copper_count;
    public int tin_count;
}

[System.Serializable]
public class LoginSuccessData
{
    public string action;
    public PlayerStateData payload;
}

[System.Serializable]
public class LoginFailPayload { public string message; }

[System.Serializable]
public class LoginFailData
{
    public string action;
    public LoginFailPayload payload;
}

[System.Serializable]
public class SyncPlayerDataData
{
    public string action;
    public PlayerStateData payload;
}

[System.Serializable]
public class ConsecrationPayload
{
    public string action;
    public ConsecrationInner payload;
}
[System.Serializable]
public class ConsecrationInner { public int next_tier; public string message; }
