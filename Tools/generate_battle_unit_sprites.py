from __future__ import annotations

import json
import math
import random
from datetime import datetime
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter


PROJECT_ROOT = Path(__file__).resolve().parents[1]
ART_ROOT = PROJECT_ROOT / "Assets" / "Resources" / "Art"
OUT_ROOT = ART_ROOT / "BattleUnits"
TERRAIN_ROOT = ART_ROOT / "Terrain"
SCENE_ROOT = ART_ROOT / "Scenes"
MANIFEST = OUT_ROOT / "battle_unit_manifest.json"
PREVIEW = PROJECT_ROOT / "DataTables" / "battle_unit_sprites_preview.png"
MAP_PREVIEW = PROJECT_ROOT / "DataTables" / "battle_map_style_preview.png"
SIZE = 128
STYLE_LABEL = "chosen tactical token style C; normal-proportion single soldier, hex base, Chinese topknot, blue-red-gold lamellar uniforms"


UNITS = [
    ("swordsmen_volunteers", "infantry", "volunteer"),
    ("matchlock_volunteers", "musket", "volunteer"),
    ("militia_volunteers", "skirmisher", "volunteer"),
    ("outlaw_skirmishers", "skirmisher", "outlaw"),
    ("imperial_halberdiers", "heavy_spear", "imperial"),
    ("armored_iron_cavalry", "heavy_cavalry", "imperial"),
    ("steel_helmet_heavy_infantry", "heavy_infantry", "volunteer"),
    ("imperial_longbowmen", "heavy_archer", "imperial"),
    ("sword_guard_corps", "infantry", "volunteer"),
    ("imperial_axe_guard", "heavy_brute", "imperial"),
    ("vanguard_cavalry", "cavalry", "volunteer"),
    ("solemn_guard_matchlocks", "musket", "volunteer"),
    ("raiders", "skirmisher", "outlaw"),
    ("imperial_heavy_guard", "heavy_infantry", "imperial"),
    ("warhammer_volunteers", "heavy_brute", "volunteer"),
    ("imperial_shenji_artillery", "artillery", "imperial"),
    ("zealot_believers", "skirmisher", "believer"),
    ("zealot_mob", "brute", "believer"),
    ("leader_guard", "heavy_infantry", "believer"),
    ("elite_archers", "archer", "volunteer"),
    ("bandits", "skirmisher", "outlaw"),
    ("great_axe_warriors", "brute", "volunteer"),
    ("believer_elites", "infantry", "believer"),
]


ROLE_DISPLAY = {
    "infantry": "infantry",
    "musket": "matchlock",
    "skirmisher": "skirmisher",
    "heavy_spear": "heavy spear",
    "heavy_cavalry": "heavy cavalry",
    "heavy_infantry": "heavy infantry",
    "heavy_archer": "heavy archer",
    "heavy_brute": "heavy brute",
    "cavalry": "cavalry",
    "artillery": "artillery",
    "brute": "brute",
    "archer": "archer",
}


PALETTES = {
    "volunteer": {
        "coat": (36, 71, 78),
        "coat2": (51, 99, 91),
        "pants": (35, 42, 43),
        "trim": (219, 172, 76),
        "flag": (49, 113, 144),
        "base": (50, 98, 119),
        "metal": (168, 169, 154),
        "leather": (82, 49, 28),
    },
    "imperial": {
        "coat": (83, 39, 51),
        "coat2": (121, 43, 47),
        "pants": (42, 37, 39),
        "trim": (231, 178, 74),
        "flag": (172, 34, 39),
        "base": (126, 44, 51),
        "metal": (196, 181, 132),
        "leather": (75, 43, 26),
    },
    "outlaw": {
        "coat": (80, 67, 49),
        "coat2": (112, 89, 53),
        "pants": (44, 39, 31),
        "trim": (156, 119, 70),
        "flag": (88, 67, 46),
        "base": (87, 69, 48),
        "metal": (133, 132, 115),
        "leather": (84, 52, 31),
    },
    "believer": {
        "coat": (103, 42, 42),
        "coat2": (133, 56, 43),
        "pants": (42, 35, 32),
        "trim": (229, 203, 138),
        "flag": (156, 34, 34),
        "base": (126, 37, 37),
        "metal": (174, 145, 91),
        "leather": (78, 44, 24),
    },
}


