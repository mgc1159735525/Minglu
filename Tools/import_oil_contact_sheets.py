from __future__ import annotations

import argparse
import json
import shutil
from datetime import datetime
from pathlib import Path

from PIL import Image, ImageOps


PROJECT_ROOT = Path(__file__).resolve().parents[1]
STORY_PATH = PROJECT_ROOT / "Assets" / "Resources" / "MingLuStoryData.json"
PORTRAIT_DIR = PROJECT_ROOT / "Assets" / "Resources" / "Art" / "Portraits"
SOURCE_DIR = PROJECT_ROOT / "Assets" / "ArtSource" / "OilPortraitSheets"
MANIFEST_PATH = PROJECT_ROOT / "Assets" / "Resources" / "Art" / "Manifests" / "art_manifest.json"
BACKUP_ROOT = PROJECT_ROOT / "ArtBackups"
TARGET_SIZE = (512, 768)


RANGES = [
    ("returning_royalists", 1, 15, 5, 3),
    ("young_army", 16, 30, 5, 3),
    ("native_frontier", 31, 45, 5, 3),
    ("liberals", 46, 60, 5, 3),
    ("legalists", 61, 75, 5, 3),
    ("cross_faction_npc", 76, 85, 5, 2),
]


def crop_cell(sheet: Image.Image, index: int, cols: int, rows: int) -> Image.Image:
    w, h = sheet.size
    cell_w = w / cols
    cell_h = h / rows
    col = index % cols
    row = index // cols
    x0 = int(round(col * cell_w))
    y0 = int(round(row * cell_h))
    x1 = int(round((col + 1) * cell_w))
    y1 = int(round((row + 1) * cell_h))

    # Remove thin grid borders and crop to a portrait-friendly 2:3 ratio.
    pad_x = max(2, int((x1 - x0) * 0.01))
    pad_y = max(2, int((y1 - y0) * 0.01))
    x0 += pad_x
    y0 += pad_y
    x1 -= pad_x
    y1 -= pad_y

    cw = x1 - x0
    ch = y1 - y0
    target_ratio = TARGET_SIZE[0] / TARGET_SIZE[1]
    if cw / ch > target_ratio:
        new_w = int(ch * target_ratio)
        shift = (cw - new_w) // 2
        x0 += shift
        x1 = x0 + new_w
    else:
        new_h = int(cw / target_ratio)
        shift = max(0, (ch - new_h) // 3)
        y0 += shift
        y1 = y0 + new_h

    cropped = sheet.crop((x0, y0, x1, y1)).convert("RGBA")
    return ImageOps.fit(cropped, TARGET_SIZE, method=Image.Resampling.LANCZOS, centering=(0.5, 0.42))


def backup_portraits() -> Path:
    stamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    dst = BACKUP_ROOT / f"contact_sheet_import_backup_{stamp}"
    dst.mkdir(parents=True, exist_ok=True)
    for path in PORTRAIT_DIR.glob("portrait_*.png"):
        rel = path.relative_to(PROJECT_ROOT)
        out = dst / rel
        out.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(path, out)
    player = PORTRAIT_DIR / "portrait_player_mo_mingyuan.png"
    if player.exists():
        out = dst / player.relative_to(PROJECT_ROOT)
        out.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(player, out)
    return dst


def update_manifest(source_paths: dict[str, str]) -> None:
    if not MANIFEST_PATH.exists():
        return
    data = json.loads(MANIFEST_PATH.read_text(encoding="utf-8"))
    data["generatedAt"] = datetime.now().isoformat(timespec="seconds")
    data["style"] = "古典军政油画肖像；深色明暗、长发束髻/高髻、中式立领、云纹、盘扣、甲片、绶带、玉带与勋章的架空郑明风格"
    data["portraitPolicy"] = {
        "hair": "所有中式角色避免剪发；男性长发束髻或冠髻，女性高髻或发髻。",
        "clothing": "军官、军校生穿中式军政礼服；女角色如果不是军官，则穿同时代中式常服、书院服、商家服或宫廷礼服。",
        "rendering": "写实油画半身肖像，低饱和深背景，强明暗，不使用Q版或卡通比例。"
    }
    data["sourceSheets"] = source_paths
    MANIFEST_PATH.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def main() -> None:
    parser = argparse.ArgumentParser()
    for name, *_ in RANGES:
        parser.add_argument(f"--{name}", required=True)
    parser.add_argument("--player-master", default="")
    parser.add_argument("--skip-backup", action="store_true")
    args = parser.parse_args()

    story = json.loads(STORY_PATH.read_text(encoding="utf-8"))
    characters = story.get("characters", [])
    if len(characters) < 85:
        raise RuntimeError(f"Expected at least 85 story characters, found {len(characters)}")

    SOURCE_DIR.mkdir(parents=True, exist_ok=True)
    source_paths: dict[str, str] = {}
    if not args.skip_backup:
        backup = backup_portraits()
        print(f"Backed up current portraits to {backup}")

    for name, start, end, cols, rows in RANGES:
        src = Path(getattr(args, name))
        if not src.exists():
            raise FileNotFoundError(src)
        copied = SOURCE_DIR / f"{name}.png"
        shutil.copy2(src, copied)
        source_paths[name] = str(copied.relative_to(PROJECT_ROOT)).replace("\\", "/")

        sheet = Image.open(src).convert("RGBA")
        expected = end - start + 1
        if cols * rows < expected:
            raise RuntimeError(f"{name} grid {cols}x{rows} cannot hold {expected} portraits")
        for offset, number in enumerate(range(start, end + 1)):
            out = PORTRAIT_DIR / f"portrait_{number:03d}.png"
            crop_cell(sheet, offset, cols, rows).save(out)

    if args.player_master:
        player_master = Path(args.player_master)
        if not player_master.exists():
            raise FileNotFoundError(player_master)
        copied = SOURCE_DIR / "player_mo_mingyuan_master.png"
        shutil.copy2(player_master, copied)
        source_paths["player_mo_mingyuan_master"] = str(copied.relative_to(PROJECT_ROOT)).replace("\\", "/")
        img = Image.open(player_master).convert("RGBA")
        # The player master is square; crop it into the same portrait aspect while keeping the hairpiece visible.
        w, h = img.size
        target_ratio = TARGET_SIZE[0] / TARGET_SIZE[1]
        crop_w = int(h * target_ratio)
        x0 = max(0, (w - crop_w) // 2)
        portrait = img.crop((x0, 0, x0 + crop_w, h))
        ImageOps.fit(portrait, TARGET_SIZE, method=Image.Resampling.LANCZOS, centering=(0.5, 0.36)).save(PORTRAIT_DIR / "portrait_player_mo_mingyuan.png")

    update_manifest(source_paths)
    print("Imported oil portrait contact sheets into project portraits.")


if __name__ == "__main__":
    main()
