"""
generate_map.py
===============
Generates a Unity scene (.unity) with an Attack on Titan-style circular map layout.

Layout zones (from center outward):
  Zone 0  (r=0-8)   : Castle / Command Post (thành trì)
  Zone 1  (r=13-18) : Civilian houses (nhà dân)
  Zone 2  (r=22)    : City walls + 4 Gates (tường thành + cổng)
  Zone 3  (r=22-32) : Barracks near gates (trại lính)
  Zone 4  (r=30-52) : Dense outer forest (rừng)

Assets used:
  - Namhansanseong   : fortress buildings & gates
  - Naganeupseong    : civilian & barracks houses
  - Idyllic Fantasy Nature : trees / forest
"""

import math
import random

# ─────────────────────────────────────────────────────────────────────────────
# PREFAB REGISTRY  {name: (guid, root_transform_fileID)}
# ─────────────────────────────────────────────────────────────────────────────
PREFABS = {
    # ── Fortress / Command Center (Namhansanseong) ──────────────────────────
    "Sueojangdae_Command_Post":   ("8bec2394e29223044bc479666dc2d775", "40646826075280227"),
    "Yeonmugwan":                 ("a25ba45e8eff8ef4d8930dd521383c97", "28834942042695749"),
    "Jisudang_Pavilion":          ("d318fbd45cfb11a41b5a9337535b449d", "153090835797451959"),
    "Chimgwaejeong_Pavilion":     ("2b267e22a5493f14ab91852b6763de88", "35364208689073726"),
    # ── Gates ────────────────────────────────────────────────────────────────
    "Jihwa_Gate":                 ("9d777fb7dbbba204f8c9ef447381dd3b", "83512553845868081"),
    "Jeonseung_Gate":             ("0f909d561b0f2a344b514633a2d23a3a", "35052031624075546"),
    "Jwaik_Gate":                 ("8ece2e906cdc56b4b8df88987c6bb1e3", "35364208689073726"),
    "Wuik_Gate":                  ("163940268355a1a478963e8e9e2cd0bb", "35364208689073726"),
    # ── Civilian Houses (Naganeupseong) ───────────────────────────────────────
    "Bamboo_Rafter_House_01":     ("10ee251b6db9534439c03f82ff8c6922", "125655503574000945"),
    "Bamboo_Rafter_House_02":     ("5f28b9fdfb2ca464894ad09692210a7f", "125655503574000945"),
    "House_By_The_West_Gate_01":  ("4f88347865d132f44936c9e4046e515b", "101674288392848386"),
    "House_By_The_West_Gate_02":  ("a9a86a5efeb47c3448d40e9d0e9fabcf", "101674288392848386"),
    "House_With_Calling_Windows_01": ("f8e2c33c967f09e459016706f72db5b1", "125655503574000945"),
    "L_Shaped_House_01":          ("2c0c4f8b96c474e46acacccc4993b379", "12156030760599633"),
    "Local_Clerks_House_01":      ("64dc60584999bb34aa391b40fcb929d0", "51748715808281903"),
    "Smithy":                     ("58938506a334a7f43a2b5ecb3a7eef89", "3857755503734235"),
    "Tavern_House_01":            ("d004a0433e663c244a3232d338bb2f19", "88124691147317831"),
    "Wooden_Bench_House_01":      ("1ca5d127ceca0a840981479166a5d66a", "20105127838861747"),
    "Wooden_Bench_House_02":      ("6eabc5fec89b7904a8d55fc39e624e1a", "3134711275106046926"),
    "Wooden_Bench_House_03":      ("e84e70f27f0bb9a40bab2dd6d2e2ac67", "7803347171265870362"),
    # ── Trees (Idyllic Fantasy Nature) ───────────────────────────────────────
    "BroadleafTree_01_Green":     ("743b50117cc380a4f8f6871e04789159", "2705930986027577080"),
    "BroadleafTree_02_Green":     ("5d6ff116fb5dd6343a053c078b5cdc2b", "2705930986027577080"),
    "Fir_01":                     ("4aa7a928cc83c7d49819fe52f53ec261", "7907867142249649523"),
    "Fir_02":                     ("77e842542dc470c4cacb99376e9140d7", "7907867142249649523"),
    "WillowTree_01_Green":        ("558b79c01c25d53428c6c3656b89375f", "13982906599030212"),
    "BlossomTree_01":             ("fd5ab67aa510888499f842d51f660a4a", "2705930986027577080"),
}

