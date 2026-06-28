/**
 * build_map_mcp.mjs
 * ==================
 * Uses Unity MCP to build a circular Attack-on-Titan style map directly
 * inside Unity Editor via WebSocket.
 *
 * Zones:
 *   Zone 0  (r=0-10)   : Thành trì - Castle Center
 *   Zone 1  (r=13-18)  : Nhà Dân   - Civilian Houses
 *   Zone 2  (r=22)     : Cổng Thành - 4 Gates
 *   Zone 3  (r=22-32)  : Trại Lính  - Barracks near gates
 *   Zone 4  (r=30-52)  : Rừng       - Dense Forest
 */

import { McpUnity } from '../UnityMCP/Server~/build/unity/mcpUnity.js';

// ─── Logger ───────────────────────────────────────────────────────────────────
const logger = {
  info: (msg, ...a) => console.log(`[INFO]  ${msg}`, ...a),
  warn: (msg, ...a) => console.warn(`[WARN]  ${msg}`, ...a),
  error: (msg, ...a) => console.error(`[ERROR] ${msg}`, ...a),
  debug: () => {},
};

// ─── Asset Paths ──────────────────────────────────────────────────────────────
const ASSETS = {
  // Castle / Fortress (Namhansanseong)
  CommandPost:     'Assets/Namhansanseong/Prefabs/Buildings/Sueojangdae_Command_Post.prefab',
  Yeonmugwan:      'Assets/Namhansanseong/Prefabs/Buildings/Yeonmugwan.prefab',
  JisudangPav:     'Assets/Namhansanseong/Prefabs/Buildings/Jisudang_Pavilion.prefab',
  ChimgwaePav:     'Assets/Namhansanseong/Prefabs/Buildings/Chimgwaejeong_Pavilion.prefab',
  Pond:            'Assets/Namhansanseong/Prefabs/Buildings/Pond.prefab',

  // Gates (Namhansanseong)
  JihwaGate:       'Assets/Namhansanseong/Prefabs/Buildings/Jihwa_Gate.prefab',
  JeonseungGate:   'Assets/Namhansanseong/Prefabs/Buildings/Jeonseung_Gate.prefab',
  JwaikGate:       'Assets/Namhansanseong/Prefabs/Buildings/Jwaik_Gate.prefab',
  WuikGate:        'Assets/Namhansanseong/Prefabs/Buildings/Wuik_Gate.prefab',

  // Civilian Houses (Naganeupseong)
  BambooHouse1:    'Assets/Naganeupseong/Prefabs/Build/Bamboo_Rafter_House_01.prefab',
  BambooHouse2:    'Assets/Naganeupseong/Prefabs/Build/Bamboo_Rafter_House_02.prefab',
  BambooHouse3:    'Assets/Naganeupseong/Prefabs/Build/Bamboo_Rafter_House_03.prefab',
  WestGateHouse1:  'Assets/Naganeupseong/Prefabs/Build/House_By_The_West_Gate_01.prefab',
  WestGateHouse2:  'Assets/Naganeupseong/Prefabs/Build/House_By_The_West_Gate_02.prefab',
  CallingWindows1: 'Assets/Naganeupseong/Prefabs/Build/House_With_Calling_Windows_01.prefab',
  CallingWindows2: 'Assets/Naganeupseong/Prefabs/Build/House_With_Calling_Windows_02.prefab',
  LShapedHouse1:   'Assets/Naganeupseong/Prefabs/Build/L_Shaped_House_01.prefab',
  LocalClerks1:    'Assets/Naganeupseong/Prefabs/Build/Local_Clerks_House_01.prefab',
  LocalPersonnel1: 'Assets/Naganeupseong/Prefabs/Build/Local_Personnel_Clerks_House_01.prefab',
  Smithy:          'Assets/Naganeupseong/Prefabs/Build/Smithy.prefab',
  Tavern1:         'Assets/Naganeupseong/Prefabs/Build/Tavern_House_01.prefab',
  WoodenBench1:    'Assets/Naganeupseong/Prefabs/Build/Wooden_Bench_House_01.prefab',
  WoodenBench2:    'Assets/Naganeupseong/Prefabs/Build/Wooden_Bench_House_02.prefab',
  WoodenBench3:    'Assets/Naganeupseong/Prefabs/Build/Wooden_Bench_House_03.prefab',
  Watermill:       'Assets/Naganeupseong/Prefabs/Build/Watermill.prefab',

  // Trees (Idyllic Fantasy Nature)
  BroadleafGreen1: 'Assets/Idyllic Fantasy Nature/Prefabs/BroadleafTree_01_Green.prefab',
  BroadleafGreen2: 'Assets/Idyllic Fantasy Nature/Prefabs/BroadleafTree_02_Green.prefab',
  BroadleafGreen3: 'Assets/Idyllic Fantasy Nature/Prefabs/BroadleafTree_03_Green.prefab',
  Fir1:            'Assets/Idyllic Fantasy Nature/Prefabs/Fir_01.prefab',
  Fir2:            'Assets/Idyllic Fantasy Nature/Prefabs/Fir_02.prefab',
  Fir3:            'Assets/Idyllic Fantasy Nature/Prefabs/Fir_03.prefab',
  Willow1:         'Assets/Idyllic Fantasy Nature/Prefabs/WillowTree_01_Green.prefab',
  Willow2:         'Assets/Idyllic Fantasy Nature/Prefabs/WillowTree_02_Green.prefab',
  Blossom1:        'Assets/Idyllic Fantasy Nature/Prefabs/BlossomTree_01.prefab',
  Blossom2:        'Assets/Idyllic Fantasy Nature/Prefabs/BlossomTree_02.prefab',
  Rock1:           'Assets/Idyllic Fantasy Nature/Prefabs/Rock_Big_01.prefab',
  Cliff1:          'Assets/Idyllic Fantasy Nature/Prefabs/Cliff_01.prefab',
};

