import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { AuthService } from '@org/data-access-auth';
import { OrgService, type Store, type StoreEmployee } from '@org/data-access-org';
import { ChecklistService, type ChecklistDto } from '@org/data-access-templates';
import {
  RecurringAssignmentService,
  type RecurringAssignmentDto,
  type CreateRecurringAssignmentRequest,
} from '@org/data-access-tasks';
import { CronPickerComponent } from '@org/ui-cron-picker';

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
  private readonly route = inject(ActivatedRoute);

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

  readonly form = this.fb.nonNullable.group({
    name: ['', Validators.required],
    checklistId: ['', Validators.required],
    storeId: ['', Validators.required],
    startsAt: [new Date().toISOString().slice(0, 10), Validators.required],
    endsAt: [''],
    assignedToUserId: [''],
  });

  ngOnInit(): void {
    this.load();
    this.checklistSvc.getChecklists(undefined, true).subscribe({ next: (r) => this.checklists.set(r) });
    this.orgSvc.getStores(undefined, true).subscribe({ next: (s) => this.stores.set(s) });
    if (this.route.snapshot.queryParamMap.has('create')) this.openCreate();
  }

  private load(): void {
    this.loading.set(true);
    this.svc.getRecurringAssignments().subscribe({
      next: (r) => { this.assignments.set(r); this.loading.set(false); },
      error: () => { this.error.set('Failed to load assignments.'); this.loading.set(false); },
    });
  }

  openCreate(): void {
    this.form.reset({ startsAt: new Date().toISOString().slice(0, 10) });
    this.cronExpression.set('0 0 9 * * ?');
    this.showForm.set(true);
  }

  closeForm(): void { this.showForm.set(false); }

  onCronChange(cron: string): void { this.cronExpression.set(cron); }

  onStoreChange(storeId: string): void {
    this.orgSvc.getStoreEmployees(storeId).subscribe({ next: e => this.employees.set(e) });
  }

  onSubmit(): void {
    if (this.form.invalid || this.saving()) return;
    const { name, checklistId, storeId, startsAt, endsAt, assignedToUserId } = this.form.getRawValue();
    const body: CreateRecurringAssignmentRequest = {
      name,
      checklistId,
      storeId,
      cronExpression: this.cronExpression(),
      startsAt: new Date(startsAt).toISOString(),
      endsAt: endsAt ? new Date(endsAt).toISOString() : undefined,
      assignedToUserId: assignedToUserId || undefined,
    };
    this.saving.set(true);
    this.svc.createRecurringAssignment(body).subscribe({
      next: () => { this.saving.set(false); this.closeForm(); this.load(); },
      error: () => { this.error.set('Failed to create assignment.'); this.saving.set(false); },
    });
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
