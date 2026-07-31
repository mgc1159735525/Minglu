from __future__ import annotations

import csv
import json
import math
import sys
import urllib.request
import zipfile
import zlib
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "Tools" / "_vendor"))

import shapefile  # type: ignore
from PIL import Image, ImageChops, ImageDraw, ImageEnhance, ImageFilter


SOURCE = ROOT / "Assets" / "ArtSource" / "StrategyMap" / "SourceData"
ART_SOURCE = ROOT / "Assets" / "ArtSource" / "StrategyMap"
OUTPUT = ROOT / "Assets" / "Resources" / "Art" / "StrategyMap"
TILES = ART_SOURCE / "Tiles"
EW3_TILES = ART_SOURCE / "Tiles_EW3"
FILL_CONTROL = ROOT / "DataTables" / "csv" / "strategy_map_fills.csv"

WIDTH = 3072
HEIGHT = 1728
LON_MIN = 160.0
LON_MAX = 320.0  # -40 degrees, expressed continuously across the date line
LAT_MIN = 6.0
LAT_MAX = 85.0
TILE_COLUMNS = 4
TILE_ROWS = 3

DOWNLOADS = {
    "relief": "https://naciscdn.org/naturalearth/50m/raster/NE1_50M_SR_W.zip",
    "admin1": "https://naciscdn.org/naturalearth/10m/cultural/ne_10m_admin_1_states_provinces.zip",
    "admin0": "https://naciscdn.org/naturalearth/10m/cultural/ne_10m_admin_0_countries.zip",
}


def lon_to_domain(lon: float) -> float:
    return lon + 360.0 if lon < LON_MIN else lon


def ensure_source_data(include_relief: bool = True) -> None:
    required = {
        "relief": SOURCE / "relief" / "NE1_50M_SR_W" / "NE1_50M_SR_W.tif",
        "admin1": SOURCE / "admin1" / "ne_10m_admin_1_states_provinces.shp",
        "admin0": SOURCE / "admin0" / "ne_10m_admin_0_countries.shp",
    }
    if not include_relief:
        required.pop("relief")
    SOURCE.mkdir(parents=True, exist_ok=True)
    for name, target in required.items():
        if target.exists():
            continue
        archive = SOURCE / f"{name}.zip"
        print(f"Downloading {DOWNLOADS[name]}")
        urllib.request.urlretrieve(DOWNLOADS[name], archive)
        destination = SOURCE / name
        destination.mkdir(parents=True, exist_ok=True)
        with zipfile.ZipFile(archive) as bundle:
            bundle.extractall(destination)
        try:
            archive.unlink()
        except PermissionError:
            # Windows scanners can briefly retain a handle; the ignored cache is harmless.
            pass


def project(lon: float, lat: float) -> tuple[float, float]:
    lon = lon_to_domain(lon)
    x = (lon - LON_MIN) / (LON_MAX - LON_MIN) * WIDTH
    y = (LAT_MAX - lat) / (LAT_MAX - LAT_MIN) * HEIGHT
    return x, y


def unwrap_ring(points: list[tuple[float, float]]) -> list[tuple[float, float]]:
    if not points:
        return []
    out = [points[0]]
    previous = points[0][0]
    for lon, lat in points[1:]:
        while lon - previous > 180:
            lon -= 360
        while lon - previous < -180:
            lon += 360
        out.append((lon, lat))
        previous = lon
    mean_lon = sum(p[0] for p in out) / len(out)
    while mean_lon < LON_MIN:
        out = [(lon + 360, lat) for lon, lat in out]
        mean_lon += 360
    while mean_lon > LON_MAX:
        out = [(lon - 360, lat) for lon, lat in out]
        mean_lon -= 360
    return out


def iter_parts(shape: shapefile.Shape):
    starts = list(shape.parts) + [len(shape.points)]
    for index in range(len(starts) - 1):
        raw = shape.points[starts[index] : starts[index + 1]]
        yield unwrap_ring([(float(lon), float(lat)) for lon, lat in raw])


