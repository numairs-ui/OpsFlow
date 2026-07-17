import { Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
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
  readonly passwordVisible = signal(false);

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
    } catch (err) {
      this.errorMessage.set(this.describeLoginError(err));
    } finally {
      this.loading.set(false);
    }
  }

  togglePasswordVisibility(): void {
    this.passwordVisible.update((v) => !v);
  }

  private describeLoginError(err: unknown): string {
    if (err instanceof HttpErrorResponse) {
      if (err.status === 401) return 'Invalid email or password.';
      if (err.status === 0) {
        return 'Unable to reach the server. Check your connection and try again.';
      }
    }
    return 'Something went wrong on our end. Please try again in a moment.';
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
