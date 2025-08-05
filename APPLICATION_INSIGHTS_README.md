# Application Insights Integration Guide

This document describes the comprehensive Application Insights monitoring implementation for the Bladder Tracker application.

## Overview

Application Insights has been integrated across the entire application stack:
- **ASP.NET Core Backend**: Custom telemetry, authentication tracking, database performance monitoring
- **Angular Frontend**: Page views, user interactions, authentication flows, API call tracking
- **Infrastructure**: Log Analytics workspace, Application Insights resource via Bicep templates

## Features Implemented

### Backend Monitoring (ASP.NET Core)

#### Authentication Telemetry
- **Login Events**: Success/failure tracking with timing metrics
- **Token Refresh**: Performance and success rate monitoring
- **JWT Validation**: Detailed authentication pipeline tracking
- **User Context**: Authenticated user tracking throughout session

#### Database Performance
- **SaveChanges Tracking**: Async and sync operation timing
- **Query Performance**: Database connection and execution monitoring
- **Error Tracking**: Database exceptions and connectivity issues

#### API Monitoring
- **Request/Response Tracking**: Automatic HTTP request monitoring
- **Dependency Tracking**: External service calls
- **Exception Tracking**: Unhandled exceptions with context

### Frontend Monitoring (Angular)

#### Authentication Flow Tracking
- **Login Performance**: Client-side login timing and success rates
- **Token Refresh**: Automatic refresh monitoring
- **Logout Events**: User session termination tracking
- **Authentication State**: User authentication status changes

#### User Experience Monitoring
- **Page Views**: Automatic route tracking
- **Navigation Performance**: Route transition timing
- **Form Submissions**: Success/failure tracking with validation errors
- **API Call Monitoring**: Frontend-to-backend request tracking

#### Error Tracking
- **Unhandled Exceptions**: JavaScript errors with context
- **HTTP Errors**: Failed API requests with details
- **Performance Metrics**: Custom performance measurements

### Infrastructure Monitoring

#### Azure Resources
- **Application Insights**: Centralized telemetry collection
- **Log Analytics Workspace**: Long-term log storage and querying
- **Container App Integration**: Automatic infrastructure monitoring

## Configuration

### Environment Variables

#### Backend (ASP.NET Core)
```bash
APPLICATIONINSIGHTS_CONNECTION_STRING=InstrumentationKey=xxx;IngestionEndpoint=https://xxx;LiveEndpoint=https://xxx
ConnectionStrings__ApplicationInsights=InstrumentationKey=xxx;IngestionEndpoint=https://xxx;LiveEndpoint=https://xxx
```

#### Frontend (Angular)
Environment variables are injected at runtime via nginx:
```bash
APPLICATIONINSIGHTS_CONNECTION_STRING=InstrumentationKey=xxx;IngestionEndpoint=https://xxx;LiveEndpoint=https://xxx
API_URL=/api
PRODUCTION=true
```

### Application Settings

#### Backend Configuration
- `appsettings.json`: Base configuration with Application Insights connection string placeholder
- `appsettings.Production.json`: Production-specific Application Insights settings

#### Frontend Configuration
- Runtime environment injection via `env.js` (created from `env.template.js`)
- Build-time fallback configuration in environment files

## Custom Telemetry Events

### Authentication Events

#### UserLogin
```typescript
Properties: {
  username: string,
  success: boolean,
  userAgent: string,
  ipAddress: string,
  failureReason?: string
}
Metrics: {
  duration: number,
  tokenGenerationDuration?: number,
  refreshTokenGenerationDuration?: number
}
```

#### TokenRefresh
```typescript
Properties: {
  success: boolean,
  userAgent: string,
  ipAddress: string,
  userId?: string,
  failureReason?: string
}
Metrics: {
  duration: number,
  databaseLookupDuration: number,
  accessTokenGenerationDuration: number,
  refreshTokenGenerationDuration: number,
  tokenRevokeDuration: number
}
```

#### UserLogout
```typescript
Properties: {
  username: string,
  userAgent: string
}
```

### Performance Events

#### Database Operations
```typescript
Dependencies: {
  type: "Database",
  name: "SaveChanges" | "SaveChangesAsync",
  duration: TimeSpan,
  success: boolean
}
```

