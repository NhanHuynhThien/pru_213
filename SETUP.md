# Loa Thanh Ky Khi - Build & Setup Guide

## Quick Start (5 minutes)

### Step 1: Run the Server

**Option A: C# Server (Recommended)**
```powershell
cd Server/CSharpServer
dotnet run
```

**Option B: Python Server (with Pygame mini-game - required for upgrade steps 2-3)**
```powershell
cd Server/PythonServer
pip install pygame
py base_server.py
```
> **Important:** The Python server includes a PyGame mini-game for the Refining & Assembly upgrade steps. Without pygame, upgrades won't progress past step 1.

### Step 2: Open Unity

1. Open Unity Hub
2. Click "Open" -> Select `UnityClient/` folder
3. Wait for project to import (may take a few minutes)
4. Open `Scenes/MainMenu.unity` or `Scenes/GameScene.unity`
5. Click Play

### Step 3: Login Credentials

| Username | Password | Role | Starting State |
|----------|----------|------|----------------|
| user | user123 | user | Tier 1, 15 copper |
| admin | admin123 | admin | Tier 3, 100 copper, 50 tin |
| caothuc | kykhi | user | Tier 1, 0 copper |

---

## Full Unity Setup (First Time)

### 1. Install Packages

1. Open Unity Editor
2. `Window -> Package Manager`
3. Install: **TextMeshPro** (required for all UI text)

### 2. Setup Layers

`Edit -> Project Settings -> Tags & Layers`

| Layer | Index | Used For |
|-------|-------|---------|
| Ground | 8 | Ground plane, walls, rocks |
| Player | 9 | Player character |
| Enemy | 10 | Enemy units |
| Boss | 11 | Boss enemies |

### 3. Setup NavMesh (REQUIRED for enemy AI)

1. Select your Ground GameObject in the Hierarchy
2. In Inspector, make sure **Layer** is set to **Ground** (8)
3. Check **Navigation Static** in Inspector
4. `Window -> AI -> Navigation`
5. Select **Object** tab -> make sure Ground is listed
6. Click **Bake** button
7. (Optional) Adjust bake settings: Agent Radius 0.5, Step Height 0.4, Max Slope 45

> Without NavMesh bake, enemies will not move!

### 4. Create All ScriptableObjects

Right-click in Project folder -> `Create -> LoaThanh:`

#### 4a. PlayerStats (1x)
- Right-click -> `Create -> LoaThanh -> Player Stats`
- Name: `PlayerStats_CaoThuc`
- Inspector values (recommended):
  - playerName: "Cao Thuc"
  - maxHealth: 100
  - moveSpeed: 5
  - sprintSpeed: 8
  - jumpForce: 8
  - attackDamage: 15
  - attackSpeed: 1
  - criticalChance: 0.05
  - criticalMultiplier: 1.5
  - defense: 5
  - stamina: 100
  - maxStamina: 100
  - staminaRegen: 10
  - copperCount: 15

#### 4b. Skin Data (4x)
Right-click -> `Create -> LoaThanh -> Skin Data`

| Asset Name | Tier | SkinName | BonusHealth | BonusSpeed | BonusDamage | BonusDefense |
|------------|------|----------|-------------|------------|-------------|--------------|
| SkinBase_Tier1_GiapCham | 1 | Giap Cham | 0 | 0 | 0 | 0 |
| SkinBase_Tier2_GiapDong | 2 | Giap Dong | 50 | 0 | 2.25 | 0.5 |
| SkinBase_Tier3_GiapLinhQuy | 3 | Giap Linh Quy | 120 | 0 | 4.5 | 1.25 |
| SkinBase_Tier4_ThanVuong | 4 | Than Vuong | 250 | 0 | 7.5 | 2 |

> **Note:** Assign your 3D armor models to `ModelPrefab` field for each tier.

#### 4c. Enemy Data (3x)
Right-click -> `Create -> LoaThanh -> Enemy Data`

