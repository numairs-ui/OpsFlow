import { Route } from '@angular/router';
import { authGuard, kioskRedirectGuard } from '@org/data-access-auth';

export const appRoutes: Route[] = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  {
    path: 'dashboard',
    canActivate: [authGuard, kioskRedirectGuard],
    loadComponent: () =>
      import('./dashboard/dashboard.component.js').then((m) => m.DashboardComponent),
  },
  {
    path: 'login',
    loadComponent: () =>
      import('./login/login.component.js').then((m) => m.LoginComponent),
  },
  {
    path: 'tasks',
    canActivate: [authGuard, kioskRedirectGuard],
    loadComponent: () =>
      import('./tasks/tasks.component.js').then((m) => m.TasksComponent),
  },
  {
    path: 'tasks/:id',
    canActivate: [authGuard, kioskRedirectGuard],
    loadComponent: () =>
      import('./task-detail/task-detail.component.js').then((m) => m.TaskDetailComponent),
  },
  {
    // Shared station board — any authenticated field user (incl. the store_kiosk account) may view it.
    path: 'kiosk',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./kiosk/kiosk.component.js').then((m) => m.KioskComponent),
  },
  {
    path: 'quick-template',
    canActivate: [authGuard, kioskRedirectGuard],
    loadComponent: () =>
      import('./quick-template/quick-template.component.js').then(
        (m) => m.QuickTemplateComponent
      ),
  },
  {
    path: 'submissions',
    canActivate: [authGuard, kioskRedirectGuard],
    loadComponent: () =>
      import('./submissions/submissions.component.js').then(
        (m) => m.SubmissionsComponent
      ),
  },
  {
    path: 'unauthorized',
    loadComponent: () =>
      import('./unauthorized/unauthorized.component.js').then(
        (m) => m.UnauthorizedComponent
      ),
  },
];