# ─────────────────────────────────────────────────────────────────────────────
# SCENE HEADER
# ─────────────────────────────────────────────────────────────────────────────
SCENE_HEADER = """%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!29 &1
OcclusionCullingSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_OcclusionBakeSettings:
    smallestOccluder: 5
    smallestHole: 0.25
    backfaceThreshold: 100
  m_SceneGUID: 00000000000000000000000000000000
  m_OcclusionCullingData: {fileID: 0}
--- !u!104 &2
RenderSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 9
  m_Fog: 1
  m_FogColor: {r: 0.6, g: 0.75, b: 0.65, a: 1}
  m_FogMode: 3
  m_FogDensity: 0.005
  m_LinearFogStart: 0
  m_LinearFogEnd: 200
  m_AmbientSkyColor: {r: 0.3, g: 0.5, b: 0.35, a: 1}
  m_AmbientEquatorColor: {r: 0.2, g: 0.35, b: 0.25, a: 1}
  m_AmbientGroundColor: {r: 0.1, g: 0.15, b: 0.1, a: 1}
  m_AmbientIntensity: 1
  m_AmbientMode: 0
  m_SubtractiveShadowColor: {r: 0.42, g: 0.478, b: 0.627, a: 1}
  m_SkyboxMaterial: {fileID: 10304, guid: 0000000000000000f000000000000000, type: 0}
  m_HaloStrength: 0.5
  m_FlareStrength: 1
  m_FlareFadeSpeed: 3
  m_HaloTexture: {fileID: 0}
  m_SpotCookie: {fileID: 10001, guid: 0000000000000000e000000000000000, type: 0}
  m_DefaultReflectionMode: 0
  m_DefaultReflectionResolution: 128
  m_ReflectionBounces: 1
  m_ReflectionIntensity: 1
  m_CustomReflection: {fileID: 0}
  m_Sun: {fileID: 900000}
  m_IndirectSpecularColor: {r: 0.1, g: 0.15, b: 0.1, a: 1}
  m_UseRadianceAmbientProbe: 0
--- !u!157 &3
LightmapSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 12
  m_GIWorkflowMode: 1
  m_GISettings:
    serializedVersion: 2
    m_BounceScale: 1
    m_IndirectOutputScale: 1
    m_AlbedoBoost: 1
    m_EnvironmentLightingMode: 0
    m_EnableBakedLightmaps: 0
    m_EnableRealtimeLightmaps: 0
  m_LightmapEditorSettings:
    serializedVersion: 12
    m_Resolution: 2
    m_BakeResolution: 40
    m_AtlasSize: 1024
    m_AO: 0
    m_AOMaxDistance: 1
    m_CompAOExponent: 1
    m_CompAOExponentDirect: 0
    m_ExtractAmbientOcclusion: 0
    m_Padding: 2
    m_LightmapParameters: {fileID: 0}
    m_LightmapsBakeMode: 1
    m_TextureCompression: 1
    m_FinalGather: 0
    m_FinalGatherFiltering: 1
    m_FinalGatherRayCount: 256
    m_ReflectionCompression: 2
    m_MixedBakeMode: 2
    m_BakeBackend: 1
    m_PVRSampling: 1
    m_PVRDirectSampleCount: 32
    m_PVRSampleCount: 512
    m_PVRBounces: 2
    m_PVREnvironmentSampleCount: 256
    m_PVREnvironmentReferencePointCount: 2048
    m_PVRFilteringMode: 1
    m_PVRDenoiserTypeDirect: 1
    m_PVRDenoiserTypeIndirect: 1
    m_PVRDenoiserTypeAO: 1
    m_PVRFilterTypeDirect: 0
    m_PVRFilterTypeIndirect: 0
    m_PVRFilterTypeAO: 0
    m_PVREnvironmentMIS: 1
    m_PVRCulling: 1
    m_PVRFilteringGaussRadiusDirect: 1
    m_PVRFilteringGaussRadiusIndirect: 5
    m_PVRFilteringGaussRadiusAO: 2
    m_PVRFilteringAtrousPositionSigmaDirect: 0.5
    m_PVRFilteringAtrousPositionSigmaIndirect: 2
    m_PVRFilteringAtrousPositionSigmaAO: 1
    m_ExportTrainingData: 0
    m_TrainingDataDestination: TrainingData
    m_LightProbeSampleCountMultiplier: 4
  m_LightingDataAsset: {fileID: 0}
  m_LightingSettings: {fileID: 0}
--- !u!196 &4
NavMeshSettings:
  serializedVersion: 2
  m_ObjectHideFlags: 0
  m_BuildSettings:
    serializedVersion: 3
    agentTypeID: 0
    agentRadius: 0.5
    agentHeight: 2
    agentSlope: 45
    agentClimb: 0.4
    ledgeDropHeight: 0
    maxJumpAcrossDistance: 0
    minRegionArea: 2
    manualCellSize: 0
    cellSize: 0.16666667
    manualTileSize: 0
    tileSize: 256
    buildHeightMesh: 0
    maxJobWorkers: 0
    preserveTilesOutsideBounds: 0
    debug:
      m_Flags: 0
  m_NavMeshData: {fileID: 0}
--- !u!1 &800000
GameObject:
  m_ObjectHideFlags: 0
  serializedVersion: 6
  m_Component:
  - component: {fileID: 800001}
  - component: {fileID: 800002}
  m_Layer: 0
  m_Name: Directional Light
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &800001
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  GameObject: {fileID: 800000}
  m_LocalRotation: {x: 0.40821788, y: -0.23456968, z: 0.10938163, w: 0.8754261}
  m_LocalPosition: {x: 0, y: 30, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 50, y: -30, z: 0}
--- !u!108 &800002
Light:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  GameObject: {fileID: 800000}
  m_Enabled: 1
  serializedVersion: 10
  m_Type: 1
  m_Shape: 0
  m_Color: {r: 1.0, g: 0.95, b: 0.84, a: 1}
  m_Intensity: 1.2
  m_Range: 10
  m_SpotAngle: 30
  m_InnerSpotAngle: 21.80208
  m_CookieSize: 10
  m_Shadows:
    m_Type: 2
    m_Resolution: -1
    m_CustomResolution: -1
    m_Strength: 1
    m_Bias: 0.05
    m_NormalBias: 0.4
    m_NearPlane: 0.2
    m_CullingMatrixOverride:
      e00: 1
      e01: 0
      e02: 0
      e03: 0
      e10: 0
      e11: 1
      e12: 0
      e13: 0
      e20: 0
      e21: 0
      e22: 1
      e23: 0
      e30: 0
      e31: 0
      e32: 0
      e33: 1
    m_UseCullingMatrixOverride: 0
  m_Cookie: {fileID: 0}
  m_DrawHalo: 0
  m_Flare: {fileID: 0}
  m_RenderMode: 0
  m_CullingMask:
    serializedVersion: 2
    m_Bits: 4294967295
  m_RenderingLayerMask: 1
  m_Lightmapping: 4
  m_LightShadowCasterMode: 0
  m_AreaSize: {x: 1, y: 1}
  m_BounceIntensity: 1
  m_ColorTemperature: 6570
  m_UseColorTemperature: 0
  m_BoundingSphereOverride: {x: 0, y: 0, z: 0, w: 0}
  m_UseBoundingSphereOverride: 0
  m_UseViewFrustumForShadowCasterCull: 1
  m_ShadowRadius: 0
  m_ShadowAngle: 0
--- !u!1 &900000
GameObject:
  m_ObjectHideFlags: 0
  serializedVersion: 6
  m_Component:
  - component: {fileID: 900001}
  - component: {fileID: 900002}
  m_Layer: 0
  m_Name: Main Camera
  m_TagString: MainCamera
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &900001
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  GameObject: {fileID: 900000}
  m_LocalRotation: {x: 0.27059805, y: 0, z: 0, w: 0.9626211}
  m_LocalPosition: {x: 0, y: 35, z: -30}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 30, y: 0, z: 0}
--- !u!20 &900002
Camera:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  GameObject: {fileID: 900000}
  m_Enabled: 1
  serializedVersion: 2
  m_ClearFlags: 1
  m_BackGroundColor: {r: 0.19215686, g: 0.3019608, b: 0.4745098, a: 0}
  m_projectionMatrixMode: 1
  m_GateFitMode: 2
  m_FOVAxisMode: 0
  m_Iso: 200
  m_ShutterSpeed: 0.005
  m_Aperture: 16
  m_FocusDistance: 10
  m_FocalLength: 50
  m_BladeCount: 5
  m_Curvature: {x: 2, y: 11}
  m_BarrelClipping: 0.25
  m_Anamorphism: 0
  m_SensorSize: {x: 36, y: 24}
  m_LensShift: {x: 0, y: 0}
  m_NormalizedViewPortRect:
    serializedVersion: 2
    x: 0
    y: 0
    width: 1
    height: 1
  near clip plane: 0.3
  far clip plane: 1000
  field of view: 60
  orthographic: 0
  orthographic size: 5
  m_Depth: -1
  m_CullingMask:
    serializedVersion: 2
    m_Bits: 4294967295
  m_RenderingPath: -1
  m_TargetTexture: {fileID: 0}
  m_TargetDisplay: 0
  m_TargetEye: 3
  m_HDR: 1
  m_AllowMSAA: 1
  m_AllowDynamicResolution: 0
  m_ForceIntoRT: 0
  m_OcclusionCulling: 1
  m_StereoConvergence: 10
  m_StereoSeparation: 0.022
"""

