from __future__ import annotations

import argparse
import json
import math
import random
import shutil
from datetime import datetime
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageOps


PROJECT_ROOT = Path(__file__).resolve().parents[1]
STORY_PATH = PROJECT_ROOT / "Assets" / "Resources" / "MingLuStoryData.json"
MANIFEST_PATH = PROJECT_ROOT / "Assets" / "Resources" / "Art" / "Manifests" / "art_manifest.json"
PORTRAIT_DIR = PROJECT_ROOT / "Assets" / "Resources" / "Art" / "Portraits"
SCENE_DIR = PROJECT_ROOT / "Assets" / "Resources" / "Art" / "Scenes"
BACKUP_ROOT = PROJECT_ROOT / "ArtBackups"

PORTRAIT_SIZE = (512, 768)
SCENE_SIZE = (1280, 720)


FACTION_PALETTES = {
    "返乡团": {
        "robe": (18, 28, 44),
        "sash": (120, 31, 29),
        "trim": (178, 135, 67),
        "accent": (71, 93, 66),
    },
    "陆军青壮派": {
        "robe": (35, 48, 41),
        "sash": (103, 38, 30),
        "trim": (151, 126, 75),
        "accent": (58, 88, 98),
    },
    "印第安乡党": {
        "robe": (55, 42, 31),
        "sash": (125, 68, 36),
        "trim": (160, 121, 67),
        "accent": (47, 116, 118),
    },
    "自由派": {
        "robe": (25, 51, 60),
        "sash": (132, 61, 50),
        "trim": (188, 154, 92),
        "accent": (84, 115, 122),
    },
    "法治派": {
        "robe": (22, 24, 31),
        "sash": (83, 35, 34),
        "trim": (169, 144, 91),
        "accent": (94, 81, 67),
    },
    "重要NPC（跨派系）": {
        "robe": (42, 28, 48),
        "sash": (126, 33, 39),
        "trim": (198, 155, 79),
        "accent": (74, 94, 86),
    },
}

FEMALE_HINTS = ("夫人", "之妻", "之女", "母亲", "太后", "女", "学姐", "素心", "婉清", "小满", "新芽", "半亩", "花·", "溪·", "月影", "水灵", "鹿灵")
ELDER_HINTS = ("老", "年迈", "老太爷", "长老", "教授", "尚书", "院长", "大使", "总督", "传教士", "父", "母亲", "太后", "皇帝")
OFFICER_HINTS = ("将", "军", "校", "兵", "连长", "千户", "百总", "炮", "骑", "边防", "海盗", "剑客")
SCHOLAR_HINTS = ("教授", "学者", "书生", "院长", "律师", "记者", "主编", "县令", "夫子", "御史", "法官")


def clamp_color(color: tuple[int, int, int], delta: int) -> tuple[int, int, int]:
    return tuple(max(0, min(255, c + delta)) for c in color)


def blend(a: tuple[int, int, int], b: tuple[int, int, int], t: float) -> tuple[int, int, int]:
    return tuple(int(a[i] * (1 - t) + b[i] * t) for i in range(3))


def seeded_rng(*parts: str) -> random.Random:
    seed = "|".join(parts)
    return random.Random(seed)


