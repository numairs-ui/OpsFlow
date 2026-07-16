import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

/**
 * Thin container for the unified Templates area. Renders a type toggle (Task Templates /
 * Checklists / Recurring / Forms) above a <router-outlet>; each tab is a child route that
 * loads the corresponding existing list component unchanged.
 */
@Component({
  selector: 'app-templates-shell',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './templates-shell.component.html',
  styleUrl: './templates-shell.component.scss',
})
export class TemplatesShellComponent {}
