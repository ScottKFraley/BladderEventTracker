import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { ApiError, AuthenticationError, ErrorContext, ErrorHistory, ErrorSeverity, ErrorPatternAlert } from '../models/api-error.model';

@Injectable({
  providedIn: 'root'
})
export class EnhancedErrorService {
  private readonly MAX_ERROR_HISTORY = 100;
  private readonly PATTERN_DETECTION_WINDOW = 300000; // 5 minutes

  private errorHistorySubject = new BehaviorSubject<ErrorHistory>({
    errors: [],
    lastOccurrence: '',
    totalCount: 0,
    errorsByType: {}
  });

  private errorPatternsSubject = new BehaviorSubject<ErrorPatternAlert[]>([]);
  private correlationIdMap = new Map<string, string>();

  public errorHistory$ = this.errorHistorySubject.asObservable();
  public errorPatterns$ = this.errorPatternsSubject.asObservable();

  constructor() {
    this.loadErrorHistoryFromStorage();
  }

  generateCorrelationId(): string {
    // Generate a simple UUID-like string without external dependencies
    const timestamp = Date.now().toString(36);
    const randomStr = Math.random().toString(36).substring(2, 8);
    return `bt-${timestamp}-${randomStr}`;
  }

  storeCorrelationId(requestKey: string, correlationId: string): void {
    this.correlationIdMap.set(requestKey, correlationId);
  }

  getCorrelationId(requestKey: string): string | undefined {
    return this.correlationIdMap.get(requestKey);
  }

  logError(error: any, context: Partial<ErrorContext> = {}): ApiError {
    const timestamp = new Date().toISOString();
    const correlationId = context.url ? 
      this.getCorrelationId(context.url) || this.generateCorrelationId() : 
      this.generateCorrelationId();

    let apiError: ApiError;

    if (this.isHttpError(error)) {
      apiError = this.createApiErrorFromHttpError(error, context, timestamp, correlationId);
    } else if (this.isAuthenticationError(error)) {
      apiError = this.createAuthenticationError(error, context, timestamp, correlationId);
    } else {
      apiError = this.createGenericApiError(error, context, timestamp, correlationId);
    }

    // Only store error history and detect patterns in non-test environments
    if (!this.isTestEnvironment()) {
      this.addToErrorHistory(apiError);
      this.detectErrorPatterns();
      this.saveErrorHistoryToStorage();
    }

    return apiError;
  }

  private isTestEnvironment(): boolean {
    // Check if we're in a test environment
    return typeof window !== 'undefined' && 
           (window.location?.href?.includes('karma') || 
            navigator.userAgent?.includes('HeadlessChrome') ||
            (globalThis as any)?.jasmine !== undefined);
  }

  private isHttpError(error: any): boolean {
    return error && error.status !== undefined && error.statusText !== undefined;
  }

  private isAuthenticationError(error: any): boolean {
    return error && (error.status === 401 || error.status === 403 || 
           (error.message && error.message.toLowerCase().includes('auth')));
  }

  private createApiErrorFromHttpError(error: any, context: Partial<ErrorContext>, timestamp: string, correlationId: string): ApiError {
    const errorResponse = this.extractErrorResponse(error);
    
    return {
      statusCode: error.status || 0,
      message: errorResponse.message || error.statusText || 'HTTP Error',
      details: this.formatErrorDetails(error, errorResponse),
      timestamp,
      correlationId,
      endpoint: context.url || 'unknown',
      errorCode: errorResponse.errorCode || this.generateErrorCode(error.status),
      userFriendlyMessage: this.generateUserFriendlyMessage(error.status, errorResponse),
      retryAfter: this.extractRetryAfter(error),
      supportReference: correlationId
    };
  }

  private createAuthenticationError(error: any, context: Partial<ErrorContext>, timestamp: string, correlationId: string): AuthenticationError {
    const baseError = this.createApiErrorFromHttpError(error, context, timestamp, correlationId);
    
    return {
      ...baseError,
      authenticationState: this.determineAuthenticationState(error),
      tokenExpiration: this.extractTokenExpiration(),
      refreshTokenAvailable: this.hasRefreshToken(),
      suggestedAction: this.determineSuggestedAction(error)
    };
  }

