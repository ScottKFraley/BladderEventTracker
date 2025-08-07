import { Injectable } from '@angular/core';
import { ApplicationInsights } from '@microsoft/applicationinsights-web';
import { AngularPlugin } from '@microsoft/applicationinsights-angularplugin-js';
import { Router } from '@angular/router';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ApplicationInsightsService {
  private appInsights: ApplicationInsights;

  constructor(private router: Router) {
    const angularPlugin = new AngularPlugin();
    
    // Get connection string from environment or runtime configuration
    const connectionString = this.getApplicationInsightsConnectionString();
    
    this.appInsights = new ApplicationInsights({
      config: {
        connectionString: connectionString,
        extensions: [angularPlugin],
        extensionConfig: {
          [angularPlugin.identifier]: { router: this.router }
        },
        enableAutoRouteTracking: true,
        enableCorsCorrelation: true,
        enableRequestHeaderTracking: true,
        enableResponseHeaderTracking: true,
        enableAjaxErrorStatusText: true,
        enableAjaxPerfTracking: true,
        enableUnhandledPromiseRejectionTracking: true,
        disableAjaxTracking: false,
        disableFetchTracking: false,
        enableDebug: !environment.production
      }
    });

    this.appInsights.loadAppInsights();
    
    // Set user context
    this.appInsights.setAuthenticatedUserContext('anonymous', 'anonymous', true);
  }

  // Track custom events
  trackEvent(name: string, properties?: { [key: string]: any }, measurements?: { [key: string]: number }): void {
    this.appInsights.trackEvent({ 
      name,
      properties,
      measurements
    });
  }

  // Track page views
  trackPageView(name?: string, uri?: string): void {
    this.appInsights.trackPageView({ name, uri });
  }

  // Track exceptions
  trackException(exception: Error, properties?: { [key: string]: any }): void {
    this.appInsights.trackException({ exception }, properties);
  }

  // Track metrics
  trackMetric(name: string, average: number, properties?: { [key: string]: any }): void {
    this.appInsights.trackMetric({ name, average }, properties);
  }

  // Track dependencies (API calls, etc.)
  trackDependencyData(dependency: {
    id: string;
    name: string;
    duration: number;
    success: boolean;
    responseCode: number;
    resultCode?: number;
    data?: string;
    target?: string;
    type?: string;
  }): void {
    this.appInsights.trackDependencyData(dependency);
  }

  // Authentication specific tracking
  trackLogin(username: string, success: boolean, duration?: number, errorMessage?: string): void {
    const properties = {
      username,
      success: success.toString(),
      userAgent: navigator.userAgent,
      ...(errorMessage && { errorMessage })
    };
    
    const measurements = duration ? { duration } : undefined;
    
    this.trackEvent('UserLogin', properties, measurements);
  }

  trackLogout(username: string): void {
    this.trackEvent('UserLogout', {
      username,
      userAgent: navigator.userAgent
    });
  }

  trackTokenRefresh(success: boolean, duration?: number, errorMessage?: string): void {
    const properties = {
      success: success.toString(),
      userAgent: navigator.userAgent,
      ...(errorMessage && { errorMessage })
    };
    
    const measurements = duration ? { duration } : undefined;
    
    this.trackEvent('TokenRefresh', properties, measurements);
  }

  // Navigation tracking
  trackNavigation(from: string, to: string, duration?: number): void {
    this.trackEvent('Navigation', {
      from,
      to,
      userAgent: navigator.userAgent
    }, duration ? { duration } : undefined);
  }

  // API call tracking
  trackAPICall(endpoint: string, method: string, duration: number, success: boolean, statusCode?: number, errorMessage?: string): void {
    const properties = {
      endpoint,
      method,
      success: success.toString(),
      ...(statusCode && { statusCode: statusCode.toString() }),
      ...(errorMessage && { errorMessage })
    };
    
    this.trackEvent('APICall', properties, { duration });
  }

  // Form submission tracking
  trackFormSubmission(formName: string, success: boolean, validationErrors?: string[]): void {
    const properties = {
      formName,
      success: success.toString(),
      ...(validationErrors && { validationErrors: validationErrors.join(', ') })
    };
    
    this.trackEvent('FormSubmission', properties);
  }

  // Performance tracking
  trackPerformanceMetric(name: string, value: number, properties?: { [key: string]: any }): void {
    this.trackMetric(`Performance.${name}`, value, properties);
  }

  // Set user context when authenticated
  setAuthenticatedUser(userId: string, accountId?: string): void {
    this.appInsights.setAuthenticatedUserContext(userId, accountId, true);
  }

  // Clear user context on logout
  clearAuthenticatedUser(): void {
    this.appInsights.clearAuthenticatedUserContext();
  }

  // Add telemetry initializer for custom properties
  addTelemetryInitializer(telemetryInitializer: (envelope: any) => boolean | void): void {
    this.appInsights.addTelemetryInitializer(telemetryInitializer);
  }

  // Flush telemetry
  flush(): void {
    this.appInsights.flush();
  }

  // Get Application Insights connection string from environment or runtime configuration
  private getApplicationInsightsConnectionString(): string {
    // Try to get from runtime configuration first (e.g., from window object set by server)
    const runtimeConfig = (window as any)?.appConfig?.applicationInsights?.connectionString;
    if (runtimeConfig) {
      return runtimeConfig;
    }
    
    // Try to get from environment variables (if set during build)
    const envConfig = (environment as any)?.applicationInsights?.connectionString;
    if (envConfig) {
      return envConfig;
    }
    
    // Return empty string as fallback (Application Insights will handle gracefully)
    return '';
  }
}