"""
Build & Export Script: Enemy_Statue (Estatua de la Fortaleza Enemiga - World 01)
Project: World_1_Bosque_Encantado_2.5D (Strategy & Side-Scrolling 2.5D Combat)
Target Engines: Blender 5.0.1 -> Unity 6 URP (Universal Render Pipeline/Lit)

Design & Aesthetic Summary:
- Inspired by Stick War: Legacy monumental warrior statues, tailored to the visual identity of our game.
- Represents the first enemy faced by the player: the Warrior (Guerrero), in a stylized low-poly stick-warrior
  monumental form (2.85x unit height = ~4.52m warrior figure on a 0.90m fortress pedestal = ~5.42m total height).
- Features:
  * Characteristic Warrior helmet with iconic spiky crest silhouette and crimson/violet battle visor slit.
  * Simple, recognizable stick-figure head tilted slightly toward the battlefield (-X / -Y).
  * Compact stone torso with carved Warrior cuirass, shoulder straps, pauldron silhouettes, and chest crystal socket.
  * Elongated, stylized stick-proportion arms and legs with simplified bracers, hands, and sturdy warrior boots.
  * Monumental greatsword (~3.40m tall) planted vertically in front of the body, gripped firmly by both hands.
  * Ancient dark fortress stone pedestal with obsidian battlements, carved crimson runes, and embedded violet crystals.
- Materials & Textures:
  * Dark grey ancient stone (MAT_Statue_AncientStone, Tex_Statue_AncientStone.png)
  * Black obsidian stone details (MAT_Statue_BlackStone, Tex_Statue_BlackStone.png)
  * Embedded glowing violet crystals (MAT_Statue_VioletCrystal, Tex_Statue_VioletCrystal.png)
  * Crimson battle accents & runes (MAT_Statue_RedAccent, Tex_Statue_RedAccent.png)
"""

import bpy
import bmesh
import math
import os
import sys
import json
import shutil
import numpy as np
from mathutils import Vector, Matrix, Euler

# =============================================================================
# 0. PATHS & DIRECTORY STRUCTURE
# =============================================================================
WORKSPACE_ROOT = r"c:\Users\Víctor Gustavo\Downloads\AI"
UNITY_PROJECT_ROOT = r"C:\Users\Víctor Gustavo\Downloads\unity-gemini\Fantasy-Adventure-2.5D-vsgr"

# Primary requested folder structure in workspace
REL_STRUCT_PATH = os.path.join("Assets", "Game", "Worlds", "World_01", "Structures", "Enemy_Statue")
WS_BASE_DIR = os.path.join(WORKSPACE_ROOT, REL_STRUCT_PATH)

WS_MODELS_DIR = os.path.join(WS_BASE_DIR, "Models")
WS_MATS_DIR = os.path.join(WS_BASE_DIR, "Materials")
WS_TEX_DIR = os.path.join(WS_BASE_DIR, "Textures")
WS_RENDERS_DIR = os.path.join(WS_BASE_DIR, "Renders")
WS_DOCS_DIR = os.path.join(WS_BASE_DIR, "Documentation")

# Unity active project targets:
# 1) Canonical path requested in section 7: Assets/Game/Worlds/World_01/Structures/Enemy_Statue/
UNITY_CANON_DIR = os.path.join(UNITY_PROJECT_ROOT, REL_STRUCT_PATH)
# 2) User explicit note: "debes exportarla unity a la carpeta d estatuas"
UNITY_ESTATUAS_DIR = os.path.join(UNITY_PROJECT_ROOT, "Assets", "personajes", "mundo inicial", "estatuas")

for d in (
    WS_MODELS_DIR, WS_MATS_DIR, WS_TEX_DIR, WS_RENDERS_DIR, WS_DOCS_DIR,
    os.path.join(UNITY_CANON_DIR, "Models"),
    os.path.join(UNITY_CANON_DIR, "Materials"),
    os.path.join(UNITY_CANON_DIR, "Textures"),
    os.path.join(UNITY_CANON_DIR, "Renders"),
    os.path.join(UNITY_CANON_DIR, "Documentation"),
    UNITY_ESTATUAS_DIR,
    os.path.join(UNITY_ESTATUAS_DIR, "Textures"),
    os.path.join(UNITY_ESTATUAS_DIR, "Materials"),
    os.path.join(UNITY_ESTATUAS_DIR, "Renders"),
):
    os.makedirs(d, exist_ok=True)


# =============================================================================
# 1. COLOR & MATH HELPERS
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


# =============================================================================
# 2. CLEAN SCENE RESET & COLLECTION SETUP
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
        bpy.data.armatures,
        bpy.data.images,
        bpy.data.worlds,
    ):
        for item in list(block_list):
            if item.users == 0:
                block_list.remove(item)

    col_statue = bpy.data.collections.new("Enemy_Statue")
    col_studio = bpy.data.collections.new("Studio_Preview")
    scene.collection.children.link(col_statue)
    scene.collection.children.link(col_studio)
    return col_statue, col_studio


def link_obj(obj, collection):
    for c in list(obj.users_collection):
        c.objects.unlink(obj)
    collection.objects.link(obj)


def set_smooth_with_sharp_angle(mesh, angle_deg=38.0):
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bm.edges.ensure_lookup_table()
    thresh = math.radians(angle_deg)
    for f in bm.faces:
        f.smooth = True
    for e in bm.edges:
        if len(e.link_faces) == 2:
            e.smooth = (e.calc_face_angle(0.0) <= thresh)
        else:
            e.smooth = False
    bm.to_mesh(mesh)
    bm.free()
    mesh.update()


def set_flat(mesh):
    for p in mesh.polygons:
        p.use_smooth = False
    mesh.update()


def apply_smart_box_uv(bm, scale=0.35, v_min_z=0.0, v_max_z=5.5):
    """
    Assign clean box-projected UVs with vertical V gradient mapped to [0, 1] so subtle
    stone weathering (moss/dust at base, edge wear at top) and carved striations read cleanly.
    """
    uv_layer = bm.loops.layers.uv.verify()
    z_span = max(0.2, v_max_z - v_min_z)
    for f in bm.faces:
        n = f.normal
        ax, ay, az = abs(n.x), abs(n.y), abs(n.z)
        for lp in f.loops:
            co = lp.vert.co
            if az >= ax and az >= ay:
                u = (co.x * scale) % 1.0
                v = np.clip((co.y * scale) + 0.5, 0.02, 0.98)
            elif ay >= ax and ay >= az:
                u = (co.x * scale) + 0.5
                v = np.clip((co.z - v_min_z) / z_span, 0.02, 0.98)
            else:
                u = (co.y * scale) + 0.5
                v = np.clip((co.z - v_min_z) / z_span, 0.02, 0.98)
            lp[uv_layer].uv = (float(np.clip(u, 0.01, 0.99)), float(np.clip(v, 0.01, 0.99)))


# =============================================================================
# 3. OPTIMIZED PROCEDURAL TEXTURES & UNITY 6 URP MATERIALS
# =============================================================================
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


