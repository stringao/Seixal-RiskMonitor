#!/bin/bash
# OSRM Data Setup Script
# Downloads Portugal OSM data and processes it for OSRM

set -e

DATA_DIR="./docker/osrm-data"
PORTUGAL_PBF_URL="https://download.geofabrik.de/europe/portugal-latest.osm.pbf"
PORTUGAL_PBF_FILE="$DATA_DIR/portugal-latest.osm.pbf"

echo "=== OSRM Data Setup ==="

# Create data directory
mkdir -p "$DATA_DIR"

# Download Portugal OSM data if not exists
if [ ! -f "$PORTUGAL_PBF_FILE" ]; then
    echo "Downloading Portugal OSM data..."
    curl -L -o "$PORTUGAL_PBF_FILE" "$PORTUGAL_PBF_URL"
    echo "Download complete!"
else
    echo "Portugal PBF already exists, skipping download."
fi

# Check if extraction is needed
if [ ! -f "$DATA_DIR/portugal-latest.osrm" ]; then
    echo "Extracting OSRM data (this may take a while)..."
    docker run -t -v "$DATA_DIR:/data" osrm/osrm-backend osrm-extract -p /opt/car.lua /data/portugal-latest.osm.pbf
    echo "Extraction complete!"
else
    echo "OSRM extract already exists, skipping extraction."
fi

# Check if partitioning is needed
if [ ! -f "$DATA_DIR/portugal-latest.osrm.1" ]; then
    echo "Partitioning OSRM data..."
    docker run -t -v "$DATA_DIR:/data" osrm/osrm-backend osrm-partition /data/portugal-latest.osrm
    echo "Partitioning complete!"
else
    echo "OSRM partition already exists, skipping partitioning."
fi

# Check if customizing is needed
if [ ! -f "$DATA_DIR/portugal-latest.osrm.1.rdb" ]; then
    echo "Customizing OSRM data..."
    docker run -t -v "$DATA_DIR:/data" osrm/osrm-backend osrm-customize /data/portugal-latest.osrm
    echo "Customization complete!"
else
    echo "OSRM customization already exists, skipping customization."
fi

echo "=== OSRM Data Setup Complete ==="