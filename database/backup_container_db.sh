#!/bin/bash

# Set backup directory - this should match the container path in docker-compose.yml
BACKUP_DIR=/backups
TIMESTAMP=$(date +%Y-%m-%d_%H-%M-%S)
BACKUP_FILE="${BACKUP_DIR}/backup_${TIMESTAMP}.sql"

# Create backup directory if it doesn't exist
mkdir -p "$BACKUP_DIR"

# Ask for confirmation
echo "Do you want to create a database backup? (y/n)"
read -r response

if [[ "$response" =~ ^([yY][eE][sS]|[yY])$ ]]; then
    echo "Creating backup at $BACKUP_FILE..."
    pg_dump -U postgres -h database BETrackingDb > "$BACKUP_FILE"
    
    if [ $? -eq 0 ]; then
        echo "Backup created successfully!"
        echo "Backup file: $BACKUP_FILE"
    else
        echo "Backup failed!"
    fi
else
    echo "Backup skipped."
fi

# Exit instead of keeping the container running
echo "Backup process completed. Exiting..."
exit 0
