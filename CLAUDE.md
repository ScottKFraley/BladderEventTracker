# CLAUDE.md - Bladder Event Tracker Project

## Project Overview

You are a specialized software development expert working on the **Bladder Event Tracker** application, a comprehensive health monitoring system for tracking bladder-related events and patterns. This is a full-stack application designed to help users monitor their bladder health with detailed analytics and reporting capabilities.

**Project Locations**:
* **Windows**: `C:\dev\apps\BladderTracker\appProject\` - Primary development environment
* **WSL2/Ubuntu**: `/mnt/c/dev/apps/BladderTracker/appProject/` - For Docker and Claude Code operations
* **Current Working Directory**: `/home/user/BladderEventTracker` - Claude Code environment

**Repository**: GitHub - ScottKFraley/BladderEventTracker

## Core Technology Stack

### Frontend (Angular 17)
- **Framework**: Angular v17.1.0 (standalone components architecture)
- **Build System**: Angular CLI with `@angular-devkit/build-angular:application` builder
- **Styling**: SASS (required - no other CSS preprocessors)
  - Bootstrap 5.3.3 with Bootswatch themes
  - Component styles use SASS with scoped styling
- **State Management**:
  - NgRx Store v17.1.1 (config, trackingLogs)
  - NgRx Effects for side effects
  - NgRx Store DevTools for debugging
  - RxJS 7.8.0 for reactive programming
- **UI Components**:
  - Angular CDK v17.3.10
  - ng-bootstrap v16.0.0
  - SurveyJS (survey-core, survey-angular-ui, survey-creator-angular v1.12.17)
- **Monitoring**: Application Insights integration (@microsoft/applicationinsights-web, @microsoft/applicationinsights-angularplugin-js)
- **Testing**: Jasmine/Karma with ChromeHeadless
- **Commands**: All Angular commands must be run from the `frontend/` folder
- **Node Version**: v20+ required

### Backend (ASP.NET Core 9)
- **Framework**: ASP.NET Web API with .NET 9.0
- **API Architecture**: Minimal APIs pattern (required) - endpoints organized in `EndPoints/` folder
- **ORM**: Entity Framework Core 9.0.4 with SQL Server provider
- **Authentication**:
  - JWT Bearer Token (Microsoft.AspNetCore.Authentication.JwtBearer v9.0.3)
  - Refresh Token implementation for extended sessions
  - Custom TokenService for token generation and validation
- **Logging**:
  - Serilog v9.0.0 (AspNetCore integration)
  - Application Insights sink (Serilog.Sinks.ApplicationInsights v4.0.0)
  - Console and Debug sinks for development
- **API Documentation**: Swashbuckle/Swagger v7.3.1
- **Testing**: xUnit framework with separate test projects
- **Commands**: All .NET commands run from the `backend/` folder
- **Code Standards**: Always use the latest .NET 9/C# 13 syntax and patterns
  - EF Core check constraints: `entity.ToTable("TableName", ck => ck.HasCheckConstraint(...))`
  - Use modern C# features (records, pattern matching, nullable reference types, etc.)
  - Follow current Microsoft documentation patterns
  - User Secrets for local development (UserSecretsId: dfeb2633-6389-49a5-b75e-38866d846f82)

### Database
- **Type**: Microsoft SQL Server
- **Local Development**: SQL Server Developer Edition (running in Docker via host.docker.internal)
- **Production**: Azure SQL Database (free tier with scale-to-zero capability)
- **Database Name**: BETrackingDb
- **Migrations**: EF Core Code-First migrations (located in `backend/trackerApi/Migrations/`)
- **Connection**: TrustServerCertificate=true, MultipleActiveResultSets=true

### Infrastructure & DevOps
- **Containerization**: Docker with Docker Compose v2 syntax (`docker compose` without hyphen)
  - `docker-compose.yml` - Development configuration with profiles (nginx, frontend, backend, development, production)
  - `docker-compose.prod.yml` - Production-like configuration
  - Container orchestration for backend, frontend (dev), and nginx
- **Development Environment**: Windows with WSL2 Ubuntu v24
- **IDE**: Visual Studio Community Edition 2022, VS Code
- **Cloud Platform**: Microsoft Azure
  - Azure Container Apps for hosting
  - Azure Container Registry (bladdertracker.azurecr.io)
  - Azure SQL Database (serverless, free tier)
  - Azure Application Insights for monitoring
  - Azure Log Analytics for logging
- **CI/CD**: GitHub Actions (.github/workflows/deploy-to-azure.yml)
  - Automated testing (frontend and backend unit tests)
  - Docker image builds (nginx and backend)
  - Bicep template deployment (azure-container-apps.bicep)
  - Integration tests are separate and don't block deployment
- **Monitoring**:
  - Application Insights (connection string-based configuration)
  - Serilog structured logging
  - Custom debug endpoints for troubleshooting

## Development Principles & Workflow

### Code Development Guidelines

1. **No Direct Code Generation**: 
   - Do NOT emit complete code solutions directly
   - Instead, create Claude Code prompts for implementation
   - Provide architectural guidance and implementation strategies

2. **File Creation Protocol**:
   - **Warning Required**: Alert before creating files >20 lines
   - **Explanation Required**: Describe what will be created and ask for permission
   - **Command Format**: Start with `code <file_name>` for easy execution

3. **Code Modification Guidelines**:
   - For small fixes: Show only the changed lines with 3-4 lines of context
   - Add comments at end of changed lines for clarity
   - Reference "above changes" instead of repeating commands
   - Avoid showing entire functions when only a few lines changed

4. **Git Commit Messages**:
   - Always provide as complete git commands
   - Format: `git commit -m "descriptive message"`
   - Follow conventional commit patterns when applicable

### Testing Requirements

#### Frontend Testing
- **Location**: `frontend/` directory
- **Preferred Commands**: Use npm scripts from package.json
  - `npm run test` - Run tests once (headless)
  - `npm run test:ci` - Run tests with coverage
  - `npm test -- --include="**/auth*.spec.ts"` - Run specific tests
- **Script Management**: 
  - **Always check package.json first** for existing test scripts
  - **Add new scripts when appropriate** (e.g., `"test:auth": "ng test --include='**/auth*.spec.ts' --watch=false"`)
  - **Prefer npm scripts over direct CLI commands** in instructions to user
- **Alternative**: Direct Angular CLI (`ng test --watch=false --browsers=ChromeHeadless`)
- **Framework**: Angular testing utilities with Jasmine/Karma
- **Coverage**: Component, service, and integration tests

#### Backend Testing  
- **Location**: `backend/` directory
- **Framework**: xUnit
- **Coverage**: Unit tests, integration tests, API endpoint tests
- **Requirement**: All tests must pass before deployment

### Project Structure Standards

```
/home/user/BladderEventTracker/  (or /mnt/c/dev/apps/BladderTracker/appProject/)
├── frontend/                           # Angular 17 application
│   ├── src/
│   │   ├── app/
│   │   │   ├── admin/                 # Admin configuration components
│   │   │   │   └── admin-config/
│   │   │   ├── auth/                  # Authentication (login, auth service, guards)
│   │   │   │   └── login/
│   │   │   ├── components/            # Shared components
│   │   │   │   └── warm-up/           # Azure cold start warm-up component
│   │   │   ├── dashboard/             # Main dashboard view
│   │   │   ├── dashboard-totals/      # Dashboard totals component
│   │   │   ├── debug/                 # Debug information component
│   │   │   ├── interceptors/          # HTTP interceptors (auth)
│   │   │   ├── models/                # TypeScript interfaces/models
│   │   │   ├── navbar/                # Navigation bar component
│   │   │   ├── services/              # Angular services
│   │   │   ├── state/                 # NgRx state management
│   │   │   │   ├── config/            # Configuration state
│   │   │   │   └── tracking-logs/     # Tracking logs state
│   │   │   ├── survey/                # SurveyJS integration
│   │   │   ├── tracking-log-detail/   # Detail view for logs
│   │   │   ├── app.component.ts       # Root component
│   │   │   ├── app.config.ts          # Application configuration
│   │   │   └── app.routes.ts          # Route definitions
│   │   ├── assets/                    # Static assets
│   │   ├── environments/              # Environment configurations
│   │   └── styles/                    # Global SASS styles
│   ├── package.json                   # Frontend dependencies
│   ├── angular.json                   # Angular CLI configuration
│   ├── proxy.conf.dev.json            # Dev proxy configuration
│   ├── proxy.conf.docker.json         # Docker proxy configuration
│   └── Dockerfile                     # Frontend container definition
├── backend/
│   ├── trackerApi/                    # Main API project
│   │   ├── EndPoints/                 # Minimal API endpoint definitions
│   │   │   ├── AuthenticationEndpoints.cs
│   │   │   ├── RefreshTokenEndpoints.cs
│   │   │   ├── TrackerEndpoints.cs
│   │   │   ├── UserEndpoints.cs
│   │   │   ├── DebugEndpoints.cs
│   │   │   ├── WarmUpEndpoints.cs
│   │   │   └── ITrackerEndpoints.cs
│   │   ├── Services/                  # Business logic services
│   │   │   ├── TokenService.cs
│   │   │   ├── TrackingLogService.cs
│   │   │   ├── UserService.cs
│   │   │   └── ConnectionStringHelper.cs
│   │   ├── Models/                    # Entity models
│   │   │   ├── TrackingLogItem.cs
│   │   │   ├── UserModel.cs
│   │   │   └── RefreshToken.cs
│   │   ├── Migrations/                # EF Core migrations
│   │   ├── Properties/                # Project properties
│   │   ├── btDbContext.cs             # Entity Framework DbContext
│   │   ├── Program.cs                 # Application entry point
│   │   ├── appsettings.json           # Configuration (base)
│   │   ├── appsettings.Development.json
│   │   ├── appsettings.DevVS.json     # Visual Studio dev settings
│   │   ├── appsettings.Production.json
│   │   ├── trackerApi.csproj          # Project file
│   │   ├── Dockerfile                 # Backend container definition
│   │   └── docker-compose.Dev_backend.yml
│   ├── trackerApi.UnitTests/          # Unit test project
│   │   ├── appsettings.unitTests.json
│   │   └── trackerApi.UnitTests.csproj
│   ├── trackerApi.IntegrationTests/   # Integration test project
│   │   ├── appsettings.Testing.json
│   │   └── trackerApi.IntegrationTests.csproj
│   ├── trackerApi.TestUtils/          # Shared test utilities
│   │   └── trackerApi.TestUtils.csproj
│   └── trackerApi.sln                 # Solution file
├── database/                           # Database scripts and utilities
│   ├── init.sql                       # Database initialization
│   ├── backup_container_db.sh         # Backup script
│   ├── read_Jotform_data/             # Data import from Jotform
│   ├── read_onenote_data/             # Data import from OneNote
│   └── read_tally_data/               # Data import from Tally
├── nginx/                              # Nginx reverse proxy configuration
│   └── Dockerfile
├── postmanArtifacts/                   # API testing
│   ├── Contract_Testing.postman_collection.json
│   ├── development.postman_envro.json
│   └── testing.postman_enviro.json
├── .github/workflows/                  # CI/CD workflows
│   └── deploy-to-azure.yml
├── docker-compose.yml                  # Development orchestration
├── docker-compose.prod.yml             # Production orchestration
├── azure-container-apps.bicep          # Azure infrastructure as code
├── CLAUDE.md                           # This file - AI assistant guidance
├── README.md                           # Project documentation
├── .env.example                        # Environment variables template
└── start_be_db_svcs.bat                # Windows batch scripts
```

## Application Domain Context

### Core Functionality
The Bladder Event Tracker application helps users:
- **Log bladder events** with timestamps and detailed metrics
- **Monitor patterns and trends** over time with dashboard visualizations
- **Track detailed metrics** including leak amount (0-3), urgency (0-4), pain level (0-10)
- **Survey-based data collection** using SurveyJS for structured input
- **Dashboard analytics** showing totals and patterns
- **Admin configuration** for system settings
- **Secure authentication** with JWT and refresh tokens
- Generate health reports for medical consultations (Planned)
- Export data for healthcare providers (Planned)

### Key Entities (Implemented)
- **User** (`Models/UserModel.cs`):
  - Username (unique)
  - PasswordHash (BCrypt)
  - IsAdmin flag
  - Authentication and authorization
- **TrackingLogItem** (`Models/TrackingLogItem.cs`):
  - Id (GUID)
  - UserId (foreign key)
  - TimeStamp
  - LeakAmount (0-3, check constraint)
  - Urgency (0-4, check constraint)
  - PainLevel (0-10, check constraint)
  - Additional metadata
- **RefreshToken** (`Models/RefreshToken.cs`):
  - Token value
  - UserId
  - ExpiresAt
  - CreatedAt
  - Used/Revoked flags
  - Extended session management

### API Endpoints (Implemented)
- **Authentication** (`EndPoints/AuthenticationEndpoints.cs`):
  - POST `/api/auth/login` - User login with JWT generation
  - POST `/api/auth/logout` - Token invalidation
- **Refresh Tokens** (`EndPoints/RefreshTokenEndpoints.cs`):
  - POST `/api/refresh-token` - Token refresh
  - POST `/api/refresh-token/revoke` - Revoke refresh token
  - GET `/api/refresh-token/active-count` - Count active tokens
- **Tracking Logs** (`EndPoints/TrackerEndpoints.cs`):
  - GET `/api/tracking-logs` - List all logs for user
  - GET `/api/tracking-logs/{id}` - Get specific log
  - POST `/api/tracking-logs` - Create new log
  - PUT `/api/tracking-logs/{id}` - Update log
  - DELETE `/api/tracking-logs/{id}` - Delete log
- **Users** (`EndPoints/UserEndpoints.cs`):
  - GET `/api/users/profile` - Current user profile
  - PUT `/api/users/profile` - Update profile
- **Debug** (`EndPoints/DebugEndpoints.cs`):
  - GET `/api/debug/connection-test` - Test database connection
  - GET `/api/debug/environment` - Environment information
  - GET `/api/debug/config` - Configuration details
- **Warm-up** (`EndPoints/WarmUpEndpoints.cs`):
  - GET `/api/warmup` - Azure cold start mitigation
  - GET `/api/warmup/detailed` - Detailed warmup with DB check

### Security & Privacy Considerations
- **HIPAA Compliance**: Health data requires special handling
- **Data Encryption**: At rest (Azure SQL TDE) and in transit (HTTPS/TLS 1.2+)
- **Authentication**:
  - BCrypt password hashing
  - JWT Bearer tokens with short expiration (configurable, default 30 days for development)
  - Refresh tokens for extended sessions (separate table with revocation support)
  - Token refresh threshold: 5 minutes (300000ms) before expiration
- **User Consent**: Clear privacy policies and data usage
- **Access Control**:
  - Strict user data isolation (UserId filtering on all queries)
  - IsAdmin flag for administrative functions
  - HTTP interceptors enforce authentication on frontend
- **Secure Configuration**:
  - User Secrets for local development
  - Azure Key Vault integration for production (planned)
  - Secrets passed via GitHub Actions secrets
  - Connection strings with TrustServerCertificate for development

## Architecture Patterns & Key Features

### Backend Architecture
1. **Minimal APIs Pattern**:
   - Endpoints organized in `EndPoints/` folder
   - Each endpoint file implements `ITrackerEndpoints` interface
   - Registration via `MapTrackerEndpoints()` extension method
   - Clear separation of concerns

2. **Service Layer**:
   - `TokenService`: JWT generation, validation, refresh token management
   - `TrackingLogService`: Business logic for tracking logs
   - `UserService`: User profile management
   - `ConnectionStringHelper`: Environment-aware connection string handling

3. **Database Context**:
   - `AppDbContext` (btDbContext.cs) with DbSets for all entities
   - Check constraints defined in `OnModelCreating`
   - GUID primary keys with SQL Server NEWID() defaults
   - Proper index configuration for performance

4. **Logging & Monitoring**:
   - Serilog with multiple sinks (Console, Debug, Application Insights)
   - Structured logging with context enrichment
   - Bootstrap logger for early startup issues
   - Application Insights telemetry correlation

5. **Configuration Management**:
   - Multiple appsettings files for different environments
   - AppSettings section for custom configuration (TimeZoneId)
   - JwtSettings section for token configuration
   - Environment variable overrides in production

### Frontend Architecture
1. **Standalone Components**:
   - Angular 17 standalone architecture (no NgModules)
   - Each component is self-contained with its own imports
   - Component-level route guards and resolvers

2. **State Management**:
   - NgRx Store for global state (config, trackingLogs)
   - Effects for side effects (API calls, error handling)
   - Selectors for derived state
   - Store DevTools for debugging

3. **Authentication Flow**:
   - Login component with form validation
   - Auth service with token storage (localStorage)
   - Auth interceptor adds JWT to requests
   - Auth guard protects routes
   - Automatic token refresh before expiration
   - APP_INITIALIZER for startup auth check

4. **Warm-up Component**:
   - Mitigates Azure Container Apps cold start
   - Calls backend warm-up endpoint
   - Shows progress while warming up
   - Redirects to dashboard when ready
   - Handles timeout scenarios gracefully

5. **Application Insights Integration**:
   - Custom ApplicationInsightsService
   - Angular plugin for automatic tracking
   - Page view tracking
   - Exception tracking
   - Custom event tracking
   - Mobile debug service for troubleshooting

6. **Service Architecture**:
   - `AuthService`: Authentication, token management
   - `TrackingLogService`: CRUD operations for logs
   - `ConfigService`: Application configuration
   - `ApiEndpointsService`: Centralized API endpoint management
   - `SurveyService`: SurveyJS integration
   - `WarmUpService`: Cold start handling
   - `EnvironmentService`: Environment detection
   - `EnhancedErrorService`: Error handling and logging

## Claude Code Integration Patterns

### Problem Analysis Prompts
When encountering issues, create prompts like:
```
Analyze the [specific component/issue] in the Bladder Event Tracker project. 
The issue is: [description]
Please examine the relevant files and provide:
1. Root cause analysis
2. Recommended solution approach
3. Implementation steps
4. Testing strategy
```

### Feature Development Prompts
For new features:
```
Implement [feature name] for the Bladder Event Tracker:
Requirements: [specific requirements]
Technical constraints: [Angular 17, Minimal APIs, etc.]
Please provide:
1. Database schema changes (EF migrations)
2. Backend API endpoints (Minimal API pattern)
3. Frontend components (Angular 17 standalone)
4. Unit tests for both frontend and backend
5. Integration considerations
```

### Testing Enhancement Prompts
For improving test coverage:
```
Enhance testing for the Bladder Event Tracker project:
Focus area: [frontend/backend/integration]
Current gaps: [identified areas]
Please provide:
1. Test strategy and structure
2. Mock data and fixtures
3. Test implementation approach
4. CI/CD integration steps
```

## Development Workflow

### Local Development Setup

#### Prerequisites
1. **Windows Environment**: Primary development on Windows with WSL2
2. **SQL Server**: SQL Server Developer Edition (Docker recommended)
   - Connection string: `Server=host.docker.internal;Database=BETrackingDb;...`
   - Default credentials in appsettings.Development.json
3. **Docker Desktop**: For containerized development
   - Enable WSL2 integration
   - Ensure Docker Compose v2 is available
4. **Node.js**: v20+ for Angular development
5. **.NET SDK**: .NET 9.0 SDK
6. **Visual Studio**: 2022 Community Edition or higher (recommended)

#### Development Workflows

**Option 1: Full Docker Development**
```bash
# Start all services with Docker Compose
docker compose --profile development up -d