# ─────────────────────────────────────────────────────────────────────────────
# HELPERS
# ─────────────────────────────────────────────────────────────────────────────

def quat_from_euler_y(deg: float):
    """Returns (qx,qy,qz,qw) for a Y-axis rotation."""
    half = math.radians(deg / 2)
    return 0.0, math.sin(half), 0.0, math.cos(half)


def make_prefab_instance(file_id: int, name: str, guid: str,
                         transform_fid: str,
                         x: float, y: float, z: float,
                         rot_y: float = 0.0,
                         scale: float = 1.0) -> str:
    qx, qy, qz, qw = quat_from_euler_y(rot_y)
    return (
        f"--- !u!1001 &{file_id}\n"
        f"PrefabInstance:\n"
        f"  m_ObjectHideFlags: 0\n"
        f"  serializedVersion: 2\n"
        f"  m_Modification:\n"
        f"    serializedVersion: 3\n"
        f"    m_TransformParent: {{fileID: 0}}\n"
        f"    m_Modifications:\n"
        f"    - target: {{fileID: {transform_fid}, guid: {guid}, type: 3}}\n"
        f"      propertyPath: m_LocalPosition.x\n"
        f"      value: {x:.4f}\n"
        f"      objectReference: {{fileID: 0}}\n"
        f"    - target: {{fileID: {transform_fid}, guid: {guid}, type: 3}}\n"
        f"      propertyPath: m_LocalPosition.y\n"
        f"      value: {y:.4f}\n"
        f"      objectReference: {{fileID: 0}}\n"
        f"    - target: {{fileID: {transform_fid}, guid: {guid}, type: 3}}\n"
        f"      propertyPath: m_LocalPosition.z\n"
        f"      value: {z:.4f}\n"
        f"      objectReference: {{fileID: 0}}\n"
        f"    - target: {{fileID: {transform_fid}, guid: {guid}, type: 3}}\n"
        f"      propertyPath: m_LocalRotation.w\n"
        f"      value: {qw:.7f}\n"
        f"      objectReference: {{fileID: 0}}\n"
        f"    - target: {{fileID: {transform_fid}, guid: {guid}, type: 3}}\n"
        f"      propertyPath: m_LocalRotation.x\n"
        f"      value: {qx:.7f}\n"
        f"      objectReference: {{fileID: 0}}\n"
        f"    - target: {{fileID: {transform_fid}, guid: {guid}, type: 3}}\n"
        f"      propertyPath: m_LocalRotation.y\n"
        f"      value: {qy:.7f}\n"
        f"      objectReference: {{fileID: 0}}\n"
        f"    - target: {{fileID: {transform_fid}, guid: {guid}, type: 3}}\n"
        f"      propertyPath: m_LocalRotation.z\n"
        f"      value: {qz:.7f}\n"
        f"      objectReference: {{fileID: 0}}\n"
        f"    - target: {{fileID: {transform_fid}, guid: {guid}, type: 3}}\n"
        f"      propertyPath: m_LocalEulerAnglesHint.x\n"
        f"      value: 0\n"
        f"      objectReference: {{fileID: 0}}\n"
        f"    - target: {{fileID: {transform_fid}, guid: {guid}, type: 3}}\n"
        f"      propertyPath: m_LocalEulerAnglesHint.y\n"
        f"      value: {rot_y:.3f}\n"
        f"      objectReference: {{fileID: 0}}\n"
        f"    - target: {{fileID: {transform_fid}, guid: {guid}, type: 3}}\n"
        f"      propertyPath: m_LocalEulerAnglesHint.z\n"
        f"      value: 0\n"
        f"      objectReference: {{fileID: 0}}\n"
        f"    - target: {{fileID: {transform_fid}, guid: {guid}, type: 3}}\n"
        f"      propertyPath: m_LocalScale.x\n"
        f"      value: {scale:.4f}\n"
        f"      objectReference: {{fileID: 0}}\n"
        f"    - target: {{fileID: {transform_fid}, guid: {guid}, type: 3}}\n"
        f"      propertyPath: m_LocalScale.y\n"
        f"      value: {scale:.4f}\n"
        f"      objectReference: {{fileID: 0}}\n"
        f"    - target: {{fileID: {transform_fid}, guid: {guid}, type: 3}}\n"
        f"      propertyPath: m_LocalScale.z\n"
        f"      value: {scale:.4f}\n"
        f"      objectReference: {{fileID: 0}}\n"
        f"    m_RemovedComponents: []\n"
        f"    m_RemovedGameObjects: []\n"
        f"    m_AddedGameObjects: []\n"
        f"    m_AddedComponents: []\n"
        f"  m_SourcePrefab: {{fileID: 100100000, guid: {guid}, type: 3}}\n"
    )


