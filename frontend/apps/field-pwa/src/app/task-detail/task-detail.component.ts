import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '@org/data-access-auth';
import { OrgService, type StoreEmployee } from '@org/data-access-org';
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
  private readonly orgSvc = inject(OrgService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly task = signal<TaskDetailDto | null>(null);
  readonly storeSettings = signal<StoreSettingsDto | null>(null);
  readonly actionBusy = signal(false);

  // Completion
  readonly fieldValues = signal<Record<string, string>>({});
  readonly completionResult = signal<CompleteTaskResponse | null>(null);
  readonly completedByVolunteer = signal('');

  // Photo capture — per-field upload state keyed by `${templateId}:${fieldId}`
  readonly photoBusy = signal<Record<string, boolean>>({});
  readonly photoError = signal<Record<string, string>>({});

  // Checklist-session scoring (A3): per-item score + optional item photo, keyed by templateId.
  readonly itemScores = signal<Record<string, number>>({});
  readonly itemPhotos = signal<Record<string, string>>({});

  // Manager modals
  readonly activeModal = signal<ModalType>(null);
  readonly cancelReason = signal('');
  readonly deferReason = signal('');
  readonly deferDate = signal('');

  // Assign / claim
  readonly employees = signal<StoreEmployee[]>([]);
  readonly assigningTo = signal<string>('');
  readonly assigning = signal(false);
  readonly claiming = signal(false);

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
  readonly canAssign = computed(() => this.isManager() &&
    ['Pending', 'InProgress', 'Overdue'].includes(this.task()?.status ?? ''));
  readonly canClaim = computed(() => !this.isManager() &&
    !this.task()?.assignedToUserId &&
    ['Pending', 'InProgress', 'Overdue'].includes(this.task()?.status ?? ''));

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
        const storeId = this.auth.currentUser()?.storeId;
        if (storeId && this.isManager()) {
          this.orgSvc.getStoreEmployees(storeId).subscribe({ next: e => this.employees.set(e) });
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

  fieldKey(templateId: string, fieldId: string): string {
    return `${templateId}:${fieldId}`;
  }

  isPhotoBusy(templateId: string, fieldId: string): boolean {
    return this.photoBusy()[this.fieldKey(templateId, fieldId)] === true;
  }

  photoFieldError(templateId: string, fieldId: string): string {
    return this.photoError()[this.fieldKey(templateId, fieldId)] ?? '';
  }

  /** File chosen from the camera/gallery → compress → upload to a signed URL → store the blob URL. */
  async onPhotoSelected(templateId: string, fieldId: string, event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    const key = this.fieldKey(templateId, fieldId);
    const t = this.task();
    if (!t) return;

    this.photoBusy.update(v => ({ ...v, [key]: true }));
    this.photoError.update(v => ({ ...v, [key]: '' }));

    try {
      const blob = await this.compressImage(file);
      const { uploadUrl, blobUrl } = await firstValueFrom(
        this.taskSvc.getPhotoUploadUrl(t.id, templateId, fieldId),
      );

      // PUT the bytes straight to storage — bypassing our API (and its JWT interceptor), which is
      // the whole point of a signed URL. Azure Blob requires the x-ms-blob-type header; Supabase's
      // signed upload URL does not.
      const headers: Record<string, string> = { 'Content-Type': 'image/jpeg' };
      if (uploadUrl.includes('blob.core.windows.net')) headers['x-ms-blob-type'] = 'BlockBlob';

      const res = await fetch(uploadUrl, { method: 'PUT', headers, body: blob });
      if (!res.ok) throw new Error(`Upload failed (${res.status})`);

      // The stored value is the blob URL — it submits with the rest of the completion like any field.
      this.setFieldValue(templateId, fieldId, blobUrl);
    } catch {
      this.photoError.update(v => ({ ...v, [key]: 'Upload failed. Tap to try again.' }));
    } finally {
      this.photoBusy.update(v => ({ ...v, [key]: false }));
      input.value = ''; // allow re-selecting the same file after a failure
    }
  }

  clearPhoto(templateId: string, fieldId: string): void {
    this.setFieldValue(templateId, fieldId, '');
    this.photoError.update(v => ({ ...v, [this.fieldKey(templateId, fieldId)]: '' }));
  }

  /**
   * Downscale to a max edge and re-encode as JPEG so a multi-MB phone photo uploads quickly.
   * Falls back to the original file if the browser can't decode it (e.g. HEIC without support).
   */
  private async compressImage(file: File, maxEdge = 1600, quality = 0.8): Promise<Blob> {
    try {
      const bitmap = await createImageBitmap(file);
      const scale = Math.min(1, maxEdge / Math.max(bitmap.width, bitmap.height));
      const width = Math.round(bitmap.width * scale);
      const height = Math.round(bitmap.height * scale);

      const canvas = document.createElement('canvas');
      canvas.width = width;
      canvas.height = height;
      const ctx = canvas.getContext('2d');
      if (!ctx) return file;
      ctx.drawImage(bitmap, 0, 0, width, height);
      bitmap.close();

      const blob = await new Promise<Blob | null>(resolve =>
        canvas.toBlob(resolve, 'image/jpeg', quality),
      );
      return blob ?? file;
    } catch {
      return file;
    }
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

  // ── Checklist-session scoring (A3) ───────────────────────────────────────────

  isScored(template: TaskTemplateItemDto): boolean {
    return template.scoringType === 'PassFail' || template.scoringType === 'Scale1To5';
  }

  getItemScore(templateId: string): number | null {
    const v = this.itemScores()[templateId];
    return v === undefined ? null : v;
  }

  setItemScore(templateId: string, score: number): void {
    this.itemScores.update((v) => ({ ...v, [templateId]: score }));
  }

  itemPhotoUrl(templateId: string): string {
    return this.itemPhotos()[templateId] ?? '';
  }

  async onItemPhotoSelected(templateId: string, event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    const t = this.task();
    if (!file || !t) return;

    const key = `${templateId}:__item`;
    this.photoBusy.update((v) => ({ ...v, [key]: true }));
    try {
      const blob = await this.compressImage(file);
      const { uploadUrl, blobUrl } = await firstValueFrom(
        this.taskSvc.getPhotoUploadUrl(t.id, templateId, 'item-score'),
      );
      const headers: Record<string, string> = { 'Content-Type': 'image/jpeg' };
      if (uploadUrl.includes('blob.core.windows.net')) headers['x-ms-blob-type'] = 'BlockBlob';
      const res = await fetch(uploadUrl, { method: 'PUT', headers, body: blob });
      if (!res.ok) throw new Error(`Upload failed (${res.status})`);
      this.itemPhotos.update((v) => ({ ...v, [templateId]: blobUrl }));
    } catch {
      this.photoError.update((v) => ({ ...v, [key]: 'Upload failed. Tap to try again.' }));
    } finally {
      this.photoBusy.update((v) => ({ ...v, [key]: false }));
      input.value = '';
    }
  }

  isItemPhotoBusy(templateId: string): boolean {
    return this.photoBusy()[`${templateId}:__item`] === true;
  }

  submitCompletion(): void {
    const t = this.task();
    if (!t || this.actionBusy()) return;

    const submissions: FieldSubmission[] = [];
    for (const [key, value] of Object.entries(this.fieldValues())) {
      const [templateId, fieldId] = key.split(':');
      submissions.push({ templateId, fieldId, value });
    }

    // One score per scored item; Pass/Fail encodes Pass = 1, Fail = 0.
    const itemScores = this.parsedTemplates()
      .map((g) => g.template)
      .filter((tmpl) => this.isScored(tmpl))
      .map((tmpl) => ({
        templateId: tmpl.templateId,
        score: this.itemScores()[tmpl.templateId] ?? 0,
        photoUrl: this.itemPhotos()[tmpl.templateId] || undefined,
      }));

    this.actionBusy.set(true);
    this.error.set(null);
    this.taskSvc.completeTask(t.id, {
      completedByVolunteerName: this.completedByVolunteer() || undefined,
      fieldValues: submissions,
      itemScores: itemScores.length > 0 ? itemScores : undefined,
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

  assign(): void {
    const taskId = this.task()?.id;
    if (!taskId || this.assigning()) return;
    this.assigning.set(true);
    this.taskSvc.assignTask(taskId, this.assigningTo() || null).subscribe({
      next: () => { this.assigning.set(false); this.loadTask(taskId); },
      error: () => this.assigning.set(false),
    });
  }

  claim(): void {
    const taskId = this.task()?.id;
    if (!taskId || this.claiming()) return;
    this.claiming.set(true);
    this.taskSvc.claimTask(taskId).subscribe({
      next: () => { this.claiming.set(false); this.loadTask(taskId); },
      error: () => this.claiming.set(false),
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
