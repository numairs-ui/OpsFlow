import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { OrgService, type Store, type StoreEmployee } from '@org/data-access-org';
import { TemplateService, type TemplateDto } from '@org/data-access-templates';
import { TaskService, type CreateTaskRequest } from '@org/data-access-tasks';

type TaskMode = 'template' | 'notes';

@Component({
  selector: 'app-create-task',
  imports: [ReactiveFormsModule],
  templateUrl: './create-task.component.html',
  styleUrl: './create-task.component.scss',
})
export class CreateTaskComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly orgSvc = inject(OrgService);
  private readonly templateSvc = inject(TemplateService);
  private readonly taskSvc = inject(TaskService);
  private readonly router = inject(Router);

  readonly stores = signal<Store[]>([]);
  readonly templates = signal<TemplateDto[]>([]);
  readonly employees = signal<StoreEmployee[]>([]);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly mode = signal<TaskMode>('template');
  readonly isTemplateMode = computed(() => this.mode() === 'template');

  readonly form = this.fb.group({
    storeId: ['', Validators.required],
    dueAt: [this.defaultDueAt(), Validators.required],
    taskTemplateId: [''],
    notes: [''],
    assignedToUserId: [''],
  });

  ngOnInit(): void {
    this.orgSvc.getStores(undefined, true).subscribe({ next: (s) => this.stores.set(s) });
    this.templateSvc.getTemplates({ isActive: true, pageSize: 200 }).subscribe({
      next: (r) => this.templates.set(r.items),
    });

    this.form.controls['storeId'].valueChanges.subscribe((storeId) => {
      if (storeId) {
        this.orgSvc.getStoreEmployees(storeId).subscribe({ next: (e) => this.employees.set(e) });
      } else {
        this.employees.set([]);
        this.form.controls['assignedToUserId'].setValue('');
      }
    });
  }

  setMode(mode: TaskMode): void {
    this.mode.set(mode);
    const templateCtrl = this.form.controls['taskTemplateId'];
    if (mode === 'template') {
      templateCtrl.setValidators(Validators.required);
    } else {
      templateCtrl.clearValidators();
      templateCtrl.setValue('');
    }
    templateCtrl.updateValueAndValidity();
  }

  onSubmit(): void {
    if (this.form.invalid || this.saving()) return;
    const { storeId, dueAt, taskTemplateId, notes, assignedToUserId } = this.form.getRawValue();

    const body: CreateTaskRequest = {
      storeId: storeId!,
      dueAt: new Date(dueAt!).toISOString(),
      notes: notes || undefined,
      taskTemplateId: this.isTemplateMode() ? (taskTemplateId || undefined) : undefined,
      assignedToUserId: assignedToUserId || undefined,
    };

    this.saving.set(true);
    this.error.set(null);
    this.taskSvc.createTask(body).subscribe({
      next: () => { this.saving.set(false); this.router.navigate(['/admin/tasks']); },
      error: () => { this.error.set('Failed to create task.'); this.saving.set(false); },
    });
  }

  cancel(): void { this.router.navigate(['/admin/tasks']); }

  private defaultDueAt(): string {
    // Default to end of today, local time, formatted for datetime-local input.
    const d = new Date();
    d.setHours(23, 0, 0, 0);
    const tzOffset = d.getTimezoneOffset() * 60000;
    return new Date(d.getTime() - tzOffset).toISOString().slice(0, 16);
  }
}
