import { HttpInterceptorFn, HttpRequest, HttpHandlerFn, HttpErrorResponse, HttpResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../auth/auth.service';
import { EnhancedErrorService } from '../services/enhanced-error.service';
import { Observable, throwError, BehaviorSubject, filter, take, switchMap, catchError, EMPTY, tap, timeout } from 'rxjs';
import { ErrorContext } from '../models/api-error.model';

let isRefreshing = false;
let refreshTokenSubject: BehaviorSubject<string | null> = new BehaviorSubject<string | null>(null);
let refreshCount = 0;
const MAX_REFRESH_ATTEMPTS = 2;

// Timeout configurations for different types of requests
const TIMEOUT_CONFIG = {
  AUTH_LOGIN: 120000,      // 2 minutes for login (cold start)
  AUTH_REFRESH: 60000,     // 1 minute for token refresh
  AUTH_OTHER: 30000,       // 30 seconds for other auth operations
  API_CALLS: 45000,        // 45 seconds for regular API calls
  WARMUP: 240000           // 4 minutes for warmup (already handled in service)
};

export const authInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>, 
  next: HttpHandlerFn
): Observable<any> => {
  const authService = inject(AuthService);
  const router = inject(Router);
  
  // Only inject enhanced error service in non-test environments to avoid test failures
  let errorService: any = null;
  let correlationId = '';
  
  if (!isTestEnvironment()) {
    try {
      errorService = inject(EnhancedErrorService);
      correlationId = errorService.generateCorrelationId();
      const requestKey = `${req.method}-${req.url}`;
      errorService.storeCorrelationId(requestKey, correlationId);
      
      // Add correlation ID to request headers
      req = req.clone({
        setHeaders: {
          'X-Correlation-ID': correlationId,
          'X-Request-Source': 'angular-app',
          'X-User-Agent': navigator.userAgent
        }
      });
    } catch (error) {
      console.warn('Enhanced error service not available:', error);
    }
  }

  // Mobile compatibility: Add null safety check for authService
  if (!authService || typeof authService.getToken !== 'function') {
    console.warn('AuthService not available or getToken method missing, proceeding without token');
    return next(req).pipe(
      timeout(getTimeoutForRequest(req)),
      catchError((error) => {
        const context = createErrorContext(req, error);
        const enhancedError = errorService.logError(error, context);
        return throwError(() => enhancedError);
      })
    );
  }

  // Add Authorization header if token exists
  const token = authService.getToken();
  if (token) {
    req = addToken(req, token);
  }

  // Apply conditional timeout based on request type
  const timeoutMs = getTimeoutForRequest(req);
  
  return next(req).pipe(
    timeout(timeoutMs),
    tap((response) => {
      // Log successful requests for debugging (reduced verbosity in tests)
      if (!isTestEnvironment()) {
        console.log('HTTP Success:', {
          method: req.method,
          url: req.url,
          status: response instanceof HttpResponse ? response.status : 'unknown',
          correlationId: correlationId,
          userAgent: navigator.userAgent,
          timestamp: new Date().toISOString(),
          responseHeaders: response instanceof HttpResponse ? response.headers.keys() : []
        });
      }
      
      // Reset refresh count on successful requests (except auth endpoints)
      if (!isAuthEndpoint(req.url) && refreshCount > 0) {
        refreshCount = 0;
      }
    }),
    catchError((error: HttpErrorResponse) => {
      // Create enhanced error context
      const context = createErrorContext(req, error, {
        correlationId,
        timeoutUsed: timeoutMs,
        isRefreshing,
        refreshCount
      });

      // Log enhanced error information (only if error service is available)
      let enhancedError = error;
      if (errorService) {
        try {
          enhancedError = errorService.logError(error, context);
          
          // Enhanced logging for debugging (reduced verbosity in tests)
          if (!isTestEnvironment()) {
            console.error('HTTP Interceptor Enhanced Error:', {
              correlationId,
              enhancedError,
              originalError: {
                method: req.method,
                url: req.url,
                status: error.status,
                statusText: error.statusText,
                message: error.message,
                error: error.error,
                headers: error.headers?.keys?.() || []
              },
              context,
              networkInfo: getNetworkInfo(),
              timestamp: new Date().toISOString()
            });
          }
        } catch (logError) {
          console.warn('Error in enhanced error logging:', logError);
          enhancedError = error; // Fallback to original error
        }
      }
      
      if (error.status === 401 && !isAuthEndpoint(req.url) && authService) {
        return handle401Error(req, next, authService, router, errorService || null, correlationId);
      }
      
      return throwError(() => enhancedError);
    })
  );
};