// ─── Math helpers ─────────────────────────────────────────────────────────────
function seededRandom(seed) {
  let s = seed;
  return () => {
    s = (s * 1664525 + 1013904223) & 0xffffffff;
    return (s >>> 0) / 0xffffffff;
  };
}
const rand = seededRandom(12345);

function randBetween(min, max) {
  return min + rand() * (max - min);
}

function ringPos(radius, angleDeg, jitter = 0) {
  const rad = (angleDeg * Math.PI) / 180;
  const r = radius + (jitter > 0 ? randBetween(-jitter, jitter) : 0);
  return { x: r * Math.sin(rad), z: r * Math.cos(rad) };
}

function isNearGate(angleDeg, margin = 20) {
  for (const gate of [0, 90, 180, 270]) {
    const diff = Math.abs(((angleDeg - gate + 180 + 360) % 360) - 180);
    if (diff < margin) return true;
  }
  return false;
}

// ─── Placement plan ───────────────────────────────────────────────────────────
const GATE_R = 22;

const CIVILIAN_HOUSES = [
  ASSETS.BambooHouse1, ASSETS.BambooHouse2, ASSETS.BambooHouse3,
  ASSETS.CallingWindows1, ASSETS.CallingWindows2,
  ASSETS.LShapedHouse1,
  ASSETS.LocalClerks1, ASSETS.LocalPersonnel1,
  ASSETS.WoodenBench1, ASSETS.WoodenBench2, ASSETS.WoodenBench3,
  ASSETS.Tavern1, ASSETS.Smithy,
];

const BARRACKS = [
  ASSETS.WestGateHouse1, ASSETS.WestGateHouse2,
  ASSETS.LocalClerks1, ASSETS.WoodenBench1,
];

const TREE_TYPES = [
  ASSETS.BroadleafGreen1, ASSETS.BroadleafGreen2, ASSETS.BroadleafGreen3,
  ASSETS.Fir1, ASSETS.Fir2, ASSETS.Fir3,
  ASSETS.Willow1, ASSETS.Willow2,
  ASSETS.Blossom1, ASSETS.Blossom2,
];

// Build placements array: { assetPath, x, y, z, rotY, scale }
const placements = [];

function place(assetPath, x, z, rotY = 0, scale = 1.0) {
  placements.push({ assetPath, x, y: 0, z, rotY, scale });
}

// ── ZONE 0: Castle Center ─────────────────────────────────────────────────────
place(ASSETS.CommandPost,  0,   0,   0,   0.5);
place(ASSETS.Yeonmugwan,  10,   5, 180,   0.4);
place(ASSETS.JisudangPav, -7,  -6,  90,   0.4);
place(ASSETS.ChimgwaePav, -7,   6,   0,   0.4);
place(ASSETS.Pond,         3,  -5, 180,   0.5);

// ── ZONE 1: City Gates ────────────────────────────────────────────────────────
place(ASSETS.JihwaGate,     0,         -GATE_R, 180, 0.5);
place(ASSETS.JeonseungGate, GATE_R,     0,      270, 0.5);
place(ASSETS.JwaikGate,     0,          GATE_R,   0, 0.5);
place(ASSETS.WuikGate,     -GATE_R,     0,       90, 0.5);

