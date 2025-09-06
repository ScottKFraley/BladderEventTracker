// auth.service.ts
import { Injectable, Inject } from '@angular/core';
import { TOKEN_REFRESH_THRESHOLD } from './auth.config';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { BehaviorSubject, Observable, throwError, Subscription, timeout } from 'rxjs';
import { tap, catchError } from 'rxjs/operators';
import { Router } from '@angular/router';
// import { environment } from '../../environments/environment';
import { ApiEndpointsService } from '../services/api-endpoints.service';
import { ApplicationInsightsService } from '../services/application-insights.service';

export interface LoginDto {
  username: string;
  password: string;
}

export interface AuthResponse {
  token: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly AUTH_TIMEOUT = 120000; // 2 minutes for Azure SQL cold start
  private readonly TOKEN_REFRESH_TIMEOUT = 60000; // 1 minute for token refresh

  private isAuthenticatedSubject = new BehaviorSubject<boolean>(false);
  private tokenExpiryTimer: any;
  private refreshTimer: any;
  private subscriptions = new Subscription();
  private currentTokenExp: number | null = null; // Cache token expiry from cookie


  constructor(
    private http: HttpClient,
    private router: Router,
    private apiEndpoints: ApiEndpointsService,
    private appInsights: ApplicationInsightsService,
    @Inject(TOKEN_REFRESH_THRESHOLD) private readonly tokenRefreshThreshold: number = 300000
  ) {
    this.checkAuthStatus();
  }

  ngOnDestroy() {
    this.subscriptions.unsubscribe();
    this.stopRefreshTimer();
  }

  stopRefreshTimer() {
    if (this.refreshTimer) {
      clearTimeout(this.refreshTimer);
      this.refreshTimer = null;
    }
    this.subscriptions.unsubscribe();
    this.subscriptions = new Subscription();
  }

  login(credentials: LoginDto): Observable<AuthResponse> {
    const startTime = performance.now();

    return this.http.post<AuthResponse>(
      this.apiEndpoints.getAuthEndpoints().login,
      credentials,
      { withCredentials: true }
    ).pipe(
      timeout(this.AUTH_TIMEOUT),
      tap(response => {
        const duration = performance.now() - startTime;
        this.appInsights.trackLogin(credentials.username, true, duration);
        this.appInsights.setAuthenticatedUser(credentials.username);
        this.handleSuccessfulAuth(response);
      }),
      catchError(error => {
        const duration = performance.now() - startTime;
        
        // Enhanced error handling for timeout scenarios
        let errorMessage = error.message || error;
        if (error.message?.includes('Timeout') || (error as any).name === 'TimeoutError') {
          errorMessage = `Login request timed out after ${this.AUTH_TIMEOUT/1000} seconds. This may be due to database initialization. Please try again.`;
        }
        
        this.appInsights.trackLogin(credentials.username, false, duration, errorMessage);
        return this.handleError(error);
      })
    );
  }

  logout(): void {
    const currentUser = this.getCurrentUserId();
    if (currentUser) {
      this.appInsights.trackLogout(currentUser);
    }

    this.appInsights.clearAuthenticatedUser();
    
    // Clear all cached auth data and timers
    this.currentTokenExp = null;
    this.currentUserId = null;
    this.isAuthenticatedSubject.next(false);
    clearTimeout(this.tokenExpiryTimer);
    this.stopRefreshTimer();
    
    // Note: HTTP cookies will be cleared by the backend when calling revoke endpoint
    this.router.navigate(['/login']);
  }

  isAuthenticated(): Observable<boolean> {
    return this.isAuthenticatedSubject.asObservable();
  }

  getToken(): string | null {
    // With HTTP cookies, we can't directly access the token from JavaScript
    // The token will be automatically included in requests via withCredentials: true
    // For UI purposes, we rely on the authentication state
    return this.isAuthenticatedSubject.value ? 'cookie-based-token' : null;
  }

  refreshToken(): Observable<any> {
    const startTime = performance.now();

    return this.http.post<AuthResponse>(
      this.apiEndpoints.getAuthEndpoints().refresh,
      {},
      { withCredentials: true }
    ).pipe(
      timeout(this.TOKEN_REFRESH_TIMEOUT),
      tap(response => {
        const duration = performance.now() - startTime;
        this.appInsights.trackTokenRefresh(true, duration);
        this.handleSuccessfulAuth(response);
      }),
      catchError(error => {
        const duration = performance.now() - startTime;
        
        // Enhanced error handling for timeout scenarios
        let errorMessage = error.message || error;
        if (error.message?.includes('Timeout') || (error as any).name === 'TimeoutError') {
          errorMessage = `Token refresh timed out after ${this.TOKEN_REFRESH_TIMEOUT/1000} seconds.`;
        }
        
        this.appInsights.trackTokenRefresh(false, duration, errorMessage);
        return this.handleError(error);
      })
    );
  }

  revokeToken(): Observable<any> {
    return this.http.post<any>(
      this.apiEndpoints.getAuthEndpoints().revoke,
      {},
      { withCredentials: true }
    ).pipe(
      timeout(30000), // 30 seconds for revoke operations
      tap(() => this.logout()),
      catchError(this.handleError)
    );
  }

  revokeAllTokens(): Observable<any> {
    return this.http.post<any>(
      this.apiEndpoints.getAuthEndpoints().revokeAll,
      {},
      { withCredentials: true }
    ).pipe(
      timeout(30000), // 30 seconds for revoke operations
      tap(() => this.logout()),
      catchError(this.handleError)
    );
  }