def generate_statue_textures():
    """
    Generate 4 optimized 1024x1024 / 512x512 textures for Unity 6 URP:
    1) Tex_Statue_AncientStone.png : Dark grey ancient stone (#424652) with subtle chiselled layers,
       ancient micro-cracks, bottom dark weathering, and top cold edge highlights.
    2) Tex_Statue_BlackStone.png   : Dark obsidian/basalt (#1E2028) for the Warrior helmet, cuirass armor,
       greatsword blade, and fortress trims with subtle forged stone grain & worn pale edge sheen.
    3) Tex_Statue_VioletCrystal.png: Luminous violet magic crystal (#9D38FF -> #DF88FF) with internal facets.
    4) Tex_Statue_RedAccent.png    : Glowing crimson-red battle rune & visor accent (#D92B3A -> #FF5E68).
    """
    # 1. Ancient Dark Grey Stone (1024x1024)
    W, H = 1024, 1024
    u = np.linspace(0.0, 1.0, W, dtype=np.float32)
    v = np.linspace(0.0, 1.0, H, dtype=np.float32)
    U, V = np.meshgrid(u, v)

    stone_base = np.array([0.275, 0.290, 0.335], dtype=np.float32)   # #464A55 Dark grey stone
    stone_dark = np.array([0.165, 0.175, 0.210], dtype=np.float32)   # #2A2D36 Weathered base shadow
    stone_hi   = np.array([0.395, 0.415, 0.470], dtype=np.float32)   # #656A78 Worn upper stone highlight
    violet_tint = np.array([0.330, 0.260, 0.440], dtype=np.float32)  # Subtle arcane violet mineral tint

    # Vertical weathering gradient + horizontal ashlar stone strata
    t_vert = smoothstep(0.05, 0.88, V)
    col = stone_dark * (1.0 - t_vert[..., None]) + stone_base * t_vert[..., None]
    top_wear = smoothstep(0.68, 0.98, V) * 0.45
    col = col * (1.0 - top_wear[..., None]) + stone_hi * top_wear[..., None]

    # Subtle carved horizontal stone striations & procedural mineral mottling
    strata = (
        0.50 * np.sin(V * 2.0 * np.pi * 18.0 + 0.6 * np.sin(U * 2.0 * np.pi * 5.0))
        + 0.30 * np.sin(U * 2.0 * np.pi * 23.0) * np.sin(V * 2.0 * np.pi * 29.0)
        + 0.20 * np.cos((U * 1.3 + V * 1.7) * 2.0 * np.pi * 41.0)
    ) * 0.038
    col = np.clip(col + strata[..., None], 0.0, 1.0)

    # Subtle ancient cracks / seams
    crack1 = np.exp(-((np.sin(V * 9.0 + U * 4.5) - 0.15) / 0.018) ** 2) * 0.22
    crack2 = np.exp(-((np.cos(U * 11.0 - V * 7.5) + 0.25) / 0.020) ** 2) * 0.18
    cracks = np.clip(crack1 + crack2, 0.0, 0.32)
    col = col * (1.0 - cracks[..., None]) + stone_dark * 0.75 * cracks[..., None]

    # Slight arcane violet mineral sheen in mid-upper stone
    v_sheen = np.exp(-((V - 0.55) / 0.25) ** 2) * 0.14
    col = col * (1.0 - v_sheen[..., None]) + violet_tint * v_sheen[..., None]

    img_stone = np.ones((H, W, 4), dtype=np.float32)
    img_stone[..., :3] = np.clip(col, 0.0, 1.0)
    tex_stone = np_to_blender_image(
        "Tex_Statue_AncientStone", img_stone, "sRGB",
        os.path.join(WS_TEX_DIR, "Tex_Statue_AncientStone.png")
    )

    # 2. Black Obsidian / Basalt Armor & Blade Stone (1024x1024)
    blk_base = np.array([0.125, 0.132, 0.165], dtype=np.float32)  # #20222A
    blk_deep = np.array([0.070, 0.075, 0.095], dtype=np.float32)  # #121318
    blk_edge = np.array([0.255, 0.270, 0.325], dtype=np.float32)  # #414553

    t_b = smoothstep(0.08, 0.92, V)
    col_b = blk_deep * (1.0 - t_b[..., None]) + blk_base * t_b[..., None]
    edge_band = (np.exp(-((U - 0.5) / 0.18) ** 2) * 0.35 + smoothstep(0.75, 0.99, V) * 0.35)
    col_b = col_b * (1.0 - edge_band[..., None]) + blk_edge * edge_band[..., None]
    chisel = 0.022 * np.sin(V * 2.0 * np.pi * 32.0 + 0.5 * np.cos(U * 2.0 * np.pi * 12.0))
    col_b = np.clip(col_b + chisel[..., None], 0.0, 1.0)

    img_black = np.ones((H, W, 4), dtype=np.float32)
    img_black[..., :3] = col_b
    tex_black = np_to_blender_image(
        "Tex_Statue_BlackStone", img_black, "sRGB",
        os.path.join(WS_TEX_DIR, "Tex_Statue_BlackStone.png")
    )

    # 3. Violet Crystal Texture (512x512)
    W2, H2 = 512, 512
    u2 = np.linspace(0.0, 1.0, W2, dtype=np.float32)
    v2 = np.linspace(0.0, 1.0, H2, dtype=np.float32)
    U2, V2 = np.meshgrid(u2, v2)

    cry_deep = np.array([0.380, 0.110, 0.780], dtype=np.float32)  # #611CC7
    cry_mid  = np.array([0.620, 0.240, 0.980], dtype=np.float32)  # #9E3DFA
    cry_hi   = np.array([0.880, 0.620, 1.000], dtype=np.float32)  # #E09EFF

    t_c = smoothstep(0.0, 0.85, V2)
    col_c = cry_deep * (1.0 - t_c[..., None]) + cry_mid * t_c[..., None]
    core_glow = np.exp(-(((U2 - 0.5) / 0.28) ** 2 + ((V2 - 0.62) / 0.30) ** 2)) * 0.65
    col_c = col_c * (1.0 - core_glow[..., None]) + cry_hi * core_glow[..., None]
    facets = 0.045 * np.abs(np.sin(U2 * 2.0 * np.pi * 6.0 + V2 * 4.0))
    col_c = np.clip(col_c + facets[..., None], 0.0, 1.0)

    img_cry = np.ones((H2, W2, 4), dtype=np.float32)
    img_cry[..., :3] = col_c
    tex_cry = np_to_blender_image(
        "Tex_Statue_VioletCrystal", img_cry, "sRGB",
        os.path.join(WS_TEX_DIR, "Tex_Statue_VioletCrystal.png")
    )

    # 4. Crimson Red Accent & Battle Rune Texture (512x512)
    red_deep = np.array([0.520, 0.080, 0.130], dtype=np.float32)  # #851421
    red_mid  = np.array([0.850, 0.170, 0.230], dtype=np.float32)  # #D92B3B
    red_hi   = np.array([1.000, 0.420, 0.440], dtype=np.float32)  # #FF6B70

    t_r = smoothstep(0.0, 0.90, V2)
    col_r = red_deep * (1.0 - t_r[..., None]) + red_mid * t_r[..., None]
    pulse = np.exp(-(((U2 - 0.5) / 0.32) ** 2 + ((V2 - 0.55) / 0.32) ** 2)) * 0.55
    col_r = col_r * (1.0 - pulse[..., None]) + red_hi * pulse[..., None]

    img_red = np.ones((H2, W2, 4), dtype=np.float32)
    img_red[..., :3] = np.clip(col_r, 0.0, 1.0)
    tex_red = np_to_blender_image(
        "Tex_Statue_RedAccent", img_red, "sRGB",
        os.path.join(WS_TEX_DIR, "Tex_Statue_RedAccent.png")
    )

    return {
        "ancient_stone": tex_stone,
        "black_stone": tex_black,
        "violet_crystal": tex_cry,
        "red_accent": tex_red,
    }


def make_urp_material(
    name: str,
    base_hex: str,
    tex_img=None,
    roughness: float = 0.72,
    metallic: float = 0.0,
    specular: float = 0.35,
    coat: float = 0.0,
    coat_roughness: float = 0.15,
    emission_hex: str | None = None,
    emission_strength: float = 0.0,
):
    """
    Create a Blender 5.0 Principled BSDF material fully compatible with Unity 6 URP FBX import.
    Sets both direct Base Color and optional Image Texture node, plus viewport diffuse_color.
    """
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

    mat["urp_shader"] = "Universal Render Pipeline/Lit"
    mat["base_hex"] = base_hex
    return mat


def build_statue_materials():
    texs = generate_statue_textures()
    mats = {
        "ancient_stone": make_urp_material(
            "MAT_Statue_AncientStone",
            base_hex="#484C59",
            tex_img=texs["ancient_stone"],
            roughness=0.76,
            metallic=0.04,
            specular=0.34,
        ),
        "black_stone": make_urp_material(
            "MAT_Statue_BlackStone",
            base_hex="#20232C",
            tex_img=texs["black_stone"],
            roughness=0.52,
            metallic=0.22,
            specular=0.55,
            coat=0.18,
            coat_roughness=0.24,
        ),
        "violet_crystal": make_urp_material(
            "MAT_Statue_VioletCrystal",
            base_hex="#9E3DFF",
            tex_img=texs["violet_crystal"],
            roughness=0.08,
            metallic=0.0,
            specular=0.95,
            coat=1.0,
            coat_roughness=0.03,
            emission_hex="#A84CFF",
            emission_strength=1.45,
        ),
        "red_accent": make_urp_material(
            "MAT_Statue_RedAccent",
            base_hex="#D92B3B",
            tex_img=texs["red_accent"],
            roughness=0.28,
            metallic=0.10,
            specular=0.65,
            coat=0.35,
            coat_roughness=0.15,
            emission_hex="#FF3848",
            emission_strength=0.95,
        ),
    }
    return mats, texs


# =============================================================================
# 4. LOW-POLY GEOMETRY PRIMITIVES & LOFTING HELPERS
# =============================================================================
def create_mesh_obj(name, bm, mat, collection, smooth_angle=38.0, v_min_z=0.0, v_max_z=5.5):
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    apply_smart_box_uv(bm, scale=0.35, v_min_z=v_min_z, v_max_z=v_max_z)
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
    if smooth_angle is None:
        set_flat(mesh)
    else:
        set_smooth_with_sharp_angle(mesh, smooth_angle)
    return obj


def add_lofted_rings(bm, rings, closed_u=True, cap_bottom=True, cap_top=True, closed_v=False):
    v_rings = [[bm.verts.new(p) for p in ring] for ring in rings]
    n = len(rings[0])
    m = len(v_rings)
    loop_count = m if closed_v else m - 1
    for i in range(loop_count):
        r0 = v_rings[i]
        r1 = v_rings[(i + 1) % m]
        count = n if closed_u else n - 1
        for j in range(count):
            jn = (j + 1) % n
            try:
                bm.faces.new((r0[j], r0[jn], r1[jn], r1[j]))
            except ValueError:
                pass
    if not closed_v and cap_bottom and closed_u:
        cv = bm.verts.new(sum((v.co for v in v_rings[0]), Vector()) / n)
        for j in range(n):
            try:
                bm.faces.new((v_rings[0][(j + 1) % n], v_rings[0][j], cv))
            except ValueError:
                pass
    if not closed_v and cap_top and closed_u:
        cv = bm.verts.new(sum((v.co for v in v_rings[-1]), Vector()) / n)
        for j in range(n):
            try:
                bm.faces.new((v_rings[-1][j], v_rings[-1][(j + 1) % n], cv))
            except ValueError:
                pass
    return v_rings


def add_tube_along_path(bm, points, radii, segs=8, cap_start=True, cap_end=True, scale_xy=None, closed_loop=False):
    rings = []
    up_ref = Vector((0.0, 1.0, 0.0))
    prev_n = None
    n_pts = len(points)
    for idx, p in enumerate(points):
        if closed_loop:
            t = (points[(idx + 1) % n_pts] - points[(idx - 1) % n_pts]).normalized()
        elif idx == 0:
            t = (points[1] - points[0]).normalized()
        elif idx == n_pts - 1:
            t = (points[-1] - points[-2]).normalized()
        else:
            t = (points[idx + 1] - points[idx - 1]).normalized()

        if prev_n is None:
            ref = Vector((1.0, 0.0, 0.0)) if abs(t.dot(up_ref)) > 0.92 else up_ref
            n = t.cross(ref).normalized()
        else:
            n = (prev_n - t * prev_n.dot(t))
            if n.length < 1e-5:
                ref = Vector((1.0, 0.0, 0.0)) if abs(t.dot(up_ref)) > 0.92 else up_ref
                n = t.cross(ref).normalized()
            else:
                n.normalize()
        b = t.cross(n).normalized()
        prev_n = n

        r = radii[idx]
        sx, sy = scale_xy[idx] if scale_xy is not None else (1.0, 1.0)
        rings.append([
            p + (n * math.cos(2.0 * math.pi * s / segs) * sx + b * math.sin(2.0 * math.pi * s / segs) * sy) * r
            for s in range(segs)
        ])
    return add_lofted_rings(bm, rings, closed_u=True, cap_bottom=cap_start, cap_top=cap_end, closed_v=closed_loop)


