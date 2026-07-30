from __future__ import annotations

import json
import math
import random
from datetime import datetime
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont, ImageOps


PROJECT_ROOT = Path(__file__).resolve().parents[1]
OUT_ROOT = PROJECT_ROOT / "Assets" / "Resources" / "Art" / "BattleUnits"
DESIGN_ROOT = PROJECT_ROOT / "Assets" / "Resources" / "Art" / "BattleUnitDesigns"
MANIFEST = OUT_ROOT / "battle_unit_manifest.json"
PREVIEW = PROJECT_ROOT / "DataTables" / "battle_unit_sprites_preview.png"
CONFIG = PROJECT_ROOT / "Assets" / "Resources" / "Data" / "MingLuGameConfig.json"
SOURCE_SHEET_ROOT = PROJECT_ROOT / "DataTables" / "battle_unit_sequence_sources"

SIZE = 256
SCALE = 3
SHEET_COLUMNS = 6
SHEET_ROWS = ("idle", "move", "attack", "hit")
IDLE_FRAMES = 4
MOVE_FRAMES = 6
ATTACK_FRAMES = 6
HIT_FRAMES = 6
RECOVER_FRAMES = 6
DEFEAT_FRAMES = 8
FRAME_COUNTS = {
    "idle": IDLE_FRAMES,
    "move": MOVE_FRAMES,
    "attack": ATTACK_FRAMES,
    "hit": HIT_FRAMES,
    "recover": RECOVER_FRAMES,
    "defeat": DEFEAT_FRAMES,
}
STYLE_LABEL = (
    "painted tactical miniature sprites animated from standing unit designs; "
    "visible left-right gait cycles, attack anticipation, impact recoil, and full-frame raster output"
)

ROLE_DISPLAY = {
    "infantry": "步兵",
    "musket": "火枪",
    "skirmisher": "散兵",
    "heavy_spear": "重枪",
    "heavy_cavalry": "重骑",
    "heavy_infantry": "重步",
    "heavy_archer": "重弓",
    "heavy_brute": "重猛",
    "cavalry": "骑兵",
    "artillery": "重器",
    "brute": "猛士",
    "archer": "弓兵",
}

KEYWORD_FAMILY = {
    "义勇军": "volunteer",
    "禁军": "imperial",
    "贼徒": "outlaw",
    "信徒": "believer",
}

PALETTES = {
    "volunteer": {
        "coat": (27, 70, 103),
        "coat2": (43, 91, 126),
        "pants": (35, 45, 51),
        "trim": (225, 172, 76),
        "red": (155, 47, 37),
        "flag": (48, 99, 139),
        "base": (87, 111, 83),
        "metal": (180, 185, 175),
        "leather": (90, 56, 33),
        "cloth": (197, 165, 102),
        "shadow": (17, 21, 22),
    },
    "imperial": {
        "coat": (94, 41, 57),
        "coat2": (133, 48, 49),
        "pants": (43, 38, 42),
        "trim": (236, 181, 74),
        "red": (184, 38, 38),
        "flag": (178, 36, 40),
        "base": (122, 59, 58),
        "metal": (203, 188, 133),
        "leather": (80, 48, 29),
        "cloth": (214, 178, 112),
        "shadow": (24, 18, 20),
    },
    "outlaw": {
        "coat": (84, 73, 53),
        "coat2": (116, 90, 52),
        "pants": (49, 41, 32),
        "trim": (163, 123, 72),
        "red": (124, 51, 37),
        "flag": (91, 70, 47),
        "base": (91, 78, 53),
        "metal": (139, 138, 120),
        "leather": (86, 53, 31),
        "cloth": (180, 148, 92),
        "shadow": (22, 20, 17),
    },
    "believer": {
        "coat": (110, 43, 43),
        "coat2": (145, 60, 44),
        "pants": (45, 36, 32),
        "trim": (234, 205, 134),
        "red": (176, 35, 34),
        "flag": (158, 34, 34),
        "base": (128, 68, 47),
        "metal": (181, 150, 93),
        "leather": (78, 44, 24),
        "cloth": (215, 179, 118),
        "shadow": (29, 18, 16),
    },
}

FLAG_UNITS = {
    "leader_guard",
    "imperial_halberdiers",
    "believer_elites",
    "armored_iron_cavalry",
    "imperial_heavy_guard",
}

LEFT_FACING_DESIGNS = {
    "swordsmen_volunteers",
    "outlaw_skirmishers",
    "imperial_halberdiers",
    "armored_iron_cavalry",
    "steel_helmet_heavy_infantry",
    "sword_guard_corps",
    "imperial_axe_guard",
    "vanguard_cavalry",
    "raiders",
    "imperial_heavy_guard",
    "zealot_believers",
    "zealot_mob",
    "leader_guard",
    "bandits",
    "great_axe_warriors",
    "believer_elites",
}

FALLBACK_UNITS = [
    ("swordsmen_volunteers", "剑士队", "义勇军", "infantry"),
    ("matchlock_volunteers", "火绳枪队", "义勇军", "musket"),
    ("militia_volunteers", "民兵团", "义勇军", "skirmisher"),
    ("outlaw_skirmishers", "亡徒军", "贼徒", "skirmisher"),
    ("imperial_halberdiers", "禁卫长戟队", "禁军", "heavy_spear"),
    ("armored_iron_cavalry", "具装铁骑军", "禁军", "heavy_cavalry"),
    ("steel_helmet_heavy_infantry", "钢盔军", "义勇军", "heavy_infantry"),
    ("imperial_longbowmen", "禁军长弓兵", "禁军", "heavy_archer"),
    ("sword_guard_corps", "剑卫军团", "义勇军", "infantry"),
    ("imperial_axe_guard", "禁军斧卫", "禁军", "heavy_brute"),
    ("vanguard_cavalry", "先锋骑军", "义勇军", "cavalry"),
    ("solemn_guard_matchlocks", "肃卫火枪队", "义勇军", "musket"),
    ("raiders", "掠杀军", "贼徒", "skirmisher"),
    ("imperial_heavy_guard", "重甲禁卫军", "禁军", "heavy_infantry"),
    ("warhammer_volunteers", "重锤军", "义勇军", "heavy_brute"),
    ("imperial_shenji_artillery", "禁军神机队", "禁军", "artillery"),
    ("zealot_believers", "狂热信众", "信徒", "skirmisher"),
    ("zealot_mob", "狂热暴徒", "信徒", "brute"),
    ("leader_guard", "领袖卫队", "信徒", "heavy_infantry"),
    ("elite_archers", "精锐弓兵队", "义勇军", "archer"),
    ("bandits", "土匪", "贼徒", "skirmisher"),
    ("great_axe_warriors", "巨斧军", "义勇军", "brute"),
    ("believer_elites", "信徒精锐", "信徒", "infantry"),
]


def stable_hash(value):
    h = 2166136261
    for ch in str(value):
        h ^= ord(ch)
        h = (h * 16777619) & 0xFFFFFFFF
    return h


def rgba(color, alpha=255):
    return (color[0], color[1], color[2], alpha)


def blend(a, b, t):
    return tuple(int(a[i] * (1.0 - t) + b[i] * t) for i in range(3))


def lighten(color, amount):
    return tuple(max(0, min(255, channel + amount)) for channel in color)


def darken(color, amount):
    return tuple(max(0, min(255, channel - amount)) for channel in color)


def scaled(value):
    return int(round(value * SCALE))


def scale_box(box):
    return tuple(scaled(v) for v in box)


def scale_points(points):
    return [(scaled(x), scaled(y)) for x, y in points]


def font(size):
    candidates = [
        Path("C:/Windows/Fonts/msyh.ttc"),
        Path("C:/Windows/Fonts/simhei.ttf"),
        Path("C:/Windows/Fonts/simsun.ttc"),
    ]
    for candidate in candidates:
        if candidate.exists():
            return ImageFont.truetype(str(candidate), size)
    return ImageFont.load_default()


def line(draw, points, fill, width=1, outline=(22, 16, 12, 170)):
    if points and isinstance(points[0], (int, float)):
        scaled_points = tuple(scaled(v) for v in points)
    else:
        scaled_points = scale_points(points)
    if outline and width >= 2:
        draw.line(scaled_points, fill=outline, width=scaled(width + 2), joint="curve")
    draw.line(scaled_points, fill=fill, width=scaled(width), joint="curve")


def ellipse(draw, box, fill, outline=(22, 16, 12, 165), width=1):
    draw.ellipse(scale_box(box), fill=fill, outline=outline, width=max(1, scaled(width)))


