import { Component, OnInit, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { TenantService } from '@org/data-access-org';

@Component({
  selector: 'app-tenant-settings',
  imports: [ReactiveFormsModule],
  templateUrl: './tenant-settings.component.html',
  styleUrl: './tenant-settings.component.scss',
})
export class TenantSettingsComponent implements OnInit {
  private readonly tenantSvc = inject(TenantService);
  private readonly fb = inject(FormBuilder);

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly saved = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    logoUrl: [''],
    primaryContactEmail: ['', Validators.email],
  });

  ngOnInit(): void {
    this.tenantSvc.getSettings().subscribe({
      next: (s) => {
        this.form.patchValue({
          name: s.name,
          logoUrl: s.logoUrl ?? '',
          primaryContactEmail: s.primaryContactEmail ?? '',
        });
        this.loading.set(false);
      },
      error: () => { this.error.set('Failed to load tenant settings.'); this.loading.set(false); },
    });
  }

  save(): void {
    if (this.form.invalid || this.saving()) return;
    const { name, logoUrl, primaryContactEmail } = this.form.getRawValue();
    this.saving.set(true);
    this.saved.set(false);
    this.error.set(null);

    this.tenantSvc.updateSettings({
      name: name!,
      logoUrl: logoUrl || null,
      primaryContactEmail: primaryContactEmail || null,
    }).subscribe({
      next: () => { this.saving.set(false); this.saved.set(true); setTimeout(() => this.saved.set(false), 3000); },
      error: () => { this.error.set('Failed to save settings.'); this.saving.set(false); },
    });
  }
}