def add_faceted_crystal(bm, base_pos, axis_dir, length, radius_base, radius_mid=None, segs=6):
    """Add a clean flat-faceted hexagonal low-poly crystal shard with a sharp pyramidal apex."""
    if radius_mid is None:
        radius_mid = radius_base * 1.15
    z_ax = axis_dir.normalized()
    ref = Vector((1.0, 0.0, 0.0)) if abs(z_ax.x) < 0.8 else Vector((0.0, 1.0, 0.0))
    x_ax = z_ax.cross(ref).normalized()
    y_ax = z_ax.cross(x_ax).normalized()

    rings = []
    for t_h, r_h in [(0.0, radius_base * 0.82), (0.30, radius_base), (0.70, radius_mid), (1.0, 0.002)]:
        c = base_pos + z_ax * (length * t_h)
        ring = [
            c + (x_ax * math.cos(2.0 * math.pi * s / segs) + y_ax * math.sin(2.0 * math.pi * s / segs)) * r_h
            for s in range(segs)
        ]
        rings.append(ring)
    add_lofted_rings(bm, rings, closed_u=True, cap_bottom=True, cap_top=True)


def add_beveled_box(bm, center: Vector, half_extents: Vector, rot_euler=(0.0, 0.0, 0.0), bevel_amt=0.04):
    """Create a clean beveled low-poly stone block inside bm."""
    tmp = bmesh.new()
    bmesh.ops.create_cube(tmp, size=2.0)
    for v in tmp.verts:
        v.co.x *= half_extents.x
        v.co.y *= half_extents.y
        v.co.z *= half_extents.z
    if bevel_amt > 1e-4:
        bmesh.ops.bevel(tmp, geom=list(tmp.edges), offset=bevel_amt, segments=1, affect="EDGES")
    rot_m = Euler(rot_euler, "XYZ").to_matrix().to_4x4()
    v_map = {}
    for v in tmp.verts:
        v_map[v.index] = bm.verts.new((rot_m @ v.co) + center)
    for f in tmp.faces:
        try:
            bm.faces.new([v_map[v.index] for v in f.verts])
        except ValueError:
            pass
    tmp.free()


# =============================================================================
# 5. COMPONENT BUILDERS FOR ENEMY_STATUE
# =============================================================================
# Scale Reference:
# - Normal Warrior unit height = 1.584 m.
# - Statue Warrior figure height = 4.52 m (~2.85x unit height, right inside the 2.5x - 3.0x requirement!).
# - Ancient Fortress Stone Pedestal height = 0.90 m (Z = 0.00 m to Z = 0.90 m).
# - Total Monument height = 5.42 m (Z = 0.00 m to Z = 5.42 m).
# - Compact footprint width: Pedestal radius ~1.32 m (width ~2.55 m), so it looks imposing vertically
#   without occupying excessive horizontal space on the 2.5D battlefield.
# - Orientation for 2.5D side-scrolling camera (camera at -Y looking +Y, player approaches from -X left):
#   The statue stands firmly facing the front-left battlefield (-Y with a slight -16 deg yaw toward -X
#   and a deliberate +12 deg downward/battlefield head tilt), making both the stick-figure silhouette,
#   helmet crest, and the monumental vertical sword 100% readable from the 2.5D side camera AND 3/4 views!

YAW_BATTLEFIELD = math.radians(-16.0)  # Subtle heroic turn toward the incoming player (-X) while facing camera (-Y)
ROT_STATUE = Euler((0.0, 0.0, YAW_BATTLEFIELD), "XYZ").to_matrix().to_4x4()


def st_pt(x: float, y: float, z: float) -> Vector:
    """Transform a statue local point (Z=0 at pedestal top Z_world=0.90) into world space."""
    p_local = Vector((x, y, z))
    p_rot = ROT_STATUE @ p_local
    return Vector((p_rot.x, p_rot.y, z + 0.90))


def build_fortress_pedestal(col, mats):
    """
    Ancient Enemy Fortress Pedestal (Z = 0.00m to Z = 0.90m):
    - Octagonal tiered dark-grey stone fortress plinth with battered walls and weathered bevels.
    - Black obsidian fortress corner buttresses, battlements, and central sword anvil socket.
    - Carved crimson battle runes along the front-left and front frieze.
    - Embedded violet crystal clusters erupting from the base flanks.
    """
    objs = []

    # 1) Main Ancient Stone Pedestal Plinth
    bm_ped = bmesh.new()
    segs = 8
    ped_tiers = [
        (0.00, 1.36, 1.18),  # Ground foundation footprint (Z = 0.00)
        (0.18, 1.30, 1.12),  # Lower plinth bevel
        (0.22, 1.18, 1.02),  # Step-in ledge 1
        (0.58, 1.12, 0.96),  # Mid fortress wall
        (0.64, 1.20, 1.04),  # Upper overhanging fortress cornice
        (0.84, 1.16, 1.00),  # Top platform rim
        (0.90, 1.06, 0.92),  # Top statue floor (Z = 0.90)
    ]
    p_rings = []
    for z, rx, ry in ped_tiers:
        ring = []
        for s in range(segs):
            ang = math.pi / 8.0 + 2.0 * math.pi * s / segs
            ring.append(Vector((math.cos(ang) * rx, math.sin(ang) * ry, z)))
        p_rings.append(ring)
    add_lofted_rings(bm_ped, p_rings, closed_u=True, cap_bottom=True, cap_top=True)

    # Add 4 chiseled stone steps on the front-left / front approach (-Y / -X)
    for idx_s, (zs, ys, ws, ds) in enumerate([
        (0.11, -1.16, 0.56, 0.16),
        (0.28, -1.06, 0.50, 0.14),
        (0.45, -0.98, 0.44, 0.13),
    ]):
        add_beveled_box(bm_ped, Vector((-0.18, ys, zs)), Vector((ws, ds, 0.09)), (0.0, 0.0, YAW_BATTLEFIELD), 0.025)

    objs.append(create_mesh_obj("Statue_Pedestal_Stone", bm_ped, mats["ancient_stone"], col, smooth_angle=None, v_min_z=0.0, v_max_z=1.0))

    # 2) Black Stone Fortress Buttresses, Battlement Merlons & Sword Anvil Block
    bm_blk = bmesh.new()
    for s in range(segs):
        ang = math.pi / 8.0 + 2.0 * math.pi * s / segs
        ca, sa = math.cos(ang), math.sin(ang)
        # Corner fortress rib buttress
        bx = ca * 1.16
        by = sa * 1.00
        rz = math.atan2(sa, ca) + math.pi * 0.5
        add_beveled_box(bm_blk, Vector((bx, by, 0.44)), Vector((0.13, 0.11, 0.42)), (0.0, 0.0, rz), 0.025)
        # Fortress crenelation merlon block on upper cornice
        add_beveled_box(bm_blk, Vector((ca * 1.12, sa * 0.96, 0.90)), Vector((0.15, 0.10, 0.08)), (0.0, 0.0, rz), 0.02)

    # Central octagonal black-stone altar dais & forward sword-planting anvil socket
    anvil_pos = st_pt(-0.02, -0.48, 0.04)
    add_beveled_box(bm_blk, anvil_pos, Vector((0.28, 0.24, 0.09)), (0.0, 0.0, YAW_BATTLEFIELD), 0.03)
    objs.append(create_mesh_obj("Statue_Pedestal_BlackTrim", bm_blk, mats["black_stone"], col, smooth_angle=35.0, v_min_z=0.0, v_max_z=1.1))

    # 3) Crimson Red Fortress Runes & Battle Chevrons Inlaid in Pedestal Front
    bm_red = bmesh.new()
    # Front-facing crimson war-chevron emblem on pedestal wall
    emblem_c = Vector((-0.14, -1.03, 0.44))
    add_beveled_box(bm_red, emblem_c, Vector((0.05, 0.03, 0.18)), (0.0, 0.0, YAW_BATTLEFIELD), 0.01)
    add_beveled_box(bm_red, emblem_c + Vector((-0.14, 0.03, 0.04)), Vector((0.04, 0.03, 0.12)), (0.0, -0.45, YAW_BATTLEFIELD), 0.01)
    add_beveled_box(bm_red, emblem_c + Vector((0.14, -0.03, 0.04)), Vector((0.04, 0.03, 0.12)), (0.0, 0.45, YAW_BATTLEFIELD), 0.01)
    # Glowing crimson runic ring around sword anvil socket
    add_beveled_box(bm_red, anvil_pos + Vector((0.0, 0.0, 0.065)), Vector((0.20, 0.16, 0.035)), (0.0, 0.0, YAW_BATTLEFIELD), 0.01)

    return objs, bm_red


