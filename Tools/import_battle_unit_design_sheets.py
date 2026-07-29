from __future__ import annotations

import json
from datetime import datetime
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont


PROJECT_ROOT = Path(__file__).resolve().parents[1]
SOURCE_ROOT = PROJECT_ROOT / "DataTables" / "battle_unit_design_sources"
OUT_ROOT = PROJECT_ROOT / "Assets" / "Resources" / "Art" / "BattleUnitDesigns"
MANIFEST = OUT_ROOT / "battle_unit_design_manifest.json"
PREVIEW = PROJECT_ROOT / "DataTables" / "battle_unit_designs_preview.png"

CANVAS_SIZE = 512
CELL_COLUMNS = 4
CELL_ROWS = 3

UNITS = [
    ("design_sheet_a.png", 0, "swordsmen_volunteers", "剑士队", "义勇军", "步兵"),
    ("design_sheet_a.png", 1, "matchlock_volunteers", "火绳枪队", "义勇军", "火枪"),
    ("design_sheet_a.png", 2, "militia_volunteers", "民兵团", "义勇军", "散兵"),
    ("design_sheet_a.png", 3, "outlaw_skirmishers", "亡徒军", "贼徒", "散兵"),
    ("design_sheet_a.png", 4, "imperial_halberdiers", "禁卫长戟队", "禁军", "重枪"),
    ("design_sheet_a.png", 5, "armored_iron_cavalry", "具装铁骑军", "禁军", "重骑"),
    ("design_sheet_a.png", 6, "steel_helmet_heavy_infantry", "钢盔军", "义勇军", "重步"),
    ("design_sheet_a.png", 7, "imperial_longbowmen", "禁军长弓兵", "禁军", "重弓"),
    ("design_sheet_a.png", 8, "sword_guard_corps", "剑卫军团", "义勇军", "步兵"),
    ("design_sheet_a.png", 9, "imperial_axe_guard", "禁军斧卫", "禁军", "重猛"),
    ("design_sheet_a.png", 10, "vanguard_cavalry", "先锋骑军", "义勇军", "骑兵"),
    ("design_sheet_a.png", 11, "solemn_guard_matchlocks", "肃卫火枪队", "义勇军", "火枪"),
    ("design_sheet_b.png", 0, "raiders", "掠杀军", "贼徒", "散兵"),
    ("design_sheet_b.png", 1, "imperial_heavy_guard", "重甲禁卫军", "禁军", "重步"),
    ("design_sheet_b.png", 2, "warhammer_volunteers", "重锤军", "义勇军", "重猛"),
    ("design_sheet_b.png", 3, "imperial_shenji_artillery", "禁军神机队", "禁军", "重器"),
    ("design_sheet_b.png", 4, "zealot_believers", "狂热信众", "信徒", "散兵"),
    ("design_sheet_b.png", 5, "zealot_mob", "狂热暴徒", "信徒", "猛士"),
    ("design_sheet_b.png", 6, "leader_guard", "领袖卫队", "信徒", "重步"),
    ("design_sheet_b.png", 7, "elite_archers", "精锐弓兵队", "义勇军", "弓兵"),
    ("design_sheet_b.png", 8, "bandits", "土匪", "贼徒", "散兵"),
    ("design_sheet_b.png", 9, "great_axe_warriors", "巨斧军", "义勇军", "猛士"),
    ("design_sheet_b.png", 10, "believer_elites", "信徒精锐", "信徒", "步兵"),
]


def font(size: int) -> ImageFont.ImageFont:
    for candidate in (
        Path("C:/Windows/Fonts/msyh.ttc"),
        Path("C:/Windows/Fonts/simhei.ttf"),
        Path("C:/Windows/Fonts/simsun.ttc"),
    ):
        if candidate.exists():
            return ImageFont.truetype(str(candidate), size)
    return ImageFont.load_default()


def remove_chroma_key(img: Image.Image) -> Image.Image:
    img = img.convert("RGBA")
    pixels = []
    for r, g, b, a in img.getdata():
        green_delta = g - max(r, b)
        if g > 165 and green_delta > 68:
            pixels.append((r, g, b, 0))
        elif g > 118 and green_delta > 34:
            edge = min(1.0, (green_delta - 34) / 46.0)
            alpha = int(a * (1.0 - edge))
            pixels.append((r, min(g, max(r, b) + 16), b, alpha))
        else:
            pixels.append((r, g, b, a))
    img.putdata(pixels)
    alpha = img.getchannel("A").filter(ImageFilter.GaussianBlur(0.2))
    img.putalpha(alpha)
    return img


def remove_stray_components(img: Image.Image) -> Image.Image:
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
            components.append(
                {
                    "area": len(points),
                    "points": points,
                    "box": (min_x, min_y, max_x, max_y),
                    "centerBias": abs(((min_x + max_x) * 0.5) - width * 0.5),
                }
            )

    if not components:
        return img

    largest = max(item["area"] for item in components)
    keep = [
        item
        for item in components
        if item["area"] >= max(420, largest * 0.055) or (item["area"] >= max(250, largest * 0.025) and item["centerBias"] < width * 0.22)
    ]
    clean_alpha = Image.new("L", img.size, 0)
    clean_pixels = clean_alpha.load()
    src_alpha = alpha.load()
    for item in keep:
        for x, y in item["points"]:
            clean_pixels[x, y] = src_alpha[x, y]
    result = img.copy()
    result.putalpha(clean_alpha.filter(ImageFilter.GaussianBlur(0.12)))
    return result


