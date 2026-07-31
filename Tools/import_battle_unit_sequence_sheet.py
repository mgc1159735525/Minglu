from __future__ import annotations

import argparse
import json
import shutil
import subprocess
import sys
from pathlib import Path


PROJECT_ROOT = Path(__file__).resolve().parents[1]
SOURCE_SHEET_ROOT = PROJECT_ROOT / "DataTables" / "battle_unit_sequence_sources"
GENERATOR = PROJECT_ROOT / "Tools" / "generate_battle_unit_sprites.py"
ANIMS = ("idle", "move", "attack", "hit", "recover", "defeat")


def parse_args():
    parser = argparse.ArgumentParser(
        description="Import painted full-frame battle unit animation source sheets and regenerate Unity frames."
    )
    parser.add_argument("--sheet", required=True, help="Source sprite sheet path.")
    parser.add_argument(
        "--unit",
        action="append",
        required=True,
        help="Target common unit id. Pass more than once to reuse the same sheet for multiple units.",
    )
    parser.add_argument(
        "--anim",
        choices=ANIMS,
        help="Import the sheet as one horizontal animation strip, e.g. move.png with 12 painted frames.",
    )
    parser.add_argument("--columns", type=int, help="Frame columns for an animation grid source.")
    parser.add_argument("--rows", type=int, help="Frame rows for an animation grid source.")
    parser.add_argument(
        "--no-regenerate",
        action="store_true",
        help="Only copy the source sheet. Do not regenerate Assets/Resources/Art/BattleUnits.",
    )
    return parser.parse_args()


def main():
    args = parse_args()
    sheet = Path(args.sheet).resolve()
    if not sheet.exists():
        raise FileNotFoundError(sheet)

    SOURCE_SHEET_ROOT.mkdir(parents=True, exist_ok=True)
    for unit_id in args.unit:
        if not unit_id.replace("_", "").isalnum():
            raise ValueError(f"Unsafe unit id: {unit_id}")
        if args.anim:
            unit_source_dir = SOURCE_SHEET_ROOT / unit_id
            unit_source_dir.mkdir(parents=True, exist_ok=True)
            target = unit_source_dir / f"{args.anim}.png"
            if args.columns or args.rows:
                layout = {
                    "columns": args.columns or 1,
                    "rows": args.rows or 1,
                    "note": "Full-frame painted animation grid. Cells are read left-to-right, top-to-bottom.",
                }
                (unit_source_dir / f"{args.anim}.json").write_text(
                    json.dumps(layout, ensure_ascii=False, indent=2) + "\n",
                    encoding="utf-8",
                )
        else:
            target = SOURCE_SHEET_ROOT / f"{unit_id}.png"
            if args.columns or args.rows:
                layout = {
                    "columns": args.columns or 12,
                    "rows": ["idle", "move", "attack", "hit", "recover", "defeat"],
                    "note": "Full-frame painted unit sheet. Rows are actions; columns are frames read left-to-right.",
                }
                (SOURCE_SHEET_ROOT / f"{unit_id}.json").write_text(
                    json.dumps(layout, ensure_ascii=False, indent=2) + "\n",
                    encoding="utf-8",
                )
        shutil.copy2(sheet, target)
        print(f"Imported source sheet: {target}")

    if not args.no_regenerate:
        subprocess.run([sys.executable, str(GENERATOR)], cwd=str(PROJECT_ROOT), check=True)


if __name__ == "__main__":
    main()
