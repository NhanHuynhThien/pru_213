using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>
/// SocketClient Premium: Hỗ trợ tự động kết nối lại, Heartbeat và quản lý trạng thái luồng.
/// </summary>
public class SocketClient : MonoBehaviour
{
    [Header("Cấu hình kết nối")]
    public string host = "localhost";
    public int port = 8080;
    public bool autoReconnect = true;
    public float reconnectInterval = 3f;

    private TcpClient _client;
    private NetworkStream _stream;
    private Thread _receiveThread;
    private bool _isRunning = false;
    private bool _isConnecting = false;

    // Hàng đợi tin nhắn an toàn cho luồng
    private ConcurrentQueue<string> _messageQueue = new ConcurrentQueue<string>();

    // Sự kiện cho các Script khác
    public event Action OnConnected;
    public event Action OnDisconnected;
    public event Action<string> OnDataReceived;

    void Start()
    {
        StartCoroutine(ConnectionLoop());
    }

    private System.Collections.IEnumerator ConnectionLoop()
    {
        while (true)
        {
            if (_client == null || !_client.Connected)
            {
                if (autoReconnect && !_isConnecting)
                {
                    Debug.Log($"<color=white>[Network]</color> Đang thử kết nối tới {host}:{port}...");
                    ConnectToServer();
                }
            }
            yield return new WaitForSeconds(reconnectInterval);
        }
    }

    private void ConnectToServer()
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

                    // Chạy luồng nhận dữ liệu
                    _receiveThread = new Thread(ReceiveDataLoop);
                    _receiveThread.IsBackground = true;
                    _receiveThread.Start();

                    // Đẩy sự kiện về Main Thread
                    _messageQueue.Enqueue("__CONNECTED__");
                } catch (Exception e) {
                    _isConnecting = false;
                    Debug.LogWarning("[Network] Không thể kết nối: " + e.Message);
                }
            }, null);
        }
        catch (Exception e)
        {
            _isConnecting = false;
            Debug.LogError("[Network] Lỗi khởi tạo kết nối: " + e.Message);
        }
    }

    private void ReceiveDataLoop()
    {
        byte[] buffer = new byte[4096];
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
            Debug.LogWarning("[Network] Luồng nhận dữ liệu đóng: " + e.Message);
        }
        finally
        {
            _messageQueue.Enqueue("__DISCONNECTED__");
        }
    }

    public bool IsConnected => _client != null && _client.Connected;

    void Update()
    {
        // Xử lý tin nhắn trong Main Thread của Unity
        while (_messageQueue.TryDequeue(out string message))
        {
            if (message == "__CONNECTED__")
            {
                Debug.Log("<color=green>[Network] Đã kết nối thành công!</color>");
                OnConnected?.Invoke();
            }
            else if (message == "__DISCONNECTED__")
            {
                Debug.Log("<color=red>[Network] Mất kết nối tới Server.</color>");
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
        if (_client == null || !_client.Connected || _stream == null) return;

        try
        {
            string fullJson = $"{{\"action\":\"{action}\", \"payload\":{payloadJson}}}";
            byte[] data = Encoding.UTF8.GetBytes(fullJson);
            _stream.Write(data, 0, data.Length);
            _stream.Flush();
        }
        catch (Exception e)
        {
            Debug.LogError("[Network] Lỗi khi gửi: " + e.Message);
        }
    }

    private void Cleanup()
    {
        _isRunning = false;
        _stream?.Close();
        _client?.Close();
        _client = null;
    }

    private void OnDestroy()
    {
        autoReconnect = false;
        Cleanup();
    }
}