def rgba(c, a=255):
    return (c[0], c[1], c[2], a)


def stable_hash(value):
    h = 2166136261
    for ch in str(value):
        h ^= ord(ch)
        h = (h * 16777619) & 0xFFFFFFFF
    return h


def blend(a, b, t):
    return tuple(int(a[i] * (1 - t) + b[i] * t) for i in range(3))


def lighten(c, amount):
    return tuple(max(0, min(255, x + amount)) for x in c)


def ellipse(draw, box, fill, outline=None, width=1):
    draw.ellipse(tuple(int(v) for v in box), fill=fill, outline=outline, width=width)


def line(draw, xy, fill, width=1):
    draw.line(tuple(int(v) for v in xy), fill=fill, width=width)


def draw_compact_base(draw, p, phase, hit_flash=0):
    y = 104 + math.sin(phase) * 0.5
    shadow = [(32, y - 3), (47, y - 13), (82, y - 13), (99, y - 3), (83, y + 9), (46, y + 9)]
    top = [(29, y - 6), (45, y - 19), (83, y - 19), (101, y - 6), (84, y + 8), (45, y + 8)]
    rim = [(34, y - 8), (48, y - 17), (81, y - 17), (95, y - 8), (81, y + 3), (49, y + 3)]
    draw.polygon([(int(x), int(yy + 6)) for x, yy in shadow], fill=(0, 0, 0, 52))
    draw.polygon([(int(x), int(yy)) for x, yy in top], fill=rgba((43, 37, 29), 230), outline=rgba((26, 21, 17), 220))
    draw.polygon([(int(x), int(yy)) for x, yy in rim], fill=rgba(blend(p["base"], (166, 139, 79), 0.38), 238), outline=rgba(p["trim"], 230))
    draw.line([(37, y - 7), (49, y - 15), (80, y - 15), (92, y - 7)], fill=rgba(lighten(p["base"], 56), 110), width=2)
    for gx in (48, 64, 80):
        draw.line((gx, y - 14, gx - 5, y + 2), fill=(35, 29, 22, 42), width=1)
    if hit_flash:
        draw.polygon([(int(x), int(yy)) for x, yy in top], fill=(225, 45, 36, hit_flash))


def draw_small_flag(draw, p, x, y, phase, attack):
    wave = int(math.sin(phase) * 2)
    line(draw, (x, y + 10, x, y + 66), rgba((62, 47, 34), 235), 3)
    flag = [
        (x + 3, y + 10),
        (x + 31 + int(attack * 5), y + 15 + wave),
        (x + 25, y + 29 + wave),
        (x + 3, y + 26),
    ]
    draw.polygon(flag, fill=rgba(p["flag"], 225), outline=rgba(p["trim"], 150))
    line(draw, (x + 8, y + 15, x + 26, y + 17 + wave), rgba(p["trim"], 140), 2)


def soldier_skin(seed):
    rng = random.Random(seed)
    return (
        168 + rng.randint(-12, 11),
        119 + rng.randint(-9, 8),
        83 + rng.randint(-8, 7),
    )


def draw_head(draw, x, y, p, heavy, seed, scale=1.0):
    skin = soldier_skin(seed)
    hair = (20, 16, 14)
    rx = int(5 * scale)
    ry = int(6 * scale)
    ellipse(draw, (x - rx, y - ry, x + rx, y + ry), rgba(skin), (55, 38, 26, 200), 1)
    if heavy:
        draw.arc((x - rx - 2, y - ry - 3, x + rx + 2, y + ry - 1), 185, 355, fill=rgba(p["metal"], 238), width=max(2, int(2 * scale)))
        line(draw, (x - rx, y - ry - 1, x + rx, y - ry - 1), rgba(lighten(p["metal"], 24), 170), 1)
        ellipse(draw, (x - 3, y - ry - 8, x + 3, y - ry - 3), rgba(hair, 235))
        line(draw, (x - 4, y - ry - 4, x + 4, y - ry - 4), rgba(p["trim"], 190), 1)
    else:
        draw.pieslice((x - rx - 2, y - ry - 5, x + rx + 2, y + 2), 190, 350, fill=rgba(hair, 245))
        ellipse(draw, (x - 3, y - ry - 7, x + 3, y - ry - 2), rgba(hair, 245))
        line(draw, (x - rx, y - ry - 2, x + rx, y - ry - 2), rgba(p["trim"], 205), 1)
    draw.point((x - 2, y - 1), fill=(22, 17, 13, 255))
    draw.point((x + 3, y - 1), fill=(22, 17, 13, 255))


