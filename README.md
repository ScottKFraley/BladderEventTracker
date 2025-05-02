# Bladder Tracker Application

This application consists of three containers:
- Angular frontend
- Backend API
- PostgreSQL database

## Azure Container Apps Deployment

This application is configured to deploy to Azure Container Apps with scale-to-zero capability, meaning you only pay when the application is being used.

### Prerequisites

1. Azure subscription
2. GitHub account
3. Azure CLI installed (for local deployment)

### Setup Instructions

#### 1. Create Azure Resources

```bash
# Login to Azure
az login

# Create a resource group
az group create --name bladder-tracker-rg --location eastus

# Create Azure Container Registry
az acr create --resource-group bladder-tracker-rg --name bladdertracker --sku Basic --admin-enabled true

# Get the ACR credentials
az acr credential show --name bladdertracker
```

#### 2. Configure GitHub Secrets

Add the following secrets to your GitHub repository:

- `AZURE_CREDENTIALS`: Service principal credentials for Azure (see below)
- `ACR_USERNAME`: Username for Azure Container Registry
- `ACR_PASSWORD`: Password for Azure Container Registry
- `DB_PASSWORD`: Secure password for PostgreSQL database

To create the Azure service principal:

```bash
az ad sp create-for-rbac --name "BladderTrackerApp" --role contributor \
  --scopes /subscriptions/{subscription-id}/resourceGroups/bladder-tracker-rg \
  --sdk-auth
```

Copy the entire JSON output and save it as the `AZURE_CREDENTIALS` secret.

#### 3. Update the YAML Configuration

In the `azure-container-apps.yaml` file, replace:
- `{subscription-id}` with your Azure subscription ID
- `{resource-group}` with `bladder-tracker-rg`
- `{environment-name}` with `bladder-tracker-env`

#### 4. Deploy the Application

Push to the main branch or manually trigger the GitHub Actions workflow.

## Local Development

For local development, use Docker Compose:

```bash
docker-compose up
```

## Monitoring and Scaling

Monitor your application in the Azure Portal. The application will automatically:
- Scale to zero when not in use
- Scale up when traffic increases
- Scale down when traffic decreases