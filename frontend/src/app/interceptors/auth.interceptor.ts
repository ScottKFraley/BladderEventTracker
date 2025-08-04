import { HttpInterceptorFn, HttpRequest, HttpHandlerFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../auth/auth.service';
import { Observable, throwError, BehaviorSubject, filter, take, switchMap, catchError, EMPTY, tap } from 'rxjs';

let isRefreshing = false;
let refreshTokenSubject: BehaviorSubject<string | null> = new BehaviorSubject<string | null>(null);
let refreshCount = 0;
const MAX_REFRESH_ATTEMPTS = 2;

export const authInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>, 
  next: HttpHandlerFn
): Observable<any> => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Add Authorization header if token exists
  const token = authService.getToken();
  if (token) {
    req = addToken(req, token);
  }

  return next(req).pipe(
    tap(() => {
      // Reset refresh count on successful requests (except auth endpoints)
      if (!isAuthEndpoint(req.url) && refreshCount > 0) {
        refreshCount = 0;
      }
    }),
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && !isAuthEndpoint(req.url)) {
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
          authService.logout();
          router.navigate(['/login']);
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