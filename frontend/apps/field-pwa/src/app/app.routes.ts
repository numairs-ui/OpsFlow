import { Route } from '@angular/router';
import { authGuard, roleGuard } from '@org/util-guards';

export const appRoutes: Route[] = [
  { path: '', redirectTo: 'tasks', pathMatch: 'full' },
  {
    path: 'login',
    loadComponent: () =>
      import('./login/login.component.js').then((m) => m.LoginComponent),
  },
  {
    path: 'tasks',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./tasks/tasks.component.js').then((m) => m.TasksComponent),
  },
  {
    path: 'tasks/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./task-detail/task-detail.component.js').then((m) => m.TaskDetailComponent),
  },
  {
    path: 'kiosk',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./kiosk/kiosk.component.js').then((m) => m.KioskComponent),
  },
  {
    path: 'quick-template',
    canActivate: [authGuard, roleGuard('store_manager')],
    loadComponent: () =>
      import('./quick-template/quick-template.component.js').then(
        (m) => m.QuickTemplateComponent
      ),
  },
  {
    path: 'submissions',
    canActivate: [authGuard, roleGuard('store_manager')],
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
