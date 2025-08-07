import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class EnvironmentService {
  private config: any = {};

  constructor() {
    // Try to get configuration from window object (injected by nginx)
    if (typeof window !== 'undefined' && (window as any).__env) {
      this.config = (window as any).__env;
    }
  }

  getApplicationInsightsConnectionString(): string {
    return this.config.APPLICATIONINSIGHTS_CONNECTION_STRING || '';
  }

  getApiUrl(): string {
    return this.config.API_URL || '/api';
  }

  isProduction(): boolean {
    return this.config.PRODUCTION === 'true';
  }

  getConfig(): any {
    return this.config;
  }
}