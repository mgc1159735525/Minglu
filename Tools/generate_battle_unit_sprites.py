from __future__ import annotations

import json
import math
import random
from dataclasses import dataclass
from datetime import datetime
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont


PROJECT_ROOT = Path(__file__).resolve().parents[1]
ART_ROOT = PROJECT_ROOT / "Assets" / "Resources" / "Art"
OUT_ROOT = ART_ROOT / "BattleUnits"
PART_ROOT = ART_ROOT / "BattleUnitParts"
MANIFEST = OUT_ROOT / "battle_unit_manifest.json"
RIG_MANIFEST = PART_ROOT / "battle_unit_rig_manifest.json"
PREVIEW = PROJECT_ROOT / "DataTables" / "battle_unit_sprites_preview.png"
PARTS_PREVIEW = PROJECT_ROOT / "DataTables" / "battle_unit_parts_preview.png"

SIZE = 128
STYLE_LABEL = (
    "modular inked miniature style; normal-proportion Chinese topknot soldiers, "
    "blue-red-gold lamellar uniforms, tabletop hex base, articulated reusable body parts"
)


UNITS = [
    ("swordsmen_volunteers", "剑士队", "infantry", "volunteer"),
    ("matchlock_volunteers", "火绳枪队", "musket", "volunteer"),
    ("militia_volunteers", "民兵团", "skirmisher", "volunteer"),
    ("outlaw_skirmishers", "亡徒军", "skirmisher", "outlaw"),
    ("imperial_halberdiers", "禁卫长戟队", "heavy_spear", "imperial"),
    ("armored_iron_cavalry", "具装铁骑军", "heavy_cavalry", "imperial"),
    ("steel_helmet_heavy_infantry", "钢盔军", "heavy_infantry", "volunteer"),
    ("imperial_longbowmen", "禁军长弓兵", "heavy_archer", "imperial"),
    ("sword_guard_corps", "剑卫军团", "infantry", "volunteer"),
    ("imperial_axe_guard", "禁军斧卫", "heavy_brute", "imperial"),
    ("vanguard_cavalry", "先锋骑军", "cavalry", "volunteer"),
    ("solemn_guard_matchlocks", "肃卫火枪队", "musket", "volunteer"),
    ("raiders", "掠杀军", "skirmisher", "outlaw"),
    ("imperial_heavy_guard", "重甲禁卫军", "heavy_infantry", "imperial"),
    ("warhammer_volunteers", "重锤军", "heavy_brute", "volunteer"),
    ("imperial_shenji_artillery", "禁军神机队", "artillery", "imperial"),
    ("zealot_believers", "狂热信众", "skirmisher", "believer"),
    ("zealot_mob", "狂热暴徒", "brute", "believer"),
    ("leader_guard", "领袖卫队", "heavy_infantry", "believer"),
    ("elite_archers", "精锐弓兵队", "archer", "volunteer"),
    ("bandits", "土匪", "skirmisher", "outlaw"),
    ("great_axe_warriors", "巨斧军", "brute", "volunteer"),
    ("believer_elites", "信徒精锐", "infantry", "believer"),
]


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


PALETTES = {
    "volunteer": {
        "coat": (28, 66, 91),
        "coat2": (39, 85, 111),
        "pants": (31, 43, 48),
        "trim": (219, 166, 74),
        "red": (145, 47, 35),
        "flag": (43, 93, 130),
        "base": (77, 104, 84),
        "metal": (174, 176, 163),
        "leather": (91, 55, 31),
        "cloth": (194, 164, 106),
    },
    "imperial": {
        "coat": (86, 41, 54),
        "coat2": (121, 48, 48),
        "pants": (38, 35, 38),
        "trim": (229, 176, 72),
        "red": (174, 38, 37),
        "flag": (164, 35, 38),
        "base": (118, 54, 57),
        "metal": (197, 183, 131),
        "leather": (80, 47, 28),
        "cloth": (208, 174, 110),
    },
    "outlaw": {
        "coat": (80, 69, 50),
        "coat2": (111, 88, 53),
        "pants": (45, 39, 31),
        "trim": (156, 119, 70),
        "red": (118, 50, 36),
        "flag": (88, 67, 46),
        "base": (87, 75, 52),
        "metal": (133, 132, 115),
        "leather": (84, 52, 31),
        "cloth": (176, 145, 92),
    },
    "believer": {
        "coat": (103, 42, 42),
        "coat2": (136, 61, 44),
        "pants": (42, 35, 32),
        "trim": (229, 202, 133),
        "red": (168, 35, 34),
        "flag": (151, 34, 34),
        "base": (126, 65, 46),
        "metal": (174, 145, 91),
        "leather": (78, 44, 24),
        "cloth": (210, 176, 117),
    },
}


FLAG_UNITS = {
    "leader_guard",
    "imperial_halberdiers",
    "believer_elites",
    "armored_iron_cavalry",
    "imperial_heavy_guard",
}


@dataclass
class Part:
    name: str
    image: Image.Image
    pivot: tuple[int, int]
    role: str


