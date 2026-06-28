# Loa Thanh Ky Khi - Complete Game System

**"Noi Co Khi Gap Than Linh"**

Mot game 3D Unity ket hop C# Socket Server va Python Pygame Mini-Game,
tai hien huyen thoai no than An Duong Vuong va hanh trinh tien hoa giap 4 tang.

## Quick Links

- **[SETUP.md](SETUP.md)** - Day la huong dan setup chi tiet trong Unity (bao gom Inspector assignments, troubleshooting)
- **Server:** Chay `Server/CSharpServer/` (dotnet run) hoac `Server/PythonServer/` (py base_server.py)
- **Client:** Mo Unity, open project `UnityClient/`, play scene `MainMenu.unity` hoac `GameScene.unity`

## He Thong 4 Tiers (Cap Do Giap)

| Tier | Ten               | Mo ta                       | Dac diem                                              |
| ---- | ----------------- | --------------------------- | ----------------------------------------------------- |
| 1    | **Giap Cham**     | Vai, da thu tho so          | Chi so co ban                                         |
| 2    | **Giap Dong**     | Dong xanh, hoa van Dong Son | +50 HP, +15% damage, +10% defense                     |
| 3    | **Giap Linh Quy** | Hieu ung lan toa xanh       | +120 HP, +30% damage, +25% defense, trieu hoi mui ten |
| 4    | **Than Vuong**    | Vang ruc, hieu ung xoay oc  | +250 HP, +50% damage, +40% defense, NO THAN           |

## Cac He Thong Da Xay Dung

### 1. He Thong Nguoi Choi

- `PlayerController.cs` - Di chuyen 3D (WASD), nhay, tan cong (F), chien dau, right-click de xoay
- `PlayerCombat.cs` - Quan ly HP, nhan sat thuong, hoi mau, stamina regen
- `PlayerStats.cs` - ScriptableObject chua tat ca chi so nhan vat
- `CameraController.cs` - Camera 3D theo sau nhan vat voi orbit bang chuot

### 2. He Thong Ke Dich

- `EnemyController.cs` - AI ke dich voi 5 trang thai (Idle, Patrol, Chase, Attack, Stunned, Dead)
- `EnemySpawner.cs` - He thong sinh song wave voi scaling theo tier
- `EnemyData.cs` - ScriptableObject dinh nghia tat ca loai ke dich

### 3. He Thong Boss

- `BossController.cs` - Boss AI voi 3 ky nang dac biet (Dash, AOE, Summon Minions)
- `BossSpawner.cs` - Spawn boss khi bat dau song boss
- `BossData.cs` - ScriptableObject cho boss cua tung tier
- 4 Boss cho 4 tier (Trung Uy U Minh, Thien Loc U Minh, Bong Than Am Duong, Vu Than Mien Tan Sat)

### 4. He Thong Chien Dau

- `Projectile.cs` - He thong dan dan (Mui ten, phep thuat, mui ten trieu hoi) voi object pooling + duplicate hit prevention
- `DamagePopup.cs` - Hieu ung so damage bay len (Critical/Thuong) voi ObjectPool
- `StatusEffect.cs` - Hieu ung trang thai (Poison, Burn, Stun, Slow, Buff, Shield)

### 5. He Thong UI

- `UIManager.cs` - Quan ly tat ca UI (HUD, Pause, GameOver, Victory, Upgrade, BossHP)
- `IntroSequence.cs` - Scene gioi thieu voi hieu ung chu typewriter

### 6. He Thong Upgrade

- `UpgradeSystem.cs` - Quy trinh 4 buoc: Exploration -> Refining (PyGame) -> Assembly (PyGame) -> Consecration
- `SkinManager.cs` - Quan ly doi skin/model theo tier
- `ArmorVFX.cs` - Hieu ung particle + emission cho 4 tier (MaterialPropertyBlock)

### 7. He Thong VFX

- `ParticleManager.cs` - Quan ly particle effects voi real object pooling (hit, death, upgrade, consecration)

### 8. He Thong Audio

- `AudioManager.cs` - Quan ly nhac theo tier, SFX voi pooled AudioSources

### 9. He Thong Mang

- `NetworkManager.cs` - TCP Socket client ket noi server voi auto-reconnect
- `LoginManager.cs` - Xu ly dang nhap, dong bo du lieu

### 10. He Thong Tien Ich

- `ObjectPool.cs` - Generic object pooling voi IPooledObject interface
- `ResourceNode.cs` - Node khai thac (Copper, Tin, Bronze, TurtleShell)
- `SceneLoader.cs` - Async scene loading voi progress bar

## Cac Scripts Chinh