# Services started:
# - backend: API on port 5000 and 8080
# - frontend: Dev container (manual start of ng serve)
# Access: http://localhost:4200 (frontend), http://localhost:8080 (API)
```

**Option 2: Hybrid Development (Recommended)**
```bash
# 1. Start backend in Docker (from project root)
docker compose --profile backend up -d

# 2. Run frontend locally (from frontend/)
cd frontend
npm install
npm run start:dev

# Access: http://localhost:4200
# Backend API: http://localhost:8080
```

**Option 3: Visual Studio Development**
```bash
# 1. Start SQL Server in Docker (if not already running)
# 2. Open backend/trackerApi.sln in Visual Studio
# 3. Use appsettings.DevVS.json configuration
# 4. Run frontend separately: npm run start:dev
```

#### Database Migrations
```bash
# From backend/trackerApi directory
dotnet ef migrations add MigrationName
dotnet ef database update

# Or in Package Manager Console (Visual Studio)
Add-Migration MigrationName
Update-Database
```

### Testing Strategy

#### Frontend Testing
```bash
cd frontend

# Run all tests once (CI mode)
npm run test

# Run tests with coverage
npm run test:ci

# Run specific test file (example)
npm test -- --include="**/auth*.spec.ts"

# Watch mode (for development)
ng test
```

#### Backend Testing
```bash
cd backend

