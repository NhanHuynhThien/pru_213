/**
 * build_map_mcp_v2.mjs
 * =====================
 * Builds a circular Attack-on-Titan style map in Unity Editor via MCP.
 *
 * Strategy:
 *  1. Load existing empty-ish scene (or create fresh via YAML already done)
 *  2. For each asset: add_asset_to_scene -> get instanceId -> set_transform
 *  3. Use batch_execute (max 100 ops) to minimize round-trips
 *  4. save_scene at the end
 *
 * Zones:
 *   Zone 0 (r=0–10)  : Thành Trì   – Fortress / Castle Center
 *   Zone 1 (r=13–18) : Nhà Dân     – Civilian Houses
 *   Zone 2 (r=22)    : Cổng Thành  – 4 Gates at cardinal points
 *   Zone 3 (r=22–32) : Trại Lính   – Barracks near each gate
 *   Zone 4 (r=30–52) : Rừng        – Dense outer forest
 */

import { McpUnity } from '../UnityMCP/Server~/build/unity/mcpUnity.js';

// ─── Logger ───────────────────────────────────────────────────────────────────
const logger = {
  info:  (m, ...a) => console.log(`[INFO]  ${m}`, ...a),
  warn:  (m, ...a) => console.warn(`[WARN]  ${m}`, ...a),
  error: (m, ...a) => console.error(`[ERROR] ${m}`, ...a),
  debug: () => {},
};

// ─── Asset paths ──────────────────────────────────────────────────────────────
const A = {
  // Fortress (Namhansanseong)
  CommandPost:  'Assets/Namhansanseong/Prefabs/Buildings/Sueojangdae_Command_Post.prefab',
  Yeonmugwan:   'Assets/Namhansanseong/Prefabs/Buildings/Yeonmugwan.prefab',
  Jisudang:     'Assets/Namhansanseong/Prefabs/Buildings/Jisudang_Pavilion.prefab',
  Chimgwae:     'Assets/Namhansanseong/Prefabs/Buildings/Chimgwaejeong_Pavilion.prefab',
  Pond:         'Assets/Namhansanseong/Prefabs/Buildings/Pond.prefab',
  // Gates (Namhansanseong)
  JihwaGate:    'Assets/Namhansanseong/Prefabs/Buildings/Jihwa_Gate.prefab',
  JeonGate:     'Assets/Namhansanseong/Prefabs/Buildings/Jeonseung_Gate.prefab',
  JwaikGate:    'Assets/Namhansanseong/Prefabs/Buildings/Jwaik_Gate.prefab',
  WuikGate:     'Assets/Namhansanseong/Prefabs/Buildings/Wuik_Gate.prefab',
  // Civilian houses (Naganeupseong)
  Bamboo1:      'Assets/Naganeupseong/Prefabs/Build/Bamboo_Rafter_House_01.prefab',
  Bamboo2:      'Assets/Naganeupseong/Prefabs/Build/Bamboo_Rafter_House_02.prefab',
  Bamboo3:      'Assets/Naganeupseong/Prefabs/Build/Bamboo_Rafter_House_03.prefab',
  WGate1:       'Assets/Naganeupseong/Prefabs/Build/House_By_The_West_Gate_01.prefab',
  WGate2:       'Assets/Naganeupseong/Prefabs/Build/House_By_The_West_Gate_02.prefab',
  Calling1:     'Assets/Naganeupseong/Prefabs/Build/House_With_Calling_Windows_01.prefab',
  LShaped1:     'Assets/Naganeupseong/Prefabs/Build/L_Shaped_House_01.prefab',
  LClerks1:     'Assets/Naganeupseong/Prefabs/Build/Local_Clerks_House_01.prefab',
  Personnel1:   'Assets/Naganeupseong/Prefabs/Build/Local_Personnel_Clerks_House_01.prefab',
  Smithy:       'Assets/Naganeupseong/Prefabs/Build/Smithy.prefab',
  Tavern1:      'Assets/Naganeupseong/Prefabs/Build/Tavern_House_01.prefab',
  Bench1:       'Assets/Naganeupseong/Prefabs/Build/Wooden_Bench_House_01.prefab',
  Bench2:       'Assets/Naganeupseong/Prefabs/Build/Wooden_Bench_House_02.prefab',
  Bench3:       'Assets/Naganeupseong/Prefabs/Build/Wooden_Bench_House_03.prefab',
  // Trees (Idyllic Fantasy Nature)
  Broadleaf1:   'Assets/Idyllic Fantasy Nature/Prefabs/BroadleafTree_01_Green.prefab',
  Broadleaf2:   'Assets/Idyllic Fantasy Nature/Prefabs/BroadleafTree_02_Green.prefab',
  Broadleaf3:   'Assets/Idyllic Fantasy Nature/Prefabs/BroadleafTree_03_Green.prefab',
  Fir1:         'Assets/Idyllic Fantasy Nature/Prefabs/Fir_01.prefab',
  Fir2:         'Assets/Idyllic Fantasy Nature/Prefabs/Fir_02.prefab',
  Fir3:         'Assets/Idyllic Fantasy Nature/Prefabs/Fir_03.prefab',
  Willow1:      'Assets/Idyllic Fantasy Nature/Prefabs/WillowTree_01_Green.prefab',
  Willow2:      'Assets/Idyllic Fantasy Nature/Prefabs/WillowTree_02_Green.prefab',
  Blossom1:     'Assets/Idyllic Fantasy Nature/Prefabs/BlossomTree_01.prefab',
  Blossom2:     'Assets/Idyllic Fantasy Nature/Prefabs/BlossomTree_02.prefab',
};

