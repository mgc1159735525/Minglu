from __future__ import annotations

import argparse
import shutil
import subprocess
import sys
from pathlib import Path


PROJECT_ROOT = Path(__file__).resolve().parents[1]
SOURCE_SHEET_ROOT = PROJECT_ROOT / "DataTables" / "battle_unit_sequence_sources"
GENERATOR = PROJECT_ROOT / "Tools" / "generate_battle_unit_sprites.py"


def parse_args():
    parser = argparse.ArgumentParser(
        description="Import a 4-row x 6-column battle unit sprite sheet and regenerate Unity animation frames."
    )
    parser.add_argument("--sheet", required=True, help="Source sprite sheet path.")
    parser.add_argument(
        "--unit",
        action="append",
        required=True,
        help="Target common unit id. Pass more than once to reuse the same sheet for multiple units.",
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
        target = SOURCE_SHEET_ROOT / f"{unit_id}.png"
        shutil.copy2(sheet, target)
        print(f"Imported source sheet: {target}")

    subprocess.run([sys.executable, str(GENERATOR)], cwd=str(PROJECT_ROOT), check=True)


if __name__ == "__main__":
    main()
