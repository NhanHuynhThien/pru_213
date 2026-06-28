using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LoaThanhServer
{
    // Trạng thái của người chơi
    public class PlayerState
    {
        public string username { get; set; } = "";
        public string role { get; set; } = "user";
        public int player_id { get; set; }
        public int current_tier { get; set; } = 1;
        public int copper_count { get; set; } = 15;
        public int tin_count { get; set; } = 0;
        public int bronze_ingot { get; set; } = 0;
        public int turtle_shell { get; set; } = 0;
    }

    public class UserAccount
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string Role { get; set; } = "user";
        public PlayerState State { get; set; } = new();
    }

    public class ClientRequest
    {
        public string action { get; set; } = "";
        public JsonElement payload { get; set; }
    }

    class Program
    {
        private static readonly int Port = 8080;

        private static readonly Dictionary<string, UserAccount> Accounts = new(StringComparer.OrdinalIgnoreCase)
        {
            {
                "user", new UserAccount
                {
                    Username = "user",
                    Password = "user123",
                    Role = "user",
                    State = new PlayerState { username = "user", role = "user", player_id = 1, current_tier = 1, copper_count = 15, tin_count = 0 }
                }
            },
            {
                "admin", new UserAccount
                {
                    Username = "admin",
                    Password = "admin123",
                    Role = "admin",
                    State = new PlayerState { username = "admin", role = "admin", player_id = 2, current_tier = 3, copper_count = 100, tin_count = 50 }
                }
            },
            {
                "caothuc", new UserAccount
                {
                    Username = "caothuc",
                    Password = "kykhi",
                    Role = "user",
                    State = new PlayerState { username = "caothuc", role = "user", player_id = 3, current_tier = 1, copper_count = 0, tin_count = 0 }
                }
            }
        };

        private static readonly Dictionary<TcpClient, PlayerState> ActiveSessions = new();
        private static int totalConnections = 0;

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            PrintBanner();

            TcpListener listener = new TcpListener(IPAddress.Any, Port);
            try
            {
                listener.Start();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[SUCCESS] C# Socket Server running at port: {Port}");
                Console.WriteLine("[INFO] Waiting for Unity client connections...");
                Console.ResetColor();

                while (true)
                {
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    totalConnections++;
                    _ = HandleClientAsync(client);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[FATAL ERROR] Server error: {ex.Message}");
                Console.ResetColor();
            }
            finally
            {
                listener.Stop();
            }
        }

        private static void PrintBanner()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(@"======================================================================");
            Console.WriteLine(@"*          LOA THANH KY KHI - C# SOCKET SERVER v2.0               *");
            Console.WriteLine(@"*   He Thong Luyen Kim & Quan Ly Nguoi Choi - Unity Integration   *");
            Console.WriteLine(@"======================================================================");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"  [Account] user     | Password: user123  | Role: user");
            Console.WriteLine($"  [Account] admin    | Password: admin123 | Role: admin (Cheat)");
            Console.WriteLine($"  [Account] caothuc  | Password: kykhi    | Role: user");
            Console.WriteLine($@"======================================================================");
            Console.WriteLine($@"  Server Status: ONLINE | Port: {Port} | Connections: 0");
            Console.WriteLine(@"======================================================================");
            Console.ResetColor();
        }

        private static async Task HandleClientAsync(TcpClient client)
        {
            string clientEndPoint = client.Client.RemoteEndPoint?.ToString() ?? "Unknown";
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"\n[Network] New client connected from: {clientEndPoint} (Total: {totalConnections})");
            Console.ResetColor();

            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[8192];
            StringBuilder messageBuffer = new();

            try
            {
                while (true)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string rawMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    messageBuffer.Append(rawMessage);

                    string content = messageBuffer.ToString();
                    while (content.Contains("{") && content.Contains("}"))
                    {
                        int start = content.IndexOf('{');
                        int end = content.IndexOf('}');
                        if (start < end)
                        {
                            string jsonMsg = content.Substring(start, end - start + 1);
                            content = content.Substring(end + 1);
                            messageBuffer.Clear();
                            messageBuffer.Append(content);

                            try
                            {
                                var request = JsonSerializer.Deserialize<ClientRequest>(jsonMsg);
                                if (request != null)
                                {
                                    await ProcessRequestAsync(client, stream, request);
                                }
                            }
                            catch (JsonException)
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine($"[Warn] Invalid JSON, skipping: {jsonMsg.Substring(0, Math.Min(50, jsonMsg.Length))}...");
                                Console.ResetColor();
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Connection Error] Disconnected {clientEndPoint}: {ex.Message}");
                Console.ResetColor();
            }
            finally
            {
                ActiveSessions.Remove(client);
                client.Close();
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"[Network] Client {clientEndPoint} disconnected.");
                Console.ResetColor();
            }
        }

        private static async Task ProcessRequestAsync(TcpClient client, NetworkStream stream, ClientRequest request)
        {
            string action = request.action;
            JsonElement payload = request.payload;

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"[Received <- Client] Action: {action}");
            Console.ResetColor();

            switch (action)
            {
                case "LOGIN":
                    await HandleLoginAsync(client, stream, payload);
                    break;
                case "GET_PLAYER_DATA":
                    await HandleGetPlayerDataAsync(client, stream, payload);
                    break;
                case "START_UPGRADE_PROCESS":
                    await HandleStartUpgradeAsync(client, stream, payload);
                    break;
                case "SYNC_DATA":
                    await HandleSyncDataAsync(client, stream, payload);
                    break;
                case "ADMIN_CHEAT":
                    await HandleAdminCheatAsync(client, stream, payload);
                    break;
                case "OPEN_MINI_GAME":
                    await HandleOpenMiniGameAsync(client, stream, payload);
                    break;
                case "MINI_GAME_RESULT":
                    await HandleMiniGameResultAsync(client, stream, payload);
                    break;
                case "SAVE_STATE":
                    await HandleSaveStateAsync(client, stream, payload);
                    break;
                case "LOAD_STATE":
                    await HandleLoadStateAsync(client, stream, payload);
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[Warn] Unsupported action: {action}");
                    Console.ResetColor();
                    break;
            }
        }

        private static async Task HandleLoginAsync(TcpClient client, NetworkStream stream, JsonElement payload)
        {
            string username = "", password = "";

            if (payload.ValueKind != JsonValueKind.Null && payload.ValueKind != JsonValueKind.Undefined)
            {
                if (payload.TryGetProperty("username", out var userProp)) username = userProp.GetString() ?? "";
                if (payload.TryGetProperty("password", out var passProp)) password = passProp.GetString() ?? "";
            }

            if (Accounts.TryGetValue(username, out var account) && account.Password == password)
            {
                PlayerState playerState = account.State;
                ActiveSessions[client] = playerState;

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[Auth OK] {username} logged in. Role: {account.Role} | Tier: {playerState.current_tier}");
                Console.ResetColor();

                var response = new
                {
                    action = "LOGIN_SUCCESS",
                    payload = new
                    {
                        username = playerState.username,
                        role = playerState.role,
                        player_id = playerState.player_id,
                        current_tier = playerState.current_tier,
                        copper_count = playerState.copper_count,
                        tin_count = playerState.tin_count,
                        bronze_ingot = playerState.bronze_ingot,
                        turtle_shell = playerState.turtle_shell
                    }
                };

                await SendResponseAsync(stream, response);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Auth FAIL] Login failed: {username}");
                Console.ResetColor();

                var response = new
                {
                    action = "LOGIN_FAIL",
                    payload = new { message = "Tai khoan hoac mat khau khong chinh xac!" }
                };

                await SendResponseAsync(stream, response);
            }
        }

        private static async Task HandleGetPlayerDataAsync(TcpClient client, NetworkStream stream, JsonElement payload)
        {
            ActiveSessions.TryGetValue(client, out var state);
            state ??= Accounts["user"].State;

            var response = new
            {
                action = "SYNC_PLAYER_DATA",
                payload = new
                {
                    username = state.username,
                    role = state.role,
                    player_id = state.player_id,
                    current_tier = state.current_tier,
                    copper_count = state.copper_count,
                    tin_count = state.tin_count,
                    bronze_ingot = state.bronze_ingot,
                    turtle_shell = state.turtle_shell
                }
            };

            await SendResponseAsync(stream, response);
        }

        private static async Task HandleStartUpgradeAsync(TcpClient client, NetworkStream stream, JsonElement payload)
        {
            ActiveSessions.TryGetValue(client, out var state);
            state ??= Accounts["user"].State;

            int targetTier = state.current_tier + 1;
            if (payload.ValueKind != JsonValueKind.Null && payload.ValueKind != JsonValueKind.Undefined)
            {
                if (payload.TryGetProperty("target_tier", out var tierProp)) targetTier = tierProp.GetInt32();
            }

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"\n==================== [TIEN TRINH LUYEN KIM C# SERVER] ====================");
            Console.WriteLine($"[Step 2 & 3] Bat dau Luyen Kim -> Che tac Giap cap {targetTier} cho {state.username}!");
            Console.ResetColor();

            int steps = 5;
            for (int i = 1; i <= steps; i++)
            {
                await Task.Delay(500);
                int percent = i * (100 / steps);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"\r  [Refining] Dang nung chay nguyen lieu va dong khuon: {percent}% [");
                for (int j = 0; j < steps; j++)
                    Console.Write(j < i ? "=" : " ");
                Console.Write("]");
                Console.ResetColor();
            }
            Console.WriteLine();
            Console.WriteLine($"  [Refining] Luyen kim va lap rap hoan tat!");

            state.current_tier = targetTier;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  [Update] Nguoi choi {state.username}: Tier -> {state.current_tier}");
            Console.WriteLine($"================================================================================\n");
            Console.ResetColor();

            var response = new
            {
                action = "REQUIRE_CONSECRATION",
                payload = new
                {
                    next_tier = state.current_tier,
                    message = $"Luyen khi hoan tat! Bat dau nghi thuc Thanh Tay de kich hoat Giap {GetTierName(targetTier)}."
                }
            };

            await SendResponseAsync(stream, response);
        }

        private static async Task HandleSyncDataAsync(TcpClient client, NetworkStream stream, JsonElement payload)
        {
            if (!ActiveSessions.TryGetValue(client, out var state))
                state = Accounts["user"].State;

            if (payload.ValueKind != JsonValueKind.Null && payload.ValueKind != JsonValueKind.Undefined)
            {
                if (payload.TryGetProperty("copper_count", out var copProp)) state.copper_count = copProp.GetInt32();
                if (payload.TryGetProperty("tin_count", out var tinProp)) state.tin_count = tinProp.GetInt32();
            }

            Console.WriteLine($"[Sync] {state.username} - Copper: {state.copper_count}, Tin: {state.tin_count}");

            await SendResponseAsync(stream, new { status = "SUCCESS" });
        }

        private static async Task HandleAdminCheatAsync(TcpClient client, NetworkStream stream, JsonElement payload)
        {
            if (!ActiveSessions.TryGetValue(client, out var state) || state.role != "admin")
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[Cheat Denied] Not authorized!");
                Console.ResetColor();
                return;
            }

            int setTier = state.current_tier;
            int setCopper = state.copper_count;

            if (payload.ValueKind != JsonValueKind.Null && payload.ValueKind != JsonValueKind.Undefined)
            {
                if (payload.TryGetProperty("tier", out var tierProp)) setTier = tierProp.GetInt32();
                if (payload.TryGetProperty("copper", out var copProp)) setCopper = copProp.GetInt32();
            }

            state.current_tier = Math.Clamp(setTier, 1, 4);
            state.copper_count = setCopper;

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"[Admin Cheat] {state.username}: Set Tier={state.current_tier}, Copper={state.copper_count}");
            Console.ResetColor();

            var response = new
            {
                action = "SYNC_PLAYER_DATA",
                payload = new
                {
                    username = state.username,
                    role = state.role,
                    player_id = state.player_id,
                    current_tier = state.current_tier,
                    copper_count = state.copper_count,
                    tin_count = state.tin_count,
                    bronze_ingot = state.bronze_ingot,
                    turtle_shell = state.turtle_shell
                }
            };

            await SendResponseAsync(stream, response);
        }

        private static async Task HandleOpenMiniGameAsync(TcpClient client, NetworkStream stream, JsonElement payload)
        {
            if (!ActiveSessions.TryGetValue(client, out var state))
                state = Accounts["user"].State;

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[MiniGame] {state.username} mo Pygame mini-game (Chay truc tiep: py base_server.py)");
            Console.ResetColor();

            var response = new
            {
                action = "MINI_GAME_READY",
                payload = new { message = "Hay chay Pygame mini-game de bat dau!" }
            };

            await SendResponseAsync(stream, response);
        }

        private static async Task HandleMiniGameResultAsync(TcpClient client, NetworkStream stream, JsonElement payload)
        {
            if (!ActiveSessions.TryGetValue(client, out var state))
                state = Accounts["user"].State;

            bool won = false;
            int targetTier = state.current_tier + 1;

            if (payload.ValueKind != JsonValueKind.Null && payload.ValueKind != JsonValueKind.Undefined)
            {
                if (payload.TryGetProperty("won", out var wonProp)) won = wonProp.GetBoolean();
                if (payload.TryGetProperty("target_tier", out var tierProp)) targetTier = tierProp.GetInt32();
            }

            if (won)
            {
                state.current_tier = Math.Clamp(targetTier, 1, 4);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[MiniGame WIN] {state.username} thang! Tier -> {state.current_tier}");
                Console.ResetColor();
            }

            var response = new
            {
                action = won ? "REQUIRE_CONSECRATION" : "UPGRADE_FAILED",
                payload = won
    ? new
    {
        next_tier = state.current_tier,
        message = "Thang roi! Thanh Tay di thoi!"
    }
    : new
    {
        next_tier = state.current_tier,
        message = "That bai. Thu lai!"
    }
            };

            await SendResponseAsync(stream, response);
        }

        private static async Task HandleSaveStateAsync(TcpClient client, NetworkStream stream, JsonElement payload)
        {
            if (!ActiveSessions.TryGetValue(client, out var state))
                state = Accounts["user"].State;

            Console.WriteLine($"[SaveState] {state.username} saved. Tier: {state.current_tier}, Copper: {state.copper_count}");
            await SendResponseAsync(stream, new { status = "SUCCESS", message = "State saved!" });
        }

        private static async Task HandleLoadStateAsync(TcpClient client, NetworkStream stream, JsonElement payload)
        {
            if (!ActiveSessions.TryGetValue(client, out var state))
                state = Accounts["user"].State;

            var response = new
            {
                action = "SYNC_PLAYER_DATA",
                payload = new
                {
                    username = state.username,
                    role = state.role,
                    player_id = state.player_id,
                    current_tier = state.current_tier,
                    copper_count = state.copper_count,
                    tin_count = state.tin_count,
                    bronze_ingot = state.bronze_ingot,
                    turtle_shell = state.turtle_shell
                }
            };

            await SendResponseAsync(stream, response);
        }

        private static async Task SendResponseAsync(NetworkStream stream, object responseObj)
        {
            try
            {
                string jsonString = JsonSerializer.Serialize(responseObj);
                byte[] data = Encoding.UTF8.GetBytes(jsonString);

                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine($"[Send -> Client] {jsonString.Substring(0, Math.Min(100, jsonString.Length))}...");
                Console.ResetColor();

                await stream.WriteAsync(data, 0, data.Length);
                await stream.FlushAsync();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Send Error] {ex.Message}");
                Console.ResetColor();
            }
        }

        private static string GetTierName(int tier)
        {
            return tier switch
            {
                1 => "Gap Cham",
                2 => "Giap Dong",
                3 => "Mai Rua Than",
                4 => "Than Vuong",
                _ => "Unknown"
            };
        }
    }
}