// ─── Seeded RNG ───────────────────────────────────────────────────────────────
function makeRng(seed) {
  let s = seed >>> 0;
  return () => { s = (Math.imul(1664525, s) + 1013904223) >>> 0; return s / 0x100000000; };
}
const rng = makeRng(54321);
const rb = (lo, hi) => lo + rng() * (hi - lo);

function ringXZ(r, angleDeg, jitter = 0) {
  const rad = angleDeg * Math.PI / 180;
  const rv = r + (jitter > 0 ? rb(-jitter, jitter) : 0);
  return { x: rv * Math.sin(rad), z: rv * Math.cos(rad) };
}

function nearGate(angleDeg, margin = 20) {
  for (const g of [0, 90, 180, 270]) {
    if (Math.abs(((angleDeg - g + 180 + 360) % 360) - 180) < margin) return true;
  }
  return false;
}

// ─── Build placement list ─────────────────────────────────────────────────────
// Each entry: { path, x, z, rotY, scale }
const items = [];

function p(path, x, z, rotY = 0, scale = 1) {
  items.push({ path, x, y: 0, z, rotY, scale });
}

const GATE_R = 22;
const CIVILIANS = [
  A.Bamboo1, A.Bamboo2, A.Bamboo3,
  A.Calling1, A.LShaped1, A.LClerks1, A.Personnel1,
  A.Bench1, A.Bench2, A.Bench3,
  A.Tavern1, A.Smithy,
];
const BARRACKS = [A.WGate1, A.WGate2, A.LClerks1, A.Bench1];
const TREES = [
  A.Broadleaf1, A.Broadleaf2, A.Broadleaf3,
  A.Fir1, A.Fir2, A.Fir3,
  A.Willow1, A.Willow2,
  A.Blossom1, A.Blossom2,
];

// Zone 0 – Castle
p(A.CommandPost,  0,    0,   0,  0.50);
p(A.Yeonmugwan,  10,    5, 180,  0.38);
p(A.Jisudang,    -7,   -6,  90,  0.38);
p(A.Chimgwae,    -7,    6,   0,  0.38);
p(A.Pond,         3,   -5, 180,  0.50);

// Zone 1 – Gates (4 cardinal points)
p(A.JihwaGate,     0,       -GATE_R, 180, 0.50);
p(A.JeonGate,  GATE_R,           0, 270, 0.50);
p(A.JwaikGate,     0,        GATE_R,   0, 0.50);
p(A.WuikGate, -GATE_R,           0,  90, 0.50);

