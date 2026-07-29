from __future__ import annotations

import json
import math
import os
import random
import zipfile
from pathlib import Path
from typing import Iterable
from urllib.request import urlretrieve

from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageFont


PROJECT_ROOT = Path(__file__).resolve().parents[1]
CACHE_DIR = PROJECT_ROOT / "ArtBackups" / "MapSources"
OUTPUT_SCENE = PROJECT_ROOT / "Assets" / "Resources" / "Art" / "Scenes" / "scene_strategy.png"
OUTPUT_PREVIEW = PROJECT_ROOT / "DataTables" / "north_america_strategy_map_preview.png"
CONFIG_PATH = PROJECT_ROOT / "Assets" / "Resources" / "Data" / "MingLuGameConfig.json"

ADMIN1_URL = "https://raw.githubusercontent.com/nvkelso/natural-earth-vector/master/geojson/ne_10m_admin_1_states_provinces.geojson"
RASTER_URL = "https://naciscdn.org/naturalearth/50m/raster/NE1_50M_SR_W.zip"

ADMIN1_PATH = CACHE_DIR / "ne_10m_admin_1_states_provinces.geojson"
RASTER_ZIP_PATH = CACHE_DIR / "NE1_50M_SR_W.zip"
RASTER_TIF_PATH = CACHE_DIR / "NE1_50M_SR_W.tif"

OUTPUT_SIZE = (1280, 720)
PREVIEW_SIZE = (1920, 1080)

# Mainland North America plus Alaska, Mexico, Central America, and the Caribbean edge.
BBOX = (-170.0, -52.0, 7.0, 75.0)  # lon_min, lon_max, lat_min, lat_max

NORTH_AMERICA_COUNTRIES = {
    "Canada",
    "United States of America",
    "Mexico",
    "Greenland",
    "Guatemala",
    "Belize",
    "Honduras",
    "El Salvador",
    "Nicaragua",
    "Costa Rica",
    "Panama",
    "Cuba",
    "Jamaica",
    "Haiti",
    "Dominican Republic",
    "Bahamas",
    "The Bahamas",
    "Puerto Rico",
}

COUNTRY_FILLS = {
    "Canada": (118, 146, 111, 58),
    "United States of America": (182, 151, 99, 54),
    "Mexico": (159, 127, 82, 58),
    "Greenland": (185, 200, 203, 64),
    "Guatemala": (142, 118, 82, 56),
    "Belize": (142, 118, 82, 56),
    "Honduras": (142, 118, 82, 56),
    "El Salvador": (142, 118, 82, 56),
    "Nicaragua": (142, 118, 82, 56),
    "Costa Rica": (142, 118, 82, 56),
    "Panama": (142, 118, 82, 56),
    "Cuba": (136, 146, 115, 54),
    "Jamaica": (136, 146, 115, 54),
    "Haiti": (136, 146, 115, 54),
    "Dominican Republic": (136, 146, 115, 54),
    "Bahamas": (136, 146, 115, 54),
    "The Bahamas": (136, 146, 115, 54),
    "Puerto Rico": (136, 146, 115, 54),
}

PROVINCE_GEO_POINTS = {
    "beichenwan": (-150.0, 60.5),
    "hanhebao": (-137.0, 64.0),
    "xihaiwei": (-125.0, 50.5),
    "songwanpu": (-122.7, 45.8),
    "liuyunshan": (-116.0, 54.0),
    "luojiguan": (-111.5, 45.3),
    "dashazhou": (-116.2, 39.0),
    "jinshaigang": (-122.0, 37.4),
    "nanlinggang": (-117.1, 32.2),
    "yulongyuan": (-112.0, 34.5),
    "yinshanzhen": (-106.0, 39.2),
    "chilingyi": (-106.2, 34.0),
    "heishuiling": (-98.0, 56.5),
    "caohaiying": (-100.2, 47.0),
    "huangyuantai": (-98.2, 39.0),
    "fengqiyuan": (-98.8, 34.3),
    "muchuanzhen": (-98.2, 30.0),
    "changhedu": (-90.0, 36.7),
    "mihekou": (-90.0, 29.5),
    "shihuaiying": (-86.0, 34.0),
    "beihuaitai": (-93.0, 46.0),
    "wuhucheng": (-84.2, 43.5),
    "tiehuwei": (-86.0, 44.5),
    "luoshuiying": (-82.0, 39.8),
    "xueyuanbao": (-79.0, 56.0),
    "shenghekou": (-72.0, 47.4),
    "baisongling": (-68.0, 45.6),
    "yunjinggang": (-62.5, 47.2),
    "longmenbao": (-74.0, 43.0),
    "xinjing": (-74.0, 40.8),
    "wanghaiguan": (-71.0, 42.8),
    "canghaiyi": (-76.3, 39.0),
    "haimenwei": (-76.5, 37.6),
    "qinglingguan": (-81.3, 37.0),
    "yanzecheng": (-83.5, 33.2),
    "suiyangfu": (-79.5, 34.0),
    "nanwanfu": (-82.4, 27.5),
}