# Run all tests
dotnet test

# Run only unit tests (deployment requirement)
dotnet test --filter "Category=Unit"

# Run only integration tests
dotnet test --filter "Category=Integration"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

**Test Structure:**
- **Unit Tests**: `trackerApi.UnitTests/` - Fast, isolated, no external dependencies
- **Integration Tests**: `trackerApi.IntegrationTests/` - Database interactions, slower
- **Test Utilities**: `trackerApi.TestUtils/` - Shared test helpers and fixtures

**CI/CD Test Policy:**
- Unit tests MUST pass for deployment (frontend AND backend)
- Integration tests run separately and don't block deployment
- Test coverage reports uploaded as artifacts

### Deployment Pipeline

#### GitHub Actions Workflow
Location: `.github/workflows/deploy-to-azure.yml`

**Pipeline Stages:**
1. **Test Job** (blocks deployment):
   - Setup Node.js and .NET
   - Install dependencies
   - Run Angular tests (ChromeHeadless with coverage)
   - Run .NET unit tests (Category=Unit)
   - Upload test results as artifacts

2. **Build and Deploy Job** (requires test job):
   - Azure login
   - Build Docker images (nginx, backend)
   - Push to Azure Container Registry
   - Deploy via Bicep template
   - Environment variables from GitHub secrets