def rgba(color, alpha=255):
    return (color[0], color[1], color[2], alpha)


def stable_hash(value):
    h = 2166136261
    for ch in str(value):
        h ^= ord(ch)
        h = (h * 16777619) & 0xFFFFFFFF
    return h


def blend(a, b, t):
    return tuple(int(a[i] * (1.0 - t) + b[i] * t) for i in range(3))


def lighten(color, amount):
    return tuple(max(0, min(255, channel + amount)) for channel in color)


def darken(color, amount):
    return tuple(max(0, min(255, channel - amount)) for channel in color)


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


def part_image(size):
    return Image.new("RGBA", (size, size), (0, 0, 0, 0))


def line(draw, points, fill, width=1, outline=(24, 18, 14, 185)):
    if outline and width >= 2:
        draw.line(points, fill=outline, width=width + 2, joint="curve")
    draw.line(points, fill=fill, width=width, joint="curve")


def ellipse(draw, box, fill, outline=(25, 19, 14, 170), width=1):
    draw.ellipse(tuple(int(v) for v in box), fill=fill, outline=outline, width=width)


def polygon(draw, points, fill, outline=(24, 18, 14, 170), width=1):
    points = [(int(x), int(y)) for x, y in points]
    draw.polygon(points, fill=fill)
    if outline:
        draw.line(points + [points[0]], fill=outline, width=width, joint="curve")


def add_ink_finish(img):
    alpha = img.getchannel("A")
    outline_alpha = alpha.filter(ImageFilter.MaxFilter(3))
    outline = Image.new("RGBA", img.size, (19, 14, 11, 0))
    outline.putalpha(outline_alpha)
    result = Image.alpha_composite(outline, img)
    result.putalpha(Image.composite(alpha, outline_alpha, alpha))
    return result.filter(ImageFilter.GaussianBlur(0.08))


def draw_lamellar_rows(draw, x0, y0, x1, rows, palette):
    width = x1 - x0
    for row in range(rows):
        y = y0 + row * 6
        line(draw, (x0, y, x1, y), rgba(palette["metal"], 110), 2, None)
        for col in range(5):
            x = x0 + col * width / 5.0 + 2
            draw.rectangle((x, y - 3, x + width / 6.0, y + 3), fill=rgba(blend(palette["coat2"], palette["metal"], 0.22), 150))


def draw_trimmed_cloth_tail(draw, palette, x, y, flip=1):
    polygon(
        draw,
        [(x, y), (x + 7 * flip, y + 3), (x + 4 * flip, y + 18), (x - 4 * flip, y + 15)],
        rgba(palette["red"], 205),
        rgba((44, 28, 20), 120),
        1,
    )
    line(draw, (x + 2 * flip, y + 4, x + 2 * flip, y + 15), rgba(palette["trim"], 120), 1, None)


def make_base_part(palette, seed):
    img = part_image(128)
    draw = ImageDraw.Draw(img, "RGBA")
    rng = random.Random(seed)
    y = 66
    top = [(27, y - 5), (44, y - 19), (83, y - 19), (102, y - 5), (84, y + 10), (44, y + 10)]
    inner = [(34, y - 8), (49, y - 16), (80, y - 16), (94, y - 8), (81, y + 2), (49, y + 2)]
    shadow = [(31, y + 3), (47, y - 8), (82, y - 8), (99, y + 3), (84, y + 15), (46, y + 15)]
    polygon(draw, shadow, (0, 0, 0, 56), None)
    polygon(draw, top, rgba((43, 36, 28), 238), rgba((21, 16, 13), 225), 2)
    polygon(draw, inner, rgba(blend(palette["base"], (172, 143, 77), 0.34), 242), rgba(palette["trim"], 210), 2)
    for _ in range(13):
        gx = rng.randint(42, 87)
        gy = rng.randint(y - 15, y + 2)
        line(draw, (gx, gy, gx + rng.randint(-4, 4), gy - rng.randint(2, 7)), rgba((70, 99, 49), 150), 1, None)
    for gx in (49, 65, 80):
        line(draw, (gx, y - 15, gx - 5, y + 1), (34, 27, 19, 46), 1, None)
    return Part("base", add_ink_finish(img), (64, 64), "base")