def rectangle(draw, box, fill, outline=(22, 16, 12, 150), width=1):
    draw.rectangle(scale_box(box), fill=fill, outline=outline, width=max(1, scaled(width)))


def rounded_rectangle(draw, box, radius, fill, outline=(22, 16, 12, 150), width=1):
    draw.rounded_rectangle(scale_box(box), radius=scaled(radius), fill=fill, outline=outline, width=max(1, scaled(width)))


def polygon(draw, points, fill, outline=(22, 16, 12, 160), width=1):
    pts = scale_points(points)
    draw.polygon(pts, fill=fill)
    if outline:
        draw.line(pts + [pts[0]], fill=outline, width=max(1, scaled(width)), joint="curve")


def arc(draw, box, start, end, fill, width=1):
    draw.arc(scale_box(box), start, end, fill=fill, width=max(1, scaled(width)))


def endpoint(origin, length, angle_degrees):
    angle = math.radians(angle_degrees)
    return (origin[0] + math.cos(angle) * length, origin[1] + math.sin(angle) * length)


def add_finish(img):
    alpha = img.getchannel("A")
    outline_alpha = alpha.filter(ImageFilter.MaxFilter(9)).filter(ImageFilter.GaussianBlur(0.45 * SCALE))
    outline = Image.new("RGBA", img.size, (15, 10, 8, 0))
    outline.putalpha(outline_alpha.point(lambda a: min(160, a)))
    result = Image.alpha_composite(outline, img)
    result.putalpha(Image.composite(alpha, outline_alpha, alpha))
    result = result.resize((SIZE, SIZE), Image.Resampling.LANCZOS)
    return result.filter(ImageFilter.UnsharpMask(radius=0.7, percent=95, threshold=3))


def add_painted_grain(img, seed):
    rng = random.Random(seed)
    pixels = img.load()
    for _ in range(5200):
        x = rng.randrange(img.width)
        y = rng.randrange(img.height)
        r, g, b, a = pixels[x, y]
        if a < 24:
            continue
        delta = rng.randint(-7, 7)
        pixels[x, y] = (
            max(0, min(255, r + delta)),
            max(0, min(255, g + delta)),
            max(0, min(255, b + delta)),
            a,
        )
    return img


def source_sheet_path(unit_id):
    return SOURCE_SHEET_ROOT / f"{unit_id}.png"


def remove_chroma_key(img):
    img = img.convert("RGBA")
    pixels = []
    for r, g, b, a in img.getdata():
        green_delta = g - max(r, b)
        if g > 165 and green_delta > 72:
            pixels.append((r, g, b, 0))
        elif g > 120 and green_delta > 38:
            edge = min(1.0, (green_delta - 38) / 42.0)
            alpha = int(a * (1.0 - edge))
            pixels.append((r, min(g, max(r, b) + 18), b, alpha))
        else:
            pixels.append((r, g, b, a))
    img.putdata(pixels)
    alpha = img.getchannel("A").filter(ImageFilter.GaussianBlur(0.25))
    img.putalpha(alpha)
    return img


def remove_stray_source_components(img, anim):
    alpha = img.getchannel("A")
    mask = alpha.point(lambda value: 255 if value > 24 else 0)
    width, height = mask.size
    pixels = mask.load()
    visited = bytearray(width * height)
    components = []

    for y in range(height):
        for x in range(width):
            index = y * width + x
            if visited[index] or pixels[x, y] == 0:
                continue
            stack = [(x, y)]
            visited[index] = 1
            points = []
            min_x = max_x = x
            min_y = max_y = y
            while stack:
                px, py = stack.pop()
                points.append((px, py))
                min_x = min(min_x, px)
                max_x = max(max_x, px)
                min_y = min(min_y, py)
                max_y = max(max_y, py)
                for nx, ny in ((px + 1, py), (px - 1, py), (px, py + 1), (px, py - 1)):
                    if nx < 0 or ny < 0 or nx >= width or ny >= height:
                        continue
                    nindex = ny * width + nx
                    if visited[nindex] or pixels[nx, ny] == 0:
                        continue
                    visited[nindex] = 1
                    stack.append((nx, ny))
            components.append({"area": len(points), "points": points, "box": (min_x, min_y, max_x, max_y)})

    if not components:
        return img

    largest = max(item["area"] for item in components)
    keep = []
    for item in components:
        min_x, min_y, max_x, max_y = item["box"]
        is_edge_sliver = (min_y == 0 and max_y <= 14) or (max_y >= height - 1 and min_y >= height - 10)
        if item["area"] == largest:
            keep.append(item)
        elif not is_edge_sliver and item["area"] >= largest * 0.06:
            keep.append(item)
        elif anim == "attack" and not is_edge_sliver and item["area"] >= max(120, largest * 0.015):
            keep.append(item)

    clean_alpha = Image.new("L", img.size, 0)
    clean_pixels = clean_alpha.load()
    src_alpha = alpha.load()
    for item in keep:
        for x, y in item["points"]:
            clean_pixels[x, y] = src_alpha[x, y]
    result = img.copy()
    result.putalpha(clean_alpha.filter(ImageFilter.GaussianBlur(0.12)))
    return result


def source_sheet_frame(unit_id, anim, frame):
    path = source_sheet_path(unit_id)
    if not path.exists() or anim not in SHEET_ROWS:
        return None
    sheet = Image.open(path).convert("RGBA")
    cell_w = sheet.width // SHEET_COLUMNS
    cell_h = sheet.height // len(SHEET_ROWS)
    row = SHEET_ROWS.index(anim)
    col = frame % SHEET_COLUMNS
    crop = sheet.crop((col * cell_w, row * cell_h, (col + 1) * cell_w, (row + 1) * cell_h))
    crop = remove_stray_source_components(remove_chroma_key(crop), anim)
    if crop.size != (SIZE, SIZE):
        crop = crop.resize((SIZE, SIZE), Image.Resampling.LANCZOS)
    return crop


def design_path(unit_id):
    return DESIGN_ROOT / f"{unit_id}.png"


