import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { InventoryService } from '@org/data-access-tasks';
import type { StoreSettingsDto } from '@org/data-access-tasks';
import { OrgService, TenantService } from '@org/data-access-org';
import type { DoughNeedTargetDto, Store } from '@org/data-access-org';
import { AuthService } from '@org/data-access-auth';

interface DoughRow {
  key: string;
  label: string;
  day2: number;
  day3: number;
}

const DOUGH_SIZES: { key: string; label: string }[] = [
  { key: 'dough_10in', label: '10" Dough' },
  { key: 'dough_12in', label: '12" Dough' },
  { key: 'dough_14in', label: '14" Dough' },
  { key: 'dough_16in', label: '16" Dough' },
];

// Curated locale set (each has data bundled at bootstrap in app.config) paired with a default currency.
export const SUPPORTED_LOCALES: { code: string; label: string; currency: string }[] = [
  { code: 'en-US', label: 'English (United States)', currency: 'USD' },
  { code: 'en-GB', label: 'English (United Kingdom)', currency: 'GBP' },
  { code: 'en-CA', label: 'English (Canada)', currency: 'CAD' },
  { code: 'en-AU', label: 'English (Australia)', currency: 'AUD' },
  { code: 'en-IE', label: 'English (Ireland)', currency: 'EUR' },
];

// "HH:mm:ss" (backend TimeOnly) ↔ "HH:mm" (<input type=time>).
function toTimeInput(v: string | null | undefined): string {
  return v ? v.slice(0, 5) : '';
}
function fromTimeInput(v: string): string | null {
  return v ? `${v}:00` : null;
}

