from __future__ import annotations

import csv
import heapq
import json
import math
import sys
import urllib.request
import zipfile
import zlib
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "Tools" / "_vendor"))

import numpy as np
import shapefile  # type: ignore
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageFont

from build_north_america_strategy_map import (
    ART_SOURCE,
    HEIGHT,
    LAT_MAX,
    LAT_MIN,
    LON_MAX,
    LON_MIN,
    OUTPUT,
    SOURCE,
    WIDTH,
    ensure_source_data,
    iter_parts,
    part_visible,
)


GRID_W = 768
GRID_H = 432
TARGET_CITY_COUNT = 86
CITY_TILES = ART_SOURCE / "Tiles_CityRegions"
CITY_INDEX = ART_SOURCE / "north_america_city_regions.csv"

CITY_URL = "https://naciscdn.org/naturalearth/10m/cultural/ne_10m_populated_places.zip"
RIVER_URL = "https://naciscdn.org/naturalearth/10m/physical/ne_10m_rivers_lake_centerlines.zip"
CITY_SHP = SOURCE / "cities" / "ne_10m_populated_places.shp"
RIVER_SHP = SOURCE / "rivers" / "ne_10m_rivers_lake_centerlines.shp"
ADMIN0_SHP = SOURCE / "admin0" / "ne_10m_admin_0_countries.shp"

NORTH_AMERICA_CODES = {
    "USA", "CAN", "MEX", "GRL", "BLZ", "GTM", "HND", "SLV", "NIC", "CRI", "PAN",
    "CUB", "JAM", "HTI", "DOM", "BHS", "PRI", "TTO", "BRB", "GRD", "LCA", "VCT",
    "ATG", "DMA", "KNA", "BMU", "VIR", "CYM", "SPM",
}

CHINESE_REGION_NAMES = [
    "天澜", "天岁", "羽州", "霁川", "星原", "月泽", "苍汐", "曜岭", "玄雾", "青汀",
    "赤霞", "银津", "玉衡", "风眠", "云栖", "雁回", "鹿鸣", "鹤归", "龙吟", "凤翎",
    "松影", "溪月", "汐语", "霞照", "黎星", "晨霜", "暮云", "岚歌", "瀚月", "沧羽",
    "景星", "朔风", "熙光", "昭雾", "宁霞", "安月", "靖星", "怀霜", "望云", "归鹤",
    "绯雪", "素潮", "碧落", "金乌", "白鹿", "紫宸", "墨羽", "青冥", "玄鸟", "长风",
    "流火", "飞霜", "落星", "朝雾", "夜澜", "晴岚", "雨鹤", "雪汐", "雷泽", "云帆",
    "星槎", "月轮", "雾海", "霜岫", "苍曜", "青蘅", "赤羽", "银澜", "玉霄", "风烛",
    "云镜", "雁书", "鹿野", "鹤梦", "龙烛", "凤歌", "松月", "溪岚", "汐风", "霞羽",
    "黎光", "晨星", "暮雪", "岚月", "瀚霜", "沧星",
]


def project_to(lon: float, lat: float, width: int, height: int) -> tuple[float, float]:
    if lon < LON_MIN:
        lon += 360.0
    x = (lon - LON_MIN) / (LON_MAX - LON_MIN) * width
    y = (LAT_MAX - lat) / (LAT_MAX - LAT_MIN) * height
    return x, y


def download_shape(url: str, directory: Path, target: Path) -> None:
    if target.exists():
        return
    directory.mkdir(parents=True, exist_ok=True)
    archive = SOURCE / f"{directory.name}.zip"
    urllib.request.urlretrieve(url, archive)
    with zipfile.ZipFile(archive) as bundle:
        bundle.extractall(directory)
    try:
        archive.unlink()
    except PermissionError:
        pass


def ensure_city_sources() -> None:
    ensure_source_data(include_relief=False)
    download_shape(CITY_URL, SOURCE / "cities", CITY_SHP)
    download_shape(RIVER_URL, SOURCE / "rivers", RIVER_SHP)


def record_lower(record: shapefile._Record) -> dict[str, object]:
    return {str(key).lower(): value for key, value in record.as_dict().items()}


def haversine_km(a: dict[str, object], b: dict[str, object]) -> float:
    lat1 = math.radians(float(a["lat"]))
    lat2 = math.radians(float(b["lat"]))
    dlat = lat2 - lat1
    dlon = math.radians(float(b["lon"]) - float(a["lon"]))
    value = math.sin(dlat / 2) ** 2 + math.cos(lat1) * math.cos(lat2) * math.sin(dlon / 2) ** 2
    return 6371.0 * 2.0 * math.asin(min(1.0, math.sqrt(value)))


