import { SlicePipe } from '@angular/common';
import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '@org/data-access-auth';
import { OrgService, type Region, type Store, type StoreAssignment, type User, type UserActivity, type UserRole } from '@org/data-access-org';
import { noWhitespace, nonEmptyArray, roleLabel } from '@org/ui-core';

const STORE_SCOPED_ROLES: UserRole[] = ['store_manager', 'store_employee', 'store_kiosk'];
const REGION_SCOPED_ROLES: UserRole[] = ['supervisor', 'admin'];

@Component({
  selector: 'app-users',
  imports: [ReactiveFormsModule, SlicePipe],
  templateUrl: './users.component.html',
  styleUrl: './users.component.scss',
})
export class UsersComponent implements OnInit {
  private readonly org = inject(OrgService);
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);

  readonly roleLabel = roleLabel;
  readonly isSuperAdmin = computed(() => this.auth.currentUser()?.role === 'super_admin');

  readonly users = signal<User[]>([]);
  readonly regions = signal<Region[]>([]);
  readonly stores = signal<Store[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly showForm = signal(false);
  readonly editingUser = signal<User | null>(null);

  // Detail panel
  readonly detailUser = signal<User | null>(null);
  readonly assignments = signal<StoreAssignment[]>([]);
  readonly loadingAssignments = signal(false);
  readonly assigningStore = signal<string>('');
  readonly activity = signal<UserActivity[]>([]);
  readonly loadingActivity = signal(false);

  // Inline two-step deactivate confirmation (no browser confirm())
  readonly confirmingUserId = signal<string | null>(null);

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
    displayName: ['', [Validators.required, noWhitespace]],
    role: ['store_employee' as UserRole, Validators.required],
    storeId: [''],
    regionIds: [[] as string[]],
  });

  readonly selectedRole = signal<string>('store_employee');

  // super_admin can mint any role; a region-scoped admin can only create roles below admin.
  readonly availableRoles = computed(() =>
    this.isSuperAdmin() ? this.roles : this.roles.filter((r) => r.value !== 'super_admin' && r.value !== 'admin')
  );

  readonly needsStore = computed(() => STORE_SCOPED_ROLES.includes(this.selectedRole() as UserRole));
  readonly needsRegions = computed(() => REGION_SCOPED_ROLES.includes(this.selectedRole() as UserRole));
  readonly singleRegionOnly = computed(() => this.selectedRole() === 'supervisor');

  ngOnInit(): void {
    this.load();
    this.org.getRegions(true).subscribe({ next: (r) => this.regions.set(r) });
    this.org.getStores(undefined, true).subscribe({ next: (s) => this.stores.set(s) });
    this.form.controls['role'].valueChanges.subscribe((v) => {
      const role = (v ?? '') as UserRole;
      this.selectedRole.set(role);
      const storeCtrl = this.form.controls['storeId'];
      const regionCtrl = this.form.controls['regionIds'];

      if (STORE_SCOPED_ROLES.includes(role)) {
        storeCtrl.setValidators(Validators.required);
      } else {
        storeCtrl.clearValidators();
        storeCtrl.setValue('');
      }

      if (REGION_SCOPED_ROLES.includes(role)) {
        regionCtrl.setValidators(nonEmptyArray);
      } else {
        regionCtrl.clearValidators();
        regionCtrl.setValue([]);
      }
      storeCtrl.updateValueAndValidity();
      regionCtrl.updateValueAndValidity();
    });
  }

  private load(): void {
    this.loading.set(true);
    this.org.getUsers({ activeOnly: false }).subscribe({
      next: (data) => { this.users.set(data); this.loading.set(false); },
      error: () => { this.error.set('Failed to load users.'); this.loading.set(false); },
    });
  }

  activityTypeLabel(type: string): string {
    return type === 'form' ? 'Form Submission' : 'Task';
  }

  // ── Create / Edit form ──────────────────────────────────────────────────────

  openCreate(): void {
    this.editingUser.set(null);
    this.form.reset({ role: 'store_employee', storeId: '', regionIds: [] });
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
      regionIds: user.regionIds ?? [],
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

  editFromDetail(): void {
    const user = this.detailUser();
    if (!user) return;
    this.closeDetail();
    this.openEdit(user);
  }

  onSubmit(): void {
    if (this.form.invalid || this.saving()) return;
    const { email, password, displayName, role, storeId, regionIds } = this.form.getRawValue();
    const scopedStore = STORE_SCOPED_ROLES.includes(role as UserRole) ? (storeId || undefined) : undefined;
    const scopedRegions = REGION_SCOPED_ROLES.includes(role as UserRole) ? (regionIds ?? []) : [];
    this.saving.set(true);
    this.error.set(null);
    const editing = this.editingUser();
    if (editing) {
      this.org.updateUser(editing.userId, {
        displayName: displayName!,
        role: role!,
        storeId: scopedStore,
        regionIds: scopedRegions,
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
        storeId: scopedStore,
        regionIds: scopedRegions,
      }).subscribe({
        next: () => { this.saving.set(false); this.closeForm(); this.load(); },
        error: () => { this.error.set('Failed to create user.'); this.saving.set(false); },
      });
    }
  }

  // ── Inline two-step deactivate (no browser confirm) ─────────────────────────

  beginDeactivate(userId: string): void {
    this.confirmingUserId.set(userId);
  }

  cancelDeactivate(): void {
    this.confirmingUserId.set(null);
  }

  executeDeactivate(user: User): void {
    this.confirmingUserId.set(null);
    this.error.set(null);
    this.org.deactivateUser(user.userId).subscribe({
      next: () => {
        // If deactivated from within the detail panel, close it first
        if (this.detailUser()?.userId === user.userId) this.closeDetail();
        this.load();
      },
      error: () => this.error.set(`Failed to deactivate "${user.displayName}". Please try again.`),
    });
  }

  reactivate(user: User): void {
    this.error.set(null);
    this.org.reactivateUser(user.userId).subscribe({
      next: () => {
        if (this.detailUser()?.userId === user.userId) this.closeDetail();
        this.load();
      },
      error: () => this.error.set(`Failed to reactivate "${user.displayName}". Please try again.`),
    });
  }

  // ── Detail panel ────────────────────────────────────────────────────────────

  openDetail(user: User): void {
    this.detailUser.set(user);
    this.confirmingUserId.set(null);
    this.assigningStore.set('');
    this.activity.set([]);

    // Load store assignments for managers
    if (user.role === 'store_manager') {
      this.loadingAssignments.set(true);
      this.org.getStoreAssignments(user.userId).subscribe({
        next: (a) => { this.assignments.set(a); this.loadingAssignments.set(false); },
        error: () => this.loadingAssignments.set(false),
      });
    } else {
      this.assignments.set([]);
    }

    // Load activity history
    this.loadingActivity.set(true);
    this.org.getUserActivity(user.userId).subscribe({
      next: (items) => { this.activity.set(items); this.loadingActivity.set(false); },
      error: () => this.loadingActivity.set(false),
    });
  }

  closeDetail(): void {
    this.detailUser.set(null);
    this.assignments.set([]);
    this.activity.set([]);
    this.confirmingUserId.set(null);
  }

  addAssignment(): void {
    const storeId = this.assigningStore();
    const user = this.detailUser();
    if (!storeId || !user) return;
    this.org.addStoreAssignment(user.userId, storeId).subscribe({
      next: () => {
        this.assigningStore.set('');
        this.org.getStoreAssignments(user.userId).subscribe({ next: (a) => this.assignments.set(a) });
      },
    });
  }

  removeAssignment(storeId: string): void {
    const user = this.detailUser();
    if (!user) return;
    this.org.removeStoreAssignment(user.userId, storeId).subscribe({
      next: () => {
        this.org.getStoreAssignments(user.userId).subscribe({ next: (a) => this.assignments.set(a) });
      },
    });
  }

  readonly roles: { value: UserRole; label: string }[] = [
    { value: 'super_admin', label: 'Super Admin' },
    { value: 'admin', label: 'Administrator' },
    { value: 'supervisor', label: 'Supervisor' },
    { value: 'store_manager', label: 'Store Manager' },
    { value: 'store_employee', label: 'Store Employee' },
    { value: 'store_kiosk', label: 'Store Kiosk' },
  ];
}