function addToken(request: HttpRequest<any>, token: string): HttpRequest<any> {
  return request.clone({
    setHeaders: {
      Authorization: `Bearer ${token}`
    }
  });
}

function isAuthEndpoint(url: string): boolean {
  const authEndpoints = [
    '/auth/login', 
    '/auth/refresh', 
    '/auth/revoke', 
    '/auth/revoke-all', 
    '/auth/token'
  ];
  // More precise matching to avoid false positives
  return authEndpoints.some(endpoint => 
    url.includes(endpoint) || url.endsWith(endpoint)
  );
}

function handle401Error(
  request: HttpRequest<any>, 
  next: HttpHandlerFn,
  authService: AuthService,
  router: Router,
  errorService: any, // Could be null in tests
  correlationId: string
): Observable<any> {
  // Mobile compatibility: Verify authService is still valid
  if (!authService || typeof authService.refreshToken !== 'function') {
    console.error('AuthService not available for token refresh on mobile');
    if (router && typeof router.navigate === 'function') {
      router.navigate(['/login']);
    }
    return throwError(() => new Error('AuthService not available'));
  }

  // Prevent infinite refresh loops
  if (refreshCount >= MAX_REFRESH_ATTEMPTS) {
    console.warn('Maximum refresh attempts reached, logging out');
    resetRefreshState();
    authService.logout();
    router.navigate(['/login']);
    return throwError(() => new Error('Maximum refresh attempts exceeded'));
  }

  if (!isRefreshing) {
    isRefreshing = true;
    refreshCount++;
    refreshTokenSubject.next(null);

    return authService.refreshToken().pipe(
      switchMap((response: any) => {
        if (!response || !response.token) {
          throw new Error('Invalid refresh token response');
        }
        
        const newToken = response.token;
        isRefreshing = false;
        refreshCount = 0; // Reset on successful refresh
        refreshTokenSubject.next(newToken);
        
        // Retry the original request with new token
        return next(addToken(request, newToken));
      }),
      catchError((refreshError) => {
        console.error('Token refresh failed:', refreshError);
        resetRefreshState();
        
        // Check if refresh token is also expired/invalid
        if (refreshError.status === 401 || refreshError.status === 403) {
          console.log('Refresh token expired, logging out');
          if (authService && typeof authService.logout === 'function') {
            authService.logout();
          }
          if (router && typeof router.navigate === 'function') {
            router.navigate(['/login']);
          }
        }
        
        return throwError(() => refreshError);
      })
    );
  } else {
    // If refresh is in progress, wait for it to complete
    return refreshTokenSubject.pipe(
      filter(token => token !== null),
      take(1),
      switchMap(token => {
        if (!token) {
          return throwError(() => new Error('Token refresh failed'));
        }
        // Retry the original request with the new token
        return next(addToken(request, token));
      }),
      catchError((error) => {
        // If waiting for refresh fails, also handle it gracefully
        console.error('Error while waiting for token refresh:', error);
        return throwError(() => error);
      })
    );
  }
}

function resetRefreshState(): void {
  isRefreshing = false;
  refreshCount = 0;
  refreshTokenSubject.next(null);
}

/**
 * Get appropriate timeout value based on request type
 */