def load_cities() -> list[dict[str, object]]:
    candidates: list[dict[str, object]] = []
    reader = shapefile.Reader(str(CITY_SHP), encoding="utf-8")
    for shape_record in reader.iterShapeRecords():
        record = record_lower(shape_record.record)
        code = str(record.get("adm0_a3") or "")
        if code not in NORTH_AMERICA_CODES:
            continue
        lon = float(record.get("longitude") or shape_record.shape.points[0][0])
        lat = float(record.get("latitude") or shape_record.shape.points[0][1])
        lon_domain = lon + 360 if lon < LON_MIN else lon
        if not (LON_MIN <= lon_domain <= LON_MAX and LAT_MIN <= lat <= LAT_MAX):
            continue
        population = max(0, int(record.get("pop_max") or 0))
        capital = int(record.get("adm0cap") or 0)
        score = math.log10(max(1000, population)) + capital * 3.0 + int(record.get("worldcity") or 0)
        candidates.append(
            {
                "id": f"CITY_{int(record.get('ne_id') or len(candidates))}",
                "name": str(record.get("nameascii") or record.get("name") or ""),
                "name_zh": str(record.get("name_zh") or record.get("name") or ""),
                "country": str(record.get("adm0name") or code),
                "country_code": code,
                "lon": lon,
                "lat": lat,
                "population": population,
                "capital": capital,
                "score": score,
            }
        )
    candidates.sort(key=lambda city: float(city["score"]), reverse=True)

    selected: list[dict[str, object]] = []
    # Always retain national capitals, including the smaller Caribbean states.
    for city in candidates:
        if int(city["capital"]) and all(haversine_km(city, other) >= 90 for other in selected):
            selected.append(city)
    for minimum_distance in (360, 300, 240, 190, 145):
        for city in candidates:
            if city in selected:
                continue
            if all(haversine_km(city, other) >= minimum_distance for other in selected):
                selected.append(city)
                if len(selected) >= TARGET_CITY_COUNT:
                    return assign_strategy_names(selected)
    return assign_strategy_names(selected[:TARGET_CITY_COUNT])


def assign_strategy_names(cities: list[dict[str, object]]) -> list[dict[str, object]]:
    if len(cities) > len(CHINESE_REGION_NAMES):
        raise ValueError("Not enough Chinese strategy names for selected cities")
    # Coordinate order keeps the assignment stable even if population rankings change.
    ordered = sorted(cities, key=lambda city: (-float(city["lat"]), float(city["lon"])))
    for city, strategy_name in zip(ordered, CHINESE_REGION_NAMES):
        city["strategy_name"] = strategy_name
    return cities


def draw_land_mask() -> np.ndarray:
    image = Image.new("L", (GRID_W, GRID_H), 0)
    draw = ImageDraw.Draw(image)
    reader = shapefile.Reader(str(ADMIN0_SHP), encoding="utf-8")
    for shape_record in reader.iterShapeRecords():
        record = record_lower(shape_record.record)
        code = str(record.get("adm0_a3") or record.get("sov_a3") or "")
        if code not in NORTH_AMERICA_CODES:
            continue
        for part in iter_parts(shape_record.shape):
            if not part_visible(part):
                continue
            points = [project_to(lon, lat, GRID_W, GRID_H) for lon, lat in part]
            if len(points) >= 3:
                draw.polygon(points, fill=255)
    return np.asarray(image, dtype=np.uint8) > 0


