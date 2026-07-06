import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { OrgService, type StoreEmployee } from '@org/data-access-org';
import { TaskService } from '@org/data-access-tasks';
import type { TaskDetailDto, TaskTemplateItemDto } from '@org/data-access-tasks';

interface ParsedField {
  id: string;
  type: string;
  label: string;
  required: boolean;
  rangeMin?: number;
  rangeMax?: number;
  subItems?: { id: string; label: string; required: boolean }[];
}

type ModalType = 'cancel' | 'defer' | null;

@Component({
  selector: 'app-task-detail',
  imports: [DatePipe],
  templateUrl: './task-detail.component.html',
  styleUrl: './task-detail.component.scss',
})
export class TaskDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly taskSvc = inject(TaskService);
  private readonly orgSvc = inject(OrgService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly task = signal<TaskDetailDto | null>(null);
  readonly actionBusy = signal(false);

  readonly employees = signal<StoreEmployee[]>([]);
  readonly userNames = signal<Map<string, string>>(new Map());
  readonly assigningTo = signal<string>('');
  readonly assigning = signal(false);

  readonly activeModal = signal<ModalType>(null);
  readonly cancelReason = signal('');
  readonly deferReason = signal('');
  readonly deferDate = signal('');

  readonly parsedTemplates = computed((): Array<{ template: TaskTemplateItemDto; fields: ParsedField[] }> => {
    const t = this.task();
    if (!t) return [];
    return t.templates.map((tmpl) => ({ template: tmpl, fields: this.parseFields(tmpl) }));
  });

  readonly canAssign = computed(() =>
    ['Pending', 'InProgress', 'Overdue', 'CorrectiveActionRaised'].includes(this.task()?.status ?? '')
  );
  readonly canDefer = computed(() => {
    const s = this.task()?.status;
    return s !== undefined && !['Completed', 'Verified', 'Cancelled', 'Deferred'].includes(s);
  });
  readonly canCancel = computed(() => {
    const s = this.task()?.status;
    return s !== undefined && !['Completed', 'Verified', 'Cancelled'].includes(s);
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) { this.goBack(); return; }
    this.loadTask(id);
  }

  private loadTask(id: string): void {
    this.loading.set(true);
    this.taskSvc.getTask(id).subscribe({
      next: (t) => {
        this.task.set(t);
        this.loading.set(false);
        this.orgSvc.getStoreEmployees(t.storeId).subscribe({ next: (e) => this.employees.set(e) });
        this.orgSvc.getUsers({ activeOnly: false }).subscribe({
          next: (users) => this.userNames.set(new Map(users.map((u) => [u.userId, u.displayName]))),
        });
      },
      error: () => { this.error.set('Failed to load task.'); this.loading.set(false); },
    });
  }

  parseFields(template: TaskTemplateItemDto): ParsedField[] {
    try { return JSON.parse(template.fieldsJson) as ParsedField[]; }
    catch { return []; }
  }

  assigneeName(userId: string | undefined): string {
    if (!userId) return 'Unassigned';
    return this.userNames().get(userId) ?? userId;
  }

  statusPillClass(status: string): string {
    switch (status) {
      case 'Completed':
      case 'Verified':
        return 'pill--completed';
      case 'InProgress':
        return 'pill--inprogress';
      case 'Overdue':
      case 'CorrectiveActionRaised':
        return 'pill--overdue';
      case 'Cancelled':
        return 'pill--inactive';
      default:
        return 'pill--pending';
    }
  }

  assign(): void {
    const taskId = this.task()?.id;
    if (!taskId || this.assigning()) return;
    this.assigning.set(true);
    this.taskSvc.assignTask(taskId, this.assigningTo() || null).subscribe({
      next: () => { this.assigning.set(false); this.loadTask(taskId); },
      error: () => { this.assigning.set(false); this.error.set('Failed to update assignment.'); },
    });
  }

  openModal(type: ModalType): void {
    this.cancelReason.set('');
    this.deferReason.set('');
    this.deferDate.set('');
    this.activeModal.set(type);
  }

  submitCancel(): void {
    const t = this.task();
    if (!t || !this.cancelReason().trim() || this.actionBusy()) return;
    this.actionBusy.set(true);
    this.taskSvc.cancelTask(t.id, { reason: this.cancelReason() }).subscribe({
      next: () => { this.actionBusy.set(false); this.activeModal.set(null); this.loadTask(t.id); },
      error: () => { this.actionBusy.set(false); this.error.set('Failed to cancel task.'); },
    });
  }

  submitDefer(): void {
    const t = this.task();
    if (!t || !this.deferReason().trim() || !this.deferDate() || this.actionBusy()) return;
    this.actionBusy.set(true);
    this.taskSvc.deferTask(t.id, { reason: this.deferReason(), deferredTo: this.deferDate() }).subscribe({
      next: () => { this.actionBusy.set(false); this.activeModal.set(null); this.loadTask(t.id); },
      error: () => { this.actionBusy.set(false); this.error.set('Failed to defer task.'); },
    });
  }

  minDeferDate(): string {
    const d = new Date();
    d.setDate(d.getDate() + 1);
    return d.toISOString().slice(0, 10);
  }

  goBack(): void {
    this.router.navigate(['/admin/tasks']);
  }
}