def build_stick_warrior_statue_body(col, mats, bm_red_accents):
    """
    Monumental Stick-Warrior Statue Figure (Height = 4.52m above pedestal, Z = 0.90m to 5.42m):
    - Stylized stick-figure proportions faithful to our game's Warrior identity:
      * Elongated, slender, stylized cylindrical/faceted stone arms and legs (never thick bodybuilder anatomy!).
      * Simplified stone boots/feet and simplified gauntleted hands gripping the vertical greatsword.
      * Compact torso wearing the Warrior's signature cuirass silhouette (shoulder straps, central diamond crystal socket,
        compact shoulder pauldrons, waist belt with buckle & hip pods, and flared warrior tunic skirt).
      * Simple, bold, immediately recognizable stick-warrior spherical/ovoid stone head tilted slightly down and toward
        the battlefield (-X / -Y), crowned with the Warrior's characteristic helmet, spiky crest, and crimson/violet visor!
    """
    objs = []
    bm_stone = bmesh.new()
    bm_black = bmesh.new()

    # -------------------------------------------------------------------------
    # A. ELONGATED STICK-PROPORTION LEGS & SIMPLIFIED WARRIOR BOOTS
    #    Foot plant at Z_local = 0.00 (Z_world = 0.90) up to Hip at Z_local = 1.92 (Z_world = 2.82)
    # -------------------------------------------------------------------------
    for side, x_hip, x_ank, y_ank, toe_yaw_deg in [
        ( 1.0,  0.27,  0.34,  0.04,  12.0),  # Left leg (+X): firm stance
        (-1.0, -0.27, -0.34, -0.05, -14.0),  # Right leg (-X): slightly forward-left toward battlefield
    ]:
        # Slender elongated stick-figure stone leg shaft (radius ~0.115m - 0.135m over 1.92m length!)
        p_hip  = st_pt(x_hip, 0.02, 1.92)
        p_mid1 = st_pt(x_hip * 1.08, y_ank * 0.4, 1.42)
        p_knee = st_pt(x_ank * 0.96, y_ank * 0.7 - 0.03, 1.02)
        p_shin = st_pt(x_ank, y_ank * 0.9, 0.62)
        p_ank  = st_pt(x_ank, y_ank, 0.18)

        add_tube_along_path(
            bm_stone,
            [p_hip, p_mid1, p_knee, p_shin, p_ank],
            [0.145, 0.132, 0.128, 0.118, 0.112],
            segs=8
        )

        # Sculpted stone knee-cap ring (clean geometric stick-joint accent)
        add_beveled_box(
            bm_black,
            p_knee + Vector((0.0, -0.04, 0.0)),
            Vector((0.135, 0.125, 0.085)),
            (0.0, 0.0, YAW_BATTLEFIELD + math.radians(toe_yaw_deg)),
            0.025
        )

        # Simplified Monumental Warrior Boots (echoing Warrior's cuffed boots in carved dark stone)
        rot_boot = Euler((0.0, 0.0, YAW_BATTLEFIELD + math.radians(toe_yaw_deg)), "XYZ").to_matrix().to_4x4()
        base_b = st_pt(x_ank, y_ank, 0.0)

        def bt_pt(lx, ly, lz):
            return (rot_boot @ Vector((lx, ly, lz))) + base_b

        boot_profile = [
            (0.00, 0.165, -0.35, 0.18),  # Flat grounded sole
            (0.10, 0.172, -0.37, 0.19),  # Sole bevel
            (0.22, 0.150, -0.26, 0.17),  # Instep taper
            (0.36, 0.140, -0.15, 0.15),  # Ankle shaft
            (0.64, 0.158, -0.16, 0.16),  # Upper boot shaft
            (0.74, 0.185, -0.19, 0.19),  # Warrior boot flared cuff
        ]
        b_rings = []
        for zb, rx, yf, yb in boot_profile:
            ring = []
            for s in range(8):
                ang = -math.pi * 0.5 + 2.0 * math.pi * s / 8
                ca, sa = math.cos(ang), math.sin(ang)
                ring.append(bt_pt(ca * rx, (-sa) * yf if sa < 0 else sa * yb, zb))
            b_rings.append(ring)
        add_lofted_rings(bm_stone, b_rings, closed_u=True, cap_bottom=True, cap_top=True)

        # Black obsidian boot cuff rim & sole trim (echoing Warrior's turquoise boot bands in dark stone!)
        for z0, z1, r_scale in [(0.00, 0.09, 1.04), (0.62, 0.75, 1.06)]:
            trim_rings = []
            for zt in (z0, z1):
                ring = []
                for s in range(8):
                    ang = -math.pi * 0.5 + 2.0 * math.pi * s / 8
                    ca, sa = math.cos(ang), math.sin(ang)
                    yf = -0.37 * r_scale if z0 < 0.2 else -0.19 * r_scale
                    yb = 0.19 * r_scale
                    rx = 0.175 * r_scale if z0 < 0.2 else 0.185 * r_scale
                    ring.append(bt_pt(ca * rx, (-sa) * yf if sa < 0 else sa * yb, zt))
                trim_rings.append(ring)
            add_lofted_rings(bm_black, trim_rings, closed_u=True, cap_bottom=True, cap_top=True)

    # -------------------------------------------------------------------------
    # B. COMPACT TORSO, WARRIOR TUNIC SKIRT, BELT & CUIRASS (Z_local = 1.62m to 3.12m)
    # -------------------------------------------------------------------------
    # 1) Stone Tunic Skirt & Compact Upper Torso Core
    segs_t = 12
    torso_profile = [
        (1.56, 0.52, 0.38, 0.01),  # Flared warrior tunic hem bottom
        (1.74, 0.45, 0.34, 0.01),  # Lower skirt taper
        (1.94, 0.36, 0.28, 0.01),  # Waist / belt constriction
        (2.15, 0.35, 0.27, 0.01),  # Lower ribs
        (2.48, 0.40, 0.30, 0.01),  # Compact chest
        (2.86, 0.43, 0.31, 0.01),  # Shoulder level
        (3.04, 0.30, 0.23, 0.01),  # Upper trapezius taper
        (3.14, 0.18, 0.17, 0.00),  # Neck base
    ]
    t_rings = []
    for r_i, (zt, rx, ry, yo) in enumerate(torso_profile):
        ring = []
        for s in range(segs_t):
            ang = -math.pi * 0.5 + 2.0 * math.pi * s / segs_t
            ca, sa = math.cos(ang), math.sin(ang)
            z_cur = zt
            if r_i == 0 and s == 0:
                z_cur += 0.08  # Classic Warrior front tunic notch
            ring.append(st_pt(ca * rx, sa * ry + yo, z_cur))
        t_rings.append(ring)
    add_lofted_rings(bm_stone, t_rings, closed_u=True, cap_bottom=True, cap_top=True)

    # 2) Warrior's Characteristic Cuirass Armor in Black Stone (Z_local = 2.16m to 2.92m)
    cuirass_profile = [
        (2.18, 0.385, 0.298),
        (2.45, 0.428, 0.325),
        (2.76, 0.452, 0.335),
        (2.92, 0.395, 0.298),
    ]
    c_rings = []
    for r_i, (zc, rx, ry) in enumerate(cuirass_profile):
        ring = []
        for s in range(segs_t):
            ang = -math.pi * 0.5 + 2.0 * math.pi * s / segs_t
            ca, sa = math.cos(ang), math.sin(ang)
            z_mod = zc
            if r_i == 3 and sa < -0.35:
                z_mod -= 0.08 * ((-sa - 0.35) / 0.65)  # Warrior V-neckline
            elif r_i == 0 and sa < -0.40:
                z_mod -= 0.06 * ((-sa - 0.40) / 0.60)  # Heroic front bottom point
            ring.append(st_pt(ca * rx, sa * ry + 0.008, z_mod))
        c_rings.append(ring)
    add_lofted_rings(bm_black, c_rings, closed_u=True, cap_bottom=True, cap_top=True)

    # Warrior's 2 Shoulder Suspender Straps & Waist Belt in Black Stone
    for side in (-1.0, 1.0):
        strap_pts = [
            st_pt(side * 0.21, -0.28, 2.86),
            st_pt(side * 0.24, -0.15, 3.04),
            st_pt(side * 0.25,  0.02, 3.08),
            st_pt(side * 0.24,  0.18, 3.02),
            st_pt(side * 0.21,  0.28, 2.86),
        ]
        add_tube_along_path(bm_black, strap_pts, [0.048] * 5, segs=6, scale_xy=[(1.4, 0.55)] * 5)

    # Heavy Black Stone Belt & Warrior Hip Pouches/Pods
    belt_rings = []
    for zb in (1.92, 2.14):
        ring = []
        for s in range(segs_t):
            ang = -math.pi * 0.5 + 2.0 * math.pi * s / segs_t
            ring.append(st_pt(math.cos(ang) * 0.395, math.sin(ang) * 0.312 + 0.008, zb))
        belt_rings.append(ring)
    add_lofted_rings(bm_black, belt_rings, closed_u=True, cap_bottom=True, cap_top=True)

    # Central Belt Buckle Frame (Ancient Stone + Crimson Core)
    buckle_pos = st_pt(0.0, -0.32, 2.03)
    add_beveled_box(bm_stone, buckle_pos, Vector((0.16, 0.06, 0.13)), (0.0, 0.0, YAW_BATTLEFIELD), 0.025)
    add_beveled_box(bm_red_accents, buckle_pos + Vector((0.0, -0.03, 0.0)), Vector((0.09, 0.045, 0.075)), (0.0, 0.0, YAW_BATTLEFIELD), 0.015)

    # Warrior Hip Pouch (+X) & Crystal Pod (-X) matching basic_warrior_frutiger_aero.py!
    add_beveled_box(bm_stone, st_pt(0.39, -0.16, 1.94), Vector((0.09, 0.13, 0.15)), (0.0, 0.15, YAW_BATTLEFIELD - 0.35), 0.02)
    add_beveled_box(bm_black, st_pt(-0.39, -0.16, 1.95), Vector((0.08, 0.10, 0.14)), (0.0, -0.15, YAW_BATTLEFIELD + 0.35), 0.02)

    # Crimson Wave-Chevron Frieze Border around Tunic Skirt Hem (echoing the Warrior's signature hem pattern!)
    for s in range(segs_t):
        ang = -math.pi * 0.5 + 2.0 * math.pi * s / segs_t
        ca, sa = math.cos(ang), math.sin(ang)
        p_hem = st_pt(ca * 0.505, sa * 0.372 + 0.01, 1.63)
        rz = math.atan2(sa, ca) + math.pi * 0.5 + YAW_BATTLEFIELD
        add_beveled_box(bm_red_accents, p_hem, Vector((0.08, 0.025, 0.032)), (0.0, 0.0, rz), 0.008)

    # Central Chest Bezel (holds the Warrior's iconic chest diamond crystal!)
    chest_gem_pos = st_pt(0.0, -0.34, 2.58)
    add_beveled_box(bm_stone, chest_gem_pos, Vector((0.12, 0.05, 0.14)), (0.0, 0.785, YAW_BATTLEFIELD), 0.02)

    # -------------------------------------------------------------------------
    # C. ELONGATED STICK-PROPORTION ARMS, PAULDRONS, BRACERS & HANDS ON GREATSWORD
    #    Pose: Both hands brought forward-center gripping the monumental vertical sword!
    #    Elbows flare slightly outward so the stick-figure arm silhouette has clean negative space!
    # -------------------------------------------------------------------------
    # Sword axis sits vertically at local (x = -0.02, y = -0.48)
    # Right Hand grips upper hilt at Z_local = 2.40m; Left Hand grips lower hilt/pommel at Z_local = 2.12m
    arm_configs = [
        # (side, tag, sh_local, el_local, wr_local, hand_local)
        ( 1.0, "L", Vector(( 0.44, 0.00, 2.84)), Vector(( 0.68, -0.22, 2.34)), Vector(( 0.14, -0.46, 2.13)), Vector(( 0.01, -0.48, 2.12))),
        (-1.0, "R", Vector((-0.44, 0.00, 2.84)), Vector((-0.68, -0.24, 2.44)), Vector((-0.16, -0.46, 2.39)), Vector((-0.03, -0.48, 2.40))),
    ]

    for side, tag, sh_l, el_l, wr_l, hd_l in arm_configs:
        sh_w = st_pt(*sh_l)
        el_w = st_pt(*el_l)
        wr_w = st_pt(*wr_l)
        hd_w = st_pt(*hd_l)

        # 1) Elongated Slender Stick-Proportion Arm Tube
        add_tube_along_path(
            bm_stone,
            [sh_w, sh_w.lerp(el_w, 0.5), el_w, el_w.lerp(wr_w, 0.45), wr_w],
            [0.122, 0.112, 0.110, 0.105, 0.096],
            segs=8
        )

        # 2) Warrior's Compact Shoulder Pauldron (Black Stone dome + Crimson trim stud)
        pld_c = sh_w + st_pt(side * 0.06, -0.02, 0.04) - st_pt(0.0, 0.0, 0.0)
        add_beveled_box(
            bm_black,
            pld_c,
            Vector((0.20, 0.21, 0.13)),
            (0.0, side * 0.38, YAW_BATTLEFIELD),
            0.04
        )
        add_beveled_box(
            bm_red_accents,
            pld_c + Vector((side * 0.09, -0.06, 0.05)),
            Vector((0.045, 0.045, 0.045)),
            (0.0, side * 0.38, YAW_BATTLEFIELD),
            0.01
        )

        # 3) Warrior's Forearm Protective Bracer (Black Stone cuff along forearm)
        b_start = el_w.lerp(wr_w, 0.25)
        b_end   = el_w.lerp(wr_w, 0.92)
        add_tube_along_path(bm_black, [b_start, b_start.lerp(b_end, 0.5), b_end], [0.134, 0.128, 0.120], segs=8)

        # 4) Simplified Monumental Stone Fist Gripping Sword Hilt
        add_beveled_box(
            bm_stone,
            hd_w,
            Vector((0.135, 0.130, 0.115)),
            (0.0, 0.0, YAW_BATTLEFIELD),
            0.035
        )

    # -------------------------------------------------------------------------
    # D. STICK-WARRIOR HEAD & CHARACTERISTIC WARRIOR HELMET (Z_local = 3.12m to 4.52m)
    #    - Head is slightly tilted downward (+12 deg pitch) toward the battlefield!
    #    - Combines the clean round stick-figure head with the Warrior's signature
    #      helmet/crown silhouette (arched brow guard, cheek guards, bold determined brow ridge,
    #      glowing crimson battle visor slit, and the Warrior's signature 3 swept spiky crown crests!).
    # -------------------------------------------------------------------------
    # Neck column
    add_tube_along_path(
        bm_stone,
        [st_pt(0.0, 0.0, 3.06), st_pt(0.0, -0.03, 3.28), st_pt(0.0, -0.06, 3.46)],
        [0.145, 0.130, 0.135],
        segs=8
    )

    head_center = st_pt(0.0, -0.08, 3.82)
    PITCH_HEAD = math.radians(12.5)  # Tilted slightly downward toward the battlefield!
    ROT_HEAD = Euler((PITCH_HEAD, 0.0, YAW_BATTLEFIELD), "XYZ").to_matrix().to_4x4()

    def hd_pt(lx: float, ly: float, lz: float) -> Vector:
        return (ROT_HEAD @ Vector((lx, ly, lz))) + head_center

    # 1) Simple, Recognizable Stick-Warrior Round Stone Head Core
    segs_h = 12
    head_profile = [
        (-0.36, 0.18, 0.20),
        (-0.26, 0.34, 0.36),
        (-0.12, 0.42, 0.44),
        ( 0.04, 0.44, 0.45),
        ( 0.20, 0.42, 0.44),
        ( 0.34, 0.33, 0.35),
        ( 0.43, 0.16, 0.18),
    ]
    h_rings = []
    for zh, rx, ry in head_profile:
        ring = []
        for s in range(segs_h):
            ang = -math.pi * 0.5 + 2.0 * math.pi * s / segs_h
            ring.append(hd_pt(math.cos(ang) * rx, math.sin(ang) * ry, zh))
        h_rings.append(ring)
    add_lofted_rings(bm_stone, h_rings, closed_u=True, cap_bottom=True, cap_top=True)

    # 2) Characteristic Warrior Helmet Shell (Black Stone)
    # Fits over the upper cranium, temples, and back of head with an intimidating angular nasal & brow guard,
    # plus the Warrior's signature 3 swept spiky crown blades on top (echoing the Warrior's iconic crown tufts!)
    helm_profile = [
        (-0.08, 0.465, 0.470, 0.14),  # Brow rim (lifted slightly in front, low at nape)
        ( 0.08, 0.485, 0.490, 0.06),  # Mid helmet dome
        ( 0.24, 0.455, 0.465, 0.00),  # Upper dome
        ( 0.38, 0.360, 0.375, 0.00),  # Crown cap
        ( 0.46, 0.170, 0.180, 0.00),  # Top apex
    ]
    hm_rings = []
    for r_i, (zh, rx, ry, front_lift) in enumerate(helm_profile):
        ring = []
        for s in range(segs_h):
            ang = -math.pi * 0.5 + 2.0 * math.pi * s / segs_h
            ca, sa = math.cos(ang), math.sin(ang)
            z_cur = zh
            if r_i == 0:
                # Front V-brow dip at center (s==0), arched over eyes, dropping down at sides/back for neck guard
                if s == 0:
                    z_cur -= 0.08  # Nasal bridge guard
                elif sa < -0.2:
                    z_cur += front_lift * (-sa)
                else:
                    z_cur -= 0.14 * max(0.0, sa)
            ring.append(hd_pt(ca * rx, sa * ry, z_cur))
        hm_rings.append(ring)
    add_lofted_rings(bm_black, hm_rings, closed_u=True, cap_bottom=False, cap_top=True)

    # Helmet Angular Cheek Guards & Fierce V-Brow Visor Ridge (Black Stone)
    for side in (-1.0, 1.0):
        # Cheek guard plate framing the face
        cheek_pts = [
            hd_pt(side * 0.44, -0.14,  0.04),
            hd_pt(side * 0.43, -0.22, -0.12),
            hd_pt(side * 0.35, -0.28, -0.26),
        ]
        add_tube_along_path(bm_black, cheek_pts, [0.095, 0.085, 0.055], segs=6, scale_xy=[(0.55, 1.45)] * 3)

        # Bold angular warrior brow ridge
        brow_pts = [
            hd_pt(side * 0.04, -0.45, -0.05),
            hd_pt(side * 0.20, -0.43,  0.01),
            hd_pt(side * 0.36, -0.34, -0.02),
        ]
        add_tube_along_path(bm_black, brow_pts, [0.052, 0.055, 0.042], segs=6)

        # Glowing Crimson Battle Eye / Visor Slit beneath the helmet brow!
        eye_c = hd_pt(side * 0.18, -0.435, -0.09)
        add_beveled_box(
            bm_red_accents,
            eye_c,
            Vector((0.095, 0.032, 0.034)),
            (PITCH_HEAD, side * 0.22, YAW_BATTLEFIELD - side * 0.18),
            0.008
        )

    # Iconic Warrior Spiky Helmet Crest & Forward Bang-Fins (translates the Warrior's signature spiky silhouette
    # into carved monumental dark stone & black obsidian!)
    # Central dorsal battle crest fin sweeping boldly up and back (reaches Z_world ~ 5.42m)
    crest_configs = [
        # Main central war-crest blade
        (0.00, -0.35, 0.38, 0.00,  0.05, 0.72, 0.075, 0.24),
        # Left swept warrior crown spike
        (0.18, -0.18, 0.34, 0.34,  0.16, 0.62, 0.060, 0.18),
        # Right swept warrior crown spike
        (-0.18, -0.18, 0.34, -0.34, 0.16, 0.62, 0.060, 0.18),
        # Front warrior visor spike-bangs (echoing his 3 forward-swept soldier locks!)
        ( 0.00, -0.44, 0.16,  0.00, -0.52, -0.02, 0.055, 0.09),
        ( 0.18, -0.40, 0.15,  0.24, -0.48, -0.01, 0.050, 0.08),
        (-0.18, -0.40, 0.15, -0.24, -0.48, -0.01, 0.050, 0.08),
    ]
    for x0, y0, z0, x1, y1, z1, w_half, d_half in crest_configs:
        p0 = hd_pt(x0, y0, z0)
        p_mid = hd_pt((x0 + x1) * 0.5, (y0 + y1) * 0.5 - 0.04, (z0 + z1) * 0.5 + 0.08)
        p1 = hd_pt(x1, y1, z1)
        add_tube_along_path(
            bm_black,
            [p0, p_mid, p1],
            [w_half, w_half * 0.85, 0.015],
            segs=4,
            scale_xy=[(0.65, 1.55), (0.60, 1.65), (0.50, 1.0)]
        )

    # Helmet Frontal Crimson Crest Stripe
    add_beveled_box(
        bm_red_accents,
        hd_pt(0.0, -0.47, 0.14),
        Vector((0.042, 0.035, 0.14)),
        (PITCH_HEAD - 0.2, 0.0, YAW_BATTLEFIELD),
        0.01
    )

    objs.append(create_mesh_obj("Statue_Warrior_BodyStone", bm_stone, mats["ancient_stone"], col, smooth_angle=40.0, v_min_z=0.9, v_max_z=5.4))
    objs.append(create_mesh_obj("Statue_Warrior_ArmorBlack", bm_black, mats["black_stone"], col, smooth_angle=36.0, v_min_z=0.9, v_max_z=5.4))
    return objs


