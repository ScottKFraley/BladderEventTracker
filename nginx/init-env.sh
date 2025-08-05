#!/bin/sh

# Environment injection script for nginx container
# This script processes the env.template.js file and creates env.js with actual environment variables

# Set default values if environment variables are not set
APPLICATIONINSIGHTS_CONNECTION_STRING=${APPLICATIONINSIGHTS_CONNECTION_STRING:-""}
API_URL=${API_URL:-"/api"}
PRODUCTION=${PRODUCTION:-"true"}

# Process the template file
envsubst '${APPLICATIONINSIGHTS_CONNECTION_STRING},${API_URL},${PRODUCTION}' < /usr/share/nginx/html/assets/env.template.js > /usr/share/nginx/html/assets/env.js

echo "Environment configuration injected:"
echo "  APPLICATIONINSIGHTS_CONNECTION_STRING: [REDACTED]"
echo "  API_URL: $API_URL"
echo "  PRODUCTION: $PRODUCTION"

# Start nginx
exec "$@"