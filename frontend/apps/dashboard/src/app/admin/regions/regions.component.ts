import { SlicePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { AuthService } from '@org/data-access-auth';
import { OrgService, type Region, type Store } from '@org/data-access-org';
import { noWhitespace } from '@org/ui-core';

@Component({
  selector: 'app-regions',
  imports: [ReactiveFormsModule, SlicePipe],
  templateUrl: './regions.component.html',
  styleUrl: './regions.component.scss',
})
export class RegionsComponent implements OnInit {
  private readonly org = inject(OrgService);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);

  // Region creation/editing is org-wide franchise structure — super-admin only. A region-scoped
  // admin gets a read-only view of the regions it's assigned to (backend already filters the list).
  readonly canManageRegions = computed(() => this.auth.currentUser()?.role === 'super_admin');

  readonly regions = signal<Region[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly saved = signal(false);
  readonly showForm = signal(false);
  readonly editingRegion = signal<Region | null>(null);

  readonly detailRegion = signal<Region | null>(null);
  readonly regionStores = signal<Store[]>([]);
  readonly regionStoresLoading = signal(false);

  // "Add Stores" picker — a bottom sheet listing every store the caller can already manage
  // (super_admin: all stores; region-scoped admin: stores in its other assigned regions)
  // that isn't already in the region being viewed. Selecting stores reassigns their region.
  readonly storePickerOpen = signal(false);
  readonly storePickerLoading = signal(false);
  readonly storePickerCandidates = signal<Store[]>([]);
  readonly selectedStoreIds = signal<ReadonlySet<string>>(new Set());
  readonly assigningStores = signal(false);
  readonly assignError = signal<string | null>(null);

  readonly storePickerGroups = computed(() => {
    const grouped = new Map<string, Store[]>();
    for (const s of this.storePickerCandidates()) {
      const list = grouped.get(s.regionName) ?? [];
      list.push(s);
      grouped.set(s.regionName, list);
    }
    return [...grouped.entries()]
      .sort(([a], [b]) => a.localeCompare(b))
      .map(([regionName, stores]) => ({ regionName, stores }));
  });

  readonly form = this.fb.group({
    name: ['', [Validators.required, noWhitespace]],
    description: [''],
  });

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.org.getRegions(false).subscribe({
      next: (data) => { this.regions.set(data); this.loading.set(false); },
      error: () => { this.error.set('Failed to load regions.'); this.loading.set(false); },
    });
  }

  openCreate(): void {
    this.editingRegion.set(null);
    this.form.reset();
    this.showForm.set(true);
  }

  openEdit(region: Region): void {
    this.editingRegion.set(region);
    this.form.patchValue({ name: region.name, description: region.description ?? '' });
    this.showForm.set(true);
  }

  closeForm(): void {
    this.showForm.set(false);
    this.editingRegion.set(null);
    this.form.reset();
  }

  onSubmit(): void {
    if (this.form.invalid || this.saving()) return;
    const { name, description } = this.form.getRawValue();
    const body = { name: name!, description: description ?? undefined };
    const onSuccess = () => { this.saving.set(false); this.saved.set(true); setTimeout(() => this.saved.set(false), 2500); this.closeForm(); this.load(); };
    const onError = () => { this.error.set('Failed to save region.'); this.saving.set(false); };
    this.saving.set(true);
    const editing = this.editingRegion();
    if (editing) {
      this.org.updateRegion(editing.id, body).subscribe({ next: onSuccess, error: onError });
    } else {
      this.org.createRegion(body).subscribe({ next: onSuccess, error: onError });
    }
  }

  deactivate(region: Region): void {
    if (!confirm(`Deactivate region "${region.name}"?`)) return;
    this.org.deactivateRegion(region.id).subscribe({ next: () => this.load() });
  }

  openDetail(region: Region): void {
    this.detailRegion.set(region);
    this.regionStores.set([]);
    this.regionStoresLoading.set(true);
    this.org.getStores(region.id, false).subscribe({
      next: (stores) => { this.regionStores.set(stores); this.regionStoresLoading.set(false); },
      error: () => this.regionStoresLoading.set(false),
    });
  }

  closeDetail(): void {
    this.detailRegion.set(null);
    this.regionStores.set([]);
  }

  editFromDetail(): void {
    const region = this.detailRegion();
    if (!region) return;
    this.closeDetail();
    this.openEdit(region);
  }

  viewStore(store: Store): void {
    this.closeDetail();
    this.router.navigate(['/admin/stores'], { queryParams: { detail: store.id } });
  }

  openStorePicker(): void {
    const region = this.detailRegion();
    if (!region) return;
    this.storePickerOpen.set(true);
    this.storePickerLoading.set(true);
    this.selectedStoreIds.set(new Set());
    this.assignError.set(null);
    // getStores() already returns only stores the caller can manage (region-scoped for admin,
    // all of them for super_admin) — just drop the ones already in this region.
    this.org.getStores(undefined, true).subscribe({
      next: (stores) => {
        this.storePickerCandidates.set(stores.filter((s) => s.regionId !== region.id));
        this.storePickerLoading.set(false);
      },
      error: () => { this.storePickerLoading.set(false); },
    });
  }

  closeStorePicker(): void {
    this.storePickerOpen.set(false);
    this.storePickerCandidates.set([]);
    this.selectedStoreIds.set(new Set());
    this.assignError.set(null);
  }

  toggleStoreSelection(storeId: string): void {
    const next = new Set(this.selectedStoreIds());
    if (next.has(storeId)) next.delete(storeId); else next.add(storeId);
    this.selectedStoreIds.set(next);
  }

  confirmAssignStores(): void {
    const region = this.detailRegion();
    const ids = this.selectedStoreIds();
    if (!region || ids.size === 0 || this.assigningStores()) return;

    const stores = this.storePickerCandidates().filter((s) => ids.has(s.id));
    this.assigningStores.set(true);
    this.assignError.set(null);

    forkJoin(
      stores.map((s) =>
        this.org.updateStore(s.id, { name: s.name, address: s.address ?? undefined, regionId: region.id })
      )
    ).subscribe({
      next: () => {
        this.assigningStores.set(false);
        this.closeStorePicker();
        this.openDetail(region);
      },
      error: () => {
        this.assigningStores.set(false);
        this.assignError.set('Failed to add one or more stores. Please try again.');
      },
    });
  }
}