**Required GitHub Secrets:**
- `AZURE_CREDENTIALS` - Service principal for Azure
- `ACR_PASSWORD` - Container registry password
- `DB_PASSWORD` - SQL Server admin password
- `JWT_SECRET_KEY` - JWT signing key

#### Manual Deployment
```bash
# 1. Build Docker images
docker build -t bladdertracker.azurecr.io/bladder-tracker/backend:latest ./backend/trackerApi
docker build -t bladdertracker.azurecr.io/bladder-tracker/nginx:latest -f nginx/Dockerfile .

# 2. Push to ACR
az acr login --name bladdertracker
docker push bladdertracker.azurecr.io/bladder-tracker/backend:latest
docker push bladdertracker.azurecr.io/bladder-tracker/nginx:latest

# 3. Deploy with Bicep
az deployment group create \
  --resource-group azure-bladder-tracker \
  --template-file azure-container-apps.bicep \
  --parameters sqlAdminPassword='***' jwtSecretKey='***' imageTag='latest'
```

### Azure Infrastructure

**Resources Created by Bicep:**
- **Container App Environment**: Managed environment for container apps
- **Log Analytics Workspace**: Centralized logging (30-day retention)
- **Application Insights**: APM and monitoring (30-day retention)
- **Azure SQL Server**: SQL Server 12.0 with TLS 1.2
- **Azure SQL Database**: Free tier (32GB, scale-to-zero after 60 min idle)
- **Container App**: Backend API with nginx reverse proxy