def draw_river_mask() -> tuple[np.ndarray, list[list[tuple[float, float]]]]:
    image = Image.new("L", (GRID_W, GRID_H), 0)
    draw = ImageDraw.Draw(image)
    visible_parts: list[list[tuple[float, float]]] = []
    reader = shapefile.Reader(str(RIVER_SHP), encoding="utf-8")
    for shape_record in reader.iterShapeRecords():
        record = record_lower(shape_record.record)
        if int(record.get("scalerank") or 99) > 6:
            continue
        for part in iter_parts(shape_record.shape):
            if not part_visible(part):
                continue
            clipped_segments: list[list[tuple[float, float]]] = []
            segment: list[tuple[float, float]] = []
            for lon, lat in part:
                point = project_to(lon, lat, GRID_W, GRID_H)
                x, y = point
                in_frame = -4 <= x <= GRID_W + 4 and -4 <= y <= GRID_H + 4
                discontinuity = segment and (
                    abs(x - segment[-1][0]) > GRID_W * 0.12
                    or abs(y - segment[-1][1]) > GRID_H * 0.18
                )
                if not in_frame or discontinuity:
                    if len(segment) >= 2:
                        clipped_segments.append(segment)
                    segment = []
                if in_frame:
                    segment.append(point)
            if len(segment) >= 2:
                clipped_segments.append(segment)
            for points in clipped_segments:
                width = 3 if int(record.get("scalerank") or 99) <= 3 else 2
                draw.line(points, fill=255, width=width, joint="curve")
                visible_parts.append(
                    [
                        (
                            LON_MIN + x / GRID_W * (LON_MAX - LON_MIN),
                            LAT_MAX - y / GRID_H * (LAT_MAX - LAT_MIN),
                        )
                        for x, y in points
                    ]
                )
    return np.asarray(image, dtype=np.uint8) > 0, visible_parts


def terrain_slope() -> np.ndarray:
    terrain = Image.open(OUTPUT / "north_america_terrain.png").convert("L")
    terrain = terrain.resize((GRID_W, GRID_H), Image.Resampling.BILINEAR)
    values = np.asarray(terrain, dtype=np.float32)
    gy, gx = np.gradient(values)
    slope = np.hypot(gx, gy)
    return np.clip(slope / 12.0, 0, 4)


def nearest_land(x: int, y: int, land: np.ndarray) -> tuple[int, int] | None:
    if 0 <= x < GRID_W and 0 <= y < GRID_H and land[y, x]:
        return x, y
    for radius in range(1, 18):
        for yy in range(max(0, y - radius), min(GRID_H, y + radius + 1)):
            for xx in (x - radius, x + radius):
                if 0 <= xx < GRID_W and land[yy, xx]:
                    return xx, yy
        for xx in range(max(0, x - radius), min(GRID_W, x + radius + 1)):
            for yy in (y - radius, y + radius):
                if 0 <= yy < GRID_H and land[yy, xx]:
                    return xx, yy
    return None


def build_regions(
    cities: list[dict[str, object]],
    land: np.ndarray,
    rivers: np.ndarray,
    slope: np.ndarray,
) -> tuple[np.ndarray, list[dict[str, object]]]:
    distance = np.full((GRID_H, GRID_W), np.inf, dtype=np.float64)
    labels = np.full((GRID_H, GRID_W), -1, dtype=np.int16)
    heap: list[tuple[float, int, int, int]] = []
    usable: list[dict[str, object]] = []
    for city in cities:
        x, y = project_to(float(city["lon"]), float(city["lat"]), GRID_W, GRID_H)
        seed = nearest_land(round(x), round(y), land)
        if seed is None:
            continue
        sx, sy = seed
        label = len(usable)
        usable.append(city)
        population_bonus = max(0.0, math.log10(max(10000, int(city["population"]))) - 5.0) * 2.3
        initial = -min(8.0, population_bonus)
        if initial < distance[sy, sx]:
            distance[sy, sx] = initial
            labels[sy, sx] = label
            heapq.heappush(heap, (initial, sx, sy, label))

    neighbors = ((1, 0, 1.0), (-1, 0, 1.0), (0, 1, 1.0), (0, -1, 1.0),
                 (1, 1, 1.414), (-1, 1, 1.414), (1, -1, 1.414), (-1, -1, 1.414))
    while heap:
        current, x, y, label = heapq.heappop(heap)
        if current > float(distance[y, x]) + 1e-9 or labels[y, x] != label:
            continue
        for dx, dy, base in neighbors:
            nx, ny = x + dx, y + dy
            if nx < 0 or nx >= GRID_W or ny < 0 or ny >= GRID_H or not land[ny, nx]:
                continue
            river_penalty = 7.5 if rivers[y, x] or rivers[ny, nx] else 0.0
            terrain_penalty = float((slope[y, x] + slope[ny, nx]) * 0.9)
            candidate = current + base + river_penalty + terrain_penalty
            if candidate < float(distance[ny, nx]):
                distance[ny, nx] = candidate
                labels[ny, nx] = label
                heapq.heappush(heap, (candidate, nx, ny, label))

    # Assign seedless islands and remote Arctic land to their nearest city.
    missing_y, missing_x = np.where(land & (labels < 0))
    if len(missing_x):
        best = np.full(len(missing_x), np.inf, dtype=np.float64)
        best_label = np.full(len(missing_x), -1, dtype=np.int16)
        for label, city in enumerate(usable):
            cx, cy = project_to(float(city["lon"]), float(city["lat"]), GRID_W, GRID_H)
            candidate = (missing_x - cx) ** 2 + (missing_y - cy) ** 2
            mask = candidate < best
            best[mask] = candidate[mask]
            best_label[mask] = label
        labels[missing_y, missing_x] = best_label
    return labels, usable