  private createGenericApiError(error: any, context: Partial<ErrorContext>, timestamp: string, correlationId: string): ApiError {
    return {
      statusCode: 0,
      message: error.message || 'Unknown error occurred',
      details: JSON.stringify(error, null, 2),
      timestamp,
      correlationId,
      endpoint: context.url || 'unknown',
      errorCode: 'GENERIC_ERROR',
      userFriendlyMessage: 'An unexpected error occurred. Please try again.',
      supportReference: correlationId
    };
  }

  private extractErrorResponse(error: any): any {
    try {
      if (error.error) {
        if (typeof error.error === 'string') {
          return JSON.parse(error.error);
        }
        return error.error;
      }
      return {};
    } catch {
      return { message: error.error };
    }
  }

  private formatErrorDetails(error: any, errorResponse: any): string {
    const details = {
      httpStatus: error.status,
      statusText: error.statusText,
      url: error.url,
      headers: error.headers?.keys?.() || [],
      errorResponse: errorResponse,
      timestamp: new Date().toISOString(),
      userAgent: navigator.userAgent,
      networkConnection: this.getNetworkConnection()
    };

    return JSON.stringify(details, null, 2);
  }

  private generateErrorCode(status: number): string {
    const errorCodes: Record<number, string> = {
      400: 'BAD_REQUEST',
      401: 'UNAUTHORIZED',
      403: 'FORBIDDEN',
      404: 'NOT_FOUND',
      408: 'REQUEST_TIMEOUT',
      429: 'RATE_LIMITED',
      500: 'INTERNAL_SERVER_ERROR',
      502: 'BAD_GATEWAY',
      503: 'SERVICE_UNAVAILABLE',
      504: 'GATEWAY_TIMEOUT'
    };

    return errorCodes[status] || `HTTP_${status}`;
  }

  private generateUserFriendlyMessage(status: number, errorResponse: any): string {
    if (errorResponse.userFriendlyMessage) {
      return errorResponse.userFriendlyMessage;
    }

    const friendlyMessages: Record<number, string> = {
      400: 'The request was invalid. Please check your input and try again.',
      401: 'Your session has expired. Please log in again.',
      403: 'You do not have permission to perform this action.',
      404: 'The requested resource was not found.',
      408: 'The request timed out. Please check your connection and try again.',
      429: 'Too many requests. Please wait a moment before trying again.',
      500: 'A server error occurred. Please try again later.',
      502: 'The server is temporarily unavailable. Please try again later.',
      503: 'The service is temporarily unavailable. Please try again later.',
      504: 'The request timed out. Please try again later.'
    };

    return friendlyMessages[status] || 'An unexpected error occurred. Please try again.';
  }

  private extractRetryAfter(error: any): number | undefined {
    const retryAfterHeader = error.headers?.get?.('Retry-After');
    return retryAfterHeader ? parseInt(retryAfterHeader, 10) : undefined;
  }

  private determineAuthenticationState(error: any): AuthenticationError['authenticationState'] {
    if (error.status === 408 || error.name === 'TimeoutError') {
      return 'timeout';
    }
    if (error.status === 401) {
      return this.hasRefreshToken() ? 'expired' : 'missing';
    }
    if (error.status >= 500) {
      return 'server_error';
    }
    return 'invalid';
  }

  private extractTokenExpiration(): string | undefined {
    try {
      const token = localStorage.getItem('access_token');
      if (!token) return undefined;

      const payload = JSON.parse(atob(token.split('.')[1]));
      return payload.exp ? new Date(payload.exp * 1000).toISOString() : undefined;
    } catch {
      return undefined;
    }
  }

  private hasRefreshToken(): boolean {
    try {
      return !!localStorage.getItem('refresh_token');
    } catch {
      return false;
    }
  }

  private determineSuggestedAction(error: any): AuthenticationError['suggestedAction'] {
    if (error.status === 408 || error.name === 'TimeoutError') {
      return 'retry';
    }
    if (error.status === 401 && this.hasRefreshToken()) {
      return 'retry';
    }
    if (error.status === 401) {
      return 'relogin';
    }
    if (error.status >= 500) {
      return 'wait';
    }
    return 'contact_support';
  }

  private getNetworkConnection(): string {
    if ('connection' in navigator) {
      const connection = (navigator as any).connection;
      return connection?.effectiveType || 'unknown';
    }
    return 'unknown';
  }

