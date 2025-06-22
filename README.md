# Bladder Tracker Application

A comprehensive tracking application for monitoring bladder health metrics. This application helps users log and visualize bladder-related events, track patterns, and monitor health improvements over time.

## NOTE

This is still a work in progress, and this readme was AI generated and is for testing at the moment. As in, does it work for you to clone, build and run the app, or...?

SKF 5/3/25

## Tech Stack

- **Frontend**: Angular 17 (Standalone Components) with NgRx for state management and RxJS
- **Styling**: SASS
- **Backend**: ASP.NET Web API (.NET 9) with Minimal APIs
- **Database**: PostgreSQL 15.10
- **ORM**: Entity Framework Core 9.0.4
- **Authentication**: JWT Bearer Tokens
- **Containerization**: Docker with Docker Compose v2
- **Testing**: xUnit for backend, Jasmine/Karma for frontend
- **Deployment**: Azure Container Apps

## Project Structure

```
BladderTracker/
└── appProject/                # Main project directory
    ├── frontend/              # Angular 17 frontend application
    ├── backend/               # .NET 9 API
    ├── database/              # Database scripts and migrations
    ├── .github/               # GitHub Actions workflows
    ├── postmanArtifacts/      # Postman collections and environments
    ├── docker-compose.yml     # Docker Compose v2 configuration for development
    ├── docker-compose.prod.yml # Docker Compose v2 configuration for production
    ├── .env                   # Environment variables (not committed to git)
    ├── .env.example           # Example environment variables template
    ├── azure-container-apps.yaml # Azure Container Apps deployment configuration
    └── azure-container-apps.bicep # Azure Bicep template for infrastructure
```

## Prerequisites

