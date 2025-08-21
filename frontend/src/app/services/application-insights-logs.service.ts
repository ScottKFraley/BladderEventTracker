import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable, throwError, of } from 'rxjs';
import { map, catchError, timeout } from 'rxjs/operators';
import { LogEntry } from '../models/api-error.model';

export interface AppInsightsLogQuery {
  timespan?: string; // ISO 8601 duration (e.g., 'PT1H' for 1 hour)
  query: string;
  maxResults?: number;
}

export interface AppInsightsLogResponse {
  tables: Array<{
    name: string;
    columns: Array<{ name: string; type: string }>;
    rows: any[][];
  }>;
}

export interface LogQueryResult {
  logs: LogEntry[];
  totalResults: number;
  queryDuration: number;
  lastFetched: string;
}

@Injectable({
  providedIn: 'root'
})
export class ApplicationInsightsLogsService {
  private http = inject(HttpClient);
  private readonly APP_INSIGHTS_API_BASE = 'https://api.applicationinsights.io/v1/apps';
  private readonly REQUEST_TIMEOUT = 30000; // 30 seconds

  // These would typically come from environment variables
  private readonly appId = 'your-app-insights-app-id'; // TODO: Replace with actual App ID
  private readonly apiKey = 'your-app-insights-api-key'; // TODO: Replace with actual API key

  /**
   * Get recent application logs from Application Insights
   */
  getRecentLogs(hours: number = 1, maxResults: number = 50): Observable<LogQueryResult> {
    const query = this.buildRecentLogsQuery(hours, maxResults);
    return this.executeQuery(query);
  }

  /**
   * Get authentication-related logs
   */
  getAuthenticationLogs(hours: number = 24, maxResults: number = 20): Observable<LogQueryResult> {
    const query = this.buildAuthLogsQuery(hours, maxResults);
    return this.executeQuery(query);
  }

  /**
   * Get error logs only
   */
  getErrorLogs(hours: number = 24, maxResults: number = 30): Observable<LogQueryResult> {
    const query = this.buildErrorLogsQuery(hours, maxResults);
    return this.executeQuery(query);
  }

  /**
   * Get logs by correlation ID
   */
  getLogsByCorrelationId(correlationId: string, hours: number = 6): Observable<LogQueryResult> {
    const query = this.buildCorrelationLogsQuery(correlationId, hours);
    return this.executeQuery(query);
  }

  /**
   * Get performance and timing logs
   */
  getPerformanceLogs(hours: number = 1, maxResults: number = 20): Observable<LogQueryResult> {
    const query = this.buildPerformanceLogsQuery(hours, maxResults);
    return this.executeQuery(query);
  }

  /**
   * Search logs by custom criteria
   */
  searchLogs(searchTerm: string, hours: number = 6, maxResults: number = 30): Observable<LogQueryResult> {
    const query = this.buildSearchQuery(searchTerm, hours, maxResults);
    return this.executeQuery(query);
  }

  private executeQuery(queryConfig: AppInsightsLogQuery): Observable<LogQueryResult> {
    if (!this.appId || !this.apiKey || 
        this.appId === 'your-app-insights-app-id' || 
        this.apiKey === 'your-app-insights-api-key') {
      // Return mock data when App Insights is not configured
      return this.getMockLogs();
    }

    const url = `${this.APP_INSIGHTS_API_BASE}/${this.appId}/query`;
    
    const headers = new HttpHeaders({
      'X-API-Key': this.apiKey,
      'Content-Type': 'application/json'
    });

    let params = new HttpParams().set('query', queryConfig.query);
    
    if (queryConfig.timespan) {
      params = params.set('timespan', queryConfig.timespan);
    }

    const startTime = performance.now();

    return this.http.get<AppInsightsLogResponse>(url, { headers, params }).pipe(
      timeout(this.REQUEST_TIMEOUT),
      map(response => this.parseLogResponse(response, startTime)),
      catchError(error => {
        console.error('Application Insights query failed:', error);
        return this.handleQueryError(error);
      })
    );
  }