| Asset Name | enemyName | maxHealth | moveSpeed | attackDamage | defense | copperReward |
|------------|-----------|-----------|-----------|--------------|---------|--------------|
| EnemyData_UMinhSat | U Minh Sat | 40 | 3 | 10 | 2 | 3 |
| EnemyData_UMinhCai | U Minh Cai | 60 | 2.5 | 15 | 4 | 5 |
| EnemyData_UMinhBinh | U Minh Binh | 50 | 3 | 12 | 2 | 4 |

Set tier multipliers in Inspector:
- tier1Multiplier: 1.0
- tier2Multiplier: 1.5
- tier3Multiplier: 2.0
- tier4Multiplier: 3.0

#### 4d. Boss Data (4x)
Right-click -> `Create -> LoaThanh -> Boss Data`

| Asset Name | bossName | requiredTier | maxHealth | attackDamage | defense | totalPhases |
|------------|----------|--------------|-----------|--------------|---------|-------------|
| BossData_TanThuong | Trung Uy U Minh | 1 | 500 | 25 | 10 | 2 |
| BossData_ThienLoc | Thien Loc U Minh | 2 | 800 | 30 | 15 | 2 |
| BossData_AmDuongVuongGhost | Bong Than Am Duong | 3 | 1200 | 35 | 20 | 3 |
| BossData_VuThanMienTanSat | Vu Than Mien Tan Sat | 4 | 2000 | 40 | 25 | 3 |

Phase health thresholds:
- Tier 1: `0.5`
- Tier 2: `0.5`
- Tier 3: `0.66, 0.33`
- Tier 4: `0.75, 0.5, 0.25`

### 5. Setup Player Prefab

1. Create Empty GameObject named "Player"
2. Add components (order matters):
   - **CharacterController** (built-in Unity)
   - **PlayerController** (from Scripts/Player)
     - Drag `PlayerStats_CaoThuc` to `Stats` field
     - Set `Camera Transform` to main camera
     - Set `Ground Mask` to Layer "Ground" (8)
   - **PlayerCombat** (from Scripts/Combat)
     - Drag `PlayerStats_CaoThuc` to `Stats` field
     - Drag Player to `Controller` field
     - Assign Animator if available
   - **SkinManager** (from Scripts/Upgrade)
     - Set `Character Root` to a child transform (or this transform if using single model)
     - Assign all 4 SkinBase ScriptableObjects to `Available Skins` list
   - **ArmorVFX** (from Scripts/VFX)
     - Set `Current Tier` to 1
3. Create child GameObject named "AttackPoint" at position (0, 1, 1)
4. Back on Player: drag AttackPoint to `Attack Point` field in PlayerController
5. Set Player GameObject tag to **Player** and Layer to **Player** (9)
6. Save as Prefab: drag into `Assets/Prefabs/` folder

### 6. Setup Enemy Prefabs (3x)

For each enemy type:

1. Create Empty GameObject named "Enemy_[Type]"
2. Add components:
   - **CharacterController** (built-in Unity) - for collision
   - **NavMeshAgent** (built-in Unity) - for movement
   - **EnemyController** (from Scripts/Enemy)
     - Drag corresponding EnemyData to `Data` field
     - Set `Sight Range`: 8, `Chase Range`: 12, `Attack Range`: 2
     - Assign Animator if available
   - Add colliders (Sphere/Capsule) for hit detection
3. Set tag to **Enemy** and Layer to **Enemy** (10)
4. Save as Prefab: drag into `Assets/Prefabs/` folder

### 7. Setup Boss Prefab

1. Create Empty GameObject named "Boss_[Tier]"
2. Add components:
   - **CharacterController** (built-in Unity)
   - **BossController** (from Scripts/Boss)
     - Drag corresponding BossData to `Data` field
     - Set `Attack Range`: 3, `Attack Cooldown`: 2
     - Assign Animator if available
   - Add colliders
3. Set tag to **Boss** and Layer to **Boss** (11)
4. Save as Prefab: drag into `Assets/Prefabs/` folder

### 8. Setup GameScene Hierarchy

If starting from scratch, create this structure:

