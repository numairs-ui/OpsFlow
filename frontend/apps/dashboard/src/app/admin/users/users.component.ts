import { SlicePipe } from '@angular/common';
import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { OrgService, type Region, type Store, type StoreAssignment, type User, type UserRole } from '@org/data-access-org';

@Component({
  selector: 'app-users',
  imports: [ReactiveFormsModule, SlicePipe],
  templateUrl: './users.component.html',
  styleUrl: './users.component.scss',
})
export class UsersComponent implements OnInit {
  private readonly org = inject(OrgService);
  private readonly fb = inject(FormBuilder);

  readonly users = signal<User[]>([]);
  readonly regions = signal<Region[]>([]);
  readonly stores = signal<Store[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly showForm = signal(false);
  readonly editingUser = signal<User | null>(null);

  // Detail / assignment panel
  readonly selectedUser = signal<User | null>(null);
  readonly assignments = signal<StoreAssignment[]>([]);
  readonly loadingAssignments = signal(false);
  readonly assigningStore = signal<string>('');

  readonly roleFilter = signal<string>('');
  readonly searchQuery = signal<string>('');

  readonly filteredUsers = computed(() => {
    let list = this.users();
    const role = this.roleFilter();
    const query = this.searchQuery().toLowerCase();
    if (role) list = list.filter((u) => u.role === role);
    if (query) list = list.filter((u) =>
      u.displayName.toLowerCase().includes(query) || u.email.toLowerCase().includes(query)
    );
    return list;
  });

  readonly availableStoresForAssignment = computed(() => {
    const assignedIds = new Set(this.assignments().map((a) => a.storeId));
    return this.stores().filter((s) => s.isActive && !assignedIds.has(s.id));
  });

  readonly form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
    displayName: ['', Validators.required],
    role: ['store_employee' as UserRole, Validators.required],
    storeId: [''],
    regionId: [''],
  });

  readonly selectedRole = signal<string>('store_employee');

  ngOnInit(): void {
    this.load();
    this.org.getRegions(true).subscribe({ next: (r) => this.regions.set(r) });
    this.org.getStores(undefined, true).subscribe({ next: (s) => this.stores.set(s) });
    this.form.controls['role'].valueChanges.subscribe((v) => this.selectedRole.set(v ?? ''));
  }

  private load(): void {
    this.loading.set(true);
    this.org.getUsers({ activeOnly: false }).subscribe({
      next: (data) => { this.users.set(data); this.loading.set(false); },
      error: () => { this.error.set('Failed to load users.'); this.loading.set(false); },
    });
  }

  storesForRegion(regionId: string): Store[] {
    return this.stores().filter((s) => s.regionId === regionId);
  }

  openCreate(): void {
    this.editingUser.set(null);
    this.form.reset({ role: 'store_employee' });
    this.selectedRole.set('store_employee');
    this.form.controls['password'].setValidators(Validators.required);
    this.form.controls['password'].updateValueAndValidity();
    this.showForm.set(true);
  }

  openEdit(user: User): void {
    this.editingUser.set(user);
    this.form.patchValue({
      email: user.email,
      password: '',
      displayName: user.displayName,
      role: user.role as UserRole,
      storeId: user.storeId ?? '',
      regionId: user.regionId ?? '',
    });
    this.selectedRole.set(user.role);
    this.form.controls['password'].clearValidators();
    this.form.controls['password'].updateValueAndValidity();
    this.showForm.set(true);
  }

  closeForm(): void {
    this.showForm.set(false);
    this.editingUser.set(null);
    this.form.reset();
  }

  onSubmit(): void {
    if (this.form.invalid || this.saving()) return;
    const { email, password, displayName, role, storeId, regionId } = this.form.getRawValue();
    this.saving.set(true);
    const editing = this.editingUser();
    if (editing) {
      this.org.updateUser(editing.userId, {
        displayName: displayName!,
        role: role!,
        storeId: storeId || undefined,
        regionId: regionId || undefined,
      }).subscribe({
        next: () => { this.saving.set(false); this.closeForm(); this.load(); },
        error: () => { this.error.set('Failed to update user.'); this.saving.set(false); },
      });
    } else {
      this.org.createUser({
        email: email!,
        password: password!,
        displayName: displayName!,
        role: role!,
        storeId: storeId || undefined,
        regionId: regionId || undefined,
      }).subscribe({
        next: () => { this.saving.set(false); this.closeForm(); this.load(); },
        error: () => { this.error.set('Failed to create user.'); this.saving.set(false); },
      });
    }
  }

  // TB-18: Deactivate / Reactivate
  deactivate(user: User): void {
    if (!confirm(`Deactivate "${user.displayName}"? They will lose access immediately.`)) return;
    this.org.deactivateUser(user.userId).subscribe({ next: () => this.load() });
  }

  reactivate(user: User): void {
    if (!confirm(`Reactivate "${user.displayName}"?`)) return;
    this.org.reactivateUser(user.userId).subscribe({ next: () => this.load() });
  }

  // TB-17: Store assignments panel
  openAssignments(user: User): void {
    this.selectedUser.set(user);
    this.assigningStore.set('');
    this.loadingAssignments.set(true);
    this.org.getStoreAssignments(user.userId).subscribe({
      next: (a) => { this.assignments.set(a); this.loadingAssignments.set(false); },
    });
  }

  closeAssignments(): void {
    this.selectedUser.set(null);
    this.assignments.set([]);
  }

  addAssignment(): void {
    const storeId = this.assigningStore();
    const user = this.selectedUser();
    if (!storeId || !user) return;
    this.org.addStoreAssignment(user.userId, storeId).subscribe({
      next: () => {
        this.assigningStore.set('');
        this.org.getStoreAssignments(user.userId).subscribe({ next: (a) => this.assignments.set(a) });
      },
    });
  }

  removeAssignment(storeId: string): void {
    const user = this.selectedUser();
    if (!user) return;
    this.org.removeStoreAssignment(user.userId, storeId).subscribe({
      next: () => {
        this.org.getStoreAssignments(user.userId).subscribe({ next: (a) => this.assignments.set(a) });
      },
    });
  }

  readonly roles: { value: UserRole; label: string }[] = [
    { value: 'store_employee', label: 'Store Employee' },
    { value: 'store_manager', label: 'Store Manager' },
    { value: 'supervisor', label: 'Supervisor' },
    { value: 'admin', label: 'Admin' },
  ];
}
