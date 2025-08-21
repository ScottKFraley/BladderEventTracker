import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MobileDebugService } from '../services/mobile-debug.service';
import { AuthService } from '../auth/auth.service';
import { TrackingLogService } from '../services/tracking-log.service';
import { DebugInfoService } from '../services/debug-info.service';
import { EnhancedErrorService } from '../services/enhanced-error.service';
import { ApplicationInsightsLogsService, LogQueryResult } from '../services/application-insights-logs.service';
import { Subscription, interval } from 'rxjs';
import { DebugInfo, ErrorPatternAlert, AuthenticationError, LogEntry } from '../models/api-error.model';

@Component({
  selector: 'app-debug',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './debug.component.html',
  styleUrls: ['./debug.component.sass']
})
export class DebugComponent implements OnInit, OnDestroy {
  debugInfo: DebugInfo | null = null;
  lastError: any = null;
  logs: string[] = [];
  isTestingConnection = false;
  connectionTestResult: any = null;
  errorPatterns: ErrorPatternAlert[] = [];
  authErrors: AuthenticationError[] = [];
  showDetailedLogs = false;
  showErrorHistory = false;
  showSystemInfo = false;
  showAppInsightsLogs = false;
  
  // Application Insights logs
  appInsightsLogs: LogEntry[] = [];
  isLoadingLogs = false;
  logsQueryResult: LogQueryResult | null = null;
  selectedLogFilter = 'recent';
  searchTerm = '';
  correlationIdSearch = '';
  
  private subscription = new Subscription();
  private originalConsoleError = console.error;
  private originalConsoleLog = console.log;

  constructor(
    private mobileDebug: MobileDebugService,
    private authService: AuthService,
    private trackingLogService: TrackingLogService,
    private debugInfoService: DebugInfoService,
    private enhancedErrorService: EnhancedErrorService,
    private appInsightsLogsService: ApplicationInsightsLogsService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.refreshDebugInfo();
    
    // Intercept console messages to display on page
    this.interceptConsole();
    
    // Subscribe to error patterns
    const errorPatternsSub = this.enhancedErrorService.getErrorPatterns().subscribe(patterns => {
      this.errorPatterns = patterns;
    });
    
    // Auto-refresh debug info every 10 seconds (reduced frequency for enhanced data)
    const refreshSub = interval(10000).subscribe(() => {
      this.refreshDebugInfo();
    });
    
    this.subscription.add(refreshSub);
    this.subscription.add(errorPatternsSub);
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
    
    // Restore original console methods
    console.error = this.originalConsoleError;
    console.log = this.originalConsoleLog;
  }

  private interceptConsole(): void {
    console.error = (...args: any[]) => {
      this.originalConsoleError.apply(console, args);
      this.addLog('ERROR: ' + args.map(arg => 
        typeof arg === 'object' ? JSON.stringify(arg, null, 2) : String(arg)
      ).join(' '));
    };

    console.log = (...args: any[]) => {
      this.originalConsoleLog.apply(console, args);
      this.addLog('LOG: ' + args.map(arg => 
        typeof arg === 'object' ? JSON.stringify(arg, null, 2) : String(arg)
      ).join(' '));
    };
  }

  private addLog(message: string): void {
    const timestamp = new Date().toLocaleTimeString();
    this.logs.unshift(`[${timestamp}] ${message}`);
    
    // Keep only last 50 log entries
    if (this.logs.length > 50) {
      this.logs = this.logs.slice(0, 50);
    }
  }

  refreshDebugInfo(): void {
    // Get enhanced debug info
    this.debugInfoService.getComprehensiveDebugInfo().subscribe({
      next: (debugInfo) => {
        this.debugInfo = debugInfo;
        this.authErrors = debugInfo.authentication.authErrors;
      },
      error: (error) => {
        console.error('Failed to get debug info:', error);
        // Fallback to mobile debug info
        this.debugInfo = this.mobileDebug.getDebugInfo() as any;
      }
    });
    
    this.lastError = this.mobileDebug.getLastError();
  }

  clearLogs(): void {
    this.logs = [];
    this.addLog('Logs cleared');
  }

  testConnection(): void {
    this.isTestingConnection = true;
    this.connectionTestResult = null;
    this.addLog('Starting connection test...');
    
    this.mobileDebug.testConnection();
    
    // Also test our API endpoints
    const userId = this.authService.getCurrentUserId();
    if (userId) {
      this.trackingLogService.getTrackingLogs(2, userId).subscribe({
        next: (logs) => {
          this.connectionTestResult = {
            success: true,
            message: `API test successful - received ${logs.length} tracking logs`,
            timestamp: new Date().toISOString()
          };
          this.addLog(`API test successful - ${logs.length} logs received`);
          this.isTestingConnection = false;
        },
        error: (error) => {
          this.connectionTestResult = {
            success: false,
            message: error,
            timestamp: new Date().toISOString()
          };
          this.addLog(`API test failed: ${error}`);
          this.isTestingConnection = false;
        }
      });
    } else {
      this.connectionTestResult = {
        success: false,
        message: 'No user ID available for API test',
        timestamp: new Date().toISOString()
      };
      this.isTestingConnection = false;
    }
  }