**Scale Configuration:**
- **Minimum Replicas**: 0 (scale to zero)
- **Maximum Replicas**: 1 (free tier limitation)
- **Auto-pause**: 60 minutes of inactivity
- **Warm-up**: Frontend calls /api/warmup on startup

### Development Best Practices

1. **Branch Strategy**:
   - `main` branch triggers automatic deployment
   - Feature branches for development
   - Claude Code uses branches: `claude/claude-md-{session-id}`

2. **Commit Messages**:
   - Descriptive commit messages
   - Reference issue numbers when applicable
   - Follow conventional commits pattern

3. **Code Review**:
   - All changes via pull requests
   - Automated tests must pass
   - Review for security implications (health data)

4. **Debugging**:
   - Use Debug endpoints (`/api/debug/*`) for troubleshooting
   - Check Application Insights for production issues
   - Serilog structured logging for detailed diagnostics

5. **Configuration Management**:
   - Use User Secrets for local development
   - Never commit secrets to git
   - Use .env.example as template
   - Azure production secrets via Key Vault or GitHub Secrets

## Response Guidelines

### Preferred Response Patterns

1. **Analysis First**: Always analyze the current state before recommending changes
2. **Incremental Approach**: Break complex changes into manageable steps
3. **Testing Integration**: Include testing strategy in all recommendations
4. **Documentation**: Provide clear explanations and rationale
5. **Security Awareness**: Consider health data privacy in all decisions

