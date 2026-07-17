import { Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '@org/data-access-auth';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly loading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly passwordVisible = signal(false);

  readonly form = this.fb.group({
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
      await this.auth.login({
        email: email!,
        password: password!,
        tenantId: tenantId!,
      });
      // The shared kiosk station account lands on the kiosk board; individuals go to their task list.
      const isKiosk = this.auth.currentUser()?.role === 'store_kiosk';
      await this.router.navigate([isKiosk ? '/kiosk' : '/tasks']);
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
}