  private handleSuccessfulAuth(response: AuthResponse): void {
    if (response.token) {
      // Extract token expiry and user ID from JWT payload
      try {
        const payload = JSON.parse(atob(response.token.split('.')[1]));
        this.currentTokenExp = payload.exp * 1000; // Convert to milliseconds
        
        // Extract user ID from JWT claims
        this.currentUserId = payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ||
                           payload.nameidentifier ||
                           payload.id ||
                           payload.sub ||
                           null;
        
        console.log('Token data extracted from JWT:', {
          expiry: new Date(this.currentTokenExp).toISOString(),
          userId: this.currentUserId
        });
      } catch (error) {
        console.error('Error decoding JWT token:', error);
        // Fallback to 1 hour from now if we can't decode the token
        this.currentTokenExp = new Date().getTime() + (60 * 60 * 1000);
        this.currentUserId = null;
      }

      this.isAuthenticatedSubject.next(true);
      this.setupTokenExpiryTimer();
    }
  }

  private checkAuthStatus(): void {
    // With HTTP cookies, we can't directly check token validity
    // Instead, we attempt to refresh the token to verify authentication
    this.attemptRefreshTokenLogin();
  }

  private attemptRefreshTokenLogin(): void {
    const subscription = this.refreshToken().subscribe({
      next: (response) => {
        console.log('Successfully refreshed token on startup');
      },
      error: (error) => {
        console.log('No valid refresh token available on startup');
        this.logout();
      }
    });

    this.subscriptions.add(subscription);
  }

  private setupTokenExpiryTimer(): void {
    if (this.currentTokenExp) {
      const now = new Date().getTime();
      const timeUntilExpiry = this.currentTokenExp - now;
      const daysUntilExpiry = Math.round(timeUntilExpiry / (1000 * 60 * 60 * 24));

      console.log('Token expiry info:', {
        currentTime: new Date(now).toISOString(),
        tokenExpiry: new Date(this.currentTokenExp).toISOString(),
        daysUntilExpiry: daysUntilExpiry
      });

      // Clear any existing timers
      this.stopRefreshTimer();

      // For 30-day tokens, only set up refresh timer if token expires within 1 day
      if (timeUntilExpiry > 0 && timeUntilExpiry < (24 * 60 * 60 * 1000)) {
        console.log('Token expires within 24 hours, setting up refresh timer');
        this.refreshTimer = setTimeout(() => {
          this.performAutomaticRefresh();
        }, timeUntilExpiry - this.tokenRefreshThreshold);
      } else {
        console.log('Token valid for', daysUntilExpiry, 'days - no refresh timer needed');
      }
    }
  }

  private performAutomaticRefresh(): void {
    const subscription = this.refreshToken().subscribe({
      next: (response) => {
        console.log('Automatic token refresh successful');
      },
      error: (error) => {
        console.error('Automatic token refresh failed:', error);
        this.logout();
      }
    });

    this.subscriptions.add(subscription);
  }

  private refreshTokenLegacy(): void {
    const subscription = this.http.post<AuthResponse>(
      this.apiEndpoints.getAuthEndpoints().token,
      {}
    ).pipe(
      tap(response => this.handleSuccessfulAuth(response)),
      catchError(error => {
        this.logout();
        return throwError(() => error);
      })
    ).subscribe();

    this.subscriptions.add(subscription);
  }

  // Store user ID when authentication succeeds
  private currentUserId: string | null = null;

  // Decode the JWT in order to have the UserId, which is needed in order 
  // to keep the data boxed in to just the user currently logged in!
  private decodeToken(token: string): any {
    try {
      const payload = token.split('.')[1];
      const decodedPayload = JSON.parse(atob(payload));
      console.log('Decoded token payload:', decodedPayload); // Add this line temporarily

      return decodedPayload;

    } catch (error) {
      console.error('Error decoding token:', error);

      return null;
    }
  }

  getCurrentUserId(): string | null {
    return this.currentUserId;
  }

  private handleError(error: HttpErrorResponse) {
    console.error('Auth Service Error Details:', {
      status: error.status,
      statusText: error.statusText,
      message: error.message,
      url: error.url,
      error: error.error,
      userAgent: navigator.userAgent,
      timestamp: new Date().toISOString()
    });

    let errorMessage = 'An error occurred';
    if (error.error instanceof ErrorEvent) {
      // Client-side error
      errorMessage = `Network error: ${error.error.message}`;
      console.error('Client-side error:', error.error);
    } else {
      // Server-side error - provide detailed information
      console.error('Server response error:', error.error);
      
      if (error.status === 401) {
        errorMessage = 'Invalid credentials';
      } else if (error.status === 0) {
        // Check if this is a timeout error
        if (error.message?.includes('Timeout') || (error as any).name === 'TimeoutError') {
          errorMessage = 'Request timed out. The server may be initializing - please try again in a moment.';
        } else {
          errorMessage = `Network connection failed (Status: ${error.status}). Check internet connection.`;
        }
      } else if (error.status >= 500) {
        errorMessage = `Server error (${error.status}): ${error.statusText || 'Internal server error'}`;
      } else if (error.status >= 400) {
        errorMessage = `Client error (${error.status}): ${error.statusText || 'Bad request'}`;
      } else {
        errorMessage = `Unexpected error (${error.status}): ${error.statusText || 'Unknown error'}`;
      }
      
      // Add error details for mobile debugging
      if (error.error?.message) {
        errorMessage += ` - Details: ${error.error.message}`;
      }
    }
    
    console.error('Final error message:', errorMessage);
    return throwError(() => errorMessage);
  }
}
