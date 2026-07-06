import { Route } from '@angular/router';
import { authGuard, roleGuard } from '@org/util-guards';

export const appRoutes: Route[] = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  {
    path: 'login',
    loadComponent: () =>
      import('./login/login.component.js').then((m) => m.LoginComponent),
  },
  {
    path: 'admin',
    canActivate: [authGuard, roleGuard('super_admin', 'admin')],
    loadComponent: () =>
      import('./admin/admin-shell/admin-shell.component.js').then(
        (m) => m.AdminShellComponent
      ),
    children: [
      { path: '', redirectTo: 'overview', pathMatch: 'full' },
      {
        path: 'overview',
        loadComponent: () =>
          import('./admin/overview/overview.component.js').then(
            (m) => m.AdminOverviewComponent
          ),
      },
      {
        path: 'tasks',
        loadComponent: () =>
          import('./admin/tasks/tasks.component.js').then(
            (m) => m.TasksComponent
          ),
      },
      {
        path: 'tasks/:id',
        loadComponent: () =>
          import('./admin/task-detail/task-detail.component.js').then(
            (m) => m.TaskDetailComponent
          ),
      },
      {
        path: 'regions',
        loadComponent: () =>
          import('./admin/regions/regions.component.js').then(
            (m) => m.RegionsComponent
          ),
      },
      {
        path: 'stores',
        loadComponent: () =>
          import('./admin/stores/stores.component.js').then(
            (m) => m.StoresComponent
          ),
      },
      {
        path: 'users',
        loadComponent: () =>
          import('./admin/users/users.component.js').then(
            (m) => m.UsersComponent
          ),
      },
      {
        path: 'roster',
        loadComponent: () =>
          import('./manager/roster/roster.component.js').then(
            (m) => m.RosterComponent
          ),
      },
      {
        path: 'templates',
        loadComponent: () =>
          import('./admin/templates/templates.component.js').then(
            (m) => m.TemplatesComponent
          ),
      },
      {
        path: 'templates/:id',
        loadComponent: () =>
          import('./admin/template-detail/template-detail.component.js').then(
            (m) => m.TemplateDetailComponent
          ),
      },
      {
        path: 'system-templates',
        data: { systemOnly: true },
        loadComponent: () =>
          import('./admin/templates/templates.component.js').then(
            (m) => m.TemplatesComponent
          ),
      },
      {
        path: 'checklists',
        loadComponent: () =>
          import('./admin/checklists/checklists.component.js').then(
            (m) => m.ChecklistsComponent
          ),
      },
      {
        path: 'checklists/:id',
        loadComponent: () =>
          import('./admin/checklist-detail/checklist-detail.component.js').then(
            (m) => m.ChecklistDetailComponent
          ),
      },
      {
        path: 'recurring-assignments',
        loadComponent: () =>
          import('./admin/recurring-assignments/recurring-assignments.component.js').then(
            (m) => m.RecurringAssignmentsComponent
          ),
      },
      {
        path: 'form-templates',
        loadComponent: () =>
          import('./admin/form-templates/form-templates.component.js').then(
            (m) => m.FormTemplatesComponent
          ),
      },
      {
        path: 'form-templates/:id',
        loadComponent: () =>
          import('./admin/form-template-detail/form-template-detail.component.js').then(
            (m) => m.FormTemplateDetailComponent
          ),
      },
      {
        path: 'store-settings',
        loadComponent: () =>
          import('./admin/store-settings/store-settings.component.js').then(
            (m) => m.StoreSettingsComponent
          ),
      },
      {
        path: 'tenant-settings',
        loadComponent: () =>
          import('./admin/tenant-settings/tenant-settings.component.js').then(
            (m) => m.TenantSettingsComponent
          ),
      },
      {
        path: 'template-import',
        loadComponent: () =>
          import('./admin/template-import/template-import.component.js').then(
            (m) => m.TemplateImportComponent
          ),
      },
      {
        path: 'submissions',
        loadComponent: () =>
          import('./shared/form-submissions/form-submissions.component.js').then(
            (m) => m.FormSubmissionsComponent
          ),
      },
    ],
  },
  {
    path: 'supervisor',
    canActivate: [authGuard, roleGuard('supervisor')],
    loadComponent: () =>
      import('./supervisor/supervisor-shell/supervisor-shell.component.js').then(
        (m) => m.SupervisorShellComponent
      ),
    children: [
      { path: '', redirectTo: 'overview', pathMatch: 'full' },
      {
        path: 'overview',
        loadComponent: () =>
          import('./supervisor/overview/overview.component.js').then(
            (m) => m.SupervisorOverviewComponent
          ),
      },
      {
        path: 'submissions',
        loadComponent: () =>
          import('./shared/form-submissions/form-submissions.component.js').then(
            (m) => m.FormSubmissionsComponent
          ),
      },
    ],
  },
  {
    path: 'manager',
    canActivate: [authGuard, roleGuard('store_manager')],
    loadComponent: () =>
      import('./manager/manager-shell/manager-shell.component.js').then(
        (m) => m.ManagerShellComponent
      ),
    children: [
      { path: '', redirectTo: 'overview', pathMatch: 'full' },
      {
        path: 'overview',
        loadComponent: () =>
          import('./manager/overview/overview.component.js').then(
            (m) => m.ManagerOverviewComponent
          ),
      },
      {
        path: 'roster',
        loadComponent: () =>
          import('./manager/roster/roster.component.js').then(
            (m) => m.RosterComponent
          ),
      },
      {
        path: 'deposit',
        loadComponent: () =>
          import('./manager/deposit/deposit.component.js').then(
            (m) => m.DepositComponent
          ),
      },
      {
        path: 'submissions',
        loadComponent: () =>
          import('./shared/form-submissions/form-submissions.component.js').then(
            (m) => m.FormSubmissionsComponent
          ),
      },
    ],
  },
  {
    path: 'unauthorized',
    loadComponent: () =>
      import('./unauthorized/unauthorized.component.js').then(
        (m) => m.UnauthorizedComponent
      ),
  },
];