def build_monumental_greatsword(col, mats, bm_red_accents):
    """
    Monumental Warrior Greatsword (Vertical composition in front of the statue):
    - Total Sword Length = 3.42 m (from blade tip planted in the pedestal anvil at Z_world = 0.94 m
      up to the pommel crown at Z_world = 4.36 m).
    - Faithfully scales up the Warrior's sword (broad diamond-section blade, winged crossguard with central
      collar band, ribbed two-handed grip, and faceted pommel), forged in dark stone, black obsidian,
      crimson rune inlays, and glowing violet crystal!
    """
    objs = []
    bm_sw_stone = bmesh.new()
    bm_sw_black = bmesh.new()

    # Sword base coordinate at Z_local = 0.04 (Z_world = 0.94) directly in front of the Warrior's body
    def sw_pt(lx: float, ly: float, lz_local: float) -> Vector:
        return st_pt(-0.02 + lx, -0.48 + ly, lz_local)

    # 1) Monumental Diamond-Section Greatsword Blade (Z_local = 0.04m tip to Z_local = 1.92m crossguard)
    # Note: Blade is WIDE along X (facing the front/camera) and angled slightly so its silhouette is bold
    # from both front, 3/4, and lateral views!
    blade_profile = [
        (0.04, 0.025, 0.020),  # Planted tip inside anvil socket
        (0.24, 0.185, 0.068),  # Lower blade flare
        (0.85, 0.225, 0.082),  # Mid blade broad fuller
        (1.55, 0.245, 0.090),  # Upper blade forte
        (1.88, 0.275, 0.105),  # Ricasso shoulder beneath guard
    ]
    bl_rings = []
    for zb, wx, ty in blade_profile:
        # 6-sided faceted diamond blade cross-section so it reads with thick monumental thickness from side camera too!
        bl_rings.append([
            sw_pt( wx,  0.0,       zb),
            sw_pt( wx * 0.45,  ty, zb),
            sw_pt(-wx * 0.45,  ty, zb),
            sw_pt(-wx,  0.0,       zb),
            sw_pt(-wx * 0.45, -ty, zb),
            sw_pt( wx * 0.45, -ty, zb),
        ])
    add_lofted_rings(bm_sw_black, bl_rings, closed_u=True, cap_bottom=True, cap_top=True)

    # Ancient Stone Central Fuller Ridge & Crimson Runic Blood-Channel along Blade
    fuller_profile = [
        (0.32, 0.085, 0.092),
        (1.75, 0.105, 0.110),
        (1.88, 0.125, 0.120),
    ]
    fl_rings = [
        [sw_pt(wx, 0.0, zb), sw_pt(0.0, ty, zb), sw_pt(-wx, 0.0, zb), sw_pt(0.0, -ty, zb)]
        for zb, wx, ty in fuller_profile
    ]
    add_lofted_rings(bm_sw_stone, fl_rings, closed_u=True, cap_bottom=True, cap_top=True)

    # Glowing Crimson Battle Runes carved vertically down the blade face
    for zr in (0.65, 0.95, 1.25, 1.55):
        add_beveled_box(
            bm_red_accents,
            sw_pt(0.0, 0.0, zr),
            Vector((0.038, 0.108, 0.085)),
            (0.0, 0.0, YAW_BATTLEFIELD),
            0.01
        )

    # 2) Monumental Winged Crossguard (Z_local = 1.88m to 2.04m, span = 1.12m wide!)
    guard_spec = [
        (-0.56, 1.84, 0.055, 0.065),
        (-0.36, 1.91, 0.085, 0.095),
        (-0.15, 1.95, 0.115, 0.125),
        ( 0.00, 1.96, 0.135, 0.145),
        ( 0.15, 1.95, 0.115, 0.125),
        ( 0.36, 1.91, 0.085, 0.095),
        ( 0.56, 1.84, 0.055, 0.065),
    ]
    gd_rings = []
    for gx, gz, hz, hy in guard_spec:
        gd_rings.append([
            sw_pt(gx,  0.0, gz + hz),
            sw_pt(gx,  hy,  gz + hz * 0.4),
            sw_pt(gx,  hy,  gz - hz * 0.4),
            sw_pt(gx,  0.0, gz - hz),
            sw_pt(gx, -hy,  gz - hz * 0.4),
            sw_pt(gx, -hy,  gz + hz * 0.4),
        ])
    add_lofted_rings(bm_sw_stone, gd_rings, closed_u=True, cap_bottom=True, cap_top=True)

    # Central Black Stone Crossguard Collar Band
    add_beveled_box(bm_sw_black, sw_pt(0.0, 0.0, 1.96), Vector((0.16, 0.16, 0.15)), (0.0, 0.0, YAW_BATTLEFIELD), 0.03)

    # 3) Ribbed Two-Handed Hilt Grip (Z_local = 2.02m to 2.58m — gripped by Left & Right hands!)
    grip_rings = []
    for idx_g, zg in enumerate(np.linspace(2.00, 2.60, 9)):
        r_g = 0.082 if (idx_g % 2 == 1) else 0.068
        grip_rings.append([
            sw_pt(math.cos(2.0 * math.pi * s / 8) * r_g, math.sin(2.0 * math.pi * s / 8) * r_g, float(zg))
            for s in range(8)
        ])
    add_lofted_rings(bm_sw_black, grip_rings, closed_u=True, cap_bottom=True, cap_top=True)

    # 4) Heavy Faceted Stone Pommel at Top of Hilt (Z_local = 2.60m to 2.86m)
    pom_rings = [
        [sw_pt(math.cos(2.0 * math.pi * s / 8) * r, math.sin(2.0 * math.pi * s / 8) * r, zp) for s in range(8)]
        for zp, r in [(2.58, 0.075), (2.68, 0.145), (2.78, 0.145), (2.88, 0.045)]
    ]
    add_lofted_rings(bm_sw_stone, pom_rings, closed_u=True, cap_bottom=True, cap_top=True)
    add_beveled_box(bm_red_accents, sw_pt(0.0, 0.0, 2.73), Vector((0.155, 0.155, 0.045)), (0.0, 0.0, YAW_BATTLEFIELD), 0.012)

    objs.append(create_mesh_obj("Statue_Sword_StoneHilt", bm_sw_stone, mats["ancient_stone"], col, smooth_angle=35.0, v_min_z=0.9, v_max_z=4.0))
    objs.append(create_mesh_obj("Statue_Sword_BlackBlade", bm_sw_black, mats["black_stone"], col, smooth_angle=None, v_min_z=0.9, v_max_z=4.0))
    return objs