```
Scene Hierarchy:
├── GameManager (add GameManager script to empty GameObject)
├── AudioManager (add AudioManager script to empty GameObject)
│   └── Drag AudioClip assets to inspector fields (menuMusic, attackSFX, etc.)
├── ParticleManager (add ParticleManager script to empty GameObject)
│   └── Drag particle prefabs to inspector fields (attackHitEffect, deathEffect, etc.)
├── NetworkManager (add NetworkManager script to empty GameObject)
│   └── Set Host: "localhost", Port: 8080
├── LoginManager (add LoginManager script to empty GameObject)
├── Camera (Main Camera)
│   └── Add CameraController script
├── Directional Light
├── Ground
│   ├── MeshCollider
│   ├── Navigation Static: ON
│   └── Layer: Ground (8)
├── Boundary Walls (4x - BoxColliders, Layer: Ground)
├── PlayerSpawnPoint (empty GameObject, position 0,0,0)
├── Player
│   └── Drag in Player prefab, assign to PlayerSpawnPoint position
├── EnemySpawner (add EnemySpawner script to empty GameObject)
│   ├── Drag EnemyData assets to Enemy Prefabs list
│   ├── Drag Enemy prefabs to Enemy Prefabs list
│   ├── Set Spawn Points (create 4 empty children around arena as spawn points)
│   └── Set Max Enemies: 10, Spawn Interval: 3, Spawn Radius: 15
├── BossSpawner (add BossSpawner script to empty GameObject)
│   ├── Drag BossData assets to Boss Datas list
│   ├── Drag Boss prefabs to Boss Prefabs list
│   └── Set Spawn Point to a Transform in the arena center
├── UpgradeSystem (add UpgradeSystem script to empty GameObject)
│   ├── Drag PlayerStats to Player Stats field
│   ├── Drag SkinManager reference
│   └── Drag ParticleManager reference
├── ResourceNodes (parent GameObject)
│   ├── CopperNode x3 (add ResourceNode script, set Type: Copper, Amount: 5)
│   ├── TinNode x2 (add ResourceNode script, set Type: Tin, Amount: 3)
│   ├── BronzeNode x1 (add ResourceNode script, set Type: Bronze, Amount: 2)
│   └── TurtleShellNode x1 (add ResourceNode script, set Type: TurtleShell, Amount: 1)
└── Canvas
    └── Add UIManager script to Canvas
        ├── Drag HealthBar (Image), StaminaBar (Image) references
        ├── Drag HealthText, StaminaText, CopperText, TierText, WaveText, TimeText (all TextMeshProUGUI)
        ├── Drag BossHealthBar (Image), BossNameText, BossHealthPanel (GameObject)
        ├── Drag UpgradePanel, UpgradeStepText, UpgradeProgressBar references
        ├── Drag PlayerCombat reference
        ├── Drag PlayerStats reference
        └── Drag EnemySpawner reference
```

### 9. ObjectPool Setup (CRITICAL for projectiles)

1. Create Empty GameObject named "ObjectPool" in scene
2. Add `ObjectPool` script
3. Add pool entries for each pooled object type:

| Tag | Prefab | Size |
|-----|--------|------|
| Projectile | Drag Projectile prefab | 20 |
| DamagePopup | Drag DamagePopup prefab | 30 |

> **Note:** If Projectile prefab is missing, create one with Projectile script and a simple mesh (capsule/arrow)

### 10. Setup Resource Node Prefabs

1. Create GameObject with model mesh (rock, ore, etc.)
2. Add `SphereCollider` as trigger (Is Trigger: ON)
3. Add `ResourceNode` script:
   - Set Type (Copper/Tin/Bronze/TurtleShell)
   - Set Amount (how much player gains)
   - Set Respawn Time (seconds)
4. Save as Prefab

---

## Upgrade System Flow

The upgrade system has **4 steps**:

| Step | Name | Duration | Server Side |
|------|------|---------|------------|
| 1 | Exploration | Instant | Unity client deducts resources |
| 2 | Refining | 5 seconds | Python PyGame mini-game (Luyen Kim) |
| 3 | Assembly | 5 seconds | Python PyGame mini-game (Lap Rap) |
| 4 | Consecration | 4 seconds | Server sends REQUIRE_CONSECRATION |

