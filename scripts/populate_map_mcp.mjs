/**
 * populate_map_mcp.mjs
 * =====================
 * Populates the currently active Unity scene (CircularMap_AoT) with
 * an Attack-on-Titan style circular map layout using MCP batch_execute.
 *
 * Assumes a fresh/empty scene is already open in Unity Editor.
 *
 * Zones:
 *   Zone 0 (r=0–10)  : Thành Trì   – Fortress / Castle Center
 *   Zone 1 (r=13–18) : Nhà Dân     – Civilian Houses
 *   Zone 2 (r=22)    : Cổng Thành  – 4 Gates at cardinal points
 *   Zone 3 (r=22–32) : Trại Lính   – Barracks near each gate
 *   Zone 4 (r=30–52) : Rừng        – Dense outer forest
 */

import { McpUnity } from '../UnityMCP/Server~/build/unity/mcpUnity.js';

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
const items = [];   // { path, x, z, rotY, scale }

function p(path, x, z, rotY = 0, scale = 1) {
  items.push({ path, x, y: 0, z, rotY, scale });
}

const GATE_R   = 22;
const CIVILIANS = [
  A.Bamboo1, A.Bamboo2, A.Bamboo3,
  A.Calling1, A.LShaped1, A.LClerks1, A.Personnel1,
  A.Bench1, A.Bench2, A.Bench3, A.Tavern1, A.Smithy,
];
const BARRACKS  = [A.WGate1, A.WGate2, A.LClerks1, A.Bench1];
const TREES     = [
  A.Broadleaf1, A.Broadleaf2, A.Broadleaf3,
  A.Fir1, A.Fir2, A.Fir3,
  A.Willow1, A.Willow2, A.Blossom1, A.Blossom2,
];

// ── Zone 0: Thành Trì (Castle Center) ────────────────────────────────────────
p(A.CommandPost,  0,    0,   0,  0.50);
p(A.Yeonmugwan,  10,    5, 180,  0.40);
p(A.Jisudang,    -7,   -6,  90,  0.38);
p(A.Chimgwae,    -7,    6,   0,  0.38);
p(A.Pond,         3,   -5, 180,  0.50);

// ── Zone 1: Cổng Thành (4 Gates at cardinal points) ──────────────────────────
p(A.JihwaGate,     0,       -GATE_R, 180, 0.50);  // South
p(A.JeonGate,  GATE_R,           0, 270, 0.50);  // East
p(A.JwaikGate,     0,        GATE_R,   0, 0.50);  // North
p(A.WuikGate, -GATE_R,           0,  90, 0.50);  // West

// ── Zone 2: Nhà Dân (Civilian Houses – two rings) ─────────────────────────────
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

// ── Zone 3: Trại Lính (Barracks near each gate) ───────────────────────────────
for (const { gateAng, faceRot } of [
  { gateAng: 180, faceRot: 180 },  // South gate
  { gateAng:  90, faceRot: 270 },  // East gate
  { gateAng:   0, faceRot:   0 },  // North gate
  { gateAng: 270, faceRot:  90 },  // West gate
]) {
  const rad = gateAng * Math.PI / 180;
  const baseX = (GATE_R + 9) * Math.sin(rad);
  const baseZ = (GATE_R + 9) * Math.cos(rad);
  const perpX = Math.cos(rad);
  const perpZ = -Math.sin(rad);
  for (const [bi, [ox, oz]] of [[-6, 0], [6, 0], [-4, 9], [4, 9]].entries()) {
    p(
      BARRACKS[bi % BARRACKS.length],
      baseX + perpX * ox + Math.sin(rad) * oz,
      baseZ + perpZ * ox + Math.cos(rad) * oz,
      faceRot + rb(-15, 15),
      rb(0.50, 0.62)
    );
  }
}

// ── Zone 4: Rừng (Dense outer forest – 4 rings) ───────────────────────────────
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

logger.info(`Total assets to place: ${items.length}`);

// ─── Helpers ──────────────────────────────────────────────────────────────────
function chunks(arr, size) {
  const out = [];
  for (let i = 0; i < arr.length; i += size) out.push(arr.slice(i, i + size));
  return out;
}

async function sendBatch(mcu, ops) {
  return mcu.sendRequest(
    { method: 'batch_execute', params: { operations: ops, stopOnError: false } },
    { timeout: 120_000 }
  );
}

