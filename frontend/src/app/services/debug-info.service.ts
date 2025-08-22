import { Injectable, inject } from '@angular/core';
import { Observable, combineLatest, map } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { EnhancedErrorService } from './enhanced-error.service';
import { DebugInfo, PerformanceMetrics, CookieDebugInfo } from '../models/api-error.model';

@Injectable({
  providedIn: 'root'
})
export class DebugInfoService {
  private authService = inject(AuthService);
  private errorService = inject(EnhancedErrorService);

  getComprehensiveDebugInfo(): Observable<DebugInfo> {
    return combineLatest([
      this.errorService.getErrorSummary(),
      this.errorService.getErrorPatterns()
    ]).pipe(
      map(([errorHistory, patterns]) => ({
        user: this.getUserInfo(),
        authentication: this.getAuthenticationInfo(),
        logs: {
          entries: [], // Will be populated by log viewer service
          lastFetched: '',
          totalEntries: 0
        },
        system: this.getSystemInfo(),
        errorHistory: {
          ...errorHistory,
          patterns
        }
      }))
    );
  }

  private getUserInfo(): DebugInfo['user'] {
    try {
      const token = this.authService.getToken();
      let username = null;
      let userId = null;
      let roles: string[] = [];
      let tokenExpiration = null;

      if (token) {
        try {
          const payload = JSON.parse(atob(token.split('.')[1]));
          username = payload.unique_name || payload.name || payload.sub || 'Unknown';
          userId = payload.sub || payload.userId || payload.user_id;
          roles = payload.role ? (Array.isArray(payload.role) ? payload.role : [payload.role]) : [];
          tokenExpiration = payload.exp ? new Date(payload.exp * 1000).toISOString() : null;
        } catch (error) {
          console.warn('Failed to decode token:', error);
        }
      }

      return {
        username,
        isAuthenticated: !!this.authService.getToken(), // Use token presence as sync auth check
        tokenExpiration,
        userId,
        roles
      };
    } catch (error) {
      return {
        username: null,
        isAuthenticated: false,
        tokenExpiration: null,
        userId: null,
        roles: []
      };
    }
  }

  private getAuthenticationInfo(): DebugInfo['authentication'] {
    try {
      const hasAccessToken = !!this.authService.getToken();
      const hasRefreshToken = !!localStorage.getItem('refresh_token');
      
      // Get auth error history from error service
      const errorHistory = this.errorService.getCurrentErrorHistory();
      const authErrors = errorHistory.errors.filter(error => 
        error.statusCode === 401 || error.statusCode === 403 || 
        (error as any).authenticationState !== undefined
      ) as any[];

      // Get last auth attempt from localStorage
      const lastAuthAttempt = localStorage.getItem('last_auth_attempt');
      const lastSuccessfulAuth = localStorage.getItem('last_successful_auth');
      
      // Get token refresh count
      const tokenRefreshCount = parseInt(localStorage.getItem('token_refresh_count') || '0', 10);

      return {
        hasAccessToken,
        hasRefreshToken,
        lastAuthAttempt,
        authErrors,
        tokenRefreshCount,
        lastSuccessfulAuth
      };
    } catch (error) {
      return {
        hasAccessToken: false,
        hasRefreshToken: false,
        lastAuthAttempt: null,
        authErrors: [],
        tokenRefreshCount: 0,
        lastSuccessfulAuth: null
      };
    }
  }

  private getSystemInfo(): DebugInfo['system'] {
    const localStorage = this.getStorageInfo('localStorage');
    const sessionStorage = this.getStorageInfo('sessionStorage');
    const cookieInfo = this.getCookieInfo();
    const performance = this.getPerformanceMetrics();
    const networkStatus = this.getNetworkStatus();

    return {
      userAgent: navigator.userAgent,
      connectionType: this.getConnectionType(),
      timestamp: new Date().toISOString(),
      localStorage,
      sessionStorage,
      cookieInfo,
      networkStatus,
      performance
    };
  }

  private getStorageInfo(storageType: 'localStorage' | 'sessionStorage'): Record<string, any> {
    const info: Record<string, any> = {};
    
    try {
      const storage = storageType === 'localStorage' ? localStorage : sessionStorage;
      
      for (let i = 0; i < storage.length; i++) {
        const key = storage.key(i);
        if (key) {
          try {
            let value = storage.getItem(key);
            
            // Mask sensitive data
            if (key.toLowerCase().includes('token') || 
                key.toLowerCase().includes('password') ||
                key.toLowerCase().includes('secret')) {
              value = '[MASKED]';
            } else if (key.startsWith('bt_')) {
              // Try to parse app-specific data
              try {
                value = JSON.parse(value || '');
              } catch {
                // Keep as string if not JSON
              }
            }
            
            info[key] = value;
          } catch (error) {
            info[key] = '[ERROR]';
          }
        }
      }
    } catch (error) {
      info['error'] = 'Storage not accessible';
    }
    
    return info;
  }

