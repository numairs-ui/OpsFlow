import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { AuthService } from '@org/data-access-auth';
import {
  FormSubmissionService,
  type FormSubmissionDetailDto,
  type FormSubmissionStatus,
  type FormSubmissionSummaryDto,
  type MySubmissionDto,
  type PendingReviewDto,
} from '@org/data-access-tasks';
import { FormTemplateService, type FormTemplateDto } from '@org/data-access-templates';
import { OrgService, type Store } from '@org/data-access-org';
import { TemplatePickerComponent, SaveToggleComponent, type SaveToggleMode } from '@org/ui-template-builder';
import type { TemplateField } from '@org/ui-field-builder';

type Tab = 'mine' | 'review' | 'all';
type Pile = 'All' | 'Draft' | 'PendingApproval' | 'Returned' | 'Rejected' | 'Approved' | 'Recorded';
type DetailMode = 'fill' | 'review' | 'view';

@Component({
  selector: 'app-form-submissions',
  imports: [DatePipe, TemplatePickerComponent, SaveToggleComponent],
  templateUrl: './form-submissions.component.html',
  styleUrl: './form-submissions.component.scss',
})
export class FormSubmissionsComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly submissionSvc = inject(FormSubmissionService);
  private readonly templateSvc = inject(FormTemplateService);
  private readonly orgSvc = inject(OrgService);

  readonly currentUser = this.auth.currentUser;
  readonly activeTab = signal<Tab>('mine');
  readonly pile = signal<Pile>('All');

  readonly isAdminOrSupervisor = computed(() => {
    const role = this.currentUser()?.role;
    return role === 'super_admin' || role === 'admin' || role === 'supervisor';
  });

  readonly mySubmissions = signal<MySubmissionDto[]>([]);
  readonly pendingReview = signal<PendingReviewDto[]>([]);
  readonly allSubmissions = signal<FormSubmissionSummaryDto[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly piles: Pile[] = ['All', 'Draft', 'PendingApproval', 'Returned', 'Rejected', 'Approved', 'Recorded'];

  readonly filteredMine = computed(() => {
    const p = this.pile();
    const items = this.mySubmissions();
    return p === 'All' ? items : items.filter((s) => s.status === p);
  });

  // Create flow
  readonly showCreatePicker = signal(false);
  readonly formTemplates = signal<FormTemplateDto[]>([]);
  readonly stores = signal<Store[]>([]);
  readonly selectedStoreId = signal<string | null>(null);

  // Detail / fill / review slide-over
  readonly detail = signal<FormSubmissionDetailDto | null>(null);
  readonly detailMode = signal<DetailMode>('view');
  readonly draftId = signal<string | null>(null);
  readonly draftTemplateFields = signal<TemplateField[]>([]);
  readonly fieldValues = signal<Record<string, string>>({});
  readonly detailBusy = signal(false);
  readonly detailError = signal<string | null>(null);

  readonly actionModal = signal<'reject' | 'return' | null>(null);
  readonly actionComment = signal('');

  readonly submissionStats = computed(() => {
    const isAdminSup = this.isAdminOrSupervisor();
    const items = isAdminSup ? this.allSubmissions() : this.mySubmissions();
    const review = this.pendingReview();
    if (!items.length && !review.length) return null;
    return {
      total: items.length,
      inFlight: items.filter(s => s.status === 'PendingApproval').length,
      awaitingReview: review.length,
      resolved: items.filter(s => s.status === 'Approved' || s.status === 'Recorded').length,
      needsAttention: items.filter(s => s.status === 'Returned' || s.status === 'Rejected' || s.status === 'Draft').length,
    };
  });

  ngOnInit(): void {
    this.loadMine();
    this.loadReview();
    if (this.isAdminOrSupervisor()) {
      this.loadAll();
      this.activeTab.set('all');
    }
  }

  switchTab(tab: Tab): void {
    this.activeTab.set(tab);
    if (tab === 'all') this.loadAll();
  }

  private loadMine(): void {
    this.loading.set(true);
    this.submissionSvc.getMySubmissions().subscribe({
      next: (r) => { this.mySubmissions.set(r); this.loading.set(false); },
      error: () => { this.error.set('Failed to load your submissions.'); this.loading.set(false); },
    });
  }

  private loadReview(): void {
    this.submissionSvc.getPendingReview().subscribe({
      next: (r) => this.pendingReview.set(r),
      error: () => undefined,
    });
  }

  private loadAll(): void {
    this.submissionSvc.getSubmissions().subscribe({
      next: (r) => this.allSubmissions.set(r),
      error: () => undefined,
    });
  }

  // --- Create flow ---
  openCreatePicker(): void {
    this.showCreatePicker.set(true);
    this.templateSvc.getFormTemplates({ isActive: true }).subscribe({
      next: (r) => this.formTemplates.set(r.items),
    });
    if (this.isAdminOrSupervisor() && !this.currentUser()?.storeId) {
      this.orgSvc.getStores().subscribe({ next: (r) => this.stores.set(r) });
    }
  }

  closeCreatePicker(): void {
    this.showCreatePicker.set(false);
  }

  private pendingCreateTemplateId: string | undefined;

  startNewSubmission(template: FormTemplateDto): void {
    this.templateSvc.getFormTemplate(template.id).subscribe({
      next: (full) => {
        let fields: TemplateField[] = [];
        try { fields = JSON.parse(full.fieldsJson); } catch { fields = []; }
        this.pendingCreateTemplateId = template.id;
        this.draftTemplateFields.set(fields);
        this.draftId.set(null);
        this.fieldValues.set({});
        this.detail.set(null);
        this.detailMode.set('fill');
        this.detailError.set(null);
        this.showCreatePicker.set(false);
      },
    });
  }

  openSummary(s: FormSubmissionSummaryDto): void {
    this.openMine({
      id: s.id,
      formTemplateId: s.formTemplateId,
      formTemplateName: s.formTemplateName,
      storeId: s.storeId,
      status: s.status,
      createdAt: s.createdAt,
      submittedAt: s.submittedAt,
      resolvedAt: s.resolvedAt,
    });
  }

  // --- Open existing submission ---
  openMine(s: MySubmissionDto): void {
    this.submissionSvc.getSubmission(s.id).subscribe({
      next: (d) => this.openDetailFromDto(d, s.status === 'Draft' || s.status === 'Returned' ? 'fill' : 'view'),
      error: () => this.error.set('Failed to load submission. Please try again.'),
    });
  }

  openReview(p: PendingReviewDto): void {
    this.submissionSvc.getSubmission(p.id).subscribe({
      next: (d) => this.openDetailFromDto(d, 'review'),
      error: () => this.error.set('Failed to load submission. Please try again.'),
    });
  }

  private openDetailFromDto(d: FormSubmissionDetailDto, mode: DetailMode): void {
    this.detail.set(d);
    this.detailMode.set(mode);
    this.draftId.set(d.id);
    let fields: TemplateField[] = [];
    try { fields = JSON.parse(d.formTemplateFieldsJson ?? '[]'); } catch { fields = []; }
    this.draftTemplateFields.set(fields);
    let values: Record<string, string> = {};
    try { values = JSON.parse(d.fieldValuesJson); } catch { values = {}; }
    this.fieldValues.set(values);
    this.detailError.set(null);
  }

  closeDetail(): void {
    this.detail.set(null);
    this.draftId.set(null);
    this.draftTemplateFields.set([]);
    this.fieldValues.set({});
    this.detailMode.set('view');
    this.actionModal.set(null);
    this.actionComment.set('');
    this.selectedStoreId.set(null);
  }

  setFieldValue(fieldId: string, value: string): void {
    this.fieldValues.update((v) => ({ ...v, [fieldId]: value }));
  }

  getFieldValue(fieldId: string): string {
    return this.fieldValues()[fieldId] ?? '';
  }

  submitFill(): void {
    const storeId = this.currentUser()?.storeId ?? this.selectedStoreId() ?? null;
    if (!storeId) { this.detailError.set('Please select a store for this submission.'); return; }

    this.detailBusy.set(true);
    this.detailError.set(null);

    const id = this.draftId();
    if (id) {
      this.submissionSvc.submitSubmission(id, { fieldValues: this.fieldValues() }).subscribe({
        next: () => { this.detailBusy.set(false); this.closeDetail(); this.loadMine(); this.loadReview(); if (this.isAdminOrSupervisor()) this.loadAll(); },
        error: (err) => { this.detailBusy.set(false); this.detailError.set(this.extractError(err)); },
      });
      return;
    }

    // First save — need the templateId. Pull it from the originally-picked template fields context.
    const templateId = this.pendingCreateTemplateId;
    this.submissionSvc.createSubmission({ formTemplateId: templateId, storeId, fieldValues: this.fieldValues() }).subscribe({
      next: (res) => {
        this.draftId.set(res.id);
        this.submissionSvc.submitSubmission(res.id, { fieldValues: this.fieldValues() }).subscribe({
          next: () => { this.detailBusy.set(false); this.closeDetail(); this.loadMine(); this.loadReview(); if (this.isAdminOrSupervisor()) this.loadAll(); },
          error: (err) => { this.detailBusy.set(false); this.detailError.set(this.extractError(err)); },
        });
      },
      error: (err) => { this.detailBusy.set(false); this.detailError.set(this.extractError(err)); },
    });
  }

  saveDraft(): void {
    this.detailBusy.set(true);
    this.detailError.set(null);
    const existingId = this.draftId();
    if (existingId) {
      this.submissionSvc.updateDraft(existingId, this.fieldValues()).subscribe({
        next: () => { this.detailBusy.set(false); this.closeDetail(); this.loadMine(); },
        error: (err: unknown) => { this.detailBusy.set(false); this.detailError.set(this.extractError(err)); },
      });
      return;
    }
    const storeId = this.currentUser()?.storeId ?? this.selectedStoreId() ?? null;
    if (!storeId) { this.detailBusy.set(false); this.detailError.set('Please select a store.'); return; }
    const templateId = this.pendingCreateTemplateId;
    this.submissionSvc.createSubmission({ formTemplateId: templateId, storeId, fieldValues: this.fieldValues() }).subscribe({
      next: (res) => { this.draftId.set(res.id); this.detailBusy.set(false); this.closeDetail(); this.loadMine(); },
      error: (err) => { this.detailBusy.set(false); this.detailError.set(this.extractError(err)); },
    });
  }

  // --- Review actions ---
  openActionModal(kind: 'reject' | 'return'): void {
    this.actionModal.set(kind);
    this.actionComment.set('');
  }

  confirmAction(): void {
    const id = this.draftId();
    if (!id) return;
    const comment = this.actionComment().trim();
    if (!comment) return;

    this.detailBusy.set(true);
    const kind = this.actionModal();
    const obs = kind === 'reject' ? this.submissionSvc.reject(id, comment) : this.submissionSvc.return(id, comment);
    obs.subscribe({
      next: () => { this.detailBusy.set(false); this.closeDetail(); this.loadMine(); this.loadReview(); },
      error: (err) => { this.detailBusy.set(false); this.detailError.set(this.extractError(err)); },
    });
  }

  approve(): void {
    const id = this.draftId();
    if (!id) return;
    this.detailBusy.set(true);
    this.submissionSvc.approve(id).subscribe({
      next: () => { this.detailBusy.set(false); this.closeDetail(); this.loadMine(); this.loadReview(); },
      error: (err) => { this.detailBusy.set(false); this.detailError.set(this.extractError(err)); },
    });
  }

  saveToggleMode(): SaveToggleMode {
    return this.detailMode() === 'review' ? 'review' : 'fill';
  }

  private extractError(err: unknown): string {
    const e = err as { error?: { message?: string; title?: string } };
    return e?.error?.message ?? e?.error?.title ?? 'Action failed. Please try again.';
  }

  pileLabel(status: FormSubmissionStatus | Pile): string {
    return status === 'PendingApproval' ? 'Pending' : status;
  }

  roleLabel(role: string): string {
    const map: Record<string, string> = {
      store_employee: 'Store Employee',
      store_manager: 'Store Manager',
      supervisor: 'Area Supervisor',
      admin: 'Admin',
    };
    return map[role] ?? role;
  }
}