  private parseLogResponse(response: AppInsightsLogResponse, startTime: number): LogQueryResult {
    const queryDuration = performance.now() - startTime;
    const logs: LogEntry[] = [];

    if (response.tables && response.tables.length > 0) {
      const table = response.tables[0];
      const columns = table.columns.map(col => col.name);
      
      table.rows.forEach(row => {
        const logEntry = this.createLogEntryFromRow(row, columns);
        if (logEntry) {
          logs.push(logEntry);
        }
      });
    }

    return {
      logs: logs.sort((a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime()),
      totalResults: logs.length,
      queryDuration: Math.round(queryDuration),
      lastFetched: new Date().toISOString()
    };
  }

  private createLogEntryFromRow(row: any[], columns: string[]): LogEntry | null {
    try {
      const getColumnValue = (columnName: string): any => {
        const index = columns.indexOf(columnName);
        return index >= 0 ? row[index] : null;
      };

      const timestamp = getColumnValue('timestamp') || getColumnValue('TimeGenerated') || new Date().toISOString();
      const message = getColumnValue('message') || getColumnValue('Message') || 
                     getColumnValue('customDimensions_message') || 'No message';
      const level = this.parseLogLevel(getColumnValue('severityLevel') || getColumnValue('SeverityLevel') || 'Info');
      const source = this.parseLogSource(getColumnValue('operation_Name') || getColumnValue('OperationName') || 'unknown');

      return {
        timestamp: new Date(timestamp).toISOString(),
        level,
        source,
        message: String(message),
        details: this.extractDetails(row, columns),
        correlationId: getColumnValue('operation_Id') || getColumnValue('OperationId'),
        userId: getColumnValue('user_Id') || getColumnValue('UserId')
      };
    } catch (error) {
      console.warn('Failed to parse log row:', error, row);
      return null;
    }
  }

  private parseLogLevel(severityLevel: any): LogEntry['level'] {
    if (typeof severityLevel === 'number') {
      switch (severityLevel) {
        case 0: return 'debug';
        case 1: return 'info';
        case 2: return 'warn';
        case 3: 
        case 4: return 'error';
        default: return 'info';
      }
    }
    
    const level = String(severityLevel).toLowerCase();
    if (['debug', 'info', 'warn', 'error'].includes(level)) {
      return level as LogEntry['level'];
    }
    
    return 'info';
  }

  private parseLogSource(operationName: string): LogEntry['source'] {
    const name = String(operationName).toLowerCase();
    
    if (name.includes('auth') || name.includes('login') || name.includes('token')) {
      return 'backend';
    }
    if (name.includes('sql') || name.includes('database')) {
      return 'backend';
    }
    if (name.includes('nginx') || name.includes('proxy')) {
      return 'nginx';
    }
    if (name.includes('container') || name.includes('app')) {
      return 'azure';
    }
    
    return 'backend';
  }

  private extractDetails(row: any[], columns: string[]): any {
    const details: any = {};
    
    // Extract custom dimensions and other relevant data
    columns.forEach((column, index) => {
      if (column.startsWith('customDimensions_') || 
          column.startsWith('customMeasurements_') ||
          column.includes('Duration') ||
          column.includes('ResponseCode') ||
          column.includes('RequestId')) {
        const key = column.replace('customDimensions_', '').replace('customMeasurements_', '');
        details[key] = row[index];
      }
    });

    return Object.keys(details).length > 0 ? details : undefined;
  }

  private buildRecentLogsQuery(hours: number, maxResults: number): AppInsightsLogQuery {
    return {
      timespan: `PT${hours}H`,
      query: `
        union traces, exceptions, requests
        | where timestamp > ago(${hours}h)
        | project timestamp, message, severityLevel, operation_Name, operation_Id, user_Id, customDimensions
        | order by timestamp desc
        | limit ${maxResults}
      `
    };
  }

  private buildAuthLogsQuery(hours: number, maxResults: number): AppInsightsLogQuery {
    return {
      timespan: `PT${hours}H`,
      query: `
        union traces, exceptions, requests
        | where timestamp > ago(${hours}h)
        | where operation_Name contains "auth" or operation_Name contains "login" or operation_Name contains "token"
           or message contains "auth" or message contains "login" or message contains "401" or message contains "403"
        | project timestamp, message, severityLevel, operation_Name, operation_Id, user_Id, customDimensions
        | order by timestamp desc
        | limit ${maxResults}
      `
    };
  }

  private buildErrorLogsQuery(hours: number, maxResults: number): AppInsightsLogQuery {
    return {
      timespan: `PT${hours}H`,
      query: `
        union traces, exceptions
        | where timestamp > ago(${hours}h)
        | where severityLevel >= 3 or type == "exceptions"
        | project timestamp, message, severityLevel, operation_Name, operation_Id, user_Id, customDimensions, type
        | order by timestamp desc
        | limit ${maxResults}
      `
    };
  }

  private buildCorrelationLogsQuery(correlationId: string, hours: number): AppInsightsLogQuery {
    return {
      timespan: `PT${hours}H`,
      query: `
        union traces, exceptions, requests
        | where timestamp > ago(${hours}h)
        | where operation_Id == "${correlationId}" or customDimensions.correlationId == "${correlationId}"
        | project timestamp, message, severityLevel, operation_Name, operation_Id, user_Id, customDimensions
        | order by timestamp asc
      `
    };
  }

  private buildPerformanceLogsQuery(hours: number, maxResults: number): AppInsightsLogQuery {
    return {
      timespan: `PT${hours}H`,
      query: `
        requests
        | where timestamp > ago(${hours}h)
        | where name contains "auth" or name contains "tracker" or name contains "login"
        | project timestamp, name, duration, resultCode, operation_Id, user_Id, customDimensions
        | order by timestamp desc
        | limit ${maxResults}
      `
    };
  }

  private buildSearchQuery(searchTerm: string, hours: number, maxResults: number): AppInsightsLogQuery {
    const escapedTerm = searchTerm.replace(/"/g, '\\"');
    return {
      timespan: `PT${hours}H`,
      query: `
        union traces, exceptions, requests
        | where timestamp > ago(${hours}h)
        | where message contains "${escapedTerm}" or operation_Name contains "${escapedTerm}"
           or tostring(customDimensions) contains "${escapedTerm}"
        | project timestamp, message, severityLevel, operation_Name, operation_Id, user_Id, customDimensions
        | order by timestamp desc
        | limit ${maxResults}
      `
    };
  }

  private handleQueryError(error: any): Observable<LogQueryResult> {
    let errorMessage = 'Failed to fetch logs from Application Insights';
    
    if (error.status === 403) {
      errorMessage = 'Access denied to Application Insights. Check API key permissions.';
    } else if (error.status === 404) {
      errorMessage = 'Application Insights app not found. Check App ID.';
    } else if (error.name === 'TimeoutError') {
      errorMessage = 'Query timed out. Try reducing the time range or result limit.';
    } else if (error.error?.error?.message) {
      errorMessage = error.error.error.message;
    }

    // Return error as a log entry for display
    const errorLog: LogEntry = {
      timestamp: new Date().toISOString(),
      level: 'error',
      source: 'frontend',
      message: errorMessage,
      details: { error: error.message, status: error.status }
    };

    return of({
      logs: [errorLog],
      totalResults: 1,
      queryDuration: 0,
      lastFetched: new Date().toISOString()
    });
  }

  private getMockLogs(): Observable<LogQueryResult> {
    // Return mock data when App Insights is not configured
    const mockLogs: LogEntry[] = [
      {
        timestamp: new Date(Date.now() - 5000).toISOString(),
        level: 'info',
        source: 'backend',
        message: 'Authentication successful for user',
        correlationId: 'mock-correlation-1',
        userId: 'user-123'
      },
      {
        timestamp: new Date(Date.now() - 15000).toISOString(),
        level: 'warn',
        source: 'backend',
        message: 'Token refresh attempted',
        correlationId: 'mock-correlation-2',
        userId: 'user-123'
      },
      {
        timestamp: new Date(Date.now() - 30000).toISOString(),
        level: 'error',
        source: 'backend',
        message: 'Database connection timeout',
        details: { duration: '5000ms', retries: 3 }
      },
      {
        timestamp: new Date(Date.now() - 60000).toISOString(),
        level: 'info',
        source: 'nginx',
        message: 'HTTP request processed',
        details: { method: 'POST', path: '/api/auth/login', status: 200 }
      }
    ];

    return of({
      logs: mockLogs,
      totalResults: mockLogs.length,
      queryDuration: 150,
      lastFetched: new Date().toISOString()
    });
  }

  /**
   * Check if Application Insights is properly configured
   */
  isConfigured(): boolean {
    return this.appId !== 'your-app-insights-app-id' && 
           this.apiKey !== 'your-app-insights-api-key' &&
           !!this.appId && !!this.apiKey;
  }

  /**
   * Get configuration status for debugging
   */
  getConfigurationStatus(): { configured: boolean; appId: string; hasApiKey: boolean } {
    return {
      configured: this.isConfigured(),
      appId: this.appId === 'your-app-insights-app-id' ? '[NOT CONFIGURED]' : `${this.appId.substring(0, 8)}...`,
      hasApiKey: this.apiKey !== 'your-app-insights-api-key' && !!this.apiKey
    };
  }
}