MAP_PANEL_SIZE = (830.0, 520.0)
MAP_NODE_MARGIN = (38.0, 28.0)


def ensure_file(url: str, path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    if path.exists() and path.stat().st_size > 1024:
        return
    print(f"Downloading {url}")
    urlretrieve(url, path)


def ensure_raster() -> Path:
    ensure_file(RASTER_URL, RASTER_ZIP_PATH)
    if RASTER_TIF_PATH.exists() and RASTER_TIF_PATH.stat().st_size > 1024:
        return RASTER_TIF_PATH

    with zipfile.ZipFile(RASTER_ZIP_PATH) as archive:
        tif_names = [name for name in archive.namelist() if name.lower().endswith((".tif", ".tiff"))]
        if not tif_names:
            raise RuntimeError("Natural Earth raster zip did not contain a TIFF file.")
        name = tif_names[0]
        with archive.open(name) as src, RASTER_TIF_PATH.open("wb") as dst:
            dst.write(src.read())
    return RASTER_TIF_PATH


def normalize_lon(lon: float) -> float:
    if lon > 180.0:
        return lon - 360.0
    if lon < -180.0:
        return lon + 360.0
    return lon


def geo_to_pixel(lon: float, lat: float, size: tuple[int, int] = OUTPUT_SIZE) -> tuple[float, float]:
    lon_min, lon_max, lat_min, lat_max = BBOX
    x = (normalize_lon(lon) - lon_min) / (lon_max - lon_min) * size[0]
    y = (lat_max - lat) / (lat_max - lat_min) * size[1]
    return x, y


def geo_to_panel(lon: float, lat: float) -> tuple[int, int]:
    lon_min, lon_max, lat_min, lat_max = BBOX
    usable_w = MAP_PANEL_SIZE[0] - MAP_NODE_MARGIN[0] * 2.0
    usable_h = MAP_PANEL_SIZE[1] - MAP_NODE_MARGIN[1] * 2.0
    x = ((normalize_lon(lon) - lon_min) / (lon_max - lon_min) - 0.5) * usable_w
    y = ((lat - lat_min) / (lat_max - lat_min) - 0.5) * usable_h
    return int(round(x)), int(round(y))


def crop_world_raster(path: Path, output_size: tuple[int, int]) -> Image.Image:
    with Image.open(path) as source:
        source = source.convert("RGB")
        width, height = source.size
        lon_min, lon_max, lat_min, lat_max = BBOX
        left = int((lon_min + 180.0) / 360.0 * width)
        right = int((lon_max + 180.0) / 360.0 * width)
        top = int((90.0 - lat_max) / 180.0 * height)
        bottom = int((90.0 - lat_min) / 180.0 * height)
        crop = source.crop((left, top, right, bottom))

    crop = crop.resize(output_size, Image.Resampling.LANCZOS)
    crop = ImageEnhance.Color(crop).enhance(0.84)
    crop = ImageEnhance.Contrast(crop).enhance(1.05)
    crop = ImageEnhance.Brightness(crop).enhance(0.92)
    return crop


def iter_rings(geometry: dict) -> Iterable[list[tuple[float, float]]]:
    if not geometry:
        return
    geometry_type = geometry.get("type")
    coordinates = geometry.get("coordinates", [])
    if geometry_type == "Polygon":
        for ring in coordinates:
            yield [(normalize_lon(float(lon)), float(lat)) for lon, lat, *_ in ring]
    elif geometry_type == "MultiPolygon":
        for polygon in coordinates:
            for ring in polygon:
                yield [(normalize_lon(float(lon)), float(lat)) for lon, lat, *_ in ring]


def geometry_intersects_bbox(geometry: dict) -> bool:
    lon_min, lon_max, lat_min, lat_max = BBOX
    found = False
    min_lon = min_lat = math.inf
    max_lon = max_lat = -math.inf
    for ring in iter_rings(geometry) or []:
        for lon, lat in ring:
            found = True
            min_lon = min(min_lon, lon)
            max_lon = max(max_lon, lon)
            min_lat = min(min_lat, lat)
            max_lat = max(max_lat, lat)
    if not found:
        return False
    return not (max_lon < lon_min or min_lon > lon_max or max_lat < lat_min or min_lat > lat_max)


def feature_country(feature: dict) -> str:
    props = feature.get("properties", {})
    for key in ("admin", "adm0_name", "geonunit", "sovereignt"):
        value = props.get(key)
        if value:
            return str(value)
    return ""


def stable_jitter(name: str, amount: int = 18) -> tuple[int, int, int]:
    rnd = random.Random(name)
    return tuple(rnd.randint(-amount, amount) for _ in range(3))


def draw_admin_blocks(base: Image.Image, admin_geojson_path: Path) -> None:
    data = json.loads(admin_geojson_path.read_text(encoding="utf-8"))
    fill_layer = Image.new("RGBA", base.size, (0, 0, 0, 0))
    border_layer = Image.new("RGBA", base.size, (0, 0, 0, 0))
    fill_draw = ImageDraw.Draw(fill_layer)
    border_draw = ImageDraw.Draw(border_layer)

    features = data.get("features", [])
    for feature in features:
        country = feature_country(feature)
        props = feature.get("properties", {})
        if country not in NORTH_AMERICA_COUNTRIES and props.get("name") != "Greenland":
            continue
        geometry = feature.get("geometry")
        if not geometry_intersects_bbox(geometry):
            continue

        name = str(props.get("name") or props.get("name_en") or "")
        base_fill = COUNTRY_FILLS.get(country, (160, 130, 92, 50))
        jitter = stable_jitter(country + name)
        fill = (
            max(0, min(255, base_fill[0] + jitter[0])),
            max(0, min(255, base_fill[1] + jitter[1])),
            max(0, min(255, base_fill[2] + jitter[2])),
            base_fill[3],
        )
        for ring in iter_rings(geometry) or []:
            points = [geo_to_pixel(lon, lat) for lon, lat in ring]
            if len(points) < 3:
                continue
            fill_draw.polygon(points, fill=fill)
            border_draw.line(points + [points[0]], fill=(68, 56, 38, 112), width=1)

    fill_layer = fill_layer.filter(ImageFilter.GaussianBlur(radius=0.25))
    base.alpha_composite(fill_layer)
    base.alpha_composite(border_layer)


def font(size: int) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    candidates = [
        Path(os.environ.get("WINDIR", "C:/Windows")) / "Fonts" / "msyh.ttc",
        Path(os.environ.get("WINDIR", "C:/Windows")) / "Fonts" / "simhei.ttf",
        Path(os.environ.get("WINDIR", "C:/Windows")) / "Fonts" / "simsun.ttc",
    ]
    for candidate in candidates:
        if candidate.exists():
            return ImageFont.truetype(str(candidate), size)
    return ImageFont.load_default()


def draw_text(draw: ImageDraw.ImageDraw, xy: tuple[float, float], text: str, font_obj, fill, anchor: str = "mm") -> None:
    x, y = xy
    for dx, dy in ((1, 1), (-1, 1), (1, -1), (-1, -1)):
        draw.text((x + dx, y + dy), text, font=font_obj, fill=(20, 14, 8, 128), anchor=anchor)
    draw.text((x, y), text, font=font_obj, fill=fill, anchor=anchor)


def draw_polyline(draw: ImageDraw.ImageDraw, coords: list[tuple[float, float]], fill, width: int, size: tuple[int, int] = OUTPUT_SIZE) -> None:
    points = [geo_to_pixel(lon, lat, size) for lon, lat in coords]
    if len(points) >= 2:
        draw.line(points, fill=fill, width=width, joint="curve")


def draw_terrain_guides(base: Image.Image) -> None:
    overlay = Image.new("RGBA", base.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(overlay)

    mountain_lines = [
        [(-150, 63), (-136, 58), (-126, 51), (-119, 45), (-113, 39), (-107, 34), (-104, 29)],
        [(-124, 49), (-121, 43), (-119, 38), (-117, 34)],
        [(-86, 45), (-82, 40), (-81, 36), (-84, 32)],
        [(-111, 28), (-106, 24), (-101, 20)],
    ]
    for line in mountain_lines:
        draw_polyline(draw, line, (95, 73, 50, 160), 8)
        draw_polyline(draw, line, (204, 183, 141, 132), 3)

    river_lines = [
        [(-95, 47), (-94, 43), (-91, 39), (-90, 35), (-90, 31), (-89, 29)],
        [(-111, 45), (-104, 43), (-99, 40), (-95, 38), (-91, 38), (-90, 35)],
        [(-97, 31), (-101, 29), (-104, 27), (-106, 25)],
        [(-79, 44), (-75, 45), (-71, 47), (-66, 49)],
        [(-139, 63), (-145, 61), (-150, 60), (-158, 61)],
    ]
    for line in river_lines:
        draw_polyline(draw, line, (58, 103, 132, 150), 4)
        draw_polyline(draw, line, (174, 211, 221, 122), 1)

    label_font = font(28)
    small_font = font(22)
    labels = [
        ("北冰洋", (-133, 72), small_font, (222, 225, 205, 190)),
        ("太平洋", (-157, 34), label_font, (226, 220, 188, 190)),
        ("大西洋", (-59, 37), label_font, (226, 220, 188, 190)),
        ("墨西哥湾", (-91, 24), small_font, (227, 221, 188, 196)),
        ("哈德逊湾", (-84, 60), small_font, (224, 224, 206, 185)),
        ("落基山脉", (-116, 43), small_font, (84, 63, 42, 190)),
        ("大平原", (-101, 43), small_font, (90, 72, 44, 182)),
        ("五大湖", (-84, 45), small_font, (36, 73, 91, 190)),
        ("阿巴拉契亚", (-81, 38), small_font, (64, 77, 45, 184)),
    ]
    for text, geo, font_obj, color in labels:
        draw_text(draw, geo_to_pixel(*geo), text, font_obj, color)

    base.alpha_composite(overlay)


def add_map_finish(base: Image.Image) -> Image.Image:
    width, height = base.size
    overlay = Image.new("RGBA", base.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(overlay)

    # Warm parchment edge and a subtle tactical-table vignette.
    edge = 30
    for i in range(edge):
        alpha = int(72 * (1.0 - i / edge))
        draw.rectangle((i, i, width - i - 1, height - i - 1), outline=(50, 31, 16, alpha), width=1)

    grid_color = (105, 88, 60, 36)
    lon = -170
    while lon <= -50:
        x, _ = geo_to_pixel(lon, 40)
        draw.line([(x, 0), (x, height)], fill=grid_color, width=1)
        lon += 10
    lat = 10
    while lat <= 70:
        _, y = geo_to_pixel(-110, lat)
        draw.line([(0, y), (width, y)], fill=grid_color, width=1)
        lat += 10

    title_font = font(34)
    draw.rounded_rectangle((28, 28, 420, 78), radius=10, fill=(30, 21, 13, 120), outline=(220, 181, 93, 140), width=2)
    draw_text(draw, (48, 52), "北美洲行政地形战略图", title_font, (238, 204, 126, 230), anchor="lm")
    base.alpha_composite(overlay)
    return base.convert("RGB")


def generate_map() -> None:
    CACHE_DIR.mkdir(parents=True, exist_ok=True)
    ensure_file(ADMIN1_URL, ADMIN1_PATH)
    raster_path = ensure_raster()

    base = crop_world_raster(raster_path, OUTPUT_SIZE).convert("RGBA")
    draw_admin_blocks(base, ADMIN1_PATH)
    draw_terrain_guides(base)
    final = add_map_finish(base)
    OUTPUT_SCENE.parent.mkdir(parents=True, exist_ok=True)
    final.save(OUTPUT_SCENE)

    preview = final.resize(PREVIEW_SIZE, Image.Resampling.LANCZOS)
    OUTPUT_PREVIEW.parent.mkdir(parents=True, exist_ok=True)
    preview.save(OUTPUT_PREVIEW)
    print(f"Wrote {OUTPUT_SCENE}")
    print(f"Wrote {OUTPUT_PREVIEW}")


def update_config_positions() -> None:
    data = json.loads(CONFIG_PATH.read_text(encoding="utf-8-sig"))
    missing = []
    for province in data.get("provinces", []):
        point = PROVINCE_GEO_POINTS.get(province.get("id"))
        if not point:
            missing.append(province.get("id", ""))
            continue
        x, y = geo_to_panel(*point)
        province["x"] = x
        province["y"] = y
    if missing:
        raise RuntimeError("Missing province geo points: " + ", ".join(missing))
    CONFIG_PATH.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"Updated province positions in {CONFIG_PATH}")


def main() -> None:
    generate_map()
    update_config_positions()


if __name__ == "__main__":
    main()