# ─────────────────────────────────────────────────────────────────────────────
# PLACEMENT LOGIC
# ─────────────────────────────────────────────────────────────────────────────

rng = random.Random(42)
entries: list[str] = []
_fid = 10_000_000

def _fid_next() -> int:
    global _fid
    v = _fid
    _fid += 100_000
    return v

def place(name: str, x: float, z: float, rot_y: float = 0.0, scale: float = 1.0):
    guid, tfid = PREFABS[name]
    entries.append(make_prefab_instance(_fid_next(), name, guid, tfid, x, 0.0, z, rot_y, scale))

def ring_pos(radius: float, angle_deg: float, jitter: float = 0.0):
    rad = math.radians(angle_deg)
    r = radius + rng.uniform(-jitter, jitter)
    return r * math.sin(rad), r * math.cos(rad)

GATE_R = 22          # city-wall gate radius
HOUSE_R_INNER = 13   # inner civilian ring
HOUSE_R_OUTER = 18   # outer civilian ring
FOREST_RINGS = [     # (radius, count, scale_range)
    (30, 36, (0.5, 0.85)),
    (37, 48, (0.65, 1.0)),
    (44, 58, (0.75, 1.15)),
    (51, 55, (0.85, 1.3)),
]

# ── ZONE 0  Castle / Fortress Center ─────────────────────────────────────────
place("Sueojangdae_Command_Post", 0,  0,   0,    0.45)   # main command post
place("Yeonmugwan",              10,  5, 180,   0.38)   # training hall
place("Jisudang_Pavilion",       -7, -6,  90,   0.38)   # pavilion west
place("Chimgwaejeong_Pavilion",  -7,  6,   0,   0.38)   # pavilion NW