def cell_box(sheet: Image.Image, index: int) -> tuple[int, int, int, int]:
    col = index % CELL_COLUMNS
    row = index // CELL_COLUMNS
    left = round(sheet.width * col / CELL_COLUMNS)
    top = round(sheet.height * row / CELL_ROWS)
    right = round(sheet.width * (col + 1) / CELL_COLUMNS)
    bottom = round(sheet.height * (row + 1) / CELL_ROWS)
    return left, top, right, bottom


def trim_and_fit(img: Image.Image) -> Image.Image:
    alpha = img.getchannel("A")
    bbox = alpha.getbbox()
    if bbox is None:
        return Image.new("RGBA", (CANVAS_SIZE, CANVAS_SIZE), (0, 0, 0, 0))
    crop = img.crop(bbox)
    max_w = CANVAS_SIZE - 54
    max_h = CANVAS_SIZE - 44
    scale = min(max_w / crop.width, max_h / crop.height, 1.8)
    resized = crop.resize((round(crop.width * scale), round(crop.height * scale)), Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (CANVAS_SIZE, CANVAS_SIZE), (0, 0, 0, 0))
    x = (CANVAS_SIZE - resized.width) // 2
    y = CANVAS_SIZE - resized.height - 18
    canvas.alpha_composite(resized, (x, y))
    return canvas


def import_designs() -> list[dict]:
    OUT_ROOT.mkdir(parents=True, exist_ok=True)
    imported = []
    sheets: dict[str, Image.Image] = {}

    for sheet_file, cell_index, unit_id, name, keyword, role in UNITS:
        sheet_path = SOURCE_ROOT / sheet_file
        if not sheet_path.exists():
            raise FileNotFoundError(sheet_path)
        if sheet_file not in sheets:
            sheets[sheet_file] = Image.open(sheet_path).convert("RGBA")
        sheet = sheets[sheet_file]
        raw = sheet.crop(cell_box(sheet, cell_index))
        art = trim_and_fit(remove_stray_components(remove_chroma_key(raw)))
        output = OUT_ROOT / f"{unit_id}.png"
        art.save(output)
        imported.append(
            {
                "id": unit_id,
                "name": name,
                "keyword": keyword,
                "role": role,
                "asset": f"Art/BattleUnitDesigns/{unit_id}",
                "sourceSheet": f"DataTables/battle_unit_design_sources/{sheet_file}",
                "sourceCell": cell_index,
            }
        )

    MANIFEST.write_text(
        json.dumps(
            {
                "generatedAt": datetime.now().isoformat(timespec="seconds"),
                "style": "painted Chinese-topknot tactical miniature standing unit designs",
                "sourceLayout": {"columns": CELL_COLUMNS, "rows": CELL_ROWS},
                "units": imported,
            },
            ensure_ascii=False,
            indent=2,
        )
        + "\n",
        encoding="utf-8",
    )
    save_preview(imported)
    return imported


def save_preview(units: list[dict]) -> None:
    cols = 4
    tile_w = 280
    tile_h = 350
    margin = 28
    rows = (len(units) + cols - 1) // cols
    preview = Image.new("RGBA", (margin * 2 + cols * tile_w, margin * 2 + rows * tile_h), (29, 24, 20, 255))
    draw = ImageDraw.Draw(preview, "RGBA")
    title_font = font(24)
    text_font = font(18)
    small_font = font(14)

    for idx, unit in enumerate(units):
        col = idx % cols
        row = idx // cols
        x = margin + col * tile_w
        y = margin + row * tile_h
        draw.rounded_rectangle((x, y, x + tile_w - 18, y + tile_h - 18), radius=10, fill=(42, 34, 27, 255), outline=(184, 135, 68, 210), width=2)
        art = Image.open(OUT_ROOT / f"{unit['id']}.png").convert("RGBA")
        art.thumbnail((220, 218), Image.Resampling.LANCZOS)
        preview.alpha_composite(art, (x + (tile_w - 18 - art.width) // 2, y + 18))
        draw.text((x + 18, y + 250), unit["name"], font=title_font, fill=(237, 199, 103, 255))
        draw.text((x + 18, y + 282), f"{unit['keyword']} / {unit['role']}", font=text_font, fill=(233, 223, 199, 255))
        draw.text((x + 18, y + 310), unit["id"], font=small_font, fill=(166, 151, 128, 255))

    PREVIEW.parent.mkdir(parents=True, exist_ok=True)
    preview.convert("RGB").save(PREVIEW)


def main() -> None:
    imported = import_designs()
    print(f"Imported {len(imported)} battle unit standing designs into {OUT_ROOT}")
    print(f"Preview: {PREVIEW}")


if __name__ == "__main__":
    main()
