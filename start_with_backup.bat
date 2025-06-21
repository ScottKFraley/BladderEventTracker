@echo off
echo Starting containers...
docker compose --project-name=bladder-event-tracker -f docker-compose.yml up "backend" --build -d
echo.
echo Running backup script...
docker exec -it database bash -c "apt-get update && apt-get install -y dos2unix && cd /scripts && dos2unix backup_container_db.sh && chmod +x backup_container_db.sh && PGPASSWORD=$POSTGRES_PASSWORD ./backup_container_db.sh"
echo.
echo Backup process completed.