  testLogin(): void {
    this.addLog('Testing login (using dummy credentials)...');
    
    this.authService.login({ username: 'test', password: 'test' }).subscribe({
      next: () => {
        this.addLog('Login test: Unexpected success');
      },
      error: (error) => {
        this.addLog(`Login test error (expected): ${error}`);
      }
    });
  }

  goToDashboard(): void {
    this.router.navigate(['/dashboard']);
  }

  goToLogin(): void {
    this.router.navigate(['/login']);
  }

  // New enhanced debugging methods
  toggleDetailedLogs(): void {
    this.showDetailedLogs = !this.showDetailedLogs;
  }

  toggleErrorHistory(): void {
    this.showErrorHistory = !this.showErrorHistory;
  }

  toggleSystemInfo(): void {
    this.showSystemInfo = !this.showSystemInfo;
  }

  clearErrorHistory(): void {
    this.enhancedErrorService.clearErrorHistory();
    this.addLog('Error history cleared');
    this.refreshDebugInfo();
  }

  async exportDebugInfo(): Promise<void> {
    try {
      const exported = await this.debugInfoService.exportDebugInfo();
      this.downloadFile(exported, 'bladder-tracker-debug.json', 'application/json');
      this.addLog('Debug info exported successfully');
    } catch (error) {
      this.addLog(`Failed to export debug info: ${error}`);
    }
  }

  async copyDebugInfo(): Promise<void> {
    try {
      const success = await this.debugInfoService.copyDebugInfoToClipboard();
      if (success) {
        this.addLog('Debug info copied to clipboard');
      } else {
        this.addLog('Failed to copy debug info to clipboard');
      }
    } catch (error) {
      this.addLog(`Failed to copy debug info: ${error}`);
    }
  }

  forceTokenRefresh(): void {
    this.addLog('Forcing token refresh...');
    this.authService.refreshToken().subscribe({
      next: () => {
        this.addLog('Token refresh successful');
        this.refreshDebugInfo();
      },
      error: (error) => {
        this.addLog(`Token refresh failed: ${error}`);
      }
    });
  }

  clearAuthTokens(): void {
    this.addLog('Clearing authentication tokens...');
    try {
      localStorage.removeItem('access_token');
      localStorage.removeItem('refresh_token');
      sessionStorage.clear();
      this.addLog('Authentication tokens cleared');
      this.refreshDebugInfo();
    } catch (error) {
      this.addLog(`Failed to clear tokens: ${error}`);
    }
  }

  simulateNetworkError(): void {
    this.addLog('Simulating network error...');
    // Create a fake error for testing
    const fakeError = new Error('Simulated network timeout');
    (fakeError as any).status = 408;
    (fakeError as any).statusText = 'Request Timeout';
    this.enhancedErrorService.logError(fakeError, {
      url: '/api/test/simulation',
      method: 'GET',
      userAgent: navigator.userAgent,
      networkConnection: 'unknown',
      timestamp: new Date().toISOString()
    });
    this.addLog('Network error simulated');
  }

  getTokenExpiration(): string {
    if (!this.debugInfo?.user.tokenExpiration) {
      return 'N/A';
    }
    
    const expiry = new Date(this.debugInfo.user.tokenExpiration);
    const now = new Date();
    const diffMs = expiry.getTime() - now.getTime();
    
    if (diffMs <= 0) {
      return 'Expired';
    }
    
    const diffMinutes = Math.floor(diffMs / (1000 * 60));
    const diffHours = Math.floor(diffMinutes / 60);
    
    if (diffHours > 0) {
      return `${diffHours}h ${diffMinutes % 60}m`;
    } else {
      return `${diffMinutes}m`;
    }
  }

  getErrorSeverityClass(severity: string): string {
    switch (severity?.toLowerCase()) {
      case 'critical': return 'text-danger fw-bold';
      case 'high': return 'text-danger';
      case 'medium': return 'text-warning';
      case 'low': return 'text-info';
      default: return 'text-muted';
    }
  }

