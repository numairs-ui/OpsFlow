import { Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '@org/data-access-auth';
import { NavIconComponent, roleLabel } from '@org/ui-core';
import { CreateMenuComponent } from '../create-menu/create-menu.component';

@Component({
  selector: 'app-admin-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, NavIconComponent, CreateMenuComponent],
  templateUrl: './admin-shell.component.html',
  styleUrl: './admin-shell.component.scss',
})
export class AdminShellComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly user = this.auth.currentUser;
  readonly roleLabel = roleLabel;

  // Org-wide controls (system templates, imports, tenant settings) are super-admin only.
  // Regions is visible to both — a region-scoped admin sees a read-only view of its own
  // assigned regions (RegionsComponent hides create/edit/deactivate for non-super-admins).
  readonly isSuperAdmin = computed(() => this.user()?.role === 'super_admin');

  // Mobile-only: the sidebar collapses into a bottom tab bar; "More" opens this sheet
  // with the rest of the nav (everything the 4 primary tabs don't cover).
  readonly moreOpen = signal(false);

  // Unified "Create" entry point (A5).
  readonly createOpen = signal(false);
  openCreate(): void { this.createOpen.set(true); }
  closeCreate(): void { this.createOpen.set(false); }

  toggleMore(): void {
    this.moreOpen.update((open) => !open);
  }

  closeMore(): void {
    this.moreOpen.set(false);
  }

  async onLogout(): Promise<void> {
    await this.auth.logout();
    await this.router.navigate(['/login']);
  }
}