### Command Execution Format
**IMPORTANT: Windows Development Environment**
This project is developed on Windows. Provide all commands as copy/paste text for Windows Command Prompt/PowerShell, NOT as bash tool executions.

- **File Operations**: `code <filename>` for file creation
- **Git Operations**: Complete git commands with messages: `git commit -m "message"` (standard format, not heredoc)
- **Docker Commands**: Use `docker compose` (v2 syntax without hyphen)
- **Angular Commands**: From `frontend/` directory with `npm test ...`
- **DotNet Commands**: From `backend/` directory
- **All Commands**: Provide as Windows-compatible copy/paste text, not bash tool execution

### Error Handling Approach
When issues arise:
1. Provide diagnostic steps first
2. Explain the root cause
3. Offer multiple solution approaches
4. Include prevention strategies
5. Reference relevant documentation or best practices

## Health Data Compliance

### Privacy Considerations
- **Data Minimization**: Only collect necessary health information
- **User Control**: Users must control their data access and sharing
- **Anonymization**: Support for data export without personal identifiers
- **Audit Trails**: Log access to sensitive health information

### Technical Implementation
- **Encryption**: All health data encrypted at rest and in transit
- **Access Logging**: Comprehensive audit logs for data access
- **Data Retention**: Configurable retention policies
- **Backup Security**: Encrypted backups with secure key management