def part_visible(part: list[tuple[float, float]]) -> bool:
    if not part:
        return False
    xs = [p[0] for p in part]
    ys = [p[1] for p in part]
    return max(xs) >= LON_MIN and min(xs) <= LON_MAX and max(ys) >= LAT_MIN and min(ys) <= LAT_MAX


def projected_part(part: list[tuple[float, float]]) -> list[tuple[float, float]]:
    return [
        (
            (lon - LON_MIN) / (LON_MAX - LON_MIN) * WIDTH,
            (LAT_MAX - lat) / (LAT_MAX - LAT_MIN) * HEIGHT,
        )
        for lon, lat in part
    ]


def crop_relief() -> Image.Image:
    source_path = SOURCE / "relief" / "NE1_50M_SR_W" / "NE1_50M_SR_W.tif"
    with Image.open(source_path) as source:
        source.load()
        sw, sh = source.size

        def sx(lon: float) -> int:
            return round((lon + 180.0) / 360.0 * sw)

        top = round((90.0 - LAT_MAX) / 180.0 * sh)
        bottom = round((90.0 - LAT_MIN) / 180.0 * sh)
        west = source.crop((sx(160.0), top, sw, bottom))
        east = source.crop((0, top, sx(-40.0), bottom))
        stitched = Image.new("RGB", (west.width + east.width, west.height))
        stitched.paste(west, (0, 0))
        stitched.paste(east, (west.width, 0))
    relief = stitched.resize((WIDTH, HEIGHT), Image.Resampling.LANCZOS)
    relief = ImageEnhance.Color(relief).enhance(0.58)
    relief = ImageEnhance.Contrast(relief).enhance(1.08)
    warm = Image.new("RGB", relief.size, (196, 166, 116))
    return Image.blend(relief, warm, 0.12)


