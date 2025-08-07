import { ApplicationConfig, APP_INITIALIZER, inject } from '@angular/core';
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

function initializeApp(): () => Promise<void> {
  return () => {
    const authService = inject(AuthService);
    const router = inject(Router);
    
    return new Promise<void>((resolve) => {
      // Check if user has valid token or can refresh
      authService.refreshToken().pipe(
        take(1)
      ).subscribe({
        next: (response) => {
          // Refresh token succeeded, user is authenticated
          console.log('App initialization: Refresh token succeeded');
          
          // Check current route
          const currentUrl = router.url;
          console.log('Current URL:', currentUrl);
          
          // If user is on login page but is authenticated, redirect to dashboard
          if (currentUrl === '/login' || currentUrl === '/') {
            console.log('User authenticated but on login page, redirecting to dashboard');
            router.navigate(['/dashboard']).then(() => {
              resolve();
            });
          } else {
            resolve();
          }
        },
        error: (error) => {
          // Refresh token failed, user is not authenticated
          console.log('App initialization: Refresh token failed, user not authenticated');
          resolve();
        }
      });
    });
  };
}

export const appConfig: ApplicationConfig = {
  providers: [
    { provide: TOKEN_REFRESH_THRESHOLD, useValue: 300000 },
    {
      provide: APP_INITIALIZER,
      useFactory: (appInsights: ApplicationInsightsService) => {
        return () => {
          // Initialize Application Insights
          console.log('Application Insights initialized');
        };
      },
      deps: [ApplicationInsightsService],
      multi: true
    },
    { provide: APP_INITIALIZER, useFactory: initializeApp, multi: true },
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