| File                | Lines | Purpose                                                       |
| ------------------- | ----- | ------------------------------------------------------------- |
| GameManager.cs      | ~143  | Global game state, scene transitions, pause/victory           |
| PlayerController.cs | ~301  | Movement, input, attack, skills (SummonArrows cooldown fixed) |
| PlayerCombat.cs     | ~130  | HP, damage, death, stamina regen                              |
| EnemyController.cs  | ~323  | Enemy AI 5-state machine                                      |
| EnemySpawner.cs     | ~188  | Wave spawning with tier scaling                               |
| BossController.cs   | ~431  | Boss battle, Dash/AOE/Summon (SummonMinions fixed)            |
| BossSpawner.cs      | ~149  | Boss encounter management                                     |
| Projectile.cs       | ~142  | Projectiles with hit deduplication                            |
| DamagePopup.cs      | ~133  | Damage numbers with ObjectPool                                |
| StatusEffect.cs     | ~144  | Poison, Burn, Stun, Slow, Buff, Shield                        |
| UIManager.cs        | ~233  | All UI panels and HUD                                         |
| UpgradeSystem.cs    | ~267  | 4-step upgrade (resource deduction fixed)                     |
| SkinManager.cs      | ~110  | Armor tier model swapping                                     |
| ArmorVFX.cs         | ~177  | Tier VFX with MaterialPropertyBlock                           |
| ParticleManager.cs  | ~196  | VFX with real pooling                                         |
| AudioManager.cs     | ~225  | Music tier system, pooled SFX                                 |
| NetworkManager.cs   | ~188  | TCP client with auto-reconnect                                |
| LoginManager.cs     | ~183  | Auth and data sync                                            |
| CameraController.cs | ~82   | Third-person follow camera                                    |
| SceneSetup.cs       | ~123  | Auto-init managers                                            |
| ObjectPool.cs       | ~121  | Generic pooling + IPooledObject                               |
| ResourceNode.cs     | ~85   | Mining with respawn                                           |
| IntroSequence.cs    | ~176  | Story intro                                                   |
| SceneLoader.cs      | ~89   | Async loading                                                 |

## Thong So Ky Thuat

### Player Stats (base)

| Chi so          | Gia tri                |
| --------------- | ---------------------- |
| Max HP          | 100 + tier bonus       |
| Move Speed      | 5                      |
| Sprint Speed    | 8                      |
| Attack Damage   | 15 \* (1 + tier bonus) |
| Defense         | 5 \* (1 + tier bonus)  |
| Critical Chance | 5%                     |
| Stamina         | 100 (regen 10/s)       |

### Enemy Scaling

| Tier   | Multiplier |
| ------ | ---------- |
| Tier 1 | x1.0       |
| Tier 2 | x1.5       |
| Tier 3 | x2.0       |
| Tier 4 | x3.0       |

### Boss HP (base)

| Tier | Boss                 | HP   |
| ---- | -------------------- | ---- |
| 1    | Trung Uy U Minh      | 500  |
| 2    | Thien Loc U Minh     | 800  |
| 3    | Bong Than Am Duong   | 1200 |
| 4    | Vu Than Mien Tan Sat | 2000 |

### Yeu Cau Nang Cap

| Tier ke tiep | Dong | Thiec |
| ------------ | ---- | ----- |
| 2            | 20   | 5     |
| 3            | 50   | 15    |
| 4            | 100  | 30    |

## Dieu Khien Game

| Phim       | Hanh Dong                                |
| ---------- | ---------------------------------------- |
| WASD       | Di chuyen                                |
| Space      | Nhay                                     |
| Shift      | Chay nhanh (ton stamina)                 |
| F          | Tan cong (gan)                           |
| Chuot phai | Xoay nhan vat theo huong nhin            |
| Q          | Trieu hoi mui ten (Tier 3+, 5s cooldown) |
| U          | Nang cap tier (khi du tai nguyen)        |
| ESC        | Pause                                    |

## Yeu Cau He Thong

### Unity Client

- Unity 2021.3 LTS hoac moi hon
- TMP (TextMeshPro) package

### Python Server + Pygame

- Python 3.10+
- pygame: `py -m pip install pygame`

### C# Server

- .NET 9.0 SDK
- Khong co dependencies ben ngoai

## Tai Khoan Dang Nhap

| Username | Password | Role  | Starting State             |
| -------- | -------- | ----- | -------------------------- |
| user     | user123  | user  | Tier 1, 15 copper          |
| admin    | admin123 | admin | Tier 3, 100 copper, 50 tin |
| caothuc  | kykhi    | user  | Tier 1, 0 copper           |

## Noi Bat Moi Trong Code

- **Projectile deduplication** - HashSet track danh sach enemy da bi dan trung, khong con double damage
- **SummonArrows cooldown** - 5 giay giua cac lan trieu hoi, kiem tra player con song
- **Boss SummonMinions** - Thuc su spawn 3 minion thay vi chi thay doi vi tri spawner
- **Resource safety** - Tai nguyen chi bi tru khi upgrade hoan tat, server-side validation
- **Persistent game state** - Python server luu trang thai ra file JSON, khoi phuc khi restart
- **Real VFX pooling** - ParticleManager thuc su pool objects thay vi chi tao pool rong
- **Pooled AudioSources** - AudioManager reuse AudioSource components thay vi AddComponent/Destroy
- **MaterialPropertyBlock** - ArmorVFX khong con tao material copies khi update emission
- **ObjectPool with AutoDespawn** - Auto-return sau lifetime thay vi Destroy

---

_Unity 2021.3+ | .NET 9 | Python 3.10+_
