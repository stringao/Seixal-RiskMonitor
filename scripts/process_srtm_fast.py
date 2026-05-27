"""
Fast SRTM terrain processing using vectorized numpy operations.
Crops to Seixal/Setubal region for speed, generates all required GeoTIFFs.
"""
import rasterio
from rasterio.warp import reproject, Resampling
from rasterio.windows import from_bounds
import numpy as np
from pathlib import Path
import json

# Paths
SRTM_FILE = Path(r"X:\dev\SeixalRiscalMonitor\SRTM_Portugal\Portugal_SRTM_30m.tif")
OUTPUT_DIR = Path(r"X:\dev\SeixalRiscalMonitor\srtm_processed")
TERRAIN_RISK_FILE = OUTPUT_DIR / "terrain_risk_1km.tif"
CONFIG_FILE = OUTPUT_DIR / "terrain_config.json"

# Crop to Setubal/Seixal region (generous buffer)
CROP_BOUNDS = (-9.5, 37.8, -8.5, 38.9)  # (left, bottom, right, top)
CELL_SIZE = 30.0  # SRTM 30m resolution
TARGET_RES = 0.00833  # ~1km


def main():
    print("Fast SRTM Terrain Processing")
    print("=" * 40)

    OUTPUT_DIR.mkdir(exist_ok=True)

    # Read cropped DEM
    print(f"\n1. Reading SRTM data (cropped to Setubal region)")
    with rasterio.open(SRTM_FILE) as src:
        window = from_bounds(*CROP_BOUNDS, src.transform)
        window = window.round_shape()
        dem = src.read(1, window=window)
        transform = src.window_transform(window)
        crs = src.crs
        nodata = -32768.0
        print(f"   Cropped: {dem.shape[1]}x{dem.shape[0]} ({dem.size} pixels)")

    # Replace nodata with NaN for vectorized math
    valid_mask = dem != nodata
    dem_f = np.where(valid_mask, dem, 0).astype(np.float32)

    # --- Vectorized Slope & Aspect (Horn's method) ---
    print("\n2. Computing slope and aspect (vectorized)")
    # Pad for edge handling
    pad = np.pad(dem_f, 1, mode='edge')

    # Horn's method partial derivatives
    dzdx = ((pad[:-2, 2:] + 2*pad[1:-1, 2:] + pad[2:, 2:]) -
            (pad[:-2, :-2] + 2*pad[1:-1, :-2] + pad[2:, :-2])) / (8 * CELL_SIZE)
    dzdy = ((pad[2:, :-2] + 2*pad[2:, 1:-1] + pad[2:, 2:]) -
            (pad[:-2, :-2] + 2*pad[:-2, 1:-1] + pad[:-2, 2:])) / (8 * CELL_SIZE)

    slope_rad = np.arctan(np.sqrt(dzdx**2 + dzdy**2))
    slope = np.degrees(slope_rad)
    slope[~valid_mask] = nodata

    aspect = np.degrees(np.arctan2(dzdx, -dzdy))
    aspect[aspect < 0] += 360
    flat_mask = (dzdx == 0) & (dzdy == 0)
    aspect[flat_mask] = -1
    aspect[~valid_mask] = nodata

    print(f"   Slope: {slope[valid_mask].min():.1f} - {slope[valid_mask].max():.1f} deg")
    print(f"   Aspect: {aspect[valid_mask & (aspect >= 0)].min():.0f} - {aspect[valid_mask & (aspect >= 0)].max():.0f} deg")

    # --- Terrain Risk (vectorized) ---
    print("\n3. Computing terrain risk score")
    s = np.where(valid_mask, slope, 0)
    a = np.where(valid_mask & (aspect >= 0), aspect, 0)

    # Slope factor (0-30)
    slope_factor = np.minimum(30, np.where(s <= 30, 15 * (s / 15) ** 1.5, 30))

    # Aspect factor
    aspect_factor = np.select(
        [((a >= 315) | (a < 45)) & valid_mask,
         (a >= 45) & (a < 135) & valid_mask,
         (a >= 135) & (a < 225) & valid_mask,
         valid_mask],
        [-5, 8, 15, 5],
        default=0
    )

    # Elevation factor
    center_lat = 38.6
    elev_factor = np.where(valid_mask, np.minimum(10, center_lat - 37), 0)

    risk = slope_factor + aspect_factor + elev_factor
    risk[~valid_mask] = 0

    # Normalize 0-100
    vmin, vmax = risk[valid_mask].min(), risk[valid_mask].max()
    if vmax > vmin:
        risk = 100 * (risk - vmin) / (vmax - vmin)
    risk[~valid_mask] = nodata

    print(f"   Risk: 0 - 100")

    # --- Solar Exposure (vectorized) ---
    print("\n4. Computing solar exposure")
    exposure = np.select(
        [((a >= 315) | (a < 45)) & valid_mask & (aspect >= 0),
         (a >= 45) & (a < 135) & valid_mask,
         (a >= 135) & (a < 225) & valid_mask,
         valid_mask & (aspect >= 0)],
        [0.7, 0.9, 1.0, 0.85],
        default=0
    ).astype(np.float32)

    # --- Terrain Complexity (vectorized with uniform_filter) ---
    print("\n5. Computing terrain complexity")
    from scipy.ndimage import uniform_filter
    s_safe = np.where(valid_mask, slope, 0)
    mean_sq = uniform_filter(s_safe**2, size=3)
    sq_mean = uniform_filter(s_safe, size=3)**2
    complexity = (mean_sq - sq_mean).astype(np.float32)
    complexity[~valid_mask] = nodata

    # --- Save rasters ---
    print("\n6. Saving processed rasters")
    profile = {
        'driver': 'GTiff',
        'dtype': 'float32',
        'nodata': nodata,
        'count': 1,
        'crs': crs,
        'transform': transform,
        'width': dem.shape[1],
        'height': dem.shape[0],
    }

    for name, data in [('slope', slope), ('aspect', aspect),
                        ('complexity', complexity), ('risk', risk),
                        ('solar_exposure', exposure)]:
        out_path = OUTPUT_DIR / f"{name}.tif"
        with rasterio.open(out_path, 'w', **profile) as dst:
            dst.write(data.astype(np.float32), 1)
        print(f"   Saved {out_path}")

    # --- Resample terrain_risk_1km ---
    print("\n7. Resampling to 1km grid")
    slope_path = OUTPUT_DIR / "slope.tif"
    with rasterio.open(slope_path) as src:
        width = int((src.bounds.right - src.bounds.left) / TARGET_RES)
        height = int((src.bounds.top - src.bounds.bottom) / TARGET_RES)
        dst_transform = rasterio.transform.from_bounds(
            src.bounds.left, src.bounds.bottom,
            src.bounds.right, src.bounds.top,
            width, height
        )
        data = np.zeros((height, width), dtype=np.float32)
        reproject(
            source=rasterio.band(src, 1),
            destination=data,
            src_transform=src.transform,
            src_crs=src.crs,
            dst_transform=dst_transform,
            dst_crs=src.crs,
            resampling=Resampling.bilinear
        )
        with rasterio.open(TERRAIN_RISK_FILE, 'w', driver='GTiff',
                           height=height, width=width, count=1,
                           dtype='float32', crs=crs,
                           transform=dst_transform, nodata=-32768) as dst:
            dst.write(data, 1)
    print(f"   Saved {TERRAIN_RISK_FILE}")

    # --- Config ---
    config = {
        "srtm_processed_dir": str(OUTPUT_DIR),
        "terrain_risk_file": str(TERRAIN_RISK_FILE),
        "slope_file": str(OUTPUT_DIR / "slope.tif"),
        "aspect_file": str(OUTPUT_DIR / "aspect.tif"),
        "solar_exposure_file": str(OUTPUT_DIR / "solar_exposure.tif"),
        "complexity_file": str(OUTPUT_DIR / "complexity.tif"),
        "nodata_value": nodata,
        "target_resolution_deg": TARGET_RES,
        "created_at": "2026-05-28"
    }
    with open(CONFIG_FILE, 'w') as f:
        json.dump(config, f, indent=2)

    print("\nDone!")


if __name__ == "__main__":
    main()
