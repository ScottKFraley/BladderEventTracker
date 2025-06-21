FROM node:20

WORKDIR /workspace

# Install global dependencies
RUN npm install -g @angular/cli

# The rest of the setup (like npm install) is handled by
# your devcontainer.json's postCreateCommand

# Set up non-root user for development
USER node

# Command to keep container running
CMD ["sleep", "infinity"]