def draw_realistic_legs(draw, x, y, p, phase, scale=1.0):
    step = int(math.sin(phase) * 4 * scale)
    pants = p["pants"]
    boots = (30, 23, 18)
    hip_y = y + int(30 * scale)
    knee_y = y + int(47 * scale)
    foot_y = y + int(64 * scale)
    lw = max(3, int(4 * scale))
    line(draw, (x - 5, hip_y, x - 7 + step, knee_y, x - 9 + step, foot_y), rgba(pants), lw)
    line(draw, (x + 5, hip_y, x + 7 - step, knee_y, x + 9 - step, foot_y), rgba(pants), lw)
    line(draw, (x - 13 + step, foot_y, x - 4 + step, foot_y), rgba(boots), max(2, int(3 * scale)))
    line(draw, (x + 4 - step, foot_y, x + 14 - step, foot_y), rgba(boots), max(2, int(3 * scale)))


def draw_realistic_torso(draw, x, y, p, heavy, scale=1.0):
    shoulder = int((10 if not heavy else 12) * scale)
    waist = int((7 if not heavy else 9) * scale)
    coat = p["coat2"] if heavy else p["coat"]
    top_y = y + int(8 * scale)
    waist_y = y + int(34 * scale)
    tail_y = y + int(45 * scale)
    draw.polygon(
        [(x - shoulder, top_y), (x + shoulder, top_y), (x + waist, waist_y), (x - waist, waist_y)],
        fill=rgba(coat),
        outline=(35, 25, 20, 230),
    )
    draw.polygon(
        [(x - waist, waist_y), (x, tail_y), (x + waist, waist_y), (x + 4, waist_y + 5), (x - 4, waist_y + 5)],
        fill=rgba(blend(coat, (24, 19, 16), 0.18)),
        outline=(35, 25, 20, 150),
    )
    line(draw, (x - shoulder + 3, top_y + 4, x + waist - 1, waist_y - 5), rgba(p["trim"], 170), max(1, int(2 * scale)))
    line(draw, (x + shoulder - 2, top_y + 4, x - waist + 2, waist_y - 4), rgba(p["trim"], 105), 1)
    if heavy:
        for k in range(4):
            yy = top_y + 5 + k * 6
            line(draw, (x - shoulder + 2, yy, x + shoulder - 2, yy), rgba(p["metal"], 145), 2)