def make_head_part(palette, heavy, seed):
    img = part_image(80)
    draw = ImageDraw.Draw(img, "RGBA")
    rng = random.Random(seed)
    skin = (169 + rng.randint(-10, 10), 119 + rng.randint(-8, 8), 82 + rng.randint(-6, 8))
    hair = (19, 15, 13)
    ellipse(draw, (31, 29, 49, 52), rgba(skin), rgba((54, 36, 25), 220), 1)
    ellipse(draw, (35, 50, 45, 58), rgba(darken(skin, 10)), None)
    if heavy:
        draw.arc((27, 21, 53, 39), 185, 356, fill=rgba(palette["metal"], 242), width=6)
        line(draw, (29, 30, 51, 30), rgba(lighten(palette["metal"], 30), 185), 2, None)
        ellipse(draw, (36, 18, 44, 27), rgba(hair, 245), None)
        line(draw, (33, 27, 47, 27), rgba(palette["trim"], 205), 2, None)
    else:
        draw.pieslice((27, 20, 53, 43), 190, 350, fill=rgba(hair, 248))
        ellipse(draw, (35, 17, 45, 26), rgba(hair, 248), None)
        line(draw, (30, 30, 50, 30), rgba(palette["trim"], 205), 2, None)
    draw.point((36, 40), fill=(20, 15, 12, 255))
    draw.point((45, 40), fill=(20, 15, 12, 255))
    line(draw, (36, 45, 43, 46), rgba((73, 42, 32), 160), 1, None)
    return Part("head", add_ink_finish(img), (40, 57), "head")


def make_torso_part(palette, role):
    img = part_image(96)
    draw = ImageDraw.Draw(img, "RGBA")
    heavy = role.startswith("heavy") or role in ("artillery", "brute")
    shoulder = 18 if heavy else 16
    waist = 12 if heavy else 10
    coat = palette["coat2"] if heavy else palette["coat"]
    polygon(draw, [(48 - shoulder, 15), (48 + shoulder, 15), (48 + waist, 55), (48 - waist, 55)], rgba(coat), rgba((33, 24, 18), 225), 2)
    polygon(draw, [(48 - waist, 53), (48, 72), (48 + waist, 53), (55, 65), (41, 65)], rgba(blend(coat, (22, 18, 15), 0.18)), rgba((33, 24, 18), 160), 1)
    line(draw, (48 - shoulder + 3, 22, 48 + waist - 1, 50), rgba(palette["trim"], 180), 3, None)
    line(draw, (48 + shoulder - 3, 22, 48 - waist + 1, 50), rgba(palette["trim"], 110), 1, None)
    draw.rectangle((34, 47, 62, 54), fill=rgba(palette["red"], 235), outline=rgba((37, 23, 18), 180), width=1)
    draw_lamellar_rows(draw, 34, 25, 62, 4 if heavy else 3, palette)
    for col in range(4):
        x = 35 + col * 7
        polygon(
            draw,
            [(x, 55), (x + 6, 55), (x + 4, 66), (x + 1, 66)],
            rgba(blend(coat, palette["metal"], 0.15), 180),
            rgba((37, 27, 20), 90),
            1,
        )
    draw_trimmed_cloth_tail(draw, palette, 40, 53, -1)
    draw_trimmed_cloth_tail(draw, palette, 56, 53, 1)
    for x in (33, 63):
        polygon(
            draw,
            [(x - 7, 15), (x, 10), (x + 7, 16), (x + 5, 25), (x - 5, 25)],
            rgba(blend(palette["metal"], coat, 0.18), 205),
            rgba((35, 26, 20), 150),
            1,
        )
    return Part("torso", add_ink_finish(img), (48, 58), "torso")


def make_arm_part(palette, side):
    img = part_image(82)
    draw = ImageDraw.Draw(img, "RGBA")
    sleeve = palette["coat2"] if side == "front" else blend(palette["coat"], (14, 13, 12), 0.18)
    line(draw, (15, 31, 35, 27, 55, 30), rgba(sleeve), 7, rgba((28, 20, 15), 185))
    line(draw, (19, 27, 38, 25, 55, 27), rgba(lighten(sleeve, 32), 86), 2, None)
    draw.rectangle((28, 22, 43, 32), fill=rgba(blend(sleeve, palette["metal"], 0.14), 145), outline=rgba((31, 22, 17), 95), width=1)
    ellipse(draw, (53, 25, 63, 35), rgba((164, 112, 78), 245), rgba((55, 36, 25), 180), 1)
    polygon(
        draw,
        [(8, 25), (17, 20), (27, 26), (23, 37), (11, 37)],
        rgba(blend(palette["metal"], sleeve, 0.28), 205),
        rgba((35, 27, 21), 145),
        1,
    )
    return Part(side + "_arm", add_ink_finish(img), (15, 31), "arm")


def make_leg_part(palette, side):
    img = part_image(78)
    draw = ImageDraw.Draw(img, "RGBA")
    pants = palette["pants"] if side == "front" else darken(palette["pants"], 10)
    line(draw, (39, 14, 39, 42, 37, 62), rgba(pants), 8, rgba((23, 18, 15), 185))
    line(draw, (36, 17, 36, 48), rgba(lighten(pants, 28), 80), 2, None)
    draw.rectangle((28, 58, 50, 67), fill=rgba((28, 22, 18), 245), outline=rgba((16, 12, 10), 190), width=1)
    draw.rectangle((35, 36, 44, 44), fill=rgba(palette["red"], 180))
    return Part(side + "_leg", add_ink_finish(img), (39, 14), "leg")


