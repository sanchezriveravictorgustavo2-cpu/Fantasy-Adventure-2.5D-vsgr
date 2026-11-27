"""
Build & Export Script: Mineral_Ore (Depósito Mineral / Mena de Cristales - World 01)
Project: World_1_Bosque_Encantado_2.5D (Strategy & Side-Scrolling 2.5D Combat)
Target Engines: Blender 5.0.1 -> Unity 6 URP (Universal Render Pipeline/Lit)

Design & Technical Contract:
1. Single uniform initial size model (NO small, medium, or large model variants).
   - Dimensions: ~1.40m (X width) x ~1.30m (Y depth) x ~1.22m (Z height).
   - Exact ground pivot at (0, 0, 0) at bottom center (Z_min = 0.000m) so when scaled uniformly
     in Unity along all 5 extraction states (100% -> 75% -> 50% -> 25% -> 0%), the base remains
     100% anchored on the ground without sinking or floating!
2. Visual Elements (Low-Poly Fantasy + Frutiger Aero):
   - Irregular, natural faceted Low-Poly rock base (Warm Slate Stone + Moss/Earth rim cushions).
   - Protruding Turquoise (#1CE8F5) and Aqua-Blue (#1A8CFF) faceted hexagonal crystal prisms.
   - Small glowing mineral veins (#42F8FF) coursing through the rock crevices.
3. Materials & Textures:
   - MAT_Mineral_RockBase (Tex_Mineral_RockBase.png)
   - MAT_Mineral_CrystalTurquoise (Tex_Mineral_CrystalTurquoise.png, emissive Frutiger Aero turquoise)
   - MAT_Mineral_CrystalAquaBlue (Tex_Mineral_CrystalAquaBlue.png, emissive sapphire-aqua)
   - MAT_Mineral_LuminousVeins (Tex_Mineral_LuminousVeins.png, bright cyan-aqua glowing seams)
"""

import bpy
import bmesh
import math
import os
import json
import shutil
import numpy as np
from mathutils import Vector, Matrix, Euler

# =============================================================================
# 0. PATHS & FOLDERS
# =============================================================================
WORKSPACE_ROOT = r"c:\Users\Víctor Gustavo\Downloads\AI"
UNITY_PROJECT_ROOT = r"C:\Users\Víctor Gustavo\Downloads\unity-gemini\Fantasy-Adventure-2.5D-vsgr"

REL_STRUCT_PATH = os.path.join("Assets", "Game", "Worlds", "World_01", "Structures", "Mineral_Ore")
WS_BASE_DIR = os.path.join(WORKSPACE_ROOT, REL_STRUCT_PATH)
UNITY_BASE_DIR = os.path.join(UNITY_PROJECT_ROOT, REL_STRUCT_PATH)

WS_MODELS_DIR = os.path.join(WS_BASE_DIR, "Models")
WS_MATS_DIR = os.path.join(WS_BASE_DIR, "Materials")
WS_TEX_DIR = os.path.join(WS_BASE_DIR, "Textures")
WS_RENDERS_DIR = os.path.join(WS_BASE_DIR, "Renders")
WS_DOCS_DIR = os.path.join(WS_BASE_DIR, "Documentation")

for base in (WS_BASE_DIR, UNITY_BASE_DIR):
    for sub in ("Models", "Materials", "Textures", "Renders", "Documentation", "Prefabs", "Scripts"):
        os.makedirs(os.path.join(base, sub), exist_ok=True)


# =============================================================================
# 1. COLOR & TEXTURE HELPERS
# =============================================================================
def srgb_to_lin_ch(c: float) -> float:
    c = max(0.0, min(1.0, float(c)))
    if c <= 0.04045:
        return c / 12.92
    return ((c + 0.055) / 1.055) ** 2.4


def srgb(r: float, g: float, b: float) -> tuple[float, float, float]:
    return (srgb_to_lin_ch(r), srgb_to_lin_ch(g), srgb_to_lin_ch(b))


def hex_col(h: str) -> tuple[float, float, float]:
    h = h.strip().lstrip("#")
    return srgb(int(h[0:2], 16) / 255.0, int(h[2:4], 16) / 255.0, int(h[4:6], 16) / 255.0)