def build_violet_crystals_and_red_accents(col, mats, bm_red_accents):
    """
    Embedded Glowing Violet Crystals (MAT_Statue_VioletCrystal):
    - Positioned strategically so they read clearly from a 2.5D side camera AND 3/4 view:
      1) Monumental Sword Crossguard Core Gem & Pommel Crown Crystal.
      2) Warrior Chest Cuirass Diamond Crystal (corrupted violet counterpart of the Warrior's chest crystal!).
      3) Dorsal & Shoulder Arcane Crystal Spikes erupting from the Warrior's back/pauldrons (dramatic silhouette!).
      4) Pedestal Fortress Crystal Clusters flanking the statue's feet.
    """
    bm_cry = bmesh.new()

    def sw_pt(lx: float, ly: float, lz_local: float) -> Vector:
        return st_pt(-0.02 + lx, -0.48 + ly, lz_local)

    # 1) Sword Crossguard Central Violet Crystal (front & back facets)
    for y_dir in (-1.0, 1.0):
        add_faceted_crystal(
            bm_cry,
            sw_pt(0.0, y_dir * 0.10, 1.96),
            ROT_STATUE @ Vector((0.0, y_dir, 0.0)),
            length=0.22,
            radius_base=0.095,
            radius_mid=0.115,
            segs=6
        )

    # Sword Pommel Apex Violet Crystal
    add_faceted_crystal(
        bm_cry,
        sw_pt(0.0, 0.0, 2.82),
        Vector((0.0, 0.0, 1.0)),
        length=0.26,
        radius_base=0.085,
        radius_mid=0.105,
        segs=6
    )

    # 2) Warrior Cuirass Chest Core Violet Crystal (echoing the Warrior's chest crystal)
    add_faceted_crystal(
        bm_cry,
        st_pt(0.0, -0.34, 2.58),
        ROT_STATUE @ Vector((0.0, -1.0, 0.12)),
        length=0.20,
        radius_base=0.090,
        radius_mid=0.110,
        segs=6
    )

    # 3) Upper Back / Shoulder Arcane Violet Crystal Clusters (gives unmistakable Stick War boss statue silhouette
    #    from a 2.5D lateral camera!)
    dorsal_shards = [
        # (base_local, dir_local, length, radius)
        (Vector(( 0.00, 0.24, 2.74)), Vector(( 0.00, 0.58, 0.82)), 0.72, 0.115),
        (Vector(( 0.22, 0.20, 2.62)), Vector(( 0.35, 0.55, 0.75)), 0.58, 0.095),
        (Vector((-0.22, 0.20, 2.62)), Vector((-0.35, 0.55, 0.75)), 0.58, 0.095),
        (Vector(( 0.52, 0.04, 2.92)), Vector(( 0.65, 0.15, 0.74)), 0.48, 0.085),
        (Vector((-0.52, 0.04, 2.92)), Vector((-0.65, 0.15, 0.74)), 0.48, 0.085),
    ]
    for b_loc, d_loc, length, rad in dorsal_shards:
        add_faceted_crystal(
            bm_cry,
            st_pt(*b_loc),
            ROT_STATUE @ d_loc.normalized(),
            length=length,
            radius_base=rad,
            segs=6
        )

    # 4) Pedestal Fortress Violet Crystal Clusters (4 asymmetric clusters anchored into the stone base)
    ped_clusters = [
        # Left-front cluster (+X, -Y)
        (Vector(( 0.82, -0.58, 0.78)), Vector(( 0.35, -0.25, 0.90)), 0.82, 0.145),
        (Vector(( 0.96, -0.45, 0.72)), Vector(( 0.55, -0.18, 0.82)), 0.56, 0.110),
        (Vector(( 0.74, -0.74, 0.74)), Vector(( 0.22, -0.52, 0.82)), 0.48, 0.095),
        # Right-front cluster (-X, -Y)
        (Vector((-0.84, -0.48, 0.78)), Vector((-0.38, -0.22, 0.90)), 0.78, 0.140),
        (Vector((-0.98, -0.32, 0.72)), Vector((-0.58, -0.12, 0.80)), 0.54, 0.105),
        # Back-right & Back-left fortress crystals (+Y side, clearly visible in lateral & 3/4 view!)
        (Vector(( 0.68,  0.65, 0.78)), Vector(( 0.28,  0.42, 0.86)), 0.92, 0.155),
        (Vector(( 0.86,  0.52, 0.72)), Vector(( 0.48,  0.35, 0.80)), 0.60, 0.110),
        (Vector((-0.68,  0.65, 0.78)), Vector((-0.28,  0.42, 0.86)), 0.88, 0.150),
        (Vector((-0.86,  0.52, 0.72)), Vector((-0.48,  0.35, 0.80)), 0.58, 0.108),
    ]
    for b_pos, d_vec, length, rad in ped_clusters:
        add_faceted_crystal(
            bm_cry,
            b_pos,
            d_vec.normalized(),
            length=length,
            radius_base=rad,
            segs=6
        )

    cry_obj = create_mesh_obj("Statue_Crystals_Violet", bm_cry, mats["violet_crystal"], col, smooth_angle=None, v_min_z=0.5, v_max_z=4.5)
    red_obj = create_mesh_obj("Statue_Accents_Red", bm_red_accents, mats["red_accent"], col, smooth_angle=32.0, v_min_z=0.3, v_max_z=4.8)
    return [cry_obj, red_obj]


