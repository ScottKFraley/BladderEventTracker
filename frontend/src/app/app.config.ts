import { ApplicationConfig } from '@angular/core';
import { provideRouter } from '@angular/router';
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

export const appConfig: ApplicationConfig = {
  providers: [
    { provide: TOKEN_REFRESH_THRESHOLD, useValue: 300000 },
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
