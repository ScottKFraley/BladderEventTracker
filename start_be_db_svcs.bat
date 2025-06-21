@echo off
echo Starting 'backend' and 'database' containers...
docker compose --project-name=bladder-event-tracker -f docker-compose.yml up "backend" --build -d
echo.