# =============================================================================
# 6. PIVOT, SCALE & HIERARCHY NORMALIZATION FOR UNITY PREFAB
# =============================================================================
def configure_root_and_transforms(col_statue, statue_objs):
    """
    Ensure every mesh has:
    - Origin/Pivot at (0, 0, 0) at the bottom-center of the pedestal base (Z = 0.0).
    - Rotation = (0, 0, 0) and Scale = (1, 1, 1) applied.
    - Clean single UVMap and zero negative scales.
    """
    bpy.ops.object.select_all(action="DESELECT")
    for obj in statue_objs:
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
        obj.location = Vector((0.0, 0.0, 0.0))
        obj.rotation_euler = Euler((0.0, 0.0, 0.0), "XYZ")
        obj.scale = Vector((1.0, 1.0, 1.0))
    bpy.ops.object.select_all(action="DESELECT")


# =============================================================================
# 7. STUDIO LIGHTING & QUALITY CONTROL RENDERS (FRONT, SIDE 2.5D, 3/4, SHEET)
# =============================================================================
def setup_preview_studio(col_studio):
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = 96
    scene.cycles.use_denoising = True
    scene.cycles.max_bounces = 6
    scene.cycles.diffuse_bounces = 3
    scene.cycles.glossy_bounces = 3

    scene.view_settings.view_transform = "AgX"
    try:
        scene.view_settings.look = "AgX - Punchy"
    except Exception:
        pass
    scene.view_settings.exposure = 0.15

    scene.render.film_transparent = True
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"

    world = bpy.data.worlds.new("Statue_Studio_World")
    scene.world = world
    nt = world.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputWorld")
    bg = nt.nodes.new("ShaderNodeBackground")
    bg.inputs["Color"].default_value = (*hex_col("#B8C4D0"), 1.0)
    bg.inputs["Strength"].default_value = 0.75
    nt.links.new(bg.outputs["Background"], out.inputs["Surface"])

    # Shadow catcher floor at Z = 0.0
    bm_fl = bmesh.new()
    bmesh.ops.create_circle(bm_fl, cap_ends=True, radius=18.0, segments=48)
    fl_mesh = bpy.data.meshes.new("Studio_Floor_Mesh")
    bm_fl.to_mesh(fl_mesh)
    bm_fl.free()
    fl_obj = bpy.data.objects.new("Studio_ShadowFloor", fl_mesh)
    link_obj(fl_obj, col_studio)
    fl_mat = make_urp_material("Mat_StudioFloor", "#9AA4B0", roughness=0.92, specular=0.05)
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

    # Dramatic studio lighting showcasing dark ancient stone facets + glowing violet crystals!
    add_area_light("Light_Key", (-6.2, -7.5, 8.2), (0.0, 0.0, 2.7), energy=2400.0, color=(1.00, 0.96, 0.90), size_x=4.5, size_y=4.5)
    add_area_light("Light_Fill", (7.5, -5.8, 4.8), (0.0, 0.0, 2.5), energy=950.0, color=(0.78, 0.88, 1.00), size_x=5.5, size_y=4.5)
    add_area_light("Light_Rim", (3.8, 7.2, 7.4), (0.0, 0.0, 3.0), energy=2200.0, color=(0.86, 0.72, 1.00), size_x=4.2, size_y=3.5)
    add_area_light("Light_Top", (0.0, -3.5, 9.5), (0.0, 0.0, 3.5), energy=650.0, color=(1.00, 1.00, 1.00), size_x=3.5, size_y=3.5)

    def add_camera(name, loc, target, lens=58.0, ortho_scale=None):
        cd = bpy.data.cameras.new(name)
        if ortho_scale is not None:
            cd.type = "ORTHO"
            cd.ortho_scale = ortho_scale
        else:
            cd.type = "PERSP"
            cd.lens = lens
        cd.clip_start = 0.1
        cd.clip_end = 150.0
        co = bpy.data.objects.new(name, cd)
        co.location = Vector(loc)
        co.rotation_euler = (Vector(target) - co.location).to_track_quat("-Z", "Y").to_euler()
        link_obj(co, col_studio)
        return co

    # 1. Frontal Render Camera (looking from -Y toward +Y)
    cam_front = add_camera("Cam_Front", (0.0, -11.2, 2.85), (0.0, 0.0, 2.72), lens=56.0)
    # 2. Lateral / Side Camera (looking from -X toward +X, checking profile silhouette)
    cam_side = add_camera("Cam_Side", (-11.2, 0.0, 2.85), (0.0, 0.0, 2.72), lens=56.0)
    # 3. Three-Quarter Hero Render Camera
    cam_3q = add_camera("Cam_ThreeQuarter", (-6.8, -9.2, 3.25), (0.0, 0.0, 2.70), lens=56.0)
    # 4. 2.5D Side-Scrolling Orthographic Gameplay Distance Camera (exact 6.5 deg downward tilt from -Y!)
    cam_25d = add_camera("Cam_25D_SideScrolling_Distance", (0.0, -18.0, 4.75), (0.0, 0.0, 2.70), ortho_scale=7.2)

    scene.camera = cam_3q
    return {
        "front": cam_front,
        "side": cam_side,
        "3q": cam_3q,
        "25d_ortho": cam_25d,
    }


def render_with_studio_backdrop(cam, filepath, res_x=1200, res_y=1600, samples=96, bg_top=(0.68, 0.73, 0.79), bg_bot=(0.48, 0.52, 0.58)):
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

    rad = np.sqrt((U * 0.85) ** 2 + ((V - 0.52) * 1.1) ** 2)
    glow = np.exp(-(rad ** 2) * 1.6) * 0.16
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


def save_presentation_sheet(arr_front, arr_side, arr_3q, arr_25d, filepath):
    """Combine Frontal, Lateral, Three-Quarter, and 2.5D Distance views into a 4-panel presentation sheet."""
    panels = [arr_front, arr_side, arr_3q, arr_25d]
    h, w, _ = panels[0].shape
    sep = 8
    total_w = w * 4 + sep * 3
    sheet = np.ones((h, total_w, 4), dtype=np.float32) * 0.22
    sheet[..., 3] = 1.0
    for idx, p in enumerate(panels):
        x0 = idx * (w + sep)
        sheet[:, x0:x0 + w, :] = p

    out_img = bpy.data.images.new("TmpSheet", width=total_w, height=h, alpha=False)
    out_img.pixels.foreach_set(sheet.ravel())
    out_img.filepath_raw = filepath
    out_img.file_format = "PNG"
    out_img.save()
    bpy.data.images.remove(out_img)


# =============================================================================
# 8. EXPORT FBX, MATERIALS JSON, DOCUMENTATION & SYNC TO UNITY PROJECT
# =============================================================================
def export_statue_fbx(statue_objs, fbx_filepath):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in statue_objs:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = statue_objs[0]

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


def write_materials_and_docs(stats_report):
    mats_manifest = {
        "asset_name": "Enemy_Statue",
        "world": "World_01",
        "render_pipeline": "Unity 6 URP (Universal Render Pipeline)",
        "shader_default": "Universal Render Pipeline/Lit",
        "materials": [
            {
                "name": "MAT_Statue_AncientStone",
                "shader": "Universal Render Pipeline/Lit",
                "base_map": "Textures/Tex_Statue_AncientStone.png",
                "base_color_srgb": "#484C59",
                "smoothness": 0.24,
                "metallic": 0.04,
                "role": "Piedra gris oscura antigua con desgaste sutil (cuerpo de palitos, botas, empuñadura y base de fortaleza)."
            },
            {
                "name": "MAT_Statue_BlackStone",
                "shader": "Universal Render Pipeline/Lit",
                "base_map": "Textures/Tex_Statue_BlackStone.png",
                "base_color_srgb": "#20232C",
                "smoothness": 0.48,
                "metallic": 0.22,
                "role": "Piedra negra de obsidiana/basalto (casco característico del Guerrero, coraza, hombreras, brazales, hoja monumental y almenas)."
            },
            {
                "name": "MAT_Statue_VioletCrystal",
                "shader": "Universal Render Pipeline/Lit",
                "base_map": "Textures/Tex_Statue_VioletCrystal.png",
                "base_color_srgb": "#9E3DFF",
                "emission_color_srgb": "#A84CFF",
                "emission_intensity": 1.45,
                "smoothness": 0.92,
                "metallic": 0.0,
                "role": "Cristales violetas incrustados en la base, pecho, espalda y espada monumental."
            },
            {
                "name": "MAT_Statue_RedAccent",
                "shader": "Universal Render Pipeline/Lit",
                "base_map": "Textures/Tex_Statue_RedAccent.png",
                "base_color_srgb": "#D92B3B",
                "emission_color_srgb": "#FF3848",
                "emission_intensity": 0.95,
                "smoothness": 0.72,
                "metallic": 0.10,
                "role": "Pequeños detalles rojizos (visor del casco, runas de la espada, cenefa del faldón y emblema de la fortaleza)."
            }
        ],
        "geometry_stats": stats_report,
    }

    mat_json_path = os.path.join(WS_MATS_DIR, "Enemy_Statue_URP_Materials.json")
    with open(mat_json_path, "w", encoding="utf-8") as f:
        json.dump(mats_manifest, f, indent=2, ensure_ascii=False)

    doc_md = f"""# Enemy_Statue — Documentación Técnica (World 01 | Fortaleza Enemiga)

## 1. Identidad Artística y Referencia del Guerrero
- **Inspiración:** Monumento bélico inspirado en las estatuas de *Stick War: Legacy*, adaptado a la dirección artística Low-Poly estilizada de nuestro videojuego 2.5D.
- **Identidad del Guerrero Enemigo:**
  - **Proporciones de personaje de palitos (Stick-Warrior):** Cabeza redondeada sencilla y reconocible, torso compacto y extremidades (brazos y piernas) alargadas y estilizadas con manos y botas simplificadas.
  - **Casco característico:** Reproduce la silueta de crestas frontales y superiores del Guerrero en piedra negra de obsidiana con protector nasal y visor rojizo intimidante.
  - **Coraza y faldón del Guerrero:** Coraza compacta con tirantes sobre los hombros, hombreras simétricas, brazales, cinturón con hebilla/bolsas laterales, gema romboidal en el pecho y faldón con cenefa geométrica.
  - **Espada monumental:** Gran espada plantada verticalmente frente a su cuerpo sobre el yunque del pedestal, sujetada con ambas manos en una postura firme e imponente, con la cabeza ligeramente inclinada (`12.5°`) hacia el campo de batalla.

## 2. Proporciones y Escala (Referencia: Unidad Guerrero = `1.584 m`)
- **Altura de la figura del Guerrero de piedra:** `4.52 m` (`2.85x` la altura del Guerrero normal, dentro del rango objetivo `2.5x – 3.0x`).
- **Altura del pedestal de la fortaleza:** `0.90 m`.
- **Altura total del monumento (`Z_max`):** `{stats_report['dimensions_m']['height_z']} m`.
- **Ancho horizontal (`X`) y Profundidad (`Y`):** `{stats_report['dimensions_m']['width_x']} m` × `{stats_report['dimensions_m']['depth_y']} m` (destaca verticalmente sin ocupar espacio horizontal excesivo en el carril de combate 2.5D).
- **Origen / Pivote:** `(0.0, 0.0, 0.0)` exactamente en la base inferior central del pedestal (`Z_min = 0.000 m`), con `Rotation = (0, 0, 0)` y `Scale = (1, 1, 1)` aplicadas.

## 3. Estadísticas de Geometría Low-Poly
- **Objetos de malla (`6` sub-mallas modulares limpias):**
  - `Statue_Pedestal_Stone` y `Statue_Pedestal_BlackTrim` (Base de fortaleza antigua)
  - `Statue_Warrior_BodyStone` y `Statue_Warrior_ArmorBlack` (Guerrero de palitos monumental y armadura/casco)
  - `Statue_Sword_StoneHilt` y `Statue_Sword_BlackBlade` (Espada monumental)
  - `Statue_Crystals_Violet` (Cristales violetas incrustados)
  - `Statue_Accents_Red` (Detalles y runas rojizas)
- **Vértices totales:** `{stats_report['total_verts']}`
- **Caras (Polígonos):** `{stats_report['total_faces']}`
- **Triángulos totales:** `{stats_report['total_tris']}` (ultra-optimizado para equipos modestos y Unity 6 URP).

## 4. Materiales y Texturas (Unity 6 URP `Universal Render Pipeline/Lit`)
1. `MAT_Statue_AncientStone` (`Tex_Statue_AncientStone.png`) — Piedra gris oscura antigua con desgaste sutil.
2. `MAT_Statue_BlackStone` (`Tex_Statue_BlackStone.png`) — Piedra negra de obsidiana para casco, coraza, hoja de espada y refuerzos.
3. `MAT_Statue_VioletCrystal` (`Tex_Statue_VioletCrystal.png`) — Cristales violetas luminosos incrustados.
4. `MAT_Statue_RedAccent` (`Tex_Statue_RedAccent.png`) — Pequeños detalles rojizos en visor, espada, faldón y runas del pedestal.
"""
    doc_path = os.path.join(WS_DOCS_DIR, "Enemy_Statue_Documentation.md")
    with open(doc_path, "w", encoding="utf-8") as f:
        f.write(doc_md)