  private getCookieInfo(): CookieDebugInfo {
    try {
      const cookies = document.cookie.split(';').reduce((acc, cookie) => {
        const [name, value] = cookie.trim().split('=');
        acc[name] = value;
        return acc;
      }, {} as Record<string, string>);

      return {
        authCookiePresent: !!cookies['auth_token'] || !!cookies['access_token'],
        authCookieExpiry: this.extractCookieExpiry('auth_token') || this.extractCookieExpiry('access_token'),
        refreshCookiePresent: !!cookies['refresh_token'],
        refreshCookieExpiry: this.extractCookieExpiry('refresh_token'),
        sameSite: 'Lax', // Default assumption, hard to detect
        secure: location.protocol === 'https:',
        httpOnly: false // Can't detect HttpOnly cookies from JS
      };
    } catch {
      return {
        authCookiePresent: false,
        refreshCookiePresent: false,
        sameSite: 'unknown',
        secure: false,
        httpOnly: false
      };
    }
  }

  private extractCookieExpiry(cookieName: string): string | undefined {
    // This is a simplified version - in practice, cookie expiry is hard to detect from JS
    return undefined;
  }

  private getPerformanceMetrics(): PerformanceMetrics {
    try {
      const navigationTiming = performance.getEntriesByType('navigation')[0] as any;
      const paintEntries = performance.getEntriesByType('paint');
      
      let firstContentfulPaint = 0;
      let largestContentfulPaint = 0;
      
      paintEntries.forEach((entry: any) => {
        if (entry.name === 'first-contentful-paint') {
          firstContentfulPaint = entry.startTime;
        }
      });

      // Get LCP if available
      if ('PerformanceObserver' in window) {
        try {
          // This is just a placeholder - actual LCP requires observer
          largestContentfulPaint = firstContentfulPaint * 1.2; // Rough estimate
        } catch {
          // Observer not supported
        }
      }

      const apiResponseTimes = this.getApiResponseTimes();
      const memoryUsage = this.getMemoryUsage();
      const connectionSpeed = this.getConnectionSpeed();

      return {
        pageLoadTime: navigationTiming ? navigationTiming.loadEventEnd - navigationTiming.navigationStart : 0,
        firstContentfulPaint,
        largestContentfulPaint,
        apiResponseTimes,
        memoryUsage,
        connectionSpeed
      };
    } catch {
      return {
        pageLoadTime: 0,
        firstContentfulPaint: 0,
        largestContentfulPaint: 0,
        apiResponseTimes: {},
        memoryUsage: undefined,
        connectionSpeed: 'unknown'
      };
    }
  }

  private getApiResponseTimes(): Record<string, number> {
    try {
      const resourceEntries = performance.getEntriesByType('resource') as PerformanceResourceTiming[];
      const apiTimes: Record<string, number> = {};

      resourceEntries
        .filter(entry => entry.name.includes('/api/'))
        .forEach(entry => {
          const apiPath = new URL(entry.name).pathname;
          apiTimes[apiPath] = entry.responseEnd - entry.requestStart;
        });

      return apiTimes;
    } catch {
      return {};
    }
  }

  private getMemoryUsage(): number | undefined {
    try {
      if ('memory' in performance) {
        return (performance as any).memory?.usedJSHeapSize;
      }
    } catch {
      // Memory API not available
    }
    return undefined;
  }

  private getConnectionSpeed(): PerformanceMetrics['connectionSpeed'] {
    try {
      if ('connection' in navigator) {
        const connection = (navigator as any).connection;
        return connection?.effectiveType || 'unknown';
      }
    } catch {
      // Connection API not available
    }
    return 'unknown';
  }

  private getConnectionType(): string {
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

  private getNetworkStatus(): 'online' | 'offline' | 'unstable' {
    try {
      if (!navigator.onLine) {
        return 'offline';
      }

      // Simple heuristic for unstable connection
      if ('connection' in navigator) {
        const connection = (navigator as any).connection;
        const rtt = connection?.rtt;
        const downlink = connection?.downlink;
        
        if (rtt > 2000 || (downlink && downlink < 0.5)) {
          return 'unstable';
        }
      }

      return 'online';
    } catch {
      return navigator.onLine ? 'online' : 'offline';
    }
  }

  exportDebugInfo(): Promise<string> {
    return new Promise((resolve) => {
      this.getComprehensiveDebugInfo().subscribe(debugInfo => {
        const exportData = {
          ...debugInfo,
          exportTimestamp: new Date().toISOString(),
          version: '1.0.0',
          appName: 'Bladder Event Tracker'
        };
        
        resolve(JSON.stringify(exportData, null, 2));
      });
    });
  }

  async copyDebugInfoToClipboard(): Promise<boolean> {
    try {
      const debugInfo = await this.exportDebugInfo();
      await navigator.clipboard.writeText(debugInfo);
      return true;
    } catch (error) {
      console.error('Failed to copy debug info to clipboard:', error);
      return false;
    }
  }
}