// auth.service.ts
import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { BehaviorSubject, Observable, throwError } from 'rxjs';
import { tap, catchError } from 'rxjs/operators';
import { Router } from '@angular/router';
import { environment } from '../../environments/environment';


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
  private readonly AUTH_API = '/api/auth';
  private readonly TOKEN_KEY = 'auth_token';
  private readonly TOKEN_EXPIRY_KEY = 'auth_token_expiry';
  private readonly TOKEN_REFRESH_THRESHOLD = 5 * 60 * 1000; // 5 minutes in milliseconds

  private isAuthenticatedSubject = new BehaviorSubject<boolean>(false);
  private tokenExpiryTimer: any;

  constructor(
    private http: HttpClient,
    private router: Router
  ) {
    this.checkAuthStatus();
  }

  // export class YourService {
  //   private apiUrl = environment.apiUrl;

  //   constructor(private http: HttpClient) { }

  //   getData() {
  //     return this.http.get(`${this.apiUrl}/your-endpoint`);
  //   }
  // }

  login(credentials: LoginDto): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.AUTH_API}/login`, credentials)
      .pipe(
        tap(response => this.handleSuccessfulAuth(response)),
        catchError(this.handleError)
      );
  }

  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.TOKEN_EXPIRY_KEY);
    this.isAuthenticatedSubject.next(false);
    clearTimeout(this.tokenExpiryTimer);
    this.router.navigate(['/login']);
  }

  isAuthenticated(): Observable<boolean> {
    return this.isAuthenticatedSubject.asObservable();
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  private handleSuccessfulAuth(response: AuthResponse): void {
    if (response.token) {
      localStorage.setItem(this.TOKEN_KEY, response.token);

      // Set token expiry (example: 1 hour from now)
      const expiry = new Date().getTime() + (60 * 60 * 1000);
      localStorage.setItem(this.TOKEN_EXPIRY_KEY, expiry.toString());

      this.isAuthenticatedSubject.next(true);
      this.setupTokenExpiryTimer();
    }
  }

  private checkAuthStatus(): void {
    const token = this.getToken();
    const expiry = localStorage.getItem(this.TOKEN_EXPIRY_KEY);

    if (token && expiry) {
      const expiryTime = parseInt(expiry, 10);
      const now = new Date().getTime();

      if (now < expiryTime) {
        this.isAuthenticatedSubject.next(true);
        this.setupTokenExpiryTimer();
      } else {
        this.logout();
      }
    }
  }

  private setupTokenExpiryTimer(): void {
    const expiry = localStorage.getItem(this.TOKEN_EXPIRY_KEY);
    if (expiry) {
      const expiryTime = parseInt(expiry, 10);
      const now = new Date().getTime();
      const timeUntilExpiry = expiryTime - now;

      // Refresh token 5 minutes before expiry
      const refreshTime = timeUntilExpiry - this.TOKEN_REFRESH_THRESHOLD;

      if (refreshTime > 0) {
        this.tokenExpiryTimer = setTimeout(() => {
          this.refreshToken();
        }, refreshTime);
      }
    }
  }

  private refreshToken(): void {
    this.http.post<AuthResponse>(`${this.AUTH_API}/token`, {})
      .pipe(
        tap(response => this.handleSuccessfulAuth(response)),
        catchError(error => {
          this.logout();
          return throwError(() => error);
        })
      ).subscribe();
  }

  private handleError(error: HttpErrorResponse) {
    let errorMessage = 'An error occurred';
    if (error.error instanceof ErrorEvent) {
      // Client-side error
      errorMessage = error.error.message;
    } else {
      // Server-side error
      errorMessage = error.status === 401
        ? 'Invalid credentials'
        : 'Server error, please try again later';
    }
    return throwError(() => errorMessage);
  }
}
