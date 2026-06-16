import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '@org/data-access-auth';

@Component({
  selector: 'app-supervisor-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './supervisor-shell.component.html',
  styleUrl: './supervisor-shell.component.scss',
})
export class SupervisorShellComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly user = this.auth.currentUser;

  async onLogout(): Promise<void> {
    await this.auth.logout();
    await this.router.navigate(['/login']);
  }
}