def draw_arm_and_weapon(draw, role, x, y, p, attack, phase, facing=1, scale=1.0):
    metal = p["metal"]
    wood = p["leather"]
    sleeve = blend(p["coat"], (21, 18, 16), 0.12)
    top_y = y + int(17 * scale)
    hand_y = y + int((33 - attack * 8) * scale)
    front_hand_x = x + int((14 + attack * 4) * facing * scale)
    back_hand_x = x - int(10 * facing * scale)
    line(draw, (x + 8 * facing, top_y, front_hand_x, hand_y), rgba(sleeve), max(3, int(4 * scale)))
    line(draw, (x - 7 * facing, top_y + 4, back_hand_x, hand_y + 6), rgba(sleeve), max(3, int(4 * scale)))

    if role == "musket":
        yy = hand_y - int(4 * attack)
        line(draw, (x - 20 * facing, yy + 10, x + (35 + 6 * attack) * facing, yy - 4), rgba(wood), max(3, int(4 * scale)))
        line(draw, (x - 9 * facing, yy + 5, x + (40 + 6 * attack) * facing, yy - 6), rgba(metal), 2)
        if attack > 0.55:
            fx = x + int((47 + 6 * attack) * facing)
            draw.polygon([(fx, yy - 10), (fx + 20 * facing, yy - 16), (fx + 11 * facing, yy + 4)], fill=(255, 205, 80, 190))
            ellipse(draw, (fx - 8, yy - 12, fx + 12, yy + 6), (255, 235, 150, 110))
    elif role in ("archer", "heavy_archer"):
        bx = x + int(18 * facing * scale)
        draw.arc((bx - 13, y + 3, bx + 13, y + 51), -67 if facing > 0 else 113, 70 if facing > 0 else 247, fill=rgba((122, 75, 38)), width=max(2, int(3 * scale)))
        line(draw, (x - 2 * facing, y + 28, bx + int((14 + attack * 6) * facing), y + int(22 - attack * 8)), rgba(metal), 2)
        if attack > 0.62:
            line(draw, (bx + 18 * facing, y + 22, bx + 44 * facing, y + 11), rgba(metal), 2)
    elif role == "heavy_spear":
        tx = x + int((38 + attack * 16) * facing)
        ty = y + int(5 - attack * 5)
        line(draw, (x - 16 * facing, y + 48, tx, ty), rgba(wood), max(3, int(4 * scale)))
        draw.polygon([(tx, ty - 11), (tx - 7 * facing, ty + 2), (tx - 15 * facing, ty - 12)], fill=rgba(metal))
        draw.polygon([(tx - 8 * facing, ty - 5), (tx - 19 * facing, ty + 1), (tx - 13 * facing, ty - 12)], fill=rgba(p["trim"], 205))
    elif role in ("brute", "heavy_brute"):
        hx = x + int((26 + attack * 13) * facing)
        hy = y + int(10 - attack * 12)
        line(draw, (x - 14 * facing, y + 48, hx, hy), rgba(wood), 5)
        draw.polygon([(hx, hy - 12), (hx + 14 * facing, hy), (hx, hy + 13)], fill=rgba(metal))
    elif role == "infantry":
        tx = x + int((25 + attack * 14) * facing)
        ty = y + int(14 - attack * 7)
        line(draw, (x - 10 * facing, y + 44, tx - 5 * facing, ty + 7), rgba((72, 45, 26)), max(2, int(3 * scale)))
        draw.arc((tx - 25, ty - 16, tx + 19, ty + 28), -55 if facing > 0 else 125, 26 if facing > 0 else 206, fill=rgba(metal), width=max(3, int(4 * scale)))
        draw.arc((tx - 22, ty - 13, tx + 18, ty + 24), -50 if facing > 0 else 130, 16 if facing > 0 else 196, fill=rgba((240, 238, 218), 150), width=1)
    else:
        tx = x + int((30 + attack * 13) * facing)
        ty = y + int(12 - attack * 6)
        line(draw, (x - 14 * facing, y + 48, tx, ty), rgba(wood), max(3, int(4 * scale)))
        draw.polygon([(tx, ty - 11), (tx + 8 * facing, ty + 2), (tx - 6 * facing, ty + 10)], fill=rgba(metal))


def draw_shield(draw, role, x, y, p, facing=1, scale=1.0):
    if role not in ("infantry", "heavy_infantry", "heavy_spear"):
        return
    sx = x - int(15 * facing * scale)
    fill = blend(p["coat"], p["flag"], 0.36)
    draw.polygon(
        [
            (sx - 7, y + 24),
            (sx + 7, y + 24),
            (sx + 6, y + 42),
            (sx, y + 52),
            (sx - 6, y + 42),
        ],
        fill=rgba(fill, 232),
        outline=rgba(p["trim"], 200),
    )
    line(draw, (sx, y + 27, sx, y + 48), rgba(p["trim"], 125), 1)


def draw_normal_soldier(draw, role, x, y, p, phase, attack, seed, facing=1, scale=1.0):
    heavy = role.startswith("heavy") or role == "artillery"
    draw_realistic_legs(draw, x, y, p, phase + seed, scale)
    draw_realistic_torso(draw, x, y, p, heavy, scale)
    draw_shield(draw, role, x, y, p, facing, scale)
    weapon_role = "infantry" if role == "skirmisher" else role
    draw_arm_and_weapon(draw, weapon_role, x, y, p, attack, phase, facing, scale)
    draw_head(draw, x, y + int(1 * scale), p, heavy, seed, scale)


