import { ApplicationConfig, APP_INITIALIZER } from '@angular/core';
import { provideRouter, Router } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { routes } from './app.routes';
import { authInterceptor } from './interceptors';
import { provideStore } from '@ngrx/store';
import { provideEffects } from '@ngrx/effects';
import { provideStoreDevtools } from '@ngrx/store-devtools';
import { configReducer } from './state/config/config.reducer';
import { ConfigEffects } from './state/config/config.effects';
import { trackingLogReducer } from './state/tracking-logs/tracking-log.reducer';
import { TrackingLogEffects } from './state/tracking-logs/tracking-log.effects';
import { TOKEN_REFRESH_THRESHOLD } from './auth/auth.config';
import { AuthService } from './auth/auth.service';
import { ApplicationInsightsService } from './services/application-insights.service';
import { take } from 'rxjs/operators';

export const appConfig: ApplicationConfig = {
  providers: [
    { provide: TOKEN_REFRESH_THRESHOLD, useValue: 300000 },
    {
      provide: APP_INITIALIZER,
      useFactory: (appInsights: ApplicationInsightsService) => {
        return () => {
          // Initialize Application Insights
          console.log('Initializing Application Insights...');
          return Promise.resolve();
        };
      },
      deps: [ApplicationInsightsService],
      multi: true
    },
    {
      provide: APP_INITIALIZER,
      useFactory: (authService: AuthService, router: Router) => {
        return () => {
          return new Promise<void>((resolve) => {
            // Check current route - if user is directly accessing a specific route, let them proceed
            const currentUrl = router.url;
            console.log('App initialization - Current URL:', currentUrl);
            
            // If user is accessing a specific route (not root or warmup), skip warm-up initialization
            if (currentUrl !== '/' && currentUrl !== '/warmup') {
              console.log('User accessing specific route, performing auth check...');
              
              // Check if user has valid token or can refresh
              authService.refreshToken().pipe(
                take(1)
              ).subscribe({
                next: (response) => {
                  console.log('App initialization: Refresh token succeeded for direct route access');
                  resolve();
                },
                error: (error) => {
                  console.log('App initialization: Refresh token failed for direct route access');
                  // Don't redirect here, let the route guard handle it
                  resolve();
                }
              });
            } else {
              // User is on root or warmup route, let the warm-up component handle initialization
              console.log('App initialization: Allowing warm-up component to handle initialization');
              resolve();
            }
          });
        };
      },
      deps: [AuthService, Router],
      multi: true
    },
    provideRouter(routes),
    provideHttpClient(
      withInterceptors([authInterceptor])
    ),
    provideStore({
      config: configReducer,
      trackingLogs: trackingLogReducer
    }),
    provideEffects([ConfigEffects, TrackingLogEffects]),
    provideStoreDevtools({ maxAge: 25 })
  ]
};