## Common Issues & Troubleshooting

### Azure Cold Start Issues
**Problem**: Azure Container Apps scale to zero, causing 20-30 second initial load times

**Solution Implemented**:
- Warm-up endpoints (`/api/warmup`, `/api/warmup/detailed`)
- Frontend warm-up component that calls backend before showing login
- APP_INITIALIZER configuration for startup checks
- Progress indicators during warm-up

### Token Expiration Handling
**Problem**: Users logged out unexpectedly when tokens expire

**Solution Implemented**:
- Refresh token mechanism with separate table
- Automatic token refresh 5 minutes before expiration
- Auth interceptor handles 401 responses
- Graceful degradation to login page

### Database Connection Issues
**Local Development**:
- Ensure SQL Server is running: `docker ps`
- Check connection string in appsettings.Development.json
- Use `host.docker.internal` for Docker-to-host communication
- Debug endpoint: `/api/debug/connection-test`

**Production**:
- Verify Azure SQL firewall rules
- Check Application Insights for connection errors
- Ensure connection string has correct format
- Verify managed identity or SQL authentication

### Docker Compose Issues
**Problem**: Services not starting correctly

**Solutions**:
- Use Docker Compose v2 syntax: `docker compose` (no hyphen)
- Check profile configuration: `docker compose --profile backend up`
- View logs: `docker compose logs backend`
- Rebuild images: `docker compose build --no-cache`

### Frontend Build/Test Issues
**Problem**: Angular tests failing or build errors

**Solutions**:
- Clear npm cache: `npm cache clean --force`
- Delete node_modules: `rm -rf node_modules && npm install`
- Check Node.js version: `node --version` (requires v20+)
- Run tests with verbose logging: `ng test --browsers=ChromeHeadless --watch=false`

### Application Insights Not Logging
**Problem**: No telemetry appearing in Application Insights

**Solutions**:
- Verify connection string in appsettings.json
- Check ApplicationInsights configuration in Program.cs
- Ensure Serilog Application Insights sink is configured
- Allow 2-5 minutes for telemetry to appear
- Check ingestion endpoint accessibility

## Integration Points

### Implemented Azure Services
- **Azure Container Apps**: Hosting for containerized applications
  - Nginx reverse proxy (port 80)
  - Backend API (internal port 5000)
  - Scale-to-zero capability for cost savings
- **Azure SQL Database**: Serverless free tier
  - BETrackingDb database
  - Automatic pause after 60 minutes idle
  - TLS 1.2 encrypted connections
- **Azure Container Registry**: bladdertracker.azurecr.io
  - Private image repository
  - Admin credentials for CI/CD
- **Application Insights**: APM and monitoring
  - Connection string-based configuration
  - Custom events and telemetry
  - Integration with frontend and backend
- **Log Analytics Workspace**: Centralized logging
  - 30-day retention
  - Query language for analysis

### Frontend Integrations
- **SurveyJS**: Dynamic form generation and data collection
  - survey-core v1.12.17
  - survey-angular-ui for Angular components
  - survey-creator-angular for admin config
- **Bootstrap 5.3.3**: UI framework with Bootswatch themes
- **ng-bootstrap**: Angular-specific Bootstrap components
- **Application Insights Web SDK**: Frontend telemetry and error tracking

### External APIs (Planned)
- **Health Monitoring APIs**: Integration with health devices/wearables
- **Notification Services**: Email/SMS for reminders and alerts
- **Export Services**: PDF generation for health reports
- **Backup Services**: Automated data backup to Azure Storage
- **Analytics Services**: Advanced health pattern analysis

## Performance Considerations

### Frontend Optimization (Implemented)
- **Build Configuration**:
  - Production build with optimization and hashing
  - Bundle size budgets: 1.2MB warning, 2MB error for initial
  - Component style budget: 6KB warning, 10KB error
  - Tree-shaking and dead code elimination
- **State Management**:
  - NgRx for efficient state updates
  - Selectors for memoized derived state
  - Effects for side effect management
- **Lazy Loading**: Route-based code splitting (ready for implementation)
- **OnPush Change Detection**: For performance-critical components (planned)
- **Application Insights**: Performance monitoring and user analytics

### Frontend Optimization (Planned)
- **Virtual Scrolling**: For large data lists (CDK virtual-scroll)
- **Service Workers**: For offline capabilities and caching
- **Image Optimization**: Lazy loading and responsive images
- **Bundle Analysis**: Regular bundle size monitoring

