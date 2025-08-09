import { Routes, provideRouter } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: '/warmup', pathMatch: 'full' },
  { path: 'warmup', loadComponent: () => import('./components/warm-up/warm-up.component').then(m => m.WarmUpComponent) },
  { path: 'login', loadComponent: () => import('./auth/login/login.component').then(m => m.LoginComponent) },
  { path: 'dashboard', loadComponent: () => import('./dashboard/dashboard.component').then(m => m.DashboardComponent) },
  { path: 'survey', loadComponent: () => import('./survey/survey.component').then(m => m.SurveyComponent) },
  { path: 'debug', loadComponent: () => import('./debug/debug.component').then(m => m.DebugComponent) },
  {
    path: 'tracking-log-detail/:id',
    loadComponent: () => import('./tracking-log-detail/tracking-log-detail.component')
      .then(m => m.TrackingLogDetailComponent)
  }
];

export const AppRouting = provideRouter(routes);