def draw_horseman(draw, x, y, p, phase, heavy, attack, seed, facing=1):
    rng = random.Random(seed)
    horse = (78, 55, 40) if not heavy else (48, 45, 42)
    horse = tuple(max(0, min(255, v + rng.randint(-5, 5))) for v in horse)
    ellipse(draw, (x - 31, y + 20, x + 29, y + 45), rgba(horse), (34, 24, 19, 220), 1)
    ellipse(draw, (x + 16 * facing, y + 9, x + 43 * facing, y + 29), rgba(horse), (34, 24, 19, 220), 1)
    line(draw, (x + 31 * facing, y + 13, x + 42 * facing, y + 6), rgba((36, 25, 18)), 3)
    for i, leg in enumerate((-22, -7, 8, 23)):
        step = int(math.sin(phase + i) * 4)
        line(draw, (x + leg, y + 42, x + leg + step, y + 62), rgba(horse, 240), 4)
        line(draw, (x + leg + step - 3, y + 62, x + leg + step + 4, y + 62), rgba((28, 22, 18)), 2)
    if heavy:
        draw.arc((x - 31, y + 18, x + 31, y + 47), 180, 360, fill=rgba(p["metal"], 210), width=5)
    draw_normal_soldier(draw, "heavy_spear", x - 1 * facing, y - 12, p, phase, attack, seed + 9, facing, 0.82)


def draw_cannon(draw, x, y, p, attack, facing=1):
    draw.rectangle((x - 33, y + 21, x + 19, y + 35), fill=rgba((43, 38, 34)), outline=rgba(p["trim"], 170), width=2)
    ellipse(draw, (x - 29, y + 28, x - 13, y + 44), rgba(p["metal"], 235), (34, 27, 20, 210), 1)
    ellipse(draw, (x + 5, y + 28, x + 21, y + 44), rgba(p["metal"], 235), (34, 27, 20, 210), 1)
    line(draw, (x - 6, y + 20, x + 41 * facing, y + 8 - int(attack * 5)), rgba(p["metal"]), 9)
    line(draw, (x - 8, y + 18, x + 38 * facing, y + 6 - int(attack * 5)), rgba(lighten(p["metal"], 35), 150), 3)
    if attack > 0.5:
        fx = x + 48 * facing
        ellipse(draw, (fx - 10, y - 5, fx + 16, y + 21), (255, 194, 78, 145))
        draw.polygon([(fx, y - 12), (fx + 23 * facing, y + 6), (fx, y + 25)], fill=(255, 220, 110, 170))


def role_offsets(role):
    if role in ("cavalry", "heavy_cavalry"):
        return [(-13, -3), (15, 8)]
    if role == "artillery":
        return []
    if role == "skirmisher":
        return [(-17, 6), (14, -5)]
    if role in ("brute", "heavy_brute"):
        return [(-13, 3), (17, -6)]
    if role in ("archer", "heavy_archer", "musket"):
        return [(-14, 5), (16, -7)]
    return [(-15, 5), (15, -7)]