- [Git](https://git-scm.com/)
- [Docker](https://www.docker.com/products/docker-desktop) with WSL2 support
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js](https://nodejs.org/) (v18+) and npm
- [Angular CLI](https://angular.io/cli) (v17)
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli) (for deployment)
- [Visual Studio 2022](https://visualstudio.microsoft.com/vs/) (Community Edition or higher)

## Getting Started

### Clone the Repository

```bash
git clone https://github.com/yourusername/BladderTracker.git
cd BladderTracker/appProject
```

### Environment Setup

1. Create a `.env` file based on the `.env.example` template:

```bash
cp .env.example .env
```

2. Edit the `.env` file with your specific configuration values.

### Build the Application

#### Frontend

```bash
cd frontend
npm install
npm run build
```

#### Backend

```bash
cd backend/trackerApi
dotnet restore
dotnet build
```

### Initialize the Database

1. Start the PostgreSQL container:

```bash
docker compose up -d database
```

2. Run the database migrations:

```bash
cd backend/trackerApi
dotnet ef database update
```

### Add an Admin User

Connect to the PostgreSQL database and execute:

```sql
-- First, ensure the pgcrypto extension is enabled
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- Then create the admin user
INSERT INTO "Users" ("Username", "PasswordHash", "IsAdmin") 
VALUES('admin', crypt('yourpassword', gen_salt('bf')), true);
```

You can connect to the database using:

```bash
docker exec -it database psql -U postgres -d BETrackingDb
```

## Running the Application

### Local Development

Start all services using Docker Compose v2:

```bash
docker compose up -d
```

Access the application:
- Frontend: http://localhost:4200
- Backend API: http://localhost:8080

### Production Environment

For production-like environment locally:

```bash
docker compose -f docker-compose.prod.yml up -d
```

### Running Individual Components

#### Frontend

```bash
cd frontend
npm run start:dev
```

#### Backend API

```bash
cd backend/trackerApi
dotnet run
```

## Testing

### Unit Tests

#### Frontend Tests

```bash
cd frontend
ng test
```

#### Backend Tests

```bash
cd backend/trackerApi
dotnet test
```

### Integration Tests

```bash
cd backend/trackerApi.IntegrationTests
dotnet test
```

Note: Integration tests require a running database instance.

### API Testing with Postman

The repository includes Postman collections in the `postmanArtifacts` directory for testing the API endpoints:

1. Import the collection and environment files into Postman
2. Set up the environment variables
3. Run the collection to test the API endpoints

## Deployment to Azure

The application is configured to deploy to Azure Container Apps with scale-to-zero capability, meaning you only pay when the application is being used.

### Prerequisites

1. Azure subscription
2. GitHub account for CI/CD
3. Azure CLI installed

### Setup Azure Resources

```bash
# Login to Azure
az login

# Create a resource group
az group create --name bladder-tracker-rg --location westus

# Create Azure Container Registry
az acr create --resource-group bladder-tracker-rg --name bladdertracker --sku Basic --admin-enabled true

# Get the ACR credentials
az acr credential show --name bladdertracker
```

### Configure GitHub Secrets

Add the following secrets to your GitHub repository:

- `AZURE_CREDENTIALS`: Service principal credentials for Azure
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

### Deployment Options

#### Option 1: GitHub Actions (CI/CD)

The workflow in `.github/workflows/` will automatically build and deploy the application when you push to the main branch.

#### Option 2: Manual Deployment with Azure CLI

```bash
# Deploy using the Azure Container Apps YAML configuration
az containerapp create --resource-group bladder-tracker-rg --name bladder-tracker --yaml azure-container-apps.yaml

# Or deploy using the Bicep template
az deployment group create --resource-group bladder-tracker-rg --template-file azure-container-apps.bicep --parameters dbPassword=yourSecurePassword
```

## Monitoring and Scaling

Monitor your application in the Azure Portal. The application will automatically:
- Scale to zero when not in use
- Scale up when traffic increases
- Scale down when traffic decreases

## Environment Variables

### Frontend Environment Variables

Create an `environment.ts` file in `frontend/src/environments/`:

```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:8080/api'
};
```

### Backend Environment Variables

The backend uses the following environment variables:

- `ASPNETCORE_ENVIRONMENT`: Set to `Development` or `Production`
- `PG_DATABASE`: PostgreSQL database name
- `PG_USER`: PostgreSQL username
- `PG_PASSWORD`: PostgreSQL password
- `PG_HOST`: PostgreSQL host
- `PG_PORT`: PostgreSQL port
- `JWT_SECRET`: Secret key for JWT token generation
- `JWT_ISSUER`: Issuer for JWT tokens
- `JWT_AUDIENCE`: Audience for JWT tokens
- `JWT_EXPIRY_MINUTES`: Token expiration time in minutes

## API Endpoints

The backend provides the following key API endpoints:

### Authentication

- `POST /api/auth/login`: Authenticate user and get JWT token
- ~~`POST /api/auth/register`: Register a new user~~ (Not implemented yet.)

### Tracking Logs

- `GET /api/tracking-logs`: Get all tracking logs for current user
- `GET /api/tracking-logs/{id}`: Get a specific tracking log
- `POST /api/tracking-logs`: Create a new tracking log
- `PUT /api/tracking-logs/{id}`: Update an existing tracking log
- `DELETE /api/tracking-logs/{id}`: Delete a tracking log

### User Management

- `GET /api/users/profile`: Get current user profile
- `PUT /api/users/profile`: Update user profile

## Troubleshooting

### Common Issues

1. **Database Connection Issues**
   - Ensure PostgreSQL container is running: `docker ps`
   - Check connection string in backend configuration
   - Verify that the database migrations have been applied

2. **Frontend Build Errors**
   - Clear npm cache: `npm cache clean --force`
   - Delete node_modules and reinstall: `rm -rf node_modules && npm install`
   - Ensure you're using Node.js version compatible with Angular 17

3. **Container Startup Issues**
   - Check Docker logs: `docker compose logs`
   - Ensure all required environment variables are set
   - Check if ports are already in use by other applications

4. **JWT Authentication Issues**
   - Verify JWT configuration in backend
   - Check token expiration settings
   - Ensure frontend is correctly sending the Authorization header

5. **WSL2 Integration Issues**
   - Ensure WSL2 is properly configured for Docker
   - Check Docker Desktop settings for WSL2 integration

## Development Workflow

1. **Local Development**
   - Run `docker compose up -d` to start all services
   - Make changes to frontend or backend code
   - Frontend changes will be hot-reloaded
   - For backend changes, restart the backend container: `docker compose restart backend`

2. **Adding New Features**
   - Create feature branches from main
   - Implement and test your changes
   - Submit a pull request for review

3. **Database Schema Changes**
   - Create a new EF Core migration: `dotnet ef migrations add MigrationName`
   - Apply the migration: `dotnet ef database update`
   - Update any affected API endpoints

## License

[MIT](LICENSE)# Deployment ready