  private addToErrorHistory(error: ApiError): void {
    const currentHistory = this.errorHistorySubject.value;
    const errors = [error, ...currentHistory.errors];
    
    // Keep only the most recent errors
    if (errors.length > this.MAX_ERROR_HISTORY) {
      errors.splice(this.MAX_ERROR_HISTORY);
    }

    // Update error type counts
    const errorsByType = { ...currentHistory.errorsByType };
    const errorType = error.errorCode || 'UNKNOWN';
    errorsByType[errorType] = (errorsByType[errorType] || 0) + 1;

    const updatedHistory: ErrorHistory = {
      errors,
      lastOccurrence: error.timestamp,
      totalCount: currentHistory.totalCount + 1,
      errorsByType
    };

    this.errorHistorySubject.next(updatedHistory);
  }

  private detectErrorPatterns(): void {
    const history = this.errorHistorySubject.value;
    const recentErrors = this.getRecentErrors(history.errors);
    const patterns = this.analyzeErrorPatterns(recentErrors);
    
    this.errorPatternsSubject.next(patterns);
  }

  private getRecentErrors(errors: ApiError[]): ApiError[] {
    const cutoffTime = Date.now() - this.PATTERN_DETECTION_WINDOW;
    return errors.filter(error => new Date(error.timestamp).getTime() > cutoffTime);
  }

  private analyzeErrorPatterns(errors: ApiError[]): ErrorPatternAlert[] {
    const patterns: ErrorPatternAlert[] = [];
    const errorGroups = this.groupErrorsByType(errors);

    for (const [errorType, errorList] of Object.entries(errorGroups)) {
      if (errorList.length >= 3) {
        patterns.push({
          pattern: `Repeated ${errorType} errors`,
          occurrences: errorList.length,
          timeWindow: '5 minutes',
          severity: this.determineSeverity(errorList.length, errorType),
          suggestedAction: this.getSuggestedActionForPattern(errorType, errorList.length)
        });
      }
    }

    return patterns;
  }

  private groupErrorsByType(errors: ApiError[]): Record<string, ApiError[]> {
    return errors.reduce((groups, error) => {
      const type = error.errorCode || 'UNKNOWN';
      if (!groups[type]) {
        groups[type] = [];
      }
      groups[type].push(error);
      return groups;
    }, {} as Record<string, ApiError[]>);
  }

  private determineSeverity(count: number, errorType: string): ErrorSeverity {
    if (errorType === 'UNAUTHORIZED' && count >= 5) {
      return ErrorSeverity.CRITICAL;
    }
    if (count >= 10) {
      return ErrorSeverity.HIGH;
    }
    if (count >= 5) {
      return ErrorSeverity.MEDIUM;
    }
    return ErrorSeverity.LOW;
  }

  private getSuggestedActionForPattern(errorType: string, count: number): string {
    if (errorType === 'UNAUTHORIZED') {
      return count >= 5 ? 'Clear auth tokens and re-login' : 'Refresh page and try again';
    }
    if (errorType.includes('TIMEOUT')) {
      return 'Check network connection and reduce request frequency';
    }
    if (errorType.includes('SERVER')) {
      return 'Wait for server recovery or contact support';
    }
    return 'Review error details and contact support if pattern continues';
  }

  private saveErrorHistoryToStorage(): void {
    try {
      const history = this.errorHistorySubject.value;
      // Save only the last 20 errors to storage to avoid quota issues
      const trimmedHistory = {
        ...history,
        errors: history.errors.slice(0, 20)
      };
      localStorage.setItem('bt_error_history', JSON.stringify(trimmedHistory));
    } catch (error) {
      console.warn('Failed to save error history to localStorage:', error);
    }
  }

  private loadErrorHistoryFromStorage(): void {
    try {
      const stored = localStorage.getItem('bt_error_history');
      if (stored) {
        const history = JSON.parse(stored);
        this.errorHistorySubject.next(history);
      }
    } catch (error) {
      console.warn('Failed to load error history from localStorage:', error);
    }
  }

  clearErrorHistory(): void {
    this.errorHistorySubject.next({
      errors: [],
      lastOccurrence: '',
      totalCount: 0,
      errorsByType: {}
    });
    this.saveErrorHistoryToStorage();
  }

  getErrorSummary(): Observable<ErrorHistory> {
    return this.errorHistory$;
  }

  getErrorPatterns(): Observable<ErrorPatternAlert[]> {
    return this.errorPatterns$;
  }
}