def render_unit(unit_id, role, family, anim, frame):
    p = PALETTES[family]
    phase = frame * math.pi / 2.0
    attack = [0.0, 0.42, 1.0, 0.16][frame % 4] if anim == "attack" else 0.0
    hit_flash = [0, 115, 75, 0][frame % 4] if anim == "hit" else 0
    bob = math.sin(phase) * (3.0 if anim == "move" else 0.8)
    if anim == "attack":
        bob -= attack * 2.0
    if anim == "hit":
        bob += [0, -4, 3, 0][frame % 4]

    img = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img, "RGBA")
    draw_compact_base(draw, p, phase, hit_flash)

    facing = 1
    if role in ("cavalry", "heavy_cavalry"):
        draw_horseman(draw, 62, 42 + int(bob), p, phase, role == "heavy_cavalry", attack, stable_hash(unit_id), facing)
        if family in ("imperial", "believer"):
            draw_small_flag(draw, p, 26, 23 + int(bob * 0.35), phase, attack * 0.65)
    elif role == "artillery":
        draw_cannon(draw, 61, 61 + int(bob), p, attack, facing)
        draw_normal_soldier(draw, "infantry", 43, 39 + int(bob), p, phase, 0, stable_hash(unit_id), facing, 0.82)
        draw_normal_soldier(draw, "infantry", 84, 39 + int(bob), p, phase + 0.8, 0, stable_hash(unit_id) + 3, facing, 0.82)
    else:
        scale = 1.08
        if role in ("brute", "heavy_brute", "heavy_infantry", "heavy_spear"):
            scale = 1.12
        if role in ("archer", "heavy_archer", "musket"):
            scale = 1.05
        draw_normal_soldier(draw, role, 64, 34 + int(bob), p, phase, attack, stable_hash(unit_id), facing, scale)
        if unit_id in ("leader_guard", "imperial_halberdiers", "believer_elites"):
            draw_small_flag(draw, p, 25, 24 + int(bob * 0.35), phase, attack * 0.5)

    if anim == "attack" and attack > 0.55 and role not in ("musket", "artillery", "archer", "heavy_archer"):
        draw.arc((70, 18, 126, 92), -35, 55, fill=(255, 225, 116, 145), width=5)
    if anim == "hit":
        for i in range(4):
            x = 27 + i * 20 + (frame % 2) * 5
            y = 38 + (i % 2) * 16
            line(draw, (x, y, x + 8, y - 8), (238, 204, 112, 160), 2)
    img = img.filter(ImageFilter.GaussianBlur(0.1))
    if anim == "hit" and frame in (1, 2):
        img = img.rotate(-5 if frame == 1 else 4, resample=Image.Resampling.BICUBIC, center=(64, 94))
    return img


