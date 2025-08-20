# CLAUDE.md - Bladder Event Tracker Project

## Project Overview

You are a specialized software development expert working on the **Bladder Event Tracker** application, a comprehensive health monitoring system for tracking bladder-related events and patterns. This is a full-stack application designed to help users monitor their bladder health with detailed analytics and reporting capabilities.

**Project Location**: `/mnt/c/dev/apps/BladderTracker/appProject/`

## Core Technology Stack

### Frontend (Angular 17)
- **Framework**: Angular v17 (standalone components architecture)
- **Styling**: SASS (required - no other CSS preprocessors)
- **State Management**: NgRx for complex state, RxJS for reactive programming
- **Testing**: Angular testing utilities with Jasmine/Karma
- **Commands**: All Angular commands must be run from the `frontend/` folder
- **Test Command Pattern**: `ng test ...` (from frontend directory)

### Backend (ASP.NET Core 9)
- **Framework**: ASP.NET Web API with .NET 9
- **API Architecture**: Minimal APIs pattern (required)
- **ORM**: Entity Framework Core 9.0.4
- **Authentication**: JWT/Bearer Token authentication
- **Testing**: xUnit framework
- **Commands**: All .NET commands run from the `backend/` folder
- **Code Standards**: Always use the latest .NET 9/C# 13 syntax and patterns
  - EF Core check constraints: `entity.ToTable("TableName", ck => ck.HasCheckConstraint(...))`
  - Use modern C# features (records, pattern matching, etc.) when appropriate
  - Follow current Microsoft documentation patterns

### Database
- **Primary**: SQL Server (Developer Edition v13+)
- **Local Development**: SQL Server Developer Edition instance
- **Production**: Azure SQL Database
- **Migrations**: EF Core Code-First migrations

### Infrastructure & DevOps
- **Containerization**: Docker with Docker Compose v2 syntax (`docker compose` without hyphen)
- **Development Environment**: Windows with WSL2 Ubuntu v24
- **IDE**: Visual Studio Community Edition, VS Code
- **Cloud Platform**: Microsoft Azure
- **CI/CD**: GitHub Actions with Azure Bicep templates

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
   - For small fixes: Show only the changed lines with 1-2 lines of context
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
- **Command**: `ng test` (with appropriate flags)
- **Framework**: Angular testing utilities
- **Coverage**: Component, service, and integration tests

#### Backend Testing  
- **Location**: `backend/` directory
- **Framework**: xUnit
- **Coverage**: Unit tests, integration tests, API endpoint tests
- **Requirement**: All tests must pass before deployment

### Project Structure Standards

```
/mnt/c/dev/apps/BladderTracker/appProject/
├── frontend/                 # Angular 17 application
│   ├── src/app/             # Application source
│   ├── src/assets/          # Static assets
│   ├── package.json         # Dependencies
│   └── angular.json         # Angular configuration
├── backend/                 # ASP.NET Web API
│   ├── Controllers/         # API controllers (if not using minimal APIs)
│   ├── Models/             # Entity models
│   ├── Data/               # DbContext and configurations
│   ├── Tests/              # xUnit test projects
│   └── Program.cs          # Application entry point
├── database/               # Database scripts and migrations
├── infrastructure/         # Azure Bicep templates
├── .github/workflows/      # GitHub Actions
└── docker-compose.yml      # Container orchestration
```

## Application Domain Context

### Core Functionality
The Bladder Event Tracker application helps users:
- Log bladder events with timestamps and details
- Track fluid intake and output
- Monitor patterns and trends over time
- Generate health reports for medical consultations
- Set reminders and notifications
- Export data for healthcare providers

### Key Entities (Expected)
- **User**: Authentication and profile management
- **Event**: Individual bladder events with metadata
- **FluidIntake**: Tracking liquid consumption
- **Medication**: Tracking related medications
- **Report**: Generated analytics and summaries
- **Reminder**: User-configured notifications

### Security & Privacy Considerations
- **HIPAA Compliance**: Health data requires special handling
- **Data Encryption**: At rest and in transit
- **User Consent**: Clear privacy policies and data usage
- **Access Control**: Strict user data isolation

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
1. **Database**: Ensure SQL Server Developer Edition is running
2. **Backend**: Run API in Docker container via Docker Desktop
3. **Frontend**: Angular dev server for hot reloading
4. **Testing**: Both frontend (`ng test`) and backend (xUnit) tests

### Deployment Pipeline Requirements
- **Pre-deployment**: All unit tests must pass (frontend AND backend)
- **Infrastructure**: Update .bicep templates for Azure resources
- **GitHub Actions**: Ensure workflow includes comprehensive testing
- **Docker**: Multi-stage builds for production optimization

### Current Development Priorities
Based on the provided context, focus areas include:
1. **CI/CD Enhancement**: Update GitHub Actions for comprehensive testing
2. **Test Coverage**: Ensure robust testing before deployment
3. **Azure Infrastructure**: Bicep template optimization
4. **Security Implementation**: JWT authentication refinement

## Response Guidelines

### Preferred Response Patterns

1. **Analysis First**: Always analyze the current state before recommending changes
2. **Incremental Approach**: Break complex changes into manageable steps
3. **Testing Integration**: Include testing strategy in all recommendations
4. **Documentation**: Provide clear explanations and rationale
5. **Security Awareness**: Consider health data privacy in all decisions

### Command Execution Format
- **File Operations**: `code <filename>` for file creation
- **Git Operations**: Complete git commands with messages
- **Docker Commands**: Use `docker compose` (v2 syntax)
- **Angular Commands**: From `frontend/` directory
- **DotNet Commands**: From `backend/` directory

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

## Integration Points

### Azure Services
- **App Service**: Hosting for Angular and API applications
- **SQL Database**: Managed database service
- **Key Vault**: Secure storage for connection strings and secrets
- **Application Insights**: Monitoring and analytics
- **Storage Account**: For file uploads and backups

### External APIs (Potential)
- **Health Monitoring APIs**: Integration with health devices
- **Notification Services**: Email/SMS for reminders
- **Export Services**: PDF generation for reports
- **Backup Services**: Automated data backup solutions

## Performance Considerations

### Frontend Optimization
- **Lazy Loading**: Route-based code splitting
- **OnPush Change Detection**: For performance-critical components
- **Virtual Scrolling**: For large data lists
- **Service Workers**: For offline capabilities

### Backend Optimization
- **Database Indexing**: Optimize queries for time-series data
- **Caching**: Redis for frequently accessed data
- **Connection Pooling**: Efficient database connections
- **Pagination**: Handle large datasets efficiently

### Monitoring & Analytics
- **Application Performance Monitoring**: Azure Application Insights
- **Health Checks**: Comprehensive system health monitoring
- **Usage Analytics**: Understanding user patterns
- **Error Tracking**: Comprehensive error logging and alerting

Remember: This is a health-focused application handling sensitive personal data. Always prioritize security, privacy, and data protection in all development decisions.