function getTimeoutForRequest(req: HttpRequest<any>): number {
  const url = req.url.toLowerCase();
  
  // Authentication endpoints
  if (url.includes('/auth/login')) {
    return TIMEOUT_CONFIG.AUTH_LOGIN;
  } else if (url.includes('/auth/refresh')) {
    return TIMEOUT_CONFIG.AUTH_REFRESH;
  } else if (url.includes('/auth/')) {
    return TIMEOUT_CONFIG.AUTH_OTHER;
  }
  
  // Warmup endpoint (though service handles its own timeout)
  if (url.includes('/warmup')) {
    return TIMEOUT_CONFIG.WARMUP;
  }
  
  // Default for API calls
  return TIMEOUT_CONFIG.API_CALLS;
}

/**
 * Create enhanced error context for debugging
 */
function createErrorContext(
  req: HttpRequest<any>, 
  error: any, 
  additionalContext: any = {}
): ErrorContext {
  return {
    userAgent: navigator.userAgent,
    url: req.url,
    method: req.method,
    requestHeaders: extractHeaders(req.headers),
    responseHeaders: error.headers ? extractHeaders(error.headers) : undefined,
    networkConnection: getNetworkConnection(),
    timestamp: new Date().toISOString(),
    userId: getCurrentUserId(),
    sessionId: getSessionId(),
    ...additionalContext
  };
}

/**
 * Extract headers safely for debugging
 */
function extractHeaders(headers: any): Record<string, string> {
  const extracted: Record<string, string> = {};
  
  try {
    if (headers && typeof headers.keys === 'function') {
      const keys = headers.keys();
      for (const key of keys) {
        // Mask sensitive headers
        if (key.toLowerCase().includes('authorization')) {
          extracted[key] = '[MASKED]';
        } else {
          extracted[key] = headers.get(key);
        }
      }
    }
  } catch (error) {
    console.warn('Failed to extract headers:', error);
  }
  
  return extracted;
}

/**
 * Get network connection information
 */
function getNetworkConnection(): string {
  try {
    if ('connection' in navigator) {
      const connection = (navigator as any).connection;
      return connection?.effectiveType || 'unknown';
    }
    return navigator.onLine ? 'online' : 'offline';
  } catch {
    return 'unknown';
  }
}

/**
 * Get comprehensive network information for debugging
 */
function getNetworkInfo(): any {
  try {
    const info: any = {
      online: navigator.onLine,
      userAgent: navigator.userAgent,
      language: navigator.language,
      platform: navigator.platform,
      cookieEnabled: navigator.cookieEnabled
    };

    if ('connection' in navigator) {
      const connection = (navigator as any).connection;
      info.connection = {
        effectiveType: connection?.effectiveType,
        downlink: connection?.downlink,
        rtt: connection?.rtt,
        saveData: connection?.saveData
      };
    }

    if ('serviceWorker' in navigator) {
      info.serviceWorkerAvailable = true;
    }

    return info;
  } catch {
    return { error: 'Failed to get network info' };
  }
}

/**
 * Get current user ID for context
 */
function getCurrentUserId(): string | undefined {
  try {
    const token = localStorage.getItem('access_token');
    if (token) {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return payload.sub || payload.userId || payload.user_id;
    }
  } catch {
    // Ignore errors
  }
  return undefined;
}

/**
 * Get session ID for correlation
 */
function getSessionId(): string | undefined {
  try {
    let sessionId = sessionStorage.getItem('bt_session_id');
    if (!sessionId) {
      sessionId = `session-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
      sessionStorage.setItem('bt_session_id', sessionId);
    }
    return sessionId;
  } catch {
    return undefined;
  }
}

/**
 * Check if we're running in a test environment
 */
function isTestEnvironment(): boolean {
  return typeof window !== 'undefined' && 
         (window.location?.href?.includes('karma') || 
          navigator.userAgent?.includes('HeadlessChrome') ||
          (globalThis as any)?.jasmine !== undefined);
}