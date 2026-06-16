import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '@org/data-access-auth';

@Component({
  selector: 'app-manager-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './manager-shell.component.html',
  styleUrl: './manager-shell.component.scss',
})
export class ManagerShellComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly user = this.auth.currentUser;

  async onLogout(): Promise<void> {
    await this.auth.logout();
    await this.router.navigate(['/login']);
  }
}
