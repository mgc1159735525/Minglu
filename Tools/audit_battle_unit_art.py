from __future__ import annotations

import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


PROJECT_ROOT = Path(__file__).resolve().parents[1]
ART_ROOT = PROJECT_ROOT / "Assets" / "Resources" / "Art" / "BattleUnits"
SOURCE_ROOT = PROJECT_ROOT / "DataTables" / "battle_unit_sequence_sources"
CONFIG = PROJECT_ROOT / "Assets" / "Resources" / "Data" / "MingLuGameConfig.json"
OUT_PREVIEW = PROJECT_ROOT / "DataTables" / "battle_unit_runtime_scale_check.png"
OUT_REPORT = PROJECT_ROOT / "DataTables" / "battle_unit_art_audit.json"

FRAME_COUNTS = {
    "idle": 6,
    "move": 12,
    "attack": 8,
    "hit": 8,
    "recover": 8,
    "defeat": 10,
}

SAMPLE_FRAMES = [
    ("idle", 0),
    ("move", 2),
    ("move", 8),
    ("attack", 4),
    ("hit", 3),
    ("recover", 7),
    ("defeat", 9),
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


def source_state(unit_id: str) -> dict[str, bool]:
    unit_dir = SOURCE_ROOT / unit_id
    return {anim: (unit_dir / f"{anim}.png").exists() for anim in FRAME_COUNTS}


def frame_state(unit_id: str) -> dict[str, int]:
    unit_dir = ART_ROOT / unit_id
    return {anim: len(list(unit_dir.glob(f"{anim}_*.png"))) for anim in FRAME_COUNTS}


def runtime_frame_problems(unit_id: str) -> list[str]:
    problems: list[str] = []
    unit_dir = ART_ROOT / unit_id
    for anim, expected in FRAME_COUNTS.items():
        for frame in range(expected):
            path = unit_dir / f"{anim}_{frame}.png"
            if not path.exists():
                problems.append(f"{anim}_{frame}: missing")
                continue
            img = Image.open(path).convert("RGBA")
            alpha = img.getchannel("A")
            bbox = alpha.getbbox()
            if bbox is None:
                problems.append(f"{anim}_{frame}: empty")
                continue
            visible = sum(1 for value in alpha.getdata() if value > 20)
            if visible < 1800:
                problems.append(f"{anim}_{frame}: too few visible pixels ({visible})")
            left, top, right, bottom = bbox
            if left <= 0 or top <= 0 or right >= img.width or bottom >= img.height:
                problems.append(f"{anim}_{frame}: touches canvas edge {bbox}")
    return problems


def has_runtime_frames(unit_id: str) -> bool:
    counts = frame_state(unit_id)
    return all(counts.get(anim) == expected for anim, expected in FRAME_COUNTS.items())


def make_preview(units: list[dict]) -> None:
    scale = 82
    label_h = 38
    cols = len(SAMPLE_FRAMES)
    row_w = 220 + cols * 104
    row_h = label_h + scale + 14
    margin = 18
    canvas = Image.new("RGBA", (row_w + margin * 2, margin * 2 + len(units) * row_h), (24, 20, 17, 255))
    draw = ImageDraw.Draw(canvas, "RGBA")
    title_font = font(16)
    small_font = font(11)

    for row, unit in enumerate(units):
        y = margin + row * row_h
        draw.rectangle((margin, y, row_w + margin, y + row_h - 6), fill=(34, 28, 23, 255))
        name = unit.get("name") or unit["id"]
        draw.text((margin + 8, y + 9), f"{name}\n{unit['id']}", font=title_font, fill=(236, 214, 158, 255))
        for col, (anim, frame) in enumerate(SAMPLE_FRAMES):
            path = ART_ROOT / unit["id"] / f"{anim}_{frame}.png"
            x = margin + 220 + col * 104
            if path.exists():
                img = Image.open(path).convert("RGBA")
                img.thumbnail((scale, scale), Image.Resampling.LANCZOS)
                bg = Image.new("RGBA", (scale, scale), (58, 53, 43, 255))
                bg.alpha_composite(img, ((scale - img.width) // 2, (scale - img.height) // 2))
                canvas.alpha_composite(bg, (x, y + 6))
            draw.text((x, y + scale + 9), f"{anim}_{frame}", font=small_font, fill=(194, 179, 139, 255))

    OUT_PREVIEW.parent.mkdir(parents=True, exist_ok=True)
    canvas.convert("RGB").save(OUT_PREVIEW)


def main() -> None:
    units = load_units()
    report = []
    for unit in units:
        unit_id = unit["id"]
        sources = source_state(unit_id)
        frames = frame_state(unit_id)
        problems = runtime_frame_problems(unit_id)
        report.append(
            {
                "id": unit_id,
                "name": unit.get("name") or unit_id,
                "runtimeFramesComplete": has_runtime_frames(unit_id),
                "runtimeFrameProblems": problems,
                "paintedSourceActions": [anim for anim, exists in sources.items() if exists],
                "missingPaintedSourceActions": [anim for anim, exists in sources.items() if not exists],
                "frameCounts": frames,
            }
        )
    OUT_REPORT.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    make_preview(units)
    print(f"Audit report: {OUT_REPORT}")
    print(f"Runtime-scale preview: {OUT_PREVIEW}")


if __name__ == "__main__":
    main()
