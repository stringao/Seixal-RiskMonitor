"""
Pre-process SRTM data to compute terrain risk factors.
Reads Portugal_SRTM_30m.tif, computes slope, aspect, and terrain risk score,
then outputs a resampled terrain_risk_1km.tif matching the weather grid.

Run: python scripts/process_srtm_terrain.py
"""

import rasterio
from rasterio.warp import calculate_default_transform, reproject, Resampling
import numpy as np
from pathlib import Path
import json

# Paths
SRTM_DIR = Path(r"X:\dev\SeixalRiscalMonitor\SRTM_Portugal")
SRTM_FILE = SRTM_DIR / "Portugal_SRTM_30m.tif"
OUTPUT_DIR = Path(r"X:\dev\SeixalRiscalMonitor\srtm_processed")
TERRAIN_RISK_FILE = OUTPUT_DIR / "terrain_risk_1km.tif"
CONFIG_FILE = OUTPUT_DIR / "terrain_config.json"

# Target resolution in degrees (approximately 1km at mid-latitudes)
TARGET_RES = 0.00833  # ~1km (1/120 degree)

# Portugal bounding box (approximate)
PORTUGAL_BBOX = {
    "min_lon": -9.5, "max_lon": -6.0,
    "min_lat": 37.0, "max_lat": 42.0
}


def compute_slope_aspect(dem):
    """Compute slope (degrees) and aspect (degrees) from DEM using finite differences.

    Slope: maximum gradient in any direction.
    Aspect: direction of steepest descent (0=N, 90=E, 180=S, 270=W).
    """
    rows, cols = dem.shape
    slope = np.zeros_like(dem, dtype=np.float32)
    aspect = np.zeros_like(dem, dtype=np.float32)

    # Handle NoData
    nodata = -32768.0

    for i in range(1, rows - 1):
        for j in range(1, cols - 1):
            if dem[i, j] == nodata:
                continue

            # 3x3 neighborhood elevations
            z = [
                [dem[i-1, j-1], dem[i-1, j], dem[i-1, j+1]],
                [dem[i,   j-1], dem[i,   j], dem[i,   j+1]],
                [dem[i+1, j-1], dem[i+1, j], dem[i+1, j+1]]
            ]

            # Cell size in meters (approximate at this latitude)
            cell_size = 30.0  # SRTM 30m

            # Partial derivatives using Horn's method
            dzdx = ((z[0][2] + 2*z[1][2] + z[2][2]) - (z[0][0] + 2*z[1][0] + z[2][0])) / (8 * cell_size)
            dzdy = ((z[2][0] + 2*z[2][1] + z[2][2]) - (z[0][0] + 2*z[0][1] + z[0][2])) / (8 * cell_size)

            # Slope in degrees
            slope_rad = np.arctan(np.sqrt(dzdx**2 + dzdy**2))
            slope[i, j] = np.degrees(slope_rad)

            # Aspect in degrees (clockwise from North)
            if dzdx == 0 and dzdy == 0:
                aspect[i, j] = -1  # Flat
            else:
                aspect_rad = np.arctan2(dzdx, -dzdy)  # Note: -dzdy because row increases southward
                aspect_deg = np.degrees(aspect_rad)
                if aspect_deg < 0:
                    aspect_deg += 360
                aspect[i, j] = aspect_deg

    return slope, aspect