  private downloadFile(content: string, filename: string, contentType: string): void {
    const blob = new Blob([content], { type: contentType });
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);
  }

  // Application Insights log viewing methods
  toggleAppInsightsLogs(): void {
    this.showAppInsightsLogs = !this.showAppInsightsLogs;
    if (this.showAppInsightsLogs && this.appInsightsLogs.length === 0) {
      this.loadLogs();
    }
  }

  loadLogs(): void {
    this.isLoadingLogs = true;
    this.addLog(`Loading ${this.selectedLogFilter} logs...`);

    let logObservable;
    
    switch (this.selectedLogFilter) {
      case 'auth':
        logObservable = this.appInsightsLogsService.getAuthenticationLogs(24, 30);
        break;
      case 'errors':
        logObservable = this.appInsightsLogsService.getErrorLogs(24, 50);
        break;
      case 'performance':
        logObservable = this.appInsightsLogsService.getPerformanceLogs(1, 20);
        break;
      case 'search':
        if (!this.searchTerm.trim()) {
          this.addLog('Please enter a search term');
          this.isLoadingLogs = false;
          return;
        }
        logObservable = this.appInsightsLogsService.searchLogs(this.searchTerm, 6, 30);
        break;
      case 'correlation':
        if (!this.correlationIdSearch.trim()) {
          this.addLog('Please enter a correlation ID');
          this.isLoadingLogs = false;
          return;
        }
        logObservable = this.appInsightsLogsService.getLogsByCorrelationId(this.correlationIdSearch, 6);
        break;
      default:
        logObservable = this.appInsightsLogsService.getRecentLogs(1, 50);
    }

    logObservable.subscribe({
      next: (result) => {
        this.logsQueryResult = result;
        this.appInsightsLogs = result.logs;
        this.addLog(`Loaded ${result.logs.length} log entries in ${result.queryDuration}ms`);
        this.isLoadingLogs = false;
        
        // Update debug info with logs
        if (this.debugInfo) {
          this.debugInfo.logs = {
            entries: result.logs,
            lastFetched: result.lastFetched,
            totalEntries: result.totalResults
          };
        }
      },
      error: (error) => {
        this.addLog(`Failed to load logs: ${error.message}`);
        this.isLoadingLogs = false;
      }
    });
  }

  onLogFilterChange(): void {
    if (this.showAppInsightsLogs) {
      this.loadLogs();
    }
  }

  searchLogs(): void {
    if (this.selectedLogFilter !== 'search') {
      this.selectedLogFilter = 'search';
    }
    this.loadLogs();
  }

  searchByCorrelationId(): void {
    if (this.selectedLogFilter !== 'correlation') {
      this.selectedLogFilter = 'correlation';
    }
    this.loadLogs();
  }

  getLogLevelClass(level: string): string {
    switch (level?.toLowerCase()) {
      case 'error': return 'text-danger';
      case 'warn': return 'text-warning';
      case 'info': return 'text-info';
      case 'debug': return 'text-muted';
      default: return 'text-dark';
    }
  }

  getLogSourceBadgeClass(source: string): string {
    switch (source?.toLowerCase()) {
      case 'frontend': return 'badge bg-primary';
      case 'backend': return 'badge bg-success';
      case 'nginx': return 'badge bg-info';
      case 'azure': return 'badge bg-warning';
      default: return 'badge bg-secondary';
    }
  }

  formatLogTimestamp(timestamp: string): string {
    try {
      const date = new Date(timestamp);
      return date.toLocaleString();
    } catch {
      return timestamp;
    }
  }

  formatLogDetails(details: any): string {
    if (!details) return '';
    try {
      return JSON.stringify(details, null, 2);
    } catch {
      return String(details);
    }
  }

  copyLogToClipboard(log: LogEntry): void {
    const logText = `[${log.timestamp}] ${log.level.toUpperCase()} (${log.source}): ${log.message}`;
    navigator.clipboard?.writeText(logText).then(() => {
      this.addLog('Log entry copied to clipboard');
    }).catch(() => {
      this.addLog('Failed to copy log entry');
    });
  }

  exportAppInsightsLogs(): void {
    if (this.appInsightsLogs.length === 0) {
      this.addLog('No logs to export');
      return;
    }

    const exportData = {
      exportTimestamp: new Date().toISOString(),
      queryResult: this.logsQueryResult,
      filter: this.selectedLogFilter,
      searchTerm: this.searchTerm,
      correlationId: this.correlationIdSearch,
      appInsightsConfig: this.appInsightsLogsService.getConfigurationStatus(),
      logs: this.appInsightsLogs
    };

    const content = JSON.stringify(exportData, null, 2);
    this.downloadFile(content, 'app-insights-logs.json', 'application/json');
    this.addLog('Application Insights logs exported');
  }

  refreshLogs(): void {
    if (this.showAppInsightsLogs) {
      this.loadLogs();
    }
  }

  getAppInsightsConfigStatus(): any {
    return this.appInsightsLogsService.getConfigurationStatus();
  }

  isAppInsightsConfigured(): boolean {
    return this.appInsightsLogsService.isConfigured();
  }

  // Removed duplicate method - using enhanced version above

  private showTextToCopy(text: string): void {
    // Create a textarea element to show the text for manual copying
    const textarea = document.createElement('textarea');
    textarea.value = text;
    textarea.style.position = 'absolute';
    textarea.style.top = '0';
    textarea.style.left = '0';
    textarea.style.width = '100%';
    textarea.style.height = '200px';
    textarea.style.zIndex = '1000';
    document.body.appendChild(textarea);
    textarea.select();
    
    this.addLog('Debug info displayed in textarea - please copy manually');
    
    // Remove after 10 seconds
    setTimeout(() => {
      if (document.body.contains(textarea)) {
        document.body.removeChild(textarea);
      }
    }, 10000);
  }
}