### Backend Optimization (Implemented)
- **Database Constraints**: Check constraints at DB level for data integrity
- **GUID Primary Keys**: Distributed ID generation with SQL Server NEWID()
- **Entity Framework**:
  - Proper index configuration
  - Efficient query patterns
  - No-tracking queries where appropriate
- **Minimal APIs**: Lower overhead than traditional controllers
- **Structured Logging**: Serilog with context for debugging
- **Application Insights**: Backend performance monitoring

### Backend Optimization (Planned)
- **Database Indexing**: Add indexes for common query patterns
- **Response Caching**: Cache frequently accessed data
- **Connection Pooling**: Optimize EF Core connection pool settings
- **Pagination**: Implement for tracking logs endpoint
- **Response Compression**: Gzip/Brotli for API responses
- **Rate Limiting**: Protect against abuse

### Monitoring & Analytics
- **Application Insights Integration**:
  - Frontend: Page views, user flows, exceptions
  - Backend: Request telemetry, dependencies, exceptions
  - Custom events for business metrics
- **Structured Logging**:
  - Serilog with multiple sinks
  - Context enrichment for correlation
  - Log levels configured per environment
- **Debug Endpoints**: Real-time troubleshooting in all environments
- **Health Checks**: Database connectivity and service health (planned)

### Cost Optimization
- **Scale-to-Zero**: Azure Container Apps with 60-minute auto-pause
- **Free Tier Database**: Azure SQL free tier (32GB limit)
- **Log Retention**: 30 days to balance cost and debugging needs
- **Container Registry**: Basic SKU for cost efficiency

## Important Notes & Conventions

### Database Important Notes
- **Database Type**: SQL Server (NOT PostgreSQL - README.md is outdated)
- **Primary Keys**: GUID/uniqueidentifier with NEWID() default
- **Timestamps**: DateTime in configured timezone (America/Los_Angeles)
- **Check Constraints**: Defined in OnModelCreating for data validation
- **User Isolation**: All queries filter by UserId for security

### Authentication Flow
1. User logs in → JWT token + Refresh token generated
2. JWT stored in localStorage (frontend)
3. Auth interceptor adds JWT to all API requests
4. Token refresh 5 minutes before expiration
5. Refresh token allows extended sessions without re-login
6. Logout revokes refresh token

### Environment Configuration
- **Development**: appsettings.Development.json, User Secrets
- **DevVS**: appsettings.DevVS.json for Visual Studio development
- **Production**: appsettings.Production.json + Environment variables
- **Testing**: Separate appsettings for unit and integration tests

### Proxy Configuration
Frontend has multiple proxy configurations:
- `proxy.conf.json` - Default
- `proxy.conf.dev.json` - Development environment
- `proxy.conf.docker.json` - Docker environment
Used to proxy `/api/*` requests to backend during development

### File Naming Conventions
- **Backend**:
  - PascalCase for C# files
  - Interfaces prefixed with 'I' (ITokenService)
  - Endpoints suffixed with 'Endpoints' (AuthenticationEndpoints)
  - Services suffixed with 'Service' (TokenService)
- **Frontend**:
  - kebab-case for files (auth.service.ts)
  - PascalCase for classes (AuthService)
  - Interfaces without 'I' prefix
  - Models suffixed with .model.ts
  - State files in separate folders (reducers, effects, actions, selectors)

### Testing Conventions
- **Backend**: Category attribute for test filtering (`[Trait("Category", "Unit")]`)
- **Frontend**: .spec.ts suffix for test files
- **Test Doubles**: Mock services with .mock.ts suffix
- **Test Utilities**: Shared in trackerApi.TestUtils project

### Known Limitations
- **Free Tier Database**: 32GB storage limit, 60-minute auto-pause
- **Container Apps**: Single replica in free tier (no high availability)
- **Cold Starts**: 20-30 seconds after scale-to-zero (mitigated with warm-up)
- **No CDN**: Static assets served directly (consider Azure CDN for production)

### Future Considerations
- **User Registration**: Currently admin-only user creation
- **Password Reset**: No self-service password reset
- **Email Notifications**: No email service integrated
- **Data Export**: No export functionality yet
- **Reports**: Health report generation not implemented
- **Mobile Apps**: Currently web-only (responsive design ready)

---

**Remember**: This is a health-focused application handling sensitive personal data. Always prioritize security, privacy, and data protection in all development decisions. Follow HIPAA guidelines and ensure proper data handling in any new features or changes.
