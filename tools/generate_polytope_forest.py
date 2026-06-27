#!/usr/bin/env python3
"""Generates PolytopeForestLayout.prefab constrained to the flat playable area."""

from __future__ import annotations

import math
import random
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PREFAB_DIR = ROOT / "Assets/Polytope Studio/Lowpoly_Environments/Prefabs"
OUTPUT = ROOT / "Assets/Game/Gameplay/_Scenes/Prefabs/PolytopeForestLayout.prefab"

MIN_X = -8.0
MAX_X = 50.0
MIN_Z = -36.0
MAX_Z = 2.0
CELL_SIZE = 4.5
SPAWN = (7.0, -27.0)
SPAWN_CLEAR = 3.2
CORRIDOR_END = (14.0, -18.0)
CORRIDOR_CLEAR = 2.6

ZONES = (
    ("south", -36.0, -19.0, 38),
    ("mid", -19.0, -10.0, 32),
    ("north", -10.0, 2.0, 30),
)

WEIGHTED = [
    ("Trees/PT_Pine_Tree_03_green.prefab", 10),
    ("Trees/PT_Fruit_Tree_01_green.prefab", 8),
    ("Trees/PT_Fruit_Tree_01_apples.prefab", 4),
    ("Trees/PT_Fruit_Tree_01_pears.prefab", 4),
    ("Trees/PT_Fruit_Tree_01_plums.prefab", 4),
    ("Trees/PT_Pine_Tree_03_dead.prefab", 3),
    ("Trees/PT_Fruit_Tree_01_dead.prefab", 3),
    ("Shrubs/PT_Generic_Shrub_01_green.prefab", 8),
    ("Shrubs/PT_Generic_Shrub_01_dead.prefab", 5),
    ("Plants/PT_Grass_02.prefab", 2),
    ("Flowers/PT_Poppy_02.prefab", 2),
    ("Rocks/PT_Generic_Rock_01.prefab", 5),
    ("Rocks/PT_River_Rock_Pile_02.prefab", 3),
    ("Trees/PT_Pine_Tree_03_stump.prefab", 3),
    ("Trees/PT_Fruit_Tree_01_stump.prefab", 3),
    ("Trees/PT_Pine_Tree_03_logs.prefab", 2),
    ("Trees/PT_Fruit_Tree_01_logs.prefab", 2),
]

FOREST_COLLIDER_GUID = "d4e5f6a7b8091726354a8b9c0d1e2f3a"
FOREST_FILTER_GUID = "a8b9c0d1e2f3a4b5c6d7e8091726354a"


def read_guid(meta_path: Path) -> str:
    text = meta_path.read_text(encoding="utf-8")
    match = re.search(r"^guid: ([0-9a-f]+)", text, re.MULTILINE)
    if not match:
        raise ValueError(f"No guid in {meta_path}")
    return match.group(1)


def read_root_transform_id(prefab_path: Path) -> int:
    text = prefab_path.read_text(encoding="utf-8")
    blocks = text.split("--- !u!4 ")
    for block in blocks[1:]:
        if "m_Father: {fileID: 0}" in block:
            first_line = block.splitlines()[0]
            return int(first_line.split("&", 1)[1].strip())
    raise ValueError(f"No root transform in {prefab_path}")


def build_pool() -> list[tuple[str, int]]:
    pool: list[tuple[str, int]] = []
    for rel, weight in WEIGHTED:
        prefab_path = PREFAB_DIR / rel
        if not prefab_path.exists():
            print(f"skip missing {rel}")
            continue
        guid = read_guid(prefab_path.with_suffix(prefab_path.suffix + ".meta"))
        root_id = read_root_transform_id(prefab_path)
        for _ in range(max(1, weight)):
            pool.append((guid, root_id))
    return pool


def dist(a, b):
    return math.hypot(a[0] - b[0], a[1] - b[1])


def dist_segment(p, a, b):
    ax, az = a
    bx, bz = b
    px, pz = p
    abx, abz = bx - ax, bz - az
    t = ((px - ax) * abx + (pz - az) * abz) / (abx * abx + abz * abz + 1e-6)
    t = max(0.0, min(1.0, t))
    cx, cz = ax + abx * t, az + abz * t
    return math.hypot(px - cx, pz - cz)


def is_clear_zone(x: float, z: float) -> bool:
    if dist((x, z), SPAWN) < SPAWN_CLEAR:
        return True
    if dist_segment((x, z), SPAWN, CORRIDOR_END) < CORRIDOR_CLEAR:
        return True
    return False


def zone_cells(z_min: float, z_max: float) -> list[tuple[float, float]]:
    cells = []
    x = MIN_X + CELL_SIZE * 0.5
    while x < MAX_X:
        z = z_min + CELL_SIZE * 0.5
        while z < z_max:
            cells.append((x, z))
            z += CELL_SIZE
        x += CELL_SIZE
    return cells