def make_shield_part(palette, role):
    img = part_image(62)
    draw = ImageDraw.Draw(img, "RGBA")
    shield_color = blend(palette["coat"], palette["flag"], 0.42)
    if role == "heavy_spear":
        polygon(draw, [(16, 10), (46, 10), (43, 42), (31, 55), (19, 42)], rgba(shield_color, 235), rgba(palette["trim"], 225), 2)
    else:
        polygon(draw, [(19, 12), (43, 12), (45, 36), (31, 53), (17, 36)], rgba(shield_color, 235), rgba(palette["trim"], 220), 2)
    line(draw, (31, 16, 31, 48), rgba(palette["trim"], 128), 2, None)
    line(draw, (22, 26, 40, 26), rgba(lighten(shield_color, 24), 88), 2, None)
    return Part("shield", add_ink_finish(img), (31, 31), "shield")


def make_weapon_part(palette, role):
    img = part_image(140)
    draw = ImageDraw.Draw(img, "RGBA")
    metal = palette["metal"]
    wood = palette["leather"]
    pivot = (45, 70)
    if role in ("musket",):
        line(draw, (12, 75, 128, 63), rgba(wood), 6, rgba((37, 22, 14), 210))
        line(draw, (35, 70, 132, 60), rgba(metal), 2, None)
        draw.rectangle((23, 72, 48, 80), fill=rgba(darken(wood, 20), 245), outline=rgba((31, 20, 14), 180), width=1)
        line(draw, (70, 66, 83, 84), rgba((87, 47, 29), 170), 2, None)
    elif role in ("archer", "heavy_archer"):
        draw.arc((53, 30, 91, 112), -78, 76, fill=rgba((135, 82, 39), 245), width=5)
        line(draw, (75, 34, 75, 107), rgba((225, 206, 164), 160), 1, None)
        line(draw, (20, 73, 119, 60), rgba(metal), 2, None)
        polygon(draw, [(119, 60), (108, 55), (111, 64)], rgba(metal), None)
        line(draw, (33, 71, 29, 78), rgba(palette["red"], 180), 1, None)
    elif role in ("heavy_spear", "cavalry", "heavy_cavalry"):
        line(draw, (6, 92, 130, 42), rgba(wood), 5, rgba((37, 22, 14), 210))
        polygon(draw, [(132, 41), (114, 39), (124, 56)], rgba(metal), rgba((40, 32, 24), 160), 1)
        polygon(draw, [(112, 48), (96, 50), (106, 63)], rgba(palette["red"], 210), None)
    elif role in ("brute", "heavy_brute"):
        line(draw, (24, 100, 92, 35), rgba(wood), 6, rgba((36, 22, 14), 210))
        polygon(draw, [(87, 25), (119, 40), (93, 60)], rgba(metal), rgba((42, 34, 27), 160), 2)
        line(draw, (82, 39, 102, 50), rgba(lighten(metal, 35), 110), 2, None)
    else:
        line(draw, (25, 91, 55, 70), rgba((73, 43, 25), 255), 4, rgba((34, 20, 14), 190))
        blade = [(56, 67), (110, 36), (126, 25), (115, 43), (62, 73)]
        polygon(draw, blade, rgba(metal, 238), rgba((46, 41, 34), 150), 1)
        line(draw, (63, 67, 112, 40), rgba((244, 242, 223), 125), 2, None)
        polygon(draw, [(53, 67), (44, 58), (50, 77)], rgba(palette["trim"], 220), rgba((38, 27, 18), 120), 1)
    return Part("weapon", add_ink_finish(img), pivot, "weapon")


def make_flag_part(palette):
    img = part_image(96)
    draw = ImageDraw.Draw(img, "RGBA")
    line(draw, (24, 83, 24, 20), rgba((66, 46, 30), 245), 4, rgba((31, 20, 13), 180))
    flag = [(27, 19), (74, 28), (68, 49), (27, 42)]
    polygon(draw, flag, rgba(palette["flag"], 232), rgba(palette["trim"], 180), 2)
    line(draw, (35, 29, 64, 34), rgba(palette["trim"], 150), 2, None)
    line(draw, (32, 39, 60, 43), rgba(darken(palette["flag"], 34), 115), 2, None)
    return Part("flag", add_ink_finish(img), (24, 83), "flag")


def make_horse_body_part(palette, heavy, seed):
    img = part_image(122)
    draw = ImageDraw.Draw(img, "RGBA")
    rng = random.Random(seed)
    horse = (77, 54, 39) if not heavy else (48, 45, 42)
    horse = tuple(max(0, min(255, channel + rng.randint(-5, 5))) for channel in horse)
    ellipse(draw, (21, 48, 89, 78), rgba(horse, 248), rgba((30, 21, 17), 220), 2)
    polygon(draw, [(28, 50), (12, 43), (25, 62)], rgba(darken(horse, 8), 238), rgba((30, 21, 17), 180), 1)
    draw.arc((22, 43, 91, 81), 185, 360, fill=rgba(lighten(horse, 32), 75), width=3)
    draw.rectangle((41, 43, 72, 62), fill=rgba(palette["coat"], 230), outline=rgba(palette["trim"], 190), width=2)
    draw.rectangle((43, 41, 70, 47), fill=rgba(palette["red"], 220))
    if heavy:
        draw.arc((22, 45, 91, 82), 180, 360, fill=rgba(palette["metal"], 205), width=6)
        line(draw, (41, 50, 78, 50), rgba(palette["trim"], 145), 2, None)
    return Part("horse_body", add_ink_finish(img), (58, 63), "horse")