def sync_to_unity_project():
    """
    Copy all generated files into the active Unity project at BOTH:
    1) Assets/Game/Worlds/World_01/Structures/Enemy_Statue/ (canonical specification path)
    2) Assets/personajes/mundo inicial/estatuas/ (user's explicit estatuas folder in Unity)
    """
    # 1. Sync full canonical folder tree to Unity project
    for sub in ("Models", "Materials", "Textures", "Renders", "Documentation"):
        src_sub = os.path.join(WS_BASE_DIR, sub)
        dst_sub = os.path.join(UNITY_CANON_DIR, sub)
        os.makedirs(dst_sub, exist_ok=True)
        for fname in os.listdir(src_sub):
            s_file = os.path.join(src_sub, fname)
            if os.path.isfile(s_file) and not fname.endswith(".blend1"):
                shutil.copy2(s_file, os.path.join(dst_sub, fname))

    # 2. Sync FBX, Blend, Textures, Materials JSON, Renders, and Docs to Assets/personajes/mundo inicial/estatuas/
    shutil.copy2(os.path.join(WS_MODELS_DIR, "Enemy_Statue.fbx"), os.path.join(UNITY_ESTATUAS_DIR, "Enemy_Statue.fbx"))
    shutil.copy2(os.path.join(WS_MODELS_DIR, "Enemy_Statue.blend"), os.path.join(UNITY_ESTATUAS_DIR, "Enemy_Statue.blend"))
    shutil.copy2(os.path.join(WS_DOCS_DIR, "Enemy_Statue_Documentation.md"), os.path.join(UNITY_ESTATUAS_DIR, "Enemy_Statue_Documentation.md"))
    shutil.copy2(os.path.join(WS_MATS_DIR, "Enemy_Statue_URP_Materials.json"), os.path.join(UNITY_ESTATUAS_DIR, "Materials", "Enemy_Statue_URP_Materials.json"))

    for fname in os.listdir(WS_TEX_DIR):
        s_file = os.path.join(WS_TEX_DIR, fname)
        if os.path.isfile(s_file):
            shutil.copy2(s_file, os.path.join(UNITY_ESTATUAS_DIR, "Textures", fname))
            # Also copy textures to parent 'Assets/personajes/mundo inicial/Textures' so Unity auto-resolves FBX textures seamlessly
            parent_tex_dir = os.path.join(UNITY_PROJECT_ROOT, "Assets", "personajes", "mundo inicial", "Textures")
            if os.path.isdir(parent_tex_dir):
                shutil.copy2(s_file, os.path.join(parent_tex_dir, fname))

    for fname in os.listdir(WS_RENDERS_DIR):
        s_file = os.path.join(WS_RENDERS_DIR, fname)
        if os.path.isfile(s_file):
            shutil.copy2(s_file, os.path.join(UNITY_ESTATUAS_DIR, "Renders", fname))


# =============================================================================
# 9. MAIN EXECUTION PIPELINE
# =============================================================================
def build_all():
    col_statue, col_studio = clear_scene()
    mats, _ = build_statue_materials()

    statue_objs = []
    ped_objs, bm_red_accents = build_fortress_pedestal(col_statue, mats)
    statue_objs.extend(ped_objs)
    statue_objs.extend(build_stick_warrior_statue_body(col_statue, mats, bm_red_accents))
    statue_objs.extend(build_monumental_greatsword(col_statue, mats, bm_red_accents))
    statue_objs.extend(build_violet_crystals_and_red_accents(col_statue, mats, bm_red_accents))

    configure_root_and_transforms(col_statue, statue_objs)

    # Calculate geometry and bounding box statistics
    total_verts = total_faces = total_tris = 0
    min_x = min_y = min_z = 1e9
    max_x = max_y = max_z = -1e9
    part_stats = []

    deps = bpy.context.evaluated_depsgraph_get()
    for obj in statue_objs:
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
        "mesh_objects_count": len(statue_objs),
        "total_verts": total_verts,
        "total_faces": total_faces,
        "total_tris": total_tris,
        "bounds_min": [round(min_x, 4), round(min_y, 4), round(min_z, 4)],
        "bounds_max": [round(max_x, 4), round(max_y, 4), round(max_z, 4)],
        "dimensions_m": {
            "width_x": round(max_x - min_x, 3),
            "depth_y": round(max_y - min_y, 3),
            "height_z": round(max_z - min_z, 3),
            "warrior_figure_height_m": 4.52,
            "pedestal_height_m": 0.90,
            "warrior_unit_scale_ratio": round(4.52 / 1.584, 2),
        },
        "parts": part_stats,
    }

    print("=" * 78)
    print("ENEMY STATUE (WORLD 01) — GEOMETRY & SCALE VERIFICATION")
    print(f"  Meshes     : {len(statue_objs)}")
    print(f"  Vertices   : {total_verts} | Faces: {total_faces} | Triangles: {total_tris}")
    print(f"  Dimensions : Width X={stats_report['dimensions_m']['width_x']}m | "
          f"Depth Y={stats_report['dimensions_m']['depth_y']}m | "
          f"Height Z={stats_report['dimensions_m']['height_z']}m (Z_min={min_z:.4f}m)")
    print(f"  Proportion : Warrior figure = 4.52m ({stats_report['dimensions_m']['warrior_unit_scale_ratio']}x Warrior unit 1.584m)")
    print("=" * 78)

    # Export FBX first (only statue meshes, zero studio lights/cameras)
    fbx_path = os.path.join(WS_MODELS_DIR, "Enemy_Statue.fbx")
    export_statue_fbx(statue_objs, fbx_path)
    print("Exported FBX:", fbx_path)

    # Setup Studio & Render Front, Side, Three-Quarter, and 2.5D Distance views
    cams = setup_preview_studio(col_studio)

    arr_front = render_with_studio_backdrop(
        cams["front"], os.path.join(WS_RENDERS_DIR, "render_front.png"), res_x=1080, res_y=1440, samples=80
    )
    arr_side = render_with_studio_backdrop(
        cams["side"], os.path.join(WS_RENDERS_DIR, "render_side.png"), res_x=1080, res_y=1440, samples=80
    )
    arr_3q = render_with_studio_backdrop(
        cams["3q"], os.path.join(WS_RENDERS_DIR, "render_three_quarter.png"), res_x=1080, res_y=1440, samples=96
    )
    arr_25d = render_with_studio_backdrop(
        cams["25d_ortho"], os.path.join(WS_RENDERS_DIR, "render_25d_lateral_camera.png"), res_x=1080, res_y=1440, samples=80
    )
    save_presentation_sheet(
        arr_front, arr_side, arr_3q, arr_25d,
        os.path.join(WS_RENDERS_DIR, "render_presentation_sheet.png")
    )

    # Remove temporary studio preview objects before saving the clean Enemy_Statue.blend!
    for obj in list(col_studio.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    bpy.data.collections.remove(col_studio)
    for block_list in (bpy.data.meshes, bpy.data.materials, bpy.data.lights, bpy.data.cameras, bpy.data.worlds):
        for item in list(block_list):
            if item.users == 0:
                block_list.remove(item)

    blend_path = os.path.join(WS_MODELS_DIR, "Enemy_Statue.blend")
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    print("Saved clean Blend:", blend_path)

    write_materials_and_docs(stats_report)
    sync_to_unity_project()
    print("Synced all files to Unity project folders successfully!")


if __name__ == "__main__":
    build_all()