def generate_placements(pool, seed=20260629):
    rng = random.Random(seed)
    accepted: list[tuple[float, float]] = []
    placements = []
    zone_counts = {name: 0 for name, _, _, _ in ZONES}

    def try_place(x: float, z: float) -> bool:
        x = max(MIN_X, min(MAX_X, x))
        z = max(MIN_Z, min(MAX_Z, z))
        if is_clear_zone(x, z):
            return False
        if any(dist((x, z), other) < 2.4 for other in accepted):
            return False
        accepted.append((x, z))
        guid, root_id = rng.choice(pool)
        rot_y = rng.random() * 360.0
        scale = 0.88 + rng.random() * 0.35
        placements.append((guid, root_id, x, 0.0, z, rot_y, scale))
        zone = zone_for_z(z)
        if zone:
            zone_counts[zone] += 1
        return True

    for name, z_min, z_max, quota in ZONES:
        cells = zone_cells(z_min, z_max)
        rng.shuffle(cells)
        placed = 0
        for cx, cz in cells:
            if placed >= quota:
                break
            jitter = CELL_SIZE * 0.28
            if try_place(cx + rng.uniform(-jitter, jitter), cz + rng.uniform(-jitter, jitter)):
                placed += 1

        attempts = 0
        while zone_counts[name] < quota and attempts < quota * 25:
            attempts += 1
            x = MIN_X + rng.random() * (MAX_X - MIN_X)
            z = z_min + rng.random() * (z_max - z_min)
            try_place(x, z)

    print("zone counts:", zone_counts, "total:", len(placements))
    return placements


def zone_for_z(z: float) -> str | None:
    for name, z_min, z_max, _ in ZONES:
        if z_min <= z < z_max:
            return name
    return None


def fmt_instance(instance_id: int, transform_id: int, parent_id: int, guid: str, root_id: int, x, y, z, rot_y, scale):
    return f"""--- !u!1001 &{instance_id}
PrefabInstance:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Modification:
    serializedVersion: 3
    m_TransformParent: {{fileID: {parent_id}}}
    m_Modifications:
    - target: {{fileID: {root_id}, guid: {guid}, type: 3}}
      propertyPath: m_LocalPosition.x
      value: {x:.3f}
      objectReference: {{fileID: 0}}
    - target: {{fileID: {root_id}, guid: {guid}, type: 3}}
      propertyPath: m_LocalPosition.y
      value: {y:.3f}
      objectReference: {{fileID: 0}}
    - target: {{fileID: {root_id}, guid: {guid}, type: 3}}
      propertyPath: m_LocalPosition.z
      value: {z:.3f}
      objectReference: {{fileID: 0}}
    - target: {{fileID: {root_id}, guid: {guid}, type: 3}}
      propertyPath: m_LocalEulerAnglesHint.x
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: {root_id}, guid: {guid}, type: 3}}
      propertyPath: m_LocalEulerAnglesHint.y
      value: {rot_y:.3f}
      objectReference: {{fileID: 0}}
    - target: {{fileID: {root_id}, guid: {guid}, type: 3}}
      propertyPath: m_LocalEulerAnglesHint.z
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: {root_id}, guid: {guid}, type: 3}}
      propertyPath: m_LocalScale.x
      value: {scale:.3f}
      objectReference: {{fileID: 0}}
    - target: {{fileID: {root_id}, guid: {guid}, type: 3}}
      propertyPath: m_LocalScale.y
      value: {scale:.3f}
      objectReference: {{fileID: 0}}
    - target: {{fileID: {root_id}, guid: {guid}, type: 3}}
      propertyPath: m_LocalScale.z
      value: {scale:.3f}
      objectReference: {{fileID: 0}}
    m_RemovedComponents: []
    m_RemovedGameObjects: []
    m_AddedGameObjects: []
    m_AddedComponents: []
  m_SourcePrefab: {{fileID: 100100000, guid: {guid}, type: 3}}
--- !u!4 &{transform_id} stripped
Transform:
  m_CorrespondingSourceObject: {{fileID: {root_id}, guid: {guid}, type: 3}}
  m_PrefabInstance: {{fileID: {instance_id}}}
  m_PrefabAsset: {{fileID: 0}}
"""


def main():
    pool = build_pool()
    if not pool:
        raise SystemExit("No prefabs found")
    placements = generate_placements(pool)
    parent_id = 1900030002
    children = []
    blocks = []
    instance_id = 1910010000
    transform_id = 1910010001

    for guid, root_id, x, y, z, rot_y, scale in placements:
        children.append(f"  - {{fileID: {transform_id}}}")
        blocks.append(fmt_instance(instance_id, transform_id, parent_id, guid, root_id, x, y, z, rot_y, scale))
        instance_id += 2
        transform_id += 2

    header = f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &1900030001
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {parent_id}}}
  - component: {{fileID: 1900030003}}
  - component: {{fileID: 1900030004}}
  m_Layer: 0
  m_Name: ---POLYTOPE_FOREST---
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &{parent_id}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 1900030001}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children:
{chr(10).join(children)}
  m_Father: {{fileID: 0}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!114 &1900030003
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 1900030001}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {FOREST_COLLIDER_GUID}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
--- !u!114 &1900030004
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 1900030001}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {FOREST_FILTER_GUID}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
"""

    OUTPUT.write_text(header + "\n".join(blocks) + "\n", encoding="utf-8")
    print(f"Wrote {len(placements)} props to {OUTPUT}")


if __name__ == "__main__":
    main()
