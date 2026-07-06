import { SlicePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { forkJoin } from 'rxjs';
import { OrgService, type Region, type Store, type StoreEmployee, type User } from '@org/data-access-org';
import { noWhitespace } from '@org/ui-core';

const ROSTER_ROLES = ['store_employee', 'store_manager'];

@Component({
  selector: 'app-stores',
  imports: [ReactiveFormsModule, SlicePipe],
  templateUrl: './stores.component.html',
  styleUrl: './stores.component.scss',
})
export class StoresComponent implements OnInit {
  private readonly org = inject(OrgService);
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);

  readonly stores = signal<Store[]>([]);
  readonly regions = signal<Region[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly saved = signal(false);
  readonly showForm = signal(false);
  readonly editingStore = signal<Store | null>(null);

  readonly detailStore = signal<Store | null>(null);
  readonly storeEmployees = signal<StoreEmployee[]>([]);
  readonly storeEmployeesLoading = signal(false);

  // "Assign Roster" picker — a bottom sheet listing staff the caller can already manage
  // (super_admin: everyone; region-scoped admin: staff in its own regions) who aren't already
  // at this store. Selecting staff reassigns their primary store.
  readonly rosterPickerOpen = signal(false);
  readonly rosterPickerLoading = signal(false);
  readonly rosterPickerCandidates = signal<User[]>([]);
  readonly selectedUserIds = signal<ReadonlySet<string>>(new Set());
  readonly assigningRoster = signal(false);
  readonly rosterAssignError = signal<string | null>(null);

  readonly rosterPickerGroups = computed(() => {
    const grouped = new Map<string, User[]>();
    for (const u of this.rosterPickerCandidates()) {
      const key = u.storeName ?? 'Unassigned';
      const list = grouped.get(key) ?? [];
      list.push(u);
      grouped.set(key, list);
    }
    return [...grouped.entries()]
      .sort(([a], [b]) => a.localeCompare(b))
      .map(([storeName, users]) => ({ storeName, users }));
  });

  readonly form = this.fb.group({
    name: ['', [Validators.required, noWhitespace]],
    address: [''],
    regionId: ['', Validators.required],
  });

  ngOnInit(): void {
    this.loadRegions();
    this.load();
    this.route.queryParams.subscribe((params) => {
      const detailId = params['detail'];
      if (detailId) this.openDetailById(detailId);
    });
  }

  private loadRegions(): void {
    this.org.getRegions(true).subscribe({
      next: (data) => this.regions.set(data),
    });
  }

  private load(): void {
    this.loading.set(true);
    this.org.getStores(undefined, false).subscribe({
      next: (data) => { this.stores.set(data); this.loading.set(false); },
      error: () => { this.error.set('Failed to load stores.'); this.loading.set(false); },
    });
  }

  storesByRegion(): { region: Region | null; stores: Store[] }[] {
    const regionMap = new Map(this.regions().map((r) => [r.id, r]));
    const grouped = new Map<string, Store[]>();
    for (const s of this.stores()) {
      const list = grouped.get(s.regionId) ?? [];
      list.push(s);
      grouped.set(s.regionId, list);
    }
    return [...grouped.entries()].map(([regionId, stores]) => ({
      region: regionMap.get(regionId) ?? null,
      stores,
    }));
  }

  openCreate(): void {
    this.editingStore.set(null);
    this.form.reset();
    this.showForm.set(true);
  }

  openEdit(store: Store): void {
    this.editingStore.set(store);
    this.form.patchValue({ name: store.name, address: store.address ?? '', regionId: store.regionId });
    this.showForm.set(true);
  }

  closeForm(): void {
    this.showForm.set(false);
    this.editingStore.set(null);
    this.form.reset();
  }

  onSubmit(): void {
    if (this.form.invalid || this.saving()) return;
    const { name, address, regionId } = this.form.getRawValue();
    const body = { name: name!, address: address ?? undefined, regionId: regionId! };
    const onSuccess = () => { this.saving.set(false); this.saved.set(true); setTimeout(() => this.saved.set(false), 2500); this.closeForm(); this.load(); };
    const onError = () => { this.error.set('Failed to save store.'); this.saving.set(false); };
    this.saving.set(true);
    const editing = this.editingStore();
    if (editing) {
      this.org.updateStore(editing.id, body).subscribe({ next: onSuccess, error: onError });
    } else {
      this.org.createStore(body).subscribe({ next: onSuccess, error: onError });
    }
  }

  deactivate(store: Store): void {
    if (!confirm(`Deactivate store "${store.name}"?`)) return;
    this.org.deactivateStore(store.id).subscribe({ next: () => this.load() });
  }

  openDetail(store: Store): void {
    this.detailStore.set(store);
    this.storeEmployees.set([]);
    this.storeEmployeesLoading.set(true);
    this.org.getStoreEmployees(store.id).subscribe({
      next: (employees) => { this.storeEmployees.set(employees); this.storeEmployeesLoading.set(false); },
      error: () => this.storeEmployeesLoading.set(false),
    });
  }

  private openDetailById(storeId: string): void {
    const existing = this.stores().find((s) => s.id === storeId);
    if (existing) { this.openDetail(existing); return; }
    this.org.getStores(undefined, false).subscribe({
      next: (all) => {
        this.stores.set(all);
        const match = all.find((s) => s.id === storeId);
        if (match) this.openDetail(match);
      },
    });
  }

  closeDetail(): void {
    this.detailStore.set(null);
    this.storeEmployees.set([]);
  }

  editFromDetail(): void {
    const store = this.detailStore();
    if (!store) return;
    this.closeDetail();
    this.openEdit(store);
  }

  roleLabel(role: string): string {
    const map: Record<string, string> = {
      store_employee: 'Employee',
      store_manager: 'Manager',
      supervisor: 'Supervisor',
      admin: 'Admin',
    };
    return map[role] ?? role;
  }

  openRosterPicker(): void {
    const store = this.detailStore();
    if (!store) return;
    this.rosterPickerOpen.set(true);
    this.rosterPickerLoading.set(true);
    this.selectedUserIds.set(new Set());
    this.rosterAssignError.set(null);
    // getUsers() already returns only staff the caller can manage (region-scoped for admin,
    // everyone for super_admin) — narrow to roster-eligible roles not already at this store.
    this.org.getUsers({ activeOnly: true }).subscribe({
      next: (users) => {
        this.rosterPickerCandidates.set(
          users.filter((u) => ROSTER_ROLES.includes(u.role) && u.storeId !== store.id)
        );
        this.rosterPickerLoading.set(false);
      },
      error: () => { this.rosterPickerLoading.set(false); },
    });
  }

  closeRosterPicker(): void {
    this.rosterPickerOpen.set(false);
    this.rosterPickerCandidates.set([]);
    this.selectedUserIds.set(new Set());
    this.rosterAssignError.set(null);
  }

  toggleUserSelection(userId: string): void {
    const next = new Set(this.selectedUserIds());
    if (next.has(userId)) next.delete(userId); else next.add(userId);
    this.selectedUserIds.set(next);
  }

  confirmAssignRoster(): void {
    const store = this.detailStore();
    const ids = this.selectedUserIds();
    if (!store || ids.size === 0 || this.assigningRoster()) return;

    const users = this.rosterPickerCandidates().filter((u) => ids.has(u.userId));
    this.assigningRoster.set(true);
    this.rosterAssignError.set(null);

    forkJoin(
      users.map((u) =>
        this.org.updateUser(u.userId, { displayName: u.displayName, role: u.role, storeId: store.id, regionIds: u.regionIds })
      )
    ).subscribe({
      next: () => {
        this.assigningRoster.set(false);
        this.closeRosterPicker();
        this.openDetail(store);
      },
      error: () => {
        this.assigningRoster.set(false);
        this.rosterAssignError.set('Failed to assign one or more staff. Please try again.');
      },
    });
  }
}
