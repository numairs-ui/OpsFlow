import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '@org/data-access-auth';
import { InventoryService, TaskService } from '@org/data-access-tasks';
import type {
  CompleteTaskResponse, DoughNeedTargetDto,
  FieldSubmission, StoreSettingsDto, TaskDetailDto, TaskTemplateItemDto,
} from '@org/data-access-tasks';

interface ParsedField {
  id: string;
  type: string;
  label: string;
  required: boolean;
  rangeMin?: number;
  rangeMax?: number;
  correctiveActionText?: string;
  subItems?: { id: string; label: string; required: boolean }[];
}

type ModalType = 'complete' | 'cancel' | 'defer' | null;

@Component({
  selector: 'app-task-detail',
  imports: [DatePipe, DecimalPipe],
  templateUrl: './task-detail.component.html',
  styleUrl: './task-detail.component.scss',
})
export class TaskDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly taskSvc = inject(TaskService);
  private readonly inventorySvc = inject(InventoryService);
  private readonly auth = inject(AuthService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly task = signal<TaskDetailDto | null>(null);
  readonly storeSettings = signal<StoreSettingsDto | null>(null);
  readonly actionBusy = signal(false);

  // Completion
  readonly fieldValues = signal<Record<string, string>>({});
  readonly completionResult = signal<CompleteTaskResponse | null>(null);
  readonly completedByVolunteer = signal('');

  // Manager modals
  readonly activeModal = signal<ModalType>(null);
  readonly cancelReason = signal('');
  readonly deferReason = signal('');
  readonly deferDate = signal('');

  readonly isManager = computed(() => {
    const role = this.auth.currentUser()?.role ?? '';
    return ['store_manager', 'supervisor', 'admin', 'regional_manager'].includes(role);
  });

  readonly parsedTemplates = computed((): Array<{ template: TaskTemplateItemDto; fields: ParsedField[] }> => {
    const t = this.task();
    if (!t) return [];
    return t.templates.map(tmpl => ({
      template: tmpl,
      fields: this.parseFields(tmpl),
    }));
  });

  readonly canComplete = computed(() => {
    const t = this.task();
    return t !== null && (t.status === 'Pending' || t.status === 'InProgress' || t.status === 'Overdue');
  });
  readonly canVerify = computed(() => this.task()?.status === 'Completed' && this.isManager());
  readonly canCancel = computed(() => {
    const s = this.task()?.status;
    return this.isManager() && s !== undefined && !['Completed', 'Verified', 'Cancelled'].includes(s);
  });
  readonly canDefer = computed(() => {
    const s = this.task()?.status;
    return this.isManager() && s !== undefined && !['Completed', 'Verified', 'Cancelled', 'Deferred'].includes(s);
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) { this.router.navigate(['/tasks']); return; }
    this.loadTask(id);
  }

  private loadTask(id: string): void {
    this.taskSvc.getTask(id).subscribe({
      next: (t) => {
        this.task.set(t);
        this.loading.set(false);
        if (t.isMdog) {
          this.inventorySvc.getStoreSettings(t.storeId).subscribe({
            next: (s) => this.storeSettings.set(s),
            error: () => { /* non-fatal */ },
          });
          // Pre-populate previous values
          const prePopulated: Record<string, string> = {};
          for (const [fieldId, count] of Object.entries(t.previousValues ?? {})) {
            for (const tmpl of t.templates) {
              prePopulated[`${tmpl.templateId}:${fieldId}`] = String(count);
            }
          }
          this.fieldValues.set(prePopulated);
        }
      },
      error: () => { this.error.set('Failed to load task.'); this.loading.set(false); },
    });
  }

  parseFields(template: TaskTemplateItemDto): ParsedField[] {
    try { return JSON.parse(template.fieldsJson) as ParsedField[]; }
    catch { return []; }
  }

  getFieldValue(templateId: string, fieldId: string): string {
    return this.fieldValues()[`${templateId}:${fieldId}`] ?? '';
  }

  setFieldValue(templateId: string, fieldId: string, value: string): void {
    this.fieldValues.update(v => ({ ...v, [`${templateId}:${fieldId}`]: value }));
  }

  toggleSubItem(templateId: string, fieldId: string, subId: string): void {
    const key = `${templateId}:${fieldId}`;
    const current = (this.fieldValues()[key] ?? '').split(',').filter(Boolean);
    const idx = current.indexOf(subId);
    if (idx >= 0) current.splice(idx, 1); else current.push(subId);
    this.setFieldValue(templateId, fieldId, current.join(','));
  }

  isSubItemChecked(templateId: string, fieldId: string, subId: string): boolean {
    return (this.fieldValues()[`${templateId}:${fieldId}`] ?? '').split(',').includes(subId);
  }

  isPrepopulated(fieldId: string): boolean {
    const t = this.task();
    return t?.isMdog === true && Object.prototype.hasOwnProperty.call(t.previousValues, fieldId);
  }

  previousValue(fieldId: string): number | null {
    return this.task()?.previousValues?.[fieldId] ?? null;
  }

  needTarget(fieldId: string): DoughNeedTargetDto | null {
    return this.storeSettings()?.doughNeedTargets?.[fieldId] ?? null;
  }

  surplusDeficit(templateId: string, fieldId: string): { day2: number; day3: number } | null {
    const target = this.needTarget(fieldId);
    if (!target) return null;
    const val = parseFloat(this.getFieldValue(templateId, fieldId));
    if (isNaN(val)) return null;
    return { day2: val - target.day2Need, day3: val - target.day3Need };
  }

  submitCompletion(): void {
    const t = this.task();
    if (!t || this.actionBusy()) return;

    const submissions: FieldSubmission[] = [];
    for (const [key, value] of Object.entries(this.fieldValues())) {
      const [templateId, fieldId] = key.split(':');
      submissions.push({ templateId, fieldId, value });
    }

    this.actionBusy.set(true);
    this.error.set(null);
    this.taskSvc.completeTask(t.id, {
      completedByVolunteerName: this.completedByVolunteer() || undefined,
      fieldValues: submissions,
    }).subscribe({
      next: (res) => {
        this.actionBusy.set(false);
        this.completionResult.set(res);
        this.loadTask(t.id);
      },
      error: (err) => {
        this.actionBusy.set(false);
        this.error.set(err?.error?.detail ?? 'Failed to complete task. Check required fields.');
      },
    });
  }

  verify(): void {
    const t = this.task();
    if (!t || this.actionBusy()) return;
    this.actionBusy.set(true);
    this.taskSvc.verifyTask(t.id).subscribe({
      next: () => { this.actionBusy.set(false); this.loadTask(t.id); },
      error: () => { this.actionBusy.set(false); this.error.set('Failed to verify task.'); },
    });
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
      next: () => { this.actionBusy.set(false); this.activeModal.set(null); this.router.navigate(['/tasks']); },
      error: () => { this.actionBusy.set(false); this.error.set('Failed to defer task.'); },
    });
  }

  openModal(type: ModalType): void {
    this.cancelReason.set('');
    this.deferReason.set('');
    this.deferDate.set('');
    this.activeModal.set(type);
  }

  minDeferDate(): string {
    const d = new Date();
    d.setDate(d.getDate() + 1);
    return d.toISOString().slice(0, 10);
  }

  goBack(): void { this.router.navigate(['/tasks']); }
}