def make_horse_head_part(palette, heavy):
    img = part_image(72)
    draw = ImageDraw.Draw(img, "RGBA")
    horse = (78, 55, 40) if not heavy else (50, 47, 43)
    ellipse(draw, (22, 24, 57, 47), rgba(horse, 248), rgba((30, 21, 17), 220), 2)
    line(draw, (47, 28, 63, 17), rgba(darken(horse, 10), 245), 5, rgba((30, 21, 17), 180))
    line(draw, (31, 28, 54, 37), rgba(palette["trim"], 165), 2, None)
    draw.point((55, 29), fill=(10, 8, 7, 255))
    return Part("horse_head", add_ink_finish(img), (26, 37), "horse")


def make_horse_leg_part(heavy):
    img = part_image(62)
    draw = ImageDraw.Draw(img, "RGBA")
    color = (62, 48, 38) if not heavy else (45, 42, 40)
    line(draw, (31, 8, 29, 35, 32, 54), rgba(color, 245), 6, rgba((26, 19, 15), 190))
    line(draw, (24, 54, 40, 54), rgba((24, 19, 16), 245), 3, None)
    return Part("horse_leg", add_ink_finish(img), (31, 8), "horse")


def make_cannon_part(palette):
    img = part_image(132)
    draw = ImageDraw.Draw(img, "RGBA")
    draw.rectangle((28, 75, 83, 91), fill=rgba((45, 39, 33), 245), outline=rgba(palette["trim"], 175), width=2)
    ellipse(draw, (26, 84, 47, 105), rgba(palette["metal"], 238), rgba((32, 25, 20), 210), 2)
    ellipse(draw, (73, 84, 94, 105), rgba(palette["metal"], 238), rgba((32, 25, 20), 210), 2)
    line(draw, (52, 72, 117, 50), rgba(palette["metal"], 255), 12, rgba((31, 25, 20), 190))
    line(draw, (49, 68, 114, 47), rgba(lighten(palette["metal"], 40), 115), 3, None)
    return Part("cannon", add_ink_finish(img), (65, 83), "cannon")


def make_unit_parts(unit_id, role, family):
    palette = PALETTES[family]
    seed = stable_hash(unit_id)
    heavy = role.startswith("heavy") or role == "artillery"
    parts = {
        "base": make_base_part(palette, seed),
        "head": make_head_part(palette, heavy, seed),
        "torso": make_torso_part(palette, role),
        "front_arm": make_arm_part(palette, "front"),
        "back_arm": make_arm_part(palette, "back"),
        "front_leg": make_leg_part(palette, "front"),
        "back_leg": make_leg_part(palette, "back"),
        "weapon": make_weapon_part(palette, role),
    }
    if role in ("infantry", "heavy_infantry", "heavy_spear"):
        parts["shield"] = make_shield_part(palette, role)
    if unit_id in FLAG_UNITS:
        parts["flag"] = make_flag_part(palette)
    if role in ("cavalry", "heavy_cavalry"):
        parts["horse_body"] = make_horse_body_part(palette, role == "heavy_cavalry", seed)
        parts["horse_head"] = make_horse_head_part(palette, role == "heavy_cavalry")
        parts["horse_leg"] = make_horse_leg_part(role == "heavy_cavalry")
    if role == "artillery":
        parts["cannon"] = make_cannon_part(palette)
    return parts


def rotate_part(part, angle, scale=1.0):
    image = part.image
    pivot = part.pivot
    if scale != 1.0:
        new_size = (max(1, int(image.width * scale)), max(1, int(image.height * scale)))
        image = image.resize(new_size, Image.Resampling.LANCZOS)
        pivot = (int(pivot[0] * scale), int(pivot[1] * scale))
    if abs(angle) > 0.01:
        image = image.rotate(angle, resample=Image.Resampling.BICUBIC, center=pivot)
    return image, pivot


def paste_part(canvas, part, anchor, angle=0.0, scale=1.0):
    image, pivot = rotate_part(part, angle, scale)
    x = int(round(anchor[0] - pivot[0]))
    y = int(round(anchor[1] - pivot[1]))
    canvas.alpha_composite(image, (x, y))


