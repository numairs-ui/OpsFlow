import { Component, inject, signal } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { AuthService, type CurrentUser } from '@org/data-access-auth';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);

  readonly loading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
    tenantId: ['bajco-dev', Validators.required],
  });

  get email(): AbstractControl {
    return this.form.controls['email'];
  }

  get password(): AbstractControl {
    return this.form.controls['password'];
  }

  async onSubmit(): Promise<void> {
    if (this.form.invalid) return;

    this.loading.set(true);
    this.errorMessage.set(null);

    try {
      const { email, password, tenantId } = this.form.getRawValue();
      await this.auth.login({ email, password, tenantId });
      const user = this.auth.currentUser();
      if (!user) {
        this.errorMessage.set('Invalid email or password.');
        return;
      }
      await this.navigateByRole(user);
    } catch {
      this.errorMessage.set('Invalid email or password.');
    } finally {
      this.loading.set(false);
    }
  }

  private async navigateByRole(user: CurrentUser): Promise<void> {
    const routes: Record<string, string> = {
      super_admin: '/admin',
      admin: '/admin',
      supervisor: '/supervisor',
      store_manager: '/manager',
    };
    const target = routes[user.role] ?? '/login';
    // Full-page load (not SPA nav) so the app re-bootstraps and picks up the org's
    // locale/currency — LOCALE_ID is fixed at bootstrap (see app.config). The session is
    // restored from the refresh cookie during app-init.
    window.location.assign(target);
  }
}