def compute_terrain_risk(slope, aspect, lat):
    """Compute terrain risk score based on slope, aspect, and latitude.

    Factors:
    - High slope = harder firefighting access + faster fire uphill spread
    - South-facing (in Portugal) = more solar radiation = drier = higher risk
    - North-facing = more moisture retention = lower risk
    - High elevation = typically lower humidity + wind exposure
    """
    risk = np.zeros_like(slope, dtype=np.float32)
    nodata_mask = slope == -32768.0

    for i in range(slope.shape[0]):
        for j in range(slope.shape[1]):
            if nodata_mask[i, j]:
                continue

            s = slope[i, j]
            a = aspect[i, j]

            # Slope factor: exponential increase with slope (0-30 points)
            # Steep terrain >30deg is significantly harder to fight
            slope_factor = min(30, 15 * (s / 15) ** 1.5) if s <= 30 else 30

            # Aspect factor: south-facing = higher solar exposure in Portugal
            # North-facing (315-45 deg) = lower risk
            # East/West (45-135, 225-315) = moderate
            # South (135-225) = higher risk
            if 315 <= a <= 360 or 0 <= a < 45:
                aspect_factor = -5  # North - moisture retention
            elif 45 <= a < 135:
                aspect_factor = 8   # East - morning sun, some moisture
            elif 135 <= a < 225:
                aspect_factor = 15  # South - maximum solar exposure
            else:  # 225-315
                aspect_factor = 5   # West - afternoon sun

            # Elevation factor: higher = more wind, less moisture (0-10 points)
            # Approximated from latitude (actual elevation read from DEM)
            elev_factor = min(10, lat - 37) if lat > 37 else 0

            # Combined risk
            risk[i, j] = slope_factor + aspect_factor + elev_factor

    # Normalize to 0-100 scale
    risk_min = risk[~nodata_mask].min()
    risk_max = risk[~nodata_mask].max()
    if risk_max > risk_min:
        risk = 100 * (risk - risk_min) / (risk_max - risk_min)

    # Apply nodata mask
    risk[nodata_mask] = -32768.0

    return risk


def compute_solar_exposure_index(aspect, lat, month=7):
    """Estimate solar exposure index (kWh/m²/year approximation).

    Based on aspect + latitude. Summer month (July) used as reference.
    North-facing slopes in Portugal receive less direct radiation in summer.
    """
    exposure = np.zeros_like(aspect, dtype=np.float32)

    # Latitude factor for solar angle
    lat_rad = np.radians(lat)
    solar_angle = 90 - lat + 23.5 * np.sin(2 * np.pi * (month - 3) / 12)  # Approx solar declination

    for i in range(aspect.shape[0]):
        for j in range(aspect.shape[1]):
            a = aspect[i, j]
            if a < 0:
                continue

            # Effective angle between slope normal and sun
            # Simplified: assume sun is in the south (180 degrees) at noon in Portugal
            if 315 <= a <= 360 or 0 <= a < 45:
                exposure[i, j] = 0.7  # North - minimal direct exposure
            elif 45 <= a < 135:
                exposure[i, j] = 0.9   # East - morning sun
            elif 135 <= a < 225:
                exposure[i, j] = 1.0   # South - maximum exposure
            else:
                exposure[i, j] = 0.85  # West - afternoon sun

    return exposure


def compute_terrain_complexity(slope, window=3):
    """Compute terrain complexity from slope variance in a window.

    Returns variance of slope in 3x3 neighborhood.
    """
    rows, cols = slope.shape
    complexity = np.zeros_like(slope, dtype=np.float32)
    half = window // 2
    nodata_mask = slope == -32768.0

    for i in range(half, rows - half):
        for j in range(half, cols - half):
            if nodata_mask[i, j]:
                continue

            window_vals = []
            for di in range(-half, half + 1):
                for dj in range(-half, half + 1):
                    if not nodata_mask[i + di, j + dj]:
                        window_vals.append(slope[i + di, j + dj])

            if len(window_vals) >= 4:
                complexity[i, j] = np.var(window_vals)

    return complexity


def resample_to_grid(src_path, dst_path, target_res):
    """Resample raster to target resolution (~1km grid)."""
    with rasterio.open(src_path) as src:
        # Calculate new dimensions
        width = int((src.bounds.right - src.bounds.left) / target_res)
        height = int((src.bounds.top - src.bounds.bottom) / target_res)

        transform = rasterio.transform.from_bounds(
            src.bounds.left, src.bounds.bottom,
            src.bounds.right, src.bounds.top,
            width, height
        )

        # Create destination array
        data = np.zeros((height, width), dtype=np.float32)

        # Reproject
        reproject(
            source=rasterio.band(src, 1),
            destination=data,
            src_transform=src.transform,
            src_crs=src.crs,
            dst_transform=transform,
            dst_crs=src.crs,
            resampling=Resampling.bilinear
        )

        # Write output
        with rasterio.open(
            dst_path,
            'w',
            driver='GTiff',
            height=height,
            width=width,
            count=1,
            dtype=data.dtype,
            crs=src.crs,
            transform=transform,
            nodata=-32768
        ) as dst:
            dst.write(data, 1)

        return dst_path, src.crs, transform


