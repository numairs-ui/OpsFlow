import { Component, inject, output } from '@angular/core';
import { Router } from '@angular/router';

interface CreateOption {
  label: string;
  desc: string;
  icon: string;
  route: string;
}

/**
 * Unified "Create" entry point for the dashboard (A5). Presents the four creation destinations and
 * dispatches to today's screens — pure navigation chrome, no new backend calls. Modeled on the
 * field-pwa TypeSelector pattern but with all four OpsFlow creation surfaces.
 */
@Component({
  selector: 'app-create-menu',
  imports: [],
  templateUrl: './create-menu.component.html',
  styleUrl: './create-menu.component.scss',
})
export class CreateMenuComponent {
  private readonly router = inject(Router);

  readonly closed = output<void>();

  readonly options: CreateOption[] = [
    { label: 'One-time task', desc: 'A single standalone task', icon: '✓', route: '/admin/create-task' },
    { label: 'Recurring', desc: 'Repeat on a schedule across stores', icon: '🔁', route: '/admin/templates/recurring' },
    { label: 'Checklist', desc: 'A scored multi-item checklist', icon: '☑', route: '/admin/templates/checklists' },
    { label: 'Form', desc: 'A form that routes for approval', icon: '📄', route: '/admin/templates/forms' },
  ];

  choose(option: CreateOption): void {
    this.closed.emit();
    this.router.navigateByUrl(option.route);
  }

  close(): void {
    this.closed.emit();
  }
}