// Zone 2 – Civilian houses (two rings, avoid gate corridors)
let hi = 0;
for (const [ringR, cnt] of [[13, 14], [18, 18]]) {
  for (let i = 0; i < cnt; i++) {
    const ang = (360 / cnt) * i + rb(-10, 10);
    if (nearGate(ang, 18)) continue;
    const { x, z } = ringXZ(ringR, ang, 1.5);
    p(CIVILIANS[hi % CIVILIANS.length], x, z, ang + 90 + rb(-25, 25), rb(0.55, 0.70));
    hi++;
  }
}

// Zone 3 – Barracks near gates
for (const { ang, face } of [
  { ang: 180, face: 180 }, { ang: 90, face: 270 },
  { ang: 0,   face: 0   }, { ang: 270, face: 90 },
]) {
  const rad = ang * Math.PI / 180;
  const bx = (GATE_R + 9) * Math.sin(rad);
  const bz = (GATE_R + 9) * Math.cos(rad);
  const px = Math.cos(rad), pz = -Math.sin(rad);
  for (const [bi, [ox, oz]] of [[-6, 0], [6, 0], [-4, 9], [4, 9]].entries()) {
    p(
      BARRACKS[bi % BARRACKS.length],
      bx + px * ox + Math.sin(rad) * oz,
      bz + pz * ox + Math.cos(rad) * oz,
      face + rb(-15, 15),
      rb(0.50, 0.62)
    );
  }
}

// Zone 4 – Dense outer forest (4 rings)
for (const [fr, fc, slo, shi] of [
  [30, 36, 0.50, 0.85],
  [37, 48, 0.65, 1.00],
  [44, 58, 0.75, 1.15],
  [51, 55, 0.85, 1.30],
]) {
  for (let i = 0; i < fc; i++) {
    const ang = (360 / fc) * i + rb(-4, 4);
    const { x, z } = ringXZ(fr, ang, 2.5);
    p(TREES[Math.floor(rng() * TREES.length)], x, z, rb(0, 360), rb(slo, shi));
  }
}

logger.info(`Total items to place: ${items.length}`);

// ─── MCP helpers ──────────────────────────────────────────────────────────────

async function sendBatch(mcu, ops, stopOnError = false) {
  return mcu.sendRequest({
    method: 'batch_execute',
    params: { operations: ops, stopOnError },
  }, { timeout: 120_000 });
}

// Split array into chunks of given size
function chunks(arr, size) {
  const out = [];
  for (let i = 0; i < arr.length; i += size) out.push(arr.slice(i, i + size));
  return out;
}