#### API Calls (Frontend)
```typescript
Properties: {
  endpoint: string,
  method: string,
  success: boolean,
  statusCode?: string,
  errorMessage?: string
}
Metrics: {
  duration: number
}
```

## Deployment

### Using Bicep Templates

1. **Deploy Infrastructure**:
   ```bash
   az deployment group create \
     --resource-group your-rg \
     --template-file azure-container-apps.bicep \
     --parameters sqlAdminPassword=your-password acrPassword=your-acr-password
   ```

2. **Retrieve Connection String**:
   ```bash
   az deployment group show \
     --resource-group your-rg \
     --name your-deployment \
     --query properties.outputs.applicationInsightsConnectionString.value
   ```

### Manual Configuration

If not using Bicep templates:

1. **Create Application Insights**: Create in Azure Portal or via CLI
2. **Configure Connection Strings**: Set environment variables in Container Apps
3. **Update Application Settings**: Ensure backend appsettings reference the connection string

## Monitoring Queries

### Key Performance Indicators

#### Authentication Success Rate
```kusto
customEvents
| where name == "UserLogin"
| summarize 
    TotalAttempts = count(),
    SuccessfulLogins = countif(customDimensions.success == "true"),
    SuccessRate = round(100.0 * countif(customDimensions.success == "true") / count(), 2)
by bin(timestamp, 1h)
```

#### Average Login Duration
```kusto
customEvents
| where name == "UserLogin" and customDimensions.success == "true"
| summarize AvgLoginDuration = avg(customMeasurements.duration)
by bin(timestamp, 1h)
```

#### Database Performance
```kusto
dependencies
| where type == "Database"
| summarize
    AvgDuration = avg(duration),
    MaxDuration = max(duration),
    FailureRate = round(100.0 * countif(success == false) / count(), 2)
by name, bin(timestamp, 1h)
```

#### Token Refresh Performance
```kusto
customEvents
| where name == "TokenRefresh"
| summarize
    TotalRefreshes = count(),
    SuccessfulRefreshes = countif(customDimensions.success == "true"),
    AvgDuration = avg(customMeasurements.duration)
by bin(timestamp, 1h)
```

## Troubleshooting

### Common Issues

#### Connection String Not Found
- Verify environment variables are set in Container Apps
- Check appsettings.json has correct connection string reference
- Ensure Bicep template outputs are correct

#### Frontend Telemetry Not Working
- Verify `env.js` is created and accessible
- Check browser console for Application Insights errors
- Ensure nginx init script is executing properly

#### Missing Authentication Events
- Verify TelemetryClient is injected in authentication endpoints
- Check that ApplicationInsightsService is initialized in Angular
- Ensure user context is being set after successful authentication

### Debug Mode

#### Backend Debug Logging
Set log level to Debug in appsettings:
```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.ApplicationInsights": "Debug"
    }
  }
}
```

#### Frontend Debug Mode
Application Insights debug mode is enabled automatically in non-production environments via:
```typescript
enableDebugExceptions: !this.envService.isProduction()
```

## Performance Considerations

### Sampling
- **Adaptive Sampling**: Enabled by default to manage data volume
- **Fixed-Rate Sampling**: Can be configured if needed
- **Ingestion Sampling**: Additional server-side sampling available

### Data Retention
- **Application Insights**: 30 days (configurable up to 730 days)
- **Log Analytics**: 30 days (configurable)

### Cost Optimization
- Monitor data ingestion volume
- Use sampling to reduce costs
- Configure appropriate retention periods
- Filter out noisy telemetry if needed

## Security Considerations

- Connection strings contain sensitive information - store as secrets
- User identifiers are tracked - ensure compliance with privacy policies
- IP addresses are collected - consider data residency requirements
- Sensitive data is not logged in authentication failures

## Next Steps

1. **Custom Dashboards**: Create Application Insights dashboards for key metrics
2. **Alerts**: Set up alerts for authentication failures, performance degradation
3. **Workbooks**: Create detailed workbooks for troubleshooting
4. **Integration**: Consider integration with other monitoring tools (e.g., Azure Monitor)