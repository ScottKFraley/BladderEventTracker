import { Routes, provideRouter } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: '/login', pathMatch: 'full' },
  { path: 'login', loadComponent: () => import('./auth/login/login.component').then(m => m.LoginComponent) },
  { path: 'dashboard', loadComponent: () => import('./dashboard/dashboard.component').then(m => m.DashboardComponent) },
  { path: 'survey', loadComponent: () => import('./survey/survey.component').then(m => m.SurveyComponent) }
];

export const AppRouting = provideRouter(routes);