def main():
    print("SRTM Terrain Processing Script")
    print("=" * 40)

    # Create output directory
    OUTPUT_DIR.mkdir(exist_ok=True)

    print(f"\n1. Reading SRTM data from {SRTM_FILE}")
    with rasterio.open(SRTM_FILE) as src:
        dem = src.read(1)
        print(f"   Dimensions: {src.width}x{src.height}")
        print(f"   CRS: {src.crs}")
        print(f"   Bounds: {src.bounds}")
        print(f"   NoData value: {src.nodata}")

    nodata = -32768.0

    print("\n2. Computing slope and aspect")
    slope, aspect = compute_slope_aspect(dem)
    print(f"   Slope range: {slope[slope != nodata].min():.1f} - {slope[slope != nodata].max():.1f} degrees")
    print(f"   Aspect range: {aspect[aspect >= 0].min():.0f} - {aspect[aspect >= 0].max():.0f} degrees")

    print("\n3. Computing terrain complexity")
    complexity = compute_terrain_complexity(slope)
    print(f"   Complexity range: {complexity[complexity > 0].min():.2f} - {complexity[complexity > 0].max():.2f}")

    print("\n4. Computing terrain risk score")
    # Use center latitude of Portugal
    center_lat = (PORTUGAL_BBOX["min_lat"] + PORTUGAL_BBOX["max_lat"]) / 2
    terrain_risk = compute_terrain_risk(slope, aspect, center_lat)
    print(f"   Risk range: {terrain_risk[terrain_risk > 0].min():.1f} - {terrain_risk[terrain_risk > 0].max():.1f}")

    print("\n5. Computing solar exposure index")
    solar_exposure = compute_solar_exposure_index(aspect, center_lat)
    print(f"   Exposure range: {solar_exposure[solar_exposure > 0].min():.2f} - {solar_exposure[solar_exposure > 0].max():.2f}")

    print("\n6. Saving processed rasters")
    base_profile = {
        'driver': 'GTiff',
        'dtype': np.float32,
        'nodata': nodata,
        'count': 1,
        'crs': 'EPSG:4326'
    }

    # Save individual rasters
    for name, data in [('slope', slope), ('aspect', aspect), ('complexity', complexity),
                        ('risk', terrain_risk), ('solar_exposure', solar_exposure)]:
        out_path = OUTPUT_DIR / f"{name}.tif"
        with rasterio.open(out_path, 'w', **base_profile,
                           width=dem.shape[1], height=dem.shape[0]) as dst:
            dst.write(data.astype(np.float32), 1)
        print(f"   Saved {out_path}")

    print("\n7. Resampling to 1km grid for weather integration")
    resample_to_grid(SRTM_FILE, TERRAIN_RISK_FILE, TARGET_RES)
    print(f"   Saved {TERRAIN_RISK_FILE}")

    # Save configuration for C# service
    config = {
        "srtm_processed_dir": str(OUTPUT_DIR),
        "terrain_risk_file": str(TERRAIN_RISK_FILE),
        "slope_file": str(OUTPUT_DIR / "slope.tif"),
        "aspect_file": str(OUTPUT_DIR / "aspect.tif"),
        "solar_exposure_file": str(OUTPUT_DIR / "solar_exposure.tif"),
        "complexity_file": str(OUTPUT_DIR / "complexity.tif"),
        "nodata_value": nodata,
        "target_resolution_deg": TARGET_RES,
        "bounding_box": PORTUGAL_BBOX,
        "created_at": str(Path(__file__).stat().st_mtime)
    }
    with open(CONFIG_FILE, 'w') as f:
        json.dump(config, f, indent=2)
    print(f"   Saved config: {CONFIG_FILE}")

    print("\nDone! C# service can now read terrain data from:")
    print(f"  {TERRAIN_RISK_FILE}")


if __name__ == "__main__":
    main()