### Upgrade Costs

| Tier | Copper | Tin | Bronze | TurtleShell |
|------|--------|-----|--------|-------------|
| Tier 2 | 20 | 5 | - | - |
| Tier 3 | 50 | 15 | 10 | 5 |
| Tier 4 | 100 | 30 | 25 | 10 |

> **Note:** Upgrade costs are defined in UpgradeSystem.cs and can be adjusted in Inspector.

### Tier Bonuses Applied

| Tier | HP Bonus | Damage Bonus | Defense Bonus | Special |
|------|----------|-------------|---------------|---------|
| 1 | +0 | x1.0 | x1.0 | - |
| 2 | +50 | x1.15 | x1.10 | - |
| 3 | +120 | x1.30 | x1.25 | Summon Arrows (Q) |
| 4 | +250 | x1.50 | x1.40 | Summon Arrows (Q) + stronger |

---

## Controls

| Key | Action |
|-----|--------|
| WASD | Move |
| Space | Jump |
| Left Shift | Sprint (costs stamina) |
| F | Melee Attack |
| Right Mouse | Rotate player toward mouse cursor |
| Q | Summon Arrows (Tier 3+) |
| U | Open Upgrade panel / Start upgrade |
| ESC | Pause game |

---

## Architecture

```
+-------------------+
|  Unity Client     |
|  (C# Scripts)     |
+-------+-----------+
        | TCP Socket
        v
+-------+-----------+
|  C# Server        |
|  (Port 8080)      |
+--------+----------+
         | (optional integration)
         v
+--------+----------+
|  Python Server +  |
|  Pygame MiniGame  |
|  (Refining/Assembly)|
+-------------------+
```

## Network Protocol

Messages are JSON over TCP:

| Action | Direction | Payload |
|--------|-----------|---------|
| LOGIN | Client->Server | `{"username", "password"}` |
| LOGIN_SUCCESS | Server->Client | `{"username", "role", "player_id", "current_tier", "copper_count", "tin_count"}` |
| GET_PLAYER_DATA | Client->Server | `{}` |
| SYNC_DATA | Client->Server | `{"copper_count", "tin_count"}` |
| START_UPGRADE_PROCESS | Client->Server | `{"target_tier"}` |
| REQUIRE_CONSECRATION | Server->Client | `{"next_tier", "message"}` |

---

## Troubleshooting

### "Socket connection failed"
- Make sure the C# or Python server is running first
- Check that port 8080 is not blocked by firewall
- Verify `host` in NetworkManager Inspector matches server IP

### "Player falls through ground"
- Add CharacterController component to Player
- Add MeshCollider to ground plane
- Make sure ground Layer is set to "Ground" (8)
- **Build NavMesh** (`Window -> AI -> Navigation -> Bake`)

### "Enemies don't move / don't spawn"
- **Build NavMesh** first - this is the #1 cause
- Make sure Ground has Navigation Static ON
- Check EnemySpawner Inspector: Enemy Prefabs list must have entries
- Check Enemy prefab has EnemyController script and EnemyData assigned
- Make sure Enemy prefab is assigned in EnemySpawner

### "Boss doesn't spawn"
- Check BossSpawner Inspector: Boss Prefabs list must have entries
- Check Boss prefab has BossController script and BossData assigned
- Make sure Boss prefab is assigned in BossSpawner

### "Missing references / null errors"
- Re-assign all ScriptableObjects in Inspector (drag from Project to fields)
- Assign PlayerStats to PlayerController AND PlayerCombat
- Assign SkinManager reference to UpgradeSystem
- Assign ParticleManager reference to UpgradeSystem
- Assign all enemy prefabs to EnemySpawner
- Assign all boss prefabs to BossSpawner
- Assign all particle prefabs to ParticleManager
- Assign ObjectPool pools in Inspector