def smoothstep(edge0, edge1, x):
    t = np.clip((x - edge0) / (edge1 - edge0 + 1e-8), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def np_to_blender_image(name, rgba_array, colorspace="sRGB", filepath=None):
    h, w, _ = rgba_array.shape
    img = bpy.data.images.new(name=name, width=w, height=h, alpha=True, float_buffer=False)
    img.colorspace_settings.name = colorspace
    img.pixels.foreach_set(np.clip(rgba_array, 0.0, 1.0).astype(np.float32).ravel())
    img.pack()
    if filepath:
        img.filepath_raw = filepath
        img.file_format = "PNG"
        img.save()
    return img


# =============================================================================
# 2. SCENE RESET & UV / SHADING HELPERS
# =============================================================================
def clear_scene():
    scene = bpy.context.scene
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    for col in list(bpy.data.collections):
        bpy.data.collections.remove(col)
    for block_list in (
        bpy.data.meshes,
        bpy.data.materials,
        bpy.data.lights,
        bpy.data.cameras,
        bpy.data.images,
        bpy.data.worlds,
    ):
        for item in list(block_list):
            if item.users == 0:
                block_list.remove(item)

    col_ore = bpy.data.collections.new("Mineral_Ore")
    col_studio = bpy.data.collections.new("Studio_Preview")
    scene.collection.children.link(col_ore)
    scene.collection.children.link(col_studio)
    return col_ore, col_studio


def link_obj(obj, collection):
    for c in list(obj.users_collection):
        c.objects.unlink(obj)
    collection.objects.link(obj)


def set_flat(mesh):
    for p in mesh.polygons:
        p.use_smooth = False
    mesh.update()


def apply_smart_box_uv(bm, scale=0.65, v_min_z=0.0, v_max_z=1.25):
    uv_layer = bm.loops.layers.uv.verify()
    z_span = max(0.15, v_max_z - v_min_z)
    for f in bm.faces:
        n = f.normal
        ax, ay, az = abs(n.x), abs(n.y), abs(n.z)
        for lp in f.loops:
            co = lp.vert.co
            v_height = np.clip((co.z - v_min_z) / z_span, 0.03, 0.97)
            if az >= ax and az >= ay:
                u = (co.x * scale + 0.5) % 1.0
            elif ay >= ax and ay >= az:
                u = (co.x * scale + 0.5) % 1.0
            else:
                u = (co.y * scale + 0.5) % 1.0
            lp[uv_layer].uv = (float(np.clip(u, 0.02, 0.98)), float(v_height))


def create_mesh_obj(name, bm, mat, collection, v_min_z=0.0, v_max_z=1.25):
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    apply_smart_box_uv(bm, scale=0.65, v_min_z=v_min_z, v_max_z=v_max_z)
    mesh = bpy.data.meshes.new(name + "_Mesh")
    bm.to_mesh(mesh)
    bm.free()

    if len(mesh.uv_layers) > 0:
        mesh.uv_layers[0].name = "UVMap"
        mesh.uv_layers.active = mesh.uv_layers[0]

    obj = bpy.data.objects.new(name, mesh)
    link_obj(obj, collection)
    if mat is not None:
        obj.data.materials.append(mat)
    # Crisp flat-shaded low-poly facets per World 1 Art Direction Bible!
    set_flat(mesh)
    return obj


# =============================================================================
# 3. PROCEDURAL FRUTIGER AERO TEXTURES & MATERIALS
# =============================================================================
def generate_ore_textures():
    """
    Generate 4 high-craft stylized textures matching World 1 Frutiger Aero palette:
    1) Tex_Mineral_RockBase.png        : Warm-cool slate rock (#7D92A5) with mossy/earthy base grounding
                                         and subtle aqua mineral shimmer in the upper stone facets.
    2) Tex_Mineral_CrystalTurquoise.png: Luminous Frutiger Aero Turquoise crystal (#1CE8F5 -> #98FFF8).
    3) Tex_Mineral_CrystalAquaBlue.png : Luminous Sapphire-Aqua crystal (#1A8CFF -> #62E4FF).
    4) Tex_Mineral_LuminousVeins.png   : Radiant cyan-turquoise glowing mineral veins (#42F8FF -> #C4FFFF).
    """
    W, H = 512, 512
    u = np.linspace(0.0, 1.0, W, dtype=np.float32)
    v = np.linspace(0.0, 1.0, H, dtype=np.float32)
    U, V = np.meshgrid(u, v)

    # 1. Stylized Warm-Slate & Moss-Grounded Rock Base
    rock_base = np.array([0.505, 0.585, 0.655], dtype=np.float32)   # #8195A7 Warm Slate
    rock_dark = np.array([0.365, 0.455, 0.530], dtype=np.float32)   # #5D7487 Deep Slate
    moss_rim  = np.array([0.290, 0.610, 0.415], dtype=np.float32)   # #4A9C6A Lush Forest Moss at base
    rock_hi   = np.array([0.675, 0.765, 0.835], dtype=np.float32)   # #ACC3D5 Sunlit mineral ridge

    t_v = smoothstep(0.14, 0.85, V)
    col_r = rock_dark * (1.0 - t_v[..., None]) + rock_base * t_v[..., None]

    # Subtle mossy grounding along the bottom 16% of the rock base so it blends into World 1 grass
    moss_mask = (1.0 - smoothstep(0.04, 0.20, V)) * (0.65 + 0.35 * np.sin(U * 2.0 * np.pi * 5.0))
    col_r = col_r * (1.0 - 0.55 * moss_mask[..., None]) + moss_rim * (0.55 * moss_mask[..., None])

    # Upper sunlit stone highlight & subtle crystalline sparkle
    hi_mask = smoothstep(0.68, 0.96, V) * 0.48
    col_r = col_r * (1.0 - hi_mask[..., None]) + rock_hi * hi_mask[..., None]

    strata = (
        0.5 * np.sin(V * 2.0 * np.pi * 11.0 + 0.7 * np.sin(U * 2.0 * np.pi * 4.0))
        + 0.3 * np.cos(U * 2.0 * np.pi * 15.0) * np.sin(V * 2.0 * np.pi * 17.0)
    ) * 0.035
    col_r = np.clip(col_r + strata[..., None], 0.0, 1.0)

    img_rock = np.ones((H, W, 4), dtype=np.float32)
    img_rock[..., :3] = col_r
    tex_rock = np_to_blender_image(
        "Tex_Mineral_RockBase", img_rock, "sRGB",
        os.path.join(WS_TEX_DIR, "Tex_Mineral_RockBase.png")
    )

    # 2. Frutiger Aero Turquoise Crystal Texture
    turq_base = np.array([0.045, 0.680, 0.785], dtype=np.float32)   # #0BADC8
    turq_mid  = np.array([0.140, 0.910, 0.960], dtype=np.float32)   # #24E8F5
    turq_tip  = np.array([0.720, 0.995, 0.985], dtype=np.float32)   # #B8FEFB

    t1 = smoothstep(0.0, 0.65, V)
    t2 = smoothstep(0.55, 0.98, V)
    col_t = turq_base * (1.0 - t1[..., None]) + turq_mid * t1[..., None]
    col_t = col_t * (1.0 - t2[..., None]) + turq_tip * t2[..., None]
    facet_shimmer = 0.05 * np.abs(np.sin(U * 2.0 * np.pi * 6.0))
    col_t = np.clip(col_t + facet_shimmer[..., None], 0.0, 1.0)

    img_turq = np.ones((H, W, 4), dtype=np.float32)
    img_turq[..., :3] = col_t
    tex_turq = np_to_blender_image(
        "Tex_Mineral_CrystalTurquoise", img_turq, "sRGB",
        os.path.join(WS_TEX_DIR, "Tex_Mineral_CrystalTurquoise.png")
    )

    # 3. Frutiger Aero Aqua-Blue Crystal Texture
    blue_base = np.array([0.055, 0.380, 0.920], dtype=np.float32)   # #0E61EB
    blue_mid  = np.array([0.120, 0.640, 1.000], dtype=np.float32)   # #1FA3FF
    blue_tip  = np.array([0.580, 0.940, 1.000], dtype=np.float32)   # #94F0FF

    col_b = blue_base * (1.0 - t1[..., None]) + blue_mid * t1[..., None]
    col_b = col_b * (1.0 - t2[..., None]) + blue_tip * t2[..., None]
    col_b = np.clip(col_b + facet_shimmer[..., None], 0.0, 1.0)

    img_blue = np.ones((H, W, 4), dtype=np.float32)
    img_blue[..., :3] = col_b
    tex_blue = np_to_blender_image(
        "Tex_Mineral_CrystalAquaBlue", img_blue, "sRGB",
        os.path.join(WS_TEX_DIR, "Tex_Mineral_CrystalAquaBlue.png")
    )

    # 4. Luminous Mineral Veins Texture
    vein_edge = np.array([0.120, 0.840, 0.960], dtype=np.float32)   # #1FD6F5
    vein_core = np.array([0.780, 0.995, 1.000], dtype=np.float32)   # #C7FEFF
    pulse = np.exp(-((U - 0.5) / 0.28) ** 2)
    col_v = vein_edge * (1.0 - pulse[..., None]) + vein_core * pulse[..., None]

    img_vein = np.ones((H, W, 4), dtype=np.float32)
    img_vein[..., :3] = np.clip(col_v, 0.0, 1.0)
    tex_vein = np_to_blender_image(
        "Tex_Mineral_LuminousVeins", img_vein, "sRGB",
        os.path.join(WS_TEX_DIR, "Tex_Mineral_LuminousVeins.png")
    )

    return {
        "rock": tex_rock,
        "turquoise": tex_turq,
        "aquablue": tex_blue,
        "veins": tex_vein,
    }


def make_urp_material(
    name: str,
    base_hex: str,
    tex_img=None,
    roughness: float = 0.72,
    metallic: float = 0.0,
    specular: float = 0.35,
    coat: float = 0.0,
    coat_roughness: float = 0.08,
    emission_hex: str | None = None,
    emission_strength: float = 0.0,
):
    mat = bpy.data.materials.new(name=name)
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")

    base_lin = hex_col(base_hex)
    bsdf.inputs["Base Color"].default_value = (*base_lin, 1.0)
    bsdf.inputs["Roughness"].default_value = roughness
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Specular IOR Level"].default_value = specular
    bsdf.inputs["Coat Weight"].default_value = coat
    bsdf.inputs["Coat Roughness"].default_value = coat_roughness

    if tex_img is not None:
        tex_node = nt.nodes.new("ShaderNodeTexImage")
        tex_node.image = tex_img
        tex_node.extension = "REPEAT"
        nt.links.new(tex_node.outputs["Color"], bsdf.inputs["Base Color"])

    if emission_hex is not None and emission_strength > 0.0:
        emit_lin = hex_col(emission_hex)
        bsdf.inputs["Emission Color"].default_value = (*emit_lin, 1.0)
        bsdf.inputs["Emission Strength"].default_value = emission_strength
    else:
        bsdf.inputs["Emission Color"].default_value = (0.0, 0.0, 0.0, 1.0)
        bsdf.inputs["Emission Strength"].default_value = 0.0

    mat.diffuse_color = (*base_lin, 1.0)
    if hasattr(mat, "surface_render_method"):
        mat.surface_render_method = "DITHERED"

    return mat


def build_ore_materials():
    texs = generate_ore_textures()
    mats = {
        "rock": make_urp_material(
            "MAT_Mineral_RockBase",
            base_hex="#8195A7",
            tex_img=texs["rock"],
            roughness=0.74,
            metallic=0.05,
            specular=0.35,
        ),
        "turquoise": make_urp_material(
            "MAT_Mineral_CrystalTurquoise",
            base_hex="#24E8F5",
            tex_img=texs["turquoise"],
            roughness=0.06,
            metallic=0.0,
            specular=0.95,
            coat=1.0,
            coat_roughness=0.03,
            emission_hex="#1CE8F5",
            emission_strength=1.15,
        ),
        "aquablue": make_urp_material(
            "MAT_Mineral_CrystalAquaBlue",
            base_hex="#1A8CFF",
            tex_img=texs["aquablue"],
            roughness=0.06,
            metallic=0.0,
            specular=0.95,
            coat=1.0,
            coat_roughness=0.03,
            emission_hex="#1FA8FF",
            emission_strength=1.10,
        ),
        "veins": make_urp_material(
            "MAT_Mineral_LuminousVeins",
            base_hex="#48F6FF",
            tex_img=texs["veins"],
            roughness=0.12,
            metallic=0.0,
            specular=0.80,
            coat=0.60,
            coat_roughness=0.05,
            emission_hex="#42F8FF",
            emission_strength=2.20,
        ),
    }
    return mats, texs


# =============================================================================
# 4. GEOMETRY BUILDERS: ROCK BASE, LUMINOUS VEINS & TURQUOISE/AQUA CRYSTALS
# =============================================================================
def add_lofted_rings(bm, rings, closed_u=True, cap_bottom=True, cap_top=True):
    v_rings = [[bm.verts.new(p) for p in ring] for ring in rings]
    n = len(rings[0])
    m = len(v_rings)
    for i in range(m - 1):
        r0 = v_rings[i]
        r1 = v_rings[i + 1]
        for j in range(n):
            jn = (j + 1) % n
            try:
                bm.faces.new((r0[j], r0[jn], r1[jn], r1[j]))
            except ValueError:
                pass
    if cap_bottom and closed_u:
        cv = bm.verts.new(sum((v.co for v in v_rings[0]), Vector()) / n)
        for j in range(n):
            try:
                bm.faces.new((v_rings[0][(j + 1) % n], v_rings[0][j], cv))
            except ValueError:
                pass
    if cap_top and closed_u:
        cv = bm.verts.new(sum((v.co for v in v_rings[-1]), Vector()) / n)
        for j in range(n):
            try:
                bm.faces.new((v_rings[-1][j], v_rings[-1][(j + 1) % n], cv))
            except ValueError:
                pass
    return v_rings


def add_faceted_rock_boulder(bm, center_xy, z_bottom, rx, ry, height, segs=9, seed_angle=0.0, tilt_xy=(0.0, 0.0)):
    """
    Add an irregular, natural flat-faceted low-poly rock boulder whose bottom ring is strictly
    coplanar with z_bottom (Z = 0.000m) so the deposit sits 100% flush on the ground!
    """
    cx, cy = center_xy
    tx, ty = tilt_xy
    layer_specs = [
        (0.00, 1.00),  # Strictly grounded at z_bottom = 0.000
        (0.24, 1.06),  # Slight lower swell
        (0.56, 0.88),  # Mid faceted shoulder
        (0.84, 0.56),  # Upper crown bevel
        (1.00, 0.22),  # Top irregular plateau
    ]
    rings = []
    for l_idx, (t_z, r_scale) in enumerate(layer_specs):
        z = z_bottom + height * t_z
        ring = []
        for s in range(segs):
            ang = seed_angle + 2.0 * math.pi * s / segs
            # Deterministic natural angular irregularity
            irr = (
                1.0
                + 0.14 * math.sin(ang * 2.0 + seed_angle * 1.7 + l_idx * 0.9)
                + 0.09 * math.cos(ang * 3.0 - seed_angle * 2.3 + l_idx * 1.4)
            )
            px = cx + tx * t_z + math.cos(ang) * rx * r_scale * irr
            py = cy + ty * t_z + math.sin(ang) * ry * r_scale * irr
            # Keep bottom ring strictly at z_bottom = 0.000m; vary upper rings organically
            if l_idx == 0:
                pz = z_bottom
            else:
                pz = max(z_bottom + 0.02, z + height * 0.05 * math.sin(ang * 2.0 + l_idx * 1.5))
            ring.append(Vector((px, py, pz)))
        rings.append(ring)
    add_lofted_rings(bm, rings, closed_u=True, cap_bottom=True, cap_top=True)


def add_faceted_crystal_prism(bm, base_pos: Vector, axis_dir: Vector, length: float, radius_base: float, segs: int = 6, twist_deg: float = 0.0):
    """
    Add a clean faceted hexagonal/pentagonal Frutiger Aero crystal shard with a sharp pyramidal tip.
    """
    z_ax = axis_dir.normalized()
    ref = Vector((1.0, 0.0, 0.0)) if abs(z_ax.x) < 0.8 else Vector((0.0, 1.0, 0.0))
    x_ax = z_ax.cross(ref).normalized()
    y_ax = z_ax.cross(x_ax).normalized()
    tw = math.radians(twist_deg)

    rings = []
    profile = [
        (0.00, radius_base * 0.84),
        (0.26, radius_base * 1.02),
        (0.72, radius_base * 1.14),
        (1.00, 0.002),
    ]
    for idx, (t_h, r_h) in enumerate(profile):
        c = base_pos + z_ax * (length * t_h)
        ring = []
        for s in range(segs):
            ang = tw + 2.0 * math.pi * s / segs
            # Slight asymmetric facet ratio for natural crystal look
            asym = 1.08 if (s % 2 == 0) else 0.94
            pt = c + (x_ax * math.cos(ang) * asym + y_ax * math.sin(ang) / asym) * r_h
            # Ensure no crystal vertex ever drops below Z = 0.005m
            pt.z = max(0.005, pt.z)
            ring.append(pt)
        rings.append(ring)
    add_lofted_rings(bm, rings, closed_u=True, cap_bottom=True, cap_top=True)


def add_luminous_vein_seam(bm, pts: list[Vector], width: float = 0.032, thickness: float = 0.022):
    """
    Add a faceted glowing mineral vein strip embedded along the rock crevices.
    """
    up_ref = Vector((0.0, 0.0, 1.0))
    rings = []
    n_pts = len(pts)
    for i, p in enumerate(pts):
        if i == 0:
            t = (pts[1] - pts[0]).normalized()
        elif i == n_pts - 1:
            t = (pts[-1] - pts[-2]).normalized()
        else:
            t = (pts[i + 1] - pts[i - 1]).normalized()

        out_dir = Vector((p.x, p.y, p.z * 0.6)).normalized()
        if out_dir.length < 1e-4:
            out_dir = up_ref
        side_dir = t.cross(out_dir)
        if side_dir.length < 1e-4:
            side_dir = t.cross(up_ref)
        side_dir.normalize()
        out_dir = side_dir.cross(t).normalized()

        # Taper ends of the mineral vein into sharp crystalline tips
        taper = 0.25 if (i == 0 or i == n_pts - 1) else (1.15 if i == n_pts // 2 else 0.95)
        w = width * taper
        th = thickness * taper
        ring = [
            p - side_dir * w,
            p + out_dir * th,
            p + side_dir * w,
            p - out_dir * (th * 0.6),
        ]
        for pt in ring:
            pt.z = max(0.01, pt.z)
        rings.append(ring)
    add_lofted_rings(bm, rings, closed_u=True, cap_bottom=True, cap_top=True)


def build_mineral_ore_geometry(col_ore, mats):
    """
    Construct the single canonical Mineral_Ore model:
    - 1) Mineral_Rock_Base          : Irregular multi-lobe low-poly rock formation grounded at Z = 0.000m.
    - 2) Mineral_Luminous_Veins     : Glowing cyan-aqua mineral seams running across the rock facets.
    - 3) Mineral_Crystals_Turquoise : Primary Frutiger Aero Turquoise crystal shards protruding from the rock.
    - 4) Mineral_Crystals_AquaBlue  : Secondary Sapphire-Aqua crystal shards creating rich prismatic contrast.
    """
    objs = []

    # 1) Irregular Natural Low-Poly Rock Base (Z = 0.000m to ~0.64m)
    bm_rock = bmesh.new()
    rock_lobes = [
        # (center_xy, z_bottom, rx, ry, height, segs, seed_angle, tilt_xy)
        (( 0.00,  0.00), 0.000, 0.56, 0.50, 0.62, 10, 0.25, (-0.03, -0.02)),  # Main central boulder
        ((-0.28, -0.14), 0.000, 0.36, 0.32, 0.44,  8, 1.15, (-0.05, -0.03)),  # West-front shoulder
        (( 0.30, -0.10), 0.000, 0.34, 0.30, 0.41,  8, 2.40, ( 0.04, -0.02)),  # East-front shoulder
        (( 0.06,  0.24), 0.000, 0.38, 0.32, 0.48,  8, 3.80, ( 0.02,  0.04)),  # North-back ridge
        ((-0.22,  0.20), 0.000, 0.30, 0.28, 0.36,  7, 4.95, (-0.04,  0.03)),  # North-west spur
    ]
    for c_xy, zb, rx, ry, h, segs, seed, tilt in rock_lobes:
        add_faceted_rock_boulder(bm_rock, c_xy, zb, rx, ry, h, segs=segs, seed_angle=seed, tilt_xy=tilt)

    objs.append(create_mesh_obj("Mineral_Rock_Base", bm_rock, mats["rock"], col_ore, v_min_z=0.0, v_max_z=0.70))

    # 2) Small Luminous Mineral Veins (MAT_Mineral_LuminousVeins)
    bm_veins = bmesh.new()
    vein_paths = [
        # Front-left diagonal glowing vein
        [
            Vector((-0.46, -0.32, 0.10)),
            Vector((-0.34, -0.40, 0.24)),
            Vector((-0.16, -0.44, 0.38)),
            Vector((-0.02, -0.36, 0.52)),
        ],
        # Front-right branching glowing vein
        [
            Vector(( 0.44, -0.26, 0.12)),
            Vector(( 0.30, -0.36, 0.26)),
            Vector(( 0.14, -0.39, 0.40)),
            Vector(( 0.02, -0.33, 0.54)),
        ],
        # Side-west crevice vein
        [
            Vector((-0.56,  0.04, 0.11)),
            Vector((-0.46, -0.06, 0.28)),
            Vector((-0.30, -0.10, 0.43)),
        ],
        # Side-east crevice vein
        [
            Vector(( 0.54,  0.08, 0.10)),
            Vector(( 0.42,  0.02, 0.27)),
            Vector(( 0.26, -0.02, 0.41)),
        ],
        # Back-crown mineral seam
        [
            Vector((-0.18,  0.46, 0.14)),
            Vector(( 0.00,  0.44, 0.30)),
            Vector(( 0.16,  0.35, 0.45)),
        ],
    ]
    for v_pts in vein_paths:
        add_luminous_vein_seam(bm_veins, v_pts, width=0.034, thickness=0.024)

    objs.append(create_mesh_obj("Mineral_Luminous_Veins", bm_veins, mats["veins"], col_ore, v_min_z=0.05, v_max_z=0.65))

    # 3) Protruding Turquoise Crystals (MAT_Mineral_CrystalTurquoise)
    bm_turq = bmesh.new()
    turquoise_shards = [
        # (base_pos, axis_dir, length, radius_base, segs, twist_deg)
        # Tall dominant central-front turquoise spire
        (Vector((-0.04, -0.04, 0.42)), Vector((-0.08, -0.14, 0.98)), 0.80, 0.145, 6, 15.0),
        # Left-tilted hero turquoise shard
        (Vector((-0.24, -0.08, 0.34)), Vector((-0.44, -0.20, 0.87)), 0.64, 0.120, 6, 32.0),
        # Right-front angled turquoise shard
        (Vector(( 0.22, -0.14, 0.32)), Vector(( 0.42, -0.32, 0.85)), 0.58, 0.112, 6,  8.0),
        # Back-right upper turquoise crystal
        (Vector(( 0.18,  0.16, 0.36)), Vector(( 0.32,  0.34, 0.88)), 0.62, 0.115, 6, 45.0),
        # Low front-left outcrop shard
        (Vector((-0.38, -0.18, 0.22)), Vector((-0.58, -0.38, 0.72)), 0.42, 0.088, 5, 20.0),
        # Low front-right outcrop shard
        (Vector(( 0.38,  0.04, 0.24)), Vector(( 0.62,  0.12, 0.78)), 0.44, 0.090, 6, 12.0),
    ]
    for b_pos, ax_dir, length, rad, segs, tw in turquoise_shards:
        add_faceted_crystal_prism(bm_turq, b_pos, ax_dir, length, rad, segs=segs, twist_deg=tw)

    objs.append(create_mesh_obj("Mineral_Crystals_Turquoise", bm_turq, mats["turquoise"], col_ore, v_min_z=0.20, v_max_z=1.22))

    # 4) Protruding Aqua-Blue Crystals (MAT_Mineral_CrystalAquaBlue)
    bm_aqua = bmesh.new()
    aquablue_shards = [
        # Secondary tall right-center sapphire-aqua spire
        (Vector(( 0.12,  0.02, 0.40)), Vector(( 0.22, -0.08, 0.97)), 0.72, 0.132, 6, 28.0),
        # Front-center forward-jutting aqua crystal
        (Vector((-0.02, -0.22, 0.30)), Vector((-0.05, -0.52, 0.85)), 0.52, 0.105, 6,  5.0),
        # Left-back splaying sapphire crystal
        (Vector((-0.20,  0.18, 0.34)), Vector((-0.38,  0.36, 0.85)), 0.60, 0.114, 6, 40.0),
        # Far-left low accent crystal
        (Vector((-0.42,  0.06, 0.22)), Vector((-0.68,  0.14, 0.72)), 0.40, 0.085, 5, 18.0),
        # Front-right lower accent crystal
        (Vector(( 0.28, -0.24, 0.20)), Vector(( 0.48, -0.46, 0.75)), 0.38, 0.082, 5, 52.0),
    ]
    for b_pos, ax_dir, length, rad, segs, tw in aquablue_shards:
        add_faceted_crystal_prism(bm_aqua, b_pos, ax_dir, length, rad, segs=segs, twist_deg=tw)

    objs.append(create_mesh_obj("Mineral_Crystals_AquaBlue", bm_aqua, mats["aquablue"], col_ore, v_min_z=0.18, v_max_z=1.15))

    # Normalize transforms & verify exact ground pivot at (0, 0, 0)
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objs:
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
        obj.location = Vector((0.0, 0.0, 0.0))
        obj.rotation_euler = Euler((0.0, 0.0, 0.0), "XYZ")
        obj.scale = Vector((1.0, 1.0, 1.0))
    bpy.ops.object.select_all(action="DESELECT")

    return objs


# =============================================================================
# 5. STUDIO LIGHTING & RENDERS (100% MODEL + 5-STAGE EXTRACTION SHEET)
# =============================================================================
def setup_preview_studio(col_studio):
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = 80
    scene.cycles.use_denoising = True
    scene.cycles.max_bounces = 6

    scene.view_settings.view_transform = "AgX"
    try:
        scene.view_settings.look = "AgX - Punchy"
    except Exception:
        pass
    scene.view_settings.exposure = 0.10

    scene.render.film_transparent = True
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"

    world = bpy.data.worlds.new("Mineral_Studio_World")
    scene.world = world
    nt = world.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputWorld")
    bg = nt.nodes.new("ShaderNodeBackground")
    bg.inputs["Color"].default_value = (*hex_col("#D4F2FA"), 1.0)
    bg.inputs["Strength"].default_value = 0.85
    nt.links.new(bg.outputs["Background"], out.inputs["Surface"])

    # Shadow catcher floor at Z = 0.000m
    bm_fl = bmesh.new()
    bmesh.ops.create_circle(bm_fl, cap_ends=True, radius=10.0, segments=48)
    fl_mesh = bpy.data.meshes.new("Studio_Floor_Mesh")
    bm_fl.to_mesh(fl_mesh)
    bm_fl.free()
    fl_obj = bpy.data.objects.new("Studio_ShadowFloor", fl_mesh)
    link_obj(fl_obj, col_studio)
    fl_mat = make_urp_material("Mat_StudioFloor", "#C0DCE6", roughness=0.90, specular=0.05)
    fl_obj.data.materials.append(fl_mat)
    fl_obj.is_shadow_catcher = True

    def add_area_light(name, loc, target, energy, color, size_x, size_y=None):
        ld = bpy.data.lights.new(name, type="AREA")
        ld.energy = energy
        ld.color = color
        ld.shape = "RECTANGLE" if size_y else "SQUARE"
        ld.size = size_x
        if size_y:
            ld.size_y = size_y
        lo = bpy.data.objects.new(name, ld)
        lo.location = Vector(loc)
        lo.rotation_euler = (Vector(target) - lo.location).to_track_quat("-Z", "Y").to_euler()
        link_obj(lo, col_studio)
        return lo

    add_area_light("Light_Key", (-2.6, -3.0, 3.4), (0.0, 0.0, 0.55), energy=360.0, color=(1.00, 0.97, 0.92), size_x=2.2, size_y=2.2)
    add_area_light("Light_Fill", (3.0, -2.4, 2.0), (0.0, 0.0, 0.50), energy=150.0, color=(0.76, 0.96, 1.00), size_x=2.6, size_y=2.2)
    add_area_light("Light_Rim", (1.2, 2.8, 2.8), (0.0, 0.0, 0.65), energy=320.0, color=(0.72, 0.98, 1.00), size_x=2.0, size_y=1.6)

    def add_camera(name, loc, target, lens=58.0, ortho_scale=None):
        cd = bpy.data.cameras.new(name)
        if ortho_scale is not None:
            cd.type = "ORTHO"
            cd.ortho_scale = ortho_scale
        else:
            cd.type = "PERSP"
            cd.lens = lens
        cd.clip_start = 0.05
        cd.clip_end = 100.0
        co = bpy.data.objects.new(name, cd)
        co.location = Vector(loc)
        co.rotation_euler = (Vector(target) - co.location).to_track_quat("-Z", "Y").to_euler()
        link_obj(co, col_studio)
        return co

    cam_front = add_camera("Cam_Front", (0.0, -3.15, 0.95), (0.0, 0.0, 0.56), lens=58.0)
    cam_side  = add_camera("Cam_Side", (-3.15, 0.0, 0.95), (0.0, 0.0, 0.56), lens=58.0)
    cam_3q    = add_camera("Cam_ThreeQuarter", (-2.05, -2.55, 1.25), (0.0, 0.0, 0.55), lens=58.0)
    cam_25d   = add_camera("Cam_25D_SideScrolling", (0.0, -6.0, 1.28), (0.0, 0.0, 0.58), ortho_scale=2.25)

    scene.camera = cam_3q
    return {"front": cam_front, "side": cam_side, "3q": cam_3q, "25d": cam_25d}


def render_with_backdrop(cam, filepath, res_x=1024, res_y=1024, samples=72, bg_top=(0.82, 0.94, 0.98), bg_bot=(0.62, 0.78, 0.85)):
    scene = bpy.context.scene
    scene.camera = cam
    scene.render.resolution_x = res_x
    scene.render.resolution_y = res_y
    scene.render.resolution_percentage = 100
    scene.cycles.samples = samples

    tmp_path = filepath + ".rgba.png"
    scene.render.filepath = tmp_path
    bpy.ops.render.render(write_still=True)

    img = bpy.data.images.load(tmp_path)
    w, h = img.size
    arr = np.empty(w * h * 4, dtype=np.float32)
    img.pixels.foreach_get(arr)
    arr = arr.reshape((h, w, 4))
    bpy.data.images.remove(img)

    u = np.linspace(-1.0, 1.0, w, dtype=np.float32)
    v = np.linspace(0.0, 1.0, h, dtype=np.float32)
    U, V = np.meshgrid(u, v)

    top_c = np.array(bg_top, dtype=np.float32)
    bot_c = np.array(bg_bot, dtype=np.float32)
    grad = bot_c * (1.0 - V[..., None]) + top_c * V[..., None]
    rad = np.sqrt((U * 0.85) ** 2 + ((V - 0.48) * 1.0) ** 2)
    glow = np.exp(-(rad ** 2) * 1.8) * 0.14
    bg = np.clip(grad + glow[..., None], 0.0, 1.0)

    alpha = arr[..., 3:4]
    soft_shadow_fade = smoothstep(0.025, 0.085, alpha)
    alpha_clean = np.where(alpha > 0.35, alpha, alpha * soft_shadow_fade)
    comp_rgb = arr[..., :3] * alpha_clean + bg * (1.0 - alpha_clean)

    out_arr = np.ones((h, w, 4), dtype=np.float32)
    out_arr[..., :3] = np.clip(comp_rgb, 0.0, 1.0)

    out_img = bpy.data.images.new("TmpComp", width=w, height=h, alpha=False)
    out_img.pixels.foreach_set(out_arr.ravel())
    out_img.filepath_raw = filepath
    out_img.file_format = "PNG"
    out_img.save()
    bpy.data.images.remove(out_img)

    if os.path.exists(tmp_path):
        os.remove(tmp_path)
    return out_arr


def render_extraction_stages_sheet(ore_objs, cam, filepath):
    """
    Render a visual proof sheet showing the 4 visible extraction states (100%, 75%, 50%, 25%)
    all anchored cleanly on the ground floor Z = 0.000m without sinking or floating!
    """
    panels = []
    for scale_pct in (1.00, 0.75, 0.50, 0.25):
        for o in ore_objs:
            o.scale = Vector((scale_pct, scale_pct, scale_pct))
        bpy.context.view_layer.update()
        tmp_stage_path = os.path.join(WS_RENDERS_DIR, f"_tmp_stage_{int(scale_pct*100)}.png")
        arr = render_with_backdrop(cam, tmp_stage_path, res_x=640, res_y=640, samples=56)
        panels.append(arr)
        if os.path.exists(tmp_stage_path):
            os.remove(tmp_stage_path)

    # Restore 100% uniform scale
    for o in ore_objs:
        o.scale = Vector((1.0, 1.0, 1.0))
    bpy.context.view_layer.update()

    h, w, _ = panels[0].shape
    sep = 6
    total_w = w * 4 + sep * 3
    sheet = np.ones((h, total_w, 4), dtype=np.float32) * 0.25
    sheet[..., 3] = 1.0
    for idx, p in enumerate(panels):
        x0 = idx * (w + sep)
        sheet[:, x0:x0 + w, :] = p

    out_img = bpy.data.images.new("TmpStagesSheet", width=total_w, height=h, alpha=False)
    out_img.pixels.foreach_set(sheet.ravel())
    out_img.filepath_raw = filepath
    out_img.file_format = "PNG"
    out_img.save()
    bpy.data.images.remove(out_img)


# =============================================================================
# 6. FBX EXPORT & UNITY SYNC
# =============================================================================
def export_ore_fbx(ore_objs, fbx_filepath):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in ore_objs:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = ore_objs[0]

    bpy.ops.export_scene.fbx(
        filepath=fbx_filepath,
        use_selection=True,
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        use_space_transform=True,
        bake_space_transform=False,
        object_types={"MESH"},
        use_mesh_modifiers=True,
        mesh_smooth_type="FACE",
        use_tspace=True,
        use_custom_props=True,
        path_mode="COPY",
        embed_textures=True,
        axis_forward="-Z",
        axis_up="Y",
    )
    bpy.ops.object.select_all(action="DESELECT")


def sync_to_unity_project(stats_report):
    mats_manifest = {
        "asset_name": "Mineral_Ore",
        "world": "World_01",
        "render_pipeline": "Unity 6 URP (Universal Render Pipeline)",
        "shader_default": "Universal Render Pipeline/Lit",
        "uniform_initial_scale": [1.0, 1.0, 1.0],
        "extraction_states": [
            {"state": 1, "scale_factor": 1.00, "description": "100% de tamaño inicial"},
            {"state": 2, "scale_factor": 0.75, "description": "75% de tamaño"},
            {"state": 3, "scale_factor": 0.50, "description": "50% de tamaño"},
            {"state": 4, "scale_factor": 0.25, "description": "25% de tamaño"},
            {"state": 5, "scale_factor": 0.00, "description": "0%, desaparece"},
        ],
        "materials": [
            {
                "name": "MAT_Mineral_RockBase",
                "shader": "Universal Render Pipeline/Lit",
                "base_map": "Textures/Tex_Mineral_RockBase.png",
                "base_color_srgb": "#8195A7",
                "smoothness": 0.26,
                "metallic": 0.05,
            },
            {
                "name": "MAT_Mineral_CrystalTurquoise",
                "shader": "Universal Render Pipeline/Lit",
                "base_map": "Textures/Tex_Mineral_CrystalTurquoise.png",
                "base_color_srgb": "#24E8F5",
                "emission_color_srgb": "#1CE8F5",
                "emission_intensity": 1.15,
                "smoothness": 0.94,
                "metallic": 0.0,
            },
            {
                "name": "MAT_Mineral_CrystalAquaBlue",
                "shader": "Universal Render Pipeline/Lit",
                "base_map": "Textures/Tex_Mineral_CrystalAquaBlue.png",
                "base_color_srgb": "#1A8CFF",
                "emission_color_srgb": "#1FA8FF",
                "emission_intensity": 1.10,
                "smoothness": 0.94,
                "metallic": 0.0,
            },
            {
                "name": "MAT_Mineral_LuminousVeins",
                "shader": "Universal Render Pipeline/Lit",
                "base_map": "Textures/Tex_Mineral_LuminousVeins.png",
                "base_color_srgb": "#48F6FF",
                "emission_color_srgb": "#42F8FF",
                "emission_intensity": 2.20,
                "smoothness": 0.88,
                "metallic": 0.0,
            },
        ],
        "geometry_stats": stats_report,
    }

    with open(os.path.join(WS_MATS_DIR, "Mineral_Ore_URP_Materials.json"), "w", encoding="utf-8") as f:
        json.dump(mats_manifest, f, indent=2, ensure_ascii=False)

    for sub in ("Models", "Materials", "Textures", "Renders", "Documentation"):
        src_sub = os.path.join(WS_BASE_DIR, sub)
        dst_sub = os.path.join(UNITY_BASE_DIR, sub)
        os.makedirs(dst_sub, exist_ok=True)
        for fname in os.listdir(src_sub):
            s_file = os.path.join(src_sub, fname)
            if os.path.isfile(s_file) and not fname.endswith(".blend1"):
                shutil.copy2(s_file, os.path.join(dst_sub, fname))


def build_all():
    col_ore, col_studio = clear_scene()
    mats, _ = build_ore_materials()
    ore_objs = build_mineral_ore_geometry(col_ore, mats)

    total_verts = total_faces = total_tris = 0
    min_x = min_y = min_z = 1e9
    max_x = max_y = max_z = -1e9
    part_stats = []

    deps = bpy.context.evaluated_depsgraph_get()
    for obj in ore_objs:
        ev = obj.evaluated_get(deps)
        me = ev.to_mesh()
        me.calc_loop_triangles()
        v_cnt = len(me.vertices)
        f_cnt = len(me.polygons)
        t_cnt = len(me.loop_triangles)
        total_verts += v_cnt
        total_faces += f_cnt
        total_tris += t_cnt
        for v in me.vertices:
            wco = obj.matrix_world @ v.co
            min_x, max_x = min(min_x, wco.x), max(max_x, wco.x)
            min_y, max_y = min(min_y, wco.y), max(max_y, wco.y)
            min_z, max_z = min(min_z, wco.z), max(max_z, wco.z)
        part_stats.append({
            "name": obj.name,
            "verts": v_cnt,
            "faces": f_cnt,
            "tris": t_cnt,
            "materials": [m.name for m in me.materials if m],
        })
        ev.to_mesh_clear()

    stats_report = {
        "mesh_objects_count": len(ore_objs),
        "total_verts": total_verts,
        "total_faces": total_faces,
        "total_tris": total_tris,
        "bounds_min": [round(min_x, 4), round(min_y, 4), round(min_z, 4)],
        "bounds_max": [round(max_x, 4), round(max_y, 4), round(max_z, 4)],
        "dimensions_m": {
            "width_x": round(max_x - min_x, 3),
            "depth_y": round(max_y - min_y, 3),
            "height_z": round(max_z - min_z, 3),
        },
        "ground_pivot_z_min": round(min_z, 5),
        "parts": part_stats,
    }

    print("=" * 78)
    print("MINERAL_ORE (WORLD 01) — GEOMETRY & GROUND PIVOT VERIFICATION")
    print(f"  Meshes     : {len(ore_objs)}")
    print(f"  Vertices   : {total_verts} | Faces: {total_faces} | Triangles: {total_tris}")
    print(f"  Dimensions : Width X={stats_report['dimensions_m']['width_x']}m | "
          f"Depth Y={stats_report['dimensions_m']['depth_y']}m | "
          f"Height Z={stats_report['dimensions_m']['height_z']}m (Z_min={min_z:.5f}m)")
    print("=" * 78)

    fbx_path = os.path.join(WS_MODELS_DIR, "Mineral_Ore.fbx")
    export_ore_fbx(ore_objs, fbx_path)

    cams = setup_preview_studio(col_studio)
    render_with_backdrop(cams["3q"], os.path.join(WS_RENDERS_DIR, "render_three_quarter.png"), res_x=1024, res_y=1024, samples=80)
    render_with_backdrop(cams["front"], os.path.join(WS_RENDERS_DIR, "render_front.png"), res_x=1024, res_y=1024, samples=72)
    render_with_backdrop(cams["side"], os.path.join(WS_RENDERS_DIR, "render_side.png"), res_x=1024, res_y=1024, samples=72)
    render_extraction_stages_sheet(ore_objs, cams["3q"], os.path.join(WS_RENDERS_DIR, "render_extraction_states_100_75_50_25.png"))

    # Clean studio objects before saving master .blend
    for obj in list(col_studio.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    bpy.data.collections.remove(col_studio)
    for block_list in (bpy.data.meshes, bpy.data.materials, bpy.data.lights, bpy.data.cameras, bpy.data.worlds):
        for item in list(block_list):
            if item.users == 0:
                block_list.remove(item)

    blend_path = os.path.join(WS_MODELS_DIR, "Mineral_Ore.blend")
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    sync_to_unity_project(stats_report)
    print("Mineral_Ore build, renders, FBX export, and Unity sync complete!")


if __name__ == "__main__":
    build_all()