def load_design_source(unit_id):
    path = design_path(unit_id)
    if not path.exists():
        return None
    img = Image.open(path).convert("RGBA")
    if img.size == (512, 512):
        return ImageOps.mirror(img) if unit_id in LEFT_FACING_DESIGNS else img

    bbox = img.getchannel("A").getbbox()
    if not bbox:
        return Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    crop = img.crop(bbox)
    scale = min(470 / crop.width, 478 / crop.height)
    crop = crop.resize((max(1, round(crop.width * scale)), max(1, round(crop.height * scale))), Image.Resampling.LANCZOS)
    result = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    result.alpha_composite(crop, ((512 - crop.width) // 2, 512 - crop.height - 18))
    if unit_id in LEFT_FACING_DESIGNS:
        result = ImageOps.mirror(result)
    return result


def layer_rect(img, box):
    x0, y0, x1, y1 = [int(round(v)) for v in box]
    x0 = max(0, min(img.width, x0))
    y0 = max(0, min(img.height, y0))
    x1 = max(0, min(img.width, x1))
    y1 = max(0, min(img.height, y1))
    result = Image.new("RGBA", img.size, (0, 0, 0, 0))
    if x1 <= x0 or y1 <= y0:
        return result
    crop = img.crop((x0, y0, x1, y1))
    result.alpha_composite(crop, (x0, y0))
    return result


def transform_layer(layer, dx=0, dy=0, angle=0, scale=1.0, shear=0.0):
    bbox = layer.getchannel("A").getbbox()
    if not bbox:
        return Image.new("RGBA", layer.size, (0, 0, 0, 0))
    crop = layer.crop(bbox)
    if abs(shear) > 0.001:
        crop = crop.transform(
            crop.size,
            Image.Transform.AFFINE,
            (1.0, shear, -shear * crop.height * 0.5, 0.0, 1.0, 0.0),
            resample=Image.Resampling.BICUBIC,
        )
    if abs(scale - 1.0) > 0.001:
        crop = crop.resize((max(1, round(crop.width * scale)), max(1, round(crop.height * scale))), Image.Resampling.LANCZOS)
    if abs(angle) > 0.001:
        crop = crop.rotate(angle, resample=Image.Resampling.BICUBIC, expand=True)
    cx = (bbox[0] + bbox[2]) * 0.5 + dx
    cy = (bbox[1] + bbox[3]) * 0.5 + dy
    result = Image.new("RGBA", layer.size, (0, 0, 0, 0))
    result.alpha_composite(crop, (round(cx - crop.width * 0.5), round(cy - crop.height * 0.5)))
    return result


def composite_layers(*layers):
    result = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    for layer in layers:
        result.alpha_composite(layer)
    return result


def split_design_layers(img, role):
    bbox = img.getchannel("A").getbbox()
    if not bbox:
        empty = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
        return empty, empty, empty, empty

    x0, y0, x1, y1 = bbox
    height = y1 - y0
    base_h = max(42, int(height * (0.13 if role not in ("cavalry", "heavy_cavalry", "artillery") else 0.10)))
    base_top = max(y0 + 1, y1 - base_h)
    hip_y = y0 + int((base_top - y0) * (0.60 if role not in ("cavalry", "heavy_cavalry") else 0.56))
    mid_x = x0 + int((x1 - x0) * (0.50 if role not in ("cavalry", "heavy_cavalry") else 0.53))
    overlap = 16

    base = layer_rect(img, (x0 - 10, base_top, x1 + 10, y1 + 4))
    upper = layer_rect(img, (x0 - 12, y0 - 8, x1 + 12, hip_y + 18))
    back_leg = layer_rect(img, (x0 - 8, hip_y - 6, mid_x + overlap, base_top + 18))
    front_leg = layer_rect(img, (mid_x - overlap, hip_y - 6, x1 + 8, base_top + 18))
    return base, upper, back_leg, front_leg


def downsample_design_frame(img):
    alpha = img.getchannel("A").filter(ImageFilter.GaussianBlur(0.12))
    img = img.copy()
    img.putalpha(alpha)
    return img.resize((SIZE, SIZE), Image.Resampling.LANCZOS)


def draw_move_marks(draw, bbox, frame, role):
    x0, y0, x1, y1 = bbox
    phase = (frame % MOVE_FRAMES) / float(MOVE_FRAMES)
    if frame in (1, 4):
        y = y1 - 54
        for i in range(2):
            start = x0 + 28 + i * 35 + (8 if frame == 4 else 0)
            draw.line((start, y + i * 16, start - 34, y + 10 + i * 12), fill=(222, 198, 128, 72), width=3)
    if role in ("cavalry", "heavy_cavalry") and frame in (0, 2, 3, 5):
        draw.arc((x0 + 42, y1 - 116, x1 - 28, y1 - 24), 198 + phase * 22, 264 + phase * 22, fill=(220, 191, 104, 74), width=4)


def make_straight_walk_leg_layer(bbox, frame, role):
    layer = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    if role in ("cavalry", "heavy_cavalry", "artillery"):
        return layer

    draw = ImageDraw.Draw(layer, "RGBA")
    x0, y0, x1, y1 = bbox
    width = x1 - x0
    height = y1 - y0
    cx = x0 + width * 0.50
    hip_y = y0 + height * 0.60
    knee_y = y0 + height * 0.76
    foot_y = y1 - height * 0.13
    phase = frame % MOVE_FRAMES

    # Contact -> right foot forward -> passing -> left foot forward -> passing -> recovery.
    front_step = [10, 30, 12, -12, -26, -8][phase]
    back_step = [-16, -26, -6, 12, 30, 10][phase]
    front_lift = [0, -10, -5, 0, -3, -8][phase]
    back_lift = [-3, 0, -8, -5, -11, -3][phase]
    front_knee = [2, 8, -4, -2, -7, 4][phase]
    back_knee = [-4, -7, 5, 2, 8, -2][phase]

    leg_color = (34, 40, 43, 205)
    front_color = (50, 59, 62, 218)
    boot_color = (33, 24, 17, 226)
    outline = (12, 10, 8, 160)

    def draw_leg(hip_x, step, lift, knee_shift, color, flip):
        knee_x = hip_x + step * 0.40 + knee_shift
        foot_x = hip_x + step
        fy = foot_y + lift
        ky = knee_y + lift * 0.34
        draw.line((hip_x, hip_y, knee_x, ky), fill=outline, width=16)
        draw.line((knee_x, ky, foot_x, fy), fill=outline, width=15)
        draw.line((hip_x, hip_y, knee_x, ky), fill=color, width=10)
        draw.line((knee_x, ky, foot_x, fy), fill=color, width=9)
        boot_w = 30
        boot_h = 13
        toe = 10 * flip
        draw.rounded_rectangle((foot_x - boot_w * 0.5, fy - boot_h * 0.3, foot_x + boot_w * 0.5 + toe, fy + boot_h), radius=5, fill=boot_color, outline=outline, width=2)

    draw_leg(cx - 19, back_step, back_lift, back_knee, leg_color, -1)
    draw_leg(cx + 18, front_step, front_lift, front_knee, front_color, 1)
    return layer


def draw_attack_effect(draw, bbox, role, frame):
    x0, y0, x1, y1 = bbox
    width = x1 - x0
    height = y1 - y0
    if frame < 2:
        return
    if role in ("musket", "skirmisher"):
        mx = x0 + int(width * 0.78)
        my = y0 + int(height * 0.34)
        flame = 26 + frame * 3
        draw.polygon(
            [(mx, my), (mx + flame, my - 13), (mx + flame + 28, my), (mx + flame, my + 13)],
            fill=(255, 191, 53, 210),
        )
        draw.polygon([(mx + 8, my), (mx + flame + 16, my - 7), (mx + flame + 20, my + 5)], fill=(255, 245, 160, 230))
        if frame >= 3:
            sx = mx + flame + 26
            for i in range(3):
                draw.ellipse((sx + i * 18, my - 18 - i * 7, sx + 22 + i * 18, my + 4 - i * 4), fill=(215, 214, 190, 78))
    elif role in ("archer", "heavy_archer"):
        y = y0 + int(height * 0.36)
        draw.line((x0 + int(width * 0.52), y, x1 + 46, y - 26), fill=(245, 220, 142, 190), width=5)
        draw.polygon([(x1 + 44, y - 26), (x1 + 24, y - 32), (x1 + 29, y - 17)], fill=(245, 236, 184, 218))
    elif role == "artillery":
        mx = x0 + int(width * 0.74)
        my = y0 + int(height * 0.45)
        draw.ellipse((mx, my - 36, mx + 96, my + 36), fill=(255, 205, 65, 138))
        draw.polygon([(mx + 24, my - 31), (mx + 128, my), (mx + 24, my + 31)], fill=(255, 178, 44, 170))
        if frame >= 3:
            draw.ellipse((mx + 80, my - 45, mx + 154, my + 18), fill=(204, 202, 184, 92))
    else:
        arc_box = (x0 + int(width * 0.23), y0 + int(height * 0.08), x1 + int(width * 0.16), y0 + int(height * 0.72))
        start = 300 if role in ("heavy_spear", "cavalry", "heavy_cavalry") else 312
        end = 34 if role in ("heavy_spear", "cavalry", "heavy_cavalry") else 52
        draw.arc(arc_box, start, 360, fill=(249, 226, 146, 205), width=8)
        draw.arc(arc_box, 0, end, fill=(249, 226, 146, 205), width=8)


def draw_hit_effect(draw, bbox, frame):
    if frame not in (1, 2, 3):
        return
    x0, y0, x1, y1 = bbox
    cx = x0 + int((x1 - x0) * 0.36)
    cy = y0 + int((y1 - y0) * 0.42)
    color = (226, 57, 50, 178)
    for angle in (-32, 0, 28):
        length = 30 + frame * 7
        dx = math.cos(math.radians(angle)) * length
        dy = math.sin(math.radians(angle)) * length
        draw.line((cx, cy, cx - dx, cy + dy), fill=color, width=5)
    draw.ellipse((cx - 9, cy - 9, cx + 9, cy + 9), fill=(255, 212, 120, 172))


def fade_layer(layer, factor):
    result = layer.copy()
    alpha = result.getchannel("A").point(lambda value: int(value * factor))
    result.putalpha(alpha)
    return result


def draw_recover_effect(draw, bbox, frame):
    if frame > 2:
        return
    x0, y0, x1, y1 = bbox
    y = y1 - (y1 - y0) * 0.12
    strength = 1.0 - frame / max(1, RECOVER_FRAMES - 1)
    for i in range(2):
        draw.arc(
            (x0 + 38 + i * 25, y - 16 - i * 8, x0 + 116 + i * 30, y + 18),
            190,
            338,
            fill=(220, 198, 132, int(82 * strength)),
            width=3,
        )


def draw_defeat_effect(draw, bbox, frame):
    x0, y0, x1, y1 = bbox
    p = frame / max(1, DEFEAT_FRAMES - 1)
    dust_alpha = int(116 * min(1.0, p * 1.7))
    y = y1 - (y1 - y0) * 0.10
    for i in range(5):
        spread = 18 + i * 26 + p * 18
        draw.ellipse(
            (x0 + spread, y - 10 - i * 2, x0 + spread + 34, y + 13 + i),
            fill=(182, 160, 107, max(0, dust_alpha - i * 14)),
        )
    if frame in (1, 2, 3):
        cx = x0 + (x1 - x0) * 0.46
        cy = y0 + (y1 - y0) * 0.42
        draw.line((cx, cy, cx - 42, cy - 10), fill=(221, 58, 48, 150), width=5)
        draw.line((cx, cy, cx - 30, cy + 18), fill=(221, 58, 48, 116), width=4)


def design_animation_frame(unit_id, role, anim, frame):
    source = load_design_source(unit_id)
    if source is None:
        return None

    bbox = source.getchannel("A").getbbox()
    if not bbox:
        return downsample_design_frame(source)

    base, upper, back_leg, front_leg = split_design_layers(source, role)

    if anim == "idle":
        bob = [0, -3, -1, 2][frame % IDLE_FRAMES]
        sway = [-0.5, 0.4, 0.2, -0.3][frame % IDLE_FRAMES]
        body = transform_layer(composite_layers(upper, back_leg, front_leg), dy=bob, angle=sway)
        result = composite_layers(base, body)
    elif anim == "move":
        phase = frame % MOVE_FRAMES
        if role in ("cavalry", "heavy_cavalry"):
            cycle = [-1.0, -0.35, 0.45, 1.0, 0.35, -0.45][phase]
            body_bob = [0, -8, -3, 0, -8, -3][phase]
            upper_dx = [5, 7, 6, 5, 7, 6][phase]
            upper_angle = [0.5, 0.2, -0.1, 0.5, 0.2, -0.1][phase]
            stride = 31
            back = transform_layer(back_leg, dx=-cycle * stride, dy=-3, angle=-cycle * 7, shear=cycle * -0.025)
            front = transform_layer(front_leg, dx=cycle * stride, dy=-7 if phase in (1, 4) else 0, angle=cycle * 8, shear=cycle * 0.025)
        else:
            front_dx = [18, 10, -2, -14, -8, 8][phase]
            back_dx = [-12, -4, 10, 18, 10, -2][phase]
            front_lift = [0, -9, -4, 0, -2, -7][phase]
            back_lift = [-2, -7, 0, 0, -9, -4][phase]
            body_bob = [0, -5, -2, 0, -5, -2][phase]
            upper_dx = [4, 5, 6, 4, 5, 6][phase]
            upper_angle = [0.8, 0.4, 0.0, 0.8, 0.4, 0.0][phase]
            back = transform_layer(back_leg, dx=back_dx, dy=back_lift, angle=back_dx * 0.34, shear=back_dx * 0.0015)
            front = transform_layer(front_leg, dx=front_dx, dy=front_lift, angle=front_dx * 0.34, shear=front_dx * 0.0015)
        upper_moved = transform_layer(upper, dx=upper_dx, dy=body_bob, angle=upper_angle)
        result = composite_layers(base, back, front, upper_moved)
        draw = ImageDraw.Draw(result, "RGBA")
        draw_move_marks(draw, bbox, frame, role)
    elif anim == "attack":
        phase = frame % ATTACK_FRAMES
        attack_dx = [0, -8, 12, 26, 10, 0][phase]
        attack_dy = [0, -2, -4, 0, 2, 0][phase]
        attack_angle = [0, -4, 5, 9, 2, 0][phase]
        leg_brace = [0, -5, 8, 13, 4, 0][phase]
        back = transform_layer(back_leg, dx=-leg_brace * 0.5, dy=0, angle=-leg_brace * 0.4)
        front = transform_layer(front_leg, dx=leg_brace, dy=0, angle=leg_brace * 0.45)
        upper_attack = transform_layer(upper, dx=attack_dx, dy=attack_dy, angle=attack_angle)
        result = composite_layers(base, back, front, upper_attack)
        draw_attack_effect(ImageDraw.Draw(result, "RGBA"), bbox, role, frame)
    elif anim == "hit":
        phase = frame % HIT_FRAMES
        recoil_dx = [0, -12, -24, -17, -7, 0][phase]
        recoil_dy = [0, -1, 5, 3, 1, 0][phase]
        recoil_angle = [0, -5, -11, -7, -2, 0][phase]
        body = transform_layer(composite_layers(upper, back_leg, front_leg), dx=recoil_dx, dy=recoil_dy, angle=recoil_angle)
        result = composite_layers(base, body)
        draw_hit_effect(ImageDraw.Draw(result, "RGBA"), bbox, frame)
    elif anim == "recover":
        phase = frame % RECOVER_FRAMES
        recoil_dx = [-14, -10, -6, -2, 1, 0][phase]
        recoil_dy = [4, 2, 0, -2, -1, 0][phase]
        recoil_angle = [-8, -5, -3, -1, 0.5, 0][phase]
        body = transform_layer(composite_layers(upper, back_leg, front_leg), dx=recoil_dx, dy=recoil_dy, angle=recoil_angle)
        result = composite_layers(base, body)
        draw_recover_effect(ImageDraw.Draw(result, "RGBA"), bbox, frame)
    else:
        phase = frame % DEFEAT_FRAMES
        p = phase / max(1, DEFEAT_FRAMES - 1)
        fall_dx = [0, -12, -24, -34, -42, -46, -48, -48][phase]
        fall_dy = [0, 5, 14, 26, 39, 48, 54, 58][phase]
        fall_angle = [0, -10, -22, -38, -55, -68, -74, -78][phase]
        body = transform_layer(composite_layers(upper, back_leg, front_leg), dx=fall_dx, dy=fall_dy, angle=fall_angle, scale=1.0 - p * 0.16)
        body = fade_layer(body, 1.0 - p * 0.45)
        result = composite_layers(base, body)
        draw_defeat_effect(ImageDraw.Draw(result, "RGBA"), bbox, frame)

    return downsample_design_frame(result)


def load_units():
    if CONFIG.exists():
        data = json.loads(CONFIG.read_text(encoding="utf-8"))
        rows = data.get("commonUnits") or []
        if rows:
            result = []
            for unit in rows:
                keyword = unit.get("keyword", "义勇军")
                result.append(
                    (
                        unit["id"],
                        unit.get("name") or unit["id"],
                        keyword,
                        unit.get("role", "infantry"),
                    )
                )
            return result
    return FALLBACK_UNITS


def family_for(keyword):
    return KEYWORD_FAMILY.get(keyword, "volunteer")


def pose_for(anim, frame):
    if anim == "idle":
        return {
            "bob": 0,
            "hit_x": 0,
            "stride": 0,
            "lean": 0,
            "weapon": -36,
            "arm": 0,
            "flash": 0,
            "recoil": 0,
        }
    if anim == "move":
        steps = [
            (0.0, 0, 0),
            (0.9, -4, -2),
            (0.4, -2, -1),
            (-0.35, 0, 1),
            (-0.95, -4, 3),
            (-0.25, -1, 1),
        ]
        stride, bob, lean = steps[frame % MOVE_FRAMES]
        return {
            "bob": bob,
            "hit_x": 0,
            "stride": stride,
            "lean": lean,
            "weapon": -35 + stride * 8,
            "arm": stride,
            "flash": 0,
            "recoil": 0,
        }
    if anim == "attack":
        attacks = [
            (-0.25, 0, -7, -64, 0),
            (-0.55, -2, -10, -82, 0),
            (0.15, -4, 3, -28, 0),
            (0.58, -6, 11, 12, 1),
            (0.42, -4, 8, 30, 1),
            (0.0, 0, 0, -36, 0),
        ]
        arm, bob, lean, weapon, flash = attacks[frame % ATTACK_FRAMES]
        return {
            "bob": bob,
            "hit_x": 0,
            "stride": 0.26 if flash else -0.12,
            "lean": lean,
            "weapon": weapon,
            "arm": arm,
            "flash": flash,
            "recoil": 0,
        }
    hits = [
        (0, 0, 0, 0),
        (-6, -3, -8, 1),
        (-10, -5, -13, 1),
        (5, 0, 7, 1),
        (2, 0, 3, 0),
        (0, 0, 0, 0),
    ]
    hit_x, bob, lean, recoil = hits[frame % HIT_FRAMES]
    return {
        "bob": bob,
        "hit_x": hit_x,
        "stride": 0,
        "lean": lean,
        "weapon": -36,
        "arm": 0,
        "flash": 0,
        "recoil": recoil,
    }


def draw_ground_base(draw, palette, seed, cx=128, cy=220):
    rng = random.Random(seed)
    ellipse(draw, (48, cy - 9, 208, cy + 22), (0, 0, 0, 52), None, 1)
    polygon(
        draw,
        [(61, cy - 13), (89, cy - 31), (167, cy - 31), (198, cy - 13), (174, cy + 13), (86, cy + 13)],
        rgba((42, 36, 30), 245),
        rgba((18, 14, 12), 230),
        2,
    )
    polygon(
        draw,
        [(71, cy - 16), (96, cy - 27), (160, cy - 27), (187, cy - 16), (166, cy + 1), (92, cy + 1)],
        rgba(blend(palette["base"], (182, 154, 88), 0.32), 246),
        rgba(palette["trim"], 205),
        2,
    )
    for _ in range(16):
        x = rng.randint(86, 170)
        y = rng.randint(cy - 24, cy)
        if rng.random() < 0.58:
            line(draw, (x, y, x + rng.randint(-7, 7), y - rng.randint(4, 12)), rgba((77, 108, 54), 140), 1, None)
        else:
            rr = rng.randint(2, 4)
            ellipse(draw, (x - rr, y - rr, x + rr, y + rr), rgba((83, 76, 61), 134), rgba((35, 29, 23), 90), 1)


def draw_support_soldier(draw, cx, feet_y, palette, role, pose, side=1):
    back_tint = blend(palette["coat"], (31, 28, 24), 0.28)
    pants = darken(palette["pants"], 6)
    y = feet_y + pose["bob"] * 0.35
    stride = pose["stride"] * 0.45
    hip_y = y - 48
    shoulder_y = y - 91
    head_y = y - 111
    draw_limb(draw, (cx - 6, hip_y), (cx - 13 - stride * 7, y - 6), pants, 7, False)
    draw_limb(draw, (cx + 7, hip_y), (cx + 14 + stride * 7, y - 5), pants, 7, False)
    draw_boot(draw, cx - 14 - stride * 7, y - 3, -1)
    draw_boot(draw, cx + 14 + stride * 7, y - 3, 1)
    polygon(draw, [(cx - 18, shoulder_y), (cx + 18, shoulder_y), (cx + 11, hip_y + 5), (cx - 11, hip_y + 5)], rgba(back_tint, 218), rgba((24, 18, 14), 170), 1)
    rectangle(draw, (cx - 13, shoulder_y + 25, cx + 13, shoulder_y + 32), rgba(palette["red"], 185), None)
    for row in range(3):
        line(draw, (cx - 13, shoulder_y + 10 + row * 8, cx + 13, shoulder_y + 10 + row * 8), rgba(palette["metal"], 88), 1, None)
    mask = blend(back_tint, (18, 14, 12), 0.4)
    ellipse(draw, (cx - 9, head_y - 13, cx + 9, head_y + 10), rgba((144, 101, 76), 145), rgba((36, 24, 18), 170), 1)
    rounded_rectangle(draw, (cx - 9, head_y - 1, cx + 9, head_y + 12), 3, rgba(mask, 220), rgba((30, 21, 17), 150), 1)
    arc(draw, (cx - 13, head_y - 18, cx + 13, head_y + 3), 188, 352, rgba((18, 14, 12), 235), 5)
    ellipse(draw, (cx - 4, head_y - 23, cx + 4, head_y - 15), rgba((18, 14, 12), 235), None)
    hand_l = (cx - 19, shoulder_y + 35)
    hand_r = (cx + 21, shoulder_y + 34)
    draw_limb(draw, (cx - 15, shoulder_y + 8), hand_l, back_tint, 7, False)
    draw_limb(draw, (cx + 15, shoulder_y + 8), hand_r, lighten(back_tint, 8), 7, False)
    if role in ("musket",):
        draw_musket(draw, (cx + 12, shoulder_y + 31), -8, palette, 0)
    elif role in ("archer", "heavy_archer"):
        draw_bow(draw, (cx + 13, shoulder_y + 31), -8, palette, 0)
    elif role in ("heavy_spear",):
        draw_spear(draw, (cx + 13, shoulder_y + 38), -28, palette)
    elif role in ("brute", "heavy_brute"):
        draw_axe(draw, (cx + 17, shoulder_y + 38), -46, palette, hammer=False)
    else:
        draw_sword(draw, (cx + 19, shoulder_y + 39), -42, palette, False)


def draw_limb(draw, start, end, color, width, highlight=True):
    line(draw, (*start, *end), rgba(color, 245), width, rgba((19, 14, 11), 205))
    ellipse(draw, (start[0] - width * 0.45, start[1] - width * 0.45, start[0] + width * 0.45, start[1] + width * 0.45), rgba(color, 230), None)
    ellipse(draw, (end[0] - width * 0.38, end[1] - width * 0.38, end[0] + width * 0.38, end[1] + width * 0.38), rgba(color, 230), None)
    if highlight:
        hx = start[0] * 0.6 + end[0] * 0.4
        hy = start[1] * 0.6 + end[1] * 0.4
        line(draw, (start[0] - 1, start[1] - 1, hx, hy), rgba(lighten(color, 34), 90), max(1, width * 0.22), None)


def draw_boot(draw, x, y, flip=1):
    polygon(draw, [(x - 6, y - 4), (x + 9 * flip, y - 4), (x + 13 * flip, y + 4), (x - 9 * flip, y + 5)], rgba((27, 21, 17), 250), rgba((13, 10, 8), 190), 1)


def draw_head(draw, cx, cy, palette, heavy=False):
    hair = (18, 14, 12)
    mask = blend(palette["coat"], (22, 17, 14), 0.44)
    ellipse(draw, (cx - 15, cy - 17, cx + 15, cy + 16), rgba((159, 114, 83), 192), rgba((48, 32, 23), 205), 1)
    rounded_rectangle(draw, (cx - 14, cy - 5, cx + 14, cy + 18), 5, rgba(mask, 245), rgba((33, 23, 18), 198), 1)
    if heavy:
        arc(draw, (cx - 20, cy - 24, cx + 20, cy + 3), 184, 356, rgba(palette["metal"], 250), 7)
        rectangle(draw, (cx - 18, cy - 10, cx + 18, cy - 2), rgba(blend(palette["metal"], (48, 39, 29), 0.18), 235), rgba((42, 31, 23), 160), 1)
        line(draw, (cx - 13, cy - 7, cx + 13, cy - 7), rgba(lighten(palette["metal"], 36), 148), 2, None)
    else:
        arc(draw, (cx - 20, cy - 26, cx + 20, cy + 8), 188, 352, rgba(hair, 250), 9)
        line(draw, (cx - 16, cy - 7, cx + 16, cy - 7), rgba(palette["trim"], 205), 2, None)
    ellipse(draw, (cx - 7, cy - 31, cx + 7, cy - 18), rgba(hair, 250), rgba((8, 6, 5), 150), 1)
    line(draw, (cx - 10, cy + 6, cx + 10, cy + 6), rgba(lighten(mask, 28), 80), 1, None)


def draw_torso(draw, cx, shoulder_y, palette, role, lean=0, heavy=False):
    shoulder = 30 if heavy else 26
    waist = 18 if heavy else 16
    coat = palette["coat2"] if heavy else palette["coat"]
    top_l = (cx - shoulder + lean * 0.14, shoulder_y)
    top_r = (cx + shoulder + lean * 0.14, shoulder_y)
    bot_r = (cx + waist + lean * 0.4, shoulder_y + 69)
    bot_l = (cx - waist + lean * 0.4, shoulder_y + 69)
    polygon(draw, [top_l, top_r, bot_r, bot_l], rgba(coat, 248), rgba((28, 21, 17), 225), 2)
    polygon(draw, [(cx - waist, shoulder_y + 68), (cx, shoulder_y + 96), (cx + waist, shoulder_y + 68), (cx + 26, shoulder_y + 90), (cx - 26, shoulder_y + 90)], rgba(blend(coat, (20, 16, 14), 0.22), 234), rgba((28, 21, 17), 150), 1)
    line(draw, (cx - shoulder + 6, shoulder_y + 12, cx + waist - 2, shoulder_y + 61), rgba(palette["trim"], 178), 4, None)
    line(draw, (cx + shoulder - 6, shoulder_y + 12, cx - waist + 2, shoulder_y + 61), rgba(lighten(palette["trim"], 10), 120), 2, None)
    rectangle(draw, (cx - 22, shoulder_y + 46, cx + 22, shoulder_y + 56), rgba(palette["red"], 238), rgba((38, 25, 19), 180), 1)
    rows = 5 if heavy else 4
    for row in range(rows):
        y = shoulder_y + 20 + row * 9
        line(draw, (cx - 21, y, cx + 21, y), rgba(palette["metal"], 120), 2, None)
        for col in range(5):
            x0 = cx - 22 + col * 9
            rectangle(draw, (x0, y - 4, x0 + 7, y + 4), rgba(blend(coat, palette["metal"], 0.22), 160), rgba((34, 25, 20), 70), 1)
            ellipse(draw, (x0 + 2, y - 1, x0 + 4, y + 1), rgba(lighten(palette["trim"], 18), 120), None)
    for sx in (cx - shoulder + 3, cx + shoulder - 3):
        polygon(draw, [(sx - 12, shoulder_y + 2), (sx, shoulder_y - 8), (sx + 12, shoulder_y + 2), (sx + 8, shoulder_y + 18), (sx - 8, shoulder_y + 18)], rgba(blend(palette["metal"], coat, 0.22), 224), rgba((35, 26, 20), 160), 1)
        line(draw, (sx - 7, shoulder_y + 5, sx + 8, shoulder_y + 6), rgba(lighten(palette["metal"], 32), 105), 1, None)


def draw_shield(draw, cx, cy, palette, heavy=False):
    shield = blend(palette["coat"], palette["flag"], 0.42)
    if heavy:
        pts = [(cx - 20, cy - 25), (cx + 20, cy - 25), (cx + 17, cy + 20), (cx, cy + 39), (cx - 17, cy + 20)]
    else:
        pts = [(cx - 18, cy - 22), (cx + 18, cy - 22), (cx + 20, cy + 12), (cx, cy + 36), (cx - 20, cy + 12)]
    polygon(draw, pts, rgba(shield, 240), rgba(palette["trim"], 220), 2)
    line(draw, (cx, cy - 16, cx, cy + 28), rgba(palette["trim"], 140), 2, None)
    line(draw, (cx - 12, cy - 2, cx + 12, cy - 2), rgba(lighten(shield, 28), 95), 2, None)
    for x, y in ((cx - 8, cy - 11), (cx + 8, cy - 11), (cx - 10, cy + 8), (cx + 10, cy + 8), (cx, cy + 24)):
        ellipse(draw, (x - 2, y - 2, x + 2, y + 2), rgba(lighten(palette["metal"], 32), 138), None)


def draw_flag(draw, cx, cy, palette, frame):
    flutter = math.sin(frame * 1.7) * 3
    line(draw, (cx, cy + 38, cx, cy - 50), rgba((66, 45, 30), 248), 4, rgba((28, 18, 13), 190))
    polygon(draw, [(cx + 3, cy - 50), (cx + 62, cy - 39 + flutter), (cx + 55, cy - 10 + flutter), (cx + 3, cy - 22)], rgba(palette["flag"], 236), rgba(palette["trim"], 185), 2)
    line(draw, (cx + 16, cy - 36, cx + 48, cy - 31 + flutter), rgba(palette["trim"], 145), 2, None)
    line(draw, (cx + 15, cy - 21, cx + 45, cy - 17 + flutter), rgba(darken(palette["flag"], 33), 125), 2, None)


def draw_sword(draw, hilt, angle, palette, heavy=False):
    end = endpoint(hilt, 68 if not heavy else 76, angle)
    side = endpoint(hilt, 11, angle + 96)
    blade_l = endpoint(hilt, 14, angle + 86)
    blade_r = endpoint(hilt, 14, angle - 86)
    tip_l = endpoint(end, 7, angle + 164)
    polygon(draw, [blade_l, end, tip_l, blade_r], rgba(palette["metal"], 246), rgba((45, 39, 31), 165), 1)
    line(draw, (blade_l[0], blade_l[1], end[0], end[1]), rgba((248, 244, 213), 115), 2, None)
    line(draw, (hilt[0], hilt[1], side[0], side[1]), rgba(palette["trim"], 230), 4, rgba((45, 28, 17), 140))
    pommel = endpoint(hilt, 12, angle + 180)
    line(draw, (hilt[0], hilt[1], pommel[0], pommel[1]), rgba(palette["leather"], 245), 5, rgba((33, 21, 14), 180))


def draw_axe(draw, grip, angle, palette, hammer=False):
    end = endpoint(grip, 78, angle)
    line(draw, (grip[0], grip[1], end[0], end[1]), rgba(palette["leather"], 250), 6, rgba((35, 22, 14), 210))
    if hammer:
        head_a = endpoint(end, 22, angle + 88)
        head_b = endpoint(end, 22, angle - 92)
        line(draw, (head_a[0], head_a[1], head_b[0], head_b[1]), rgba(palette["metal"], 248), 14, rgba((43, 34, 26), 180))
        line(draw, (head_a[0], head_a[1], end[0], end[1]), rgba(lighten(palette["metal"], 34), 105), 3, None)
    else:
        left = endpoint(end, 24, angle + 120)
        right = endpoint(end, 28, angle - 58)
        tip = endpoint(end, 18, angle + 16)
        polygon(draw, [left, tip, right, end], rgba(palette["metal"], 246), rgba((42, 34, 27), 170), 2)
        line(draw, (left[0], left[1], tip[0], tip[1]), rgba((246, 241, 209), 105), 2, None)


def draw_spear(draw, grip, angle, palette):
    start = endpoint(grip, 56, angle + 180)
    end = endpoint(grip, 96, angle)
    line(draw, (start[0], start[1], end[0], end[1]), rgba(palette["leather"], 250), 5, rgba((36, 22, 14), 210))
    tip = endpoint(end, 22, angle)
    side_l = endpoint(end, 10, angle + 108)
    side_r = endpoint(end, 10, angle - 108)
    polygon(draw, [tip, side_l, side_r], rgba(palette["metal"], 248), rgba((43, 34, 26), 170), 1)
    ribbon_l = endpoint(end, 26, angle + 135)
    ribbon_r = endpoint(end, 21, angle + 157)
    polygon(draw, [end, ribbon_l, ribbon_r], rgba(palette["red"], 210), None)


def draw_musket(draw, grip, angle, palette, flash=0):
    butt = endpoint(grip, 49, angle + 180)
    muzzle = endpoint(grip, 91, angle)
    line(draw, (butt[0], butt[1], muzzle[0], muzzle[1]), rgba(palette["leather"], 250), 8, rgba((35, 22, 15), 220))
    line(draw, (grip[0] - 6, grip[1] - 2, muzzle[0] + 2, muzzle[1] - 1), rgba(palette["metal"], 248), 3, None)
    rectangle(draw, (butt[0] - 10, butt[1] - 5, butt[0] + 14, butt[1] + 8), rgba(darken(palette["leather"], 18), 235), rgba((31, 20, 14), 170), 1)
    line(draw, (grip[0] + 11, grip[1] + 3, grip[0] + 23, grip[1] + 22), rgba((91, 48, 29), 165), 2, None)
    if flash:
        ellipse(draw, (muzzle[0] - 1, muzzle[1] - 16, muzzle[0] + 42, muzzle[1] + 17), (255, 219, 112, 134), None)
        polygon(draw, [(muzzle[0] + 3, muzzle[1] - 9), (muzzle[0] + 56, muzzle[1]), (muzzle[0] + 3, muzzle[1] + 11)], (255, 188, 58, 166), None)


def draw_bow(draw, grip, angle, palette, flash=0):
    bow_cx = grip[0] + 34
    bow_cy = grip[1] - 2
    arc(draw, (bow_cx - 20, bow_cy - 44, bow_cx + 24, bow_cy + 45), -82, 82, rgba((145, 88, 43), 250), 5)
    line(draw, (bow_cx + 5, bow_cy - 39, bow_cx + 5, bow_cy + 39), rgba((231, 210, 166), 160), 1, None)
    arrow_end = endpoint((grip[0] - 21, grip[1] + 1), 112, angle)
    line(draw, (grip[0] - 21, grip[1] + 1, arrow_end[0], arrow_end[1]), rgba(palette["metal"], 235), 2, None)
    polygon(draw, [(arrow_end[0], arrow_end[1]), (arrow_end[0] - 12, arrow_end[1] - 5), (arrow_end[0] - 8, arrow_end[1] + 6)], rgba(palette["metal"], 240), None)
    if flash:
        line(draw, (grip[0] + 28, grip[1] - 6, arrow_end[0] + 28, arrow_end[1] - 12), (246, 232, 170, 170), 2, None)


def draw_melee_flash(draw, cx, cy, role, pose):
    if not pose["flash"]:
        return
    if role in ("musket", "archer", "heavy_archer", "artillery"):
        return
    arc(draw, (cx - 14, cy - 92, cx + 105, cy + 18), -20, 62, (255, 226, 110, 130), 8)
    arc(draw, (cx - 8, cy - 83, cx + 88, cy + 8), -15, 54, (255, 244, 180, 90), 3)


def draw_hit_flash(draw, pose):
    if not pose["recoil"]:
        return
    for i in range(6):
        x = 48 + i * 27
        y = 82 + (i % 2) * 24
        line(draw, (x, y, x + 14, y - 16), (238, 207, 114, 150), 3, None)


def draw_soldier(draw, unit_id, role, family, anim, frame, pose, palette):
    heavy = role.startswith("heavy") or role in ("artillery",)
    cx = 128 + pose["hit_x"]
    feet_y = 199 + pose["bob"]
    lean = pose["lean"]
    stride = pose["stride"]
    if unit_id in FLAG_UNITS:
        draw_flag(draw, cx - 62, feet_y - 91, palette, frame)

    if role not in ("heavy_spear", "heavy_brute"):
        draw_support_soldier(draw, cx - 37, 194, palette, role, pose, -1)
        draw_support_soldier(draw, cx + 39, 196, palette, role, pose, 1)
    elif role in ("heavy_spear", "heavy_brute"):
        draw_support_soldier(draw, cx + 38, 196, palette, role, pose, 1)

    hip_l = (cx - 13 + lean * 0.08, feet_y - 75)
    hip_r = (cx + 14 + lean * 0.08, feet_y - 75)
    back_foot = (cx - 20 - stride * 14, feet_y + 1)
    front_foot = (cx + 21 + stride * 14, feet_y)
    draw_limb(draw, hip_l, (cx - 18 - stride * 9, feet_y - 36), palette["pants"], 12)
    draw_limb(draw, (cx - 18 - stride * 9, feet_y - 36), back_foot, darken(palette["pants"], 5), 11)
    draw_boot(draw, back_foot[0], back_foot[1], -1)
    draw_limb(draw, hip_r, (cx + 17 + stride * 9, feet_y - 35), lighten(palette["pants"], 6), 12)
    draw_limb(draw, (cx + 17 + stride * 9, feet_y - 35), front_foot, palette["pants"], 11)
    draw_boot(draw, front_foot[0], front_foot[1], 1)

    shoulder_y = feet_y - 141
    shoulder_l = (cx - (31 if heavy else 28), shoulder_y + 17)
    shoulder_r = (cx + (31 if heavy else 28), shoulder_y + 17)
    left_hand = (cx - 34 + lean * 0.2, shoulder_y + 65)
    right_hand = (cx + 33 + lean * 0.2, shoulder_y + 61)

    if role in ("infantry", "heavy_infantry", "heavy_spear"):
        draw_limb(draw, shoulder_l, left_hand, palette["coat"], 11)
        draw_shield(draw, left_hand[0] - 10, left_hand[1] + 9, palette, heavy)

    draw_torso(draw, cx, shoulder_y, palette, role, lean, heavy)

    if role in ("musket",):
        left_hand = (cx - 31, shoulder_y + 45 + pose["arm"] * 2)
        right_hand = (cx + 30, shoulder_y + 47 - pose["arm"] * 2)
        draw_limb(draw, shoulder_l, left_hand, palette["coat"], 10)
        draw_musket(draw, (cx + 16, shoulder_y + 45), -8 + lean * 0.15, palette, pose["flash"])
        draw_limb(draw, shoulder_r, right_hand, palette["coat2"], 10)
    elif role in ("archer", "heavy_archer"):
        left_hand = (cx - 34, shoulder_y + 48)
        right_hand = (cx + 22 + pose["arm"] * 10, shoulder_y + 44)
        draw_limb(draw, shoulder_l, left_hand, palette["coat"], 10)
        draw_bow(draw, (cx + 21, shoulder_y + 46), -9, palette, pose["flash"])
        draw_limb(draw, shoulder_r, right_hand, palette["coat2"], 10)
    elif role in ("heavy_spear",):
        grip = (cx + 25, shoulder_y + 55)
        draw_spear(draw, grip, -26 + pose["weapon"] * 0.22, palette)
        draw_limb(draw, shoulder_r, grip, palette["coat2"], 11)
    elif role in ("brute", "heavy_brute"):
        grip = (cx + 29, shoulder_y + 62)
        draw_axe(draw, grip, pose["weapon"], palette, hammer=(role == "heavy_brute" and "hammer" in unit_id or "warhammer" in unit_id))
        draw_limb(draw, shoulder_l, (cx - 27, shoulder_y + 64), palette["coat"], 11)
        draw_limb(draw, shoulder_r, grip, palette["coat2"], 12)
    else:
        grip = (cx + 33, shoulder_y + 62)
        draw_sword(draw, grip, pose["weapon"], palette, heavy)
        if role not in ("infantry", "heavy_infantry", "heavy_spear"):
            draw_limb(draw, shoulder_l, (cx - 29, shoulder_y + 63), palette["coat"], 11)
        draw_limb(draw, shoulder_r, grip, palette["coat2"], 11)

    draw_head(draw, cx + lean * 0.25, shoulder_y - 14, palette, heavy)
    draw_melee_flash(draw, cx, shoulder_y + 72, role, pose)
    draw_hit_flash(draw, pose)


def draw_horse(draw, cx, cy, palette, heavy, frame, pose):
    stride = pose["stride"] if abs(pose["stride"]) > 0 else math.sin(frame * 1.2) * 0.18
    horse = (78, 55, 40) if not heavy else (48, 45, 42)
    horse_shadow = darken(horse, 16)
    for idx, lx in enumerate((-42, -18, 22, 46)):
        swing = [16, -21, -8, 18, -24, 5][(idx + frame) % 6] if abs(stride) > 0 else [9, -8, -6, 10][idx]
        knee = (cx + lx + swing * 0.4, cy + 27)
        hoof = (cx + lx + swing, cy + 64)
        draw_limb(draw, (cx + lx, cy + 3), knee, horse_shadow if idx < 2 else horse, 9, False)
        draw_limb(draw, knee, hoof, horse_shadow if idx < 2 else horse, 8, False)
        draw_boot(draw, hoof[0], hoof[1], 1 if idx % 2 else -1)
    ellipse(draw, (cx - 61, cy - 15, cx + 45, cy + 35), rgba(horse, 250), rgba((29, 20, 16), 220), 2)
    polygon(draw, [(cx - 52, cy - 10), (cx - 79, cy - 21), (cx - 58, cy + 9)], rgba(darken(horse, 7), 240), rgba((29, 20, 16), 185), 1)
    line(draw, (cx - 37, cy + 18, cx + 39, cy + 17), rgba((33, 22, 16), 155), 2, None)
    rectangle(draw, (cx - 27, cy - 22, cx + 23, cy + 7), rgba(palette["coat"], 232), rgba(palette["trim"], 190), 2)
    rectangle(draw, (cx - 23, cy - 25, cx + 20, cy - 16), rgba(palette["red"], 224), None)
    for x in (cx - 16, cx - 2, cx + 12):
        ellipse(draw, (x - 2, cy - 6, x + 2, cy - 2), rgba(lighten(palette["trim"], 18), 130), None)
    if heavy:
        arc(draw, (cx - 58, cy - 15, cx + 45, cy + 42), 180, 360, rgba(palette["metal"], 210), 7)
        line(draw, (cx - 29, cy - 6, cx + 34, cy - 5), rgba(palette["trim"], 148), 2, None)
    ellipse(draw, (cx + 34, cy - 23, cx + 80, cy + 12), rgba(horse, 250), rgba((29, 20, 16), 220), 2)
    line(draw, (cx + 66, cy - 15, cx + 88, cy - 35), rgba(darken(horse, 8), 250), 7, rgba((29, 20, 16), 180))
    line(draw, (cx + 47, cy - 15, cx + 75, cy), rgba(palette["trim"], 160), 2, None)
    line(draw, (cx + 51, cy + 4, cx + 74, cy - 5), rgba((36, 23, 16), 170), 2, None)


def draw_cavalry(draw, unit_id, role, family, anim, frame, pose, palette):
    heavy = role == "heavy_cavalry"
    cx = 125 + pose["hit_x"]
    cy = 142 + pose["bob"]
    if unit_id in FLAG_UNITS:
        draw_flag(draw, cx - 62, cy + 30, palette, frame)
    draw_horse(draw, cx, cy + 21, palette, heavy, frame, pose)
    rider_x = cx - 5
    shoulder_y = cy - 25
    draw_limb(draw, (rider_x - 15, shoulder_y + 56), (rider_x - 23, shoulder_y + 87), palette["pants"], 8, False)
    draw_limb(draw, (rider_x + 13, shoulder_y + 56), (rider_x + 30, shoulder_y + 82), palette["pants"], 8, False)
    draw_torso(draw, rider_x, shoulder_y, palette, role, pose["lean"], True)
    grip = (rider_x + 30, shoulder_y + 54)
    draw_spear(draw, grip, -28 + pose["weapon"] * 0.25, palette)
    draw_limb(draw, (rider_x + 26, shoulder_y + 20), grip, palette["coat2"], 8)
    draw_limb(draw, (rider_x - 27, shoulder_y + 20), (rider_x - 40, shoulder_y + 53), palette["coat"], 8)
    draw_head(draw, rider_x + pose["lean"] * 0.25, shoulder_y - 13, palette, heavy)
    draw_melee_flash(draw, rider_x, shoulder_y + 75, role, pose)
    draw_hit_flash(draw, pose)


def draw_cannon(draw, cx, cy, palette, pose):
    rectangle(draw, (cx - 62, cy + 23, cx + 32, cy + 45), rgba((45, 38, 32), 250), rgba(palette["trim"], 178), 2)
    for wx in (cx - 54, cx + 19):
        ellipse(draw, (wx - 18, cy + 34, wx + 18, cy + 70), rgba(palette["metal"], 240), rgba((30, 24, 20), 218), 2)
        ellipse(draw, (wx - 7, cy + 45, wx + 7, cy + 59), rgba((38, 31, 25), 220), None)
    barrel_y = cy + 18 + pose["bob"]
    line(draw, (cx - 18, barrel_y + 19, cx + 82, barrel_y - 15), rgba(palette["metal"], 255), 17, rgba((31, 25, 20), 210))
    line(draw, (cx - 15, barrel_y + 11, cx + 78, barrel_y - 20), rgba(lighten(palette["metal"], 42), 120), 4, None)
    for x, y in ((cx - 44, cy + 32), (cx - 18, cy + 28), (cx + 15, cy + 19), (cx + 50, cy + 6)):
        ellipse(draw, (x - 3, y - 3, x + 3, y + 3), rgba(lighten(palette["trim"], 25), 135), None)
    if pose["flash"]:
        ellipse(draw, (cx + 68, barrel_y - 34, cx + 140, barrel_y + 32), (255, 213, 93, 135), None)
        polygon(draw, [(cx + 87, barrel_y - 24), (cx + 159, barrel_y - 4), (cx + 88, barrel_y + 24)], (255, 190, 57, 166), None)


def draw_crew(draw, cx, feet_y, palette, scale=0.7, flip=1):
    shoulder_y = feet_y - 88 * scale
    draw_limb(draw, (cx - 8 * scale, feet_y - 48 * scale), (cx - 16 * scale, feet_y - 7 * scale), palette["pants"], 8 * scale)
    draw_limb(draw, (cx + 8 * scale, feet_y - 48 * scale), (cx + 17 * scale, feet_y - 7 * scale), palette["pants"], 8 * scale)
    draw_torso(draw, cx, shoulder_y, palette, "artillery", 0, False)
    draw_limb(draw, (cx + 17 * scale, shoulder_y + 15 * scale), (cx + 34 * scale * flip, shoulder_y + 47 * scale), palette["coat2"], 7 * scale)
    draw_head(draw, cx, shoulder_y - 12 * scale, palette, True)


def draw_artillery(draw, unit_id, role, family, anim, frame, pose, palette):
    draw_crew(draw, 78 + pose["hit_x"] * 0.4, 191 + pose["bob"] * 0.4, palette, 0.62, -1)
    draw_crew(draw, 177 + pose["hit_x"] * 0.2, 191 + pose["bob"] * 0.4, palette, 0.62, 1)
    draw_cannon(draw, 128 + pose["hit_x"] * 0.35, 134, palette, pose)
    draw_hit_flash(draw, pose)


def render_unit(unit_id, role, family, anim, frame):
    palette = PALETTES[family]
    seed = stable_hash(f"{unit_id}:{anim}:{frame}")
    img = Image.new("RGBA", (SIZE * SCALE, SIZE * SCALE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img, "RGBA")
    pose = pose_for(anim, 0 if anim == "idle" else frame)
    draw_ground_base(draw, palette, stable_hash(unit_id))
    if role in ("cavalry", "heavy_cavalry"):
        draw_cavalry(draw, unit_id, role, family, anim, frame, pose, palette)
    elif role == "artillery":
        draw_artillery(draw, unit_id, role, family, anim, frame, pose, palette)
    else:
        draw_soldier(draw, unit_id, role, family, anim, frame, pose, palette)
    return add_finish(add_painted_grain(img, seed))


def save_frames():
    OUT_ROOT.mkdir(parents=True, exist_ok=True)
    units = load_units()
    manifest_units = []
    preview_rows = []
    for unit_id, name, keyword, role in units:
        family = family_for(keyword)
        unit_dir = OUT_ROOT / unit_id
        unit_dir.mkdir(parents=True, exist_ok=True)
        for stale in unit_dir.glob("*.png"):
            stale.unlink()
        for anim, count in FRAME_COUNTS.items():
            for frame in range(count):
                image = design_animation_frame(unit_id, role, anim, frame)
                if image is None:
                    image = source_sheet_frame(unit_id, anim, frame)
                if image is None:
                    image = render_unit(unit_id, role, family, anim, frame)
                image.save(unit_dir / f"{anim}_{frame}.png")
        manifest_units.append(
            {
                "id": unit_id,
                "name": name,
                "role": role,
                "roleDisplay": ROLE_DISPLAY.get(role, role),
                "family": family,
                "keyword": keyword,
                "asset": f"Art/BattleUnits/{unit_id}",
                "designAsset": f"Art/BattleUnitDesigns/{unit_id}" if (DESIGN_ROOT / f"{unit_id}.png").exists() else "",
                "idleFrames": IDLE_FRAMES,
                "moveFrames": MOVE_FRAMES,
                "attackFrames": ATTACK_FRAMES,
                "hitFrames": HIT_FRAMES,
                "recoverFrames": RECOVER_FRAMES,
                "defeatFrames": DEFEAT_FRAMES,
            }
        )
        preview_rows.append(make_preview_row(unit_id, name, keyword, role, family))

    MANIFEST.write_text(
        json.dumps(
            {
                "generatedAt": datetime.now().isoformat(timespec="seconds"),
                "style": STYLE_LABEL,
                "animationPipeline": "standing-design-driven-raster-sequence",
                "units": manifest_units,
            },
            ensure_ascii=False,
            indent=2,
        )
        + "\n",
        encoding="utf-8",
    )
    save_preview_grid(preview_rows, PREVIEW, 2)


def make_preview_row(unit_id, name, keyword, role, family):
    row = Image.new("RGBA", (1120, 172), (29, 25, 21, 255))
    draw = ImageDraw.Draw(row, "RGBA")
    text_font = font(15)
    small_font = font(11)
    draw.text((12, 9), f"{name} / {ROLE_DISPLAY.get(role, role)} / {keyword}", font=text_font, fill=(238, 222, 185, 255))
    samples = [("idle", 0), ("move", 1), ("move", 2), ("move", 4), ("attack", 3), ("hit", 2), ("recover", 3), ("defeat", 3), ("defeat", 7)]
    for i, (anim, frame) in enumerate(samples):
        path = OUT_ROOT / unit_id / f"{anim}_{frame}.png"
        if path.exists():
            piece = Image.open(path).convert("RGBA")
        else:
            piece = source_sheet_frame(unit_id, anim, frame) or render_unit(unit_id, role, family, anim, frame)
        piece = piece.resize((112, 112), Image.Resampling.LANCZOS)
        x = 12 + i * 122
        row.alpha_composite(piece, (x, 36))
        draw.text((x + 29, 150), f"{anim}_{frame}", font=small_font, fill=(219, 202, 166, 255))
    return row.convert("RGB")


def save_preview_grid(rows, path, columns):
    if not rows:
        return
    cell_w = rows[0].width
    cell_h = rows[0].height
    canvas = Image.new("RGB", (cell_w * columns, math.ceil(len(rows) / columns) * cell_h), (18, 16, 14))
    for idx, row in enumerate(rows):
        canvas.paste(row, ((idx % columns) * cell_w, (idx // columns) * cell_h))
    path.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(path)


if __name__ == "__main__":
    save_frames()
    print(f"Generated {len(load_units())} full-frame battle unit animation sets at {OUT_ROOT}")
    print(f"Preview: {PREVIEW}")