def pose_for(role, anim, frame):
    if anim == "idle":
        return {
            "bob": 0,
            "hit_x": 0,
            "torso": 0,
            "head": 0,
            "front_leg": 6,
            "back_leg": -5,
            "front_arm": -40 if role in ("musket", "archer", "heavy_archer") else -58,
            "back_arm": -112 if role in ("musket", "archer", "heavy_archer") else -126,
            "weapon": -7 if role in ("musket", "archer", "heavy_archer") else -38,
            "attack_flash": 0,
        }
    if anim == "move":
        cycle = [0, 1, 0, -1][frame % 4]
        return {
            "bob": -abs(cycle) * 2,
            "hit_x": 0,
            "torso": cycle * 2,
            "head": -cycle * 1,
            "front_leg": 6 + cycle * 18,
            "back_leg": -5 - cycle * 18,
            "front_arm": (-58 - cycle * 12) if role not in ("musket", "archer", "heavy_archer") else -28,
            "back_arm": (-126 + cycle * 10) if role not in ("musket", "archer", "heavy_archer") else -116,
            "weapon": (-38 - cycle * 10) if role not in ("musket", "archer", "heavy_archer") else -5,
            "attack_flash": 0,
        }
    if anim == "attack":
        attacks = [
            {"torso": -5, "front_arm": -80, "back_arm": -132, "weapon": -56, "bob": -1, "flash": 0},
            {"torso": 6, "front_arm": -35, "back_arm": -100, "weapon": -18, "bob": -2, "flash": 0},
            {"torso": 10, "front_arm": 0, "back_arm": -75, "weapon": 4, "bob": -4, "flash": 1},
            {"torso": 0, "front_arm": -52, "back_arm": -114, "weapon": -32, "bob": 0, "flash": 0},
        ]
        p = attacks[frame % 4]
        if role in ("musket", "archer", "heavy_archer", "artillery"):
            p = [
                {"torso": -2, "front_arm": -12, "back_arm": -174, "weapon": -5, "bob": 0, "flash": 0},
                {"torso": -1, "front_arm": -8, "back_arm": -168, "weapon": -3, "bob": -1, "flash": 0},
                {"torso": 2, "front_arm": -2, "back_arm": -160, "weapon": 0, "bob": -2, "flash": 1},
                {"torso": 0, "front_arm": -20, "back_arm": -174, "weapon": -7, "bob": 0, "flash": 0},
            ][frame % 4]
        return {
            "bob": p["bob"],
            "hit_x": 0,
            "torso": p["torso"],
            "head": p["torso"] * 0.25,
            "front_leg": 12,
            "back_leg": -16,
            "front_arm": p["front_arm"],
            "back_arm": p["back_arm"],
            "weapon": p["weapon"],
            "attack_flash": p["flash"],
        }
    hit = [
        {"hit_x": 0, "bob": 0, "torso": 0, "head": 0},
        {"hit_x": -6, "bob": -3, "torso": -8, "head": -9},
        {"hit_x": 4, "bob": 2, "torso": 6, "head": 7},
        {"hit_x": 0, "bob": 0, "torso": 0, "head": 0},
    ][frame % 4]
    return {
        "bob": hit["bob"],
        "hit_x": hit["hit_x"],
        "torso": hit["torso"],
        "head": hit["head"],
        "front_leg": 6,
        "back_leg": -5,
        "front_arm": -55,
        "back_arm": -126,
        "weapon": -38,
        "attack_flash": 0,
    }


def draw_attack_effect(draw, role, pose):
    if not pose["attack_flash"]:
        return
    if role == "musket":
        ellipse(draw, (109, 43, 138, 70), (255, 226, 125, 142), None)
        polygon(draw, [(116, 48), (151, 39), (129, 66)], (255, 190, 65, 174), None)
    elif role in ("archer", "heavy_archer"):
        line(draw, (97, 55, 133, 41), (245, 232, 171, 205), 2, None)
    elif role == "artillery":
        ellipse(draw, (104, 40, 148, 79), (255, 208, 93, 135), None)
        polygon(draw, [(118, 43), (153, 58), (116, 79)], (255, 192, 64, 165), None)
    else:
        draw.arc((70, 16, 130, 92), -28, 54, fill=(255, 224, 116, 150), width=6)


def draw_hit_effect(draw, frame):
    if frame not in (1, 2):
        return
    for i in range(5):
        x = 27 + i * 18 + (frame % 2) * 6
        y = 35 + (i % 2) * 15
        line(draw, (x, y, x + 9, y - 9), (238, 204, 112, 175), 2, None)