def draw_terrain_icon(kind, size=80):
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(img, "RGBA")
    if kind == "mountain":
        for x, h, c in [(18, 34, (167, 166, 160)), (38, 43, (142, 143, 143)), (56, 27, (190, 184, 170))]:
            d.polygon([(x - 18, 62), (x, 62 - h), (x + 21, 62)], fill=rgba(c, 235), outline=(76, 72, 68, 150))
            d.polygon([(x, 62 - h), (x + 7, 62 - h + 15), (x - 5, 62 - h + 14)], fill=(235, 235, 228, 190))
    elif kind == "forest":
        for x, y, s in [(25, 45, 20), (42, 36, 25), (56, 48, 19), (31, 55, 18)]:
            d.rectangle((x - 3, y, x + 3, y + 17), fill=(73, 55, 34, 220))
            d.polygon([(x, y - s), (x - s // 2, y + 8), (x + s // 2, y + 8)], fill=(69, 107, 70, 235), outline=(36, 64, 40, 170))
            d.polygon([(x, y - s + 12), (x - s // 2, y + 17), (x + s // 2, y + 17)], fill=(54, 88, 56, 235))
    elif kind == "river":
        d.line((4, 52, 20, 43, 35, 47, 51, 34, 76, 39), fill=(54, 107, 137, 235), width=14)
        d.line((5, 48, 22, 39, 36, 43, 51, 30, 76, 35), fill=(127, 182, 196, 150), width=4)
    elif kind == "city":
        d.rectangle((19, 39, 61, 62), fill=(164, 132, 74, 235), outline=(77, 57, 36, 190), width=2)
        d.polygon([(17, 40), (40, 24), (63, 40)], fill=(115, 76, 52, 235), outline=(77, 57, 36, 180))
        d.rectangle((35, 47, 45, 62), fill=(68, 48, 35, 220))
        d.rectangle((23, 47, 31, 54), fill=(212, 189, 126, 190))
        d.rectangle((49, 47, 57, 54), fill=(212, 189, 126, 190))
    elif kind == "objective":
        d.ellipse((16, 18, 64, 66), fill=(205, 51, 45, 230), outline=(244, 219, 134, 240), width=4)
        d.rectangle((38, 25, 43, 58), fill=(235, 224, 180, 245))
        d.polygon([(44, 28), (62, 34), (44, 42)], fill=(246, 221, 118, 235))
    else:
        d.ellipse((13, 49, 67, 63), fill=(0, 0, 0, 45))
        for x, y in [(24, 44), (40, 39), (55, 47)]:
            d.polygon([(x - 15, y + 13), (x, y - 6), (x + 18, y + 13)], fill=(196, 181, 128, 210), outline=(132, 118, 82, 130))
    return img.filter(ImageFilter.GaussianBlur(0.08))


def make_map_scene(kind):
    w, h = 1280, 720
    img = Image.new("RGB", (w, h), (67, 103, 132))
    d = ImageDraw.Draw(img, "RGBA")
    rng = random.Random(kind)
    for _ in range(2600):
        x = rng.randrange(w)
        y = rng.randrange(h)
        base = (255, 255, 255, rng.randrange(6, 18)) if y < h * 0.45 else (20, 18, 15, rng.randrange(5, 14))
        d.point((x, y), fill=base)
    land = [
        (0, 220), (120, 185), (270, 210), (420, 155), (620, 190),
        (760, 130), (920, 185), (1010, 260), (960, 382), (1110, 500),
        (1010, 650), (780, 594), (590, 650), (410, 564), (250, 640),
        (40, 572), (0, 530),
    ]
    coast = [(x, y + (15 if kind == "battlefield" else 0)) for x, y in land]
    d.line(coast + [coast[0]], fill=(81, 144, 162, 255), width=32, joint="curve")
    d.line(coast + [coast[0]], fill=(34, 67, 84, 255), width=8, joint="curve")
    d.polygon(coast, fill=(139, 134, 112, 255), outline=(63, 62, 58, 180))
    d.polygon([(0, 250), (270, 214), (505, 225), (510, 720), (0, 720)], fill=(165, 153, 122, 215))
    d.polygon([(470, 188), (820, 146), (1010, 268), (975, 432), (720, 458), (540, 356)], fill=(152, 154, 156, 180))
    d.polygon([(830, 420), (1030, 485), (975, 665), (710, 590), (692, 474)], fill=(112, 145, 82, 170))
    for x, y, s in [(402, 395, 65), (482, 410, 82), (565, 385, 60), (622, 420, 70)]:
        d.polygon([(x - s, y + s // 2), (x, y - s), (x + s, y + s // 2)], fill=(144, 146, 148, 210), outline=(80, 82, 88, 100))
        d.polygon([(x, y - s), (x + s // 4, y - s // 3), (x - s // 5, y - s // 4)], fill=(231, 230, 224, 160))
    for x, y in [(145, 325), (207, 302), (1020, 538), (950, 562), (245, 485), (880, 506)]:
        d.line((x - 32, y, x + 36, y - 10), fill=(86, 117, 70, 210), width=6)
        d.line((x, y - 24, x, y + 26), fill=(86, 117, 70, 210), width=5)
        d.line((x + 18, y - 14, x + 18, y + 20), fill=(70, 101, 62, 210), width=4)
    for x, y in [(260, 390), (338, 500), (760, 520), (860, 325), (1030, 312)]:
        d.rectangle((x - 35, y - 12, x + 35, y + 25), fill=(151, 115, 70, 210), outline=(77, 56, 36, 180), width=3)
        d.polygon([(x - 42, y - 12), (x, y - 40), (x + 42, y - 12)], fill=(99, 70, 50, 230), outline=(77, 56, 36, 180))
    for x, y in [(168, 446), (222, 520), (700, 640), (840, 596), (1092, 612)]:
        d.polygon([(x - 38, y + 18), (x, y - 9), (x + 44, y + 16)], fill=(210, 195, 140, 150), outline=(158, 138, 94, 90))
    for x, y in [(600, 280), (720, 270), (790, 300), (660, 330)]:
        d.polygon([(x - 34, y - 12), (x + 20, y - 18), (x + 40, y + 18), (x - 20, y + 28)], fill=(121, 151, 78, 150), outline=(76, 91, 60, 100))
    for y in [145, 268, 620]:
        d.arc((80, y - 40, 360, y + 40), 200, 340, fill=(128, 184, 199, 80), width=3)
        d.arc((120, y - 20, 300, y + 30), 205, 335, fill=(128, 184, 199, 60), width=2)
    vignette = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    vd = ImageDraw.Draw(vignette, "RGBA")
    for i in range(90):
        alpha = int(i * 1.3)
        vd.rectangle((i, i, w - i, h - i), outline=(0, 0, 0, alpha), width=1)
    return Image.alpha_composite(img.convert("RGBA"), vignette).convert("RGB")


def save_frames():
    OUT_ROOT.mkdir(parents=True, exist_ok=True)
    manifest_units = []
    for unit_id, role, family in UNITS:
        unit_dir = OUT_ROOT / unit_id
        unit_dir.mkdir(parents=True, exist_ok=True)
        for anim in ("idle", "move", "attack", "hit"):
            frames = 2 if anim == "idle" else 4
            for frame in range(frames):
                render_unit(unit_id, role, family, anim, frame).save(unit_dir / f"{anim}_{frame}.png")
        manifest_units.append(
            {
                "id": unit_id,
                "role": role,
                "roleDisplay": ROLE_DISPLAY[role],
                "family": family,
                "asset": f"Art/BattleUnits/{unit_id}",
                "idleFrames": 2,
                "moveFrames": 4,
                "attackFrames": 4,
                "hitFrames": 4,
            }
        )
    MANIFEST.write_text(
        json.dumps(
            {
                "generatedAt": datetime.now().isoformat(timespec="seconds"),
                "style": STYLE_LABEL,
                "units": manifest_units,
            },
            ensure_ascii=False,
            indent=2,
        )
        + "\n",
        encoding="utf-8",
    )


def save_terrain_assets():
    TERRAIN_ROOT.mkdir(parents=True, exist_ok=True)
    for kind in ("plain", "mountain", "forest", "river", "city", "objective"):
        draw_terrain_icon(kind).save(TERRAIN_ROOT / f"terrain_{kind}.png")
    for scene in ("battlefield", "strategy"):
        make_map_scene(scene).save(SCENE_ROOT / f"scene_{scene}.png")


def save_preview():
    rows = []
    for unit_id, role, family in UNITS:
        row = Image.new("RGBA", (384, 132), (31, 29, 26, 255))
        d = ImageDraw.Draw(row, "RGBA")
        d.text((10, 8), f"{unit_id} / {ROLE_DISPLAY[role]}", fill=(234, 221, 188, 255))
        for i, anim in enumerate(("idle", "move", "attack", "hit")):
            frame = 1 if anim == "hit" else 0 if anim == "idle" else min(i, 3)
            piece = render_unit(unit_id, role, family, anim, frame).resize((86, 86), Image.Resampling.LANCZOS)
            row.alpha_composite(piece, (6 + i * 92, 28))
            d.text((20 + i * 92, 114), anim, fill=(218, 202, 166, 255))
        rows.append(row.convert("RGB"))
    preview = Image.new("RGB", (768, math.ceil(len(rows) / 2) * 132), (20, 18, 16))
    for idx, row in enumerate(rows):
        preview.paste(row, ((idx % 2) * 384, (idx // 2) * 132))
    PREVIEW.parent.mkdir(parents=True, exist_ok=True)
    preview.save(PREVIEW)

    canvas = make_map_scene("battlefield").resize((960, 540), Image.Resampling.LANCZOS).convert("RGBA")
    d = ImageDraw.Draw(canvas, "RGBA")
    for x, y, kind in [(160, 250, "city"), (290, 360, "mountain"), (560, 225, "forest"), (690, 355, "river"), (430, 310, "objective")]:
        canvas.alpha_composite(draw_terrain_icon(kind, 72), (x, y))
    for x, y, unit in [(212, 260, UNITS[0]), (650, 288, UNITS[5]), (532, 202, UNITS[15])]:
        unit_img = render_unit(unit[0], unit[1], unit[2], "idle", 0).resize((92, 92), Image.Resampling.LANCZOS)
        canvas.alpha_composite(unit_img, (x, y))
    d.text((24, 24), "normal-proportion battle unit preview", fill=(244, 231, 197, 255))
    canvas.convert("RGB").save(MAP_PREVIEW)


def main():
    save_frames()
    save_terrain_assets()
    save_preview()
    print(f"Generated {len(UNITS)} battle unit animation sets at {OUT_ROOT}")
    print(f"Generated terrain icons at {TERRAIN_ROOT}")
    print(f"Updated map scenes at {SCENE_ROOT}")
    print(f"Preview: {PREVIEW}")
    print(f"Map preview: {MAP_PREVIEW}")


if __name__ == "__main__":
    main()
