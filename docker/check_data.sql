SELECT "Name", "County", ST_Y("Geometry") as lat, ST_X("Geometry") as lng FROM "FireStations" LIMIT 5;