# ── ZONE 1  City Wall Gates ───────────────────────────────────────────────────
# South (angle=180 → -Z), North (0 → +Z), East (90 → +X), West (270 → -X)
place("Jihwa_Gate",      0,          -GATE_R, 180, 0.5)   # South gate
place("Jeonseung_Gate",  GATE_R,      0,       270, 0.5)  # East  gate
place("Jwaik_Gate",      0,           GATE_R,    0, 0.5)  # North gate
place("Wuik_Gate",      -GATE_R,      0,        90, 0.5)  # West  gate

# ── ZONE 2  Civilian Houses ───────────────────────────────────────────────────
CIVILIAN_HOUSES = [
    "Bamboo_Rafter_House_01", "Bamboo_Rafter_House_02",
    "House_With_Calling_Windows_01", "L_Shaped_House_01",
    "Local_Clerks_House_01", "Wooden_Bench_House_01",
    "Wooden_Bench_House_02", "Wooden_Bench_House_03",
    "Tavern_House_01", "Smithy",
    "House_By_The_West_Gate_01", "House_By_The_West_Gate_02",
]

def is_near_gate(angle_deg: float, margin: float = 20.0) -> bool:
    """True if angle is within 'margin' degrees of any cardinal gate."""
    for gate_a in (0.0, 90.0, 180.0, 270.0):
        diff = abs(((angle_deg - gate_a) + 180) % 360 - 180)
        if diff < margin:
            return True
    return False

