import { Component, inject, signal } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '@org/data-access-auth';
import { TypeSelectorComponent, type CreationType } from '@org/ui-template-builder';

@Component({
  imports: [RouterModule, TypeSelectorComponent],
  selector: 'app-root',
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly currentUser = this.auth.currentUser;
  readonly fabOpen = signal(false);

  isManager(): boolean {
    return this.currentUser()?.role === 'store_manager';
  }

  openFab(): void {
    this.fabOpen.set(true);
  }

  closeFab(): void {
    this.fabOpen.set(false);
  }

  onTypeSelected(type: CreationType): void {
    this.closeFab();
    if (type === 'task') {
      this.router.navigate(['/quick-template']);
    } else {
      this.router.navigate(['/submissions'], { queryParams: { action: 'create' } });
    }
  }
}
