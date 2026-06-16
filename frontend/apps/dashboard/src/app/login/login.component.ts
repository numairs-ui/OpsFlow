import { Component, inject, signal } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService, type CurrentUser } from '@org/data-access-auth';

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
      await this.navigateByRole(this.auth.currentUser()!);
    } catch {
      this.errorMessage.set('Invalid email or password.');
    } finally {
      this.loading.set(false);
    }
  }

  private async navigateByRole(user: CurrentUser): Promise<void> {
    const routes: Record<string, string> = {
      admin: '/admin',
      supervisor: '/supervisor',
      store_manager: '/manager',
    };
    await this.router.navigate([routes[user.role] ?? '/dashboard']);
  }
}
