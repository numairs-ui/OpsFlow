import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '@org/data-access-auth';
import { OrgService, type Store, type StoreEmployee } from '@org/data-access-org';
import { ChecklistService, type ChecklistDto } from '@org/data-access-templates';
import {
  RecurringAssignmentService,
  type RecurringAssignmentDto,
  type CreateRecurringAssignmentRequest,
} from '@org/data-access-tasks';
import { CronPickerComponent } from '@org/ui-cron-picker';
import { nonEmptyArray } from '@org/ui-core';

@Component({
  selector: 'app-recurring-assignments',
  imports: [ReactiveFormsModule, CronPickerComponent, DatePipe],
  templateUrl: './recurring-assignments.component.html',
  styleUrl: './recurring-assignments.component.scss',
})
export class RecurringAssignmentsComponent implements OnInit {
  private readonly svc = inject(RecurringAssignmentService);
  private readonly checklistSvc = inject(ChecklistService);
  private readonly orgSvc = inject(OrgService);
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);

  readonly assignments = signal<RecurringAssignmentDto[]>([]);
  readonly checklists = signal<ChecklistDto[]>([]);
  readonly stores = signal<Store[]>([]);
  readonly employees = signal<StoreEmployee[]>([]);
  readonly loadingEmployees = signal(false);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly showForm = signal(false);
  readonly cronExpression = signal('0 0 9 * * ?');

  readonly currentUser = this.auth.currentUser;

  // Drives assignee visibility: a specific employee only makes sense for exactly one store.
  readonly selectedStoreIds = signal<string[]>([]);
  readonly singleStore = computed(() => this.selectedStoreIds().length === 1);

  readonly form = this.fb.group({
    name: ['', Validators.required],
    checklistId: ['', Validators.required],
    storeIds: [[] as string[], nonEmptyArray],
    startsAt: [new Date().toISOString().slice(0, 10), Validators.required],
    endsAt: [''],
    assignedToUserId: [''],
  });

  ngOnInit(): void {
    this.load();
    this.checklistSvc.getChecklists(undefined, true).subscribe({ next: (r) => this.checklists.set(r) });
    this.orgSvc.getStores(undefined, true).subscribe({ next: (s) => this.stores.set(s) });

    this.form.controls['storeIds'].valueChanges.subscribe((ids) => {
      const storeIds = (ids ?? []) as string[];
      this.selectedStoreIds.set(storeIds);

      // The assignee picker only applies to a single store — load its roster, else clear it.
      if (storeIds.length === 1) {
        this.orgSvc.getStoreEmployees(storeIds[0]).subscribe({ next: (e) => this.employees.set(e) });
      } else {
        this.employees.set([]);
        this.form.controls['assignedToUserId'].setValue('');
      }
    });
  }

  private load(): void {
    this.loading.set(true);
    this.svc.getRecurringAssignments().subscribe({
      next: (r) => { this.assignments.set(r); this.loading.set(false); },
      error: () => { this.error.set('Failed to load assignments.'); this.loading.set(false); },
    });
  }

  openCreate(): void {
    this.form.reset({ startsAt: new Date().toISOString().slice(0, 10), storeIds: [] });
    this.selectedStoreIds.set([]);
    this.employees.set([]);
    this.cronExpression.set('0 0 9 * * ?');
    this.showForm.set(true);
  }

  closeForm(): void { this.showForm.set(false); }

  onCronChange(cron: string): void { this.cronExpression.set(cron); }

  onSubmit(): void {
    if (this.form.invalid || this.saving()) return;
    const { name, checklistId, storeIds, startsAt, endsAt, assignedToUserId } = this.form.getRawValue();
    const targetStoreIds = (storeIds ?? []) as string[];
    const body: CreateRecurringAssignmentRequest = {
      name: name!,
      checklistId: checklistId!,
      targetStoreIds,
      cronExpression: this.cronExpression(),
      startsAt: new Date(startsAt!).toISOString(),
      endsAt: endsAt ? new Date(endsAt).toISOString() : undefined,
      // Only send an assignee when targeting exactly one store (matches the backend validator rule).
      assignedToUserId: targetStoreIds.length === 1 && assignedToUserId ? assignedToUserId : undefined,
    };
    this.saving.set(true);
    this.svc.createRecurringAssignment(body).subscribe({
      next: () => { this.saving.set(false); this.closeForm(); this.load(); },
      error: () => { this.error.set('Failed to create assignment.'); this.saving.set(false); },
    });
  }

  storeNames(a: RecurringAssignmentDto): string {
    return a.targetStores.map((t) => t.storeName).join(', ');
  }

  togglePause(a: RecurringAssignmentDto): void {
    const op$ = a.isPaused ? this.svc.resume(a.id) : this.svc.pause(a.id);
    op$.subscribe({ next: () => this.load() });
  }

  deleteAssignment(a: RecurringAssignmentDto): void {
    if (!confirm(`Delete assignment "${a.name}"? All pending task instances will remain.`)) return;
    this.svc.delete(a.id).subscribe({ next: () => this.load() });
  }
}