### "Scripts don't compile"
- Install TextMeshPro: `Window -> Package Manager -> TextMeshPro`
- Make sure all script GUIDs in scene files match actual script classes
- Check Console for specific error messages

### "Upgrade system not working"
- Make sure Python server is running for steps 2 & 3
- Check UpgradeSystem Inspector references (PlayerStats, SkinManager, ParticleManager)
- Verify PlayerStats is assigned to both PlayerController and PlayerCombat

### "Audio not playing"
- Assign AudioClip assets to AudioManager Inspector fields
- If AudioSource is null, AudioManager creates one automatically
- Check that AudioSource components have clips assigned

### "Particle effects not showing"
- Assign particle prefabs to ParticleManager Inspector fields
- Make sure particle prefabs exist in `Assets/Prefabs/` folder
- Check that particle systems are set to PLAY ON AWAKE or triggered by scripts

---

## File Summary

| File | Purpose |
|------|---------|
| GameManager.cs | Global game state, scene transitions, pause/victory |
| PlayerController.cs | Movement, input, attack, skills |
| PlayerCombat.cs | HP, damage, death, stamina regen |
| EnemyController.cs | Enemy AI with 5 states (Idle/Patrol/Chase/Attack/Dead) |
| EnemySpawner.cs | Wave spawning with tier scaling |
| BossController.cs | Boss battle with phase transitions, Dash/AOE/Summon abilities |
| BossSpawner.cs | Boss encounter management |
| Projectile.cs | Arrow/magic projectiles with pooling |
| DamagePopup.cs | Floating damage numbers |
| StatusEffect.cs | Poison, Burn, Stun, Slow, Buff, Shield |
| UIManager.cs | All UI panels and HUD |
| UpgradeSystem.cs | 4-step upgrade: Explore -> Refine -> Assemble -> Consecrate |
| SkinManager.cs | Armor tier model swapping |
| ArmorVFX.cs | Tier-based emission and particle effects |
| ParticleManager.cs | VFX pooling and playback |
| AudioManager.cs | Music tier system, SFX playback |
| NetworkManager.cs | TCP client with auto-reconnect |
| LoginManager.cs | Auth, data sync, server communication |
| CameraController.cs | Third-person follow camera with orbit |
| SceneSetup.cs | Auto-init managers and spawners on scene load |
| ObjectPool.cs | Generic object pooling for projectiles/effects |
| ResourceNode.cs | Mining nodes with respawn timer |
| IntroSequence.cs | Story intro with typewriter text |
| SceneLoader.cs | Async loading with progress bar |
| PlayerStats.cs | ScriptableObject: base + tier bonuses |
| EnemyData.cs | ScriptableObject: enemy stats + tier scaling |
| BossData.cs | ScriptableObject: boss stats + phase thresholds |
| SkinBase.cs | ScriptableObject: tier skin data + bonuses |

**Total: 28 scripts + 3 scenes + 2 servers + documentation**

---

## Technical Notes

### Tier Progression Flow
1. Player collects resources from ResourceNodes
2. Player presses U to start upgrade
3. Unity deducts resources immediately (Step 1)
4. Python server runs PyGame mini-game (Steps 2 & 3)
5. Server sends REQUIRE_CONSECRATION to Unity
6. Unity runs Consecration effect (Step 4)
7. Tier bonus stats applied, skin/model changes

### Enemy AI State Machine
```
[Idle] --random timer--> [Patrol]
[Patrol] --player in sight--> [Chase]
[Chase] --player in range--> [Attack]
[Attack] --player out of range--> [Chase]
[Any] --stunned--> [Stunned]
[Any] --HP <= 0--> [Dead]
```

### Boss Phase Transition
- Boss has multiple phases based on health thresholds
- Each phase can change attack speed, damage, or unlock abilities
- Phase transition has invincibility window with visual effect

### ScriptableObject Data Flow
```
PlayerStats (base) --> PlayerController, PlayerCombat, UpgradeSystem
EnemyData --> EnemyController, EnemySpawner
BossData --> BossController, BossSpawner
SkinBase --> SkinManager, ArmorVFX
```