def region_palette(city: dict[str, object]) -> tuple[int, int, int, int]:
    palette = [
        (167, 148, 83), (121, 145, 92), (164, 111, 73), (112, 139, 137),
        (171, 141, 106), (139, 119, 88), (149, 135, 79), (133, 110, 99),
    ]
    value = zlib.crc32(str(city["id"]).encode("utf-8"))
    return palette[value % len(palette)] + (126,)


def label_boundaries(labels: np.ndarray, land: np.ndarray) -> np.ndarray:
    boundary = np.zeros_like(land, dtype=bool)
    boundary[:, 1:] |= (labels[:, 1:] != labels[:, :-1]) & land[:, 1:] & land[:, :-1]
    boundary[1:, :] |= (labels[1:, :] != labels[:-1, :]) & land[1:, :] & land[:-1, :]
    return boundary


def region_neighbors(labels: np.ndarray) -> dict[int, set[int]]:
    result: dict[int, set[int]] = {}
    for left, right in ((labels[:, 1:], labels[:, :-1]), (labels[1:, :], labels[:-1, :])):
        mask = (left != right) & (left >= 0) & (right >= 0)
        for a, b in zip(left[mask].tolist(), right[mask].tolist()):
            result.setdefault(int(a), set()).add(int(b))
            result.setdefault(int(b), set()).add(int(a))
    return result


def render_map(
    labels: np.ndarray,
    cities: list[dict[str, object]],
    river_parts: list[list[tuple[float, float]]],
) -> Image.Image:
    base = Image.open(OUTPUT / "north_america_terrain.png").convert("RGB")
    base = ImageEnhance.Color(base).enhance(0.62)
    base = ImageEnhance.Contrast(base).enhance(1.18)
    base = ImageEnhance.Brightness(base).enhance(0.80)
    parchment = Image.open(ART_SOURCE / "parchment_ocean_base.png").convert("RGB")
    parchment = parchment.resize((WIDTH, HEIGHT), Image.Resampling.LANCZOS)
    base = Image.blend(base, parchment, 0.10).convert("RGBA")

    fill_small = Image.new("RGBA", (GRID_W, GRID_H), (0, 0, 0, 0))
    fill_pixels = np.asarray(fill_small).copy()
    for index, city in enumerate(cities):
        fill_pixels[labels == index] = region_palette(city)
    fill_layer = Image.fromarray(fill_pixels, mode="RGBA").resize((WIDTH, HEIGHT), Image.Resampling.NEAREST)
    result = Image.alpha_composite(base, fill_layer)

    boundaries = label_boundaries(labels, labels >= 0)
    boundary_image = Image.fromarray((boundaries * 255).astype(np.uint8), mode="L")
    boundary_image = boundary_image.resize((WIDTH, HEIGHT), Image.Resampling.NEAREST)
    boundary_image = boundary_image.filter(ImageFilter.MaxFilter(3))
    ink = Image.new("RGBA", (WIDTH, HEIGHT), (61, 38, 31, 225))
    result = Image.composite(ink, result, boundary_image)

    draw = ImageDraw.Draw(result)
    for part in river_parts:
        points = [project_to(lon, lat, WIDTH, HEIGHT) for lon, lat in part]
        draw.line(points, fill=(75, 111, 125, 220), width=4, joint="curve")
        draw.line(points, fill=(150, 180, 183, 155), width=2, joint="curve")

    font_path = Path("C:/Windows/Fonts/msyh.ttc")
    font = ImageFont.truetype(str(font_path), 16) if font_path.exists() else ImageFont.load_default()
    capital_font = ImageFont.truetype(str(font_path), 19) if font_path.exists() else font
    occupied: list[tuple[float, float, float, float]] = []
    for city in cities:
        x, y = project_to(float(city["lon"]), float(city["lat"]), WIDTH, HEIGHT)
        if not (0 <= x < WIDTH and 0 <= y < HEIGHT):
            continue
        radius = 7 if int(city["capital"]) else 5
        draw.ellipse((x - radius, y - radius, x + radius, y + radius), fill=(245, 217, 151, 255), outline=(55, 35, 30, 255), width=3)
        label = str(city["strategy_name"])
        selected_font = capital_font if int(city["capital"]) else font
        box = draw.textbbox((0, 0), label, font=selected_font, stroke_width=2)
        label_w, label_h = box[2] - box[0], box[3] - box[1]
        options = [
            (x + radius + 4, y - label_h / 2),
            (x - radius - 4 - label_w, y - label_h / 2),
            (x - label_w / 2, y - radius - 5 - label_h),
            (x - label_w / 2, y + radius + 5),
        ]
        placed = False
        for tx, ty in options:
            candidate = (tx - 2, ty - 2, tx + label_w + 2, ty + label_h + 2)
            if tx < 0 or ty < 0 or candidate[2] >= WIDTH or candidate[3] >= HEIGHT:
                continue
            if any(not (candidate[2] < other[0] or candidate[0] > other[2] or candidate[3] < other[1] or candidate[1] > other[3]) for other in occupied):
                continue
            draw.text((tx, ty), label, font=selected_font, fill=(47, 34, 29, 255),
                      stroke_width=2, stroke_fill=(232, 211, 166, 225))
            occupied.append(candidate)
            placed = True
            break
        if not placed and int(city["capital"]):
            tx, ty = options[0]
            draw.text((tx, ty), label, font=selected_font, fill=(47, 34, 29, 255),
                      stroke_width=2, stroke_fill=(232, 211, 166, 225))
    return result.convert("RGB")


