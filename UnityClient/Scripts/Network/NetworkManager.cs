using UnityEngine;
using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    [Header("Connection")]
    public string host = "localhost";
    public int port = 8080;
    public bool autoReconnect = true;
    public float reconnectInterval = 3f;

    private TcpClient _client;
    private NetworkStream _stream;
    private Thread _receiveThread;
    private bool _isRunning = false;
    private bool _isConnecting = false;
    private ConcurrentQueue<string> _messageQueue = new();

    public event Action OnConnected;
    public event Action OnDisconnected;
    public event Action<string> OnDataReceived;

    public bool IsConnected => _client != null && _client.Connected;

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
        if (autoReconnect)
            StartCoroutine(ConnectionLoop());
    }

    System.Collections.IEnumerator ConnectionLoop()
    {
        while (true)
        {
            if (!IsConnected && !_isConnecting)
            {
                ConnectToServer();
            }
            yield return new WaitForSeconds(reconnectInterval);
        }
    }

    void ConnectToServer()
    {
        try
        {
            _isConnecting = true;
            _client = new TcpClient();
            _client.BeginConnect(host, port, (ar) => {
                try {
                    _client.EndConnect(ar);
                    _stream = _client.GetStream();
                    _isRunning = true;
                    _isConnecting = false;

                    _receiveThread = new Thread(ReceiveDataLoop);
                    _receiveThread.IsBackground = true;
                    _receiveThread.Start();

                    _messageQueue.Enqueue("__CONNECTED__");
                } catch (Exception e) {
                    _isConnecting = false;
                    Debug.LogWarning("[Network] Khong the ket noi: " + e.Message);
                }
            }, null);
        }
        catch (Exception e)
        {
            _isConnecting = false;
            Debug.LogError("[Network] Loi khoi tao ket noi: " + e.Message);
        }
    }

    void ReceiveDataLoop()
    {
        byte[] buffer = new byte[8192];
        try
        {
            while (_isRunning && _client != null && _client.Connected)
            {
                if (_stream.DataAvailable)
                {
                    int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        _messageQueue.Enqueue(data);
                    }
                }
                Thread.Sleep(10);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Network] Luong nhan du lieu dong: " + e.Message);
        }
        finally
        {
            _messageQueue.Enqueue("__DISCONNECTED__");
        }
    }

    void Update()
    {
        while (_messageQueue.TryDequeue(out string message))
        {
            if (message == "__CONNECTED__")
            {
                Debug.Log("<color=green>[Network] Da ket noi thanh cong!</color>");
                OnConnected?.Invoke();
            }
            else if (message == "__DISCONNECTED__")
            {
                Debug.Log("<color=red>[Network] Mat ket noi Server.</color>");
                OnDisconnected?.Invoke();
                Cleanup();
            }
            else
            {
                OnDataReceived?.Invoke(message);
            }
        }
    }

    public void SendAction(string action, string payloadJson = "{}")
    {
        if (_client == null || !IsConnected || _stream == null) return;

        try
        {
            string fullJson = $"{{\"action\":\"{action}\", \"payload\":{payloadJson}}}";
            byte[] data = Encoding.UTF8.GetBytes(fullJson);
            _stream.Write(data, 0, data.Length);
            _stream.Flush();
        }
        catch (Exception e)
        {
            Debug.LogError("[Network] Loi gui: " + e.Message);
        }
    }

    public void SendJson(string json)
    {
        if (_client == null || !IsConnected || _stream == null) return;

        try
        {
            byte[] data = Encoding.UTF8.GetBytes(json);
            _stream.Write(data, 0, data.Length);
            _stream.Flush();
        }
        catch (Exception e)
        {
            Debug.LogError("[Network] Loi gui: " + e.Message);
        }
    }

    void Cleanup()
    {
        _isRunning = false;
        _stream?.Close();
        _client?.Close();
        _client = null;
    }

    void OnDestroy()
    {
        autoReconnect = false;
        Cleanup();
    }
}