// ── ZONE 2: Civilian Houses (2 rings, skip near gates) ────────────────────────
let houseIdx = 0;
for (const [ringR, count] of [[13, 14], [18, 18]]) {
  for (let i = 0; i < count; i++) {
    const angle = (360 / count) * i + randBetween(-10, 10);
    if (isNearGate(angle, 18)) continue;
    const { x, z } = ringPos(ringR, angle, 1.5);
    const hn = CIVILIAN_HOUSES[houseIdx % CIVILIAN_HOUSES.length];
    const rot = angle + 90 + randBetween(-25, 25);
    place(hn, x, z, rot, randBetween(0.55, 0.70));
    houseIdx++;
  }
}

// ── ZONE 3: Barracks near each gate ───────────────────────────────────────────
const gateConfigs = [
  { angle: 180, face: 180 },  // South
  { angle:  90, face: 270 },  // East
  { angle:   0, face:   0 },  // North
  { angle: 270, face:  90 },  // West
];

for (const { angle, face } of gateConfigs) {
  const rad = (angle * Math.PI) / 180;
  const baseX = (GATE_R + 9) * Math.sin(rad);
  const baseZ = (GATE_R + 9) * Math.cos(rad);
  const perpX = Math.cos(rad);
  const perpZ = -Math.sin(rad);

  const offsets = [[-6, 0], [6, 0], [-4, 9], [4, 9]];
  for (let bi = 0; bi < offsets.length; bi++) {
    const [px, pzOff] = offsets[bi];
    const bx = baseX + perpX * px + Math.sin(rad) * pzOff;
    const bz = baseZ + perpZ * px + Math.cos(rad) * pzOff;
    const bname = BARRACKS[bi % BARRACKS.length];
    place(bname, bx, bz, face + randBetween(-15, 15), randBetween(0.5, 0.62));
  }
}

// ── ZONE 4: Dense Outer Forest ────────────────────────────────────────────────
const forestRings = [
  { r: 30, count: 36, sMin: 0.5,  sMax: 0.85 },
  { r: 37, count: 48, sMin: 0.65, sMax: 1.0  },
  { r: 44, count: 58, sMin: 0.75, sMax: 1.15 },
  { r: 51, count: 55, sMin: 0.85, sMax: 1.3  },
];

for (const { r, count, sMin, sMax } of forestRings) {
  for (let i = 0; i < count; i++) {
    const angle = (360 / count) * i + randBetween(-4, 4);
    const { x, z } = ringPos(r, angle, 2.5);
    const tn = TREE_TYPES[Math.floor(rand() * TREE_TYPES.length)];
    place(tn, x, z, randBetween(0, 360), randBetween(sMin, sMax));
  }
}

console.log(`[INFO]  Total placements planned: ${placements.length}`);

// ─── Main: connect to Unity MCP and execute ───────────────────────────────────
async function main() {
  const mcu = new McpUnity(logger);
  await mcu.start('map-builder');

  if (!mcu.isConnected) {
    console.error('[ERROR] Could not connect to Unity MCP. Make sure Unity Editor is open.');
    process.exit(1);
  }

  console.log('[INFO]  Connected to Unity MCP WebSocket.');

  // 1. Create a fresh scene
  console.log('[INFO]  Creating new scene: CircularMap_AoT ...');
  const createRes = await mcu.sendRequest({
    method: 'create_scene',
    params: {
      sceneName: 'CircularMap_AoT',
      folderPath: 'Map_1',
      makeActive: true,
      addToBuildSettings: false,
    },
  });
  console.log('[INFO]  Scene created:', createRes.message || createRes);

  // 2. Add all assets one by one
  let success = 0, failed = 0;
  for (let i = 0; i < placements.length; i++) {
    const p = placements[i];
    try {
      const res = await mcu.sendRequest({
        method: 'add_asset_to_scene',
        params: {
          assetPath: p.assetPath,
          position: { x: p.x, y: p.y, z: p.z },
        },
      });

      // After placing, set rotation and scale via update_transform if we got a gameObjectPath back
      // The response should contain the instance path
      success++;
      if (success % 20 === 0) {
        console.log(`[INFO]  Progress: ${success}/${placements.length} placed (${failed} failed)`);
      }
    } catch (err) {
      failed++;
      if (failed <= 10) {
        console.warn(`[WARN]  Failed to place ${p.assetPath.split('/').pop()}: ${err.message}`);
      }
    }
  }

  console.log(`[INFO]  Placement done. Success: ${success}, Failed: ${failed}`);

  // 3. Save scene
  console.log('[INFO]  Saving scene...');
  const saveRes = await mcu.sendRequest({
    method: 'save_scene',
    params: {},
  });
  console.log('[INFO]  Scene saved:', saveRes.message || saveRes);

  await mcu.stop();
  console.log('[INFO]  Done! Open CircularMap_AoT in Unity Editor to view the map.');
}

main().catch(err => {
  console.error('[FATAL]', err);
  process.exit(1);
});