def export_regions(labels: np.ndarray, cities: list[dict[str, object]]) -> None:
    neighbors = region_neighbors(labels)
    for index, city in enumerate(cities):
        if neighbors.get(index):
            continue
        candidates = [
            (haversine_km(city, other), other_index)
            for other_index, other in enumerate(cities)
            if other_index != index
        ]
        if candidates:
            _, nearest = min(candidates)
            neighbors.setdefault(index, set()).add(nearest)
            neighbors.setdefault(nearest, set()).add(index)
    with CITY_INDEX.open("w", newline="", encoding="utf-8-sig") as handle:
        fields = ["id", "strategy_name", "source_city_name", "source_city_name_zh", "country", "country_code",
                  "longitude", "latitude", "population", "map_x", "map_y", "neighbors"]
        writer = csv.DictWriter(handle, fieldnames=fields)
        writer.writeheader()
        for index, city in enumerate(cities):
            x, y = project_to(float(city["lon"]), float(city["lat"]), WIDTH, HEIGHT)
            writer.writerow(
                {
                    "id": city["id"],
                    "strategy_name": city["strategy_name"],
                    "source_city_name": city["name"],
                    "source_city_name_zh": city["name_zh"],
                    "country": city["country"],
                    "country_code": city["country_code"],
                    "longitude": f"{float(city['lon']):.6f}",
                    "latitude": f"{float(city['lat']):.6f}",
                    "population": city["population"],
                    "map_x": f"{x / WIDTH:.6f}",
                    "map_y": f"{y / HEIGHT:.6f}",
                    "neighbors": ";".join(str(cities[n]["id"]) for n in sorted(neighbors.get(index, set()))),
                }
            )


def save_tiles(image: Image.Image) -> None:
    CITY_TILES.mkdir(parents=True, exist_ok=True)
    tile_w, tile_h = WIDTH // 4, HEIGHT // 3
    for row in range(3):
        for col in range(4):
            image.crop((col * tile_w, row * tile_h, (col + 1) * tile_w, (row + 1) * tile_h)).save(
                CITY_TILES / f"north_america_city_r{row + 1}_c{col + 1}.png",
                optimize=True,
            )


def main() -> None:
    ensure_city_sources()
    cities = load_cities()
    land = draw_land_mask()
    rivers, river_parts = draw_river_mask()
    slope = terrain_slope()
    labels, cities = build_regions(cities, land, rivers, slope)
    image = render_map(labels, cities, river_parts)
    output_path = OUTPUT / "north_america_city_regions.png"
    image.save(output_path, optimize=True)
    save_tiles(image)
    export_regions(labels, cities)
    summary = {
        "map": str(output_path),
        "city_regions": len(cities),
        "grid": [GRID_W, GRID_H],
        "partition": "multi-source terrain-cost expansion",
        "barriers": ["major rivers", "terrain slope", "coastlines"],
    }
    (ART_SOURCE / "north_america_city_regions_manifest.json").write_text(
        json.dumps(summary, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    print(json.dumps(summary, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