// ─── Main ─────────────────────────────────────────────────────────────────────
async function main() {
  const mcu = new McpUnity(logger);
  await mcu.start('map-builder-v2');

  if (!mcu.isConnected) {
    logger.error('Cannot connect to Unity MCP. Make sure Unity Editor is open and MCP server is running.');
    process.exit(1);
  }
  logger.info('Connected to Unity MCP.');

  // ── Step 1: Load our pre-built YAML scene to get a clean canvas ─────────────
  logger.info('Loading scene: DemoMap_CircularCity ...');
  try {
    const loadRes = await mcu.sendRequest({
      method: 'load_scene',
      params: { scenePath: 'Assets/Map_1/DemoMap_CircularCity.unity' },
    }, { timeout: 30_000 });
    logger.info('Scene loaded:', loadRes.message);
  } catch (err) {
    logger.warn(`load_scene failed (${err.message}), trying to create fresh scene...`);
    // Fallback: use menu item to create new scene without dialog
    try {
      await mcu.sendRequest({
        method: 'execute_menu_item',
        params: { menuPath: 'File/New Scene' },
      }, { timeout: 15_000 });
      logger.info('New scene created via menu.');
    } catch (e2) {
      logger.warn(`menu item also failed: ${e2.message}. Continuing anyway...`);
    }
  }

  // ── Step 2: Place all assets using batch_execute (max 20 per batch) ──────────
  // Each batch: add asset -> immediately set_transform using instanceId from result
  // Since batch_execute executes sequentially, we need two approaches:
  //   a) add_asset_to_scene returns instanceId
  //   b) then set_transform uses that instanceId
  // We can't chain results inside a single batch, so:
  //   - Do add_asset_to_scene in one batch, collect instanceIds
  //   - Then do set_transform in next batch using those instanceIds

  const BATCH_SIZE = 20;
  const allChunks = chunks(items, BATCH_SIZE);

  let totalSuccess = 0;
  let totalFailed = 0;
  const instanceIds = []; // instanceId for each placed item

  logger.info(`Placing ${items.length} assets in ${allChunks.length} batches (${BATCH_SIZE} per batch)...`);

  // Phase A: add_asset_to_scene in batches
  for (let ci = 0; ci < allChunks.length; ci++) {
    const chunk = allChunks[ci];
    const ops = chunk.map((item, i) => ({
      tool: 'add_asset_to_scene',
      id: `add_${ci}_${i}`,
      params: {
        assetPath: item.path,
        position: { x: item.x, y: item.y, z: item.z },
      },
    }));

    try {
      const res = await sendBatch(mcu, ops, false);
      const results = res.results || [];

      for (let i = 0; i < results.length; i++) {
        const r = results[i];
        if (r.success && r.result?.instanceId != null) {
          instanceIds.push({ instanceId: r.result.instanceId, item: chunk[i] });
          totalSuccess++;
        } else {
          instanceIds.push(null); // placeholder
          totalFailed++;
          if (totalFailed <= 5) {
            logger.warn(`Failed to place ${chunk[i]?.path?.split('/').pop()}: ${r.error || 'unknown'}`);
          }
        }
      }

      logger.info(`  Batch ${ci + 1}/${allChunks.length}: ${res.summary?.succeeded || 0} placed, ${res.summary?.failed || 0} failed`);
    } catch (err) {
      logger.error(`  Batch ${ci + 1} failed entirely: ${err.message}`);
      // Push nulls for all items in this chunk
      for (let i = 0; i < chunk.length; i++) instanceIds.push(null);
    }
  }

  logger.info(`Phase A complete. Placed: ${totalSuccess}, Failed: ${totalFailed}`);

  // Phase B: set_transform for all successfully placed objects
  logger.info('Setting rotations and scales...');
  const validInstances = instanceIds.filter(Boolean);
  const transformChunks = chunks(validInstances, BATCH_SIZE);
  let transformSuccess = 0, transformFailed = 0;

  for (let ci = 0; ci < transformChunks.length; ci++) {
    const chunk = transformChunks[ci];
    const ops = chunk.map(({ instanceId, item }, i) => ({
      tool: 'set_transform',
      id: `xform_${ci}_${i}`,
      params: {
        instanceId,
        rotation: { x: 0, y: item.rotY, z: 0 },
        scale:    { x: item.scale, y: item.scale, z: item.scale },
        space: 'local',
      },
    }));

    try {
      const res = await sendBatch(mcu, ops, false);
      transformSuccess += res.summary?.succeeded || 0;
      transformFailed  += res.summary?.failed    || 0;
      logger.info(`  Transform batch ${ci + 1}/${transformChunks.length}: ${res.summary?.succeeded || 0} ok`);
    } catch (err) {
      logger.error(`  Transform batch ${ci + 1} failed: ${err.message}`);
    }
  }

  logger.info(`Phase B complete. Transforms set: ${transformSuccess}, Failed: ${transformFailed}`);

  // ── Step 3: Save the scene ───────────────────────────────────────────────────
  logger.info('Saving scene...');
  try {
    const saveRes = await mcu.sendRequest({
      method: 'save_scene',
      params: {},
    }, { timeout: 30_000 });
    logger.info('Scene saved:', saveRes.message);
  } catch (err) {
    logger.error('Save scene failed:', err.message);
  }

  await mcu.stop();

  logger.info('');
  logger.info('=== DONE ===');
  logger.info(`Assets placed:    ${totalSuccess}/${items.length}`);
  logger.info(`Transforms set:   ${transformSuccess}/${validInstances.length}`);
  logger.info('Open "Assets/Map_1/DemoMap_CircularCity.unity" in Unity Editor to see the map.');
}

main().catch(err => {
  console.error('[FATAL]', err.message || err);
  process.exit(1);
});
