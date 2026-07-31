from __future__ import annotations

import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


PROJECT_ROOT = Path(__file__).resolve().parents[1]
ART_ROOT = PROJECT_ROOT / "Assets" / "Resources" / "Art" / "BattleUnits"
CONFIG = PROJECT_ROOT / "Assets" / "Resources" / "Data" / "MingLuGameConfig.json"
OUT_ROOT = PROJECT_ROOT / "DataTables" / "battle_unit_gif_previews"
INDEX_HTML = OUT_ROOT / "index.html"

ANIMATIONS = [
    ("idle", "待机", 6, 120),
    ("move", "移动", 12, 80),
    ("attack", "攻击", 8, 85),
    ("hit", "受击", 8, 90),
    ("recover", "回复", 8, 95),
    ("defeat", "消灭", 10, 105),
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


def load_units() -> list[dict]:
    data = json.loads(CONFIG.read_text(encoding="utf-8"))
    return data.get("commonUnits") or []


def load_frame(unit_id: str, anim: str, frame: int) -> Image.Image | None:
    path = ART_ROOT / unit_id / f"{anim}_{frame}.png"
    if not path.exists():
        return None
    return Image.open(path).convert("RGBA")


def compose_frame(unit: dict, anim_name: str, anim_label: str, frame_index: int, sprite: Image.Image | None) -> Image.Image:
    canvas = Image.new("RGBA", (420, 300), (25, 21, 18, 255))
    draw = ImageDraw.Draw(canvas, "RGBA")
    title_font = font(22)
    small_font = font(14)

    draw.rounded_rectangle((12, 12, 408, 288), radius=10, fill=(36, 30, 25, 255), outline=(164, 118, 55, 210), width=2)
    draw.text((24, 22), unit.get("name") or unit["id"], font=title_font, fill=(238, 202, 112, 255))
    draw.text((24, 52), f"{unit['id']}  /  {anim_label} {frame_index + 1}", font=small_font, fill=(204, 188, 150, 255))

    if sprite is not None:
        preview = sprite.copy()
        preview.thumbnail((210, 210), Image.Resampling.LANCZOS)
        shadow = Image.new("RGBA", (240, 28), (0, 0, 0, 0))
        shadow_draw = ImageDraw.Draw(shadow, "RGBA")
        shadow_draw.ellipse((20, 5, 220, 24), fill=(0, 0, 0, 95))
        canvas.alpha_composite(shadow, (90, 236))
        x = (420 - preview.width) // 2
        y = 78 + (176 - preview.height) // 2
        canvas.alpha_composite(preview, (x, y))
    else:
        draw.text((150, 145), "MISSING FRAME", font=small_font, fill=(230, 88, 72, 255))

    draw.text((24, 264), "runtime preview from Assets/Resources/Art/BattleUnits", font=small_font, fill=(140, 126, 104, 255))
    return canvas.convert("P", palette=Image.Palette.ADAPTIVE)


def build_unit_gif(unit: dict) -> Path:
    unit_id = unit["id"]
    frames: list[Image.Image] = []
    durations: list[int] = []

    for anim, label, count, duration in ANIMATIONS:
        for frame_index in range(count):
            sprite = load_frame(unit_id, anim, frame_index)
            frames.append(compose_frame(unit, anim, label, frame_index, sprite))
            durations.append(duration)
        if frames:
            frames.extend([frames[-1]] * 3)
            durations.extend([180, 180, 180])

    OUT_ROOT.mkdir(parents=True, exist_ok=True)
    out = OUT_ROOT / f"{unit_id}.gif"
    if not frames:
        raise RuntimeError(f"No frames for {unit_id}")
    frames[0].save(out, save_all=True, append_images=frames[1:], duration=durations, loop=0, disposal=2)
    return out


def build_index(outputs: list[tuple[dict, Path]]) -> None:
    lines = [
        "<!doctype html>",
        "<meta charset=\"utf-8\">",
        "<title>Battle Unit GIF Previews</title>",
        "<style>body{background:#191512;color:#e8d9b4;font-family:Microsoft YaHei,Arial,sans-serif;margin:24px;} .grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(420px,1fr));gap:18px;} .card{background:#241e19;border:1px solid #9f7437;padding:12px;} img{width:100%;height:auto;display:block;} .name{font-size:18px;margin:0 0 8px;color:#f0ca70}.id{font-size:12px;color:#a99a78}</style>",
        "<h1>Battle Unit GIF Previews</h1>",
        "<div class=\"grid\">",
    ]
    for unit, path in outputs:
        lines.append("<div class=\"card\">")
        lines.append(f"<div class=\"name\">{unit.get('name') or unit['id']}</div>")
        lines.append(f"<div class=\"id\">{unit['id']}</div>")
        lines.append(f"<img src=\"{path.name}\" alt=\"{unit['id']}\">")
        lines.append("</div>")
    lines.append("</div>")
    INDEX_HTML.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> None:
    outputs = []
    for unit in load_units():
        out = build_unit_gif(unit)
        outputs.append((unit, out))
        print(out)
    build_index(outputs)
    print(f"Index: {INDEX_HTML}")


if __name__ == "__main__":
    main()