def compose_soldier(parts, role, anim, frame, unit_id):
    pose = pose_for(role, anim, frame)
    canvas = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    x = 64 + pose["hit_x"]
    y = 0 + pose["bob"]
    scale = 1.0
    if role in ("heavy_spear", "heavy_infantry", "heavy_brute", "brute"):
        scale = 1.05
    if role in ("musket", "archer", "heavy_archer", "skirmisher"):
        scale = 0.98

    paste_part(canvas, parts["base"], (64, 105))
    if "flag" in parts:
        paste_part(canvas, parts["flag"], (33, 98 + y * 0.25), -4 if anim == "move" and frame % 2 else 2)
    paste_part(canvas, parts["back_leg"], (x - 6, 76 + y), pose["back_leg"], scale)
    paste_part(canvas, parts["front_leg"], (x + 7, 76 + y), pose["front_leg"], scale)
    paste_part(canvas, parts["back_arm"], (x - 9, 49 + y), pose["back_arm"], scale)
    if "shield" in parts:
        paste_part(canvas, parts["shield"], (x - 18, 67 + y), -8 + pose["torso"] * 0.3, 0.9)
    paste_part(canvas, parts["torso"], (x, 74 + y), pose["torso"], scale)

    if role in ("musket", "archer", "heavy_archer"):
        paste_part(canvas, parts["weapon"], (x + 11, 57 + y), pose["weapon"], 0.93)
    else:
        paste_part(canvas, parts["weapon"], (x + 18, 55 + y), pose["weapon"], 0.92 if role == "skirmisher" else 1.0)

    paste_part(canvas, parts["front_arm"], (x + 9, 50 + y), pose["front_arm"], scale)
    paste_part(canvas, parts["head"], (x, 43 + y), pose["head"], scale)
    draw = ImageDraw.Draw(canvas, "RGBA")
    draw_attack_effect(draw, role, pose)
    if anim == "hit":
        draw_hit_effect(draw, frame)
        if frame in (1, 2):
            flash = Image.new("RGBA", (SIZE, SIZE), (220, 45, 36, 58))
            flash.putalpha(canvas.getchannel("A").point(lambda a: min(a, 58)))
            canvas = Image.alpha_composite(canvas, flash)
    if anim == "idle":
        # Idle remains a standing token; duplicate frames avoid unintended bobbing.
        pass
    return add_ink_finish(canvas)


def compose_cavalry(parts, role, anim, frame, unit_id):
    pose = pose_for(role, anim, frame)
    canvas = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(canvas, "RGBA")
    y = pose["bob"]
    x = 62 + pose["hit_x"]
    paste_part(canvas, parts["base"], (64, 106))
    if "flag" in parts:
        paste_part(canvas, parts["flag"], (31, 99 + y * 0.25), -7 if frame % 2 else 4)
    leg_angles = [12, -9, -8, 14] if anim != "move" else [18, -21, -16, 22]
    for idx, hx in enumerate((42, 56, 74, 88)):
        paste_part(canvas, parts["horse_leg"], (hx, 79 + y), leg_angles[(idx + frame) % 4], 0.92)
    paste_part(canvas, parts["horse_body"], (64 + pose["hit_x"], 70 + y), pose["torso"] * 0.35, 1.0)
    paste_part(canvas, parts["horse_head"], (92 + pose["hit_x"], 59 + y), pose["head"] * 0.3, 1.0)
    paste_part(canvas, parts["back_arm"], (x - 1, 36 + y), pose["back_arm"], 0.72)
    paste_part(canvas, parts["torso"], (x - 2, 58 + y), pose["torso"], 0.72)
    paste_part(canvas, parts["weapon"], (x + 12, 40 + y), pose["weapon"] - 10, 0.82)
    paste_part(canvas, parts["front_arm"], (x + 4, 37 + y), pose["front_arm"], 0.72)
    paste_part(canvas, parts["head"], (x - 2, 34 + y), pose["head"], 0.72)
    draw_attack_effect(draw, role, pose)
    if anim == "hit":
        draw_hit_effect(draw, frame)
    return add_ink_finish(canvas)


def compose_artillery(parts, role, anim, frame, unit_id):
    pose = pose_for(role, anim, frame)
    canvas = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(canvas, "RGBA")
    paste_part(canvas, parts["base"], (64, 106))
    paste_part(canvas, parts["back_leg"], (41, 76), -6, 0.62)
    paste_part(canvas, parts["torso"], (42, 75), -2, 0.62)
    paste_part(canvas, parts["head"], (42, 47), 0, 0.62)
    paste_part(canvas, parts["front_leg"], (88, 76), 4, 0.62)
    paste_part(canvas, parts["torso"], (88, 75), 3, 0.62)
    paste_part(canvas, parts["head"], (88, 47), 1, 0.62)
    paste_part(canvas, parts["cannon"], (65, 83 + pose["bob"]), -2 if frame == 2 and anim == "attack" else 0)
    draw_attack_effect(draw, role, pose)
    if anim == "hit":
        draw_hit_effect(draw, frame)
    return add_ink_finish(canvas)


def render_unit(parts, unit_id, role, anim, frame):
    if role in ("cavalry", "heavy_cavalry"):
        return compose_cavalry(parts, role, anim, frame, unit_id)
    if role == "artillery":
        return compose_artillery(parts, role, anim, frame, unit_id)
    return compose_soldier(parts, role, anim, frame, unit_id)