@Component({
  selector: 'app-store-settings',
  imports: [ReactiveFormsModule],
  templateUrl: './store-settings.component.html',
  styleUrl: './store-settings.component.scss',
})
export class StoreSettingsComponent implements OnInit {
  private readonly inventorySvc = inject(InventoryService);
  private readonly orgSvc = inject(OrgService);
  private readonly tenantSvc = inject(TenantService);
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);

  readonly isSuperAdmin = computed(() => this.auth.currentUser()?.role === 'super_admin');
  readonly locales = SUPPORTED_LOCALES;
  readonly timezones = [
    'America/New_York', 'America/Chicago', 'America/Denver',
    'America/Los_Angeles', 'America/Phoenix', 'America/Anchorage', 'Pacific/Honolulu',
    'Europe/London', 'Europe/Dublin',
  ];

  // ── Tenant settings (super-admin): identity + locale + new-store defaults ────
  readonly orgLoading = signal(false);
  readonly orgSaving = signal(false);
  readonly orgSaved = signal(false);
  readonly orgError = signal<string | null>(null);

  readonly orgForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    logoUrl: [''],
    primaryContactEmail: ['', Validators.email],
    localeCode: [''],
    currencyCode: [''],
    defaultTimezoneId: [''],
    defaultOverdueGraceMinutes: [null as number | null, [Validators.min(0), Validators.max(480)]],
    defaultDepositDeadlineLocalTime: [''],
    defaultTillABase: [null as number | null, [Validators.min(0)]],
    defaultTillBBase: [null as number | null, [Validators.min(0)]],
  });
  readonly defaultDoughRows = signal<DoughRow[]>(DOUGH_SIZES.map((s) => ({ key: s.key, label: s.label, day2: 24, day3: 48 })));

  // ── Per-store operational settings ───────────────────────────────────────────
  readonly stores = signal<Store[]>([]);
  readonly selectedStoreId = signal<string | null>(null);
  readonly settings = signal<StoreSettingsDto | null>(null);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly saved = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    timezoneId: ['America/New_York', Validators.required],
    overdueGraceMinutes: [30, [Validators.required, Validators.min(0), Validators.max(480)]],
    depositDeadlineLocalTime: [''],
  });

  ngOnInit(): void {
    this.orgSvc.getStores().subscribe({ next: (s) => this.stores.set(s) });
    if (this.isSuperAdmin()) this.loadOrg();
  }

  // ── Tenant settings ───────────────────────────────────────────────────────────
  private loadOrg(): void {
    this.orgLoading.set(true);
    this.tenantSvc.getSettings().subscribe({
      next: (s) => {
        this.orgForm.patchValue({
          name: s.name,
          logoUrl: s.logoUrl ?? '',
          primaryContactEmail: s.primaryContactEmail ?? '',
          localeCode: s.localeCode ?? '',
          currencyCode: s.currencyCode ?? '',
          defaultTimezoneId: s.defaultTimezoneId ?? '',
          defaultOverdueGraceMinutes: s.defaultOverdueGraceMinutes ?? null,
          defaultDepositDeadlineLocalTime: toTimeInput(s.defaultDepositDeadlineLocalTime),
          defaultTillABase: s.defaultTillABase ?? null,
          defaultTillBBase: s.defaultTillBBase ?? null,
        });
        this.defaultDoughRows.set(DOUGH_SIZES.map((d) => ({
          key: d.key,
          label: d.label,
          day2: s.defaultDoughNeedTargets?.[d.key]?.day2Need ?? 24,
          day3: s.defaultDoughNeedTargets?.[d.key]?.day3Need ?? 48,
        })));
        this.orgLoading.set(false);
      },
      error: () => { this.orgError.set('Failed to load organization settings.'); this.orgLoading.set(false); },
    });
  }

  updateDefaultDoughRow(key: string, field: 'day2' | 'day3', value: number): void {
    this.defaultDoughRows.update((rows) => rows.map((r) => (r.key === key ? { ...r, [field]: value } : r)));
  }

  // When the locale changes, default the currency to that locale's convention (user can still override).
  onLocaleChange(code: string): void {
    const match = SUPPORTED_LOCALES.find((l) => l.code === code);
    if (match) this.orgForm.patchValue({ currencyCode: match.currency });
  }

  saveOrg(): void {
    if (this.orgForm.invalid || this.orgSaving()) return;
    const v = this.orgForm.getRawValue();
    const defaultDoughNeedTargets: Record<string, DoughNeedTargetDto> = {};
    for (const row of this.defaultDoughRows()) {
      defaultDoughNeedTargets[row.key] = { day2Need: row.day2, day3Need: row.day3 };
    }
    this.orgSaving.set(true);
    this.orgSaved.set(false);
    this.orgError.set(null);
    this.tenantSvc.updateSettings({
      name: v.name,
      logoUrl: v.logoUrl || null,
      primaryContactEmail: v.primaryContactEmail || null,
      localeCode: v.localeCode || null,
      currencyCode: v.currencyCode || null,
      defaultTimezoneId: v.defaultTimezoneId || null,
      defaultOverdueGraceMinutes: v.defaultOverdueGraceMinutes,
      defaultDepositDeadlineLocalTime: fromTimeInput(v.defaultDepositDeadlineLocalTime),
      defaultTillABase: v.defaultTillABase,
      defaultTillBBase: v.defaultTillBBase,
      defaultDoughNeedTargets,
    }).subscribe({
      next: () => { this.orgSaving.set(false); this.orgSaved.set(true); setTimeout(() => this.orgSaved.set(false), 3000); },
      error: () => { this.orgError.set('Failed to save organization settings.'); this.orgSaving.set(false); },
    });
  }

  // ── Per-store operational settings ──────────────────────────────────────────
  selectStore(storeId: string): void {
    this.selectedStoreId.set(storeId);
    if (!storeId) { this.settings.set(null); return; }
    this.loading.set(true);
    this.error.set(null);
    this.inventorySvc.getStoreSettings(storeId).subscribe({
      next: (s) => {
        this.settings.set(s);
        this.form.patchValue({
          timezoneId: s.timezoneId,
          overdueGraceMinutes: s.overdueGraceMinutes,
          depositDeadlineLocalTime: toTimeInput(s.depositDeadlineLocalTime),
        });
        this.loading.set(false);
      },
      error: () => { this.loading.set(false); this.error.set('Failed to load settings.'); },
    });
  }

  save(): void {
    const storeId = this.selectedStoreId();
    const current = this.settings();
    if (!storeId || !current || this.form.invalid || this.saving()) return;

    const { timezoneId, overdueGraceMinutes, depositDeadlineLocalTime } = this.form.getRawValue();

    this.saving.set(true);
    // Preserve dough/till baselines (edited in Store detail) — updateStoreSettings takes the full DTO.
    this.inventorySvc.updateStoreSettings(storeId, {
      tillABase: current.tillABase ?? undefined,
      tillBBase: current.tillBBase ?? undefined,
      doughNeedTargets: current.doughNeedTargets ?? {},
      timezoneId,
      overdueGraceMinutes,
      depositDeadlineLocalTime: fromTimeInput(depositDeadlineLocalTime),
    }).subscribe({
      next: () => { this.saving.set(false); this.saved.set(true); setTimeout(() => this.saved.set(false), 2500); },
      error: () => { this.saving.set(false); this.error.set('Failed to save settings.'); },
    });
  }
}
