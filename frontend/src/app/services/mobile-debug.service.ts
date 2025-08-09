import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class MobileDebugService {
  
  constructor() {
    // Make debug methods globally available for mobile testing
    (window as any).mobileDebug = {
      getLastError: () => this.getLastError(),
      getNetworkInfo: () => this.getNetworkInfo(),
      getDebugInfo: () => this.getDebugInfo(),
      clearConsole: () => console.clear(),
      testConnection: () => this.testConnection(),
      navigateToDebug: () => window.location.href = '/debug'
    };
    
    console.log('Mobile Debug Service initialized. Use window.mobileDebug in console or navigate to /debug');
  }

  private lastError: any = null;
  private errorHistory: any[] = [];
  
  logError(error: any, context?: string) {
    const errorEntry = {
      error: error,
      context: context,
      timestamp: new Date().toISOString(),
      userAgent: navigator.userAgent,
      url: window.location.href
    };
    
    this.lastError = errorEntry;
    this.errorHistory.unshift(errorEntry);
    
    // Keep only last 20 errors
    if (this.errorHistory.length > 20) {
      this.errorHistory = this.errorHistory.slice(0, 20);
    }
    
    console.error('Mobile Debug - Error logged:', this.lastError);
  }
  
  getErrorHistory() {
    return this.errorHistory;
  }
  
  getLastError() {
    return this.lastError;
  }
  
  getNetworkInfo() {
    const info = {
      online: navigator.onLine,
      userAgent: navigator.userAgent,
      platform: navigator.platform,
      connection: (navigator as any).connection,
      cookieEnabled: navigator.cookieEnabled,
      language: navigator.language
    };
    
    console.log('Network Info:', info);
    return info;
  }
  
  getDebugInfo() {
    const info = {
      timestamp: new Date().toISOString(),
      url: window.location.href,
      localStorage: this.getStorageInfo(),
      sessionStorage: this.getSessionStorageInfo(),
      cookies: document.cookie,
      networkStatus: navigator.onLine,
      userAgent: navigator.userAgent
    };
    
    console.log('Debug Info:', info);
    return info;
  }
  
  private getStorageInfo() {
    try {
      return {
        length: localStorage.length,
        keys: Object.keys(localStorage),
        authToken: localStorage.getItem('auth_token') ? 'present' : 'missing',
        tokenExpiry: localStorage.getItem('auth_token_expiry')
      };
    } catch (e) {
      return { error: 'Cannot access localStorage' };
    }
  }
  
  private getSessionStorageInfo() {
    try {
      return {
        length: sessionStorage.length,
        keys: Object.keys(sessionStorage)
      };
    } catch (e) {
      return { error: 'Cannot access sessionStorage' };
    }
  }
  
  testConnection() {
    console.log('Testing connection...');
    
    fetch(window.location.origin + '/health', { 
      method: 'GET',
      cache: 'no-cache'
    })
    .then(response => {
      console.log('Connection test result:', {
        status: response.status,
        statusText: response.statusText,
        headers: Object.fromEntries(response.headers.entries())
      });
    })
    .catch(error => {
      console.error('Connection test failed:', error);
    });
  }
}