def save_unit_parts(unit_id, role, family, parts):
    unit_part_dir = PART_ROOT / unit_id
    unit_part_dir.mkdir(parents=True, exist_ok=True)
    rig_parts = []
    for name, part in parts.items():
        path = unit_part_dir / f"{name}.png"
        part.image.save(path)
        rig_parts.append(
            {
                "name": name,
                "role": part.role,
                "pivot": {"x": part.pivot[0], "y": part.pivot[1]},
                "asset": f"Art/BattleUnitParts/{unit_id}/{name}",
            }
        )
    (unit_part_dir / "rig.json").write_text(
        json.dumps(
            {
                "id": unit_id,
                "role": role,
                "family": family,
                "style": STYLE_LABEL,
                "parts": rig_parts,
            },
            ensure_ascii=False,
            indent=2,
        )
        + "\n",
        encoding="utf-8",
    )
    return rig_parts


def save_frames():
    OUT_ROOT.mkdir(parents=True, exist_ok=True)
    PART_ROOT.mkdir(parents=True, exist_ok=True)
    manifest_units = []
    rig_units = []
    preview_rows = []
    parts_preview_rows = []
    for unit_id, name, role, family in UNITS:
        parts = make_unit_parts(unit_id, role, family)
        rig_parts = save_unit_parts(unit_id, role, family, parts)
        unit_dir = OUT_ROOT / unit_id
        unit_dir.mkdir(parents=True, exist_ok=True)
        for anim in ("idle", "move", "attack", "hit"):
            frames = 2 if anim == "idle" else 4
            for frame in range(frames):
                image = render_unit(parts, unit_id, role, anim, 0 if anim == "idle" else frame)
                image.save(unit_dir / f"{anim}_{frame}.png")

        manifest_units.append(
            {
                "id": unit_id,
                "name": name,
                "role": role,
                "roleDisplay": ROLE_DISPLAY[role],
                "family": family,
                "asset": f"Art/BattleUnits/{unit_id}",
                "rig": f"Art/BattleUnitParts/{unit_id}/rig",
                "idleFrames": 2,
                "moveFrames": 4,
                "attackFrames": 4,
                "hitFrames": 4,
            }
        )
        rig_units.append(
            {
                "id": unit_id,
                "name": name,
                "role": role,
                "family": family,
                "parts": rig_parts,
            }
        )
        preview_rows.append(make_unit_preview_row(parts, unit_id, name, role, family))
        parts_preview_rows.append(make_parts_preview_row(parts, unit_id, name, role))

    MANIFEST.write_text(
        json.dumps(
            {
                "generatedAt": datetime.now().isoformat(timespec="seconds"),
                "style": STYLE_LABEL,
                "animationPipeline": "body-part-rigged-composition",
                "units": manifest_units,
            },
            ensure_ascii=False,
            indent=2,
        )
        + "\n",
        encoding="utf-8",
    )
    RIG_MANIFEST.write_text(
        json.dumps(
            {
                "generatedAt": datetime.now().isoformat(timespec="seconds"),
                "style": STYLE_LABEL,
                "units": rig_units,
            },
            ensure_ascii=False,
            indent=2,
        )
        + "\n",
        encoding="utf-8",
    )
    save_preview_grid(preview_rows, PREVIEW, 2)
    save_preview_grid(parts_preview_rows, PARTS_PREVIEW, 1)


def make_unit_preview_row(parts, unit_id, name, role, family):
    row = Image.new("RGBA", (430, 136), (29, 25, 21, 255))
    draw = ImageDraw.Draw(row, "RGBA")
    text_font = font(14)
    small_font = font(11)
    draw.text((10, 8), f"{name} / {ROLE_DISPLAY[role]} / {family}", font=text_font, fill=(235, 220, 184, 255))
    samples = [("idle", 0), ("move", 1), ("attack", 2), ("hit", 1)]
    for i, (anim, frame) in enumerate(samples):
        piece = render_unit(parts, unit_id, role, anim, frame).resize((88, 88), Image.Resampling.LANCZOS)
        row.alpha_composite(piece, (10 + i * 104, 30))
        draw.text((32 + i * 104, 119), anim, font=small_font, fill=(218, 201, 166, 255))
    return row.convert("RGB")


def make_parts_preview_row(parts, unit_id, name, role):
    row = Image.new("RGBA", (860, 102), (27, 24, 21, 255))
    draw = ImageDraw.Draw(row, "RGBA")
    text_font = font(13)
    small_font = font(9)
    draw.text((10, 8), f"{name}  {unit_id}  rig parts", font=text_font, fill=(235, 220, 184, 255))
    x = 10
    for idx, key in enumerate(sorted(parts.keys())):
        part = parts[key].image.copy()
        part.thumbnail((58, 58), Image.Resampling.LANCZOS)
        px = 178 + idx * 66
        if px + 58 >= row.width:
            break
        row.alpha_composite(part, (px, 24))
        draw.text((px, 82), key[:9], font=small_font, fill=(194, 178, 143, 255))
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


def main():
    save_frames()
    print(f"Generated {len(UNITS)} modular battle unit animation sets at {OUT_ROOT}")
    print(f"Generated articulated body parts at {PART_ROOT}")
    print(f"Preview: {PREVIEW}")
    print(f"Parts preview: {PARTS_PREVIEW}")


if __name__ == "__main__":
    main()
