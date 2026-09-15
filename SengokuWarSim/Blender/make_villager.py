"""戦国期の村人 (低ポリ) を手続き生成して FBX に書き出す Blender スクリプト。

使い方 (ヘッドレス):
    blender -b -P SengokuWarSim/Blender/make_villager.py -- --out SengokuWarSim/Assets/Models [--kinds spear,hoe,bow,sling,headman] [--preview]

出力:
    <out>/Villager_<kind>.fbx   足元原点、+Z 前向き (FBX エクスポート時に -Z forward / Y up)、1 unit = 1 m
    --preview を付けると <out>/Villager_<kind>.png も描く (Eevee/Workbench)

Unity 側の FigureFactory はプリミティブ合成で同じシルエットを作るので、
本スクリプトの出力に差し替えたい場合は Assets/Models の FBX を SquadView に割り当てればよい。
"""

import argparse
import math
import os
import sys

import bpy

KINDS = ("spear", "hoe", "bow", "sling", "headman")

COLORS = {
    "skin": (0.86, 0.68, 0.52, 1.0),
    "kimono": (0.45, 0.40, 0.33, 1.0),
    "sash": (0.16, 0.25, 0.55, 1.0),
    "straw": (0.80, 0.70, 0.40, 1.0),
    "wood": (0.48, 0.33, 0.18, 1.0),
    "iron": (0.35, 0.36, 0.38, 1.0),
    "bamboo": (0.55, 0.65, 0.30, 1.0),
    "stone": (0.55, 0.55, 0.52, 1.0),
}


def parse_args():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    p = argparse.ArgumentParser()
    p.add_argument("--out", required=True)
    p.add_argument("--kinds", default=",".join(KINDS))
    p.add_argument("--preview", action="store_true")
    return p.parse_args(argv)


def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1.0


def material(name):
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name)
        mat.use_nodes = True
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        if bsdf is not None:
            bsdf.inputs["Base Color"].default_value = COLORS[name]
            bsdf.inputs["Roughness"].default_value = 0.85
        mat.diffuse_color = COLORS[name]
    return mat


def add(op, name, mat_name, location, rotation=(0, 0, 0), scale=(1, 1, 1), **kwargs):
    op(location=location, rotation=[math.radians(r) for r in rotation], **kwargs)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(material(mat_name))
    return obj


def build_villager(kind):
    """Blender は Z-up なので、Unity の Y を Z に読み替えて組む。前方は -Y (FBX で +Z になる)。"""
    cyl = bpy.ops.mesh.primitive_cylinder_add
    sph = bpy.ops.mesh.primitive_uv_sphere_add
    cone = bpy.ops.mesh.primitive_cone_add
    cube = bpy.ops.mesh.primitive_cube_add
    parts = []

    # 脚
    parts.append(add(cyl, "LegL", "kimono", (-0.09, 0, 0.22), scale=(0.065, 0.065, 0.22), vertices=8))
    parts.append(add(cyl, "LegR", "kimono", (0.09, 0, 0.22), scale=(0.065, 0.065, 0.22), vertices=8))
    # 胴
    parts.append(add(sph, "Torso", "kimono", (0, 0, 0.78), scale=(0.18, 0.13, 0.34), segments=12, ring_count=8))
    # 帯 (村の色)
    parts.append(add(cube, "Sash", "sash", (0, 0, 0.66), scale=(0.19, 0.145, 0.045)))
    # 腕
    parts.append(add(cyl, "ArmL", "skin", (-0.23, -0.02, 0.82), rotation=(0, 12, 0), scale=(0.045, 0.045, 0.22), vertices=8))
    parts.append(add(cyl, "ArmR", "skin", (0.23, -0.02, 0.82), rotation=(0, -12, 0), scale=(0.045, 0.045, 0.22), vertices=8))
    # 頭
    parts.append(add(sph, "Head", "skin", (0, 0, 1.22), scale=(0.12, 0.12, 0.12), segments=12, ring_count=8))
    # 被り物
    if kind == "headman":
        parts.append(add(cone, "Jingasa", "iron", (0, 0, 1.36), scale=(0.36, 0.36, 0.06), vertices=12))
    else:
        parts.append(add(cone, "Kasa", "straw", (0, 0, 1.38), scale=(0.32, 0.32, 0.10), vertices=12))

    # 得物 (右手 +X、前方 -Y)
    if kind == "spear":
        parts.append(add(cyl, "BambooSpear", "bamboo", (0.30, -0.15, 1.05), rotation=(-18, 0, 0), scale=(0.018, 0.018, 1.15), vertices=6))
    elif kind == "hoe":
        parts.append(add(cyl, "HoeHandle", "wood", (0.30, -0.05, 0.95), rotation=(-10, 0, 0), scale=(0.02, 0.02, 0.6), vertices=6))
        parts.append(add(cube, "HoeBlade", "iron", (0.30, -0.22, 1.53), rotation=(60, 0, 0), scale=(0.08, 0.09, 0.02)))
    elif kind == "bow":
        parts.append(add(cyl, "BowStave", "wood", (0.32, -0.12, 1.0), rotation=(-6, 0, 0), scale=(0.015, 0.015, 0.75), vertices=6))
        parts.append(add(cyl, "Quiver", "straw", (-0.18, 0.2, 0.9), rotation=(20, 20, 0), scale=(0.04, 0.04, 0.3), vertices=6))
    elif kind == "sling":
        parts.append(add(cyl, "SlingCord", "wood", (0.30, -0.12, 0.62), rotation=(0, 90, 0), scale=(0.008, 0.008, 0.25), vertices=6))
        parts.append(add(sph, "Stone", "stone", (0.30, -0.16, 0.55), scale=(0.045, 0.045, 0.045), segments=8, ring_count=6))
    elif kind == "headman":
        parts.append(add(cube, "Sword", "iron", (-0.2, 0.05, 0.62), rotation=(0, 75, 20), scale=(0.015, 0.025, 0.35)))

    # 一つのメッシュに結合
    bpy.ops.object.select_all(action="DESELECT")
    for p in parts:
        p.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    obj = bpy.context.active_object
    obj.name = f"Villager_{kind}"
    bpy.ops.object.shade_flat()
    # 原点を足元に
    bpy.context.scene.cursor.location = (0, 0, 0)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
    return obj


def export_fbx(obj, path):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        object_types={"MESH"},
        mesh_smooth_type="FACE",
        add_leaf_bones=False,
        path_mode="AUTO",
    )


def render_preview(path):
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.render.resolution_x = 512
    scene.render.resolution_y = 512
    scene.render.filepath = path
    bpy.ops.object.camera_add(location=(2.2, -2.6, 1.6), rotation=(math.radians(72), 0, math.radians(40)))
    scene.camera = bpy.context.active_object
    bpy.ops.object.light_add(type="SUN", location=(2, -2, 5))
    bpy.ops.render.render(write_still=True)


def main():
    args = parse_args()
    os.makedirs(args.out, exist_ok=True)
    kinds = [k.strip() for k in args.kinds.split(",") if k.strip()]
    for kind in kinds:
        if kind not in KINDS:
            raise SystemExit(f"unknown kind '{kind}', expected one of {KINDS}")
        reset_scene()
        obj = build_villager(kind)
        fbx = os.path.join(args.out, f"{obj.name}.fbx")
        export_fbx(obj, fbx)
        print(f"exported {fbx}  verts={len(obj.data.vertices)} faces={len(obj.data.polygons)}")
        if args.preview:
            png = os.path.join(args.out, f"{obj.name}.png")
            render_preview(png)
            print(f"preview {png}")


if __name__ == "__main__":
    main()