// ─── Main ─────────────────────────────────────────────────────────────────────
async function main() {
  const mcu = new McpUnity(logger);
  await mcu.start('map-populate');

  if (!mcu.isConnected) {
    logger.error('Cannot connect to Unity MCP!');
    process.exit(1);
  }

  // Verify current scene
  const sceneInfo = await mcu.sendRequest({ method: 'get_scene_info', params: {} });
  logger.info(`Active scene: "${sceneInfo.activeScene?.name}" | objects: ${sceneInfo.activeScene?.rootCount}`);

  // ── Phase A: Add all assets (batch of 20) ──────────────────────────────────
  const BATCH = 20;
  const allChunks = chunks(items, BATCH);
  const placed = [];   // { instanceId, item }
  let addOk = 0, addFail = 0;

  logger.info(`Phase A: Placing ${items.length} assets in ${allChunks.length} batches...`);

  for (let ci = 0; ci < allChunks.length; ci++) {
    const chunk = allChunks[ci];
    const ops = chunk.map((item, i) => ({
      tool: 'add_asset_to_scene',
      id: `add_${ci}_${i}`,
      params: { assetPath: item.path, position: { x: item.x, y: 0, z: item.z } },
    }));

    try {
      const res = await sendBatch(mcu, ops);
      const results = res.results || [];
      for (let i = 0; i < chunk.length; i++) {
        const r = results[i];
        if (r?.success && r.result?.instanceId != null) {
          placed.push({ instanceId: r.result.instanceId, item: chunk[i] });
          addOk++;
        } else {
          addFail++;
        }
      }
      logger.info(`  Batch ${ci+1}/${allChunks.length}: +${res.summary?.succeeded||0} placed, ${res.summary?.failed||0} skipped`);
    } catch (err) {
      logger.error(`  Batch ${ci+1} error: ${err.message}`);
      addFail += chunk.length;
    }
  }

  logger.info(`Phase A done -> placed: ${addOk}, failed: ${addFail}`);

  // ── Phase B: Set rotation + scale (batch of 20) ────────────────────────────
  const xformChunks = chunks(placed, BATCH);
  let xformOk = 0, xformFail = 0;

  logger.info(`Phase B: Setting transforms for ${placed.length} objects in ${xformChunks.length} batches...`);

  for (let ci = 0; ci < xformChunks.length; ci++) {
    const chunk = xformChunks[ci];
    const ops = chunk.map(({ instanceId, item }, i) => ({
      tool: 'set_transform',
      id: `xf_${ci}_${i}`,
      params: {
        instanceId,
        rotation: { x: 0, y: item.rotY, z: 0 },
        scale:    { x: item.scale, y: item.scale, z: item.scale },
        space: 'local',
      },
    }));

    try {
      const res = await sendBatch(mcu, ops);
      xformOk   += res.summary?.succeeded || 0;
      xformFail += res.summary?.failed    || 0;
      logger.info(`  Xform batch ${ci+1}/${xformChunks.length}: ${res.summary?.succeeded||0} ok`);
    } catch (err) {
      logger.error(`  Xform batch ${ci+1} error: ${err.message}`);
      xformFail += chunk.length;
    }
  }

  logger.info(`Phase B done -> xforms set: ${xformOk}, failed: ${xformFail}`);

  // ── Phase C: Save scene ────────────────────────────────────────────────────
  logger.info('Saving scene as CircularMap_AoT...');
  try {
    const saveRes = await mcu.sendRequest(
      { method: 'save_scene', params: {} },
      { timeout: 30_000 }
    );
    logger.info('Saved:', saveRes.message);
  } catch (err) {
    logger.error('Save failed:', err.message);
  }

  await mcu.stop();

  logger.info('');
  logger.info('============================================================');
  logger.info('  MAP BUILD COMPLETE via MCP!');
  logger.info(`  Assets placed:    ${addOk} / ${items.length}`);
  logger.info(`  Transforms set:   ${xformOk} / ${placed.length}`);
  logger.info('  Scene: CircularMap_AoT (in Assets/Map_1/)');
  logger.info('============================================================');
}

main().catch(err => {
  console.error('[FATAL]', err.message || err);
  process.exit(1);
});