def build_boundary_layer(
    shp_path: Path,
    color: tuple[int, int, int, int],
    width: int,
) -> tuple[Image.Image, list[dict[str, object]]]:
    layer = Image.new("RGBA", (WIDTH, HEIGHT), (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    records: list[dict[str, object]] = []
    reader = shapefile.Reader(str(shp_path), encoding="utf-8")
    for shape_record in reader.iterShapeRecords():
        record = shape_record.record.as_dict()
        visible = False
        for part in iter_parts(shape_record.shape):
            if not part_visible(part):
                continue
            visible = True
            pts = projected_part(part)
            if len(pts) >= 2:
                draw.line(pts, fill=color, width=width, joint="curve")
        if visible:
            records.append(record)
    return layer, records


def record_value(record: dict[str, object], *names: str) -> object:
    lowered = {str(key).lower(): value for key, value in record.items()}
    for name in names:
        value = lowered.get(name.lower())
        if value not in (None, ""):
            return value
    return ""


def load_fill_controls() -> dict[tuple[str, str], dict[str, str]]:
    controls: dict[tuple[str, str], dict[str, str]] = {}
    if not FILL_CONTROL.exists():
        return controls
    with FILL_CONTROL.open("r", encoding="utf-8-sig", newline="") as handle:
        for row in csv.DictReader(handle):
            if str(row.get("enabled", "1")).strip().lower() not in ("1", "true", "yes", "是"):
                continue
            scope_type = str(row.get("scope_type", "")).strip().lower()
            scope_code = str(row.get("scope_code", "")).strip()
            if scope_type and scope_code:
                controls[(scope_type, scope_code)] = row
    return controls


def parse_hex_color(value: str) -> tuple[int, int, int] | None:
    value = value.strip().lstrip("#")
    if len(value) != 6:
        return None
    try:
        return int(value[0:2], 16), int(value[2:4], 16), int(value[4:6], 16)
    except ValueError:
        return None


def political_color(
    record: dict[str, object],
    alpha: int,
    vary_by_unit: bool,
    controls: dict[tuple[str, str], dict[str, str]],
) -> tuple[int, int, int, int]:
    palette = [
        (174, 150, 84),
        (126, 143, 91),
        (164, 112, 76),
        (122, 143, 139),
        (170, 142, 109),
        (142, 122, 89),
        (151, 137, 82),
        (136, 112, 101),
    ]
    country = str(record_value(record, "admin", "adm0_a3", "sov_a3", "name") or "unknown")
    unit = str(record_value(record, "adm1_code", "iso_3166_2", "name") or country)
    control = (
        controls.get(("admin1", unit))
        or controls.get(("country", country))
        or controls.get(("default", "*"))
    )
    configured_color = parse_hex_color(str(control.get("fill_hex", ""))) if control else None
    base = configured_color or palette[zlib.crc32(country.encode("utf-8")) % len(palette)]
    if control:
        try:
            alpha = max(0, min(255, int(control.get("fill_alpha", alpha))))
        except (TypeError, ValueError):
            pass
    shade = 0
    if vary_by_unit and ("admin1", unit) not in controls:
        shade = (zlib.crc32(unit.encode("utf-8")) % 13) - 6
    return tuple(max(0, min(255, channel + shade)) for channel in base) + (alpha,)


def fill_shape_file(
    layer: Image.Image,
    shp_path: Path,
    alpha: int,
    vary_by_unit: bool,
    controls: dict[tuple[str, str], dict[str, str]],
) -> None:
    draw = ImageDraw.Draw(layer)
    reader = shapefile.Reader(str(shp_path), encoding="utf-8")
    for shape_record in reader.iterShapeRecords():
        record = shape_record.record.as_dict()
        color = political_color(record, alpha, vary_by_unit, controls)
        for part in iter_parts(shape_record.shape):
            if not part_visible(part):
                continue
            pts = projected_part(part)
            if len(pts) >= 3:
                draw.polygon(pts, fill=color)


def build_political_fill_layer(admin1_path: Path, admin0_path: Path) -> Image.Image:
    layer = Image.new("RGBA", (WIDTH, HEIGHT), (0, 0, 0, 0))
    controls = load_fill_controls()
    # Country faces guarantee that every closed land mass receives a color.
    fill_shape_file(layer, admin0_path, alpha=126, vary_by_unit=False, controls=controls)
    # Administrative faces add subtle province-to-province variation on top.
    fill_shape_file(layer, admin1_path, alpha=112, vary_by_unit=True, controls=controls)
    return layer


def add_cartographic_finish(image: Image.Image) -> Image.Image:
    parchment_path = ART_SOURCE / "parchment_ocean_base.png"
    parchment = Image.open(parchment_path).convert("RGB").resize(image.size, Image.Resampling.LANCZOS)
    texture = Image.blend(image.convert("RGB"), parchment, 0.09)
    vignette = Image.new("L", image.size, 255)
    vd = ImageDraw.Draw(vignette)
    for i in range(90):
        alpha = round(255 * (i / 90) ** 1.8)
        vd.rectangle((i, i, WIDTH - 1 - i, HEIGHT - 1 - i), outline=alpha, width=2)
    vignette = vignette.filter(ImageFilter.GaussianBlur(24))
    shadow = Image.new("RGB", image.size, (42, 28, 23))
    return Image.composite(texture, shadow, vignette)


def save_tiles(image: Image.Image, directory: Path, stem: str) -> list[str]:
    directory.mkdir(parents=True, exist_ok=True)
    names: list[str] = []
    tile_w = WIDTH // TILE_COLUMNS
    tile_h = HEIGHT // TILE_ROWS
    for row in range(TILE_ROWS):
        for col in range(TILE_COLUMNS):
            left = col * tile_w
            top = row * tile_h
            right = WIDTH if col == TILE_COLUMNS - 1 else left + tile_w
            bottom = HEIGHT if row == TILE_ROWS - 1 else top + tile_h
            name = f"{stem}_r{row + 1}_c{col + 1}.png"
            image.crop((left, top, right, bottom)).save(directory / name, optimize=True)
            names.append(name)
    return names


def export_admin_index(records: list[dict[str, object]]) -> int:
    rows = []
    seen = set()
    for record in records:
        lon = record.get("longitude")
        lat = record.get("latitude")
        if not isinstance(lon, (int, float)) or not isinstance(lat, (int, float)):
            continue
        lon_domain = lon_to_domain(float(lon))
        if not (LON_MIN <= lon_domain <= LON_MAX and LAT_MIN <= float(lat) <= LAT_MAX):
            continue
        key = str(record.get("adm1_code") or record.get("iso_3166_2") or "")
        if not key or key in seen:
            continue
        seen.add(key)
        x, y = project(float(lon), float(lat))
        rows.append(
            {
                "id": key,
                "country": record.get("admin") or "",
                "name": record.get("name") or "",
                "name_zh": record.get("name_zh") or record.get("name") or "",
                "type": record.get("type_en") or record.get("type") or "",
                "longitude": f"{float(lon):.6f}",
                "latitude": f"{float(lat):.6f}",
                "map_x": f"{x / WIDTH:.6f}",
                "map_y": f"{y / HEIGHT:.6f}",
            }
        )
    rows.sort(key=lambda r: (str(r["country"]), str(r["name"])))
    path = ART_SOURCE / "north_america_admin1_index.csv"
    with path.open("w", newline="", encoding="utf-8-sig") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(rows[0].keys()))
        writer.writeheader()
        writer.writerows(rows)
    return len(rows)


def main() -> None:
    OUTPUT.mkdir(parents=True, exist_ok=True)
    terrain_path = OUTPUT / "north_america_terrain.png"
    ensure_source_data(include_relief=not terrain_path.exists())
    if terrain_path.exists():
        terrain = Image.open(terrain_path).convert("RGB")
    else:
        terrain = crop_relief()
        terrain.save(terrain_path, optimize=True)

    admin1_layer, admin1_records = build_boundary_layer(
        SOURCE / "admin1" / "ne_10m_admin_1_states_provinces.shp",
        (245, 221, 166, 168),
        2,
    )
    admin0_layer, _ = build_boundary_layer(
        SOURCE / "admin0" / "ne_10m_admin_0_countries.shp",
        (65, 39, 33, 235),
        5,
    )
    boundaries = Image.alpha_composite(admin1_layer, admin0_layer)
    boundaries.save(OUTPUT / "north_america_admin_boundaries.png", optimize=True)

    composite = Image.alpha_composite(terrain.convert("RGBA"), boundaries)
    composite = add_cartographic_finish(composite)
    composite.save(OUTPUT / "north_america_strategy_map_full.png", optimize=True)
    tile_names = save_tiles(composite, TILES, "north_america")

    style_reference = ART_SOURCE / "european_war_3_style_reference.png"
    ew3_tile_names: list[str] = []
    if style_reference.exists():
        styled = Image.open(style_reference).convert("RGBA").resize((WIDTH, HEIGHT), Image.Resampling.LANCZOS)
        political_fill = build_political_fill_layer(
            SOURCE / "admin1" / "ne_10m_admin_1_states_provinces.shp",
            SOURCE / "admin0" / "ne_10m_admin_0_countries.shp",
        )
        styled = Image.alpha_composite(styled, political_fill)
        styled = Image.alpha_composite(styled, boundaries)
        styled = ImageEnhance.Contrast(styled.convert("RGB")).enhance(1.04)
        styled.save(OUTPUT / "north_america_strategy_map_ew3.png", optimize=True)
        ew3_tile_names = save_tiles(styled, EW3_TILES, "north_america_ew3")
    admin_count = export_admin_index(admin1_records)

    manifest = {
        "name": "North America Strategy Map",
        "projection": "continuous equirectangular across the international date line",
        "extent": {"west": 160, "east": -40, "south": LAT_MIN, "north": LAT_MAX},
        "size": {"width": WIDTH, "height": HEIGHT},
        "tiles": {"columns": TILE_COLUMNS, "rows": TILE_ROWS, "files": tile_names},
        "ew3_style_tiles": {
            "columns": TILE_COLUMNS,
            "rows": TILE_ROWS,
            "files": ew3_tile_names,
        },
        "admin1_units_indexed": admin_count,
        "sources": [
            "Natural Earth I with Shaded Relief and Water, 1:50m",
            "Natural Earth Admin 0 Countries, 1:10m",
            "Natural Earth Admin 1 States and Provinces, 1:10m",
        ],
    }
    (ART_SOURCE / "north_america_strategy_map_manifest.json").write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    print(json.dumps(manifest, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
