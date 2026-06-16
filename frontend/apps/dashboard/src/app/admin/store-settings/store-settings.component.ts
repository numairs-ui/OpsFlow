import { Component, OnInit, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { InventoryService } from '@org/data-access-tasks';
import type { DoughNeedTargetDto, StoreSettingsDto } from '@org/data-access-tasks';
import { OrgService } from '@org/data-access-org';
import type { Store } from '@org/data-access-org';

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

@Component({
  selector: 'app-store-settings',
  imports: [ReactiveFormsModule],
  templateUrl: './store-settings.component.html',
  styleUrl: './store-settings.component.scss',
})
export class StoreSettingsComponent implements OnInit {
  private readonly inventorySvc = inject(InventoryService);
  private readonly orgSvc = inject(OrgService);
  private readonly fb = inject(FormBuilder);

  readonly stores = signal<Store[]>([]);
  readonly selectedStoreId = signal<string | null>(null);
  readonly settings = signal<StoreSettingsDto | null>(null);
  readonly doughRows = signal<DoughRow[]>(DOUGH_SIZES.map(s => ({ key: s.key, label: s.label, day2: 24, day3: 48 })));
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly saved = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.group({
    tillABase: [null as number | null],
    tillBBase: [null as number | null],
    timezoneId: ['America/New_York', Validators.required],
    overdueGraceMinutes: [30, [Validators.required, Validators.min(0), Validators.max(480)]],
  });

  readonly timezones = [
    'America/New_York', 'America/Chicago', 'America/Denver',
    'America/Los_Angeles', 'America/Phoenix', 'America/Anchorage', 'Pacific/Honolulu',
  ];

  ngOnInit(): void {
    this.orgSvc.getStores().subscribe({ next: (s) => this.stores.set(s) });
  }

  selectStore(storeId: string): void {
    this.selectedStoreId.set(storeId);
    this.loading.set(true);
    this.error.set(null);
    this.inventorySvc.getStoreSettings(storeId).subscribe({
      next: (s) => {
        this.settings.set(s);
        this.form.patchValue({
          tillABase: s.tillABase ?? null,
          tillBBase: s.tillBBase ?? null,
          timezoneId: s.timezoneId,
          overdueGraceMinutes: s.overdueGraceMinutes,
        });
        this.doughRows.set(DOUGH_SIZES.map(d => ({
          key: d.key,
          label: d.label,
          day2: s.doughNeedTargets?.[d.key]?.day2Need ?? 24,
          day3: s.doughNeedTargets?.[d.key]?.day3Need ?? 48,
        })));
        this.loading.set(false);
      },
      error: () => { this.loading.set(false); this.error.set('Failed to load settings.'); },
    });
  }

  updateDoughRow(key: string, field: 'day2' | 'day3', value: number): void {
    this.doughRows.update(rows => rows.map(r => r.key === key ? { ...r, [field === 'day2' ? 'day2' : 'day3']: value } : r));
  }

  save(): void {
    const storeId = this.selectedStoreId();
    if (!storeId || this.form.invalid || this.saving()) return;

    const { tillABase, tillBBase, timezoneId, overdueGraceMinutes } = this.form.getRawValue();

    const doughNeedTargets: Record<string, DoughNeedTargetDto> = {};
    for (const row of this.doughRows()) {
      doughNeedTargets[row.key] = { day2Need: row.day2, day3Need: row.day3 };
    }

    this.saving.set(true);
    this.inventorySvc.updateStoreSettings(storeId, {
      tillABase: tillABase ?? undefined,
      tillBBase: tillBBase ?? undefined,
      doughNeedTargets,
      timezoneId: timezoneId!,
      overdueGraceMinutes: overdueGraceMinutes!,
    }).subscribe({
      next: () => { this.saving.set(false); this.saved.set(true); setTimeout(() => this.saved.set(false), 2500); },
      error: () => { this.saving.set(false); this.error.set('Failed to save settings.'); },
    });
  }
}
