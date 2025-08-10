import { HttpInterceptorFn, HttpRequest, HttpHandlerFn, HttpErrorResponse, HttpResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../auth/auth.service';
import { Observable, throwError, BehaviorSubject, filter, take, switchMap, catchError, EMPTY, tap, timeout } from 'rxjs';

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

  // Mobile compatibility: Add null safety check for authService
  if (!authService || typeof authService.getToken !== 'function') {
    console.warn('AuthService not available or getToken method missing, proceeding without token');
    return next(req).pipe(
      timeout(getTimeoutForRequest(req))
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
      // Log successful requests for debugging
      console.log('HTTP Success:', {
        method: req.method,
        url: req.url,
        status: response instanceof HttpResponse ? response.status : 'unknown',
        userAgent: navigator.userAgent,
        timestamp: new Date().toISOString()
      });
      
      // Reset refresh count on successful requests (except auth endpoints)
      if (!isAuthEndpoint(req.url) && refreshCount > 0) {
        refreshCount = 0;
      }
    }),
    catchError((error: HttpErrorResponse) => {
      // Enhanced logging for timeout errors
      const isTimeout = error.message?.includes('Timeout') || (error as any).name === 'TimeoutError';
      
      console.error('HTTP Interceptor Error:', {
        method: req.method,
        url: req.url,
        status: error.status,
        statusText: error.statusText,
        message: error.message,
        error: error.error,
        isTimeout: isTimeout,
        timeoutUsed: timeoutMs,
        userAgent: navigator.userAgent,
        timestamp: new Date().toISOString()
      });
      
      if (error.status === 401 && !isAuthEndpoint(req.url) && authService) {
        return handle401Error(req, next, authService, router);
      }
      return throwError(() => error);
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
  router: Router
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