def add_oil_texture(img: Image.Image, strength: int = 28) -> Image.Image:
    w, h = img.size
    noise = Image.effect_noise((w, h), strength).convert("L")
    colored = ImageOps.colorize(noise, (0, 0, 0), (54, 42, 31))
    img = Image.blend(img.convert("RGB"), colored, 0.12)

    strokes = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(strokes, "RGBA")
    rng = random.Random(w * 1009 + h * 917)
    for _ in range(max(80, (w * h) // 12000)):
        x = rng.randint(-40, w + 40)
        y = rng.randint(-40, h + 40)
        length = rng.randint(35, 150)
        angle = rng.uniform(-0.7, 0.7)
        color = rng.choice([(255, 234, 188, 13), (48, 31, 23, 18), (118, 82, 46, 14)])
        x2 = int(x + math.cos(angle) * length)
        y2 = int(y + math.sin(angle) * length)
        draw.line((x, y, x2, y2), fill=color, width=rng.randint(1, 4))
    strokes = strokes.filter(ImageFilter.GaussianBlur(0.45))
    return Image.alpha_composite(img.convert("RGBA"), strokes)


def portrait_background(rng: random.Random) -> Image.Image:
    w, h = PORTRAIT_SIZE
    base = Image.new("RGB", (w, h), (28, 22, 18))
    px = base.load()
    glow_x = rng.randint(int(w * 0.38), int(w * 0.58))
    glow_y = rng.randint(int(h * 0.22), int(h * 0.34))
    for y in range(h):
        for x in range(w):
            dx = (x - glow_x) / w
            dy = (y - glow_y) / h
            d = min(1.0, math.sqrt(dx * dx * 2.0 + dy * dy * 3.0))
            warm = int(64 * (1.0 - d))
            vignette = int(52 * ((abs(x - w / 2) / (w / 2)) ** 2 + (abs(y - h / 2) / (h / 2)) ** 2) / 2)
            px[x, y] = (
                max(8, 31 + warm - vignette),
                max(7, 24 + int(warm * 0.74) - vignette),
                max(6, 19 + int(warm * 0.48) - vignette),
            )
    return add_oil_texture(base, 34)


def scene_background(rng: random.Random, key: str) -> Image.Image:
    w, h = SCENE_SIZE
    top = (37, 31, 29)
    bottom = (72, 50, 38)
    if key in ("frontier", "battlefield"):
        top, bottom = (38, 39, 38), (86, 67, 46)
    elif key in ("harbor", "strategy"):
        top, bottom = (36, 46, 49), (78, 65, 48)
    elif key in ("library", "council"):
        top, bottom = (35, 28, 25), (85, 61, 43)
    img = Image.new("RGB", (w, h))
    draw = ImageDraw.Draw(img, "RGBA")
    for y in range(h):
        t = y / (h - 1)
        col = blend(top, bottom, t)
        draw.line((0, y, w, y), fill=col)

    glow = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow, "RGBA")
    gd.ellipse((w * 0.16, -h * 0.25, w * 0.84, h * 0.86), fill=(197, 134, 72, 54))
    img = Image.alpha_composite(img.convert("RGBA"), glow)
    return add_oil_texture(img, 32)


def is_female(name: str, identity: str) -> bool:
    text = name + identity
    return any(hint in text for hint in FEMALE_HINTS)


def is_elder(name: str, identity: str) -> bool:
    text = name + identity
    return any(hint in text for hint in ELDER_HINTS)


def role_kind(identity: str) -> str:
    if any(h in identity for h in OFFICER_HINTS):
        return "officer"
    if any(h in identity for h in SCHOLAR_HINTS):
        return "scholar"
    if "商" in identity or "金主" in identity:
        return "merchant"
    return "court"


def draw_cloud_pattern(draw: ImageDraw.ImageDraw, box: tuple[int, int, int, int], color: tuple[int, int, int], alpha: int, rng: random.Random) -> None:
    x0, y0, x1, y1 = box
    for _ in range(12):
        cx = rng.randint(x0, x1)
        cy = rng.randint(y0, y1)
        r = rng.randint(8, 20)
        draw.arc((cx - r, cy - r // 2, cx + r, cy + r // 2), 180, 360, fill=(*color, alpha), width=2)
        draw.arc((cx - r // 2, cy - r, cx + r // 2, cy + r), 270, 90, fill=(*color, alpha), width=2)


def draw_frog_closures(draw: ImageDraw.ImageDraw, cx: int, y0: int, trim: tuple[int, int, int], count: int) -> None:
    for i in range(count):
        y = y0 + i * 42
        draw.line((cx - 26, y, cx + 26, y), fill=(*trim, 170), width=4)
        draw.ellipse((cx - 7, y - 7, cx + 7, y + 7), fill=(*trim, 220), outline=(72, 52, 31, 200), width=1)
        draw.arc((cx - 44, y - 12, cx - 20, y + 12), 260, 90, fill=(*trim, 170), width=3)
        draw.arc((cx + 20, y - 12, cx + 44, y + 12), 90, 280, fill=(*trim, 170), width=3)


def draw_face(draw: ImageDraw.ImageDraw, rng: random.Random, female: bool, elder: bool, skin: tuple[int, int, int], cx: int, cy: int) -> None:
    face_w = rng.randint(118, 136) if not female else rng.randint(110, 126)
    face_h = rng.randint(168, 184) if not elder else rng.randint(172, 194)
    box = (cx - face_w // 2, cy - face_h // 2, cx + face_w // 2, cy + face_h // 2)
    draw.ellipse((box[0] + 8, box[1] + 14, box[2] + 8, box[3] + 20), fill=(15, 10, 8, 95))
    draw.ellipse(box, fill=(*skin, 255), outline=(77, 48, 35, 110), width=2)
    draw.pieslice(box, 95, 275, fill=(*clamp_color(skin, -26), 84))
    draw.ellipse((cx - 42, cy - 22, cx - 18, cy - 13), fill=(34, 24, 20, 230))
    draw.ellipse((cx + 18, cy - 22, cx + 42, cy - 13), fill=(34, 24, 20, 230))
    draw.line((cx - 48, cy - 37, cx - 17, cy - 41), fill=(24, 17, 14, 220), width=4)
    draw.line((cx + 17, cy - 41, cx + 48, cy - 37), fill=(24, 17, 14, 220), width=4)
    draw.line((cx, cy - 20, cx - 8, cy + 30, cx + 8, cy + 35), fill=(*clamp_color(skin, -42), 150), width=3)
    draw.arc((cx - 30, cy + 52, cx + 30, cy + 74), 195, 345, fill=(95, 45, 42, 180), width=3)
    draw.ellipse((cx - 46, cy - 3, cx - 28, cy + 14), fill=(*clamp_color(skin, 20), 48))
    draw.ellipse((cx + 28, cy - 3, cx + 46, cy + 14), fill=(*clamp_color(skin, 20), 42))
    if elder:
        for off in (-24, 0, 24):
            draw.arc((cx - 42, cy + off, cx + 42, cy + off + 38), 200, 340, fill=(91, 61, 50, 120), width=1)
        draw.arc((cx - 46, cy + 70, cx + 46, cy + 112), 190, 350, fill=(34, 27, 24, 190), width=5)


def draw_hair(draw: ImageDraw.ImageDraw, rng: random.Random, female: bool, elder: bool, cx: int, cy: int) -> None:
    hair = (13, 12, 12) if not elder else (44, 41, 38)
    draw.ellipse((cx - 76, cy - 124, cx + 76, cy - 22), fill=(*hair, 255))
    draw.polygon([(cx - 78, cy - 65), (cx - 115, cy + 118), (cx - 62, cy + 132), (cx - 48, cy - 10)], fill=(*hair, 210))
    draw.polygon([(cx + 78, cy - 65), (cx + 112, cy + 118), (cx + 62, cy + 132), (cx + 48, cy - 10)], fill=(*hair, 210))
    if female:
        draw.ellipse((cx - 45, cy - 180, cx + 45, cy - 112), fill=(*hair, 255))
        draw.ellipse((cx - 30, cy - 194, cx + 30, cy - 140), fill=(*hair, 255))
        draw.line((cx - 56, cy - 151, cx + 58, cy - 139), fill=(181, 138, 70, 230), width=4)
        draw.line((cx - 76, cy - 134, cx + 80, cy - 162), fill=(181, 138, 70, 230), width=3)
    else:
        draw.ellipse((cx - 37, cy - 182, cx + 37, cy - 118), fill=(*hair, 255))
        draw.rectangle((cx - 31, cy - 176, cx + 31, cy - 137), fill=(24, 22, 23, 255), outline=(127, 95, 53, 190), width=2)
        draw.line((cx - 50, cy - 154, cx + 50, cy - 154), fill=(160, 120, 62, 210), width=4)
    for i in range(12):
        x = cx + rng.randint(-70, 70)
        draw.line((x, cy - 118, x + rng.randint(-15, 15), cy + rng.randint(-24, 72)), fill=(75, 68, 60, 80), width=1)


def draw_costume(draw: ImageDraw.ImageDraw, rng: random.Random, palette: dict[str, tuple[int, int, int]], kind: str, faction: str, female: bool, cx: int) -> None:
    robe = palette["robe"]
    sash = palette["sash"]
    trim = palette["trim"]
    accent = palette["accent"]
    draw.polygon([(118, 360), (394, 360), (474, 768), (38, 768)], fill=(*clamp_color(robe, -6), 255))
    draw.polygon([(165, 345), (347, 345), (395, 768), (118, 768)], fill=(*robe, 255))
    draw.polygon([(194, 342), (256, 420), (318, 342), (338, 400), (256, 462), (174, 400)], fill=(224, 213, 185, 238))
    draw.polygon([(161, 345), (252, 430), (231, 482), (134, 372)], fill=(*clamp_color(robe, 12), 255))
    draw.polygon([(351, 345), (260, 430), (281, 482), (378, 372)], fill=(*clamp_color(robe, 8), 255))
    draw.line((160, 346, 252, 431), fill=(*trim, 200), width=4)
    draw.line((352, 346, 260, 431), fill=(*trim, 200), width=4)
    draw.polygon([(120, 405), (166, 376), (403, 768), (345, 768)], fill=(*sash, 235))
    draw.line((154, 388, 388, 768), fill=(*trim, 160), width=5)
    draw_cloud_pattern(draw, (150, 430, 360, 720), trim, 58, rng)
    draw_frog_closures(draw, cx + 18, 440, trim, 5)
    if kind == "officer":
        for side in (-1, 1):
            sx = cx + side * 132
            draw.pieslice((sx - 70, 362, sx + 70, 458), 180 if side < 0 else 0, 360 if side < 0 else 180, fill=(*clamp_color(trim, -12), 210))
            for row in range(4):
                for col in range(7 - row):
                    x = sx - side * (10 + col * 13)
                    y = 390 + row * 13
                    draw.ellipse((x - 5, y - 4, x + 5, y + 5), fill=(*clamp_color(trim, 10), 170))
    elif kind == "scholar":
        draw.rectangle((180, 498, 332, 614), fill=(214, 199, 156, 72), outline=(*trim, 120), width=2)
        draw.line((200, 533, 312, 533), fill=(*trim, 80), width=2)
    elif kind == "merchant":
        draw.ellipse((214, 545, 298, 626), fill=(*accent, 135), outline=(*trim, 180), width=4)
    else:
        draw.ellipse((226, 548, 286, 608), fill=(*accent, 155), outline=(*trim, 200), width=4)
    if "印第安" in faction:
        for y in range(486, 724, 26):
            draw.line((100, y, 154, y + 13, 208, y), fill=(*accent, 155), width=3)
    if female:
        draw.arc((177, 430, 335, 560), 30, 150, fill=(*trim, 150), width=3)


def generate_portrait(character: dict, index: int) -> Image.Image:
    name = character.get("name", f"portrait_{index:03d}")
    faction = character.get("faction", "")
    identity = character.get("identity", "")
    rng = seeded_rng(name, faction, identity)
    female = is_female(name, identity)
    elder = is_elder(name, identity)
    kind = role_kind(identity)
    palette = FACTION_PALETTES.get(faction, FACTION_PALETTES["重要NPC（跨派系）"])

    img = portrait_background(rng)
    layer = Image.new("RGBA", PORTRAIT_SIZE, (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer, "RGBA")

    cx = 256 + rng.randint(-10, 10)
    face_cy = 245 + rng.randint(-12, 16)
    draw_costume(draw, rng, palette, kind, faction, female, cx)
    draw_hair(draw, rng, female, elder, cx, face_cy)
    skin_base = (184 + rng.randint(-12, 16), 128 + rng.randint(-10, 12), 94 + rng.randint(-10, 12))
    if "印第安" in faction:
        skin_base = (153 + rng.randint(-12, 18), 101 + rng.randint(-10, 12), 73 + rng.randint(-8, 12))
    if "英国" in name or "西班牙" in name or "法国" in name:
        skin_base = (198 + rng.randint(-10, 10), 145 + rng.randint(-10, 10), 112 + rng.randint(-8, 8))
    draw_face(draw, rng, female, elder, skin_base, cx, face_cy)
    if "皇帝" in name or "太子" in name or "太后" in name:
        trim = palette["trim"]
        draw.rectangle((cx - 78, face_cy - 188, cx + 78, face_cy - 158), fill=(*trim, 225), outline=(75, 49, 22, 180), width=2)
        for x in range(cx - 66, cx + 68, 22):
            draw.line((x, face_cy - 188, x + 8, face_cy - 224), fill=(*trim, 170), width=3)
    if "海盗" in identity or "独眼" in name:
        draw.polygon([(cx - 56, face_cy - 26), (cx - 14, face_cy - 38), (cx - 17, face_cy - 16), (cx - 56, face_cy - 9)], fill=(18, 15, 13, 230))

    layer = layer.filter(ImageFilter.GaussianBlur(0.25))
    img = Image.alpha_composite(img, layer)

    shade = Image.new("RGBA", PORTRAIT_SIZE, (0, 0, 0, 0))
    sd = ImageDraw.Draw(shade, "RGBA")
    sd.rectangle((0, 0, 185, 768), fill=(0, 0, 0, 38))
    sd.rectangle((420, 0, 512, 768), fill=(0, 0, 0, 54))
    sd.ellipse((105, 112, 440, 642), outline=(236, 182, 104, 12), width=2)
    img = Image.alpha_composite(img, shade)
    return img.convert("RGBA")


def draw_scene_architecture(draw: ImageDraw.ImageDraw, rng: random.Random, key: str) -> None:
    w, h = SCENE_SIZE
    trim = (188, 142, 78)
    dark = (30, 24, 22)
    red = (112, 39, 35)
    stone = (105, 86, 68)
    if key == "strategy":
        draw.rectangle((180, 110, 1100, 590), fill=(173, 148, 96, 205), outline=(92, 68, 43, 210), width=6)
        for _ in range(18):
            x = rng.randint(230, 1040)
            y = rng.randint(170, 520)
            draw.line((x, y, x + rng.randint(-90, 100), y + rng.randint(-48, 50)), fill=(58, 73, 66, 120), width=rng.randint(3, 7))
        draw.line((250, 455, 470, 390, 710, 430, 930, 330), fill=(54, 92, 104, 180), width=7)
        return
    if key == "battlefield":
        draw.rectangle((0, 500, w, h), fill=(59, 53, 40, 185))
        for x in range(170, 1120, 180):
            draw.line((x, 190, x, 530), fill=(52, 31, 22, 230), width=7)
            draw.polygon([(x, 195), (x + 82, 225), (x, 258)], fill=(112, 33, 31, 205), outline=(*trim, 150))
        draw.rectangle((170, 505, 1100, 545), fill=(31, 27, 24, 120))
        return
    if key == "frontier":
        draw.polygon([(0, 480), (260, 220), (510, 500)], fill=(69, 72, 57, 135))
        draw.polygon([(350, 500), (740, 190), (1140, 520)], fill=(58, 63, 57, 135))
        draw.rectangle((370, 370, 910, 585), fill=(74, 52, 36, 210), outline=(*trim, 120), width=4)
        draw.polygon([(330, 370), (640, 285), (950, 370)], fill=(85, 35, 31, 225), outline=(*trim, 150))
        return
    if key == "harbor":
        draw.rectangle((0, 470, w, h), fill=(37, 57, 61, 170))
        for x in (300, 680, 945):
            draw.polygon([(x - 130, 505), (x + 130, 505), (x + 75, 565), (x - 80, 565)], fill=(73, 45, 31, 230))
            draw.line((x, 230, x, 505), fill=(48, 34, 25, 220), width=7)
            draw.polygon([(x + 8, 250), (x + 145, 410), (x + 8, 430)], fill=(210, 195, 160, 170), outline=(*trim, 130))
        return
    if key == "library":
        for x in range(100, 1160, 170):
            draw.rectangle((x, 160, x + 115, 560), fill=(64, 36, 25, 215), outline=(*trim, 115), width=3)
            for y in range(200, 520, 52):
                draw.line((x + 8, y, x + 106, y), fill=(*trim, 90), width=2)
        draw.rectangle((450, 470, 830, 570), fill=(75, 43, 31, 230), outline=(*trim, 160), width=4)
        return

    roof_y = 245 if key in ("title", "academy", "palace", "council") else 295
    body_y = roof_y + 78
    draw.rectangle((205, body_y, 1075, 610), fill=(*stone, 220), outline=(*trim, 140), width=5)
    draw.polygon([(155, body_y), (640, roof_y), (1125, body_y)], fill=(*red, 230), outline=(*trim, 170))
    draw.rectangle((560, 420, 720, 610), fill=(*dark, 240), outline=(*trim, 130), width=4)
    for x in (300, 430, 850, 980):
        draw.rectangle((x, 380, x + 74, 496), fill=(34, 46, 51, 160), outline=(*trim, 120), width=3)
    if key in ("palace", "council", "title"):
        draw.ellipse((538, 260, 742, 332), fill=(*trim, 90), outline=(*trim, 170), width=3)
    if key == "academy":
        for x in range(220, 1060, 120):
            draw.line((x, 330, x, 610), fill=(73, 48, 32, 110), width=5)


def generate_scene(key: str) -> Image.Image:
    rng = seeded_rng("scene", key)
    img = scene_background(rng, key)
    layer = Image.new("RGBA", SCENE_SIZE, (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer, "RGBA")
    draw_scene_architecture(draw, rng, key)
    draw.rectangle((0, 0, SCENE_SIZE[0], SCENE_SIZE[1]), outline=(170, 126, 68, 58), width=3)
    layer = layer.filter(ImageFilter.GaussianBlur(0.35))
    img = Image.alpha_composite(img, layer)
    vignette = Image.new("RGBA", SCENE_SIZE, (0, 0, 0, 0))
    vd = ImageDraw.Draw(vignette, "RGBA")
    vd.rectangle((0, 0, SCENE_SIZE[0], 90), fill=(0, 0, 0, 70))
    vd.rectangle((0, 580, SCENE_SIZE[0], SCENE_SIZE[1]), fill=(0, 0, 0, 76))
    vd.rectangle((0, 0, 120, SCENE_SIZE[1]), fill=(0, 0, 0, 52))
    vd.rectangle((1160, 0, SCENE_SIZE[0], SCENE_SIZE[1]), fill=(0, 0, 0, 52))
    return Image.alpha_composite(img, vignette).convert("RGBA")


def crop_player_master(master_path: Path) -> Image.Image:
    img = Image.open(master_path).convert("RGBA")
    w, h = img.size
    target_ratio = PORTRAIT_SIZE[0] / PORTRAIT_SIZE[1]
    crop_w = min(w, int(h * target_ratio))
    crop_h = min(h, int(w / target_ratio))
    x0 = (w - crop_w) // 2
    y0 = max(0, min((h - crop_h) // 2, int(h * 0.04)))
    img = img.crop((x0, y0, x0 + crop_w, y0 + crop_h)).resize(PORTRAIT_SIZE, Image.Resampling.LANCZOS)
    return add_oil_texture(img, 12).convert("RGBA")


def backup_assets(paths: list[Path], label: str) -> Path:
    stamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    dst = BACKUP_ROOT / f"{label}_{stamp}"
    dst.mkdir(parents=True, exist_ok=True)
    for path in paths:
        if path.exists():
            rel = path.relative_to(PROJECT_ROOT)
            out = dst / rel
            out.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(path, out)
    return dst


def load_characters() -> list[dict]:
    data = json.loads(STORY_PATH.read_text(encoding="utf-8"))
    return data.get("characters", [])


def update_manifest() -> None:
    if not MANIFEST_PATH.exists():
        return
    data = json.loads(MANIFEST_PATH.read_text(encoding="utf-8"))
    data["generatedAt"] = datetime.now().isoformat(timespec="seconds")
    data["style"] = "古典军政油画肖像；深色明暗、长发束髻/高髻、中式立领、云纹、盘扣、甲片、绶带、玉带与勋章的架空郑明风格"
    data["portraitPolicy"] = {
        "hair": "所有角色避免剪发；男性长发束髻或冠髻，女性高髻或发髻。",
        "clothing": "所有角色服饰带中国元素；按势力加入立领、云纹、盘扣、补子、甲片、绶带、玉饰、边疆织纹。",
        "rendering": "写实油画半身肖像，低饱和深背景，强明暗，不使用Q版或卡通比例。"
    }
    MANIFEST_PATH.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--player-master", default="", help="Optional generated square master portrait for the player.")
    parser.add_argument("--skip-backup", action="store_true")
    parser.add_argument("--portraits-only", action="store_true")
    args = parser.parse_args()

    characters = load_characters()
    portrait_paths = []
    for character in characters:
        asset = character.get("asset", "")
        if asset.startswith("Art/Portraits/"):
            portrait_paths.append(PROJECT_ROOT / "Assets" / "Resources" / f"{asset}.png")
    player_path = PORTRAIT_DIR / "portrait_player_mo_mingyuan.png"
    scene_paths = list(SCENE_DIR.glob("scene_*.png"))
    paths_to_backup = portrait_paths + [player_path]
    if not args.portraits_only:
        paths_to_backup += scene_paths
    backup = None if args.skip_backup else backup_assets(paths_to_backup, "art_oil_style_backup")

    for index, character in enumerate(characters, start=1):
        asset = character.get("asset", "")
        if not asset.startswith("Art/Portraits/"):
            continue
        out = PROJECT_ROOT / "Assets" / "Resources" / f"{asset}.png"
        out.parent.mkdir(parents=True, exist_ok=True)
        generate_portrait(character, index).save(out)

    master = Path(args.player_master) if args.player_master else None
    if master and master.exists():
        crop_player_master(master).save(player_path)
    else:
        generate_portrait({"name": "夏邑", "faction": "返乡团", "identity": "新京军事学院生", "asset": "Art/Portraits/portrait_player_mo_mingyuan"}, 0).save(player_path)

    if not args.portraits_only:
        for key in ("title", "academy", "library", "palace", "council", "strategy", "battlefield", "frontier", "harbor", "street"):
            generate_scene(key).save(SCENE_DIR / f"scene_{key}.png")

    update_manifest()
    print(f"Generated {len(characters)} story portraits plus player portrait.")
    if not args.portraits_only:
        print("Generated 10 scene backgrounds.")
    if backup:
        print(f"Backed up previous assets to {backup}")


if __name__ == "__main__":
    main()