housel_idx = 0
for ring_r, count in [(HOUSE_R_INNER, 14), (HOUSE_R_OUTER, 18)]:
    for i in range(count):
        angle = (360 / count) * i + rng.uniform(-10, 10)
        if is_near_gate(angle, margin=18):
            continue
        tx, tz = ring_pos(ring_r, angle, jitter=1.5)
        hn = CIVILIAN_HOUSES[housel_idx % len(CIVILIAN_HOUSES)]
        rot = angle + 90 + rng.uniform(-25, 25)
        place(hn, tx, tz, rot, scale=rng.uniform(0.55, 0.70))
        housel_idx += 1

# ── ZONE 3  Barracks near Gates ───────────────────────────────────────────────
BARRACKS = ["House_By_The_West_Gate_01", "House_By_The_West_Gate_02",
            "Local_Clerks_House_01"]

# Each gate: 4 barracks buildings fanning out
gate_configs = [
    # (gate_angle_deg, facing_rot)
    (180,  180),   # South
    ( 90,  270),   # East
    (  0,    0),   # North
    (270,   90),   # West
]
for gate_angle, face_rot in gate_configs:
    rad = math.radians(gate_angle)
    # base position just outside gate
    base_x = (GATE_R + 8) * math.sin(rad)
    base_z = (GATE_R + 8) * math.cos(rad)
    # perpendicular direction
    perp_x = math.cos(rad)
    perp_z = -math.sin(rad)
    offsets = [(-6, 0), (6, 0), (-4, 8), (4, 8)]
    for bi, (px, pz_off) in enumerate(offsets):
        bx = base_x + perp_x * px + math.sin(rad) * pz_off
        bz = base_z + perp_z * px + math.cos(rad) * pz_off
        bname = BARRACKS[bi % len(BARRACKS)]
        brot = face_rot + rng.uniform(-15, 15)
        place(bname, bx, bz, brot, scale=rng.uniform(0.5, 0.62))

# ── ZONE 4  Dense Outer Forest ────────────────────────────────────────────────
TREE_TYPES = [
    "BroadleafTree_01_Green", "BroadleafTree_02_Green",
    "Fir_01", "Fir_02", "WillowTree_01_Green", "BlossomTree_01",
]

for fr, fc, (s_min, s_max) in FOREST_RINGS:
    for i in range(fc):
        angle = (360 / fc) * i + rng.uniform(-4, 4)
        tx, tz = ring_pos(fr, angle, jitter=2.5)
        tn = TREE_TYPES[rng.randint(0, len(TREE_TYPES) - 1)]
        place(tn, tx, tz, rot_y=rng.uniform(0, 360), scale=rng.uniform(s_min, s_max))

# ─────────────────────────────────────────────────────────────────────────────
# WRITE OUTPUT
# ─────────────────────────────────────────────────────────────────────────────
output_path = r"e:\PRU213\pru213_game-dev-nhanHuynh\Assets\Map_1\DemoMap_CircularCity.unity"

content = SCENE_HEADER + "\n".join(entries)

with open(output_path, "w", encoding="utf-8", newline="\n") as f:
    f.write(content)

print(f"[OK] Scene written to: {output_path}")
print(f"   Total prefab instances: {len(entries)}")
