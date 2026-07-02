import { Component, computed, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '@org/data-access-auth';
import { roleLabel } from '@org/ui-core';

@Component({
  selector: 'app-admin-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './admin-shell.component.html',
  styleUrl: './admin-shell.component.scss',
})
export class AdminShellComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly user = this.auth.currentUser;
  readonly roleLabel = roleLabel;

  // Org-wide controls (regions, system templates, imports, tenant settings) are super-admin only.
  // A region-scoped admin manages stores/users/templates within its assigned regions.
  readonly isSuperAdmin = computed(() => this.user()?.role === 'super_admin');

  async onLogout(): Promise<void> {
    await this.auth.logout();
    await this.router.navigate(['/login']);
  }
}
