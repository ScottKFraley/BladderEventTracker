# Build stage for frontend - use latest LTS for security
FROM node:20-alpine AS frontend-build
WORKDIR /app

# Copy package files first for better caching
COPY frontend/package*.json ./
RUN npm ci

# Copy frontend source and build
COPY frontend/ ./
RUN npm run build

# Final nginx stage  
FROM nginx:alpine

# Copy nginx configuration
COPY nginx/nginx.conf /etc/nginx/nginx.conf

# Copy environment initialization script
COPY nginx/init-env.sh /usr/local/bin/init-env.sh
RUN chmod +x /usr/local/bin/init-env.sh

# Remove default nginx files
RUN rm -rf /usr/share/nginx/html/*

# Copy built Angular app from build stage - note the browser/* pattern
COPY --from=frontend-build /app/dist/bladder-event-tracker/browser /usr/share/nginx/html

# Set proper permissions
RUN chmod -R 755 /usr/share/nginx/html
RUN chown -R nginx:nginx /usr/share/nginx/html

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost/health || exit 1

EXPOSE 80

# Use init script as entrypoint
ENTRYPOINT ["/usr/local/bin/init-env.sh"]
CMD ["nginx", "-g", "daemon off;"]
